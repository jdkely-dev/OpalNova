# OPALNOVA V1.87.0 Workflow Search Finder

V1.87.0 improves global findability by extending the existing Search All window instead of introducing a separate command model.

## Implemented

- Bumped visible/project version metadata to `1.87.0`.
- Expanded Advanced Search / Search All to include:
  - Custom Quotes.
  - Quote Options.
  - External Diamonds.
  - Workflow Actions.
- Added workflow-action search results for:
  - Daily priorities / Alert Centre.
  - Project Workbench.
  - Quotes and proposal workflow.
  - Proposal Pipeline.
  - Payments and collection.
  - Production.
  - Inventory.
  - Supplier diamonds.
  - Reports and charts.
  - Backups and restore.
  - Data integrity.
  - Customer relationship.
  - Market operations.
  - Hardware and labels.
  - Data cleanup.
- Added quick filters for proposal follow-ups due and supplier holds expiring.
- Added `MainWindow.OpenWorkflowCommand(...)` so workflow-action search results navigate to the relevant workspace section.
- Updated search window copy so Search All is clearly for records and workflow actions.
- Preserved database schema and existing saved search/view behavior.

## Deferred

- A full command palette with direct command execution remains later work.
- Searchable indexed manual/help pages remain later work after the current user guide stabilizes further.

