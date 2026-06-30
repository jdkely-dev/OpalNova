# OPALNOVA V1.78.0 - Payment Schedule Guidance

V1.78.0 continues payment and proposal workflow polish without changing the database schema.

## What Changed

- Added shared payment schedule calculation for quotes and jobs.
- Quote totals now show deposit, final balance and remaining-payment guidance.
- Proposal output now includes deposit and final-balance schedule rows.
- Payment & Collection now shows a Payment Schedule panel for the selected job.
- Job payment summary exports now include the same staged payment schedule before the payment ledger.
- Default proposal email wording now supports a `{PaymentSchedule}` placeholder for new settings.
- Added a one-time future change plan grouped by expected version numbers.

## Data Safety

- No new tables or columns were added.
- No existing payment totals are changed by viewing schedule guidance.
- The schedule is advisory and calculated from existing quote, job and payment records.
- Existing payment recording, invoice/receipt generation, handover and balance reminder behavior is preserved.

## Validation

- Debug build: passed with zero warnings and zero errors.
- Release publish: passed through `win-x64-self-contained`.
- Published app launch smoke: passed.
