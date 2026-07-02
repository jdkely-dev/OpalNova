# OPALNOVA Forward Implementation Plan

Created: 2026-06-22
Current baseline: V2.6.0 Roadmap Completion Record

## Purpose

This plan turns the broad future-feature list into a practical build sequence based on:

- daily workflow impact for a jeweller using OPALNOVA.
- ease of modification in the current WPF/C# codebase.
- dependency order between quote, proposal, production, payment, inventory, and reporting workflows.
- risk to existing data.

The main direction is to improve the quality of existing workflows before adding large new systems.

## Reference Walkthrough Refinements

The `reaserch vid.mp4` reference walkthrough reinforces three product priorities:

- Add guided dashboard/setup progress earlier than originally planned.
- Treat proposal sending as part of the proposal workflow, not as a later admin feature.
- Keep setup/pricing/supplier readiness visible as actionable checklist items.
- Surface proposal sent/viewed/accepted states before building a larger Proposal Centre.
- Use proposal acceptance as the bridge into deposits, invoicing, and production next actions.

See `OPALNOVA_REFERENCE_WALKTHROUGH_ANALYSIS.md` and `OPALNOVA_REFERENCE_VIDEO_TRANSCRIPT.md` for the detailed observations.

## Current Foundations

OPALNOVA already has useful foundations that should be reused:

- Quote workflow: `CustomQuoteBuilderWindow`, `CustomQuote`, `QuoteOption`, linked stones/materials/external diamonds, accepted option, quote expiry, linked job.
- Proposal output: `CustomQuoteDocumentService` already creates proposal HTML.
- Project/work guidance: `ProjectWorkbenchWindow` already calculates next-action style rows and creates follow-up tasks.
- Task system: `TaskWorkflowService` and `BusinessTask` already support reminders, categories, due dates, priorities, duplicate-safe open-task checks, and dashboard visibility.
- Production: `ProductionBoardWindow` already exposes job status lanes and movement.
- Payments and handover: `PaymentCollectionWindow` already handles payment recording, invoices, pickup/shipping state, sale creation.
- Supplier diamonds: `DiamondSupplierWindow`, `NivodaDiamondApiService`, `SupplierDiamondWorkflowWindow`, and `ExternalDiamond` already cover search, save, quote links, holds, order/receipt fields, and reminders.
- Inventory traceability: `InventoryTraceService`, material transactions, stock statuses, quote reservation links.
- Data safety: `BackupService` and `DataSafetyService` already exist.

## Priority And Ease Scale

Priority:

- P0: protect build quality, data, and release safety.
- P1: highest user value and supports later work.
- P2: important, but should follow P1 foundations.
- P3: useful medium-term improvements.
- P4: defer until core workflows are stable.

Ease:

- S: small change, mostly one view/service, no schema.
- M: moderate change, multiple files or additive columns/tables.
- L: large change, shared behavior or multiple workflows.
- XL: multi-system, external dependency, installer, scheduling, or hardware.

## Ranked Backlog

