# OPALNOVA V1.22.1 — Dashboard Tile Sharpness & Click Navigation

## Purpose
This maintenance/UI version fixes two dashboard issues reported during testing:

1. Dashboard tile text could look blurry.
2. Dashboard tiles were visual-only and did not navigate to the relevant section.

## Changes Made

### Sharper dashboard tile text
- Removed the drop shadow effect from dashboard tiles because WPF effects can render the entire visual subtree, including text, through a bitmap layer and make text look soft or blurry.
- Added ClearType/fixed text rendering hints to text styles.
- Enabled layout rounding and pixel snapping on the main window and tile styles.

### Clickable dashboard tiles
- Every dashboard metric tile now has a target section or workspace.
- Clicking a tile opens the related record section or tool workspace.

Examples:
- Active Jobs → Jobs
- Low Materials → Materials
- Loose Stones → Stones
- Open Purchase Orders → Purchase Orders
- Reorder Suggestions → Purchasing Studio
- Market Net Est. → Market Studio
- Needs Listing → Online Listings
- Open Tasks → Tasks

### UX polish
- Dashboard helper text now explains that tiles are clickable.
- Tile hover state now highlights the border and card background.
- Status bar confirms which section was opened from a tile.

## Validation
- XAML/XML parsing completed.
- MainWindow event handlers verified.
- Dashboard tile tags verified.
- Dashboard click handlers verified.
- Confirmed no dashboard tile shadow effect remains.
- C# brace balance scan completed.

## Build Note
This package has been statically validated. Final compiler/runtime confirmation should still be done in Visual Studio.
