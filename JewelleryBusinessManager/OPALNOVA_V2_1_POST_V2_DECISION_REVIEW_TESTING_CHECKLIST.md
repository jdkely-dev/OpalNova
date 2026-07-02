# OPALNOVA V2.1.0 Post-V2 Decision Review Testing Checklist

Use this checklist against the published V2.1 build before treating the post-V2 decision review pass as complete.

- Launch OPALNOVA and confirm the header shows `Version 2.1.0 - post-V2 decision review`.
- Open About and confirm it shows `Version 2.1.0 - Post-V2 Decision Review`.
- Open Settings & Backup and run `Decision Review`; confirm the report opens in preview.
- Open Safety & Data Studio and run `Decision Review`; confirm the report opens in preview.
- Confirm the Decision Review report shows workflow footprint, operations load, stock/supplier context and Nivoda readiness.
- Confirm the report includes decision guidance for multi-user/cloud sync, direct email delivery, supplier API ordering, scheduling/capacity, navigation and installer/update direction.
- Open Settings & Backup and run `Release Notes`; confirm V2.1.0 appears above V2.0.0.
- Open Settings & Backup and run `User Guide`; confirm the manual version shows V2.1.0 and mentions Decision Review in data safety/release testing guidance.
- Confirm Decision Review mini help opens action-specific guidance in both Settings & Backup and Safety & Data Studio.
- Confirm no Decision Review action creates tasks, payments, stock movements, sales, supplier diamond state changes or database schema changes.
- Confirm Debug build, Release publish and published launch smoke have passed before treating this as release-ready.
