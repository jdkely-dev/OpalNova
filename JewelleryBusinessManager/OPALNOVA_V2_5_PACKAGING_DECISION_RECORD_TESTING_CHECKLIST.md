# OPALNOVA V2.5.0 Packaging Decision Record Testing Checklist

Use this checklist against the published V2.5 build before treating the packaging decision record pass as complete.

- Launch OPALNOVA and confirm the header shows `Version 2.5.0 - packaging decision record`.
- Open About and confirm it shows `Version 2.5.0 - Packaging Decision Record`.
- Open Settings & Backup and run `Packaging Decision Record`; confirm the report opens in preview.
- Open Safety & Data Studio and run `Packaging Decision Record`; confirm the report opens in preview.
- Confirm the report records portable publish-folder handoff as the validated route.
- Confirm the report keeps MSIX and Inno Setup as explicit future packaging tickets.
- Confirm the report shows executable evidence, local data boundaries and the release/readiness/validation/manifest/support evidence chain.
- Confirm the report includes allowed next actions and non-negotiable packaging boundaries.
- Confirm the report says it does not create installer files, shortcuts, update feeds, task records, background jobs, data moves or schema changes.
- Use Search All for `packaging decision`; confirm the workflow action appears and opens Settings & Backup.
- Open Settings & Backup and run `Release Notes`; confirm V2.5.0 appears above V2.4.0.
- Open Settings & Backup and run `User Guide`; confirm the manual version shows V2.5.0 and mentions Packaging Decision Record in data safety/release testing guidance.
- Confirm Packaging Decision Record mini help opens action-specific guidance in both Settings & Backup and Safety & Data Studio.
- Confirm no Packaging Decision Record action creates tasks, payments, stock movements, sales, supplier diamond state changes, shortcuts, installer files or database schema changes.
- Confirm Debug build, Release publish, published file/version check and published launch smoke have passed before treating this as release-ready.