| Area | Priority | Ease | Why | Recommended Build |
| --- | --- | --- | --- | --- |
| Keep build clean, release notes, git hygiene | P0 | S | Prevents future regressions and makes every build auditable. | Every build |
| Text encoding and generated copy cleanup | P0 | S | Implemented in V1.63.0 to keep generated documents, trace reports, metadata and support copy consistent across Windows viewers. | V1.63 |
| Backup health indicator before heavier data changes | P0 | S-M | Implemented in V1.55.0 as a dashboard data-safety card using existing backup services. | V1.55 |
| Quote workspace two-panel polish | P1 | M | Quote workflow drives proposals, approvals, deposits, stock reservations, and jobs. Existing quote model supports most needs. | V1.49 |
| Quote option comparison inside app | P1 | S-M | `QuoteOption` already has pricing fields and recommendation flag. Mostly UI and calculation presentation. | V1.49 |
| Strong quote next-action buttons | P1 | M | Existing accepted option, linked job, task, and reservation features can be made easier to use. | V1.49 |
| Quote expiry and unanswered quote follow-ups | P1 | S-M | `CustomQuote.ValidUntil` and `BusinessTask` already exist. | V1.49 |
| Proposal output redesign | P1 | M | Existing HTML service means high customer-facing value without first adding PDF complexity. | V1.50 |
| Proposal send/record workflow | P1 | M | Reference app shows proposal sending as a simple modal. OPALNOVA can draft/copy/open email first, record sent status, and create follow-ups without direct SMTP. | V1.50 |
| Proposal sent/accepted status surface | P1 | M | The transcript shows sent/viewed/signed/accepted states driving production and invoicing. OPALNOVA needs clearer state before a Proposal Centre. | V1.50 |
| Proposal email templates and payment schedule display | P1 | S-M | Template-driven message text and deposit/final-balance clarity are high-value and fit the existing quote/proposal flow. | V1.50 |
| Guided first-run/setup checklist | P1 | S-M | Dashboard, settings, tasks, and backup services already exist. Implemented as dashboard setup readiness in V1.51.0. | V1.51 |
| Proposal PDF export | P2 | M-L | Best done after the HTML proposal is stable. Needs render/export verification. | V1.50 |
| Proposal revision / PDF-ready polish | P2 | S-M | Started in V1.85.0 with revisioned proposal HTML snapshots and browser Print / Save as PDF guidance without adding a PDF renderer dependency. | V1.85 |
| Proposal centre / sent proposal queue | P2 | M | Implemented in V1.64.0 as a focused Proposal Pipeline for prepared, sent, due, accepted and converted proposals. | V1.64 |
| Quote measurements, occasion, and internal note polish | P2 | S-M | Implemented in V1.65.0 with additive quote context fields, customer-facing proposal details, internal notes and required-by next-action guidance. | V1.65 |
| Preferred ring size / metal / stone in quote workflow | P2 | S | Started in V1.67.0 with an explicit customer-preference fill action that copies blank quote preference fields from the selected customer profile. | V1.67 |
| Universal next-action service | P2 | M-L | Implemented in V1.51.0 as a shared runtime service for Alert Centre and dashboard counts; Project Hub can be refactored to share it more deeply later. | V1.51 |
| Alert centre | P2 | M | Implemented in V1.51.0 as a workspace-hosted alert list with filters, search, counts, details, and workflow actions. | V1.51 |
| Recently opened tabs/items | P2 | S-M | Implemented in V1.77.0 as a dashboard Recent Work panel for current-session workflow tabs, generated reports and saved record editors. | V1.77 |
| Unsaved-change warnings for tab close | P2 | M-L | Started in V1.66.0 with Custom Quote Builder dirty tracking and broadened in V1.73.0 to hosted generic record editor tabs. | V1.66, V1.73 |
| Shared selector and date-picker polish | P2 | S | Implemented in V1.94.0 with shared ComboBox empty prompts, dark DatePicker/calendar styling and local selector styles routed through the global theme. | V1.94 |
| Workflow control consolidation | P2 | S | Implemented in V1.95.0 by routing remaining priority workflow Button/TextBox styles through shared templates and adding explicit ComboBox prompt text across XAML. | V1.95 |
| Workspace surface reduction | P2 | S | Implemented in V1.96.0 by compacting workflow headers, metric cards and selected-detail panels in the most-used operational workspaces. | V1.96 |
| Daily workflow edge polish | P2 | S | Implemented in V1.97.0 by adding a selected-job Production Board handoff into focused Payment & Collection tabs and keeping the selected job visible across payment filters. | V1.97 |
| Support snapshot polish | P2 | S | Implemented in V1.98.0 by adding a read-only support snapshot for version, runtime, data, backup, printout, photo, settings and log paths. | V1.98 |
| Pre-milestone hardening review | P0 | S | Started in V1.99.0 by reviewing V1.94-V1.98 workflow/support changes, correcting Customer Timeline help routing and removing duplicated Communication Templates help metadata before the V2.0 checkpoint. | V1.99 |
| Release-candidate validation checkpoint | P0 | S | Completed in V2.0.0 with static checks for selector prompt coverage, help-guide key uniqueness and per-section tool-action title uniqueness, plus refreshed release metadata and validation docs. | V2.0 |
| Post-V2 product decision review | P0 | S | Completed in V2.1.0 by adding a read-only decision report for installer/update, direct email, supplier API ordering, deeper scheduling, multi-user/cloud sync and navigation direction before broad new systems are started. | V2.1 |
| Installer/update readiness | P0 | S | Started in V2.2.0 by choosing installer/update readiness as the first concrete post-V2 direction and adding a read-only report for installer decisions, update-channel boundaries, portable build handoff and distribution cautions without creating installer or auto-update behavior. | V2.2 |
| Installer validation checklist | P0 | S | Started in V2.3.0 by choosing the portable publish folder as the first validation route and adding a read-only checklist for executable fingerprinting, local data boundaries, update rehearsal gates, rollback checks and installer hold conditions before creating installer assets. | V2.3 |
| Portable build manifest | P0 | S | Started in V2.4.0 by adding a read-only manifest for executable version/hash, publish-folder inventory, private-data exclusions and handoff notes before portable sharing or installer packaging. | V2.4 |
| Packaging decision record | P0 | S | Completed in V2.5.0 by recording portable handoff as the validated route and keeping MSIX/Inno Setup as explicit future packaging tickets with signing, shortcuts, update channel, uninstall and rollback rules required before implementation. | V2.5 |
| Roadmap completion record | P0 | S | Completed in V2.6.0 by recording the current no-schema version stream as finished and listing the remaining major decisions that require explicit selection before implementation. | V2.6 |
| Payment schedule tracking | P2 | M | Implemented in V1.78.0 as shared quote/job schedule guidance shown in proposals, job payment summaries and Payment & Collection. | V1.78 |
| Polished invoice/receipt templates | P2 | M | Implemented in V1.57.0 for job invoices/receipts, sale receipts, deposit receipts and payment receipts. | V1.57 |
| Balance reminder messages | P2 | S | Implemented in V1.68.0 through Payment & Collection copy-ready reminder text and duplicate-safe follow-up task creation. | V1.68 |
| Reminder task consistency | P2 | S | Implemented in V1.69.0 through shared duplicate-safe open-task checks and consistent task-code generation across active reminder workflows. | V1.69 |
| Shipping/collection confirmation document | P2 | S | Implemented in V1.70.0 as a Payment & Collection handover confirmation with payment summary, checklist, notes and sign-off lines. | V1.70 |
| Final customer thank-you/follow-up task | P2 | S | Implemented in V1.71.0 as a duplicate-safe Payment & Collection task with customer-ready after-care follow-up text. | V1.71 |
| Partial-payment history inside jobs | P2 | S | Implemented in V1.72.0 as a read-only payment summary and ledger panel inside saved job editor tabs. | V1.72 |
| Payment handover checklist | P2 | S | Implemented in V1.74.0 as a live Payment & Collection checklist whose summary feeds reminders, handover confirmation, sale notes and completion notes. | V1.74 |
| Customer communication templates | P3 | S-M | Implemented in V1.75.0 as customer-specific quote, production, handover, after-care and repeat-customer message starters. | V1.75 |
| Customer lifetime value and repeat follow-up guidance | P3 | S-M | Started in V1.75.0 through customer summary/timeline/report value guidance and generated follow-up notes. | V1.75 |
| Job completion checklist and stock consume/release wizard | P2 | L | Implemented in V1.52.0 as an explicit completion checklist that consumes reserved materials, marks reserved stones set, releases unconsumed reservations, and writes material movement audit entries. | V1.52 |
| Production stage checklist, waiting flags, and job files | P2 | M-L | Started in V1.76.0 with a generated stage checklist covering readiness, waits, quote context, reservations, supplier diamonds, payments, tasks and linked job photos/files. | V1.76 |
| Stock lifecycle clarity | P2 | M-L | Strengthened in V1.79.0 with shared lifecycle guidance for status changes, supplier diamonds and inventory reports. | V1.52, V1.79 |
| Stability milestone and redundancy check | P0 | S-M | Completed in V1.80.0 across V1.76-V1.79 and repeated in V1.90.0 across V1.81-V1.89 before the milestone commit/push. | V1.80, V1.90 |
| Inventory valuation and reorder intelligence | P3 | M | Started in V1.93.0 through a read-only report combining category valuation, low-stock reorder coverage, slow-moving guidance, supplier-stock state and material adjustment audit signals. | V1.93 |
| External diamond refresh availability/price | P3 | M-L | API search exists; live refresh still needs confirmed credential/schema behavior. V1.83.0 adds local replacement-search copy from saved diamond data and close saved alternatives. V1.91.0 adds staging handoff/schema diagnostics so Nivoda can confirm the accessible fields and mutations. V1.92.0 surfaces supplier diamond state in Operations Performance for weekly review, and V1.93.0 includes supplier stock state in Inventory Intelligence. | V1.83+ / V1.91+ |
| Supplier diamond intake and conversion to owned inventory | P3 | L | Implemented in V1.53.0 as duplicate-safe conversion from received external diamond to owned loose `Stone` inventory. | V1.53 |
| Visual reports and Excel export | P3 | M-L | Excel-compatible workbook export is implemented in V1.54.0; stock ageing/slow-moving inventory is implemented in V1.58.0; profitability reporting is implemented in V1.59.0; tax/GST summary is implemented in V1.60.0; visual charts are implemented in V1.61.0. | V1.54, V1.58, V1.59, V1.60, V1.61 |
| Customer profile dashboard and timeline | P3 | M | Customer timeline implemented in V1.56.0 using existing quote, proposal, job, sale, payment and task records; V1.75 adds value guidance and communication templates. | V1.56, V1.75 |
| Client import polish | P3 | M | Useful from the transcript, but OPALNOVA's proposal/production flow has higher immediate value. | V1.54-V1.55 |
| Market/POS speed polish | P3 | M | Implemented in V1.81.0 by routing Market Operations sales through shared sale/reconciliation logic and improving packed/sold/returned state handling. | V1.81 |
| Inventory media and batch workflow | P3 | S-M | Started in V1.82.0 with multi-select batch photo import through the existing record detail photo workflow and `PhotoRecord` storage. | V1.82 |
| Production time, capacity, and scheduling | P3 | M-L | Started in V1.84.0 with a no-schema Production Capacity Snapshot using existing due dates, labour hours and active production batches. | V1.84 |
| Data integrity check | P0 | S-M | Started in V1.86.0 as a read-only report for orphaned links, missing files, inconsistent market stock states and incomplete payment links. | V1.86 |
| Automated backup schedule | P3 | L | Health indicator is easy; true scheduling is OS/app lifecycle work. V1.86.0 deferred true scheduling until app lifecycle or installer support is explicitly chosen. | V1.55+ |
| Installer, shortcut, version check, user guide | P3 | L-XL | Release notes viewer implemented in V1.55.0; V1.89.0 adds release-readiness packaging notes and validation gates; V2.2-V2.5 complete portable handoff readiness and decision records before any installer implementation work. | V1.55, V1.89, V2.2-V2.5 |
| Built-in help/searchable guide | P3 | M-L | The built-in user guide was refreshed in V1.62.0 into a practical local manual; V1.87.0 starts findability by adding workflow actions to Search All. | V1.62, V1.87 |
| Command palette/global command bar | P4 | M-L | V1.87.0 starts a low-risk version by extending Search All with workflow-action navigation instead of adding a new command data model. | V1.87+ |
| Complex capacity/calendar view | P4 | L-XL | Needs better production stage/checklist data first. | Later |
| Scheduled reports | P4 | XL | Requires background scheduling and report stability. | Later |
| Practical jeweller calculators | P3 | S-M | Started in V1.88.0 with local ring-size, metal-weight and stone-carat tools that do not depend on hardware devices or schema changes. | V1.88 |
| Advanced hardware tools | P4 | M-XL | Current foundations exist, but hardware support can sprawl quickly. V1.88.0 intentionally keeps this pass to no-hardware local calculators. | Later |
| API-level Nivoda hold/order | P4 | XL | Must wait for confirmed accessible schema and production credentials. V1.91.0 adds a staging handoff and mutation discovery report to unblock that confirmation step. | Later |

