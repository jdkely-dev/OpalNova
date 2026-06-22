# Version 1.17.3 Purchase Order Recalculation Fix

## Fix

Visual Studio reported that `RecalculateParentPurchaseOrderAfterSave` did not exist in `MainWindow.xaml.cs`.

This version adds the missing helper methods:

- `RecalculateParentPurchaseOrderAfterSave(object entity)`
- `RecalculatePurchaseOrderTotalsById(int purchaseOrderId)`

The helpers recalculate purchase order totals after adding or editing purchase order records and purchase order item records. Delete handling was also improved so deleting a purchase order item recalculates the parent purchase order total.

## Purchase receive behaviour preserved

The existing V1.17.2 receive fixes remain in place:

- Receiving a purchase order updates linked material quantities.
- Receiving a purchase order creates material transactions.
- Purchase order item line totals are recalculated.
- Parent purchase order totals are recalculated.
- Receive warnings are formatted safely without multiline string compile errors.

## Validation

Static validation completed:

- Project file exists and parses as XML.
- XAML files parse as XML.
- XAML event handlers are present in code-behind.
- Purchase order recalculation helper references now resolve in `MainWindow.xaml.cs`.
- Purchase order receive logic still contains material quantity updates and material transaction creation.
- No interpolated raw string literals were found.

A full `dotnet build` still needs to be run in Visual Studio on Windows.
