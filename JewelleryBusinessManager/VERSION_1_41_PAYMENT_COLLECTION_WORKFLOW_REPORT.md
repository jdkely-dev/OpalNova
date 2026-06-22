# OPALNOVA V1.41 — Payment & Collection Workflow

## Purpose
Adds the final handover layer after quoting, reservation and production: payments, balances, pickup/shipping status, receipts, sale creation and completion.

## Added
- New Payment & Collection window.
- Access from Custom Workflow Studio, Production & Opal Studio, and Documents Studio.
- Job list with search and filters for active handover jobs, ready for collection, ready to ship, balances owing, completed and all jobs.
- Payment entry for job-linked deposits, progress payments and final payments.
- Automatic balance recalculation using job total, existing job paid amount and payment records.
- Invoice / receipt generation from the selected job.
- Pickup / handover reminder task creation.
- Handover actions:
  - Mark Ready For Collection
  - Mark Ready To Ship
  - Create Sale From Job
  - Mark Collected / Complete
  - Mark Shipped / Complete
- Duplicate sale prevention for a job that already has a sale record.
- Internal job notes stamped when handover actions are taken.
- Contextual mini-guide help entries for the new tool.

## Data safety
- No new database tables.
- No destructive schema changes.
- Existing Payment, Job, Sale and BusinessTask models are reused.
- Existing quote, inventory reservation and production board workflows are preserved.

## Version updates
- Project version: 1.41.0.
- Visible version text: Version 1.41 — Payment & Collection Workflow.
- Output remains OPALNOVA.exe.

## Validation performed in package preparation
- XAML XML parse check passed.
- Project XML parse check passed.
- New event-handler wiring check passed.
- C# brace balance check passed.
- ZIP integrity check passed.

## Recommended test order
1. Open Payment & Collection from Custom Workflow Studio.
2. Select a job created from an accepted quote.
3. Record a small test payment.
4. Confirm paid and balance figures update.
5. Generate the invoice / receipt.
6. Create a pickup reminder.
7. Mark the job Ready For Collection.
8. Create Sale From Job.
9. Try Create Sale From Job again and confirm duplicate prevention.
10. Mark Collected / Complete.
11. Reopen OPALNOVA and confirm the payment, sale, task and job status remain saved.
