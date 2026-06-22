# OPALNOVA V1.53.0 - External Diamond Inventory Conversion

## Summary

V1.53.0 starts external diamond production readiness by closing the loop from received supplier diamond to owned inventory. It lets a received external diamond become a normal OPALNOVA loose-stone record without requiring Nivoda API order/hold mutations or schema changes.

## Changes

- Added `ExternalDiamondInventoryService` for converting received supplier diamonds to owned `Stone` records.
- Added duplicate-safe conversion using an `ExternalDiamondId` marker in stone notes.
- Added `ExternalDiamondInventoryConversionResult` for conversion summaries.
- Supplier Diamond Holds & Orders now shows an `Owned Stone` column.
- Supplier Diamond Holds & Orders selected-detail text now shows the linked owned stone code when present.
- Added `Convert To Owned Stone` action after `Mark Received`.
- Converted supplier diamonds move to `Converted To Owned Inventory`.
- Linked quote-option external diamond links also move to `Converted To Owned Inventory`.
- Created stones use normal OPALNOVA loose-stone inventory fields: stone code, diamond type, carat, shape, colour, clarity, cut, estimated value, and source/certificate notes.

## Data Safety

- No database schema changes were introduced.
- No hardcoded credentials were added.
- Conversion requires the supplier diamond to be marked received first.
- Running conversion again reuses the existing linked stone rather than creating duplicates.
- API-level Nivoda hold/order actions remain deferred until the accessible production schema is confirmed.

## Validation

- Debug build: passed with zero warnings and zero errors.
- Release publish: passed through `win-x64-self-contained`.
- Published app launch smoke: passed; `OPALNOVA.exe` launched and closed cleanly.
