# OPALNOVA V1.68.0 - Balance Reminder Workflow

V1.68.0 adds payment reminder support to the Payment & Collection workflow.

## What Changed

- Added `Copy Balance Reminder` for the selected job in Payment & Collection.
- Added `Create Balance Follow-Up` for jobs with money still owing.
- Reminder text includes:
  - job code and title
  - total amount
  - paid amount
  - remaining balance
  - due/handover date when available
- Balance follow-up task creation is duplicate-safe for the selected job.

## What Did Not Change

- No database schema changes.
- No payment recording, invoice, receipt, sale creation or job completion logic changed.
- No direct SMS or email delivery was added; OPALNOVA creates copy-ready text and follow-up tasks.

## Validation

- Debug build should complete with zero warnings and zero errors.
- Release publish should complete for `win-x64-self-contained`.
- Published `OPALNOVA.exe` should launch and close cleanly.
