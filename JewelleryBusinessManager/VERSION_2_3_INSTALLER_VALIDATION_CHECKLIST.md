# OPALNOVA V2.3.0 - Installer Validation Checklist

V2.3.0 turns the V2.2 installer/update readiness direction into a narrow validation checklist without creating installer assets or changing runtime behavior.

## Implemented

- Bumped visible/project version metadata to 2.3.0.
- Added `DataSafetyService.CreateInstallerValidationChecklistReport()`.
- Added Installer Validation Checklist actions in Settings & Backup and Safety & Data Studio.
- Added action-specific mini-guide metadata for Installer Validation Checklist.
- Added Installer validation to Search All workflow action discovery.
- Chose the portable publish folder as the first validation route before MSIX, Inno Setup or another installer technology.
- The report summarizes executable fingerprint, SHA-256, publish-folder signal, database/settings/backup/printout paths, portable validation steps, manual update rehearsal gates, rollback checks, installer technology gates and hold conditions.
- Updated built-in release notes, user guide version text, roadmap, forward plan, future plan, handoff notes and testing checklist for the V2.3 baseline.
- Preserved database schema and existing quote, production, payment, inventory, supplier diamond, Nivoda staging, backup, restore, support snapshot, release readiness, decision review, installer/update readiness and report behavior.

## Validation

- Debug build succeeded with zero errors.
- Static checks confirmed all XAML ComboBox declarations under `Views` include friendly `Tag` prompt text.
- Static checks confirmed `SectionHelpGuides` and `HelpGuides` do not contain duplicate keys.
- Static checks confirmed no duplicate tool-action titles within the same tool section.
- Release publish succeeded for `win-x64` self-contained output.
- Published `OPALNOVA.exe` file/version check confirmed `FileVersion` `2.3.0.0` and product version `2.3.0 OPALNOVA Installer Validation Checklist`.
- Published `OPALNOVA.exe` launched and closed cleanly.
- The build and publish surfaced the known NU1900 warning because the sandbox could not reach NuGet vulnerability data at `https://api.nuget.org/v3/index.json`.

## Notes

- This pass does not create an installer, create shortcuts, move local data, add auto-update behavior, add background scheduling, create task records or change the database schema.
- The portable publish folder remains the first route to validate before installer assets are created.
- V2.3 remains uncommitted unless explicitly requested.
