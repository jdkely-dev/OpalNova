# OPALNOVA V1.86.0 Data Integrity Check Testing Checklist

Use this checklist against the published V1.86 build.

## Startup

- Launch `OPALNOVA.exe`.
- Confirm the header shows `Version 1.86.0 - data integrity check`.
- Open About and confirm it shows `Version 1.86.0 - Data Integrity Check`.

## Entry Points

- From the dashboard Data Safety card, click `Data Integrity`.
- From `Reports / Data`, click `Data Integrity`.
- Open `Settings & Backup`, then click `Data Integrity`.
- Open `Safety & Data Studio`, then click `Data Integrity`.
- Confirm each entry opens a generated report tab or browser preview without changing records.

## Report Content

- Confirm the report title is `OPALNOVA Data Integrity Check`.
- Confirm the report shows generated time and active database path.
- Confirm summary cards show total findings, broken required links, missing optional links/files and review items.
- Confirm record-count cards appear for customers, quotes, quote options, jobs, sales, payments, stock, stones, materials and photos.
- If findings exist, confirm each row shows severity, area, record, field and issue.
- If no findings exist, confirm the report clearly states that no integrity issues were found.

## Safety Checks

- Confirm running Data Integrity does not create, edit, delete, restore or import any business records.
- Confirm existing Health Check still opens the text health report.
- Confirm Create Backup and Restore Preview still behave as before.
- Confirm User Guide and Release Notes still open.

## Regression Checks

- Open Customers, Jobs, Quotes, Jewellery Stock and Payments to confirm records still load.
- Generate or open one existing report to confirm report preview behavior still works.
- Close the app cleanly.

