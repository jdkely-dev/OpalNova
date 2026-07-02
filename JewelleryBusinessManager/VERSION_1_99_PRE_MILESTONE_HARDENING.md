# OPALNOVA V1.99.0 - Pre-Milestone Hardening

V1.99.0 completes a small no-schema hardening pass before the V2.0 checkpoint.

## Implemented

- Reviewed the V1.94-V1.98 selector, workflow-surface and support-polish changes for stale version text, duplicated help metadata and action-help routing gaps.
- Corrected Customer Relationship Studio mini-guide routing so Customer Timeline opens its specific action help instead of falling back to the broad section guide.
- Removed duplicated Communication Templates help metadata from the section-guide map while preserving the action-specific help entry.
- Bumped visible/project version metadata to 1.99.0.
- Suppressed the SDK source-revision suffix in informational product version metadata so published ProductVersion matches the clean OPALNOVA release label.
- Updated built-in release notes, user guide version text, roadmap, forward plan, future plan and handoff notes for the V1.99 baseline.
- Preserved database schema and existing quote, production, payment, inventory, supplier diamond, backup, restore, support snapshot and report behavior.

## Validation

- Debug build succeeded with zero errors.
- Release publish succeeded for `win-x64` self-contained output.
- Published `OPALNOVA.exe` file/version check confirmed `FileVersion` `1.99.0.0` and product version `1.99.0 OPALNOVA Pre-Milestone Hardening`.
- Published `OPALNOVA.exe` launched and closed cleanly.
- The build and publish surfaced NU1900 warnings because the sandbox could not reach NuGet vulnerability data at `https://api.nuget.org/v3/index.json`.

## Notes

- This pass does not add new workflows or data fields.
- The next planned focus is V2.0 release-candidate validation across the V1.91-V1.99 working set.
