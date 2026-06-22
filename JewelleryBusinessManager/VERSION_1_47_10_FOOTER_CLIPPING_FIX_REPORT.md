# OPALNOVA V1.47.10 — Footer Clipping Fix

## Changes

- Increased the shared record-editor footer height so Save/Cancel buttons are no longer clipped at the bottom of tabbed record pages.
- Removed the too-small explicit 30px button height from record-editor Save/Cancel controls.
- Added safer button padding/minimum height while keeping buttons compact.
- Slightly increased workspace tab height and close-button size to prevent label/close-button clipping.
- Reduced record-editor label columns from 95px to 80px so input controls take more of each row.
- Preserved the two-column dense record layout from the previous build.

## Safety

- No database changes.
- No quote calculation changes.
- No Nivoda/API changes.
- No production, payment, inventory, or reporting logic changes.
