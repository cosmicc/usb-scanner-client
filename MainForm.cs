using UsbScannerClient.Models;
using UsbScannerClient.Services;

namespace UsbScannerClient;

public partial class MainForm : Form
{
    private const string AppTitle = "USB Scanner Client";

    private readonly TcpScannerConnection scannerConnection = new();
    private readonly AppUpdateService updateService = new();
    private readonly List<BufferedScan> bufferedScans = [];
    private readonly System.Windows.Forms.Timer scanIdleTimer = new();
    private AppSettings settings = AppSettingsStore.Load();
    private bool isApplyingUpdate;
    private bool isBusy;
    private bool isCheckingForUpdates;
    private bool isFlushing;
    private bool isSubmitting;
    private bool suppressInputTimer;
    private int totalScansTaken;
    private int totalScansSent;
    private int totalScansSavedToCsv;
    private int failedSends;
    private int rejectedScans;

    public MainForm()
    {
        InitializeComponent();
        Text = $"{AppTitle} v{GetVersion()}";
        lastBarcodeValueLabel.Text = "None";
        if (!settings.CsvModeEnabled)
        {
            RestoreQueuedScans();
        }

        ConfigureScanIdleTimer();
        UpdateServerStatus();
        UpdateStatsStatusLine();
    }

    protected override async void OnShown(EventArgs e)
    {
        base.OnShown(e);
        scanInputTextBox.Focus();

        if (!settings.CsvModeEnabled && settings.AutoConnect)
        {
            await ConnectToServerAsync();
        }

        if (settings.AutoUpdate)
        {
            await CheckForUpdatesAsync(this, false);
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        scanIdleTimer.Stop();
        if (!settings.CsvModeEnabled
            && !isApplyingUpdate
            && bufferedScans.Count > 0
            && !ConfirmCloseWithQueuedScans())
        {
            e.Cancel = true;
            scanInputTextBox.Focus();
            return;
        }

        if (!settings.CsvModeEnabled)
        {
            PersistBufferedScans();
        }

        SaveSettings();
        scannerConnection.Disconnect();
        scannerConnection.Dispose();
        base.OnFormClosing(e);
    }

    private void SaveSettings()
    {
        AppSettingsStore.Save(settings);
    }

    private async void ConnectButton_Click(object? sender, EventArgs e)
    {
        if (settings.CsvModeEnabled)
        {
            scanInputTextBox.Focus();
            return;
        }

        if (scannerConnection.IsConnected)
        {
            scannerConnection.Disconnect();
            UpdateServerStatus();
            scanInputTextBox.Focus();
            return;
        }

        await ConnectToServerAsync();
    }

    private async void SettingsButton_Click(object? sender, EventArgs e)
    {
        AppSettings oldSettings = settings.Copy();

        using var settingsForm = new SettingsForm(settings, CheckForUpdatesFromSettingsAsync);
        if (settingsForm.ShowDialog(this) != DialogResult.OK)
        {
            scanInputTextBox.Focus();
            return;
        }

        AppSettings newSettings = settingsForm.Settings;
        if (!oldSettings.CsvModeEnabled && newSettings.CsvModeEnabled)
        {
            PersistBufferedScans();
        }

        settings = newSettings;
        ConfigureScanIdleTimer();
        SaveSettings();

        if (settings.CsvModeEnabled)
        {
            scannerConnection.Disconnect();
            ClearBufferedScanRows();
            UpdateServerStatus();
        }
        else
        {
            if (oldSettings.CsvModeEnabled)
            {
                RestoreQueuedScans();
            }

            if (scannerConnection.IsConnected && !oldSettings.HasSameReceiver(settings))
            {
                scannerConnection.Disconnect();
                UpdateServerStatus();
            }

            if (scannerConnection.IsConnected)
            {
                await FlushBufferedScansAsync();
            }
        }

        UpdateStatsStatusLine();
        scanInputTextBox.Focus();
    }

    private Task CheckForUpdatesFromSettingsAsync(IWin32Window owner)
    {
        return CheckForUpdatesAsync(owner, true);
    }

    private async void ScanInputTextBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode != Keys.Enter)
        {
            return;
        }

