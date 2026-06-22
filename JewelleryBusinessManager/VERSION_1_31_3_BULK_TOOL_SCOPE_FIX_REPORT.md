# Version 1.31.3 Bulk Tool Scope Fix

## Issue Fixed
Visual Studio reported CS1628 in `MainWindow.xaml.cs` because the `out` parameter `firstType` was referenced inside a LINQ lambda expression.

## Fix Applied
The method now copies the selected record type into a normal local variable named `selectedType` before using it inside the lambda, then assigns it back to `firstType` after validation.

## Validation
- ZIP extraction checked
- XAML/XML parsing checked
- Project file parsing checked
- C# brace balance checked
- Confirmed old `selected.Any(x => x.GetType() != firstType)` pattern removed
- Confirmed no interpolated raw strings remain

## Expected Behaviour
Bulk Status Update should now compile and open inside the Data Cleanup Studio preview/work area as intended.
