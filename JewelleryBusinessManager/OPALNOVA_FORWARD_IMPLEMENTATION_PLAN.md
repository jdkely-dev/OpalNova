# OPALNOVA Forward Implementation Plan

Created: 2026-06-22
Current baseline: V1.48.3 Roadmap and Credential Safety Polish

## Purpose

This plan turns the broad future-feature list into a practical build sequence based on:

- daily workflow impact for a jeweller using OPALNOVA.
- ease of modification in the current WPF/C# codebase.
- dependency order between quote, proposal, production, payment, inventory, and reporting workflows.
- risk to existing data.

The main direction is to improve the quality of existing workflows before adding large new systems.

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
| Backup health indicator before heavier data changes | P0 | S-M | Backup services exist; useful before inventory consumption and schema additions. | V1.49.x |
| Quote workspace two-panel polish | P1 | M | Quote workflow drives proposals, approvals, deposits, stock reservations, and jobs. Existing quote model supports most needs. | V1.49 |
| Quote option comparison inside app | P1 | S-M | `QuoteOption` already has pricing fields and recommendation flag. Mostly UI and calculation presentation. | V1.49 |
| Strong quote next-action buttons | P1 | M | Existing accepted option, linked job, task, and reservation features can be made easier to use. | V1.49 |
| Quote expiry and unanswered quote follow-ups | P1 | S-M | `CustomQuote.ValidUntil` and `BusinessTask` already exist. | V1.49 |
| Proposal output redesign | P1 | M | Existing HTML service means high customer-facing value without first adding PDF complexity. | V1.50 |
| Proposal PDF export | P2 | M-L | Best done after the HTML proposal is stable. Needs render/export verification. | V1.50 |
| Universal next-action service | P2 | M-L | Project Hub rules exist, but should move into a shared service before broader alerts. | V1.51 |
| Alert centre | P2 | M | Dashboard already has alert tiles and task data; consolidate into one alert model/view. | V1.51 |
| Recently opened tabs/items | P2 | S-M | Useful once tab lifecycle is stable. Can be local settings-backed first. | V1.51 |
| Unsaved-change warnings for tab close | P2 | M-L | Important, but must be designed across hosted editors to avoid false prompts. | V1.51 |
| Payment schedule tracking | P2 | M | Supports quote approvals and handover. Should follow proposal/action changes. | V1.50-V1.51 |
| Polished invoice/receipt templates | P2 | M | `PaymentCollectionWindow` already has invoice generation entry point. | V1.50-V1.51 |
| Job completion checklist and stock consume/release wizard | P2 | L | High value but touches inventory, jobs, traceability, and irreversible stock movement. | V1.52 |
| Stock lifecycle clarity | P2 | M-L | Current statuses exist, but owned/reserved/supplier/sold/consumed need cleaner UI and audit rules. | V1.52 |
| External diamond refresh availability/price | P3 | M-L | API search exists; refresh needs careful schema/API behavior and no hardcoded assumptions. | V1.53 |
| Supplier diamond intake and conversion to owned inventory | P3 | L | Existing receipt fields exist; conversion needs stock creation, audit, and status rules. | V1.53 |
| Visual reports and Excel export | P3 | M-L | Existing report services and CSV exports help, but charts/export need stable datasets. | V1.54 |
| Customer profile dashboard and timeline | P3 | M | Customer relationship service exists. Best after quote/payment/job event flows are stronger. | V1.54-V1.55 |
| Market/POS speed polish | P3 | M | Existing market windows exist. Should follow core quote/payment/inventory improvements. | V1.55 |
| Automated backup schedule | P3 | L | Health indicator is easy; true scheduling is OS/app lifecycle work. | V1.55 |
| Installer, shortcut, version check, user guide | P3 | L-XL | Release-readiness work after app workflows stabilize. | V1.55+ |
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

### V1.50 - Premium Proposal Output

Goal: make customer-facing proposals strong enough to send directly.

Primary files:

- `Services/CustomQuoteDocumentService.cs`
- `Services/DocumentExportService.cs`
- `Views/CustomQuoteBuilderWindow.xaml.cs`
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
- Add proposal revision metadata after the layout is stable.
- Add PDF export only after HTML output is visually stable.

Definition of done:

- HTML proposal opens cleanly and reads as customer-facing, not internal.
- Proposal includes all quote options and linked supplier diamond details where available.
- PDF output matches the HTML enough for practical sending/printing.
- No unrelated quote workflow behavior changes.

### V1.51 - Universal Next Action And Alert Centre

Goal: move from scattered reminders to one shared action engine.

Primary files:

- new `Services/NextActionService.cs`
- new `Models/NextActionItem.cs` or view model class if persisted storage is not needed.
- `Views/ProjectWorkbenchWindow.xaml.cs`
- `Services/TaskWorkflowService.cs`
- `MainWindow.xaml`
- `MainWindow.xaml.cs`
- possible new `Views/AlertCentreWindow.xaml`

Scope:

- Extract action rules from Project Workbench into a reusable service.
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
- Add one Alert Centre surface fed by the same service.
- Add dashboard entry points to the same alert data.
- Add recently opened items after alert navigation is stable.
- Add unsaved-change warnings after tab/editor lifecycle is consistent.

Definition of done:

- Project Hub and Alert Centre agree on counts and priorities.
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

- Visual quote conversion report.
- Sales and cashflow charts.
- Inventory valuation and ageing.
- Slow-moving stock report.
- Profit by job type/category.
- Tax/GST summary.
- Excel export after datasets are stable.

Definition of done:

- Reports use the same calculations as dashboard/workflows.
- CSV/HTML exports still work.
- Excel export is added only for stable datasets.

### V1.55 - Release Readiness And Help

Goal: prepare for smoother real-world use and updates.

Scope:

- Backup health indicator on dashboard.
- Restore preview improvements.
- Full business archive export.
- Release notes viewer inside app.
- User guide/help manual.
- Installer and desktop shortcut creation.
- Version/update check.

Definition of done:

- User can verify backup status without opening settings.
- Restore remains preview-first and non-destructive until confirmed.
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

Start V1.49 with this ticket:

Title: V1.49 Quote Workspace Polish - layout, comparison, and next actions

Tasks:

- Rework `CustomQuoteBuilderWindow` layout into setup/options/action zones.
- Add an option comparison summary that updates from current quote options.
- Add action rail buttons for preview, recommend, accept, follow-up, convert to job, and release reservations.
- Add quote expiry status text and create-follow-up action.
- Keep existing save/load/link/reservation behavior intact.

Validation:

- `dotnet build .\JewelleryBusinessManager.csproj --no-restore`
- `dotnet publish .\JewelleryBusinessManager.csproj -c Release -p:PublishProfile=win-x64-self-contained --no-restore`
- launch smoke of published `OPALNOVA.exe`
- manual quote smoke:
  - create quote.
  - add two options.
  - link material/stone/external diamond.
  - preview proposal.
  - accept one option.
  - create job.
  - close/reopen quote workspace.
