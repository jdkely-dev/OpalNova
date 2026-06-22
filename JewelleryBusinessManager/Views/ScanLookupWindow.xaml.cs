using System.Windows;
using System.Windows.Input;
using JewelleryBusinessManager.Services;

namespace JewelleryBusinessManager.Views;

public partial class ScanLookupWindow : Window
{
    public ScanLookupWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => CodeBox.Focus();
    }

    private void Lookup_Click(object sender, RoutedEventArgs e) => Lookup();

    private void CodeBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            Lookup();
            e.Handled = true;
        }
    }

    private void Lookup()
    {
        var code = CodeBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(code))
        {
            ResultsBox.Text = "Enter or scan a code first.";
            return;
        }

        var results = BarcodeLabelService.LookupCode(code);
        ResultsBox.Text = results.Count == 0
            ? $"No matching records found for: {code}"
            : string.Join("\n\n", results);
    }
}
