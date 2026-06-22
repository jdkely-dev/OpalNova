# OPALNOVA V1.51.0 - Universal Next Action And Alert Centre

## Summary

V1.51.0 starts the universal next-action build. It adds a shared runtime alert engine, a workspace-hosted Alert Centre, and dashboard setup-readiness guidance without changing database schema or existing workflow state transitions.

## Changes

- Added `NextActionItem` as a shared runtime model for alert and next-action rows.
- Added `NextActionService` to calculate current quote, production, payment, supplier diamond, inventory, and follow-up actions from existing data.
- Added `AlertCentreWindow` with dark OPALNOVA styling, search, filters, visible counts, selected-alert detail, and open-next-step actions.
- Added Alert Centre access from the top workflow menu, quick toolbar, Project Workbench tool section, dashboard one-click actions, and dashboard tiles.
- Added a dashboard `Next Actions` tile driven by `NextActionService`.
- Added dashboard setup-readiness guidance for profile, pricing defaults, metal prices, proposal templates, customers, first quote, sent proposal, production workflow, backup health, and supplier readiness.
- Added `OPALNOVA_V1_50_PROPOSAL_SEND_TESTING_CHECKLIST.txt` for manual V1.50 proposal workflow testing.

## Data Safety

- No database tables were dropped, recreated, or changed.
- No credential defaults were added.
- Alert Centre data is calculated at runtime from existing records.

## Validation

- Debug build: passed with zero warnings and zero errors.
- Release publish: passed through `win-x64-self-contained`.
- Published app launch smoke: passed; `OPALNOVA.exe` launched and closed cleanly.
