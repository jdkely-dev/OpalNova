# OPALNOVA Codex Handoff

## Current Baseline

- Application: OPALNOVA
- Internal project/namespace: `JewelleryBusinessManager`
- Platform: Windows desktop app, WPF / C#
- Database: SQLite under `%LocalAppData%\JewelleryBusinessManager\jewellery_business_manager.db`
- Current workspace version: V1.90.0 Stability Milestone
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

## V1.90.0 State

V1.90.0 is the whole-number stability checkpoint across the V1.81-V1.89 work:

- Bumped visible/project version metadata to 1.90.0.
- Completed a redundancy review across market/POS, media import, supplier diamond replacement, production capacity, proposal revision/PDF-ready workflow, data integrity, workflow search, jeweller tools and release-readiness additions.
- Confirmed no duplicate tool action labels within the same tool section.
- Confirmed cross-studio repeated actions remain intentional shortcuts, not duplicate controls in one workspace.
- Confirmed the V1.81-V1.89 work preserves database schema and does not add destructive migrations.
- Confirmed Nivoda credentials remain user-entered and endpoint reset does not fill credentials.
- Added V1.90 audit/checklist documentation.

Validation completed:

- Debug build succeeds with zero warnings and zero errors.
- Release publish succeeds through `win-x64-self-contained`.
- Published `OPALNOVA.exe` launches and closes cleanly.
- V1.90 is the next whole-number milestone and should be committed/pushed after validation.

## V1.89.0 State

V1.89.0 prepares the release-readiness surface before the V1.90 milestone checkpoint:

- Bumped visible/project version metadata to 1.89.0.
- Added `DataSafetyService.CreateReleaseReadinessReport()`.
- Added Release Readiness entry points in Settings & Backup and Safety & Data Studio.
- Added Release Readiness to Search All workflow actions.
- The report summarizes runtime executable/app folder, database/photo/settings paths, backup/printout paths, validation gates, packaging notes, staging cautions, installer decision notes and generated document review checks.
- Installer creation, desktop shortcut creation, update/version channel, OS backup scheduling and production/staging config separation remain explicit release decisions rather than hidden app-startup behavior.
- Preserved database schema and existing backup, restore, health check, data integrity, release notes and user guide behavior.

Validation completed:

- Debug build succeeds with zero warnings and zero errors.
- Release publish succeeds through `win-x64-self-contained`.
- Published launch smoke is pending if Codex process-launch escalation remains blocked by the app usage-limit approval gate.
- Per the milestone-only git rule, V1.89 remains uncommitted until the V1.90 whole-number milestone unless explicitly requested.

## V1.88.0 State

V1.88.0 starts the practical jeweller tools pass without adding hardware dependencies:

- Bumped visible/project version metadata to 1.88.0.
- Added `JewellerToolsWindow`.
- Added ring-size reference rows for common AU/UK, US, EU, inside-diameter and inside-circumference values.
- Added a metal-weight estimator using simple dimension and density inputs.
- Added a faceted-stone carat estimator using shape-specific dimension factors.
- Added copyable results for calculator output.
- Added Jeweller Tools entry points in Pricing Studio and Hardware & POS Studio.
- Added Jeweller Tools to Search All workflow actions.
- Preserved database schema and existing device capture, pricing helper and label behavior.
- True hardware-specific improvements remain dependent on confirmed printer/camera/scale setup.

Validation completed:

- Debug build succeeds with zero warnings and zero errors.
- Release publish succeeds through `win-x64-self-contained`.
- Published launch smoke is pending because Codex process-launch escalation was blocked by the app usage-limit approval gate.
- Per the milestone-only git rule, V1.88 remains uncommitted until the next whole-number milestone unless explicitly requested.

## V1.87.0 State

V1.87.0 improves global findability without adding a new command data model:

