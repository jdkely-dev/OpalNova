# OPALNOVA V1.47.7 — Compact Tabbed Editor Layout

## Purpose
This is a usability/layout polish build based on V1.47.6. It focuses on making tabbed record editing feel less crowded and reducing unnecessary scrolling.

## Changes
- Made workspace tabs shorter and closer to the height of the quick toolbar.
- Kept the workspace tab 3px black border and dark OPALNOVA colour scheme.
- Reduced record editor header height.
- Reduced record editor save/cancel footer height.
- Increased the usable scrollable input area in record detail tabs.
- Reduced form padding, card margin and section spacing.
- Adjusted record editor columns so labels take less horizontal space and inputs take more of the row.
- Preserved the two-column field layout from V1.47.6.
- Condensed button sizing so most buttons size closer to their text instead of using wide fixed widths.

## Kept stable
- No database schema changes.
- No quote calculation changes.
- No Nivoda API changes.
- No production board logic changes.
- No payment/collection logic changes.
- No reporting logic changes.

## Test first
1. Open New Customer in a tab.
2. Open Edit Job in a tab.
3. Check the tab header height and record editor header/footer height.
4. Confirm the record editor fields use two columns and require less scrolling.
5. Confirm Save/Cancel still work.
6. Publish and open OPALNOVA.exe.
