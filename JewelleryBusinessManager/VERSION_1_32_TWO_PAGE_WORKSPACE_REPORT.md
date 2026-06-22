# Version 1.32 — Two-Page Tool Workspace Panel

## Purpose
This release refines the right-hand tool workspace so tool actions have a clearer two-page flow:

1. **Setup / Inputs** — choose required records, dropdown options, selected data and action settings.
2. **Preview / Result** — view the generated report, document, checklist, label or result preview.

## Changes
- Added **Setup / Inputs** and **Preview / Result** tabs to the right side of tool workspaces.
- Interactive tools now open on the Setup / Inputs page.
- Generated HTML reports/documents automatically switch to Preview / Result.
- Open HTML / Print and Open Folder remain available from the preview header.
- Initial tool workspace state now explains the setup workflow more clearly.
- Preserved existing in-app report viewing and all current tool functionality.

## Validation
- ZIP integrity checked.
- XAML/XML parsing checked.
- Project file parsing checked.
- MainWindow event-handler matching checked.
- C# brace balance checked.
- Confirmed new tab controls and handlers exist.
- Confirmed no interpolated raw strings were introduced.
