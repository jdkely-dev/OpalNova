# OPALNOVA V1.55 Backup and Release Testing Checklist

## Dashboard Data Safety

- Open Dashboard.
- Confirm the Data Safety card is visible.
- Confirm backup status, backup folder, active database details, and pending restore state are readable.
- Click `Create Backup`.
- Confirm a success message appears.
- Confirm Dashboard backup status updates after the backup.

## Restore Preview

- Click `Restore Preview`.
- Select a known OPALNOVA `.db` backup or export-bundle `.zip`.
- Confirm the preview shows selected file details, active database path, restore staging path, SQLite integrity OK, and key record counts.
- Click `No` to cancel and confirm no restore is staged.
- Repeat with a non-database file and confirm OPALNOVA rejects it safely.

## Release Notes

- Open `Settings & Backup`.
- Click `Release Notes`.
- Confirm release notes open in the in-app preview.
- Confirm V1.55, V1.54, V1.53, V1.52, V1.51, and V1.50 sections are present.

## Regression Checks

- Run `Health Check`.
- Open `About` and confirm it shows V1.55.0.
- Open `User Guide`.
- Create a normal report from Reports.
- Run a debug build if testing from source.
