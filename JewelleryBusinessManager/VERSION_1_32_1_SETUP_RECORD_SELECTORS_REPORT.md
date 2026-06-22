# Version 1.32.1 — Setup/Input Record Selectors

## Purpose
This patch improves the two-page Tool Workspace introduced in V1.32 by making the Setup / Inputs page capable of selecting the required records directly inside the tool panel.

## Changes
- Bulk Status Update now opens in the Setup / Inputs page with:
  - Record Type dropdown
  - Multi-select record list
  - New Status dropdown
  - Apply Bulk Status Update button
- Bulk Add Selected To Market now opens in the Setup / Inputs page with:
  - Target Market Event dropdown
  - Multi-select Jewellery Stock list
  - Add Selected Stock To Market button
- Existing main-table selections are still used as a convenience preselection where possible.
- Tools no longer require the user to leave the studio, select rows, and return before using these functions.

## Validation
- ZIP/package integrity checked.
- XAML files parsed successfully.
- Project file parsed successfully.
- MainWindow event handlers checked.
- C# brace balance checked.
- No interpolated raw strings introduced.
- Verified the new selectors use existing DbSet names, including BusinessTasks.

## Test Focus
- Data Cleanup Studio → Bulk Status Update.
- Choose record type from dropdown.
- Select records in the Setup page.
- Choose status from dropdown.
- Apply update.
- Data Cleanup Studio → Bulk Add Selected To Market.
- Choose target market from dropdown.
- Select jewellery stock in the Setup page.
- Apply update.
