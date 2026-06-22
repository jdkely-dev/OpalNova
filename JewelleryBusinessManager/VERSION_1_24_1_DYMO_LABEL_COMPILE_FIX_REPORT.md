# Version 1.24.1 - DYMO Label Compile Fix

## Issue fixed

Visual Studio reported CS9006 in `Views/DymoMiniLabelWindow.xaml.cs` because the DYMO mini-label printable HTML used an interpolated raw string containing CSS braces.

## Fix

The DYMO mini-label HTML generator now uses `StringBuilder.AppendLine(...)` instead of an interpolated raw string. This avoids C# interpreting CSS braces as interpolation syntax.

## Validation performed

- ZIP extracted successfully
- XAML/XML files parsed successfully
- Project file parsed successfully
- Confirmed no `$"""` interpolated raw strings remain in `.cs` files
- Confirmed `DymoMiniLabelWindow.BuildHtml()` uses `StringBuilder`
- Confirmed DYMO label HTML still includes style rules and barcode/label markup
- Checked XAML event-handler wiring

