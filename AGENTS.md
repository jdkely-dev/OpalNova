# OPALNOVA Codex Handoff

## Current Baseline

- Application: OPALNOVA
- Internal project/namespace: `JewelleryBusinessManager`
- Platform: Windows desktop app, WPF / C#
- Database: SQLite under `%LocalAppData%\JewelleryBusinessManager\jewellery_business_manager.db`
- Current workspace version: V1.52.0 Job Completion And Inventory Consumption
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

## V1.52.0 State

V1.52.0 begins the inventory consumption and job completion build:

- Bumped visible/project version metadata to 1.52.0.
- Added a transactional `JobCompletionService` for explicit job completion, material consumption, stone status updates, reservation release, and completion notes.
- Added `JobCompletionWindow`, a dark themed completion checklist showing linked accepted quote reservations before completion.
- Routed Production Board completion and move-to-completed actions through the new completion checklist.
- Routed Payment & Collection collected/shipped completion actions through the same checklist.
- Consumed material quantities create `MaterialTransaction` audit entries linked to the job.
- Reserved stone links can be marked consumed while the source stone status moves to `SetInJewellery`.
- Preserved database schema; reservation state changes use existing reservation-status fields.

Validation completed:

- Debug build succeeds with zero warnings and zero errors.
- Release publish succeeds through `win-x64-self-contained`.
- Published `OPALNOVA.exe` launches and closes cleanly.

## V1.51.0 State

V1.51.0 begins the universal next-action and alert-centre build:

- Bumped visible/project version metadata to 1.51.0.
- Added a shared `NextActionService` and `NextActionItem` model for runtime-calculated quote, production, payment, supplier diamond, inventory, and follow-up alerts.
- Added a dark themed `AlertCentreWindow` hosted in workspace tabs with search, filters, summary counts, selected-alert detail, and workflow-opening actions.
- Added dashboard and toolbar entry points for Alert Centre, plus a dashboard next-action count tile driven by the shared service.
- Added a dashboard setup-readiness card using existing settings, quote, job, proposal, supplier, and backup data without schema changes.
- Added a plain-text V1.50 proposal-send testing checklist for manual validation.
- Preserved database schema and existing workflow state transitions.

Validation completed:

- Debug build succeeds with zero warnings and zero errors.
- Release publish succeeds through `win-x64-self-contained`.
- Published `OPALNOVA.exe` launches and closes cleanly.

## V1.50.0 State

V1.50.0 begins the premium proposal output/send workflow build:

- Bumped visible/project version metadata to 1.50.0.
- Added additive quote proposal tracking fields for prepared/sent/accepted state, generated proposal path, email draft details, sent time, and follow-up due date.
- Added a dark themed `SendProposalWindow` for recipient, subject, message, open proposal, copy email draft, open mail draft, record sent, and create follow-up.
- Wired quote builder actions so proposal preview records a prepared proposal and send/record marks the proposal as sent without direct SMTP.
- Added proposal email subject/message template defaults to business settings.
- Created duplicate-safe sent-proposal follow-up tasks using the existing `BusinessTask` workflow.
- Improved generated proposal HTML with cleaner customer-facing layout, recommendation badge, payment schedule, next steps, and corrected separator encoding.

Validation completed:

- Debug build succeeds with zero warnings and zero errors.
- Release publish succeeds through `win-x64-self-contained`.
- Published `OPALNOVA.exe` launches and closes cleanly.

## V1.49.1 State

V1.49.1 continues the quote workspace polish build:

- Added attach/open/remove design image controls for selected quote options.
- Uses existing `QuoteOption.ImagePath`; no schema changes were introduced.
- Copies selected design images into OPALNOVA's app photo folder through `PhotoStorageService`.
- Shows design-image status in the option comparison grid and selected-option summary.
- Embeds attached option images into generated proposal HTML as data URIs.

## V1.49.0 State

V1.49.0 begins the quote workspace polish build:

- Added quote status and expiry guidance inside the quote workspace.
- Added a right-side next-action rail for save, preview, recommend, follow-up, accept, and job creation.
- Added an option comparison grid driven by existing `QuoteOption` data.
- Added quote follow-up task creation using the existing `BusinessTask` workflow.
- Kept quote persistence additive-free; no schema changes were introduced.

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
