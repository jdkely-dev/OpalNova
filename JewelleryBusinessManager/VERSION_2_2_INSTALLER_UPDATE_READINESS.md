# OPALNOVA V2.2.0 - Installer Update Readiness

V2.2.0 chooses installer/update readiness as the first concrete post-V2 direction without schema changes.

## Implemented

- Bumped visible/project version metadata to 2.2.0.
- Added `DataSafetyService.CreateInstallerUpdateReadinessReport()`.
- Added Installer/Update Readiness actions in Settings & Backup and Safety & Data Studio.
- Added action-specific mini-guide metadata for Installer/Update Readiness.
- Added Installer and updates to Search All workflow action discovery.
- The report summarizes runtime executable/app folder, publish-folder signal, database/settings/backup/printout paths, installer decisions, update-channel boundaries, portable build handoff steps and distribution cautions.
- Updated built-in release notes, user guide version text, roadmap, forward plan, future plan and handoff notes for the V2.2 baseline.
- Preserved database schema and existing quote, production, payment, inventory, supplier diamond, Nivoda staging, backup, restore, support snapshot, release readiness, decision review and report behavior.

## Validation

- Debug build succeeded with zero errors.
- Static checks confirmed all XAML ComboBox declarations under `Views` include friendly `Tag` prompt text.
- Static checks confirmed `SectionHelpGuides` and `HelpGuides` do not contain duplicate keys.
- Static checks confirmed no duplicate tool-action titles within the same tool section.
- Release publish succeeded for `win-x64` self-contained output.
- Published `OPALNOVA.exe` file/version check confirmed `FileVersion` `2.2.0.0` and product version `2.2.0 OPALNOVA Installer Update Readiness`.
- Published `OPALNOVA.exe` launched and closed cleanly.
- The build and publish surfaced the known NU1900 warning because the sandbox could not reach NuGet vulnerability data at `https://api.nuget.org/v3/index.json`.

## Notes

- This pass does not create an installer, create shortcuts, move local data, add auto-update behavior, add background scheduling or change the database schema.
- V2.2 remains uncommitted unless explicitly requested.
- The next planned focus is to choose the packaging route to test first: portable publish folder, MSIX or Inno Setup.
