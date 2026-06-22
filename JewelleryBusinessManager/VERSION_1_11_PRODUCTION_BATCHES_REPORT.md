# Version 1.11 — Production Batches & Collection Planning

## Added

- Production Batches section.
- Batch Items section.
- New Production toolbar group:
  - New Batch
  - Add To Batch
  - Batch Progress
  - Batch Report
- Dashboard cards:
  - Active Batches
  - Batch Retail Value
  - Batch Progress
- Production batch traceability support.
- Production batch report printout.
- CSV/data bundle support for production batches and batch items.

## Database notes

V1.11 adds two SQLite tables:

- ProductionBatches
- ProductionBatchItems

The startup bootstrapper creates these tables with `CREATE TABLE IF NOT EXISTS`, so existing databases can be upgraded without needing manual migrations.

## Suggested test path

1. Build and run.
2. Click New Batch and create a test batch.
3. Select a jewellery item, stone, or job and click Add To Batch.
4. Open Batch Items and confirm the linked line appears.
5. Edit the batch item and update Completed Quantity.
6. Select the batch and click Batch Progress.
7. Click Batch Report.
8. Test backup, restore, export bundle and CSV export.
