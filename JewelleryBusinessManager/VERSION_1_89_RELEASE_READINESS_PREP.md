# OPALNOVA V1.89.0 Release Readiness Prep

V1.89.0 prepares the release-readiness surface before the V1.90 milestone checkpoint.

## Implemented

- Bumped visible/project version metadata to `1.89.0`.
- Added `DataSafetyService.CreateReleaseReadinessReport()`.
- Added Release Readiness actions in:
  - Settings & Backup.
  - Safety & Data Studio.
  - Search All workflow actions.
- The generated report covers:
  - Runtime executable and app folder.
  - Database, photo, backup and printout paths.
  - Release validation gates.
  - Packaging notes for the full publish folder.
  - Installer and desktop shortcut decision notes.
  - Production/staging cautions.
  - Update/version-channel deferral.
  - Generated document review checklist.
- Preserved database schema and existing backup, restore, health check, data integrity, release notes and user guide behavior.

## Deferred

- Installer creation remains a packaging decision.
- Desktop shortcut creation remains installer-owned.
- Auto-update/version-channel behavior remains deferred until a trusted distribution path is chosen.
- Formal production/staging configuration remains deferred.

