# OPALNOVA V1.47.11 — Record Header Clipping Fix

## Focus
Fix remaining clipping in tabbed record editor pages where the Record Details header/subtitle could be cut off.

## Changes
- Converted the Record Details header to a compact single-row layout.
- Kept the header low-height while allowing the title and helper text to render cleanly.
- Slightly increased workspace tab height/padding to avoid tab header clipping.
- Preserved the larger scroll/input area, two-column record editor, compact footer, and theme styling from prior V1.47 builds.

## Not changed
- No database schema changes.
- No quote calculation changes.
- No Nivoda API changes.
- No production/payment/report logic changes.
