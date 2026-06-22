# Version 1.17.2 Purchase Receive Compile Fix

## Issue fixed
Visual Studio reported compile errors in `Services/PurchaseOrderService.cs` around line 184:

- CS1010 Newline in constant
- CS1003 Syntax error, ',' expected
- CS1026 ) expected

The receive-warning message contained literal line breaks inside a normal C# string.

## Fix
The message construction now uses `Environment.NewLine` and `string.Join(Environment.NewLine, result.Warnings)` instead of embedding physical line breaks inside quoted strings.

## Inventory receive logic preserved
The receive workflow still:

- Updates linked material current quantity
- Creates a MaterialTransaction for received purchase order lines
- Recalculates the purchase order total
- Shows warnings for lines that cannot be linked to materials

## Validation
Two static validation passes were run after the fix. Checks included:

- ZIP integrity
- XAML/XML parse checks
- XAML event handler matching
- PurchaseOrderService compile-risk scan
- No raw interpolated strings in PurchaseOrderService
- No broken multiline warning-message string remains
- Purchase receive inventory-update tokens present

Result: 0 blocking errors.