## Recommended Build Sequence

### V1.49 - Quote Workspace Polish

Goal: make quoting faster, clearer, and more guided without changing the quote data contract unless a small additive column is clearly needed.

Primary files:

- `Views/CustomQuoteBuilderWindow.xaml`
- `Views/CustomQuoteBuilderWindow.xaml.cs`
- `Services/CustomQuoteDocumentService.cs`
- `Models/CustomQuote.cs`
- `Models/QuoteOption.cs`
- `Services/TaskWorkflowService.cs`

Scope:

- Redesign the quote builder into a clearer two-panel or three-zone workflow:
  - quote/customer setup.
  - option editing and linked inventory.
  - totals, warnings, and next actions.
- Add an option comparison surface:
  - option name.
  - recommended flag.
  - total price.
  - deposit.
  - margin/cost summary.
  - linked stones, materials, and external diamonds.
- Add action rail buttons:
  - preview proposal.
  - mark option recommended.
  - mark accepted.
  - create follow-up.
  - convert accepted option to job.
  - release reservations.
- Add quote expiry/follow-up prompts using `ValidUntil` and `BusinessTask`.
- Improve image attachment handling through the existing `QuoteOption.ImagePath` before adding drag-and-drop.

Definition of done:

- Existing quotes still load and save.
- No schema-breaking changes.
- Option totals remain correct after editing, linking, duplicating, accepting, and deleting options.
- Accepted option and created job remain linked.
- Build and publish report zero warnings and zero errors.
- Launch smoke passes.

