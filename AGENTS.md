# OPALNOVA Codex Handoff

## Current Baseline

- Application: OPALNOVA
- Internal project/namespace: `JewelleryBusinessManager`
- Platform: Windows desktop app, WPF / C#
- Database: SQLite under `%LocalAppData%\JewelleryBusinessManager\jewellery_business_manager.db`
- Current workspace version: V1.48.3 Roadmap and Credential Safety Polish
- Source root: `JewelleryBusinessManager`
- Published output: `JewelleryBusinessManager\bin\Release\net10.0-windows\win-x64\publish\OPALNOVA.exe`

Keep the internal project and namespace as `JewelleryBusinessManager`. Visible branding should remain OPALNOVA.

## Current Direction

The immediate focus is UI/workflow streamlining:

- Keep the dark navy OPALNOVA theme and antique-white text.
- Avoid white input/dropdown backgrounds and bright yellow highlights.
- Prefer global/shared style fixes over one-off page patches.
- Editors and workflows should open in workspace tabs where practical.
- Reduce redundant explanatory panels and let workspace content fill the tab area.
- Selector fields should show friendly prompts, not raw object strings.

## V1.48.3 State

V1.48.3 reviewed the V1.48.1/V1.48.2 planned implementation list and adjusted the roadmap toward the highest-value workflow order:

- Keep V1.49 focused on Quote Workspace Polish.
- Keep V1.50 focused on Premium Proposal Output.
- Move Universal Next Action and Alert Centre together into V1.51.
- Keep Inventory Consumption and Job Completion as V1.52 because it needs explicit, reviewable stock movement.
- Put External Diamond Production Readiness after the quote/proposal foundations.

V1.48.3 also removed the unsafe Nivoda development credential helper from active UI behavior. The endpoint helper now resets URLs only; users must enter their own Nivoda credentials.

The eight pre-existing build warnings from V1.48.2 have been cleaned up. Current debug builds should report zero warnings and zero errors.

## V1.48.2 State

V1.48.2 imported the uploaded V1.48.1 baseline, then:

- Bumped visible/project version metadata to 1.48.2.
- Removed leftover hidden "Main Work" sidebar scaffolding.
- Fixed hosted workspace tab close sequencing so Project Workbench does not reopen after being closed.
- Routed hosted workflow tabs through the safer close refresh path.
- Fixed Project Workbench hosted-tab initialization so rows/counts/status load immediately when opened in the workspace.
- Made Project Workbench summary counters reflect the visible filtered/search rows.
- Preserved database schema and business workflow logic.

Validation completed:

- Debug build succeeds.
- Release publish succeeds through `win-x64-self-contained`.
- Published `OPALNOVA.exe` launches and closes cleanly.

V1.48.3 later cleaned up the pre-existing nullability/EF warnings in `DataCleanupService.cs`, `EditEntityWindow.xaml.cs`, and `DatabaseBootstrapper.cs`.

## Safety Rules

- Do not drop/recreate user tables. Use additive schema changes only.
- Use `CREATE TABLE IF NOT EXISTS` and `EnsureColumn(...)` before indexes.
- Preserve the LocalAppData database path unless doing a planned migration.
- Do not hardcode Nivoda or other real credentials.
- Before changing workflow logic, check whether layout, binding, style, or helper code is enough.
