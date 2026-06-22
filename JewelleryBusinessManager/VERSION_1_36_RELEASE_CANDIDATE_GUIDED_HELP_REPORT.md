# OPALNOVA V1.36 — Release Candidate + Guided Help

## Purpose
This build keeps the working V1.35.1 standalone release base and adds a lightweight in-app help layer before wider use.

## Added
- Floating help windows opened from `?` buttons.
- Top toolbar `?` help button for the current page.
- Record workspace `?` help button near Add/Edit/Delete.
- Tool workspace `?` help button near Setup / Preview controls.
- Per-function `?` buttons beside every Tool Studio action card.
- Section mini-guides for Dashboard, Pricing, Inventory, Purchasing, Production & Opal, Market, Online Selling, Tasks, Codes & Labels, Documents, Reports, Safety & Data, Hardware & POS, Customer Relationship and Data Cleanup.
- More detailed guides for key tools including Metal Prices, Pricing Helper, Stock Movement, Change Status, Customer Follow-Up, Quote, Invoice / Receipt, Backup and Restore.

## Release candidate polish
- Updated visible version text to Version 1.36 — Release Candidate + guided help.
- Updated assembly/file/product version to 1.36.0.
- Kept the app output name as OPALNOVA.exe.
- Kept the internal namespace and existing data locations unchanged to avoid data loss or last-minute code breakage.

## Safety notes
- No database schema changes.
- No changes to save/load logic.
- No changes to publishing settings except version metadata.
- Help windows are standalone WPF windows owned by the main app window.

## Validation performed in this environment
- XAML XML parse check.
- Project file XML parse check.
- C# brace balance check.
- ZIP integrity check.

## Final checks required in Visual Studio
- Build the solution.
- Open OPALNOVA.
- Click the top `?` button.
- Open several Tool Studios and click the `?` beside individual functions.
- Run the publish script and confirm OPALNOVA.exe opens.
