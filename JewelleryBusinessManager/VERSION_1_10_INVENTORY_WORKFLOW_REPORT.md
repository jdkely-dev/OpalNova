# Version 1.10 — Workflow & Inventory Tracking

## Added

- New Inventory toolbar group.
- Stock Movement window for receiving, using, returning and adjusting material inventory.
- Material movements now update material current quantity and create linked MaterialTransaction records.
- Optional links from material movements to jobs and jewellery stock items.
- Change Status window for quick jewellery/stone status updates.
- Trace Selected window for customer, material, stone, opal parcel, jewellery item, job, sale, payment, market and market-stock traceability.
- Inventory Audit Report covering low materials, recent movements, reserved stock, at-market stock, jobs due soon and loose/reserved stones.
- Extra dashboard cards for reserved stock, at-market stock, jobs due in 7 days and recent material movements.

## Database compatibility

This version uses the existing MaterialTransaction table and existing status fields. No new database tables are required.

## Recommended tests

- Build and run.
- Use Stock Movement from Materials, Jobs and Jewellery Stock.
- Confirm material quantities change correctly.
- Confirm Material Transactions are created.
- Use Change Status on Jewellery Stock and Stones.
- Use Trace Selected on Materials, Stones, Jewellery Stock, Jobs and Customers.
- Generate Inventory Report.
- Confirm backup and restore still work.
