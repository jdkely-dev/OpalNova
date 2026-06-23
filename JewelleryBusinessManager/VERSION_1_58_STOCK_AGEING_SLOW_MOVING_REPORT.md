# OPALNOVA V1.58.0 - Stock Ageing and Slow-Moving Inventory

## Summary

V1.58.0 adds a read-only stock ageing report for better purchasing, market and listing decisions. It uses existing jewellery and stone records and does not change inventory status or schema.

## Changes

- Added `DocumentExportService.CreateStockAgeingReport()`.
- Added Stock Ageing to the simplified Reports workspace.
- Added Stock Ageing to Reports Studio.
- Added age bands for 0-30, 31-90, 91-180, 181-365 and 365+ days.
- Added slow-moving inventory table for records older than 180 days.
- Report includes unsold jewellery and loose/available stones, excluding sold jewellery and sold or set stones.
- Added release notes and help text for the new report.
- Bumped project and visible version metadata to 1.58.0.

## Data Safety

- No database schema changes were introduced.
- The report is read-only and does not change inventory status.
- Age is based on record `CreatedAt`, so imported historical records may need manual interpretation.

## Validation

- Debug build: passed with zero warnings and zero errors.
- Release publish: passed through `win-x64-self-contained`.
- Published app launch smoke: passed; `OPALNOVA.exe` launched and closed cleanly.
