# V1.24.2 Windows Forms/WPF Ambiguous Reference Fix

## Fixes applied

- Fully qualified the WPF application base class in `App.xaml.cs`.
- Added WPF aliases for controls that conflicted with Windows Forms implicit namespaces.
- Fully qualified the WPF `PrintDialog` in the DYMO mini-label window.
- Added `Microsoft.Win32.OpenFileDialog` aliases where file dialogs are used.
- Added WPF `MessageBox` aliases where message boxes are used.
- Added project-level implicit namespace removal entries for `System.Windows.Forms` and `System.Drawing` while keeping Windows Forms support for multi-monitor `Screen` detection.

## Validation

Checks passed: 35
Blocking errors: 0
Warnings: 0


## Checks
- XAML parsed: App.xaml
- XAML parsed: MainWindow.xaml
- XAML parsed: EditEntityWindow.xaml
- XAML parsed: SettingsWindow.xaml
- XAML parsed: MetalPricesWindow.xaml
- XAML parsed: PricingHelperWindow.xaml
- XAML parsed: InventoryMovementWindow.xaml
- XAML parsed: InventoryStatusWindow.xaml
- XAML parsed: TraceabilityWindow.xaml
- XAML parsed: AddToBatchWindow.xaml
- XAML parsed: StoneWorkflowWindow.xaml
- XAML parsed: MarketSaleWindow.xaml
- XAML parsed: MarketReconcileWindow.xaml
- XAML parsed: ScanLookupWindow.xaml
- XAML parsed: DymoMiniLabelWindow.xaml
- XAML parsed: DeviceCaptureWindow.xaml
- XAML parsed: CustomerDisplayWindow.xaml
- XAML parsed: MarketOperationsWindow.xaml
- Project file parsed
- No interpolated raw strings remain
- App base class fully qualified
- MainWindow WpfButton alias present
- EditEntityWindow WpfComboBox alias present
- EditEntityWindow WpfTextBox alias present
- EditEntityWindow WpfImage alias present
- EditEntityWindow WpfPanel alias present
- EditEntityWindow WpfButton alias present
- EditEntityWindow WpfCheckBox alias present
- DYMO PrintDialog fully qualified
- Found Views/DymoMiniLabelWindow.xaml.cs
- Found Views/DeviceCaptureWindow.xaml.cs
- Found Views/MarketOperationsWindow.xaml.cs
- Found Views/ScanLookupWindow.xaml.cs
- Windows Forms support enabled for multi-monitor screen detection
- Project attempts to remove implicit WinForms namespace import