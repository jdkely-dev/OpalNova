# OPALNOVA V1.54.0 - Business Intelligence Excel Export

## Summary

V1.54.0 starts the reports and export upgrade by adding a single Excel-compatible business intelligence workbook. This gives OPALNOVA a practical spreadsheet handoff for business review without changing the database schema or replacing the existing CSV and HTML reports.

## Changes

- Added `DocumentExportService.ExportBusinessIntelligenceExcelWorkbook()`.
- Added SpreadsheetML `.xls` generation with a simple header style and Excel-compatible data cells.
- Added a workbook launcher HTML file so the export can open from the in-app report preview flow.
- Added `Export BI Excel` to the simplified Reports workspace.
- Added `Export BI Excel` to Reports Studio beside the existing BI CSV export.
- Added guided help text for the new export action.
- Bumped project and visible version metadata to 1.54.0.

## Workbook Sheets

- Summary
- Sales
- Outstanding Balances
- Quotes
- Inventory Value
- Reserved Inventory
- Tasks
- External Diamonds

## Data Safety

- No database schema changes were introduced.
- Existing CSV exports and HTML reports remain available.
- Export files are read-only snapshots generated into the normal OPALNOVA printout/report folder.
- The export uses existing report and workflow data only.

## Validation

- Debug build: passed with zero warnings and zero errors.
- Release publish: passed through `win-x64-self-contained`.
- Published app launch smoke: passed; `OPALNOVA.exe` launched and closed cleanly.
