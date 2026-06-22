# V1.33.1 Customer Selector Property Fix

## Issue fixed
Visual Studio reported:

`CS1061: 'Customer' does not contain a definition for 'Name'`

The new workspace record selector in `MainWindow.xaml.cs` incorrectly ordered customers by `Customer.Name`.

## Fix
Changed the customer selector ordering to use the real model property:

`Customer.FullName`

## Validation completed
- ZIP extraction verified
- Project file XML parses
- XAML files parse
- C# brace balance checked
- Removed invalid `db.Customers.AsNoTracking().OrderBy(x => x.Name)` reference
- Checked no interpolated raw strings were introduced

## Result
Blocking validation errors: 0