- Bumped visible/project version metadata to 1.87.0.
- Expanded Advanced Search / Search All to include `CustomQuote`, `QuoteOption` and `ExternalDiamond` records.
- Added quick filters for proposal follow-up due and supplier holds expiring.
- Added searchable workflow action results for daily priorities, Project Workbench, quotes, proposal pipeline, payments, production, inventory, supplier diamonds, reports, backups, data integrity, customer relationship, market, hardware/labels and cleanup workflows.
- Workflow action results navigate to the relevant workspace section through `MainWindow.OpenWorkflowCommand(...)`.
- Updated search window copy so Search All is clearly for records and workflow actions.
- Preserved database schema and existing saved search/view behavior.

Validation completed:

- Debug build succeeds with zero warnings and zero errors.
- Release publish succeeds through `win-x64-self-contained`.
- Published `OPALNOVA.exe` launches and closes cleanly.
- Per the milestone-only git rule, V1.87 remains uncommitted until the next whole-number milestone unless explicitly requested.

## V1.86.0 State

V1.86.0 starts the deeper data-safety inspection pass without adding automatic OS scheduling:

- Bumped visible/project version metadata to 1.86.0.
- Added `DataSafetyService.CreateDataIntegrityReport()`.
- Added Data Integrity entry points in the dashboard Data Safety card, Reports / Data menu, Settings & Backup and Safety & Data Studio.
- The report checks orphaned links across quotes, jobs, payments, sales, inventory, market stock, production batches, purchase orders, tasks and photos.
- The report flags missing proposal/design/photo files, negative material quantities, incomplete payment links, conflicting market stock states and other review items.
- Preserved database schema, backup/restore behavior and existing health check behavior.
- True automatic backup scheduling remains deferred until app lifecycle or installer support is explicitly chosen.

Validation completed:

- Debug build succeeds with zero warnings and zero errors.
- Release publish succeeds through `win-x64-self-contained`.
- Published `OPALNOVA.exe` launches and closes cleanly.
- Per the milestone-only git rule, V1.86 remains uncommitted until the next whole-number milestone unless explicitly requested.

## V1.85.0 State

V1.85.0 continues proposal polish without adding a PDF renderer dependency:

- Bumped visible/project version metadata to 1.85.0.
- Proposal HTML output now uses revisioned filenames like `QuoteCode_Proposal_v001_yyyyMMdd_HHmmss.html`.
- Proposal documents show generated timestamp and revision label in the customer-facing header.
- Proposal HTML includes print CSS and a `Print / Save as PDF` button for browser-based PDF output.
- Send / Record Proposal now includes `Copy PDF Steps` for copyable browser print-to-PDF instructions.
- Preserved database schema and existing proposal prepared/sent/follow-up tracking fields.
- True native PDF generation remains deferred until a PDF rendering dependency is explicitly chosen.

Validation completed:

- Debug build succeeds with zero warnings and zero errors.
- Release publish succeeds through `win-x64-self-contained`.
- Published `OPALNOVA.exe` launches and closes cleanly.
- Per the milestone-only git rule, V1.85 remains uncommitted until the next whole-number milestone unless explicitly requested.

## V1.84.0 State

V1.84.0 starts production capacity and scheduling support without a new time-entry schema:

- Bumped visible/project version metadata to 1.84.0.
- Added `DocumentExportService.CreateProductionCapacityReport()`.
- Capacity snapshot uses existing active jobs, due dates, recorded job labour hours, balances and active production batches.
- Added due-date buckets for overdue, due within 7 days, due 8-14 days and no due date.
- Added capacity guidance using a conservative 32-hour weekly planning benchmark and flags for missing labour-hour estimates.
- Added Capacity Snapshot actions in Production Board, Production workflow, Production & Opal Studio and Reports Studio.
- Preserved database schema and existing production board movement, stage checklist and job completion behavior.

Validation completed:

- Debug build succeeds with zero warnings and zero errors.
- Release publish succeeds through `win-x64-self-contained`.
- Published `OPALNOVA.exe` launches and closes cleanly.
- Per the milestone-only git rule, V1.84 remains uncommitted until the next whole-number milestone unless explicitly requested.

## V1.83.0 State

V1.83.0 continues supplier diamond workflow readiness without relying on live supplier API mutations:

