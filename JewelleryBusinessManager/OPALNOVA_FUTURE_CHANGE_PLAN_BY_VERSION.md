# OPALNOVA One-Time Future Change Plan By Expected Version

Snapshot created: 2026-06-30
Current local baseline: V1.91.0 Nivoda Staging Readiness

This is a one-time planning snapshot, not a locked contract. Versions can move if a change proves easier, riskier, or more useful than expected. The current working rule is to commit and push only at whole-number milestone versions such as V1.90 and V2.0 unless explicitly requested.

## Planning Principles

- Stabilise existing daily workflows before adding large new systems.
- Prefer shared services, global styles, and reusable workflow helpers over one-screen patches.
- Keep database changes additive and avoid schema changes unless the workflow clearly needs them.
- Keep external credentials user-entered only.
- Keep OPALNOVA focused on practical jewellery business work: quote, proposal, stock, production, payment, handover, customer follow-up, and reporting.

## V1.81 - Market / POS Speed Polish

Expected focus:

- Improve the fast market-sale path for quick selling.
- Make scan/search-to-sale behaviour clearer where existing barcode and lookup fields already support it.
- Exclude returned stock from active market-sale choices.
- Use shared market-sale logic so sales update reconciliation totals consistently.
- Add clearer end-of-day reconciliation guidance showing recorded stock sales, cash/card/other totals, and any difference.
- Add safer return-to-stock handling after market events.
- Keep this no-schema and focused on workflow speed and correctness.

Why here:

- Market/POS already exists, so this is a contained usability and consistency pass.
- It strengthens sales and stock behaviour before heavier release-readiness work.

## V1.82 - Inventory Media And Batch Workflow

Expected focus:

- Add batch photo import support for stock items where existing photo storage can be reused. Started in V1.82.0 through multi-select record detail photo import.
- Add clearer certificate and provenance attachment paths for opals, gemstones, diamonds, and finished stock.
- Improve stock photo/status visibility in inventory detail screens and generated documents.
- Add practical checks so missing image files do not break reports, proposals, or inventory exports.
- Improve stock item lifecycle display where media, certificates, provenance, and sale/consumption status intersect.

Why here:

- The app already has photo storage and option image support.
- This improves daily stock presentation without requiring large new business logic.

## V1.83 - Supplier Diamond Refresh And Replacement Support

Expected focus:

- Add refresh availability/price for saved external supplier diamonds where API behaviour is reliable.
- Add warnings when a linked supplier diamond appears unavailable.
- Improve hold-expiry and expected-arrival reminders.
- Add replacement suggestion workflow using saved supplier diamond details or previous search context where possible. Started in V1.83.0 with replacement-search copy and close saved alternatives.
- Keep Nivoda and supplier credentials user-entered only.
- Defer API-level hold/order actions until the accessible supplier schema is confirmed.

Why here:

- External diamond quote, hold, order, receipt, and owned-inventory conversion foundations already exist.
- Refresh/replacement support reduces quote risk without forcing full supplier automation.

## V1.84 - Production Time, Capacity, And Scheduling

Expected focus:

- Add lightweight production time tracking for jobs or production stages. Started in V1.84.0 with a no-schema capacity snapshot from existing labour-hour estimates.
- Add due-date rescheduling guidance.
- Make waiting-on-customer, waiting-on-supplier, and waiting-on-stone states easier to see.
- Improve production stage checklist follow-through.
- Add a simple capacity/calendar view only after the time and stage data are useful enough.

Why here:

- V1.52 and V1.76 added job completion and production checklist foundations.
- Time/capacity work should follow those foundations rather than inventing a separate scheduling model.

## V1.85 - Proposal PDF And Revision Polish

Expected focus:

- Add PDF-ready proposal export once the HTML proposal output remains stable. Started in V1.85.0 with print-ready HTML and browser Save as PDF guidance.
- Add quote/proposal revision or snapshot history. Started in V1.85.0 with revisioned proposal filenames and visible revision labels.
- Improve quote-option comparison in customer-facing output.
- Add clearer manual approval, decline, and acceptance recording.
- Add stronger certificate/image/video presentation for linked stones and supplier diamonds.
- Keep direct email sending deferred unless the draft/copy/open-mail workflow proves insufficient.

Why here:

- Proposal HTML, proposal tracking, proposal pipeline, payment schedules, and quote context fields are already in place.
- PDF/revision work is higher value after the proposal workflow has had enough stability passes.

## V1.86 - Backup Scheduling And Data Integrity Tools

Expected focus:

