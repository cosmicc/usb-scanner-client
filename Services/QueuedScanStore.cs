using System.Text.Json;

namespace UsbScannerClient.Services;

internal sealed class QueuedScanFileRecord
{
    public DateTimeOffset CapturedAt { get; set; }

    public string Barcode { get; set; } = string.Empty;

    public bool IsShortScan { get; set; }

    public string Message { get; set; } = string.Empty;
}

internal static class QueuedScanStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public static string QueuePath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "UsbScannerClient",
        "queued-scans.json");

    public static IReadOnlyList<QueuedScanFileRecord> Load()
    {
        try
        {
            if (!File.Exists(QueuePath))
            {
                return [];
            }

            string json = File.ReadAllText(QueuePath);
            List<QueuedScanFileRecord>? records =
                JsonSerializer.Deserialize<List<QueuedScanFileRecord>>(json);

            return records?
                .Where(record => !string.IsNullOrWhiteSpace(record.Barcode))
                .OrderBy(record => record.CapturedAt)
                .ToList()
                ?? [];
        }
        catch (Exception)
        {
            return [];
        }
    }

    public static void Save(IEnumerable<QueuedScanFileRecord> records)
    {
        ArgumentNullException.ThrowIfNull(records);

        string? directory = Path.GetDirectoryName(QueuePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        List<QueuedScanFileRecord> queue = records.ToList();
        if (queue.Count == 0)
        {
            if (File.Exists(QueuePath))
            {
                File.Delete(QueuePath);
            }

            return;
        }

        string json = JsonSerializer.Serialize(queue, JsonOptions);
        File.WriteAllText(QueuePath, json);
    }
}
