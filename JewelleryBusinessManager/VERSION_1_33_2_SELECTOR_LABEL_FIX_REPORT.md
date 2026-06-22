# V1.33.2 Record Selector Label Fix

## Issue fixed
After V1.33.1 changed customer record loading to order by `Customer.FullName`, the shared selector label helper still did not include `FullName` in its display-name lookup list.

That meant customer dropdowns in tool setup panels could still show generic labels such as `Customer #1` instead of the real customer name.

## Fix
Updated `GetEntityDisplayText` in `MainWindow.xaml.cs` so selector labels now check these important display fields first:

- `FullName` for customers
- `Name` for suppliers, materials, market events, batches and jewellery stock
- `ItemName` for batch and purchase order items
- `SeoTitle` for online listings
- `JobTitle` for job records
- existing code/status identifiers such as `StockCode`, `StoneCode`, `MaterialCode`, `TaskCode`, `PurchaseOrderCode` and `BatchCode`

## Result
Tool setup dropdowns and bulk-selection panels should now show clearer human-readable labels, especially for customers.

## Validation completed
- ZIP extraction verified
- Main project file present
- XAML files parse as XML
- C# brace balance checked
- Confirmed `GetEntityDisplayText` now includes `FullName`
- Confirmed no remaining `Customer.Name` usage in active C# code

## Notes
A full `dotnet build` could not be run in the packaging environment because the .NET SDK is not installed there. This patch is a small source-level change only and does not alter database schema or dependencies.
