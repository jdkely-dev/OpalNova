# OPALNOVA V1.48.1 — Global Workspace Usability Polish

## Changes

- Applied the UI changes globally across shared workspace and control styles where relevant.
- Removed the redundant Main Work intro panel from the left navigation.
- Removed the redundant Tabbed Workspace page header so active workspace tabs use more of the main area.
- Moved Add, Edit and Delete actions into the quick toolbar.
- Reduced outer workspace margins/padding so hosted tools fill the available workspace more fully.
- Made hosted tab content stretch to the full tab area and stripped small outer hosted-window margins.
- Strengthened workspace tab close handling using preview mouse and routed button events.
- Increased tab close button safe size to prevent clipping.
- Fixed Project Workbench filter display by inheriting the global dark ComboBox template.
- Made Project Workbench filter logic more robust for ComboBoxItem/string selection.
- Fixed Project Workbench status text so it reports filtered rows instead of always showing the unfiltered total.
- Compact Project Workbench header into a slim toolbar row.
- Preserved database, quote, Nivoda, production, payment, sales, and reporting logic.

## Test focus

1. Open Project Hub and confirm no white selector background appears.
2. Change filters and confirm the grid row count/status changes.
3. Close the Project Workbench tab using the x in the tab.
4. Open the Project Hub again and close with the bottom Close button.
5. Confirm Add/Edit/Delete work from the quick toolbar for record sections.
6. Confirm Custom Quotes, Production Board, Payments, Diamond Search and Diamond Holds still open in tabs and fill the workspace cleanly.