        e.SuppressKeyPress = true;
        scanIdleTimer.Stop();
        await SubmitCurrentScanAsync();
    }

    private async void ScanInputTextBox_KeyPress(object? sender, KeyPressEventArgs e)
    {
        if (e.KeyChar is not ('\r' or '\n'))
        {
            return;
        }

        e.Handled = true;
        scanIdleTimer.Stop();
        await SubmitCurrentScanAsync();
    }

    private void ScanInputTextBox_TextChanged(object? sender, EventArgs e)
    {
        if (suppressInputTimer)
        {
            return;
        }

        scanIdleTimer.Stop();
        if (scanInputTextBox.TextLength > 0)
        {
            scanIdleTimer.Start();
        }
    }

    private async void ScanIdleTimer_Tick(object? sender, EventArgs e)
    {
        scanIdleTimer.Stop();
        await SubmitCurrentScanAsync();
    }

    private async void SendButton_Click(object? sender, EventArgs e)
    {
        scanIdleTimer.Stop();
        await SubmitCurrentScanAsync();
    }

    private void ClearQueueButton_Click(object? sender, EventArgs e)
    {
        if (bufferedScans.Count == 0)
        {
            MessageBox.Show(
                this,
                "There are no queued scans to clear.",
                "Clear Queue",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            scanInputTextBox.Focus();
            return;
        }

        DialogResult result = MessageBox.Show(
            this,
            $"There are {bufferedScans.Count} scans still queued and not sent to the server.\n\n"
                + "Clear the queue anyway?",
            "Clear Queued Scans?",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning,
            MessageBoxDefaultButton.Button2);

        if (result != DialogResult.Yes)
        {
            scanInputTextBox.Focus();
            return;
        }

        foreach (BufferedScan bufferedScan in bufferedScans.ToList())
        {
            if (bufferedScan.Row.DataGridView is not null)
            {
                scanLogGrid.Rows.Remove(bufferedScan.Row);
            }
        }

        bufferedScans.Clear();
        PersistBufferedScans();
        UpdateStatsStatusLine();
        scanInputTextBox.Focus();
    }

    private async Task ConnectToServerAsync()
    {
        if (settings.CsvModeEnabled)
        {
            UpdateServerStatus();
            return;
        }

        SaveSettings();
        SetBusyState(true);
        SetServerStatus("Connecting", false);

        try
        {
            await scannerConnection.ConnectAsync(settings, CancellationToken.None);
            UpdateServerStatus();
            await FlushBufferedScansAsync();
        }
        catch (OperationCanceledException)
        {
            scannerConnection.Disconnect();
            SetServerStatus("Timed out", false);
        }
        catch
        {
            scannerConnection.Disconnect();
            SetServerStatus("Failed", false);
        }
        finally
        {
            SetBusyState(false);
            UpdateStatsStatusLine();
            scanInputTextBox.Focus();
        }
    }

    private async Task SubmitCurrentScanAsync()
    {
        if (isSubmitting)
        {
            return;
        }

        scanIdleTimer.Stop();
        string rawBarcode = scanInputTextBox.Text.Trim();
        if (rawBarcode.Length == 0)
        {
            scanInputTextBox.Focus();
            return;
        }

        isSubmitting = true;
        try
        {
            SaveSettings();
            totalScansTaken++;

            BarcodeValidationResult validation = BarcodeValidator.Validate(rawBarcode);
            string loggedBarcode = settings.CsvModeEnabled ? rawBarcode : validation.Barcode;
            var scan = new ScanRecord(loggedBarcode)
            {
                Message = validation.Message
            };
            if (!validation.CanSend)
            {
                rejectedScans++;
            }

            DataGridViewRow row = AddScanRow(scan);
            lastBarcodeValueLabel.Text = loggedBarcode;
            ClearScanInput();

            if (settings.CsvModeEnabled)
            {
                SaveScanToCsv(scan, row, validation);
                return;
            }

            if (!validation.CanSend)
            {
                scan.Status = ScanSendStatus.Rejected;
                UpdateScanRow(row, scan);
                return;
            }

            var bufferedScan = new BufferedScan(scan, row);
            if (!scannerConnection.IsConnected)
            {
                QueueBufferedScan(bufferedScan, "Queued until server connects");
                return;
            }

            SetBusyState(true);
            bool sent = await TrySendScanAsync(bufferedScan);
            if (!sent)
            {
                bufferedScans.Insert(0, bufferedScan);
                PersistBufferedScans();
            }
        }
        finally
        {
            isSubmitting = false;
            SetBusyState(false);
            UpdateServerStatus();
            UpdateStatsStatusLine();
            scanInputTextBox.Focus();
        }
    }

    private async Task FlushBufferedScansAsync()
    {
        if (settings.CsvModeEnabled
            || isFlushing
            || !scannerConnection.IsConnected
            || bufferedScans.Count == 0)
        {
            return;
        }

        isFlushing = true;
        try
        {
            while (bufferedScans.Count > 0 && scannerConnection.IsConnected)
            {
                BufferedScan bufferedScan = bufferedScans[0];

                bool sent = await TrySendScanAsync(bufferedScan);
                if (!sent)
                {
                    break;
                }

                bufferedScans.RemoveAt(0);
                PersistBufferedScans();
            }
        }
        finally
        {
            isFlushing = false;
            UpdateStatsStatusLine();
        }
    }

    private async Task<bool> TrySendScanAsync(BufferedScan bufferedScan)
    {
        UpdateScanRow(bufferedScan.Row, bufferedScan.Scan, "Sending");

        try
        {
            await scannerConnection.SendScanAsync(
                bufferedScan.Scan.Barcode,
                settings,
                CancellationToken.None);

            totalScansSent++;

            bufferedScan.Scan.Status = ScanSendStatus.Sent;
            bufferedScan.Scan.Message = $"Sent to {settings.ServerHost}:{settings.ServerPort}";
            UpdateScanRow(bufferedScan.Row, bufferedScan.Scan);
            return true;
        }
        catch (OperationCanceledException)
        {
            failedSends++;
            scannerConnection.Disconnect();
            MarkBufferedScanQueued(bufferedScan, "Queued after send timeout");
            return false;
        }
        catch (Exception ex)
        {
            failedSends++;
            scannerConnection.Disconnect();
            MarkBufferedScanQueued(bufferedScan, $"Queued after send failure: {ex.Message}");
            return false;
        }
        finally
        {
            UpdateServerStatus();
        }
    }

    private void SaveScanToCsv(
        ScanRecord scan,
        DataGridViewRow row,
        BarcodeValidationResult validation)
    {
        try
        {
            CsvScanWriter.Append(scan, validation, settings.CsvFilePath);
            totalScansSavedToCsv++;
            scan.Status = ScanSendStatus.SavedToCsv;
            scan.Message = validation.CanSend
                ? $"Saved to CSV: {settings.CsvFilePath}"
                : $"Saved rejected scan to CSV: {validation.Message}";
            UpdateScanRow(row, scan);
        }
        catch (Exception ex)
        {
            failedSends++;
            scan.Status = ScanSendStatus.Failed;
            scan.Message = $"CSV save failed: {ex.Message}";
            UpdateScanRow(row, scan);
        }
    }

    private void QueueBufferedScan(BufferedScan bufferedScan, string message)
    {
        if (!bufferedScans.Contains(bufferedScan))
        {
            bufferedScans.Add(bufferedScan);
        }

        MarkBufferedScanQueued(bufferedScan, message);
    }

    private void MarkBufferedScanQueued(BufferedScan bufferedScan, string message)
    {
        bufferedScan.Scan.Status = ScanSendStatus.Queued;
        bufferedScan.Scan.Message = message;
        UpdateScanRow(bufferedScan.Row, bufferedScan.Scan);
        PersistBufferedScans();
        UpdateStatsStatusLine();
    }

    private void RestoreQueuedScans()
    {
        foreach (QueuedScanFileRecord queuedScan in QueuedScanStore.Load())
        {
            BarcodeValidationResult validation = BarcodeValidator.Validate(queuedScan.Barcode);
            if (!validation.CanSend)
            {
                continue;
            }

            var scan = new ScanRecord(validation.Barcode, queuedScan.CapturedAt)
            {
                Status = ScanSendStatus.Queued,
                Message = string.IsNullOrWhiteSpace(queuedScan.Message)
                    ? "Queued from previous session"
                    : queuedScan.Message
            };

            DataGridViewRow row = AddScanRow(scan);
            bufferedScans.Add(new BufferedScan(scan, row));
            lastBarcodeValueLabel.Text = scan.Barcode;
        }

        PersistBufferedScans();
    }

    private void PersistBufferedScans()
    {
        if (settings.CsvModeEnabled)
        {
            return;
        }

        QueuedScanStore.Save(bufferedScans.Select(bufferedScan => new QueuedScanFileRecord
        {
            CapturedAt = bufferedScan.Scan.CapturedAt,
            Barcode = bufferedScan.Scan.Barcode,
            IsShortScan = false,
            Message = bufferedScan.Scan.Message
        }));
    }

    private void ClearBufferedScanRows()
    {
        foreach (BufferedScan bufferedScan in bufferedScans.ToList())
        {
            if (bufferedScan.Row.DataGridView is not null)
            {
                scanLogGrid.Rows.Remove(bufferedScan.Row);
            }
        }

        bufferedScans.Clear();
    }

    private bool ConfirmCloseWithQueuedScans()
    {
        DialogResult result = MessageBox.Show(
            this,
            $"There are {bufferedScans.Count} scans still queued and not sent to the server.\n\n"
                + "They will be saved and restored the next time the app opens.\n\n"
                + "Close the app anyway?",
            "Queued Scans Not Sent",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning,
            MessageBoxDefaultButton.Button2);

        return result == DialogResult.Yes;
    }

    private DataGridViewRow AddScanRow(ScanRecord scan)
    {
        scanLogGrid.Rows.Insert(
            0,
            scan.CapturedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"),
            scan.Barcode,
            scan.Status,
            scan.Message);

        DataGridViewRow row = scanLogGrid.Rows[0];
        ApplyScanRowStyle(row, scan.Status);
        return row;
    }

    private void UpdateScanRow(DataGridViewRow row, ScanRecord scan, string? status = null)
    {
        row.Cells[StatusColumn.Index].Value = status ?? GetScanStatusText(scan.Status);
        row.Cells[MessageColumn.Index].Value = scan.Message;
        ApplyScanRowStyle(row, scan.Status);
    }

    private static string GetScanStatusText(ScanSendStatus status)
    {
        return status switch
        {
            ScanSendStatus.SavedToCsv => "CSV Saved",
            _ => status.ToString()
        };
    }

    private static void ApplyScanRowStyle(DataGridViewRow row, ScanSendStatus status)
    {
        bool waiting = status is ScanSendStatus.Pending or ScanSendStatus.Queued;
        bool successful = status is ScanSendStatus.Sent or ScanSendStatus.SavedToCsv;

        row.DefaultCellStyle.BackColor = status switch
        {
            ScanSendStatus.Sent or ScanSendStatus.SavedToCsv => Color.FromArgb(222, 252, 226),
            ScanSendStatus.Pending or ScanSendStatus.Queued => Color.FromArgb(255, 246, 191),
            _ => Color.FromArgb(255, 226, 226)
        };

        row.DefaultCellStyle.SelectionBackColor = status switch
        {
            ScanSendStatus.Sent or ScanSendStatus.SavedToCsv => Color.FromArgb(77, 190, 98),
            ScanSendStatus.Pending or ScanSendStatus.Queued => Color.FromArgb(226, 181, 72),
            _ => Color.FromArgb(216, 86, 86)
        };

        row.DefaultCellStyle.SelectionForeColor = waiting || successful ? Color.Black : Color.White;
    }

    private void SetBusyState(bool isBusy)
    {
        this.isBusy = isBusy;
        connectButton.Enabled = !isBusy && !settings.CsvModeEnabled;
        settingsButton.Enabled = !isBusy;
        sendButton.Enabled = !isBusy;
        scanInputTextBox.Enabled = !isBusy;
        UpdateClearQueueButtonState();
        UseWaitCursor = isBusy;
    }

    private void UpdateServerStatus()
    {
        if (settings.CsvModeEnabled)
        {
            statusGroupBox.Text = "Output Status";
            serverStatusCaptionLabel.Text = "Output mode";
            SetServerStatus("CSV mode", false);
            connectButton.Text = "Disabled";
            connectButton.Enabled = false;
            sendButton.Text = "Save";
            UpdateClearQueueButtonState();
            return;
        }

        statusGroupBox.Text = "Connection Status";
        serverStatusCaptionLabel.Text = "Server connection";
        bool isConnected = scannerConnection.IsConnected;
        SetServerStatus(isConnected ? "Connected" : "Disconnected", isConnected);
        connectButton.Text = isConnected ? "Disconnect" : "Connect";
        connectButton.Enabled = !isBusy;
        sendButton.Text = "Send";
        UpdateClearQueueButtonState();
    }

    private void SetServerStatus(string status, bool isConnected)
    {
        serverStatusValueLabel.Text = status;
        serverIndicator.IsConnected = isConnected;
    }

    private void UpdateStatsStatusLine()
    {
        if (settings.CsvModeEnabled)
        {
            statusLabel.Text =
                $"Total scans: {totalScansTaken}   CSV saved: {totalScansSavedToCsv}   "
                + $"CSV failures: {failedSends}   Rejected: {rejectedScans}";
        }
        else
        {
            statusLabel.Text =
                $"Total scans: {totalScansTaken}   Sent: {totalScansSent}   "
                + $"Queued: {bufferedScans.Count}   Send failures: {failedSends}   "
                + $"Rejected: {rejectedScans}";
        }

        UpdateClearQueueButtonState();
    }

    private void UpdateClearQueueButtonState()
    {
        clearQueueButton.Enabled = !settings.CsvModeEnabled && !isBusy && bufferedScans.Count > 0;
    }

    private async Task CheckForUpdatesAsync(IWin32Window owner, bool showNoUpdateMessage)
    {
        if (isCheckingForUpdates)
        {
            if (showNoUpdateMessage)
            {
                MessageBox.Show(
                    owner,
                    "An update check is already running.",
                    "Update Check",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }

            return;
        }

        isCheckingForUpdates = true;
        bool showedUpdatePrompt = false;
        try
        {
            AppUpdateInfo? update = await updateService.CheckForUpdateAsync(
                AppUpdateService.GetCurrentVersion(),
                CancellationToken.None);

            if (update is null)
            {
                if (showNoUpdateMessage)
                {
                    MessageBox.Show(
                        owner,
                        $"No new updates available.\n\nCurrent version: v{AppUpdateService.GetCurrentVersionText()}",
                        "No Update Available",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }

                return;
            }

            showedUpdatePrompt = true;
            DialogResult result = MessageBox.Show(
                owner,
                $"New version available: v{update.VersionText}\n"
                    + $"Current version: v{AppUpdateService.GetCurrentVersionText()}\n\n"
                    + "Apply this update now?",
                "New Version Available",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Information);

            if (result != DialogResult.Yes)
            {
                return;
            }

            await DownloadAndApplyUpdateAsync(update, owner);
        }
        catch (Exception ex)
        {
            if (showNoUpdateMessage || showedUpdatePrompt)
            {
                MessageBox.Show(
                    owner,
                    $"Unable to check for updates.\n\n{ex.Message}",
                    "Update Check Failed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }
        finally
        {
            isCheckingForUpdates = false;
            if (!IsDisposed)
            {
                scanInputTextBox.Focus();
            }
        }
    }

    private async Task DownloadAndApplyUpdateAsync(AppUpdateInfo update, IWin32Window owner)
    {
        string destinationPath = AppUpdateService.GetDownloadDestinationPath(update);

        using var progressForm = new UpdateProgressForm();
        progressForm.SetStatus($"Downloading {update.AssetName}...");

        IProgress<UpdateDownloadProgress> progress =
            new Progress<UpdateDownloadProgress>(progressForm.ReportProgress);

        progressForm.Show(owner);
        try
        {
            await updateService.DownloadUpdateAsync(
                update,
                destinationPath,
                progress,
                CancellationToken.None);

            progressForm.SetStatus("Download complete.");
            progressForm.Close();

            MessageBox.Show(
                owner,
                "The update was downloaded. The app will close, replace the executable, and restart.",
                "Apply Update",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            PersistBufferedScans();
            isApplyingUpdate = true;
            AppUpdateService.ApplyDownloadedUpdateAndRestart(destinationPath);
            Application.Exit();
        }
        catch
        {
            if (!progressForm.IsDisposed)
            {
                progressForm.Close();
            }

            throw;
        }
    }

    private void ConfigureScanIdleTimer()
    {
        scanIdleTimer.Stop();
        scanIdleTimer.Interval = Math.Max(100, settings.ScanIdleTimeoutMilliseconds);
        scanIdleTimer.Tick -= ScanIdleTimer_Tick;
        scanIdleTimer.Tick += ScanIdleTimer_Tick;
    }

    private void ClearScanInput()
    {
        suppressInputTimer = true;
        try
        {
            scanInputTextBox.Clear();
        }
        finally
        {
            suppressInputTimer = false;
        }
    }

    private static string GetVersion()
    {
        return AppUpdateService.GetCurrentVersionText();
    }

    private sealed record BufferedScan(ScanRecord Scan, DataGridViewRow Row);
}
