# OPALNOVA V2.4.0 Portable Build Manifest Testing Checklist

Use this checklist against the published V2.4 build before treating the portable build manifest pass as complete.

- Launch OPALNOVA and confirm the header shows `Version 2.4.0 - portable build manifest`.
- Open About and confirm it shows `Version 2.4.0 - Portable Build Manifest`.
- Open Settings & Backup and run `Portable Build Manifest`; confirm the report opens in preview.
- Open Safety & Data Studio and run `Portable Build Manifest`; confirm the report opens in preview.
- Confirm the report shows executable path, FileVersion, ProductVersion and SHA-256.
- Confirm the report shows the app folder, publish-folder signal, total file count, folder count and total size.
- Confirm the report includes top-level file inventory rows for the current app folder.
- Confirm the report shows database, settings, backup and printout paths as local data boundaries.
- Confirm the report includes private-data exclusion guidance and does not include customer records or credentials.
- Confirm the report says it does not copy files, create an installer, create shortcuts, install updates, move data, create tasks or change schema.
- Use Search All for `portable build manifest`; confirm the workflow action appears and opens Settings & Backup.
- Open Settings & Backup and run `Installer Validation Checklist`; confirm its version expectations now reference V2.4.
- Open Settings & Backup and run `Release Notes`; confirm V2.4.0 appears above V2.3.0.
- Open Settings & Backup and run `User Guide`; confirm the manual version shows V2.4.0 and mentions Portable Build Manifest in data safety/release testing guidance.
- Confirm Portable Build Manifest mini help opens action-specific guidance in both Settings & Backup and Safety & Data Studio.
- Confirm no Portable Build Manifest action creates tasks, payments, stock movements, sales, supplier diamond state changes, shortcuts, installer files or database schema changes.
- Confirm Debug build, Release publish, published file/version check and published launch smoke have passed before treating this as release-ready.