### V1.50 - Premium Proposal Output And Send Workflow

Goal: make customer-facing proposals strong enough to send directly and make the send/record step simple.

Primary files:

- `Services/CustomQuoteDocumentService.cs`
- `Services/DocumentExportService.cs`
- `Views/CustomQuoteBuilderWindow.xaml.cs`
- possible new `Views/SendProposalWindow.xaml`
- optional new proposal preview/export helpers.

Scope:

- Redesign proposal HTML first:
  - OPALNOVA branded customer-facing layout.
  - clear quote summary.
  - option comparison.
  - recommended option emphasis.
  - linked diamond certificate/video fields.
  - payment schedule/deposit display.
  - terms and expiry.
- Add a proposal send/record modal:
  - recipient.
  - subject.
  - message body.
  - include proposal attachment/output option.
  - copy/open email draft action.
  - record sent status.
- Automatically offer a follow-up task after recording a proposal as sent.
- Add proposal revision metadata after the layout is stable.
- Add PDF export only after HTML output is visually stable.

Definition of done:

- HTML proposal opens cleanly and reads as customer-facing, not internal.
- Proposal includes all quote options and linked supplier diamond details where available.
- User can prepare proposal email text from OPALNOVA without hunting through files.
- Quote status can be recorded as sent without direct email integration.
- Follow-up task creation is offered after sending/recording.
- PDF output matches the HTML enough for practical sending/printing.
- No unrelated quote workflow behavior changes.

