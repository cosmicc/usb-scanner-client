using System.Diagnostics;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;

namespace UsbScannerClient.Services;

internal sealed class AppUpdateInfo
{
    public AppUpdateInfo(
        Version version,
        string versionText,
        string releaseUrl,
        string assetName,
        string assetDownloadUrl)
    {
        Version = version;
        VersionText = versionText;
        ReleaseUrl = releaseUrl;
        AssetName = assetName;
        AssetDownloadUrl = assetDownloadUrl;
    }

    public Version Version { get; }

    public string VersionText { get; }

    public string ReleaseUrl { get; }

    public string AssetName { get; }

    public string AssetDownloadUrl { get; }
}

internal sealed class UpdateDownloadProgress
{
    public UpdateDownloadProgress(long bytesReceived, long? totalBytes)
    {
        BytesReceived = bytesReceived;
        TotalBytes = totalBytes;
    }

    public long BytesReceived { get; }

    public long? TotalBytes { get; }

    public int? Percent =>
        TotalBytes is > 0
            ? (int)Math.Clamp(BytesReceived * 100L / TotalBytes.Value, 0, 100)
            : null;
}

internal sealed class AppUpdateService
{
    private const string LatestReleaseApiUrl =
        "https://api.github.com/repos/cosmicc/usb-scanner-client/releases/latest";

    private const string ExpectedAssetName = "UsbScannerClient.exe";

    private static readonly HttpClient HttpClient = CreateHttpClient();

    public async Task<AppUpdateInfo?> CheckForUpdateAsync(
        Version currentVersion,
        CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await HttpClient.GetAsync(
            LatestReleaseApiUrl,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        await using Stream contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using JsonDocument document = await JsonDocument.ParseAsync(
            contentStream,
            cancellationToken: cancellationToken);

        JsonElement root = document.RootElement;
        if (!root.TryGetProperty("tag_name", out JsonElement tagElement))
        {
            return null;
        }

        string? tagName = tagElement.GetString();
        if (!TryParseReleaseVersion(tagName, out Version? latestVersion, out string versionText)
            || latestVersion is null)
        {
            return null;
        }

        if (latestVersion <= currentVersion)
        {
            return null;
        }

        if (!TryFindExeAsset(root, out string assetName, out string downloadUrl))
        {
            throw new InvalidOperationException(
                $"Release {versionText} does not include {ExpectedAssetName}.");
        }

        string releaseUrl = root.TryGetProperty("html_url", out JsonElement urlElement)
            ? urlElement.GetString() ?? string.Empty
            : string.Empty;

        return new AppUpdateInfo(
            latestVersion,
            versionText,
            releaseUrl,
            assetName,
            downloadUrl);
    }

    public async Task DownloadUpdateAsync(
        AppUpdateInfo update,
        string destinationPath,
        IProgress<UpdateDownloadProgress> progress,
        CancellationToken cancellationToken)
    {
        string? destinationDirectory = Path.GetDirectoryName(destinationPath);
        if (!string.IsNullOrEmpty(destinationDirectory))
        {
            Directory.CreateDirectory(destinationDirectory);
        }

        string partialPath = destinationPath + ".download";
        if (File.Exists(partialPath))
        {
            File.Delete(partialPath);
        }

        using HttpRequestMessage request = new(HttpMethod.Get, update.AssetDownloadUrl);
        using HttpResponseMessage response = await HttpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        long? totalBytes = response.Content.Headers.ContentLength;
        await using Stream remoteStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using FileStream localStream = new(
            partialPath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None);

        byte[] buffer = new byte[81920];
        long totalRead = 0;
        while (true)
        {
            int read = await remoteStream.ReadAsync(buffer, cancellationToken);
            if (read == 0)
            {
                break;
            }

            await localStream.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            totalRead += read;
            progress.Report(new UpdateDownloadProgress(totalRead, totalBytes));
        }

        await localStream.FlushAsync(cancellationToken);

        if (File.Exists(destinationPath))
        {
            File.Delete(destinationPath);
        }

        File.Move(partialPath, destinationPath);
    }

    public static Version GetCurrentVersion()
    {
        string? informationalVersion = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion;

        if (TryParseReleaseVersion(informationalVersion, out Version? version, out _)
            && version is not null)
        {
            return version;
        }

        return Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0, 0);
    }

    public static string GetCurrentVersionText()
    {
        string? informationalVersion = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion;

        return string.IsNullOrWhiteSpace(informationalVersion)
            ? GetCurrentVersion().ToString(3)
            : informationalVersion;
    }

