# Version 1.11.3 File Lock Fix

This build replaces the remaining fragile file-copy paths in backup, export bundle and restore staging.

Changes:
- BackupService now uses the SQLite online backup API with retries.
- BackupService now has a shared-read fallback copier for Windows file-lock scenarios.
- Restore staging now copies the selected restore database using shared-read/retry logic.
- Pending restore application now copies the active and staged database using shared-read logic.
- SQLite validation opens files with FileShare.ReadWrite/Delete so validation does not fail just because another handle is open.
- Export bundle continues to zip a snapshot, not the active database file.
