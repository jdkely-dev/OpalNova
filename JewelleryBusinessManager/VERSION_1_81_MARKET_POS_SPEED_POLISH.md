# OPALNOVA V1.81.0 - Market POS Speed Polish

V1.81.0 is a focused market/POS workflow pass after the V1.80 stability milestone.

## Changes

- Bumped visible/project version metadata to 1.81.0.
- Routed Market Operations sale recording through `MarketProService.CreateMarketSale(...)`.
- Kept sale records, jewellery stock sold state, market stock sold state, and market reconciliation totals aligned from both market sale entry points.
- Added guards against selling already-sold market stock or stock already returned to inventory.
- Excluded returned market stock from active Record Market Sale selectors.
- Added `MarketProService.ReturnMarketStockToInventory(...)` and routed Market Operations returns through it.
- Added packed/sold/returned state display in Market Operations.
- Added live reconciliation guidance comparing recorded stock sales against entered cash/card/other totals.
- Replaced fragile market checklist/report symbols and encoded separators with plain ASCII text.
- Updated release notes, user guide, About text, roadmap, forward plan and handoff to the V1.81 baseline.

## Data Safety

- No database schema changes were introduced.
- Existing market events, market stock, jewellery stock and sale records are preserved.
- Market sale and return actions now use shared service paths to reduce inconsistent state.

## Validation

- Debug build succeeds with zero warnings and zero errors.
- Release publish succeeds through `win-x64-self-contained`.
- Published `OPALNOVA.exe` launches and closes cleanly.

Per the milestone-only git rule, this build is not committed or pushed until the next whole-number milestone unless explicitly requested.
