# OPALNOVA V1.65.0 - Quote Context Fields

V1.65.0 strengthens the quote workflow with practical customer and project context.

## What Changed

- Added additive `CustomQuote` fields for occasion, required-by date, ring size, budget/target, preferred metal and preferred stone.
- Added matching controls in Custom Quote Builder.
- Restored editing for private internal quote notes in the quote UI.
- Added customer-facing project details to generated proposal output when those fields are recorded.
- Connected required-by dates to quote next-action guidance and the shared Alert Centre / Project Workbench next-action engine.
- Added quote context into Proposal Pipeline search and selected-row details.

## What Did Not Change

- No existing quote, option, stock, production, payment or proposal-send behavior was removed.
- Internal notes are still private and are not included in proposal output.
- Existing quote records remain valid; the new schema columns are additive and nullable.

## Validation

- Debug build should complete with zero warnings and zero errors.
- Release publish should complete for `win-x64-self-contained`.
- Published `OPALNOVA.exe` should launch and close cleanly.
