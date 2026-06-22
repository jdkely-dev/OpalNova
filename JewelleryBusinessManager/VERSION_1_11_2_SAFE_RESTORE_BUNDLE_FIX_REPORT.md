# Version 1.11.2 — Safe Restore & Export Bundle Lock Fix

## Reason for this patch
Restore Backup and Export Bundle could still fail on Windows with:

> The process cannot access the file because it is being used by another process.

The root cause was that the app could still have live SQLite/Entity Framework handles open while restore/export attempted direct file operations on the active database.

## Fixes applied

- Backup and Export Bundle now use SQLite's online backup API via `SqliteConnection.BackupDatabase(...)` to create a safe database snapshot.
- Export Bundle zips the snapshot instead of the live active database file.
- Temporary snapshot cleanup is now non-fatal if Windows briefly keeps a file handle open.
- Restore Backup no longer overwrites the active database while the app is running.
- Restore Backup now validates the selected `.db` or bundle `.zip`, stages it as `pending-restore.db`, and applies it on the next app startup before SQLite opens the database.
- Startup now calls `DatabaseBootstrapper.ApplyPendingRestoreIfNeeded()` before `DatabaseBootstrapper.Initialize()`.
- The restore dialog now explains that the restore is staged and requires closing/reopening the app.
- Main window version text updated to V1.11.2.

## New restore workflow

1. Click **Restore Backup**.
2. Choose a valid `.db` backup or Export Bundle `.zip`.
3. The app stages the restore and shows a confirmation.
4. Close the app completely.
5. Run the app again.
6. The staged restore is applied before SQLite opens.

## Validation

Static validation passed:

- XML/XAML parsing
- C# brace balance with strings/comments stripped
- XAML click-handler matching
- Pending restore method present
- Pending restore applied before DB initialization
- Restore no longer directly overwrites the active DB while running
- Backup/export use SQLite online backup snapshots
- Partial bundle/snapshot cleanup is non-fatal

Blocking errors: 0
Warnings: 0
