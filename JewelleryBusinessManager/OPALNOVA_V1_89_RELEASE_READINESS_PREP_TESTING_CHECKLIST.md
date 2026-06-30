# OPALNOVA V1.89.0 Release Readiness Prep Testing Checklist

Use this checklist against the published V1.89 build.

## Startup

- Launch `OPALNOVA.exe`.
- Confirm the header shows `Version 1.89.0 - release readiness prep`.
- Open About and confirm it shows `Version 1.89.0 - Release Readiness Prep`.

## Entry Points

- Open `Settings & Backup`, then click `Release Readiness`.
- Open `Safety & Data Studio`, then click `Release Readiness`.
- Click `Search All`, search `installer` or `release`, and confirm the Release Readiness workflow action appears.
- Open the workflow action and confirm it navigates to Settings & Backup.

## Report Content

- Confirm the report title is `OPALNOVA Release Readiness`.
- Confirm it shows the installed version.
- Confirm it shows runtime executable/app folder, database path, photo path, backup path and printout path.
- Confirm it includes release gate checklist items for debug build, release publish, launch smoke, backup, Health Check, Data Integrity, Release Notes and User Guide.
- Confirm it includes packaging notes about using the full publish folder.
- Confirm it states installer, desktop shortcut and update/version channel are deferred decisions.
- Confirm it includes staging cautions and Nivoda credential caution.
- Confirm generated document review checks are listed.

## Regression Checks

- Confirm Release Notes still open.
- Confirm User Guide still opens.
- Confirm Health Check still opens.
- Confirm Data Integrity still opens.
- Confirm Create Backup and Restore Preview remain accessible.
- Close the app cleanly.

