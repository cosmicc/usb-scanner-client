# USB Scanner Client

Small Windows Forms client for a USB barcode scanner connected to a Windows PC.
Most USB scanners act like a keyboard: they type the scanned value and press
Enter. This app keeps focus on the scan box, records each scan locally in the
window, and sends the barcode to the industrial scanner logger TCP receiver.

## Required Server Component

This application is not a standalone scanner logging system. It is a Windows USB scanner client for the Industrial Scanner Logger project.

The USB Scanner Client requires the server-side Industrial Scanner Logger receiver to be installed and running:

https://github.com/cosmicc/industrial-scanner-logger

The client captures scans from a USB keyboard-wedge barcode scanner, then sends those scan events over TCP to the Industrial Scanner Logger receiver. The server handles the actual scan classification, CSV logging, PostgreSQL logging, API/web interface support, duplicate handling, and failed-scan recording.

By default, the client sends scans to TCP port `55256`, which matches the default receiver port used by `industrial-scanner-logger`.

Without the Industrial Scanner Logger server running and reachable, this client can still capture scans locally in the window and queue valid scans, but it cannot complete the intended logging workflow.

## Current Framework

- C# WinForms project targeting `net10.0-windows`.
- Current version: `v1.0.4`.
- The release executable is a self-contained Windows x64 single EXE. The .NET
  runtime is built into `UsbScannerClient.exe`, so users do not need to install
  the .NET Desktop Runtime separately.
- Default receiver target: `127.0.0.1:55256`.
- Scans are sent as one UTF-8 barcode followed by `CRLF`, matching the scanner
  TCP frame shape the logger already accepts.
- Scans complete when the scanner sends Enter/CR/LF, or when the configurable
  scan idle timeout expires for scanners that do not send a terminator.
- The TCP connection is kept open so this client behaves like a network scanner.
- If the server is disconnected, valid scans are held in a queue and saved to
  `%APPDATA%\UsbScannerClient\queued-scans.json` until they are sent.
- The window shows receiver connection state with a red/green indicator and the
  last barcode scanned.
- Receiver host, port, timeout, scan idle timeout, and auto-connect are managed
  in the separate Settings window.
- Auto-update is enabled by default. The app checks GitHub Releases at startup
  and Settings includes a `Check now` button for manual update checks.
- The on-screen log records captured time, barcode, send status, and errors,
  with green rows for sent scans, yellow rows for queued scans, and red rows for
  rejected or failed scans.
- The lower status bar shows session totals: total scans, sent scans, queued
  scans, send failures, and rejected scans.
- Settings are saved to `%APPDATA%\UsbScannerClient\settings.json`.

The paired `industrial-scanner-logger` receiver reads TCP data, splits barcode
events on `CR`, `LF`, or `CRLF`, and defaults to port `55256`.

## Scan Completion and Buffering

Most USB scanners type the barcode and then send Enter. For scanners that do not
send Enter/CR/LF, the client treats the barcode as complete after the configured
scan idle timeout, which defaults to `250 ms`.

If the receiver is disconnected, valid 34-digit scans are queued and written to
`%APPDATA%\UsbScannerClient\queued-scans.json`. Press `Connect` to reconnect;
once the TCP connection is open, the queued scans are sent to the server in the
order they were captured. If the app closes before the queue is sent, those
scans are restored into the scan list the next time the app opens. Rejected
scans are not queued or sent.

Use `Clear Queue` only when queued scans should be discarded. The app asks for
confirmation before clearing unsent scans, and it warns on close when unsent
queued scans remain.

## Scan Rules

- Normal successful payloads must be numeric and exactly 34 digits.
- Payloads shorter than 10 digits are rejected locally and are not sent or
  queued.
- Numeric payloads from 10 to 33 digits are rejected locally and are not sent or
  queued.
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

The release workflow and local publish command build a self-contained Windows
x64 single executable. End users only need `UsbScannerClient.exe`; the required
.NET runtime is built into that file. There is no installer and no separate
.NET Desktop Runtime download for normal use.

## Auto Updates

When auto-update is enabled, the app checks the normal GitHub releases for this
repository, chooses the highest non-prerelease version tag, and looks for the
`UsbScannerClient.exe` release asset. If a newer version is available, it
prompts before downloading or applying anything.

If the update is accepted, the app downloads the new executable to a temporary
folder, shows download progress, closes, replaces the existing executable, and
restarts. GitHub releases and the `UsbScannerClient.exe` asset must be reachable
from the Windows PC. For a private repository, set the
`USB_SCANNER_CLIENT_GITHUB_TOKEN` environment variable to a GitHub token with
read access to the repository, or make the repository public.

Local publish command:

```powershell
dotnet publish .\UsbScannerClient.csproj -c Release -r win-x64 --self-contained true -p:EnableWindowsTargeting=true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -p:DebugType=none -p:DebugSymbols=false
```

The executable will be under:

```text
bin\Release\net10.0-windows\win-x64\publish\UsbScannerClient.exe
```

## GitHub Releases

Pushing a version tag such as `v1.0.4` runs `.github/workflows/release.yml`.
The workflow builds the self-contained Windows x64 single executable, creates
the GitHub release, and uploads the only app asset as:

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
