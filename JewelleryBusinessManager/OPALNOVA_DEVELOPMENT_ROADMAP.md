# OPALNOVA Development Roadmap

## Current Baseline

Current workspace baseline: V1.80.0.

V1.48.2 completed the workspace stability pass for Project Workbench counts, hosted-tab initialization, tab close behavior, workspace fill, and Project Workbench dark control consistency. V1.48.3 removed the unsafe Nivoda development credential helper and aligned the roadmap to the actual app state. V1.49.0 started the quote workspace polish pass with clearer quote status, option comparison, and next-action controls. V1.49.1 adds design-image attachment, preview, and proposal embedding for quote options. V1.50.0 adds the proposal send/record workflow, proposal tracking fields, email draft preparation, sent-proposal follow-up creation, and a cleaner proposal HTML layout. V1.51.0 adds a shared runtime next-action service, a workspace-hosted Alert Centre, dashboard alert count, and setup-readiness guidance. V1.52.0 adds an explicit job completion checklist that consumes reserved materials, marks reserved stones as set, records material movement audit entries, and releases unconsumed reservations through a reviewable workflow. V1.53.0 adds received external diamond conversion into owned loose-stone inventory without schema changes. V1.54.0 adds a single Excel-compatible BI workbook export for stable reporting datasets. V1.55.0 adds dashboard backup health, restore preview, and release notes access. V1.56.0 adds customer timeline reporting and stronger customer summary cards. V1.57.0 refreshes invoice and receipt output for clearer handover paperwork. V1.58.0 adds stock ageing and slow-moving inventory reporting. V1.59.0 adds profitability reporting by product/service category and job type. V1.60.0 adds tax/GST summary reporting for bookkeeping review. V1.61.0 adds printable visual report charts for core business snapshots. V1.62.0 refreshes the built-in user guide into a practical manual. V1.63.0 standardizes generated document headings, support copy, and release metadata to avoid fragile encoding display in exported text and HTML. V1.64.0 adds a live Proposal Pipeline for proposal follow-up and quote reopening. V1.65.0 adds quote context fields and required-by next-action guidance. V1.66.0 adds quote workspace unsaved-change protection. V1.67.0 adds customer preference fill for quote ring size, metal and stone context. V1.68.0 adds balance reminder messages and follow-up creation in Payment & Collection. V1.69.0 adds shared duplicate-safe reminder task checks across active quote, proposal, project, payment and supplier-diamond workflows. V1.70.0 adds a customer/job handover confirmation document for collection and shipping workflows. V1.71.0 adds final thank-you follow-up task creation from Payment & Collection. V1.72.0 adds read-only in-editor job payment history with totals, balance, customer context and linked payment ledger rows. V1.73.0 broadens unsaved-change protection to hosted generic record editor tabs. V1.74.0 adds a live Payment & Collection handover checklist and carries its summary into handover notes. V1.75.0 adds customer-specific communication templates, lifetime value guidance and repeat follow-up suggestions. V1.76.0 adds a production stage checklist with waiting warnings, linked reservations, supplier diamond state, payments, tasks and job photo/file context. V1.77.0 adds dashboard Recent Work recall for workflow tabs, generated reports and saved record editors. V1.78.0 adds shared payment schedule guidance across quotes, proposals, job payment summaries and Payment & Collection. V1.79.0 adds stock lifecycle guidance across inventory status changes, supplier diamonds and inventory reports. V1.80.0 records a stability and redundancy checkpoint across the V1.76-V1.79 workflow additions.

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
- Add option comparison, payment schedule display, and accepted-option clarity. Payment schedule display implemented in V1.78.0 through shared quote/job schedule guidance.
- Show linked external diamond certificate/video fields where available.
- Add proposal revision/version metadata before adding full quote revision history.

### V1.51 - Universal Next Action and Alert Centre

Goal: turn OPALNOVA into a guided daily work system rather than only a record system.

