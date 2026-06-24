# OPALNOVA Codex Handoff

## Current Baseline

- Application: OPALNOVA
- Internal project/namespace: `JewelleryBusinessManager`
- Platform: Windows desktop app, WPF / C#
- Database: SQLite under `%LocalAppData%\JewelleryBusinessManager\jewellery_business_manager.db`
- Current workspace version: V1.69.0 Reminder Task Consistency
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

## V1.69.0 State

V1.69.0 continues workflow consistency cleanup after the V1.68 payment reminder pass:

- Bumped visible/project version metadata to 1.69.0.
- Added shared duplicate-safe open-task detection in `TaskWorkflowService`.
- Routed quote, proposal, project workbench, payment, pickup/handover, and supplier diamond reminder creation through the shared duplicate-check pattern.
- Replaced remaining ad-hoc task-code generation in active workflow screens with `TaskWorkflowService.GenerateTaskCode()`.
- Made pickup/handover reminders duplicate-safe for the selected job and visible on the dashboard.
- Preserved database schema and existing task/reminder records.

Validation completed:

- Debug build succeeds with zero warnings and zero errors.
- Release publish succeeds through `win-x64-self-contained`.
- Published `OPALNOVA.exe` launches and closes cleanly.

## V1.68.0 State

V1.68.0 continues payment and handover workflow polish:

- Bumped visible/project version metadata to 1.68.0.
- Added `Copy Balance Reminder` in `PaymentCollectionWindow`.
- Added duplicate-safe `Create Balance Follow-Up` task creation for selected jobs with money still owing.
- Balance reminder messages include job code/title, total, paid amount, remaining balance, and due/handover date where available.
- Preserved payment recording, invoice/receipt generation, sale creation, and job completion logic.
- Preserved database schema.

Validation completed:

- Debug build succeeds with zero warnings and zero errors.
- Release publish succeeds through `win-x64-self-contained`.
- Published `OPALNOVA.exe` launches and closes cleanly.

## V1.67.0 State

V1.67.0 continues quote context workflow polish:

- Bumped visible/project version metadata to 1.67.0.
- Added a `Use Customer Preferences` action in Custom Quote Builder after customer selection.
- The action fills blank quote ring size, preferred metal and preferred stone fields from the selected customer profile.
- Existing quote-specific field values are not overwritten.
- The action marks the quote dirty so V1.66 unsaved-change protection still applies.
- Preserved database schema and proposal generation behavior.

Validation completed:

- Debug build succeeds with zero warnings and zero errors.
- Release publish succeeds through `win-x64-self-contained`.
- Published `OPALNOVA.exe` launches and closes cleanly.

## V1.66.0 State

V1.66.0 starts unsaved-change protection in the highest-risk editor:

- Bumped visible/project version metadata to 1.66.0.
- Added reusable `IWorkspaceCloseGuard` support to hosted workspace tab closing.
- Implemented unsaved-change tracking in `CustomQuoteBuilderWindow`.
- Closing a quote workspace tab with unsaved quote/option/link/image changes now prompts Save, Discard, or Cancel.
- Starting a new quote from an edited quote now uses the same protection.
- Persisted workflow actions such as save, preview, send/record proposal, accept option, release reservations, and create job reset the dirty state after successful save.
- Preserved database schema and quote/proposal workflow behavior.

Validation completed:

- Debug build succeeds with zero warnings and zero errors.
- Release publish succeeds through `win-x64-self-contained`.
- Published `OPALNOVA.exe` launches and closes cleanly.

## V1.65.0 State

V1.65.0 continues quote/proposal workflow polish:

- Bumped visible/project version metadata to 1.65.0.
- Added additive `CustomQuote` context fields for occasion, required-by date, ring size, budget/target, preferred metal and preferred stone.
- Added matching controls in the Custom Quote Builder and restored existing private `InternalNotes` editing in the quote UI.
- Proposal output now includes customer-facing project details when recorded.
- Required-by dates now feed quote next-action guidance and shared Alert Centre/Project Workbench rules.
- Proposal Pipeline search/details now include quote context.
- Preserved proposal send/record behavior and kept internal notes out of customer-facing proposal output.

Validation completed:

- Debug build succeeds with zero warnings and zero errors.
- Release publish succeeds through `win-x64-self-contained`.
- Published `OPALNOVA.exe` launches and closes cleanly.

## V1.64.0 State

V1.64.0 continues the quote/proposal workflow pass:

