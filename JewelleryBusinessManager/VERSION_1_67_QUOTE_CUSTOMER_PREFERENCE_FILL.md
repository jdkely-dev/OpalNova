# OPALNOVA V1.67.0 - Quote Customer Preference Fill

V1.67.0 makes customer preferences easier to reuse during quote setup.

## What Changed

- Added `Use Customer Preferences` in Custom Quote Builder after customer selection.
- The action fills blank quote fields from the selected customer profile:
  - ring size
  - preferred metal
  - preferred stone
- Existing quote-specific entries are not overwritten.
- The action marks the quote as changed, so the V1.66 unsaved-change guard still protects the edit.

## What Did Not Change

- No database schema changes.
- No customer records are edited by this action.
- No proposal pricing, stock reservation or job conversion behavior changed.

## Validation

- Debug build should complete with zero warnings and zero errors.
- Release publish should complete for `win-x64-self-contained`.
- Published `OPALNOVA.exe` should launch and close cleanly.
