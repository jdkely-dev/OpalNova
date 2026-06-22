# OPALNOVA V1.17 — Purchase Orders & Supplier Reordering

## Added

- Purchase Orders section.
- Purchase Order Items section.
- Purchasing action menu.
- New Purchase Order workflow.
- Reorder Suggestions workflow based on materials at/below reorder level.
- Mark Ordered workflow.
- Receive Purchase Order workflow that increases linked material quantities and creates material transactions.
- Purchase Order printout.
- Reorder Report.
- Supplier Purchasing dashboard tile group.
- Purchase order CSV exports in Export Bundle.

## Database compatibility

V1.17 adds two tables:

- PurchaseOrders
- PurchaseOrderItems

The startup bootstrapper creates these tables for both new and existing databases.

## Main test workflow

1. Create or select a supplier.
2. Create a material with current quantity at or below reorder level.
3. Use Purchasing → Reorder Suggestions.
4. Open Purchase Orders and Purchase Order Items.
5. Review/edit quantities and unit costs.
6. Use Purchasing → Mark Ordered.
7. Use Purchasing → Receive Purchase Order.
8. Confirm material quantity increases and a Material Transaction is created.
9. Create Purchase Order Printout and Reorder Report.
10. Test Backup, Export Bundle and Restore.
