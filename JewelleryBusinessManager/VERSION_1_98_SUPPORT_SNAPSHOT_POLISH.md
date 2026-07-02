# OPALNOVA V1.98.0 - Support Snapshot Polish

V1.98.0 continues release/support hardening without schema changes.

## Implemented

- Added `DataSafetyService.CreateSupportSnapshotReport()`.
- Added Support Snapshot actions in Settings & Backup and Safety & Data Studio.
- The support snapshot reports installed version, executable path, app folder, database path, backup folder, printout folder, photo folder, settings path, saved-view path and error-log path.
- The support snapshot reports file/folder status and latest `.db` or `.zip` backup in the configured backup folder.
- Added support guidance for what to share when asking for help and what not to share publicly.
- Updated the built-in user guide, release notes and workflow help metadata for the Support Snapshot action.
- Preserved database schema and existing backup, restore, health check, data integrity, release notes, user guide and release readiness behavior.

## Validation

- Debug build succeeded with zero errors.
- Release publish succeeded for `win-x64` self-contained output.
- Published `OPALNOVA.exe` file/version check confirmed `FileVersion` `1.98.0.0` and product version `1.98.0 OPALNOVA Support Snapshot Polish`.
- Published launch smoke is pending because the process-launch escalation was blocked by the app usage-limit approval gate.
- The build and publish surfaced NU1900 warnings because the sandbox could not reach NuGet vulnerability data at `https://api.nuget.org/v3/index.json`.

## Notes

- The support snapshot is read-only and does not include customer records, payment records, inventory rows, supplier credentials or API keys.
- The report is intended to reduce troubleshooting ambiguity around local Windows paths and published runtime location.
