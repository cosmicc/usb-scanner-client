namespace UsbScannerClient.Services;

internal static class BarcodeValidator
{
    public const int MinimumBarcodeLength = 10;
    public const int SuccessfulBarcodeLength = 34;

    public static BarcodeValidationResult Validate(string scannedValue)
    {
        if (string.IsNullOrWhiteSpace(scannedValue))
        {
            return BarcodeValidationResult.Rejected(scannedValue, "No barcode scanned.");
        }

        string barcode = scannedValue.Trim();
        string payload = barcode;

        if (barcode.StartsWith(']'))
        {
            if (barcode.Length < 4)
            {
                return BarcodeValidationResult.Rejected(barcode, "Incomplete AIM symbology prefix.");
            }

            string aimPrefix = barcode[..3];
            if (!aimPrefix.StartsWith("]C", StringComparison.Ordinal))
            {
                return BarcodeValidationResult.Rejected(
                    barcode,
                    $"Rejected non-Code 128 AIM prefix {aimPrefix}.");
            }

            payload = barcode[3..];
        }

        if (payload.Length > SuccessfulBarcodeLength)
        {
            return BarcodeValidationResult.Rejected(
                payload,
                $"Rejected barcode longer than {SuccessfulBarcodeLength} digits.");
        }

        if (!payload.All(char.IsDigit))
        {
            return BarcodeValidationResult.Rejected(payload, "Rejected non-numeric barcode.");
        }

        if (payload.Length < MinimumBarcodeLength)
        {
            return BarcodeValidationResult.Rejected(
                payload,
                $"Rejected barcode shorter than {MinimumBarcodeLength} digits ({payload.Length} digits).");
        }

        if (payload.Length < SuccessfulBarcodeLength)
        {
            return BarcodeValidationResult.Rejected(
                payload,
                $"Rejected barcode shorter than {SuccessfulBarcodeLength} digits ({payload.Length}/{SuccessfulBarcodeLength} digits).");
        }

        return BarcodeValidationResult.Accepted(
            payload,
            "Valid 34-digit Code 128 payload.");
    }
}

internal sealed class BarcodeValidationResult
{
    private BarcodeValidationResult(string barcode, bool canSend, string message)
    {
        Barcode = barcode;
        CanSend = canSend;
        Message = message;
    }

    public string Barcode { get; }

    public bool CanSend { get; }

    public string Message { get; }

    public static BarcodeValidationResult Accepted(string barcode, string message)
    {
        return new BarcodeValidationResult(barcode, true, message);
    }

    public static BarcodeValidationResult Rejected(string barcode, string message)
    {
        return new BarcodeValidationResult(barcode, false, message);
    }
}
