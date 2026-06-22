# OPALNOVA Development Roadmap

## Current Baseline

Current workspace baseline: V1.55.0.

V1.48.2 completed the workspace stability pass for Project Workbench counts, hosted-tab initialization, tab close behavior, workspace fill, and Project Workbench dark control consistency. V1.48.3 removed the unsafe Nivoda development credential helper and aligned the roadmap to the actual app state. V1.49.0 started the quote workspace polish pass with clearer quote status, option comparison, and next-action controls. V1.49.1 adds design-image attachment, preview, and proposal embedding for quote options. V1.50.0 adds the proposal send/record workflow, proposal tracking fields, email draft preparation, sent-proposal follow-up creation, and a cleaner proposal HTML layout. V1.51.0 adds a shared runtime next-action service, a workspace-hosted Alert Centre, dashboard alert count, and setup-readiness guidance. V1.52.0 adds an explicit job completion checklist that consumes reserved materials, marks reserved stones as set, records material movement audit entries, and releases unconsumed reservations through a reviewable workflow. V1.53.0 adds received external diamond conversion into owned loose-stone inventory without schema changes. V1.54.0 adds a single Excel-compatible BI workbook export for stable reporting datasets. V1.55.0 adds dashboard backup health, restore preview, and release notes access.

The next work should stay focused on improving daily workflow quality before adding broad new feature areas.

For the detailed priority/ease analysis and implementation sequence, see `OPALNOVA_FORWARD_IMPLEMENTATION_PLAN.md`.

## Priority Order

### V1.49 - Quote Workspace Polish

Goal: make quoting faster, clearer, and more customer-ready without changing quote data contracts.

- Redesign the Custom Quote workspace into a cleaner two-panel layout.
- Keep customer/project setup on the left and live totals/actions on the right.
- Add clearer option comparison inside the app.
- Add stronger next-action buttons: send proposal, mark accepted, convert to job, link diamonds, reserve stock.
- Add quote expiry and unanswered-quote follow-up prompts.
- Improve design-image attachment handling before adding drag-and-drop.

### V1.50 - Premium Proposal Output And Send Workflow

Goal: make proposals suitable to send directly to customers and record the send/follow-up step without direct email-delivery risk.

- Create a premium proposal HTML layout first. Done in V1.50.0 as a first pass.
- Add proposal send/record modal with recipient, subject, message, proposal link, copy/open email draft, record sent, and follow-up task creation. Done in V1.50.0.
- Surface proposal prepared/sent/accepted/converted state on the quote. Started in V1.50.0.
- Add PDF export after the HTML layout is stable.
- Add option comparison, payment schedule display, and accepted-option clarity.
- Show linked external diamond certificate/video fields where available.
- Add proposal revision/version metadata before adding full quote revision history.

### V1.51 - Universal Next Action and Alert Centre

Goal: turn OPALNOVA into a guided daily work system rather than only a record system.

- Extract Project Workbench action rules into a reusable next-action service.
- Add one alert centre for overdue jobs, unpaid balances, expiring diamond holds, low stock, quote expiry, and follow-ups. Done in V1.51.0 as a first pass.
- Add dashboard and workspace entry points to the same alert data. Done in V1.51.0.
- Add guided setup readiness on the dashboard using existing settings/data. Done in V1.51.0.
- Add recently opened records/tabs after the alert centre is stable.
- Add unsaved-change warnings for hosted editors after the tab lifecycle remains stable.

### V1.52 - Inventory Consumption and Job Completion

Goal: close the loop from quote reservation to finished job without losing stock traceability.

- Add a job-completion checklist. Done in V1.52.0 as a first pass.
- Add a reservation release/consume wizard. Done in V1.52.0 for accepted quote reservations linked to jobs.
- Consume reserved materials/stones only through explicit completion actions. Done in V1.52.0.
- Keep owned stock, reserved stock, supplier stock, sold stock, and consumed stock visually distinct. Started in V1.52.0 through consumed/released reservation states.
- Add audit entries for stock adjustments, reservation release, and material consumption. Started in V1.52.0 with material transaction entries for consumed materials.

### V1.53 - External Diamond Production Readiness

Goal: make supplier diamonds safer to quote, hold, order, receive, and convert to owned inventory.

- Keep credentials user-entered only; do not hardcode test credentials.
- Add refresh availability/price for saved external diamonds.
- Add hold-expiry reminders and unavailable-diamond warnings.
- Add received-diamond intake. Started before V1.53.0 and strengthened by conversion workflow.
- Add explicit conversion from received supplier diamond to owned inventory when physically purchased. Done in V1.53.0 as received diamond to owned `Stone`.
- Add API hold/order actions only after the accessible Nivoda schema is confirmed.

### V1.54 - Reports and Export Upgrade

Goal: improve decision-making and bookkeeping output.

- Add visual charts for sales, quote conversion, inventory value, and cashflow.
- Add stock ageing and slow-moving inventory reports.
- Add profit by job type and product category.
- Add tax/GST summary.
- Add Excel export after report datasets are stable. Done in V1.54.0 as an Excel-compatible workbook export.

### V1.55 - Release Readiness and Data Safety

Goal: prepare OPALNOVA for regular real-world use.

- Add automated backup schedule and dashboard backup health indicator.
- Add dashboard backup health indicator. Done in V1.55.0.
- Add restore preview before applying a restore. Done in V1.55.0.
- Add full business archive export.
- Add production/staging configuration separation.
- Add release notes viewer inside the app. Done in V1.55.0.
- Add installer, desktop shortcut creation, and user guide.

## Deferred Until Core Workflow Is Stable

- Full command palette.
- Complex capacity/calendar planning.
- Scheduled report generation.
- Advanced hardware tools beyond the current camera, scale, barcode, and label foundations.
- Customer segmentation and lifetime value scoring.
- API-level Nivoda hold/order actions without confirmed schema support.

## UX Rules For Future Builds

- Fix workflow bottlenecks before adding new record types.
- Prefer shared styles and shared services over one-screen patches.
- Keep workflows in tabs where practical.
- Avoid white controls, bright yellow highlights, and redundant explanation panels.
- Keep external supplier diamonds separate from owned inventory until explicitly received or converted.
- Do not automate material consumption without a reviewable job-completion step.
