# OPALNOVA V1.90.0 Stability Milestone Testing Checklist

Use this checklist against the published V1.90 build before relying on it for daily work.

## Startup

- Launch `OPALNOVA.exe`.
- Confirm the header shows `Version 1.90.0 - stability milestone`.
- Open About and confirm it shows `Version 1.90.0 - Stability Milestone`.

## Core Navigation

- Open Dashboard.
- Open Search All and search for a customer/job/quote plus a workflow action such as `release`.
- Open Project Workbench.
- Open Alert Centre.
- Confirm workspace tabs can be closed.

## V1.81-V1.89 Feature Checks

- Open Market Studio and Market Operations.
- Open a record detail view and confirm `+ Photos` allows multi-select image import.
- Open Supplier Diamond Holds & Orders and confirm `Copy Replacement Search` is present.
- Open Production Board and confirm `Capacity Snapshot` is present.
- Open Custom Quote Builder / proposal workflow and confirm proposal output remains revisioned and PDF-ready.
- Open Data Integrity from Settings & Backup or Safety & Data Studio.
- Open Search All and confirm Workflow Actions, Custom Quotes, Quote Options and External Diamonds are searchable.
- Open Jeweller Tools from Pricing Studio or Hardware & POS Studio.
- Open Release Readiness from Settings & Backup or Safety & Data Studio.

## Safety And Reports

- Run Health Check.
- Run Data Integrity.
- Open Release Notes.
- Open User Guide.
- Create a backup or confirm the backup path is valid.
- Preview Restore Backup with a known backup file only if a test backup is available.
- Generate one business report.
- Generate or open one customer-facing document and check OPALNOVA branding/readable text.

## Regression Checks

- Confirm Customers, Jobs, Custom Quotes, Jewellery Stock, Stones, Materials, Payments and Sales sections load.
- Confirm no unexpected records are created just by opening reports/tools.
- Confirm Nivoda settings still require user-entered credentials.
- Close the app cleanly.

