# Version 1.23 — Detail Pages & Record Preview Panels

## Purpose
This version improves daily usability by adding a polished right-side record preview panel to normal record sections. The main table remains on the left, while the selected record's key information, photo, links and recent activity appear on the right.

## Added
- Right-side record preview panel for record sections.
- Preview header with record type, title and status summary.
- Photo preview using the first attached photo for the selected record.
- Quick action buttons: Edit, Add Photo, Trace and Scan Label.
- Key detail cards generated from important record fields.
- Linked record summary for customers, materials, stones, parcels, jobs, sales, markets, production batches, listings, purchase orders and tasks.
- Recent activity summary for selected records where available.

## Preserved
- Dashboard and clickable tiles.
- Tool workspaces and right-side report preview.
- Barcode labels and scan lookup.
- Backup, restore and export bundle file-lock fixes.
- All existing business workflows and reports.

## Validation
Static validation was run twice:
1. Working-folder validation after the change.
2. Final ZIP validation after packaging.

Checks included:
- ZIP integrity.
- XAML/XML parsing.
- MainWindow event handler matching.
- C# brace balance.
- Detail preview named controls present.
- Record grid and preview panel visibility references.
- Existing report/tool preview references preserved.
