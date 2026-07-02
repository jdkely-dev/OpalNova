# OPALNOVA V2.4.0 - Portable Build Manifest

V2.4.0 adds a read-only portable build manifest so the current published app folder can be reviewed before handoff or installer packaging.

## Implemented

- Bumped visible/project version metadata to 2.4.0.
- Added `DataSafetyService.CreatePortableBuildManifestReport()`.
- Added Portable Build Manifest actions in Settings & Backup and Safety & Data Studio.
- Added action-specific mini-guide metadata for Portable Build Manifest.
- Added Portable build manifest to Search All workflow action discovery.
- The report summarizes executable version/hash, publish-folder signal, app-folder file counts/size, top-level file inventory, local data boundaries, private-data exclusion checks, support-path context and handoff notes.
- Updated Installer Validation Checklist expected-version guidance to the V2.4 baseline.
- Updated built-in release notes, user guide version text, roadmap, forward plan, future plan, handoff notes and testing checklist for the V2.4 baseline.
- Preserved database schema and existing quote, production, payment, inventory, supplier diamond, Nivoda staging, backup, restore, support snapshot, release readiness, decision review, installer/update readiness and installer validation behavior.

## Validation

- Debug build succeeded with zero errors.
- Static checks confirmed all XAML ComboBox declarations under `Views` include friendly `Tag` prompt text.
- Static checks confirmed `SectionHelpGuides` and `HelpGuides` do not contain duplicate keys.
- Static checks confirmed no duplicate tool-action titles within the same tool section.
- Release publish succeeded for `win-x64` self-contained output.
- Published `OPALNOVA.exe` file/version check confirmed `FileVersion` `2.4.0.0` and product version `2.4.0 OPALNOVA Portable Build Manifest`.
- Published `OPALNOVA.exe` launched and closed cleanly.
- The build and publish surfaced the known NU1900 warning because the sandbox could not reach NuGet vulnerability data at `https://api.nuget.org/v3/index.json`.

## Notes

- This pass does not create an installer, create shortcuts, move local data, add auto-update behavior, add background scheduling, create task records or change the database schema.
- The manifest is a report only; it is not a backup, package, installer or updater.
- V2.4 remains uncommitted unless explicitly requested.
