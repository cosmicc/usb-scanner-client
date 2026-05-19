using System.Reflection;
using UsbScannerClient.Models;
using UsbScannerClient.Services;

namespace UsbScannerClient;

public partial class MainForm : Form
{
    private const string AppTitle = "USB Scanner Client";

    private readonly TcpScannerConnection scannerConnection = new();
    private AppSettings settings = AppSettingsStore.Load();
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

    private void SettingsButton_Click(object? sender, EventArgs e)
    {
        AppSettings oldSettings = settings.Copy();

        using var settingsForm = new SettingsForm(settings);
        if (settingsForm.ShowDialog(this) != DialogResult.OK)
        {
            scanInputTextBox.Focus();
            return;
        }

        settings = settingsForm.Settings;
        SaveSettings();

        if (scannerConnection.IsConnected && !oldSettings.HasSameReceiver(settings))
        {
            scannerConnection.Disconnect();
            UpdateServerStatus();
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
        await SubmitCurrentScanAsync();
    }

    private async void SendButton_Click(object? sender, EventArgs e)
    {
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
        string rawBarcode = scanInputTextBox.Text.Trim();
        if (rawBarcode.Length == 0)
        {
            scanInputTextBox.Focus();
            return;
        }

        SaveSettings();
        totalScansTaken++;

        BarcodeValidationResult validation = BarcodeValidator.Validate(rawBarcode);
        var scan = new ScanRecord(validation.Barcode)
        {
            Message = validation.Message
        };

        DataGridViewRow row = AddScanRow(scan);
        lastBarcodeValueLabel.Text = validation.Barcode;
        scanInputTextBox.Clear();

        if (!validation.CanSend)
        {
            rejectedScans++;
            scan.Status = ScanSendStatus.Rejected;
            UpdateScanRow(row, scan);
            UpdateStatsStatusLine();
            scanInputTextBox.Focus();
            return;
        }

        SetBusyState(true);
        UpdateScanRow(row, scan, "Sending");

        try
        {
            await scannerConnection.SendScanAsync(validation.Barcode, settings, CancellationToken.None);
            totalScansSent++;
            if (validation.IsShortScan)
            {
                shortScansSent++;
            }

            scan.Status = ScanSendStatus.Sent;
            scan.Message = validation.IsShortScan
                ? $"Short scan sent for failed-scan logging to {settings.ServerHost}:{settings.ServerPort}"
                : $"Sent to {settings.ServerHost}:{settings.ServerPort}";
        }
        catch (OperationCanceledException)
        {
            failedSends++;
            scan.Status = ScanSendStatus.Failed;
            scan.Message = "Timed out while sending";
        }
        catch (Exception ex)
        {
            failedSends++;
            scan.Status = ScanSendStatus.Failed;
            scan.Message = ex.Message;
        }
        finally
        {
            UpdateScanRow(row, scan);
            UpdateServerStatus();
            SetBusyState(false);
            UpdateStatsStatusLine();
            scanInputTextBox.Focus();
        }
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
            + $"Short sent: {shortScansSent}   Send failures: {failedSends}   "
            + $"Rejected: {rejectedScans}";
    }

    private static string GetVersion()
    {
        return typeof(MainForm).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion
            ?? "0.1.0";
    }
}
