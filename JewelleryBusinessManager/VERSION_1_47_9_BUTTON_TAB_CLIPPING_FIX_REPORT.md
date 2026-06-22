# OPALNOVA V1.47.9 — Button and Tab Clipping Fix

## Purpose
This patch keeps the compact tabbed/editor layout while adding enough vertical breathing room so tab labels, close buttons and normal command buttons are not clipped.

## Changes
- Increased standard button padding slightly from `10,5` to `10,6`.
- Increased standard and toolbar button minimum height from `30` to `32`.
- Increased workspace tab height from `44` to `48`.
- Increased workspace tab padding from `10,5` to `10,7`.
- Removed the negative bottom tab margin that could cause slight clipping against the tab control border.
- Increased workspace tab close button to a safe 26x26 control.
- Updated package/app metadata to 1.47.9.

## Kept Stable
- No database changes.
- No quote calculation changes.
- No Nivoda API changes.
- No production, payment, inventory or report logic changes.
- Theme, selector and dense editor layout changes are preserved.

## Suggested Test
1. Open several workspace tabs, including New Customer, Edit Job, New Quote, Production Board and Diamond Search.
2. Confirm tab text and close buttons are not clipped.
3. Check Save/Cancel and toolbar buttons in multiple pages.
4. Publish and confirm OPALNOVA.exe opens normally.
