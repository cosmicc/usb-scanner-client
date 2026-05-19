# USB Scanner Client

Small Windows Forms client for a USB barcode scanner connected to a Windows PC.
Most USB scanners act like a keyboard: they type the scanned value and press
Enter. This app keeps focus on the scan box, records each scan locally in the
window, and sends the barcode to the industrial scanner logger TCP receiver.

## Current Framework

- C# WinForms project targeting `net10.0-windows`.
- Current version: `v0.1.0-beta.1`.
- The release executable is framework-dependent. Windows PCs must have the
  .NET 10 Desktop Runtime installed before running it.
- Default receiver target: `127.0.0.1:55256`.
- Scans are sent as one UTF-8 barcode followed by `CRLF`, matching the scanner
  TCP frame shape the logger already accepts.
- The TCP connection is kept open so this client behaves like a network scanner.
- The window shows receiver connection state with a red/green indicator and the
  last barcode scanned.
- Receiver host, port, timeout, and auto-connect are managed in the separate
  Settings window.
- The on-screen log records captured time, barcode, send status, and errors.
- The lower status bar shows session totals: total scans, sent scans, short
  scans sent for failed-scan logging, send failures, and rejected scans.
- Settings are saved to `%APPDATA%\UsbScannerClient\settings.json`.

The paired `industrial-scanner-logger` receiver reads TCP data, splits barcode
events on `CR`, `LF`, or `CRLF`, and defaults to port `55256`.

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

Local publish command:

```powershell
dotnet publish .\UsbScannerClient.csproj -c Release -r win-x64 --self-contained false -p:EnableWindowsTargeting=true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -p:DebugType=none -p:DebugSymbols=false
```

The executable will be under:

```text
bin\Release\net10.0-windows\win-x64\publish\UsbScannerClient.exe
```

## GitHub Releases

When a GitHub release is published, `.github/workflows/release.yml` builds the
framework-dependent Windows executable and uploads it to that release as:

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
