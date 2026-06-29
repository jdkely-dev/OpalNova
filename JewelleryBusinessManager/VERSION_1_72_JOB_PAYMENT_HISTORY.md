# OPALNOVA V1.72.0 - Job Payment History

V1.72.0 adds in-editor payment visibility for saved jobs.

## Implemented

- Added a saved-job payment history panel to the generic record editor.
- Shows total, paid, balance and payment count inside the job editor.
- Shows linked customer context and whether a sale has already been created for the job.
- Shows read-only payment ledger rows for date, amount, method, reference and notes.
- Reuses the same paid/balance calculation pattern as Payment & Collection for compatibility with older deposit-only records.

## Preserved

- No database schema changes.
- No payment recording logic changes.
- No sale creation, invoice, receipt, handover confirmation or job completion behavior changes.
- New unsaved jobs remain unchanged until saved and reopened with a database ID.

## Validation

- Debug build succeeds with zero warnings and zero errors.
- Release publish succeeds through `win-x64-self-contained`.
- Published `OPALNOVA.exe` launches and closes cleanly.
