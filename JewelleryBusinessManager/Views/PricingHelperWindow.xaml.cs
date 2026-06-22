using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using JewelleryBusinessManager.Services;
using MessageBox = System.Windows.MessageBox;
using MessageBoxButton = System.Windows.MessageBoxButton;
using MessageBoxImage = System.Windows.MessageBoxImage;
using MessageBoxResult = System.Windows.MessageBoxResult;

namespace JewelleryBusinessManager.Views;

public partial class PricingHelperWindow : Window
{
    public PricingHelperWindow()
    {
        InitializeComponent();
        var settings = BusinessSettingsService.Load();
        TargetMarginBox.Text = settings.DefaultProfitMarginPercent.ToString(CultureInfo.CurrentCulture);
        MetalPriceSummaryText.Text = $"Using {settings.MetalPriceCurrency} per gram prices. Gold: {settings.GoldPricePerGram:C}, Silver: {settings.SilverPricePerGram:C}, Platinum: {settings.PlatinumPricePerGram:C}, Palladium: {settings.PalladiumPricePerGram:C}. Last updated: {(settings.MetalPricesLastUpdated?.ToString("g") ?? "not set")}.";
    }

    private void Calculate_Click(object sender, RoutedEventArgs e)
    {
        var settings = BusinessSettingsService.Load();
        if (!TryRead(MetalGramsBox.Text, "Metal grams", out var grams)) return;
        if (!TryRead(StoneCostBox.Text, "Stone cost", out var stoneCost)) return;
        if (!TryRead(OtherCostBox.Text, "Other cost", out var otherCost)) return;
        if (!TryRead(LabourHoursBox.Text, "Labour hours", out var labourHours)) return;
        if (!TryRead(TargetMarginBox.Text, "Target margin", out var targetMargin) || targetMargin <= 0 || targetMargin >= 100)
        {
            MessageBox.Show("Target margin must be greater than 0 and less than 100.", "Pricing Helper", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var metal = ((ComboBoxItem?)MetalBox.SelectedItem)?.Content?.ToString() ?? "Sterling Silver";
        var metalCost = MetalPriceService.EstimateMetalCost(metal, grams, settings);
        var labourCost = Math.Round(labourHours * settings.DefaultLabourRate, 2);
        var totalCost = metalCost + stoneCost + otherCost + labourCost;
        var recommendedRetail = PricingService.CalculateRecommendedRetail(totalCost, targetMargin);
        var profit = recommendedRetail - totalCost;
        var markup = PricingService.CalculateMarkup(recommendedRetail, totalCost) * 100m;

        ResultText.Text =
            $"Metal: {metal}\n" +
            $"Metal cost: {metalCost:C}\n" +
            $"Stone cost: {stoneCost:C}\n" +
            $"Other cost: {otherCost:C}\n" +
            $"Labour cost: {labourCost:C} ({labourHours:N2}h × {settings.DefaultLabourRate:C}/h)\n" +
            $"Total cost: {totalCost:C}\n\n" +
            $"Recommended retail for {targetMargin:N1}% margin: {recommendedRetail:C}\n" +
            $"Estimated profit: {profit:C}\n" +
            $"Markup: {markup:N1}%";
    }

    private static bool TryRead(string text, string label, out decimal value)
    {
        if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out value) && value >= 0)
            return true;
        MessageBox.Show($"{label} must be zero or a positive number.", "Pricing Helper", MessageBoxButton.OK, MessageBoxImage.Warning);
        return false;
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