    public static string GetDownloadDestinationPath(AppUpdateInfo update)
    {
        string versionDirectory = SanitizePathSegment(update.VersionText);
        return Path.Combine(
            Path.GetTempPath(),
            "UsbScannerClient",
            "Updates",
            versionDirectory,
            ExpectedAssetName);
    }

    public static void ApplyDownloadedUpdateAndRestart(string downloadedExePath)
    {
        string currentExecutablePath = Application.ExecutablePath;
        if (!File.Exists(currentExecutablePath)
            || !currentExecutablePath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Auto-update can only replace the published Windows executable.");
        }

        if (!File.Exists(downloadedExePath))
        {
            throw new FileNotFoundException("Downloaded update executable was not found.", downloadedExePath);
        }

        string scriptPath = Path.Combine(
            Path.GetTempPath(),
            "UsbScannerClient",
            "Updates",
            $"apply-update-{Guid.NewGuid():N}.cmd");

        Directory.CreateDirectory(Path.GetDirectoryName(scriptPath)!);
        File.WriteAllText(
            scriptPath,
            BuildApplyScript(downloadedExePath, currentExecutablePath, Environment.ProcessId));

        Process.Start(new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"/C \"{scriptPath}\"",
            CreateNoWindow = true,
            UseShellExecute = false,
            WindowStyle = ProcessWindowStyle.Hidden
        });
    }

    private static HttpClient CreateHttpClient()
    {
        var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.UserAgent.Add(
            new ProductInfoHeaderValue("UsbScannerClient", GetCurrentVersion().ToString(3)));
        httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        return httpClient;
    }

    private static bool TryFindExeAsset(
        JsonElement releaseRoot,
        out string assetName,
        out string downloadUrl)
    {
        assetName = string.Empty;
        downloadUrl = string.Empty;

        if (!releaseRoot.TryGetProperty("assets", out JsonElement assetsElement)
            || assetsElement.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        foreach (JsonElement assetElement in assetsElement.EnumerateArray())
        {
            string? name = assetElement.TryGetProperty("name", out JsonElement nameElement)
                ? nameElement.GetString()
                : null;

            if (!string.Equals(name, ExpectedAssetName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string? browserDownloadUrl =
                assetElement.TryGetProperty("browser_download_url", out JsonElement downloadElement)
                    ? downloadElement.GetString()
                    : null;

            if (string.IsNullOrWhiteSpace(browserDownloadUrl))
            {
                continue;
            }

            assetName = name ?? ExpectedAssetName;
            downloadUrl = browserDownloadUrl;
            return true;
        }

        return false;
    }

    private static bool TryParseReleaseVersion(
        string? value,
        out Version? version,
        out string versionText)
    {
        version = null;
        versionText = string.Empty;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string cleaned = value.Trim();
        if (cleaned.StartsWith('v') || cleaned.StartsWith('V'))
        {
            cleaned = cleaned[1..];
        }

        int suffixStart = cleaned.IndexOfAny(['-', '+']);
        if (suffixStart >= 0)
        {
            cleaned = cleaned[..suffixStart];
        }

        string[] parts = cleaned.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length is < 2 or > 4)
        {
            return false;
        }

        while (parts.Length < 3)
        {
            parts = [.. parts, "0"];
        }

        string normalized = string.Join(".", parts);
        if (!Version.TryParse(normalized, out Version? parsedVersion))
        {
            return false;
        }

        version = parsedVersion;
        versionText = parsedVersion.ToString(3);
        return true;
    }

    private static string SanitizePathSegment(string value)
    {
        char[] invalidCharacters = Path.GetInvalidFileNameChars();
        return new string(value.Select(c => invalidCharacters.Contains(c) ? '_' : c).ToArray());
    }

    private static string BuildApplyScript(
        string sourcePath,
        string targetPath,
        int processId)
    {
        return $"""
            @echo off
            setlocal
            set "source={sourcePath}"
            set "target={targetPath}"
            set "pid={processId}"

            :wait_for_app
            tasklist /FI "PID eq %pid%" 2>NUL | findstr /R /C:"%pid%" >NUL
            if not errorlevel 1 (
                timeout /T 1 /NOBREAK >NUL
                goto wait_for_app
            )

            copy /Y "%source%" "%target%" >NUL
            start "" "%target%"
            del "%~f0"
            """;
    }
}
