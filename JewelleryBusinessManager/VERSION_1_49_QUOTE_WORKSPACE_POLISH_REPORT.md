# OPALNOVA V1.49.0 Quote Workspace Polish

## Scope

V1.49.0 starts the quote workspace polish build from the V1.49 plan. The focus is daily quoting workflow clarity, not schema expansion.

## Changes

- Bumped project and visible app version metadata to 1.49.0.
- Added quote status and expiry guidance to the quote detail panel.
- Added a right-side next-action rail with direct actions:
  - save quote.
  - preview proposal.
  - mark selected option as recommended.
  - create a quote follow-up task.
  - accept selected option.
  - create production job.
- Added an option comparison grid that shows each option's total and state.
- Added selected-option summary text for deposit, direct cost, and linked stock counts.
- Added quote follow-up task creation through the existing `BusinessTask` model and `TaskWorkflowService.GenerateTaskCode()`.
- Normalized quote workspace separator text to ASCII in touched quote-facing output.

## Data Safety

- No database schema changes.
- Existing quote, option, inventory link, acceptance, reservation, and job conversion behavior is preserved.
- Follow-up creation uses the existing task table and avoids duplicate open follow-ups for the same quote/customer.

## Validation Checklist

- Build succeeds with zero warnings and zero errors.
- Release publish succeeds.
- Published OPALNOVA executable launches.
- Manual quote smoke:
  - create quote.
  - add/duplicate/remove options.
  - mark recommended option.
  - create follow-up.
  - preview proposal.
  - accept option.
  - create production job.
