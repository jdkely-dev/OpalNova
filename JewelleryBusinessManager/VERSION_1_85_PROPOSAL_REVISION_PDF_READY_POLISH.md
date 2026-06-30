# OPALNOVA V1.85.0 - Proposal Revision PDF-Ready Polish

V1.85.0 continues proposal polish without introducing a PDF rendering dependency.

## Changes

- Bumped visible/project version metadata to 1.85.0.
- Updated proposal HTML generation to use revisioned filenames:
  - `QuoteCode_Proposal_v001_yyyyMMdd_HHmmss.html`
  - `QuoteCode_Proposal_v002_yyyyMMdd_HHmmss.html`
  - and so on.
- Added visible proposal revision labels and generated timestamps to the customer-facing proposal header.
- Added print CSS and a `Print / Save as PDF` button to generated proposal HTML.
- Added `Copy PDF Steps` to Send / Record Proposal so browser print-to-PDF instructions can be copied with the generated proposal path.
- Updated release notes, user guide, About text, roadmap, forward plan, one-time future plan and handoff to the V1.85 baseline.

## Data Safety

- No database schema changes were introduced.
- Existing proposal prepared/sent/follow-up fields are preserved.
- Existing proposal email draft and sent-record workflows are preserved.
- True native one-click PDF generation remains deferred until a PDF renderer dependency is intentionally chosen.

## Validation

- Debug build succeeds with zero warnings and zero errors.
- Release publish succeeds through `win-x64-self-contained`.
- Published `OPALNOVA.exe` launches and closes cleanly.

Per the milestone-only git rule, this build is not committed or pushed until the next whole-number milestone unless explicitly requested.
