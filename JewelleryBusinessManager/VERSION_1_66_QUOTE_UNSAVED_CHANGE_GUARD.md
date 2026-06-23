# OPALNOVA V1.66.0 - Quote Unsaved Change Guard

V1.66.0 starts unsaved-change protection with the Custom Quote Builder, the editor most likely to lose detailed work if a tab is closed accidentally.

## What Changed

- Added a reusable workspace close-guard interface.
- Updated hosted workspace tab closing to ask close-guarded windows before removing the tab.
- Added dirty tracking to Custom Quote Builder for quote fields, context fields, internal notes, option text, option costs, recommendation changes, design images, linked stones/materials and external diamonds.
- Closing a dirty quote tab now prompts Save, Discard or Cancel.
- Starting a new quote from a dirty quote uses the same prompt.
- Successful save, preview, send/record proposal, accept option, release reservations and create job actions reset the dirty state.

## What Did Not Change

- No database schema changes.
- No quote pricing, proposal generation, reservation, acceptance or job conversion calculations changed.
- The close guard currently applies to Custom Quote Builder only; other hosted editors can adopt the same interface later.

## Validation

- Debug build should complete with zero warnings and zero errors.
- Release publish should complete for `win-x64-self-contained`.
- Published `OPALNOVA.exe` should launch and close cleanly.
