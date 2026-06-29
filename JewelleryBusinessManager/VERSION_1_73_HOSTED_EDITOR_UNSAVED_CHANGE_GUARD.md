# OPALNOVA V1.73.0 - Hosted Editor Unsaved Change Guard

V1.73.0 broadens workspace close protection from quote tabs into the generic hosted record editor.

## Implemented

- Added `WorkspaceCloseDecision` and `IWorkspaceCloseRequestHandler` for hosted workspace close flows that can be handled by the child window.
- Updated workspace tab closing to support Close, Cancel and Handled outcomes while preserving the existing `IWorkspaceCloseGuard` behavior.
- Added field snapshot dirty detection to `EditEntityWindow`.
- Closing a changed hosted record editor now prompts Save, Discard or Cancel.
- Save from the close prompt routes through the existing `Saved` event so normal `MainWindow` business rules, validation and database persistence still apply.

## Preserved

- No database schema changes.
- Existing quote unsaved-change protection is preserved.
- Existing explicit Save and Cancel buttons in hosted editors are preserved.
- Existing business rules, payment workflow, reminders, documents and job completion behavior are preserved.

## Validation

- Debug build succeeds with zero warnings and zero errors.
- Release publish succeeds through `win-x64-self-contained`.
- Published `OPALNOVA.exe` launches and closes cleanly.