- Bumped visible/project version metadata to 1.83.0.
- Added `Copy Replacement Search` to Supplier Diamond Holds & Orders.
- Replacement search copy includes selected diamond type, shape, carat range, colour/clarity, lab, original certificate and quote/customer context.
- Replacement search copy lists up to five close saved alternatives already in OPALNOVA using same type, shape and nearby carat range.
- Kept live supplier availability/price refresh deferred until real credentials and accessible schema behaviour are confirmed.
- Preserved database schema and existing supplier diamond save, quote-link, hold, order, receipt and owned-inventory conversion behavior.

Validation completed:

- Debug build succeeds with zero warnings and zero errors.
- Release publish succeeds through `win-x64-self-contained`.
- Published `OPALNOVA.exe` launches and closes cleanly.
- Per the milestone-only git rule, V1.83 remains uncommitted until the next whole-number milestone unless explicitly requested.

## V1.82.0 State

V1.82.0 starts the inventory media and batch workflow pass:

- Bumped visible/project version metadata to 1.82.0.
- Updated the main record detail `+ Photos` action to support selecting multiple image files at once.
- Batch imports reuse existing `PhotoStorageService` storage and existing `PhotoRecord` links.
- Added batch photo captions that identify batch order and linked record type/id.
- Updated the user guide, release notes, roadmap, forward plan and one-time future plan to the V1.82 baseline.
- Preserved database schema and existing single-photo, preview and photo record behavior.

Validation completed:

- Debug build succeeds with zero warnings and zero errors.
- Release publish succeeds through `win-x64-self-contained`.
- Published `OPALNOVA.exe` launches and closes cleanly.
- Per the milestone-only git rule, V1.82 remains uncommitted until the next whole-number milestone unless explicitly requested.

## V1.81.0 State

V1.81.0 continues the market/POS workflow polish pass:

- Bumped visible/project version metadata to 1.81.0.
- Routed Market Operations sale recording through `MarketProService.CreateMarketSale(...)` so sale records, jewellery stock status, market stock rows and reconciliation totals stay aligned.
- Excluded returned market stock from active market sale selection and guarded against selling already-sold or returned stock.
- Added shared `MarketProService.ReturnMarketStockToInventory(...)` for safer market return-to-stock handling.
- Added market stock state display for packed, sold and returned rows in Market Operations.
- Added live reconciliation guidance comparing recorded stock sales with cash/card/other totals.
- Replaced fragile market checklist/report symbols and encoded separators with plain ASCII text.
- Preserved database schema and existing market event, stock, sale and reconciliation records.

Validation completed:

- Debug build succeeds with zero warnings and zero errors.
- Release publish succeeds through `win-x64-self-contained`.
- Published `OPALNOVA.exe` launches and closes cleanly.
- Per the milestone-only git rule, V1.81 remains uncommitted until the next whole-number milestone unless explicitly requested.

## V1.80.0 State

V1.80.0 is a milestone stability checkpoint:

- Bumped visible/project version metadata to 1.80.0.
- Completed a redundancy review across V1.76, V1.77, V1.78 and V1.79.
- Confirmed repeated workflow labels such as Payment & Collection, Supplier Holds & Orders, Stage Checklist and Inventory Value are intentional cross-studio entry points rather than duplicate controls in the same surface.
- Confirmed V1.78 payment schedule guidance is advisory and uses existing quote, job and job-linked payment records without changing payment totals.
- Confirmed V1.79 stock lifecycle guidance is advisory/read-only in reports and does not change stock quantities, reservation states, supplier diamond states or sales.
- Confirmed current metadata, release notes, roadmap and handoff align to the V1.80 baseline.
- Created the V1.80 stability milestone note, redundancy audit and testing checklist.
- Preserved database schema and existing payment, inventory, supplier diamond, job completion, report preview and workspace behavior.

Validation completed:

- Debug build succeeds with zero warnings and zero errors.
- Release publish succeeds through `win-x64-self-contained`.
- Published `OPALNOVA.exe` launches and closes cleanly.
- V1.80 milestone commit and push to `origin/main` completed.

## V1.79.0 State

V1.79.0 continues inventory workflow clarity:

- Bumped visible/project version metadata to 1.79.0.
- Added shared `StockLifecycleService` guidance for jewellery stock, stones, quote reservation links and external supplier diamonds.
- Change Inventory Status now explains the current lifecycle meaning and the selected new status before saving.
- Inventory Value, Stock Ageing, Reserved Inventory and Opal / Stone Stock reports now include lifecycle guidance/columns.
- Supplier Diamond Holds & Orders now shows lifecycle context in the grid, selected-diamond detail, search text and reminder task descriptions.
- Cleaned the supplier diamond workflow status path text to plain ASCII.
- Preserved existing stock quantities, reservation state changes, material transactions, supplier diamond conversion and sale/job completion behavior.

Validation completed:

- Debug build succeeds with zero warnings and zero errors.
- Release publish succeeds through `win-x64-self-contained`.
- Published `OPALNOVA.exe` launches and closes cleanly.

## V1.78.0 State

V1.78.0 continues payment workflow polish:

- Bumped visible/project version metadata to 1.78.0.
- Added shared `PaymentScheduleService` and payment schedule summary models.
- Payment schedules use existing quote, quote option, job and payment records without schema changes.
- Quote totals now show deposit, final balance and remaining payment guidance.
- Proposal output now includes staged payment schedule rows for deposit and final balance.
- Payment & Collection now shows a Payment Schedule panel for the selected job using linked quote deposit percentage where available.
- Job payment summary export now includes staged payment schedule rows before the payment ledger.
- Added a one-time future change plan grouped by expected version numbers.
- Preserved existing payment recording, invoice/receipt generation, handover, balance reminder and quote/job conversion behavior.

Validation completed:

- Debug build succeeds with zero warnings and zero errors.
- Release publish succeeds through `win-x64-self-contained`.
- Published `OPALNOVA.exe` launches and closes cleanly.

## V1.77.0 State

V1.77.0 continues workspace navigation polish:

- Bumped visible/project version metadata to 1.77.0.
- Added a dashboard `Recent Work` panel for the current app session.
- Recent Work tracks hosted workflow tabs, saved record editor tabs, and generated report previews.
- Recent entries deduplicate automatically, show item type/context/time, can be cleared, and can reopen supported workflow tabs, reports, quote tabs and saved record editors.
- Reopening saved record editors preserves the existing hosted editor unsaved-change protection.
- Cleaned visible top-toolbar and touched preview separator text to plain ASCII.
- Preserved database schema and existing tab close, report preview, record editor, quote, payment, production and dashboard behavior.

Validation completed:

- Debug build succeeds with zero warnings and zero errors.
- Release publish succeeds through `win-x64-self-contained`.
- Published `OPALNOVA.exe` launches and closes cleanly.

## V1.76.0 State

V1.76.0 continues production workflow polish:

- Bumped visible/project version metadata to 1.76.0.
- Added `DocumentExportService.CreateProductionStageChecklist(...)`.
- Added a `Stage Checklist` action to Production Board, Production studio, Production & Opal Studio and Documents Studio.
- The checklist output reviews current stage, due date, customer contact, quote/accepted option context, payments, reservations, supplier diamond waits, open tasks, linked job photos/files, design notes, approval notes and bench notes.
- Cleaned visible Production Board separator/title text in the touched screen.
- Preserved database schema and existing production, payment, quote, inventory, photo and task behavior.

Validation completed:

- Debug build succeeds with zero warnings and zero errors.
- Release publish succeeds through `win-x64-self-contained`.
- Published `OPALNOVA.exe` launches and closes cleanly.

## V1.75.0 State

V1.75.0 continues customer relationship workflow polish:

- Bumped visible/project version metadata to 1.75.0.
- Added a `Communication Templates` action to Customer Relationship Studio.
- Added `CustomerRelationshipService.CreateCustomerCommunicationTemplates(...)`.
- Customer templates are generated from existing customer preferences, quotes, jobs, sales, tasks and value guidance.
- Customer summary cards and timelines now include lifetime value, value guidance, suggested next step and repeat follow-up suggestions.
- Customer relationship report now includes lifetime value, value guidance and suggested next step columns.
- Created customer follow-up tasks now include value guidance and a relevant message starter in follow-up notes.
- Preserved database schema and existing customer, quote, job, sale, payment, task and report behavior.

