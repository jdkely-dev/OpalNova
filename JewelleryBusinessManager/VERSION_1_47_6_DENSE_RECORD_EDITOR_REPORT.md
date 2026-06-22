# OPALNOVA V1.47.6 — Dense Record Editor Layout

## Purpose
Improve usability of record detail tabs by reducing unnecessary scrolling and making better use of the wider tabbed workspace.

## Changes
- Converted the generic record editor from one field per row to a compact two-column layout.
- Kept long fields such as notes, descriptions, addresses, URLs and file paths full width for readability.
- Reduced the Record Details header height and bottom button bar padding.
- Reduced Save/Cancel button size slightly.
- Reduced record-editor card margin/padding so the scrollable details area is larger.
- Reduced group-header font size and spacing.
- Reduced multi-line notes/address box height slightly.

## Kept Stable
- No database changes.
- No quote calculation changes.
- No Nivoda API changes.
- No production, payment, inventory, or report logic changes.
- Selector display fix and theme polish are preserved.

## Test Focus
- New Customer tab.
- Edit Job tab.
- New Quote related selectors.
- Save and cancel from hosted tabs.
- Detached edit window still usable.
