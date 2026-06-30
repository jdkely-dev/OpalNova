# OPALNOVA V1.83.0 - Supplier Diamond Replacement Readiness

V1.83.0 continues supplier diamond workflow readiness with local replacement support that does not require live API mutation or hardcoded credentials.

## Changes

- Bumped visible/project version metadata to 1.83.0.
- Added `Copy Replacement Search` to Supplier Diamond Holds & Orders.
- Replacement search copy includes:
  - selected supplier diamond summary.
  - lab-grown/natural type.
  - shape.
  - carat range around the selected diamond.
  - colour/clarity target.
  - lab target.
  - original certificate.
  - linked quote/customer context.
  - up to five close saved alternatives already in OPALNOVA.
- Close alternatives use same diamond type, same shape and nearby carat range, ordered by carat closeness then supplier price.
- Updated release notes, user guide, About text, roadmap, forward plan, one-time future plan and handoff to the V1.83 baseline.

## Data Safety

- No database schema changes were introduced.
- Existing external diamond save, quote-link, hold, order, receipt and conversion workflows are preserved.
- Live supplier availability/price refresh remains deferred until credentials and accessible schema behaviour are confirmed.

## Validation

- Debug build succeeds with zero warnings and zero errors.
- Release publish succeeds through `win-x64-self-contained`.
- Published `OPALNOVA.exe` launches and closes cleanly.

Per the milestone-only git rule, this build is not committed or pushed until the next whole-number milestone unless explicitly requested.
