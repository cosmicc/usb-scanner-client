# USB Scanner Client

Small Windows Forms client for a USB barcode scanner connected to a Windows PC.
Most USB scanners act like a keyboard: they type the scanned value and press
Enter. This app keeps focus on the scan box, records each scan locally in the
window, and sends the barcode to the industrial scanner logger TCP receiver.

## Current Framework

- C# WinForms project targeting `net10.0-windows`.
- Current version: `v1.0.0`.
- The release executable is framework-dependent. Windows PCs must have the
  .NET 10 Desktop Runtime installed before running it.
- Default receiver target: `127.0.0.1:55256`.
- Scans are sent as one UTF-8 barcode followed by `CRLF`, matching the scanner
  TCP frame shape the logger already accepts.
- Scans complete when the scanner sends Enter/CR/LF, or when the configurable
  scan idle timeout expires for scanners that do not send a terminator.
- The TCP connection is kept open so this client behaves like a network scanner.
- If the server is disconnected, valid scans are held in an in-memory queue and
  sent in order after the server connects.
- The window shows receiver connection state with a red/green indicator and the
  last barcode scanned.
- Receiver host, port, timeout, scan idle timeout, and auto-connect are managed
  in the separate Settings window.
- Auto-update is enabled by default. The app checks GitHub Releases at startup
  and Settings includes a `Check now` button for manual update checks.
- The on-screen log records captured time, barcode, send status, and errors.
- The lower status bar shows session totals: total scans, sent scans, queued
  scans, short scans sent for failed-scan logging, send failures, and rejected
  scans.
- Settings are saved to `%APPDATA%\UsbScannerClient\settings.json`.

The paired `industrial-scanner-logger` receiver reads TCP data, splits barcode
events on `CR`, `LF`, or `CRLF`, and defaults to port `55256`.

## Scan Completion and Buffering

Most USB scanners type the barcode and then send Enter. For scanners that do not
send Enter/CR/LF, the client treats the barcode as complete after the configured
scan idle timeout, which defaults to `250 ms`.

If the receiver is disconnected, valid 34-digit and short numeric scans are
queued in memory. Press `Connect` to reconnect; once the TCP connection is open,
the queued scans are sent to the server in the order they were captured.
Rejected scans are not queued or sent. The queue is not persisted if the app is
closed before reconnecting.

## Scan Rules

- Normal successful payloads must be numeric and exactly 34 digits.
- Numeric payloads shorter than 34 digits are still sent to the receiver so the
  server can log them as failed scans.
- Payloads longer than 34 digits are rejected locally and are not sent.
- Non-numeric payloads are rejected locally and are not sent.
- If the scanner is configured to emit an AIM symbology prefix, Code 128 values
  should arrive with a `]C` prefix. The client strips that prefix before
  sending. Without that scanner-side prefix, a keyboard-wedge scanner only gives
  the decoded text, so the app cannot prove the original symbology.

## Development

Open `UsbScannerClient.sln` on Windows in Visual Studio, or build from a Windows
terminal with:

```powershell
dotnet build .\UsbScannerClient.sln
```

## Windows Executable

The release workflow and local publish command intentionally build the version
without .NET baked in. Install the **.NET 10 Desktop Runtime** on the Windows
PC first:

https://dotnet.microsoft.com/en-us/download/dotnet/10.0

On that page, use the Windows x64 installer under **.NET Desktop Runtime**.
Then run `UsbScannerClient.exe`.

## Auto Updates

When auto-update is enabled, the app checks the latest non-prerelease GitHub
release on startup and looks for the `UsbScannerClient.exe` release asset. If a
newer version is available, it prompts before downloading or applying anything.

If the update is accepted, the app downloads the new executable to a temporary
folder, shows download progress, closes, replaces the existing executable, and
restarts. GitHub releases and the `UsbScannerClient.exe` asset must be reachable
from the Windows PC without a GitHub login.

Local publish command:

```powershell
dotnet publish .\UsbScannerClient.csproj -c Release -r win-x64 --self-contained false -p:EnableWindowsTargeting=true -p:PublishSingleFile=true -p:DebugType=none -p:DebugSymbols=false
```

The executable will be under:

```text
bin\Release\net10.0-windows\win-x64\publish\UsbScannerClient.exe
```

## GitHub Releases

Pushing a version tag such as `v1.0.0` runs `.github/workflows/release.yml`.
The workflow builds the framework-dependent Windows executable, creates the
GitHub release, and uploads the executable as:

```text
UsbScannerClient.exe
```

GitHub automatically includes the source code `.zip` and `.tar.gz` archives on
the same release page.

For first manual testing, set the server field to the IP address or DNS name of
the machine running `industrial-scanner-logger`, leave the port at `55256`
unless the receiver config changed, scan a barcode, and confirm the server logs
the event.

The logger identifies scanners by the connecting client's IPv4 last octet. If
this Windows client runs from a workstation IP such as `10.10.10.44`, the logger
will see scanner ID `44`.
