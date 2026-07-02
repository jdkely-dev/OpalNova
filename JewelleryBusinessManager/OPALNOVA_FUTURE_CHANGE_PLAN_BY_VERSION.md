# OPALNOVA One-Time Future Change Plan By Expected Version

Snapshot created: 2026-06-30
Current local baseline: V2.6.0 Roadmap Completion Record

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

- Add weekly/monthly report generation if report outputs remain stable. Started in V1.92.0 through report cadence guidance rather than background automation.
- Add workshop productivity reporting. Started in V1.92.0 through Operations Performance.
- Add supplier diamond performance reporting. Started in V1.92.0 through Operations Performance.
- Add market performance reporting. Existing Market Performance remains, and V1.92.0 adds market operations summary into Operations Performance.
- Add customer value reporting improvements.
- Add scheduled report reminders before adding background automation. Started in V1.92.0 as advisory weekly/monthly report rhythm.

Why here:

- Core reporting now includes Excel workbook, stock ageing, profitability, tax/GST, and visual charts.
- Scheduling should only be added after report contents are stable and trusted.

## V1.93 - Inventory Valuation And Reorder Intelligence

Expected focus:

- Add inventory valuation by category. Started in V1.93.0 through Inventory Intelligence.
- Add low-stock reorder recommendations. Started in V1.93.0 with incoming purchase-order coverage.
- Improve slow-moving stock guidance. Started in V1.93.0 with action guidance for older stock and stones.
- Add stock adjustment audit review surfaces if existing material transaction data is not enough. Started in V1.93.0 using existing material transactions.
- Improve reserved, owned, supplier, sold, consumed, and archived state reporting. Started in V1.93.0 by combining lifecycle guidance with supplier diamond state and stock value.

Why here:

- Stock lifecycle clarity already exists.
- Valuation and reorder recommendations are decision-support features that build on stable stock states.

## V1.94 - Shared Selector Theme Polish

Actual focus:

- Added shared ComboBox empty prompts so unselected fields show friendly muted guidance instead of blank faces.
- Added dark DatePicker text box, calendar day and calendar month/year button styling.
- Routed Payment & Collection and Production Board selector/date styles through the global OPALNOVA theme.
- Kept the pass no-schema and preserved existing selector, payment, production, inventory and supplier diamond behavior.
- Deferred deeper external supplier ordering until schema, authentication and account-specific error cases are confirmed.

Why here:

- The active handoff prioritized UI/workflow streamlining and shared style fixes over broad new feature areas.
- Supplier ordering depth remains more dependent on supplier behaviour and should stay behind local workflow safety.

## V1.95 - Workflow Control Consolidation

Actual focus:

- Routed remaining high-priority workflow Button and TextBox styles through shared OPALNOVA templates.
- Added explicit prompt text to every XAML ComboBox declaration.
- Preserved local colours/spacing and existing workflow behavior.
- Deferred payment, credit and lay-by refinement until accounting language and workflow scope are reviewed separately.

Why here:

- The active handoff prioritized UI/workflow streamlining and shared style fixes before broad new feature areas.
- Payment reminders, schedules, handover confirmation and invoices already exist, but refund/credit/lay-by work needs careful accounting language and should not be rushed.

## V1.96 - Workspace Surface Reduction

Actual focus:

- Compressed high-use workflow headers, metric cards and selected-detail panels.
- Reduced redundant explanatory copy in Alert Centre, Project Workbench, Proposal Pipeline, Payment & Collection, Production Board, Supplier Diamond Holds & Orders and Stock Movement.
- Preserved all action buttons, selectors, data grids, payment controls, supplier diamond actions and stock movement inputs.
- Kept the pass no-schema and focused on making workspace content fill the tab area.

Why here:

- The active handoff prioritized reducing redundant panels and letting workspace content fill the tab area.
- Workspace stability and hosted editor guards already exist, so surface reduction can be done without changing data behavior.

## V1.97 - Daily Workflow Edge Polish

Actual focus:

- Added an `Open Payments` action to Production Board for the selected job card.
- Added focused Payment & Collection opening for a specific job id.
- Payment & Collection switches to `All jobs` when the selected production job is hidden by the default handover filter.
- Job-specific payment handoffs open in contextual workspace tabs.
- Cleaned visible Payment & Collection list separators to plain ASCII.
- Preserved database schema, payment recording, sale creation, handover actions and production movement behavior.

