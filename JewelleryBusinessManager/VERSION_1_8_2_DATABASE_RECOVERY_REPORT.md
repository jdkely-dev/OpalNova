# Version 1.8.2 Database Recovery / Restore Validation Fix

## Issue reported
Visual Studio startup failed with:

SQLite Error 26: 'file is not a database'.

## Likely cause
A non-SQLite file had been copied to the active database path during restore testing, or the active database file became corrupted/invalid.

## Fixes applied
- Startup now catches SQLite Error 26 during database initialization.
- If the active database file is invalid, it is quarantined to LocalAppData/JewelleryBusinessManager with an `.invalid-YYYYMMDD-HHMMSS.db` name.
- A recovery note is written to the same folder.
- The app creates a fresh database so it can start again.
- Restore now validates the selected restore file before copying it into the active database path.
- Restore now checks the SQLite file header.
- Restore now runs `PRAGMA integrity_check` on selected `.db` restore files.
- Restore gives clearer errors if the user selects a CSV, HTML, text file, invalid `.db`, or renamed ZIP.

## Important note
If the app starts with a fresh database after this recovery, use Restore Backup again and select a valid backup created by Create Backup, or a full Export Bundle ZIP.
