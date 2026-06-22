# Version 1.27 — Advanced Search, Filters & Saved Views

## Added
- New Search & Views Studio workspace.
- Advanced global search window across all major record sections.
- Quick filters for low stock, jobs due, overdue jobs, needs photos, at market, reserved stock, ready to list, listing work, overdue tasks, due today, high priority, open purchase orders and open jobs.
- Search results can be opened directly in the main app; the related section opens and the matching row is selected.
- Saved views stored locally as JSON in the app data folder.
- Saved views can be applied later to reopen the matching section/search/filter.
- Saved views are included in full export bundles.

## Notes
- No database schema changes were added.
- Saved search/filter views are stored outside the SQLite database so they are low-risk and easy to back up.
- Existing record sections, tool workspaces, backup/restore/export bundle, hardware/POS and report-preview features are preserved.
