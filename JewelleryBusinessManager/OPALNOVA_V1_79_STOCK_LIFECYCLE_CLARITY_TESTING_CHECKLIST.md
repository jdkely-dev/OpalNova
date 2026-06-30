# OPALNOVA V1.79.0 Stock Lifecycle Clarity Testing Checklist

Use this checklist after launching the V1.79.0 build.

- [ ] Confirm the main header shows `Version 1.79.0 - stock lifecycle clarity`.
- [ ] Open About and confirm it shows `Version 1.79.0 - Stock Lifecycle Clarity`.
- [ ] Open a jewellery stock record and use Change Status.
- [ ] Confirm the current status and selected new status both show lifecycle guidance before saving.
- [ ] Open a stone record and use Change Status.
- [ ] Confirm stone statuses explain available, reserved, work-in-progress, set/consumed and sold meanings.
- [ ] Open Supplier Diamond Holds & Orders.
- [ ] Confirm the grid includes a Lifecycle column.
- [ ] Select a supplier diamond and confirm the selected detail explains whether it is supplier stock, received supplier stock or converted owned stock.
- [ ] Create a supplier diamond reminder only on test data, then confirm the task description includes lifecycle context.
- [ ] Generate Inventory Value and confirm the Stock Lifecycle Guide and lifecycle column are present.
- [ ] Generate Stock Ageing and confirm slow-moving rows include lifecycle guidance.
- [ ] Generate Reserved Inventory and confirm reserved rows explain committed inventory.
- [ ] Generate Opal / Stone Stock and confirm stone rows include lifecycle guidance.
- [ ] Confirm none of the reports change stock quantities, reservation statuses, supplier diamond statuses or sale records.
- [ ] Close the app cleanly.
