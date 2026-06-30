using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using MessageBox = System.Windows.MessageBox;
using MessageBoxButton = System.Windows.MessageBoxButton;
using MessageBoxImage = System.Windows.MessageBoxImage;

namespace JewelleryBusinessManager.Views;

public partial class JewellerToolsWindow : Window
{
    private sealed record RingSizeRow(string AuUk, string Us, string Eu, string InsideDiameterMm, string InsideCircumferenceMm);

    public JewellerToolsWindow()
    {
        InitializeComponent();
        RingSizeGrid.ItemsSource = RingSizes;
        Calculate();
    }

    private static readonly RingSizeRow[] RingSizes =
    {
        new("H", "4", "46.5", "14.8 mm", "46.5 mm"),
        new("I", "4.5", "47.8", "15.2 mm", "47.8 mm"),
        new("J", "5", "49.0", "15.6 mm", "49.0 mm"),
        new("K", "5.5", "50.3", "16.0 mm", "50.3 mm"),
        new("L", "6", "51.5", "16.4 mm", "51.5 mm"),
        new("M", "6.5", "52.8", "16.8 mm", "52.8 mm"),
        new("N", "7", "54.0", "17.2 mm", "54.0 mm"),
        new("O", "7.5", "55.3", "17.6 mm", "55.3 mm"),
        new("P", "8", "56.6", "18.0 mm", "56.6 mm"),
        new("Q", "8.5", "57.8", "18.4 mm", "57.8 mm"),
        new("R", "9", "59.1", "18.8 mm", "59.1 mm"),
        new("S", "9.5", "60.3", "19.2 mm", "60.3 mm"),
        new("T", "10", "61.6", "19.6 mm", "61.6 mm"),
        new("U", "10.5", "62.8", "20.0 mm", "62.8 mm"),
        new("V", "11", "64.1", "20.4 mm", "64.1 mm"),
        new("W", "11.5", "65.3", "20.8 mm", "65.3 mm"),
        new("X", "12", "66.6", "21.2 mm", "66.6 mm"),
        new("Y", "12.5", "67.9", "21.6 mm", "67.9 mm"),
        new("Z", "13", "69.1", "22.0 mm", "69.1 mm")
    };

    private void Calculate_Click(object sender, RoutedEventArgs e) => Calculate();

    private void Calculate()
    {
        if (!TryRead(MetalLengthBox.Text, "Metal length", out var metalLength)) return;
        if (!TryRead(MetalWidthBox.Text, "Metal width", out var metalWidth)) return;
        if (!TryRead(MetalThicknessBox.Text, "Metal thickness", out var metalThickness)) return;
        if (!TryRead(StoneLengthBox.Text, "Stone length", out var stoneLength)) return;
        if (!TryRead(StoneWidthBox.Text, "Stone width", out var stoneWidth)) return;
        if (!TryRead(StoneDepthBox.Text, "Stone depth", out var stoneDepth)) return;

        var metal = SelectedContent(MetalBox, "Sterling Silver");
        var density = MetalDensity(metal);
        var volumeMm3 = metalLength * metalWidth * metalThickness;
        var grams = Math.Round(volumeMm3 * density / 1000m, 2);

        var shape = SelectedContent(StoneShapeBox, "Round");
        var factor = StoneFactor(shape);
        var carats = Math.Round(stoneLength * stoneWidth * stoneDepth * factor, 2);

        ResultText.Text =
            $"Metal weight estimate\n" +
            $"{metal}: {grams:N2} g from {metalLength:N2} x {metalWidth:N2} x {metalThickness:N2} mm using density {density:N2} g/cm3.\n\n" +
            $"Stone carat estimate\n" +
            $"{shape}: {carats:N2} ct from {stoneLength:N2} x {stoneWidth:N2} x {stoneDepth:N2} mm using factor {factor:N4}.\n\n" +
            $"Use these values as quote inputs only after checking allowances for sprues, waste, setting style, stone cut and actual scale readings.";
    }

    private void CopyResults_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(ResultText.Text))
            Calculate();
        Clipboard.SetText(ResultText.Text);
        MessageBox.Show("Jeweller tool results copied.", "Jeweller Tools", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private static string SelectedContent(ComboBox box, string fallback) =>
        ((ComboBoxItem?)box.SelectedItem)?.Content?.ToString() ?? fallback;

    private static decimal MetalDensity(string metal) => metal switch
    {
        "9ct Gold" => 11.4m,
        "14ct Gold" => 13.1m,
        "18ct Gold" => 15.6m,
        "Platinum" => 21.45m,
        "Palladium" => 12.0m,
        _ => 10.36m
    };

    private static decimal StoneFactor(string shape) => shape switch
    {
        "Oval" => 0.0062m,
        "Emerald / Rectangle" => 0.0080m,
        "Pear" => 0.0060m,
        "Marquise" => 0.0056m,
        "Cushion" => 0.0080m,
        _ => 0.0061m
    };

    private static bool TryRead(string text, string label, out decimal value)
    {
        if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out value) && value > 0)
            return true;
        MessageBox.Show($"{label} must be greater than zero.", "Jeweller Tools", MessageBoxButton.OK, MessageBoxImage.Warning);
        return false;
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
