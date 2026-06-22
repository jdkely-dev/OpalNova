# OPALNOVA V1.52.0 - Job Completion And Inventory Consumption

## Summary

V1.52.0 starts the safe job-completion and inventory-consumption build. It prevents jobs from being silently marked complete without reviewing linked reserved stock, and it records material consumption through existing material transaction audit rows.

## Changes

- Added `JobCompletionReview`, material-line, stone-line, options, and result models for completion review.
- Added `JobCompletionService` to complete jobs in a single database transaction.
- Added `JobCompletionWindow`, a dark themed checklist for reviewing linked accepted quote reservations before completion.
- Production Board now opens the completion checklist when moving a selected job to Completed.
- Production Board now has a direct `Complete Selected` action.
- Payment & Collection now routes `Mark Collected / Complete` and `Mark Shipped / Complete` through the completion checklist.
- Reserved material links can be marked `Consumed`, with matching negative `MaterialTransaction` rows linked to the job.
- Reserved stone links can be marked `Consumed`, with source stones moved to `SetInJewellery`.
- Unconsumed reserved links can be marked `Released`.
- Outstanding balance and negative material stock require explicit confirmation in the checklist.

## Data Safety

- No database schema changes were introduced.
- No tables are dropped or recreated.
- Material quantity changes happen only after the user confirms the completion checklist.
- Material consumption creates audit rows in the existing `MaterialTransactions` table.
- Reservation lifecycle uses the existing `ReservationStatus` text field.

## Validation

- Debug build: passed with zero warnings and zero errors.
- Release publish: passed through `win-x64-self-contained`.
- Published app launch smoke: passed; `OPALNOVA.exe` launched and closed cleanly.
