# Changelog

All notable changes to USB Scanner Client should be recorded here.

## Unreleased

- Fixed switching from CSV mode back to server mode so the main window restores
  server connection controls immediately without restarting.

## v1.0.7 - 2026-06-11

- Added an embedded Windows executable icon based on the scanner artwork.
- Changed CSV mode to show the status indicator as green because CSV output is
  an active healthy output mode, not a disconnected server state.

## v1.0.6 - 2026-06-11

- Added direct CSV mode with a configurable CSV file location in Settings.
- Disabled server connection and queue controls while direct CSV mode is enabled.
- Saved every submitted scan to CSV in direct CSV mode, including validation
  status and validation message columns.

## v1.0.5 - 2026-05-26

- Added this changelog so future releases have a single place to record user-facing changes.
- Fixed auto-update download finalization by closing the completed download file before renaming it on Windows.
- Hardened auto-update application by launching a temporary helper script that waits for the app process to exit, retries replacement while Windows releases file locks, and then restarts the updated executable.

## v1.0.4 - 2026-05-26

- Changed GitHub release builds to publish a self-contained Windows x64 single executable.
- Embedded the .NET runtime into `UsbScannerClient.exe`, removing the end-user requirement to install the .NET Desktop Runtime separately.
- Updated release notes and README documentation for the single-EXE distribution model.

## v1.0.3 - 2026-05-20

- Added documentation that the client requires the server-side Industrial Scanner Logger receiver for the full logging workflow.
- Changed scan validation so only valid 34-digit numeric payloads are sent or queued.
- Rejected short, long, and non-numeric scans locally instead of sending short scans for server-side failed-scan logging.
- Added row colors for sent, queued, failed, and rejected scans.
- Cleaned up status totals by removing the short-scan sent count.

## v1.0.2 - 2026-05-20

- Persisted queued scans to `%APPDATA%\UsbScannerClient\queued-scans.json` so unsent scans survive app restarts.
- Restored queued scans into the scan list when the app starts.
- Added queue clearing with confirmation.
- Warned before closing when queued scans remain unsent.
- Improved update checks by selecting the highest non-prerelease GitHub release and supporting a GitHub token for private release access.

## v1.0.1 - 2026-05-20

- Bumped application and documentation version references to `v1.0.1`.

## v1.0.0 - 2026-05-20

- Added the first stable Windows Forms USB scanner client.
- Captured keyboard-wedge USB scanner input and submitted completed scans over TCP.
- Added configurable receiver host, receiver port, connection timeout, scan idle timeout, and auto-connect behavior.
- Added local scan history, receiver connection state, last scanned barcode display, and session totals.
- Added barcode validation for the expected scanner payload shape.

## v0.1.0-beta.1 - 2026-05-19

- Marked the initial build as a beta prerelease.

## v0.1.0 - 2026-05-19

- Created the initial USB scanner client project.
