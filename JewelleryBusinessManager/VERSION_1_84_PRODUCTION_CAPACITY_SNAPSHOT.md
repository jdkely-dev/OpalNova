# OPALNOVA V1.84.0 - Production Capacity Snapshot

V1.84.0 starts the production capacity and scheduling pass without introducing a new time-entry schema.

## Changes

- Bumped visible/project version metadata to 1.84.0.
- Added `DocumentExportService.CreateProductionCapacityReport()`.
- Added a no-schema Production Capacity Snapshot report using existing:
  - active jobs.
  - job due dates.
  - recorded job labour hours.
  - job balances.
  - active production batches.
- Added due-date buckets for overdue, due within 7 days, due 8-14 days and no due date.
- Added guidance for missing labour-hour estimates and jobs exceeding a conservative weekly planning benchmark.
- Added Capacity Snapshot actions in:
  - Production Board.
  - Production workflow home.
  - Production & Opal Studio.
  - Reports Studio.
- Updated release notes, user guide, About text, roadmap, forward plan, one-time future plan and handoff to the V1.84 baseline.

## Data Safety

- No database schema changes were introduced.
- Existing Production Board movement, Stage Checklist and Job Completion workflows are preserved.
- The report is read-only and does not change jobs, batches, payments or stock.

## Validation

- Debug build succeeds with zero warnings and zero errors.
- Release publish succeeds through `win-x64-self-contained`.
- Published `OPALNOVA.exe` launches and closes cleanly.

Per the milestone-only git rule, this build is not committed or pushed until the next whole-number milestone unless explicitly requested.
