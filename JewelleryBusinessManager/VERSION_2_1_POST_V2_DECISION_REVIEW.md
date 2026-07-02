# OPALNOVA V2.1.0 - Post-V2 Decision Review

V2.1.0 starts the post-V2 product decision review pass without schema changes.

## Implemented

- Bumped visible/project version metadata to 2.1.0.
- Added `DataSafetyService.CreatePostV2DecisionReviewReport()`.
- Added Decision Review actions in Settings & Backup and Safety & Data Studio.
- Added action-specific mini-guide metadata for the Decision Review action.
- The report summarizes workflow footprint, operations load, stock/supplier context and Nivoda readiness.
- The report gives decision guidance for multi-user/cloud sync, direct email delivery, API-level supplier ordering, deeper scheduling/capacity planning, workspace navigation redesign and installer/update direction.
- Updated built-in release notes, user guide version text, roadmap, forward plan, future plan and handoff notes for the V2.1 baseline.
- Preserved database schema and existing quote, production, payment, inventory, supplier diamond, Nivoda staging, backup, restore, support snapshot and report behavior.

## Validation

- Debug build succeeded with zero errors.
- Static checks confirmed all XAML ComboBox declarations under `Views` include friendly `Tag` prompt text.
- Static checks confirmed `SectionHelpGuides` and `HelpGuides` do not contain duplicate keys.
- Static checks confirmed no duplicate tool-action titles within the same tool section.
- Release publish succeeded for `win-x64` self-contained output.
- Published `OPALNOVA.exe` file/version check confirmed `FileVersion` `2.1.0.0` and product version `2.1.0 OPALNOVA Post-V2 Decision Review`.
- Published `OPALNOVA.exe` launched and closed cleanly.
- The build and publish surfaced the known NU1900 warning because the sandbox could not reach NuGet vulnerability data at `https://api.nuget.org/v3/index.json`.

## Notes

- This pass does not add cloud sync, direct email sending, supplier ordering mutations, background scheduling, installer behavior, update checks or data fields.
- V2.1 remains uncommitted unless explicitly requested.
- The next planned focus is to choose exactly one post-V2 product direction and write a narrow implementation ticket before coding the next broad system.
