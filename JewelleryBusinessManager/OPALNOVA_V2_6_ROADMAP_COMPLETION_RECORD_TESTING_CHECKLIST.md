# OPALNOVA V2.6.0 Roadmap Completion Record Testing Checklist

Use this checklist against the published V2.6 build before treating the roadmap completion pass as complete.

- Launch OPALNOVA and confirm the header shows `Version 2.6.0 - roadmap completion record`.
- Open About and confirm it shows `Version 2.6.0 - Roadmap Completion Record`.
- Open Settings & Backup and run `Roadmap Completion Record`; confirm the report opens in preview.
- Open Safety & Data Studio and run `Roadmap Completion Record`; confirm the report opens in preview.
- Confirm the report records the current no-schema version stream as complete.
- Confirm the report lists completed tracks and remaining explicit major decisions.
- Confirm the report says future work should start only after choosing one named product direction with acceptance criteria.
- Confirm the report says it does not create installer files, shortcuts, update feeds, task records, background jobs, data moves, supplier mutations, hardware dependencies or schema changes.
- Use Search All for `roadmap completion`; confirm the workflow action appears and opens Settings & Backup.
- Open Settings & Backup and run `Release Notes`; confirm V2.6.0 appears above V2.5.0.
- Open Settings & Backup and run `User Guide`; confirm the manual version shows V2.6.0 and mentions Roadmap Completion Record in data safety/release testing guidance.
- Confirm Roadmap Completion Record mini help opens action-specific guidance in both Settings & Backup and Safety & Data Studio.
- Confirm no Roadmap Completion Record action creates tasks, payments, stock movements, sales, supplier diamond state changes, shortcuts, installer files or database schema changes.
- Confirm Debug build, Release publish, published file/version check and published launch smoke have passed before treating this as release-ready.
