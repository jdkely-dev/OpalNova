# OPALNOVA V1.47.8 — Compile + SQLite Runtime Security Fix

## Fixed

- Corrected the invalid `new Thickness(8,4)` constructor in `EditEntityWindow.xaml.cs`.
- Replaced it with the valid four-value WPF constructor `new Thickness(8, 4, 8, 4)`.
- Added an explicit `SQLitePCLRaw.bundle_e_sqlite3` 3.0.3 package reference so NuGet resolves the SQLitePCLRaw v3 native SQLite runtime path instead of the deprecated/vulnerable 2.1.11 native package.
- Updated app/package metadata to 1.47.8.

## Kept stable

- No database schema changes.
- No quote, diamond, production, payment, sales, or report logic changes.
- No UI layout feature changes beyond the compile fix.

## Testing checklist

1. Rebuild in Visual Studio.
2. Confirm the `Thickness` compile error is gone.
3. Restore NuGet packages and confirm the SQLitePCLRaw warning is gone or reduced.
4. Open OPALNOVA.
5. Test New Customer / Edit Job record editor layout.
6. Publish and open `OPALNOVA.exe`.
