# OPALNOVA V1.95.0 - Workflow Control Consolidation

V1.95.0 continues the UI/workflow streamlining pass without schema changes.

## Implemented

- Routed remaining high-priority workflow Button and TextBox styles through the shared OPALNOVA application control templates.
- Preserved local workflow colours, spacing and sizing where those windows use their own brush names.
- Added explicit `Tag` prompt text to every ComboBox declaration in XAML.
- Selector empty-state prompts now show workflow-specific guidance such as `Choose a customer`, `Choose payment method`, `Choose a market` and `Choose a filter`.
- Covered quote, search, inventory, market, production, payment, supplier diamond, pricing, device, label and jeweller tool selectors.
- Preserved database schema and existing selector bindings, selected values, save actions, payment recording, production movement, supplier diamond state and report behavior.

## Validation

- Debug build succeeded with zero errors.
- Release publish succeeded for `win-x64` self-contained output.
- Published `OPALNOVA.exe` launched and closed cleanly.
- The build and publish surfaced NU1900 warnings because the sandbox could not reach NuGet vulnerability data at `https://api.nuget.org/v3/index.json`.

## Notes

- This pass builds directly on V1.94.0's shared ComboBox empty-state prompt path.
- No records, stock quantities, payments, supplier diamonds or workflow statuses are changed by this polish pass.