### V1.51 - Universal Next Action And Alert Centre

Goal: move from scattered reminders to one shared action engine, while adding a clearer first-run setup path.

Primary files:

- new `Services/NextActionService.cs`
- new `Models/NextActionItem.cs` or view model class if persisted storage is not needed.
- `Views/ProjectWorkbenchWindow.xaml.cs`
- `Services/TaskWorkflowService.cs`
- `MainWindow.xaml`
- `MainWindow.xaml.cs`
- possible new `Views/AlertCentreWindow.xaml`
- possible new `Views/SetupChecklistWindow.xaml`

Scope:

- Add a reusable next-action service, then progressively align Project Workbench rules to it after the alert centre proves stable.
- Generate next actions for:
  - quote expiry.
  - unanswered quotes.
  - accepted quotes not converted to jobs.
  - overdue jobs.
  - jobs waiting on customer/supplier/stone.
  - unpaid balances.
  - diamond holds expiring.
  - ordered diamonds not received.
  - low stock.
  - open follow-ups.
- Add one Alert Centre surface fed by the same service. Done in V1.51.0.
- Add a guided setup checklist using existing settings/data. Done in V1.51.0 as dashboard setup readiness:
  - business profile complete.
  - labour rates configured.
  - GST/settings reviewed.
  - supplier/Nivoda credentials entered where needed.
  - pricing/material defaults reviewed.
  - backup health checked.
  - first quote/proposal/job created.
