# OPALNOVA V1.96.0 - Workspace Surface Reduction

V1.96.0 continues the UI/workflow streamlining pass without schema changes.

## Implemented

- Compressed high-use workflow headers so workspace tabs show more working content above the fold.
- Removed redundant subtitle copy from compact workflow headers where the screen title and controls already explain the context.
- Tightened metric summary rows in:
  - Alert Centre,
  - Project Workbench,
  - Proposal Pipeline,
  - Supplier Diamond Holds & Orders.
- Reduced selected-detail panel padding and heading sizes in Alert Centre, Project Workbench, Proposal Pipeline and Payment & Collection.
- Reduced outer margins and filter-panel padding in Production Board, Supplier Diamond Holds & Orders and Stock Movement.
- Shortened the Supplier Diamond workflow status guidance line while preserving the same workflow sequence.
- Preserved all action buttons, selectors, data grids, payment controls, supplier diamond actions and stock movement inputs.
- Preserved database schema and existing workflow behavior.

## Validation

- Debug build succeeded with zero errors.
- Release publish succeeded for `win-x64` self-contained output.
- Published `OPALNOVA.exe` launched and closed cleanly.
- The build and publish surfaced NU1900 warnings because the sandbox could not reach NuGet vulnerability data at `https://api.nuget.org/v3/index.json`.

## Notes

- This pass intentionally reduces visual bulk rather than changing workflow logic.
- No records, stock quantities, payments, supplier diamonds or workflow statuses are changed by this polish pass.
