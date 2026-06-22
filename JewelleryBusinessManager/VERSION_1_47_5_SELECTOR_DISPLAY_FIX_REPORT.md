# OPALNOVA V1.47.5 — Selector Display Fix

## Purpose
Fix lookup/input dropdowns that displayed raw object text such as `LookupOption { Id = , Label = (none) }` before a selection was made.

## Changes
- Added safe `ToString()` display overrides for internal selector option records.
- Lookup dropdowns now display friendly labels such as `Select customer`, `Select job`, `Select stone`, `Select material`, and `Select production batch`.
- Record editor labels now show friendly names for lookup fields, such as `Customer` instead of `Customer Id`.
- Preserved the V1.47.3/V1.47.4 theme and tabbed workspace changes.

## Stability
- No database changes.
- No quote calculation changes.
- No Nivoda API changes.
- No production, payment, inventory, or report logic changes.