- Add dashboard entry points to the same alert data. Done in V1.51.0.
- Add recently opened items after alert navigation is stable.
- Add unsaved-change warnings after tab/editor lifecycle is consistent.

Definition of done:

- Dashboard and Alert Centre agree on action-needed counts and priorities.
- Dashboard can show a setup progress card without crowding daily work tiles.
- Alerts do not create duplicate tasks unless the user explicitly creates one.
- Alert actions open the right workflow tab.
- Existing dashboard tiles remain accurate.

### V1.52 - Inventory Consumption And Job Completion

Goal: make job completion auditable and safe.

Primary files:

- `Views/ProductionBoardWindow.xaml.cs`
- `Views/InventoryMovementWindow.xaml.cs`
- `Services/InventoryTraceService.cs`
- `Models/MaterialTransaction.cs`
- `Models/JewelleryItem.cs`
- `Models/Stone.cs`
- `Models/Job.cs`
- optional new completion wizard view.

Scope:

- Add job completion checklist.
- Add a reviewable consume/release wizard:
  - consume reserved materials used in the job.
  - mark stones/materials as used only after review.
  - release unused reservations.
  - create material transaction/audit notes.
- Add clearer UI labels for:
  - owned stock.
  - reserved stock.
  - supplier stock.
  - sold stock.
  - consumed/used stock.
- Add completion quality-control checklist.

Definition of done:

- No stock is consumed automatically without user confirmation.
- Every consumption/release action leaves an audit trail.
- Job completion can be cancelled before saving.
- Traceability report shows the stock movement clearly.

### V1.53 - External Diamond Production Readiness

Goal: make supplier diamonds safer after quote acceptance.

Primary files:

- `Views/DiamondSupplierWindow.xaml.cs`
- `Views/SupplierDiamondWorkflowWindow.xaml.cs`
- `Services/NivodaDiamondApiService.cs`
- `Models/ExternalDiamond.cs`
- `Models/QuoteOptionExternalDiamondLink.cs`

Scope:

- Add refresh availability/price for saved external diamonds.
- Warn when linked diamonds are unavailable or hold expiry is near.
- Add replacement suggestion workflow using saved/searchable diamonds.
- Add received-diamond intake.
- Add explicit conversion from received supplier diamond to owned inventory.
- Keep API hold/order actions deferred until the accessible Nivoda schema is confirmed.

Definition of done:

- No hardcoded credentials.
- Refresh does not overwrite user notes or local workflow status unexpectedly.
- Received-to-owned conversion is explicit and auditable.

### V1.54 - Reports And Export Upgrade

Goal: improve business visibility after workflows produce better data.

Scope:

- Visual quote conversion report. Done in V1.61.0 through the visual chart report.
- Sales and cashflow charts. Done in V1.61.0 through printable HTML/CSS charts.
- Inventory valuation and ageing.
- Slow-moving stock report.
- Profit by job type/category.
- Tax/GST summary. Done in V1.60.0 as a read-only tax/payment summary using existing sales, payments and settings.
- Excel export after datasets are stable. Done in V1.54.0 as a workbook with summary, sales, balances, quotes, inventory, reservation, task and supplier diamond sheets.