- Extract Project Workbench action rules into a reusable next-action service.
- Add one alert centre for overdue jobs, unpaid balances, expiring diamond holds, low stock, quote expiry, and follow-ups. Done in V1.51.0 as a first pass.
- Add dashboard and workspace entry points to the same alert data. Done in V1.51.0.
- Add guided setup readiness on the dashboard using existing settings/data. Done in V1.51.0.
- Add recently opened records/tabs after the alert centre is stable. Done in V1.77.0 as a dashboard Recent Work panel for current-session workflow tabs, reports and saved record editors.
- Add unsaved-change warnings for hosted editors after the tab lifecycle remains stable. Started in V1.66.0 for quote tabs and broadened in V1.73.0 for hosted generic record editor tabs.

### V1.52 - Inventory Consumption and Job Completion

Goal: close the loop from quote reservation to finished job without losing stock traceability.

- Add a job-completion checklist. Done in V1.52.0 as a first pass.
- Add a reservation release/consume wizard. Done in V1.52.0 for accepted quote reservations linked to jobs.
- Consume reserved materials/stones only through explicit completion actions. Done in V1.52.0.
- Keep owned stock, reserved stock, supplier stock, sold stock, and consumed stock visually distinct. Started in V1.52.0 through consumed/released reservation states and strengthened in V1.79.0 through shared lifecycle guidance in status changes, supplier diamonds and reports.
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

- Add visual charts for sales, quote conversion, inventory value, and cashflow. Done in V1.61.0 as a printable HTML/CSS chart report.
- Add stock ageing and slow-moving inventory reports. Done in V1.58.0 as a read-only report for unsold jewellery and loose stones.
- Add profit by job type and product category. Done in V1.59.0 as a read-only profitability report with data-quality checks.
- Add tax/GST summary. Done in V1.60.0 as a read-only tax/payment summary using existing sales, payments and settings.
- Add Excel export after report datasets are stable. Done in V1.54.0 as an Excel-compatible workbook export.

### V1.55 - Release Readiness and Data Safety

Goal: prepare OPALNOVA for regular real-world use.

- Add automated backup schedule.
- Add dashboard backup health indicator. Done in V1.55.0.
- Add restore preview before applying a restore. Done in V1.55.0.
- Add full business archive export.
- Add production/staging configuration separation.
- Add release notes viewer inside the app. Done in V1.55.0.
- Add installer, desktop shortcut creation, and user guide. User guide refresh done in V1.62.0; installer and shortcut remain later release work.

### V1.56 - Customer Timeline and Profile Polish

Goal: make customer context faster to review before follow-ups, quotes, handovers, and repeat work.

- Add customer profile summary dashboard. Started in V1.56.0 through improved summary cards.
- Add customer timeline: quotes, jobs, sales, payments, messages, follow-ups. Done in V1.56.0 for quotes, proposal events, jobs, sales, payments and tasks.
- Add preferred ring size / metal / stone display in quote workflow. Started in V1.67.0 with explicit customer preference fill into quote context fields.
- Add customer communication templates. Done in V1.75.0 through Customer Relationship Studio customer-specific message starter output.
- Add customer lifetime value and repeat-customer follow-up suggestions. Started in V1.75.0 through summary/timeline/report value guidance and follow-up task notes.
- Add production stage checklists, waiting flags and job files. Started in V1.76.0 through a generated Production Stage Checklist and production-board/studio entry points.

### V1.57 - Invoice and Receipt Polish

Goal: make customer handover documents clearer and more polished without changing payment logic.

- Add more polished invoice/receipt templates. Done in V1.57.0 for job invoices/receipts, sale receipts, deposit receipts and payment receipts.
- Add partial-payment history view inside each job. Done in V1.72.0 through a saved-job payment history panel in the generic job editor.
- Add balance reminder messages. Done in V1.68.0 through Payment & Collection reminder copy and follow-up creation.
- Make reminder task creation duplicate-safe across active workflow screens. Done in V1.69.0 through shared open-task detection and consistent task-code generation.
- Add shipping/collection confirmation document. Done in V1.70.0 through Payment & Collection handover confirmation output.
- Add handover checklist. Done in V1.74.0 through a live Payment & Collection checklist whose summary feeds handover notes and documents.
- Add final customer thank-you/follow-up task. Done in V1.71.0 through duplicate-safe Payment & Collection thank-you follow-up creation.

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