Validation completed:

- Debug build succeeds with zero warnings and zero errors.
- Release publish succeeds through `win-x64-self-contained`.
- Published `OPALNOVA.exe` launches and closes cleanly.

## V1.74.0 State

V1.74.0 completes the payment handover checklist polish item:

- Bumped visible/project version metadata to 1.74.0.
- Added a live `Handover Checklist` panel to Payment & Collection.
- Checklist items cover payment checked, item condition checked, customer notified/tracking shared, care instructions included, and handover document ready.
- The checklist auto-checks payment when no balance is owing and stays tied to the selected job across same-job refreshes.
- Handover checklist summaries are included in pickup reminders, handover confirmations, ready/complete notes, sale notes and completion notes through the existing handover notes path.
- Preserved database schema and existing payment, sale, invoice/receipt, handover confirmation, reminder and job completion behavior.

Validation completed:

- Debug build succeeds with zero warnings and zero errors.
- Release publish succeeds through `win-x64-self-contained`.
- Published `OPALNOVA.exe` launches and closes cleanly.

## V1.73.0 State

V1.73.0 broadens unsaved-change protection to hosted record editor tabs:

- Bumped visible/project version metadata to 1.73.0.
- Added `IWorkspaceCloseRequestHandler` and close-decision support for hosted workspace tabs.
- Implemented field snapshot dirty detection in `EditEntityWindow`.
- Closing a changed hosted record editor now prompts Save, Discard, or Cancel.
- Save from the close prompt routes through the existing `Saved` event so normal `MainWindow` validation, business rules and database persistence still apply.
- Preserved database schema and existing quote, payment, sale, document, reminder and job completion behavior.

Validation completed:

- Debug build succeeds with zero warnings and zero errors.
- Release publish succeeds through `win-x64-self-contained`.
- Published `OPALNOVA.exe` launches and closes cleanly.

## V1.72.0 State

V1.72.0 continues payment workflow polish inside the job editor:

- Bumped visible/project version metadata to 1.72.0.
- Added a saved-job payment history panel to `EditEntityWindow`.
- The job editor now shows total, paid, balance, payment count, linked customer, sale-created state, and read-only payment ledger rows for the selected job.
- Reuses the existing Payment & Collection paid/balance calculation pattern for compatibility with older deposit-only records.
- Preserved database schema and existing payment recording, sale creation, invoice/receipt, handover confirmation and job completion behavior.

Validation completed:

- Debug build succeeds with zero warnings and zero errors.
- Release publish succeeds through `win-x64-self-contained`.
- Published `OPALNOVA.exe` launches and closes cleanly.

## V1.71.0 State

V1.71.0 continues payment and handover workflow polish:

- Bumped visible/project version metadata to 1.71.0.
- Added `Create Thank-You Follow-Up` to Payment & Collection handover actions.
- Creates duplicate-safe customer follow-up tasks linked to the selected job/customer.
- The task includes a customer-ready after-care check-in message.
- Preserved database schema and existing payment, sale, invoice/receipt, handover confirmation and job completion behavior.

Validation completed:

- Debug build succeeds with zero warnings and zero errors.
- Release publish succeeds through `win-x64-self-contained`.
- Published `OPALNOVA.exe` launches and closes cleanly.

## V1.70.0 State

V1.70.0 continues payment and handover workflow polish:

- Bumped visible/project version metadata to 1.70.0.
- Added `DocumentExportService.CreateHandoverConfirmationFromJob(...)`.
- Added `Generate Handover Confirmation` to Payment & Collection handover actions.
- Handover confirmation output includes customer/job details, payment summary, linked payment ledger, collection/shipping checklist, handover notes, handover status guidance and signature lines.
- Preserved database schema and existing payment, sale, invoice/receipt and job completion behavior.

Validation completed:

- Debug build succeeds with zero warnings and zero errors.
- Release publish succeeds through `win-x64-self-contained`.
- Published `OPALNOVA.exe` launches and closes cleanly.

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
