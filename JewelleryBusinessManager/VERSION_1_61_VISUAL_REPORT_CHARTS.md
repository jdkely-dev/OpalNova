# OPALNOVA V1.61.0 - Visual Report Charts

## Build Scope

V1.61.0 adds a printable visual chart report for core business snapshots using existing OPALNOVA data.

## Changes

- Added `DocumentExportService.CreateVisualReportCharts()`.
- Added Visual Charts actions in quick Reports and Reports Studio.
- Added printable HTML/CSS bar charts for:
  - sales by month.
  - profit by month.
  - quote conversion by month.
  - inventory value snapshot.
  - payments received by month.
  - outstanding balances by job status.
- Added shared chart CSS to the generated document output.
- Added Visual Charts help text in Reports Studio.
- Updated visible version text, release notes, About text, roadmap and handoff notes.

## Data Safety

- No database schema changes.
- No sale, quote, payment, job, inventory or settings records are modified.
- Charts are generated from existing local records only.
- No internet access or external charting library is required.

## Interpretation Notes

- Charts are snapshots and should be used with the detailed reports before making financial decisions.
- Small or empty datasets may produce flat charts.
- Profit charts clamp negative bar width to zero while preserving the displayed money value.
- Quote conversion uses accepted option, linked job, accepted status or converted status.
