# Version 1.30.1 Dropdown Background Fix

## Purpose
Improve dropdown readability in the dark UI by making popup/list backgrounds darker while preserving the gold hover and selection contrast introduced in V1.30.

## Changes
- Added dark system brush overrides for native WPF popup/dropdown surfaces.
- Darkened ComboBox popup/list background behaviour.
- Tightened ComboBoxItem margins so light popup gaps are less likely to show through.
- Added darker item borders for clearer item separation.
- Added dark ContextMenu and MenuItem styles for consistency.
- Preserved selected/hovered dropdown text contrast.

## Validation
- ZIP integrity checked.
- App.xaml XML parsed successfully.
- MainWindow.xaml XML parsed successfully.
- Project file parsed successfully.
- No C# files were changed in this patch.
