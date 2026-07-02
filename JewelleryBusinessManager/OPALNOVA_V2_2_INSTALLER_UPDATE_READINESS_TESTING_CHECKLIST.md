# OPALNOVA V2.2.0 Installer Update Readiness Testing Checklist

Use this checklist against the published V2.2 build before treating the installer/update readiness pass as complete.

- Launch OPALNOVA and confirm the header shows `Version 2.2.0 - installer/update readiness`.
- Open About and confirm it shows `Version 2.2.0 - Installer Update Readiness`.
- Open Settings & Backup and run `Installer/Update Readiness`; confirm the report opens in preview.
- Open Safety & Data Studio and run `Installer/Update Readiness`; confirm the report opens in preview.
- Confirm the report shows runtime executable/app folder, publish-folder signal, database/settings/backup/printout paths and business context.
- Confirm the report includes installer decision guidance for installer technology, shortcut ownership, local data location, code signing and uninstall behavior.
- Confirm the report includes update-channel guidance for automatic update deferral, version verification, manual update routine and rollback.
- Confirm the report says it does not create an installer, shortcut, update feed, background job, data move or database schema change.
- Use Search All for `installer` or `updates`; confirm the Installer and updates workflow action appears and opens Settings & Backup.
- Open Settings & Backup and run `Release Notes`; confirm V2.2.0 appears above V2.1.0.
- Open Settings & Backup and run `User Guide`; confirm the manual version shows V2.2.0 and mentions Installer/Update Readiness in data safety/release testing guidance.
- Confirm Installer/Update Readiness mini help opens action-specific guidance in both Settings & Backup and Safety & Data Studio.
- Confirm no Installer/Update Readiness action creates tasks, payments, stock movements, sales, supplier diamond state changes, shortcuts, installer files or database schema changes.
- Confirm Debug build, Release publish, published file/version check and published launch smoke have passed before treating this as release-ready.
