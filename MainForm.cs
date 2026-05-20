using System.Reflection;
using UsbScannerClient.Models;
using UsbScannerClient.Services;

namespace UsbScannerClient;

public partial class MainForm : Form
{
    private const string AppTitle = "USB Scanner Client";

    private readonly TcpScannerConnection scannerConnection = new();
    private readonly List<BufferedScan> bufferedScans = [];
    private readonly System.Windows.Forms.Timer scanIdleTimer = new();
    private AppSettings settings = AppSettingsStore.Load();
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
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        scanIdleTimer.Stop();
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

        using var settingsForm = new SettingsForm(settings);
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

    private void ClearLogButton_Click(object? sender, EventArgs e)
    {
        scanLogGrid.Rows.Clear();
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
                bufferedScans.RemoveAt(0);

                bool sent = await TrySendScanAsync(bufferedScan);
                if (!sent)
                {
                    bufferedScans.Insert(0, bufferedScan);
                    break;
                }
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
        UpdateStatsStatusLine();
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
        connectButton.Enabled = !isBusy;
        settingsButton.Enabled = !isBusy;
        sendButton.Enabled = !isBusy;
        scanInputTextBox.Enabled = !isBusy;
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
        return typeof(MainForm).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion
            ?? "0.1.0";
    }

    private sealed record BufferedScan(ScanRecord Scan, DataGridViewRow Row, bool IsShortScan);
}
