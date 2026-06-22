# Version 1.32.2 Setup Selector Property Fix

## Fix
Corrected two invalid JewelleryItem property references in MainWindow.xaml.cs.

- Replaced JewelleryItem.ItemName with JewelleryItem.Name in Setup/Input record selectors.
- Bulk Status Update and Bulk Add Selected To Market selector lists now sort jewellery items by StockCode then Name.

## Validation
- ZIP integrity checked.
- XAML/XML parsed.
- Project file parsed.
- C# brace balance checked.
- Invalid JewelleryItem.ItemName references removed from MainWindow.xaml.cs.
- No interpolated raw strings introduced.
