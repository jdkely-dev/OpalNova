# Version 1.8.3 Locked Database Recovery Fix Validation Report

This package fixes the startup crash that occurred when the invalid active SQLite database file was still locked by Windows/SQLite during recovery.

## Fix summary
- Clears SQLite connection pools before recovery file operations.
- Runs garbage collection/finalizer cleanup before retrying file operations.
- Moves invalid database files to quarantine instead of delete-first recovery.
- Retries quarantine operations if Windows briefly keeps the file locked.
- If the invalid database remains locked, starts the app against a fresh recovery database path instead of crashing.
- Sidecar `-wal` and `-shm` cleanup is now non-fatal if those files are locked.

## Validation result
- Blocking errors: 0
- Warnings: 0
- Checks run: 106