- Add automated backup schedule or startup backup reminder, depending on what is safest for the desktop app lifecycle. Deferred after review because true scheduling is OS/app lifecycle work.
- Add backup health warnings when backups are stale.
- Add data integrity checks for orphaned links, missing files, missing proposal outputs, inconsistent stock statuses, and incomplete payment records. Started in V1.86.0 with a read-only Data Integrity report and dashboard/menu/studio entry points.
- Add a full business archive export if current export and backup paths are stable.
- Keep restore preview as a required safety step before applying restores.

Why here:

- Backup health and restore preview already exist.
- Data checks become more valuable as more workflows create linked records and generated files.

## V1.87 - Help, Search, And Guided Workflow

Expected focus:

- Add searchable or indexed in-app help based on the refreshed user guide. Started in V1.87.0 by making Search All surface workflow actions as searchable results.
- Add better empty states for new users without turning the app into a tutorial wall.
- Add global search or command-bar improvements after core workflow surfaces remain stable. Started in V1.87.0 by expanding Advanced Search to Custom Quotes, Quote Options, External Diamonds and Workflow Actions.
- Add keyboard shortcuts for common actions where they are obvious and low-risk.
- Expand setup checklist guidance around business profile, tax, backups, first quote, first stock item, and first sale.

Why here:

- Guided setup already exists as dashboard readiness.
- This pass should make existing functions easier to find before adding more feature depth.

## V1.88 - Practical Jeweller Tools

Expected focus:

- Improve DYMO and barcode label preview/template handling.
- Add camera capture into quotes, jobs, and inventory if local device handling is reliable.
- Add scale capture or manual scale-entry helpers for materials, stones, and jobs.
- Add ring-size conversion, metal weight estimator, stone size/carat estimator, setting-cost calculator, and casting-cost calculator. Started in V1.88.0 with a local Jeweller Tools window for ring-size reference, metal-weight estimates and stone-carat estimates.
- Add polishing/finishing checklist support if it fits the production-stage checklist model.

Why here:

- These tools are valuable, but can sprawl quickly.
- They should follow the core workflow and data-safety improvements.

## V1.89 - Release Readiness And Installer Preparation

Expected focus:

- Prepare installer and packaging notes. Started in V1.89.0 with an in-app Release Readiness report covering publish folder, installer options and validation gates.
- Add desktop shortcut creation if packaging supports it cleanly. Deferred in V1.89.0 because shortcut creation should be owned by the chosen installer.
- Add update/version-check design, even if full auto-update remains deferred. Deferred in V1.89.0 until a trusted update channel is chosen.
- Review production/staging configuration separation. Started in V1.89.0 with explicit staging cautions and a recommendation to use a separate Windows profile, VM or copied database until formal staging config exists.
- Review all generated documents for OPALNOVA branding, current version text, readable encoding, and print layout.
- Confirm release notes and user guide are accessible inside the app. V1.89.0 keeps these entry points and adds Release Readiness beside them.

Why here:

- This is the final preparation pass before the next whole-number milestone.
- It should collect release concerns rather than mixing them into daily workflow features.

## V1.90 - Major Stability Milestone And Git Checkpoint

Expected focus:

- Full redundancy check across V1.81-V1.89. Completed in V1.90.0.
- Confirm no duplicated workflow actions, dashboard cards, report entry points, or confusing repeated panels. Completed in V1.90.0; cross-studio shortcuts remain intentional.
- Confirm database migrations remain additive and existing local data opens cleanly. Completed in V1.90.0; no destructive schema changes were added in V1.81-V1.89.
- Smoke test dashboard, market/POS, quotes, proposals, production, payments, inventory, supplier diamonds, reports, backup, restore preview, release notes, and user guide. Covered by V1.90 checklist and launch smoke; deeper manual workflow click-through remains checklist-driven.
- Run debug build, release publish, and published executable launch smoke. Completed in V1.90.0 before milestone commit.
- Commit and push this milestone if validation passes. Completed after V1.90 validation.

Why here:

- This follows the user's whole-number git rule.
- It creates a stable checkpoint before larger V2.0 decisions.

## V1.91 - Nivoda Staging Readiness And Customer Segment Guidance

Expected focus:

- Generate a non-secret Nivoda staging handoff report with endpoint, GraphiQL, optional external review URL, authentication status and accessible GraphQL schema fields. Started in V1.91.0.
- Add environment and review URL settings to the Nivoda supplier window without hardcoded credentials. Started in V1.91.0.
- Add a ready-to-host static handoff page under `docs/nivoda-staging/` and a manual GitHub Pages workflow for a future externally shareable URL. Started in V1.91.0.
- Keep live Nivoda hold/order API actions gated until Nivoda confirms account-specific mutation names and payloads.
- Improve customer segmentation and reminder guidance using existing customer, sales, quotes, jobs, payments and task data. Started in V1.91.0 as no-schema guidance in existing customer outputs.

