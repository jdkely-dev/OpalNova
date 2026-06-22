# Version 1.25.2 Selection Contrast Fix

## Purpose
Improve selection visibility across the dark UI, especially selected rows, navigation items, list items, and focused controls.

## Changes
- Added high-contrast gold selection brushes.
- Added dark-hover selection brushes for rows and navigation.
- Added WPF system selection brush overrides for controls that use system highlight colors.
- Added a custom DataGridRow template with clear selected, hover, and focused visual states.
- Added a DataGridCell template so selected rows show readable dark text on gold.
- Updated ListBoxItem selection and hover states.
- Replaced the plain TreeViewItem style with a rounded, high-contrast selected/hover navigation style.
- Preserved the dark/gold branding and existing functionality.

## Validation
- ZIP source extracted successfully.
- App.xaml parses as valid XML.
- Project file parses as valid XML.
- MainWindow.xaml parses as valid XML.
- CustomerDisplayWindow.xaml duplicate Background issue remains fixed.
- No C# source files were changed in this patch.
