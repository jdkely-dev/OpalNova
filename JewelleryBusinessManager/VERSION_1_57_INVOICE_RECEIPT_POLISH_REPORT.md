# OPALNOVA V1.57.0 - Invoice and Receipt Polish

## Summary

V1.57.0 refreshes customer-facing invoice and receipt output while preserving the existing payment, sale and handover workflows. The change is presentation-focused and does not alter database schema or payment calculations.

## Changes

- Updated job invoice/receipt output with a polished document header, status badge, financial summary tiles, customer/job columns, handover status and payment checks.
- Updated sale receipt output with sale amount, payment summary, customer details, item/job context and handover note.
- Updated deposit receipt output with deposit, total, remaining balance and customer/job sections.
- Updated payment receipt output with related job/sale context, reference details and balance status.
- Added shared document helper sections for hero header, financial summary and notices.
- Added document CSS for summary tiles, status badges, notices, and payment ledger alignment.
- Updated release notes and About text to V1.57.0.
- Bumped project and visible version metadata to 1.57.0.

## Data Safety

- No database schema changes were introduced.
- Existing payment creation, sale creation and job completion logic were not changed.
- Generated documents remain read-only HTML files in the normal OPALNOVA printout folder.

## Validation

- Debug build: passed with zero warnings and zero errors.
- Release publish: passed through `win-x64-self-contained`.
- Published app launch smoke: passed; `OPALNOVA.exe` launched and closed cleanly.
