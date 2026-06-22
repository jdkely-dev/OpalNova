# OPALNOVA V1.36.1 — Hover Help Badges

## Purpose
Refined the V1.36 guided help system so help is attached directly to the original tool action buttons instead of using separate standalone question mark buttons.

## Changes
- Removed standalone ? help buttons from the top toolbar, record header and tool preview header.
- Added a small faded circular ? badge inside each Tool Studio action button.
- Hovering over a badge opens a compact floating mini-guide near the badge.
- The original action button still runs the same function when clicked.
- No database schema changes.
- No save/load logic changes.

## Validation
- XAML XML parse check passed.
- Project XML parse check passed.
- C# brace balance check passed.
- ZIP integrity should be tested after packaging.