Definition of done:

- Reports use the same calculations as dashboard/workflows.
- CSV/HTML exports still work.
- Excel export is added only for stable datasets and opens through the in-app report preview flow.

### V1.55 - Release Readiness And Help

Goal: prepare for smoother real-world use and updates.

Scope:

- Backup health indicator on dashboard. Done in V1.55.0.
- Restore preview improvements. Done in V1.55.0.
- Full business archive export.
- Release notes viewer inside app. Done in V1.55.0.
- User guide/help manual. Done in V1.62.0 as an expanded built-in HTML manual.
- Installer and desktop shortcut creation.
- Version/update check.

Definition of done:

- User can verify backup status without opening settings. Done in V1.55.0.
- Restore remains preview-first and non-destructive until confirmed. Done in V1.55.0.
- Installer/update work does not change database location or schema behavior.

## Quick Wins Worth Taking When Nearby

These are small and can be included opportunistically when touching related files:

- Fix any remaining text encoding artifacts in UI display strings.
- Make quote/proposal/purchase/invoice documents share one OPALNOVA document style helper.
- Add consistent empty states to major workflow windows.
- Add keyboard shortcuts only for stable actions:
  - save quote.
  - preview proposal.
  - new task.
  - refresh current workspace.
- Add tooltip text where an action changes stock, payment, or supplier diamond state.

## Deferred Items And Rationale

- Full command palette: useful, but less valuable than improving the main quote/production/payment paths first.
- Complex calendar/capacity planning: needs richer production stage/checklist data first.
- Scheduled report generation: requires stable report definitions and background scheduling.
- Advanced hardware tools: current foundations can be improved later, but the core workflow should not wait on device integrations.
- Customer lifetime value and segmentation: better after quote, payment, and customer timeline data are more coherent.
- API-level Nivoda hold/order: only after production schema/API access is confirmed.

## Immediate Next Work Ticket

Start the next local build with this ticket. Keep changes uncommitted unless a git checkpoint is explicitly requested.

Title: Choose New Major Stream

Tasks:

- Treat the current no-schema version stream as complete.
- Treat the installer/update readiness track as complete for portable handoff.
- Choose the next major product decision deliberately: MSIX packaging, Inno Setup packaging, true backup scheduling, advanced hardware, scheduled reports, deeper calendar/capacity planning, command-palette expansion or API-level Nivoda hold/order after schema confirmation.
- If packaging is chosen, create a named implementation ticket before coding installer assets.
- For packaging, define signing, install path, shortcut ownership, update channel, uninstall behavior, rollback and local data boundaries first.
- Keep data in the existing local app/user folders and do not move the SQLite database into the install folder.
- Preserve existing quote, production, payment, inventory, supplier diamond, backup, restore, support snapshot and report behavior.

Validation:

- `dotnet build .\JewelleryBusinessManager.csproj --no-restore`
- `dotnet publish .\JewelleryBusinessManager.csproj -c Release -p:PublishProfile=win-x64-self-contained --no-restore`
- launch smoke of published `OPALNOVA.exe`
- manual V2.6 smoke:
  - open the V1.91-V2.5 changed workflows and support reports.
  - open Installer/Update Readiness from Settings & Backup and Safety & Data Studio.
  - open Installer Validation Checklist from Settings & Backup and Safety & Data Studio.
  - open Portable Build Manifest from Settings & Backup and Safety & Data Studio.
  - open Packaging Decision Record from Settings & Backup and Safety & Data Studio.
  - open Roadmap Completion Record from Settings & Backup and Safety & Data Studio.
  - confirm Search All finds installer validation, installer/update, portable build manifest, packaging decision and roadmap completion actions.
  - confirm no stale current-version text remains in visible metadata.
  - confirm no new action creates installers, shortcuts, tasks, payments, stock movements, sales or supplier diamond state changes.
  - confirm generated release/support documents open in preview and include current version context.