Why here:

- The user needs Nivoda staging setup to move forward with API access and review.
- Customer timeline, communication templates, lifetime value guidance, and follow-up task creation already exist, so customer refinement can continue without schema changes.

## V1.92 - Advanced Reports And Scheduled Outputs

Expected focus:

- Add weekly/monthly report generation if report outputs remain stable.
- Add workshop productivity reporting.
- Add supplier diamond performance reporting.
- Add market performance reporting.
- Add customer value reporting improvements.
- Add scheduled report reminders before adding background automation.

Why here:

- Core reporting now includes Excel workbook, stock ageing, profitability, tax/GST, and visual charts.
- Scheduling should only be added after report contents are stable and trusted.

## V1.93 - Inventory Valuation And Reorder Intelligence

Expected focus:

- Add inventory valuation by category.
- Add low-stock reorder recommendations.
- Improve slow-moving stock guidance.
- Add stock adjustment audit review surfaces if existing material transaction data is not enough.
- Improve reserved, owned, supplier, sold, consumed, and archived state reporting.

Why here:

- Stock lifecycle clarity already exists.
- Valuation and reorder recommendations are decision-support features that build on stable stock states.

## V1.94 - External Supplier Ordering Depth

Expected focus:

- Add supplier purchase/order document generation where existing data is sufficient.
- Improve received-diamond intake.
- Improve replacement workflow from supplier diamonds to owned inventory.
- Add currency conversion handling if supplier pricing data makes this necessary.
- Reassess API-level supplier hold/order actions only after schema, authentication, and error cases are confirmed.

Why here:

- This is more dependent on supplier behaviour and should remain behind local workflow safety.

## V1.95 - Payment, Credit, And Lay-By Refinement

Expected focus:

- Add refund and credit-note handling.
- Add lay-by/payment-plan workflow if the shared payment schedule guidance is not enough.
- Improve overdue balance alerting and reminder history.
- Improve receipt/invoice wording for partial payment, refund, credit, and collection states.

Why here:

- Payment reminders, schedules, handover confirmation, and invoices already exist.
- Refund/credit/lay-by work needs careful accounting language and should not be rushed.

## V1.96 - Workspace Efficiency And Keyboard Workflow

Expected focus:

- Add keyboard shortcuts for common actions.
- Improve recently opened item recall beyond current-session history if worthwhile.
- Improve tab recovery and unsaved-change coverage if any high-risk editors remain uncovered.
- Add command/search refinements based on actual navigation friction.

Why here:

- Workspace stability and hosted editor guards already exist.
- This pass should be driven by real repeated-use friction rather than broad UI redesign.

## V1.97 - Hardware And Device Workflow

Expected focus:

- Deepen label, barcode, camera, and scale workflows after the basic tools are reliable.
- Add scan-to-sale improvements if barcode hardware is available.
- Add device setup checks and graceful fallbacks when hardware is unavailable.
- Avoid making any hardware workflow mandatory.

Why here:

- Hardware support is valuable but environment-dependent.
- The safest approach is opt-in tooling with clear fallbacks.

## V1.98 - Installer, Update, And Support Polish

Expected focus:

- Finalise installer behaviour.
- Finalise desktop shortcut behaviour.
- Add update/version-check presentation.
- Add release notes viewer refinements.
- Add final support/help/manual polish for real-world use.

Why here:

- This prepares V2.0 release posture after feature and workflow improvements are stable.

## V1.99 - V2.0 Release Candidate Hardening

Expected focus:

- Full UI clipping and scaling review at 100%, 125%, and 150%.
- Full dark-theme control consistency review.
- Full generated-document review.
- Full data backup, restore, import/export, and integrity review.
- Full manual business-workflow smoke test from customer enquiry to quote, proposal, job, payment, handover, follow-up, and report.

Why here:

- This should be a deliberate release candidate pass, not a feature build.

## V2.0 - Larger Product Systems Decision Point

Expected focus:

- Decide whether OPALNOVA needs multi-user, cloud sync, or shared-device workflows.
- Decide whether direct email delivery is worth adding after the draft-based workflow proves itself.
- Decide whether API-level supplier ordering belongs inside OPALNOVA.
- Decide whether deeper scheduling/calendar/capacity planning should become a major module.
- Decide whether the current workspace navigation is still enough for daily use or needs a larger redesign.

Why here:

- These are product-level decisions with higher risk and larger support burden.
- They should be handled after OPALNOVA's local desktop workflow is stable and well-tested.
