# OPALNOVA V1.93.0 - Inventory Reorder Intelligence

V1.93.0 continues the inventory decision-support pass with a consolidated no-schema inventory report.

## Implemented

- Added `DocumentExportService.CreateInventoryDecisionReport()`.
- Added `Inventory Intelligence` actions in:
  - Inventory.
  - Reports.
  - Inventory Studio.
  - Reports Studio.
- The report combines:
  - inventory valuation by category,
  - low-stock reorder recommendations,
  - incoming open purchase-order coverage,
  - slow-moving stock guidance,
  - supplier diamond decision state,
  - recent material adjustment audit signals.
- Kept the report read-only.
- Did not create purchase orders, change stock quantities, convert supplier diamonds or alter stock statuses.
- Preserved database schema and existing Inventory Value, Stock Ageing, Reorder Report and Stock Movement behavior.

## Validation

- Debug build passed with zero warnings and zero errors.
- Release publish succeeded for `win-x64` self-contained output.
- Published `OPALNOVA.exe` launched and closed cleanly.

## Notes

- Material reorder coverage uses existing open purchase-order items linked to materials.
- Slow-moving guidance uses record creation dates, so imported legacy records may need manual interpretation.
- Material adjustment audit rows depend on existing transaction reasons and links.
