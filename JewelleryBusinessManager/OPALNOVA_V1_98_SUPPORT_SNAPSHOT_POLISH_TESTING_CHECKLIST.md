# OPALNOVA V1.98.0 Support Snapshot Polish Testing Checklist

Use this checklist against the published V1.98 build.

- Launch OPALNOVA and confirm the header shows `Version 1.98.0 - support snapshot polish`.
- Open About and confirm it shows `Version 1.98.0 - Support Snapshot Polish`.
- Open Settings & Backup and run `Support Snapshot`.
- Open Safety & Data Studio and run `Support Snapshot`.
- Confirm the report opens in the in-app preview.
- Confirm the report shows version, executable path, app folder, database path, backup folder, printout folder, photo folder, settings path, saved-view path and error-log path.
- Confirm the report shows latest backup status when `.db` or `.zip` backups exist in the configured backup folder.
- Confirm the report explains what to share for support and warns not to share database backups, export bundles, customer documents or supplier credentials publicly.
- Confirm opening Support Snapshot does not create, edit, restore, import or delete business records.
- Confirm Debug build, Release publish and published launch smoke have passed before treating this as release-ready.
