# OPALNOVA Future Change Plan By Expected Version

Snapshot created during the V1.78 development pass.

This is a planning snapshot, not a locked contract. Versions can move if a change proves easier, riskier, or more useful than expected. Git commit/push should happen only at milestone versions such as V1.80, V1.90, and V2.0 unless explicitly requested.

## V1.78 - Payment Schedule Guidance

- Add shared payment schedule calculation for quotes and jobs.
- Show deposit, final balance, paid amount, remaining amount, due timing, and guidance in Payment & Collection.
- Use quote deposit percentage when a job is linked to a quote.
- Add staged payment rows to proposal output.
- Add staged payment rows to job payment summary exports.
- Keep this no-schema and based on existing quote, job, and payment records.

## V1.79 - Stock Lifecycle Clarity

- Make stock lifecycle states easier to understand across inventory, reservations, sales, and completed jobs.
- Clarify owned stock, reserved stock, supplier stock, sold stock, consumed stock, and archived stock.
- Add clearer guidance text and filters where existing stock screens are ambiguous.
- Review quote reservation, job completion, and sales transitions for confusing labels.
- Avoid new stock tables unless a real workflow gap appears.

## V1.80 - Stability Milestone And Git Checkpoint

- Full redundancy check across V1.76, V1.77, V1.78, and V1.79 work.
- Confirm no duplicated workflow actions, document actions, dashboard cards, or report entry points.
- Confirm no new UI sections repeat information already shown elsewhere.
- Run manual smoke checks for dashboard, quote, proposal, production, payment, inventory, reports, backup, and release notes.
- Run debug build, release publish, and published executable launch smoke.
- Commit and push this milestone if validation passes.

## V1.81 - Market / POS Speed Polish

- Improve the fast market-sale path for quick selling.
- Add clearer scan/search-to-sale behavior if existing barcode fields support it.
- Add an end-of-day market reconciliation report or checklist.
- Add return-to-stock guidance after market events.
- Keep offline market readiness as a practical checklist before adding complex sync logic.

## V1.82 - Inventory Media And Batch Workflow

- Add batch photo import support for stock items where existing photo storage can be reused.
- Add clearer certificate/provenance attachment paths for opals, gemstones, and finished stock.
- Improve inventory photo/status visibility in stock detail and generated documents.
- Add practical import checks so missing image files do not break reports.

## V1.83 - Supplier Diamond Refresh And Replacement Support

- Add refresh availability/price for saved external supplier diamonds where API behavior is reliable.
- Add unavailable supplier diamond warning guidance.
- Add replacement suggestion workflow from saved supplier search criteria if enough data exists.
- Improve hold-expiry and expected-arrival reminders.
- Keep real credentials user-entered only.

## V1.84 - Production Time, Capacity, And Scheduling

- Add lightweight production time tracking on jobs or production stages.
- Add due-date rescheduling guidance and warnings.
- Add waiting-on-customer, waiting-on-supplier, and waiting-on-stone visibility if not already clear.
- Add a simple capacity/calendar view only after stage and time data are usable.

## V1.85 - Proposal PDF And Revision Polish

- Add PDF-ready proposal export after HTML proposal output remains stable.
- Add quote/proposal revision history or snapshot naming.
- Improve proposal option comparison and acceptance recording.
- Add clearer customer approval/decline tracking.
- Keep email delivery as draft/copy/open-mail workflow unless direct sending becomes necessary.

## V1.86 - Backup Scheduling And Data Integrity Tools

- Add automated backup schedule or startup reminder, depending on what is safest for the desktop app lifecycle.
- Add backup health warnings when backups are stale.
- Add data integrity check tool for common orphaned links and missing files.
- Add full business archive export if the current export/backup paths are stable.

## V1.87 - Help, Search, And Guided Workflow

- Add a searchable or indexed in-app help guide.
- Add more guided empty states for new users.
- Add global command/search improvements only after the main workflows are stable.
- Expand setup checklist guidance without turning the dashboard into a tutorial wall.

## V1.88 - Practical Jeweller Tools

- Improve DYMO/barcode label preview and template handling.
- Add camera capture into quotes, jobs, and inventory if the local device path is reliable.
- Add scale capture or manual scale-entry helpers for materials and stones.
- Add ring-size conversion, metal weight estimator, stone size/carat estimator, setting cost, and casting cost tools.

## V1.89 - Release Readiness And Installer Preparation

- Add installer preparation notes and final release checks.
- Add desktop shortcut creation if packaging supports it cleanly.
- Add update/version check design, even if full auto-update is deferred.
- Review production/staging configuration separation.
- Confirm all generated documents show OPALNOVA branding and current version text.

## V1.90 - Major Stability Milestone And Git Checkpoint

- Full regression pass across all daily workflows.
- Confirm database migrations remain additive and existing local data opens cleanly.
- Confirm printouts, reports, proposals, payment documents, backups, and restore preview still work.
- Publish a stable release build.
- Commit and push this milestone if validation passes.

## V2.0 - Larger Product Systems

- Decide whether OPALNOVA needs deeper multi-user, cloud, sync, scheduling, or external integrations.
- Consider API-level Nivoda hold/order actions only after schema and credentials are confirmed.
- Consider direct email delivery only after draft-based proposal workflow proves stable.
- Consider advanced hardware workflows after label/camera/scale basics are proven.
- Consider larger UI navigation redesign only if the current workspace model starts limiting daily work.
