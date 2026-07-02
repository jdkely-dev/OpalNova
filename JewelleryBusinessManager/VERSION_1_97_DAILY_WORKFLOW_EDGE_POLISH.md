# OPALNOVA V1.97.0 - Daily Workflow Edge Polish

V1.97.0 continues the UI/workflow streamlining pass without schema changes.

## Implemented

- Added an `Open Payments` action to the Production Board toolbar.
- The Production Board action requires a selected job card and then opens Payment & Collection focused on that job.
- Added a focused Payment & Collection constructor path for opening the workflow against a specific job id.
- Payment & Collection now switches to the `All jobs` filter when a selected production job would otherwise be hidden by the default handover filter.
- Job-specific payment handoffs open in workspace tabs with contextual titles such as `Payment JOB001`.
- Preserved the standard Payment & Collection entry point for normal all-workflow use.
- Cleaned visible Payment & Collection job-list separators to plain ASCII text.
- Preserved database schema and existing production movement, payment recording, invoice/receipt, sale creation and handover behavior.

## Validation

- Debug build succeeded with zero errors.
- Release publish succeeded for `win-x64` self-contained output.
- Published `OPALNOVA.exe` launched and closed cleanly.
- The build and publish surfaced NU1900 warnings because the sandbox could not reach NuGet vulnerability data at `https://api.nuget.org/v3/index.json`.

## Notes

- This pass reduces a repeated daily handoff between production and payment workflows.
- No payments, sales, job statuses or task records are created just by opening the focused payment workflow.
