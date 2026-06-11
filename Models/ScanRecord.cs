namespace UsbScannerClient.Models;

internal enum ScanSendStatus
{
    Pending,
    Queued,
    Sent,
    SavedToCsv,
    Failed,
    Rejected
}

internal sealed class ScanRecord
{
    public ScanRecord(string barcode, DateTimeOffset? capturedAt = null)
    {
        CapturedAt = capturedAt ?? DateTimeOffset.Now;
        Barcode = barcode;
    }

    public DateTimeOffset CapturedAt { get; }

    public string Barcode { get; }

    public ScanSendStatus Status { get; set; } = ScanSendStatus.Pending;

    public string Message { get; set; } = "Queued";
}
