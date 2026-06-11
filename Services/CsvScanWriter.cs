using System.Text;
using UsbScannerClient.Models;

namespace UsbScannerClient.Services;

internal static class CsvScanWriter
{
    private static readonly string[] Header =
    [
        "CapturedAt",
        "Barcode",
        "ValidationStatus",
        "Message"
    ];

    public static void Append(ScanRecord scan, BarcodeValidationResult validation, string csvFilePath)
    {
        ArgumentNullException.ThrowIfNull(scan);
        ArgumentNullException.ThrowIfNull(validation);

        string fullPath = Path.GetFullPath(csvFilePath);
        string? directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        bool writeHeader = !File.Exists(fullPath) || new FileInfo(fullPath).Length == 0;

        using var writer = new StreamWriter(
            fullPath,
            append: true,
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

        if (writeHeader)
        {
            WriteRow(writer, Header);
        }

        WriteRow(
            writer,
            [
                scan.CapturedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss zzz"),
                scan.Barcode,
                validation.CanSend ? "Accepted" : "Rejected",
                validation.Message
            ]);
    }

    private static void WriteRow(TextWriter writer, IEnumerable<string> values)
    {
        writer.WriteLine(string.Join(",", values.Select(Escape)));
    }

    private static string Escape(string value)
    {
        if (value.IndexOfAny([',', '"', '\r', '\n']) < 0)
        {
            return value;
        }

        return $"\"{value.Replace("\"", "\"\"")}\"";
    }
}
