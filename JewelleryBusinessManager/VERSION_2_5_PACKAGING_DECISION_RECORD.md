# OPALNOVA V2.5.0 - Packaging Decision Record

V2.5.0 closes the installer/update readiness track with a read-only packaging decision record. Portable publish-folder handoff is now the validated route; MSIX and Inno Setup remain explicit future implementation tickets.

## Implemented

- Bumped visible/project version metadata to 2.5.0.
- Added `DataSafetyService.CreatePackagingDecisionRecordReport()`.
- Added Packaging Decision Record actions in Settings & Backup and Safety & Data Studio.
- Added action-specific mini-guide metadata for Packaging Decision Record.
- Added Packaging decision record to Search All workflow action discovery.
- The report records portable handoff as the validated route and keeps MSIX/Inno Setup behind explicit packaging plans.
- The report summarizes executable evidence, local data boundaries, the release/readiness/validation/manifest/support evidence chain, allowed next actions and non-negotiable packaging boundaries.
- Updated built-in release notes, user guide version text, roadmap, forward plan, future plan, handoff notes and testing checklist for the V2.5 baseline.
- Preserved database schema and existing quote, production, payment, inventory, supplier diamond, Nivoda staging, backup, restore, support snapshot, release readiness, decision review, installer/update readiness, installer validation and portable manifest behavior.

## Validation

- Debug build succeeded with zero errors.
- Static checks confirmed all XAML ComboBox declarations under `Views` include friendly `Tag` prompt text.
- Static checks confirmed `SectionHelpGuides` and `HelpGuides` do not contain duplicate keys.
- Static checks confirmed no duplicate tool-action titles within the same tool section.
- Release publish succeeded for `win-x64` self-contained output.
- Published `OPALNOVA.exe` file/version check confirmed `FileVersion` `2.5.0.0` and product version `2.5.0 OPALNOVA Packaging Decision Record`.
- Published `OPALNOVA.exe` launched and closed cleanly.
- The build and publish surfaced the known NU1900 warning because the sandbox could not reach NuGet vulnerability data at `https://api.nuget.org/v3/index.json`.

## Notes

- This pass does not create an installer, create shortcuts, move local data, add auto-update behavior, add background scheduling, create task records or change the database schema.
- Installer readiness is complete for portable handoff.
- Future installer implementation should start only after choosing MSIX or Inno Setup and writing signing, shortcut, update-channel, uninstall and rollback rules.
- V2.5 remains uncommitted unless explicitly requested.
