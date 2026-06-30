# OPALNOVA V1.82.0 - Inventory Media Batch Workflow

V1.82.0 starts the inventory media and batch workflow pass with a low-risk improvement to the existing record photo workflow.

## Changes

- Bumped visible/project version metadata to 1.82.0.
- Updated the main record detail `+ Photos` action to allow multiple image files to be selected and imported at once.
- Reused existing `PhotoStorageService.CopyPhotoToAppFolder(...)` for each imported file.
- Reused existing `PhotoRecord` links for all batch-imported photos.
- Added clearer UI text and tooltip copy for one-or-more photo attachment.
- Added batch captions that identify import order and linked record type/id.
- Updated release notes, user guide, About text, roadmap, forward plan, one-time future plan and handoff to the V1.82 baseline.

## Data Safety

- No database schema changes were introduced.
- Existing single-photo import behaviour is preserved.
- Existing photo preview, photo records, backup/export photo inclusion and health-check missing-file checks continue to use the same stored paths.

## Validation

- Debug build succeeds with zero warnings and zero errors.
- Release publish succeeds through `win-x64-self-contained`.
- Published `OPALNOVA.exe` launches and closes cleanly.

Per the milestone-only git rule, this build is not committed or pushed until the next whole-number milestone unless explicitly requested.