Why here:

- The main screens are now more compact and consistent.
- The next value is reducing repeated-use friction without widening scope.

## V1.98 - Support Snapshot Polish

Actual focus:

- Added a read-only Support Snapshot report.
- Added Support Snapshot entry points in Settings & Backup and Safety & Data Studio.
- Included installed version, executable path, app folder, database path, backup folder, printout folder, photo folder, settings path, saved-view path, error-log path and latest backup status.
- Added support guidance for what to share and what not to share publicly.
- Updated release notes, user guide and workflow help metadata.
- Preserved backup, restore, health check, data integrity, release readiness, user guide and release notes behavior.

Why here:

- This prepares V2.0 release posture after feature and workflow improvements are stable.

## V1.99 - V2.0 Release Candidate Hardening

Implemented in V1.99.0:

- Reviewed V1.94-V1.98 UI/workflow/support changes for a small no-schema hardening pass.
- Corrected Customer Relationship Studio action help routing so Customer Timeline opens its specific mini guide.
- Removed duplicated Communication Templates help metadata from the section guide map while preserving action-specific help.
- Refreshed visible/project version metadata, release notes, roadmap and handoff notes for the V1.99 baseline.

Manual release-candidate checks still useful before external distribution:

- Full UI clipping and scaling review at 100%, 125%, and 150%.
- Full dark-theme control consistency review.
- Full generated-document review.
- Full data backup, restore, import/export, and integrity review.
- Full manual business-workflow smoke test from customer enquiry to quote, proposal, job, payment, handover, follow-up, and report.

Why here:

- This should be a deliberate release candidate pass, not a feature build.

## V2.0 - Release Candidate Validation And Product Decision Point

Implemented in V2.0.0:

- Completed a release-candidate validation checkpoint across the V1.91-V1.99 working set.
- Confirmed selector prompt coverage, help-guide key uniqueness and per-section tool-action title uniqueness with static checks.
- Refreshed visible/project version metadata, release notes, user guide version text, roadmap, forward plan, handoff, version report and testing checklist for the V2.0 baseline.
- Preserved database schema and existing quote, production, payment, inventory, supplier diamond, Nivoda staging, backup, restore, support snapshot and report behavior.

Post-V2.0 decision focus:

- Decide whether OPALNOVA needs multi-user, cloud sync, or shared-device workflows.
- Decide whether direct email delivery is worth adding after the draft-based workflow proves itself.
- Decide whether API-level supplier ordering belongs inside OPALNOVA.
- Decide whether deeper scheduling/calendar/capacity planning should become a major module.
- Decide whether the current workspace navigation is still enough for daily use or needs a larger redesign.

Why here:

- These are product-level decisions with higher risk and larger support burden.
- They should be handled after OPALNOVA's local desktop workflow is stable and well-tested.

## V2.1 - Post-V2 Decision Review

Implemented in V2.1.0:

- Added a read-only Post-V2 Decision Review report.
- Added Decision Review entry points in Settings & Backup and Safety & Data Studio.
- Summarized local workflow footprint, operations load, stock/supplier context and Nivoda readiness.
- Added decision guidance for multi-user/cloud sync, direct email delivery, API-level supplier ordering, deeper scheduling/capacity planning, workspace navigation redesign and installer/update direction.
- Kept the pass no-schema and did not add email sending, supplier mutations, cloud sync, background scheduling or installer behavior.

Next decision:

- Installer/update readiness has been chosen as the first concrete post-V2 direction.

Why here:

- The V2.0 release candidate needs product-level choices before larger support-heavy systems are started.
- A read-only decision surface keeps the current desktop workflow stable while making the next investment explicit.

## V2.2 - Installer Update Readiness

Implemented in V2.2.0:

- Chose installer/update readiness as the first concrete post-V2 direction.
- Added a read-only Installer/Update Readiness report.
- Added Installer/Update Readiness entry points in Settings & Backup and Safety & Data Studio.
- Added Search All workflow discovery for installer/update readiness.
- Summarized runtime executable/app folder, publish-folder signal, database/settings/backup/printout paths, installer choices, update-channel boundaries, portable build handoff steps and distribution cautions.
- Kept the pass no-schema and did not add installer creation, shortcut creation, auto-update behavior, background scheduling or data-location changes.

