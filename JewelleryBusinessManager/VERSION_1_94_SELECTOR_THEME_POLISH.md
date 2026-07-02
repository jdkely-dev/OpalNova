# OPALNOVA V1.94.0 - Selector Theme Polish

V1.94.0 resumes the UI/workflow streamlining direction with a shared no-schema control polish pass.

## Implemented

- Added a global ComboBox empty-state prompt path.
- Unselected selectors now show muted friendly guidance instead of an empty control face.
- The prompt uses the selector `Tag` when supplied and falls back to `Choose...`.
- Added dark OPALNOVA styling for DatePicker text boxes, calendar day buttons and calendar month/year buttons.
- Routed Payment & Collection ComboBox and DatePicker styles through the shared global styles.
- Routed Production Board ComboBox styling through the shared global selector template.
- Cleaned high-use selector/list display text for supplier diamonds, quote option links, saved views, batch lookup and material movement lookup to use plain ASCII separators.
- Preserved existing selector item bindings, selected values, status changes, payments, production movement and report behavior.
- Preserved database schema.

## Validation

- Debug build succeeded with zero errors.
- Release publish succeeded for `win-x64` self-contained output.
- Published `OPALNOVA.exe` launched and closed cleanly.
- The build and publish surfaced NU1900 warnings because the sandbox could not reach NuGet vulnerability data at `https://api.nuget.org/v3/index.json`.

## Notes

- This pass intentionally fixes shared control behavior rather than adding one-off page patches.
- Existing selectors can provide more specific prompts later by setting `Tag` on the ComboBox.
- The separator cleanup is limited to display strings used by selectors and workflow lists touched by this pass.
- No records, stock quantities, payments, supplier diamonds or workflow statuses are changed by this polish pass.
