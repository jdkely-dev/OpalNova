# Version 1.11.1 — Export Bundle Lock Fix Validation Report

## Issue fixed

The V1.11 Export Bundle action could fail with:

> The process cannot access the file because it is being used by another process.

The likely cause was that the app attempted to add the live SQLite database file directly into the ZIP bundle while Entity Framework / SQLite still had the active database open.

## Fixes applied

- `DataSafetyService.CreateFullDataBundle()` now creates a temporary SQLite database snapshot using SQLite's backup API before creating the ZIP bundle.
- The active database file is no longer directly zipped.
- The temporary snapshot is validated with the existing SQLite validation routine before it is added to the bundle.
- The temporary snapshot is deleted after the bundle finishes.
- If bundle creation fails, the partial ZIP is deleted.
- `AddFileIfExists()` now reads files with `FileShare.ReadWrite | FileShare.Delete`, making photo/settings export more tolerant of briefly locked files.
- `BackupService.CreateBackup()` now also uses a SQLite snapshot instead of direct file copy, making normal backups safer too.
- About/version text updated to `Version 1.11.1 — Export Bundle Lock Fix`.

## Validation passes

Two static validation passes were run against the final package.

Checked:

- ZIP structure
- `.csproj` XML parsing
- WPF/XAML XML parsing
- XAML event handler matching
- C# brace balance
- Export Bundle no longer directly zips the active database path
- SQLite `BackupDatabase` snapshot usage exists
- Snapshot validation exists
- Locked-file friendly archive copy exists
- Existing backup/restore/database recovery code remains present
- Production batch files remain present
- EF Core package references remain present

Result:

- Blocking errors: 0
- Warnings: 0

## Recommended user test

1. Open the app.
2. Create a normal backup.
3. Click **Export Bundle** while the app is still open.
4. Confirm the ZIP is created.
5. Open the ZIP and confirm it contains:
   - `database/jewellery_business_manager.db`
   - `settings/business-settings.json` if settings exist
   - `exports/*.csv`
   - `photos/` if photos exist
6. Test restore from the created bundle ZIP.
