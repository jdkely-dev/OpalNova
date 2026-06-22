# OPALNOVA V1.36.2 — Toggle Hover Help

## Purpose
Refine the V1.36.1 faded `?` mini-guide badges so they support both temporary hover previews and deliberate pinned help windows.

## Changes
- Hovering over a help badge opens a mini-guide preview.
- Moving away from the badge closes the mini-guide automatically unless the badge has been clicked.
- Clicking a badge pins the mini-guide open.
- Clicking the same badge again closes the pinned guide.
- The help badge still prevents accidental activation of the main action button.
- No database schema, save/load, or business workflow changes were made.

## Validation
- XAML XML parse checked.
- Project XML parse checked.
- C# brace balance checked.
- ZIP package integrity checked.

## Notes
Visual Studio remains the final build and publish validation environment.
