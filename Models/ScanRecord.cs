namespace UsbScannerClient.Models;

internal enum ScanSendStatus
{
    Pending,
    Sent,
    Failed,
    Rejected
}

internal sealed class ScanRecord
{
    public ScanRecord(string barcode)
    {
        CapturedAt = DateTimeOffset.Now;
        Barcode = barcode;
    }

    public DateTimeOffset CapturedAt { get; }

    public string Barcode { get; }

    public ScanSendStatus Status { get; set; } = ScanSendStatus.Pending;

    public string Message { get; set; } = "Queued";
}
