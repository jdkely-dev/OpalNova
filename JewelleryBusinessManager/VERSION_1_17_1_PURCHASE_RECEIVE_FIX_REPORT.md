# Version 1.17.1 Purchase Receive Fix Report

## Result

- Blocking errors: 0
- Warnings: 0
- Checks run: 135

## Fixes

- Purchase order receiving now updates linked material current quantity inside an explicit database transaction.
- A material transaction is created for every received linked material line.
- Purchase order items with a missing MaterialId are resolved by exact material code/name where possible.
- Receive now returns a user-facing summary showing updated materials and warnings.
- Purchase order totals are recalculated after adding/editing purchase order items.

## Errors

None

## Warnings

None

## Final ZIP validation

- Blocking errors: 0
- Warnings: 0
- Checks run: 138
