# OPALNOVA Forward Implementation Plan

Created: 2026-06-22
Current baseline: V1.66.0 Quote Unsaved Change Guard

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
- Task system: `TaskWorkflowService` and `BusinessTask` already support reminders, categories, due dates, priorities, and dashboard visibility.
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
| Proposal centre / sent proposal queue | P2 | M | Implemented in V1.64.0 as a focused Proposal Pipeline for prepared, sent, due, accepted and converted proposals. | V1.64 |
| Quote measurements, occasion, and internal note polish | P2 | S-M | Implemented in V1.65.0 with additive quote context fields, customer-facing proposal details, internal notes and required-by next-action guidance. | V1.65 |
| Universal next-action service | P2 | M-L | Implemented in V1.51.0 as a shared runtime service for Alert Centre and dashboard counts; Project Hub can be refactored to share it more deeply later. | V1.51 |
| Alert centre | P2 | M | Implemented in V1.51.0 as a workspace-hosted alert list with filters, search, counts, details, and workflow actions. | V1.51 |
| Recently opened tabs/items | P2 | S-M | Useful once tab lifecycle is stable. Can be local settings-backed first. | V1.51 |
| Unsaved-change warnings for tab close | P2 | M-L | Started in V1.66.0 with a reusable close-guard interface and Custom Quote Builder dirty tracking. Broader editor adoption can follow once patterns are proven. | V1.66 |
| Payment schedule tracking | P2 | M | Supports quote approvals and handover. Should follow proposal/action changes. | V1.50-V1.51 |
| Polished invoice/receipt templates | P2 | M | Implemented in V1.57.0 for job invoices/receipts, sale receipts, deposit receipts and payment receipts. | V1.57 |
| Job completion checklist and stock consume/release wizard | P2 | L | Implemented in V1.52.0 as an explicit completion checklist that consumes reserved materials, marks reserved stones set, releases unconsumed reservations, and writes material movement audit entries. | V1.52 |
| Production stage checklist, waiting flags, and job files | P2 | M-L | The transcript shows these as central after proposal acceptance. Do this around the safe job-completion work. | V1.52 |
| Stock lifecycle clarity | P2 | M-L | Started in V1.52.0 through consumed/released reservation states; broader stock lifecycle UI still needs later polish. | V1.52 |
| External diamond refresh availability/price | P3 | M-L | API search exists; refresh needs careful schema/API behavior and no hardcoded assumptions. | V1.53 |
| Supplier diamond intake and conversion to owned inventory | P3 | L | Implemented in V1.53.0 as duplicate-safe conversion from received external diamond to owned loose `Stone` inventory. | V1.53 |
| Visual reports and Excel export | P3 | M-L | Excel-compatible workbook export is implemented in V1.54.0; stock ageing/slow-moving inventory is implemented in V1.58.0; profitability reporting is implemented in V1.59.0; tax/GST summary is implemented in V1.60.0; visual charts are implemented in V1.61.0. | V1.54, V1.58, V1.59, V1.60, V1.61 |
| Customer profile dashboard and timeline | P3 | M | Customer timeline implemented in V1.56.0 using existing quote, proposal, job, sale, payment and task records; fuller dashboard remains later polish. | V1.56 |
| Client import polish | P3 | M | Useful from the transcript, but OPALNOVA's proposal/production flow has higher immediate value. | V1.54-V1.55 |
| Market/POS speed polish | P3 | M | Existing market windows exist. Should follow core quote/payment/inventory improvements. | V1.55 |
| Automated backup schedule | P3 | L | Health indicator is easy; true scheduling is OS/app lifecycle work. | V1.55 |
| Installer, shortcut, version check, user guide | P3 | L-XL | Release notes viewer implemented in V1.55.0; installer/shortcut/version check remain later release work. | V1.55+ |
| Built-in help/searchable guide | P3 | M-L | The built-in user guide was refreshed in V1.62.0 into a practical local manual; searchable/paginated help remains later. | V1.62+ |
| Command palette/global command bar | P4 | M-L | Useful, but not as important as fixing workflow surfaces first. | Later |
| Complex capacity/calendar view | P4 | L-XL | Needs better production stage/checklist data first. | Later |
| Scheduled reports | P4 | XL | Requires background scheduling and report stability. | Later |
| Advanced hardware tools | P4 | M-XL | Current foundations exist, but hardware support can sprawl quickly. | Later |
| API-level Nivoda hold/order | P4 | XL | Must wait for confirmed accessible schema and production credentials. | Later |

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

Start V1.63 with this ticket:

Title: V1.63 Text Encoding and Document Copy Cleanup - remove stale artifacts

Tasks:

- Scan generated documents, release notes, guide text and visible UI strings for stale mojibake or unclear legacy labels.
- Replace corrupted punctuation with plain ASCII-safe text or existing project style.
- Keep the cleanup limited to user-facing text and generated HTML copy.
- Avoid changing business logic, database schema or workflow behavior.
- Rebuild and smoke test after cleanup because encoded text can sit inside raw string literals.

Validation:

- `dotnet build .\JewelleryBusinessManager.csproj --no-restore`
- `dotnet publish .\JewelleryBusinessManager.csproj -c Release -p:PublishProfile=win-x64-self-contained --no-restore`
- launch smoke of published `OPALNOVA.exe`
- manual text cleanup smoke:
  - open User Guide and Release Notes.
  - generate at least one common document and one report.
  - confirm no corrupted punctuation is visible in touched outputs.
  - confirm no business records are changed.
