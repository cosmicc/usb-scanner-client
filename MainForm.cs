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
    private int shortScansSent;
    private int failedSends;
    private int rejectedScans;

    public MainForm()
    {
        InitializeComponent();
        Text = $"{AppTitle} v{GetVersion()}";
        lastBarcodeValueLabel.Text = "None";
        RestoreQueuedScans();
        ConfigureScanIdleTimer();
        UpdateServerStatus();
        UpdateStatsStatusLine();
    }

    protected override async void OnShown(EventArgs e)
    {
        base.OnShown(e);
        scanInputTextBox.Focus();

        if (settings.AutoConnect)
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
        if (!isApplyingUpdate && bufferedScans.Count > 0 && !ConfirmCloseWithQueuedScans())
        {
            e.Cancel = true;
            scanInputTextBox.Focus();
            return;
        }

        PersistBufferedScans();
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

        settings = settingsForm.Settings;
        ConfigureScanIdleTimer();
        SaveSettings();

        if (scannerConnection.IsConnected && !oldSettings.HasSameReceiver(settings))
        {
            scannerConnection.Disconnect();
            UpdateServerStatus();
        }

        if (scannerConnection.IsConnected)
        {
            await FlushBufferedScansAsync();
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
            var scan = new ScanRecord(validation.Barcode)
            {
                Message = validation.Message
            };

            DataGridViewRow row = AddScanRow(scan);
            lastBarcodeValueLabel.Text = validation.Barcode;
            ClearScanInput();

            if (!validation.CanSend)
            {
                rejectedScans++;
                scan.Status = ScanSendStatus.Rejected;
                UpdateScanRow(row, scan);
                return;
            }

            var bufferedScan = new BufferedScan(scan, row, validation.IsShortScan);
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
        if (isFlushing || !scannerConnection.IsConnected || bufferedScans.Count == 0)
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
            if (bufferedScan.IsShortScan)
            {
                shortScansSent++;
            }

            bufferedScan.Scan.Status = ScanSendStatus.Sent;
            bufferedScan.Scan.Message = bufferedScan.IsShortScan
                ? $"Short scan sent for failed-scan logging to {settings.ServerHost}:{settings.ServerPort}"
                : $"Sent to {settings.ServerHost}:{settings.ServerPort}";
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
            var scan = new ScanRecord(queuedScan.Barcode, queuedScan.CapturedAt)
            {
                Status = ScanSendStatus.Queued,
                Message = string.IsNullOrWhiteSpace(queuedScan.Message)
                    ? "Queued from previous session"
                    : queuedScan.Message
            };

            DataGridViewRow row = AddScanRow(scan);
            bufferedScans.Add(new BufferedScan(scan, row, queuedScan.IsShortScan));
            lastBarcodeValueLabel.Text = scan.Barcode;
        }

        PersistBufferedScans();
    }

    private void PersistBufferedScans()
    {
        QueuedScanStore.Save(bufferedScans.Select(bufferedScan => new QueuedScanFileRecord
        {
            CapturedAt = bufferedScan.Scan.CapturedAt,
            Barcode = bufferedScan.Scan.Barcode,
            IsShortScan = bufferedScan.IsShortScan,
            Message = bufferedScan.Scan.Message
        }));
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

        return scanLogGrid.Rows[0];
    }

    private void UpdateScanRow(DataGridViewRow row, ScanRecord scan, string? status = null)
    {
        row.Cells[StatusColumn.Index].Value = status ?? scan.Status.ToString();
        row.Cells[MessageColumn.Index].Value = scan.Message;
    }

    private void SetBusyState(bool isBusy)
    {
        this.isBusy = isBusy;
        connectButton.Enabled = !isBusy;
        settingsButton.Enabled = !isBusy;
        sendButton.Enabled = !isBusy;
        scanInputTextBox.Enabled = !isBusy;
        UpdateClearQueueButtonState();
        UseWaitCursor = isBusy;
    }

    private void UpdateServerStatus()
    {
        bool isConnected = scannerConnection.IsConnected;
        SetServerStatus(isConnected ? "Connected" : "Disconnected", isConnected);
        connectButton.Text = isConnected ? "Disconnect" : "Connect";
    }

    private void SetServerStatus(string status, bool isConnected)
    {
        serverStatusValueLabel.Text = status;
        serverIndicator.IsConnected = isConnected;
    }

    private void UpdateStatsStatusLine()
    {
        statusLabel.Text =
            $"Total scans: {totalScansTaken}   Sent: {totalScansSent}   "
            + $"Queued: {bufferedScans.Count}   Short sent: {shortScansSent}   "
            + $"Send failures: {failedSends}   Rejected: {rejectedScans}";
        UpdateClearQueueButtonState();
    }

    private void UpdateClearQueueButtonState()
    {
        clearQueueButton.Enabled = !isBusy && bufferedScans.Count > 0;
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

    private sealed record BufferedScan(ScanRecord Scan, DataGridViewRow Row, bool IsShortScan);
}
