# OPALNOVA V1.55.0 - Backup Health and Release Readiness

## Summary

V1.55.0 strengthens release readiness and data safety without changing the database schema. The dashboard now shows backup freshness and restore state, restore uses a preview-first workflow, and release notes are available inside OPALNOVA.

## Changes

- Added a dashboard Data Safety card.
- Shows backup status, latest backup timestamp/size, backup folder, active database path/size, and pending restore state.
- Added dashboard actions for Create Backup, Restore Preview, Health Check, and Release Notes.
- Added `DataSafetyService.PreviewRestoreSource()` for `.db` backups and export-bundle `.zip` files.
- Restore preview validates SQLite integrity and displays key table counts before restore staging.
- Added `DataSafetyService.CreateReleaseNotes()` and wired it into Settings & Backup plus Safety & Data Studio.
- Updated About text to V1.55.0.
- Bumped project and visible version metadata to 1.55.0.

## Data Safety

- No database schema changes were introduced.
- Restore behavior remains staged; the active database is not replaced until the next app startup.
- A safety backup is still created before a staged restore is applied.
- Restore preview can be cancelled without changing the active database.

## Validation

- Debug build: passed with zero warnings and zero errors.
- Release publish: passed through `win-x64-self-contained`.
- Published app launch smoke: passed; `OPALNOVA.exe` launched and closed cleanly.
