# V1.30.2 ComboBox Face Background Fix

## Issue
The ComboBox dropdown list popup used the correct dark background, but the collapsed/visible ComboBox face still used the default light WPF control template in some windows/sections.

## Fix
- Replaced the default ComboBox control template with a custom dark-theme template.
- Darkened the collapsed ComboBox face/background.
- Preserved gold hover, focus, and open-state accents.
- Kept the dropdown popup dark with dark item backgrounds.
- Preserved the existing ComboBoxItem hover/selection contrast improvements.

## Validation
- ZIP integrity checked.
- App.xaml XML parsing passed.
- MainWindow.xaml XML parsing passed.
- Project file parsing passed.
- ComboBox custom template present.
- ComboBox popup dark background present.
- ComboBox face dark background present.
