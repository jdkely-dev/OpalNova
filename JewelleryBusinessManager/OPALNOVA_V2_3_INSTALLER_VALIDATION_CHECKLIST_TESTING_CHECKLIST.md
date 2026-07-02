# OPALNOVA V2.3.0 Installer Validation Checklist Testing Checklist

Use this checklist against the published V2.3 build before treating the installer validation pass as complete.

- Launch OPALNOVA and confirm the header shows `Version 2.3.0 - installer validation checklist`.
- Open About and confirm it shows `Version 2.3.0 - Installer Validation Checklist`.
- Open Settings & Backup and run `Installer Validation Checklist`; confirm the report opens in preview.
- Open Safety & Data Studio and run `Installer Validation Checklist`; confirm the report opens in preview.
- Confirm the report shows executable path, FileVersion, ProductVersion, SHA-256, app folder and publish-folder signal.
- Confirm the report shows database, settings, backup and printout paths outside the install/publish folder.
- Confirm the report chooses the portable publish folder as the first validation route before MSIX or Inno Setup.
- Confirm the report includes portable publish validation steps, manual update rehearsal gates, rollback checks, installer technology gates and hold conditions.
- Confirm the report says it does not create an installer, shortcut, update feed, background job, task record, data move or database schema change.
- Use Search All for `installer validation` or `portable publish`; confirm the Installer validation workflow action appears and opens Settings & Backup.
- Open Settings & Backup and run `Release Notes`; confirm V2.3.0 appears above V2.2.0.
- Open Settings & Backup and run `User Guide`; confirm the manual version shows V2.3.0 and mentions Installer Validation Checklist in data safety/release testing guidance.
- Confirm Installer Validation Checklist mini help opens action-specific guidance in both Settings & Backup and Safety & Data Studio.
- Confirm no Installer Validation Checklist action creates tasks, payments, stock movements, sales, supplier diamond state changes, shortcuts, installer files or database schema changes.
- Confirm Debug build, Release publish, published file/version check and published launch smoke have passed before treating this as release-ready.
