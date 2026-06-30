# OPALNOVA V1.86.0 Data Integrity Check

V1.86.0 adds a deeper read-only data integrity inspection layer for release safety and day-to-day maintenance.

## Implemented

- Bumped visible/project version metadata to `1.86.0`.
- Added `DataSafetyService.CreateDataIntegrityReport()`.
- Added Data Integrity access from:
  - Dashboard Data Safety card.
  - Reports / Data menu.
  - Settings & Backup.
  - Safety & Data Studio.
- The generated HTML report checks:
  - Orphaned required and optional links across quotes, quote options, jobs, payments, sales, materials, inventory, market stock, production batches, purchase orders, tasks and photos.
  - Missing proposal HTML files.
  - Missing quote-option design image files.
  - Missing photo files and missing photo parent records.
  - Negative material quantities and negative job balances.
  - Sales and payments with zero/negative amounts.
  - Payments not linked to a customer, job or sale.
  - Market stock rows marked both sold and returned.
- Preserved database schema, backup/restore behavior and existing Health Check behavior.

## Deferred

- True automatic backup scheduling remains deferred until OPALNOVA has an explicit app lifecycle, installer or Windows Task Scheduler strategy.
- Automatic repair is intentionally not included. Users should back up before manually fixing records flagged by the report.

