namespace UsbScannerClient;

internal sealed class AppSettings
{
    public string ServerHost { get; set; } = "127.0.0.1";

    public int ServerPort { get; set; } = 55256;

    public int SendTimeoutMilliseconds { get; set; } = 5000;

    public int ScanIdleTimeoutMilliseconds { get; set; } = 250;

    public bool AutoConnect { get; set; }

    public bool AutoUpdate { get; set; } = true;

    public bool CsvModeEnabled { get; set; }

    public string CsvFilePath { get; set; } = GetDefaultCsvFilePath();

    public AppSettings Copy()
    {
        return new AppSettings
        {
            ServerHost = ServerHost,
            ServerPort = ServerPort,
            SendTimeoutMilliseconds = SendTimeoutMilliseconds,
            ScanIdleTimeoutMilliseconds = ScanIdleTimeoutMilliseconds,
            AutoConnect = AutoConnect,
            AutoUpdate = AutoUpdate,
            CsvModeEnabled = CsvModeEnabled,
            CsvFilePath = CsvFilePath
        };
    }

    public bool HasSameReceiver(AppSettings other)
    {
        return string.Equals(ServerHost, other.ServerHost, StringComparison.OrdinalIgnoreCase)
            && ServerPort == other.ServerPort;
    }

    public static string GetDefaultCsvFilePath()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "UsbScannerClient",
            "scans.csv");
    }
}
