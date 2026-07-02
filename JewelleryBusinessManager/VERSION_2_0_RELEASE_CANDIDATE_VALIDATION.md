# OPALNOVA V2.0.0 - Release Candidate Validation

V2.0.0 completes a release-candidate validation checkpoint across the V1.91-V1.99 working set without schema changes.

## Implemented

- Bumped visible/project version metadata to 2.0.0.
- Ran static checks for selector prompt coverage across XAML ComboBox declarations.
- Ran static checks for duplicate keys in `SectionHelpGuides` and `HelpGuides`.
- Ran static checks for duplicate tool-action titles within each tool section.
- Updated built-in release notes, user guide version text, roadmap, forward plan, future plan and handoff notes for the V2.0 baseline.
- Preserved database schema and existing quote, production, payment, inventory, supplier diamond, Nivoda staging, backup, restore, support snapshot and report behavior.

## Validation

- Debug build succeeded with zero errors.
- Release publish succeeded for `win-x64` self-contained output.
- Published `OPALNOVA.exe` file/version check confirmed `FileVersion` `2.0.0.0` and product version `2.0.0 OPALNOVA Release Candidate Validation`.
- Published `OPALNOVA.exe` launched and closed cleanly.
- The build and publish surfaced NU1900 warnings because the sandbox could not reach NuGet vulnerability data at `https://api.nuget.org/v3/index.json`.

## Notes

- This pass does not add new workflows or data fields.
- V2.0 is an appropriate whole-number git checkpoint when explicitly requested.
- The next planned focus is a post-V2.0 product decision review before taking on larger systems such as installer packaging, update/version channel, direct email delivery, deeper scheduling, multi-user/cloud sync or API-level supplier ordering.
