# Version 1.31 — Data Cleanup & Bulk Actions

## Purpose
Adds a dedicated Data Cleanup Studio for maintaining data quality as the jewellery business database grows.

## Added
- Data Cleanup Studio workspace.
- Data Quality Report.
- Duplicate Finder report.
- Missing Data Report.
- Bulk Status Update for selected records with supported status fields.
- Bulk Add Selected To Market for selected jewellery stock.
- Create Cleanup Tasks from common issues.
- Extended multi-row selection in the main records table.

## Supported bulk status record types
- Jewellery Stock
- Stones
- Jobs
- Online Listings
- Tasks
- Purchase Orders
- Production Batches
- Other records that expose an enum Status property.

## Data safety
- No new database tables were added.
- Bulk actions require selected records and confirmation prompts.
- Reports are read-only and do not modify data.
- Existing backup, export bundle and restore flow is preserved.

## Validation summary
- XAML/XML parsing: passed.
- Project file parsing: passed.
- XAML event-handler matching: passed.
- C# brace-balance scan: passed.
- Raw interpolated string regression scan: passed.
- Data Cleanup Studio navigation/tool wiring: passed.
- ZIP integrity: passed.
