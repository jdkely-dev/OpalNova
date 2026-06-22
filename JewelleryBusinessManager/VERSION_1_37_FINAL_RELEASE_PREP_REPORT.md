# OPALNOVA V1.37 Final Release Prep Report

## Summary

Created the final release-prep package from the working OPALNOVA V1.36.2 build.

## Changes made

- Updated visible title to `OPALNOVA — Final Release Prep`.
- Updated visible version text to `Version 1.37 — Final Release Prep`.
- Updated project version metadata to `1.37.0`.
- Updated customer display default branding from `Jewellery Business` to `OPALNOVA`.
- Added final release guide.
- Added backup/restore guide.
- Added final testing checklist.
- Added V1.37 release notes.
- Refreshed release prep README.

## Stability decisions

- No database schema changes.
- No save/load logic changes.
- No feature workflow changes.
- Internal namespace/project name left unchanged for stability.

## Validation performed

- XAML XML parse check.
- Project XML parse check.
- C# brace balance check.
- ZIP integrity check.

A full Visual Studio/.NET build still needs to be run on the user's machine because the .NET SDK is not installed in this environment.
