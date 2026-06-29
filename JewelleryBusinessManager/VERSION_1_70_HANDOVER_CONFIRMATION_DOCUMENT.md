# OPALNOVA V1.70.0 - Handover Confirmation Document

V1.70.0 adds customer-facing handover confirmation paperwork to the Payment & Collection workflow.

## Implemented

- Added `DocumentExportService.CreateHandoverConfirmationFromJob(...)`.
- Added `Generate Handover Confirmation` to Payment & Collection handover actions.
- Handover confirmation output includes:
  - customer details
  - job details
  - total, paid and balance summary
  - linked job payment ledger
  - collection/shipping checklist
  - handover notes
  - handover status guidance
  - customer and business sign-off lines

## Preserved

- No database schema changes.
- No automatic job status, sale or payment mutation when generating the document.
- Existing invoice, receipt, payment, sale and job completion workflows are preserved.

## Validation

- Debug build succeeds with zero warnings and zero errors.
- Release publish succeeds through `win-x64-self-contained`.
- Published `OPALNOVA.exe` launches and closes cleanly.