Next decision:

- Choose whether the first packaging test should stay as a portable publish-folder handoff or move to MSIX/Inno Setup.

Why here:

- Installer and update behavior affects support, data safety and rollback expectations.
- A read-only readiness pass lets packaging decisions be made deliberately before installer assets or update logic are added.

## V2.3 - Installer Validation Checklist

Implemented in V2.3.0:

- Chose the portable publish folder as the first installer/update validation route.
- Added a read-only Installer Validation Checklist report.
- Added Installer Validation Checklist entry points in Settings & Backup and Safety & Data Studio.
- Added Search All workflow discovery for installer validation.
- Summarized executable fingerprint, publish-folder signal, database/settings/backup/printout paths, update rehearsal gates, rollback checks, installer technology gates and hold conditions.
- Kept the pass no-schema and did not create installer files, shortcuts, update feeds, task records, background jobs, data moves or schema changes.

Next decision:

- Run the V2.3 checklist against the published build before choosing MSIX, Inno Setup or continued portable handoff.

Why here:

- V2.2 identified the installer/update boundary; V2.3 turns that boundary into a validation routine that can stop unsafe packaging before it creates support problems.

## V2.4 - Portable Build Manifest

Implemented in V2.4.0:

- Added a read-only Portable Build Manifest report.
- Added Portable Build Manifest entry points in Settings & Backup and Safety & Data Studio.
- Added Search All workflow discovery for portable build manifest.
- Summarized executable version/hash, publish-folder signal, app-folder file counts/size, top-level file inventory, local data boundaries, private-data exclusion checks, support-path context and handoff notes.
- Updated Installer Validation Checklist expected-version guidance to the V2.4 baseline.
- Kept the pass no-schema and did not create installer files, shortcuts, update feeds, task records, background jobs, data moves or schema changes.

Next decision:

- Run the V2.4 manifest from the published build, then decide whether to continue portable handoff or start a deliberately scoped MSIX/Inno Setup packaging ticket.

Why here:

- The validation checklist says what must pass; the portable manifest records what is actually being handed off so packaging does not start from an unknown folder.

## V2.5 - Packaging Decision Record

Implemented in V2.5.0:

- Added a read-only Packaging Decision Record report.
- Added Packaging Decision Record entry points in Settings & Backup and Safety & Data Studio.
- Added Search All workflow discovery for packaging decision record.
- Recorded portable publish-folder handoff as the validated route.
- Kept MSIX and Inno Setup as explicit future packaging tickets requiring signing, install path, shortcut ownership, update channel, uninstall behavior and rollback rules before implementation.
- Summarized executable evidence, local data boundaries, the release/readiness/validation/manifest/support evidence chain, allowed next actions and non-negotiable packaging boundaries.
- Kept the pass no-schema and did not create installer files, shortcuts, update feeds, task records, background jobs, data moves or schema changes.

Next decision:

- Choose the next major product direction explicitly. Installer readiness is complete for portable handoff; any real installer work should now be a named MSIX or Inno Setup implementation ticket.

Why here:

- V2.2-V2.4 prepared and validated portable handoff. V2.5 prevents another readiness loop by recording the current decision and the conditions required before packaging implementation starts.

## V2.6 - Roadmap Completion Record

Implemented in V2.6.0:

- Added a read-only Roadmap Completion Record report.
- Added Roadmap Completion Record entry points in Settings & Backup and Safety & Data Studio.
- Added Search All workflow discovery for roadmap completion record.
- Recorded the current no-schema version stream as complete.
- Listed completed tracks, remaining explicit major decisions and the stop condition for further readiness-only version passes.
- Kept the pass no-schema and did not create installer files, shortcuts, update feeds, task records, background jobs, data moves, supplier mutations, hardware dependencies or schema changes.

Next decision:

- Choose a new major stream explicitly. The remaining options are MSIX packaging, Inno Setup packaging, true backup scheduling, advanced hardware setup, scheduled reports, deeper calendar/capacity planning, command-palette expansion or API-level Nivoda hold/order after schema confirmation.

Why here:

- The user asked to continue all versions until finished. V2.6 records the finish point for the current version sequence and prevents further automatic version churn without a concrete product decision.
