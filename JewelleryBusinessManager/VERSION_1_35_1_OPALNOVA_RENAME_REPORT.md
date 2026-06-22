# OPALNOVA V1.35.1 — Program Name / Branding Rename

## Purpose
Renamed the visible program branding from Jewellery Business Manager to OPALNOVA before standalone release packaging.

## Changes made
- Main window title now shows OPALNOVA.
- Main header now shows OPALNOVA.
- Project assembly name changed to OPALNOVA so the published executable should be OPALNOVA.exe.
- Product metadata changed to OPALNOVA.
- Release publish scripts now say OPALNOVA.
- User-facing reports, guide text, backup/export messages and release notes now use OPALNOVA branding.

## Stability note
The internal C# namespace and project file name were left as JewelleryBusinessManager to reduce risk before release. Existing local data folders were also left unchanged where they are used for storage paths, so upgrading should not hide existing data.

## Validation
- XAML XML parse check passed.
- Project XML parse check passed.
- C# brace balance check passed.
- ZIP package created successfully.

Full Visual Studio build/publish still needs to be run on a Windows machine with the .NET Desktop SDK installed.
