# OPALNOVA V1.48.3 Roadmap and Credential Safety Polish

## Scope

V1.48.3 reviewed the planned implementation list against the current V1.48.2 workspace state and made one immediate product-safety adjustment before the next feature build.

## Changes

- Updated project and visible app version metadata to 1.48.3.
- Replaced the Nivoda staging test-login helper with a safer endpoint reset helper.
- Removed hardcoded sample Nivoda username/password fill behavior from the active UI.
- Updated Diamond Supplier copy so credentials are always user-entered.
- Updated active workspace guidance from staging/test wording to supplier API wording.
- Replaced the development roadmap with a tighter build order:
  - V1.49 Quote Workspace Polish
  - V1.50 Premium Proposal Output
  - V1.51 Universal Next Action and Alert Centre
  - V1.52 Inventory Consumption and Job Completion
  - V1.53 External Diamond Production Readiness
  - V1.54 Reports and Export Upgrade
  - V1.55 Release Readiness and Data Safety
- Scrubbed old handoff/checklist wording that told testers to use the removed credential helper.
- Cleaned up the eight pre-existing build warnings:
  - nullable tuple key annotations in duplicate reporting.
  - nullable lookup display labels in the generic editor.
  - validated SQLite identifiers before dynamic additive schema SQL.

## Product Direction Notes

- Stabilising workspace behavior first was correct.
- The next best workflow improvement is quote workspace polish, because quote clarity affects proposals, approvals, payments, production handoff, and inventory reservations.
- Premium proposal output should be built after the quote workspace layout is cleaner.
- Universal next actions should be built as a shared service, not as isolated reminders on separate screens.
- Inventory consumption should stay behind an explicit completion/review step so stock movement remains auditable.
- External diamond production actions should remain credential-safe and schema-aware; API hold/order actions should wait until the accessible supplier schema is confirmed.

## Validation Checklist

- Build succeeds.
- Release publish succeeds.
- Published OPALNOVA executable launches.
- Debug build reports zero warnings and zero errors.
- No active source references remain to the removed staging test-login helper or hardcoded sample credentials.
