# OPALNOVA V1.79.0 - Stock Lifecycle Clarity

V1.79.0 continues inventory workflow polish without changing the database schema.

## What Changed

- Added shared stock lifecycle guidance for jewellery stock, stones, quote reservation links and supplier diamonds.
- Change Inventory Status now explains the current status and selected new status before saving.
- Inventory Value, Stock Ageing, Reserved Inventory and Opal / Stone Stock reports now include lifecycle guidance.
- Supplier Diamond Holds & Orders now shows supplier-vs-owned lifecycle context in the grid and selected-diamond detail.
- Supplier diamond reminder tasks now include lifecycle context.
- Cleaned the supplier diamond workflow status path text to plain ASCII.

## Data Safety

- No stock quantities are changed by lifecycle guidance.
- No reservations are consumed, released or created by report generation.
- No supplier diamond state is changed by viewing lifecycle guidance.
- Existing job completion, sale, stock movement and supplier-diamond conversion workflows are preserved.

## Validation

- Debug build: passed with zero warnings and zero errors.
- Release publish: passed through `win-x64-self-contained`.
- Published app launch smoke: passed.
