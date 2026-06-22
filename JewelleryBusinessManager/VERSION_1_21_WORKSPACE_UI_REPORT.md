# Version 1.21 — Workspace Pages & Modern UI Polish

## Purpose
This version focuses on quality-of-life, UI polish, and navigation structure. It moves specialised tools out of the crowded top menu and into dedicated tool workspace pages.

## Main changes
- Added grouped **Tool Workspaces** in the left navigation.
- Added dedicated pages for:
  - Pricing Studio
  - Inventory Studio
  - Purchasing Studio
  - Production & Opal Studio
  - Market Studio
  - Online Selling Studio
  - Tasks Studio
  - Codes & Labels Studio
  - Documents Studio
  - Reports Studio
  - Safety & Data Studio
- Replaced the crowded action menu with a cleaner page header and basic Add/Edit/Delete actions.
- Each tool workspace now shows:
  - Left side: clearly listed sub-function buttons with descriptions.
  - Right side: report/document preview area.
- Generated reports, labels, checklists and documents preview in the right side of the active tool workspace when generated from a tool page.
- The existing full in-app report viewer is preserved for reports generated outside tool workspaces.

## Preserved features
All confirmed working features from V1.20.1 were preserved:
- Records and data grid sections
- Dashboard tiles
- Backup, export bundle and restore fixes
- Live metal prices and pricing helper
- Inventory tracking
- Purchase orders and receiving
- Opal workflow
- Production batches
- Market Event Pro
- Online listings
- Tasks and work queue
- In-app report viewer
- Barcode labels and scan/lookup prep

## Recommended tests
- Build and run.
- Open each Tool Workspace from the left navigation.
- Click each sub-function button at least once where practical.
- Generate reports from a tool page and confirm they preview on the right.
- Use Open HTML / Print from the preview pane.
- Confirm normal record sections still show the data grid.
- Confirm Add/Edit/Delete still work in record sections.
- Confirm backup, export bundle and restore still work.
