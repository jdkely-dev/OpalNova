# OPALNOVA V1.71.0 - Final Customer Follow-Up

V1.71.0 adds the final customer after-care follow-up step to the Payment & Collection workflow.

## Implemented

- Added `Create Thank-You Follow-Up` to Payment & Collection handover actions.
- Creates a duplicate-safe `BusinessTask` linked to the selected job and customer.
- Uses `TaskWorkflowService.GenerateTaskCode()` and `TaskWorkflowService.OpenTaskExists(...)`.
- Adds a customer-ready thank-you / after-care message in `FollowUpNotes`.
- Shows the task on the dashboard/work queue.

## Preserved

- No database schema changes.
- No automatic job status, sale, payment, handover document or completion changes.
- Existing balance reminders, pickup reminders, handover confirmation, invoices, receipts and completion workflows are preserved.

## Validation

- Debug build succeeds with zero warnings and zero errors.
- Release publish succeeds through `win-x64-self-contained`.
- Published `OPALNOVA.exe` launches and closes cleanly.