- Bumped visible/project version metadata to 1.64.0.
- Added a hosted `ProposalPipelineWindow` for prepared, sent, follow-up due, accepted and converted proposals.
- Added pipeline filters, search, status counts, selected-proposal detail, open-value summary and action guidance.
- Pipeline rows can reopen the exact quote in a workspace tab, open the generated proposal file, copy recorded email drafts and create duplicate-safe follow-up tasks.
- Added Proposal Pipeline entry points in Workflow menu, Project Workbench actions, Alert Centre actions, Quotes & Proposals, Reports and Custom Workflow Studio.
- Preserved database schema and kept proposal send/record behavior in the existing quote builder.

Validation completed:

- Debug build succeeds with zero warnings and zero errors.
- Release publish succeeds through `win-x64-self-contained`.
- Published `OPALNOVA.exe` launches and closes cleanly.

## V1.63.0 State

V1.63.0 is a narrow text/copy cleanup checkpoint:

- Bumped visible/project version metadata to 1.63.0.
- Standardized high-visibility generated document headings and support copy to avoid fragile typographic separators in exported text and HTML.
- Replaced the remaining old internal traceability report heading with OPALNOVA branding.
- Updated the built-in guide metadata, release notes, About text, roadmap, forward plan, and V1.63 testing checklist.
- Preserved database schema and business workflow logic.

Validation completed:

- Debug build succeeds with zero warnings and zero errors.
- Release publish succeeds through `win-x64-self-contained`.
- Published `OPALNOVA.exe` launches and closes cleanly.

## V1.62.0 State

V1.62.0 continues the release-readiness and user guidance pass:

- Bumped visible/project version metadata to 1.62.0.
- Expanded `DataSafetyService.CreateUserGuide()` from a short routine page into a practical OPALNOVA manual.
- The guide now covers setup, quotes/proposals, production, payments, inventory, supplier diamonds, reports, backups, restore/import cautions, and release testing.
- The guide remains generated locally as HTML from the existing User Guide action.
- Added release notes and a V1.62 testing checklist.
- Preserved database schema and kept the guide read-only.

Validation completed:

- Debug build succeeds with zero warnings and zero errors.
- Release publish succeeds through `win-x64-self-contained`.
- Published `OPALNOVA.exe` launches and closes cleanly.

## V1.61.0 State

V1.61.0 continues the reports and decision-support pass:

- Bumped visible/project version metadata to 1.61.0.
- Added `DocumentExportService.CreateVisualReportCharts()`.
- Added Visual Charts actions in Reports and Reports Studio.
- The report renders printable HTML/CSS bar charts for sales, profit, quote conversion, inventory value, payments and outstanding balances.
- Charts reuse existing OPALNOVA records and calculations without internet access or external chart libraries.
- Added help text, release notes, and a V1.61 testing checklist.
- Preserved database schema and kept the report read-only.

Validation completed:

- Debug build succeeds with zero warnings and zero errors.
- Release publish succeeds through `win-x64-self-contained`.
- Published `OPALNOVA.exe` launches and closes cleanly.

## V1.60.0 State

V1.60.0 continues the reports and bookkeeping-readiness pass:

- Bumped visible/project version metadata to 1.60.0.
- Added `DocumentExportService.CreateTaxSummaryReport()`.
- Added Tax / GST Summary actions in Reports and Reports Studio.
- The report summarizes current month, financial quarter to date, financial year to date, and last 12 months.
- Tax is estimated from recorded sale totals using the configured business tax label, registration state, and rate.
- Added payment method totals, financial-year sales by location, current job balances, and tax/payment data checks.
- Added help text, release notes, and a V1.60 testing checklist.
- Preserved database schema and kept the report read-only.

Validation completed:

- Debug build succeeds with zero warnings and zero errors.
- Release publish succeeds through `win-x64-self-contained`.
- Published `OPALNOVA.exe` launches and closes cleanly.

## V1.59.0 State

V1.59.0 continues the reports and business intelligence pass:

- Bumped visible/project version metadata to 1.59.0.
- Added `DocumentExportService.CreateProfitabilityReport()`.
- Added Profitability actions in Reports and Reports Studio.
- Profitability reporting groups recorded sales by product/service category, including linked jewellery stock, linked job work, and unlinked sales.
- Added recorded job-sales profit by job type and estimated job profit by job type.
- Added profit-reporting data checks for unlinked sales, missing links, zero-cost sales, jobs without prices, and priced jobs without costs.
- Added help text, release notes, and a V1.59 testing checklist.
- Preserved database schema and kept the report read-only.

Validation completed:

- Debug build succeeds with zero warnings and zero errors.
- Release publish succeeds through `win-x64-self-contained`.
- Published `OPALNOVA.exe` launches and closes cleanly.

## V1.58.0 State

V1.58.0 continues the reports and inventory decision-making pass:

- Bumped visible/project version metadata to 1.58.0.
- Added `DocumentExportService.CreateStockAgeingReport()`.
- Added Stock Ageing actions in Reports and Reports Studio.
- Stock ageing groups unsold jewellery and available loose stones into age bands.
- Slow-moving inventory lists jewellery and stones older than 180 days with status, age, value, created date and updated date.
- Added help text, release notes, and a V1.58 testing checklist.
- Preserved database schema and kept the report read-only.

Validation completed:

- Debug build succeeds with zero warnings and zero errors.
- Release publish succeeds through `win-x64-self-contained`.
- Published `OPALNOVA.exe` launches and closes cleanly.

## V1.57.0 State

V1.57.0 begins the invoice and receipt polish pass:

- Bumped visible/project version metadata to 1.57.0.
- Refreshed job invoice/receipt, sale receipt, deposit receipt, and payment receipt HTML output.
- Added shared customer-facing document sections for document header, status badge, financial summary tiles, notices, and payment ledger styling.
- Added clearer handover notes for pickup/shipping/completed/cancelled job states.
- Added clearer payment checks for outstanding balances, payment methods, and references.
- Updated release notes and About text to V1.57.0.
- Preserved database schema, payment logic, sale creation logic, and existing document entry points.

Validation completed:

- Debug build succeeds with zero warnings and zero errors.
- Release publish succeeds through `win-x64-self-contained`.
- Published `OPALNOVA.exe` launches and closes cleanly.

## V1.56.0 State

V1.56.0 begins the customer relationship polish pass:

- Bumped visible/project version metadata to 1.56.0.
- Added `CustomerRelationshipService.CreateCustomerTimeline()` for a single customer activity timeline.
- Timeline combines existing quotes, proposal-sent events, jobs, sales, payments, and customer tasks.
- Added a Customer Timeline action in Customer Relationship Studio with the same selector workflow as summary/history reports.
- Improved customer summary cards with quote counts, recent quote context, and recent timeline events.
- Updated release notes and About text to V1.56.0.
- Preserved database schema and existing customer workflow behavior.

Validation completed:

- Debug build succeeds with zero warnings and zero errors.
- Release publish succeeds through `win-x64-self-contained`.
- Published `OPALNOVA.exe` launches and closes cleanly.

## V1.55.0 State

V1.55.0 begins the release readiness and data safety pass:

- Bumped visible/project version metadata to 1.55.0.
- Added a dashboard Data Safety card showing backup freshness, backup folder, active database path, database size, and pending restore state.
- Added dashboard actions for Create Backup, Restore Preview, Health Check, and Release Notes.
- Added restore preview before staging a backup restore, including file details, SQLite integrity validation, active/staging paths, and key table counts.
- Added release notes output and menu/workspace actions for viewing current major build notes.
- Updated About text to the current V1.55.0 version and paths.
- Preserved database schema, database location, and existing restore staging behavior.

Validation completed:

- Debug build succeeds with zero warnings and zero errors.
- Release publish succeeds through `win-x64-self-contained`.
- Published `OPALNOVA.exe` launches and closes cleanly.

## V1.54.0 State

V1.54.0 begins the reports and export upgrade:

- Bumped visible/project version metadata to 1.54.0.
- Added an Excel-compatible business intelligence workbook export using SpreadsheetML `.xls`.
- Added the workbook export beside the existing BI CSV export in Reports and Reports Studio.
- Exported workbook sheets cover Summary, Sales, Outstanding Balances, Quotes, Inventory Value, Reserved Inventory, Tasks, and External Diamonds.
- Added an HTML launcher for the workbook so the generated output can be opened from the in-app report preview flow.
- Preserved database schema and existing CSV/HTML report behavior.

Validation completed:

- Debug build succeeds with zero warnings and zero errors.
- Release publish succeeds through `win-x64-self-contained`.
- Published `OPALNOVA.exe` launches and closes cleanly.

## V1.53.0 State

V1.53.0 begins the external diamond production readiness build:

- Bumped visible/project version metadata to 1.53.0.
- Added `ExternalDiamondInventoryService` to convert received supplier diamonds into owned loose-stone inventory.
- Added duplicate-safe conversion using a persistent external-diamond marker in the created stone notes.
- Supplier Diamond Holds & Orders now shows linked owned stone codes where a received supplier diamond has been converted.
- Added a Convert To Owned Stone action in the supplier diamond workflow.
- Conversion creates a normal `Stone` record with diamond details, certificate/source notes, estimated value, and `Loose` status.
- Converted supplier diamonds and linked quote-option diamond links move to `Converted To Owned Inventory`.
- Preserved database schema and kept Nivoda credentials user-entered only.

Validation completed:

- Debug build succeeds with zero warnings and zero errors.
- Release publish succeeds through `win-x64-self-contained`.
- Published `OPALNOVA.exe` launches and closes cleanly.

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
