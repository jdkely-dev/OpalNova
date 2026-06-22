using System.Globalization;
using System.Windows;
using JewelleryBusinessManager.Models;
using JewelleryBusinessManager.Services;
using MessageBox = System.Windows.MessageBox;
using MessageBoxButton = System.Windows.MessageBoxButton;
using MessageBoxImage = System.Windows.MessageBoxImage;
using MessageBoxResult = System.Windows.MessageBoxResult;

namespace JewelleryBusinessManager.Views;

public partial class MetalPricesWindow : Window
{
    private BusinessSettings _settings;

    public MetalPricesWindow()
    {
        InitializeComponent();
        _settings = BusinessSettingsService.Load();
        ProviderBox.ItemsSource = MetalPriceService.SupportedProviders;
        CurrencyBox.ItemsSource = MetalPriceService.SupportedCurrencies;
        LoadSettings();
    }

    private void LoadSettings()
    {
        ProviderBox.SelectedItem = string.IsNullOrWhiteSpace(_settings.MetalPriceProvider) ? "GoldAPI.net" : _settings.MetalPriceProvider;
        if (ProviderBox.SelectedItem == null) ProviderBox.SelectedItem = "GoldAPI.net";
        CurrencyBox.SelectedItem = string.IsNullOrWhiteSpace(_settings.MetalPriceCurrency) ? "AUD" : _settings.MetalPriceCurrency;
        ApiKeyBox.Password = _settings.MetalPriceApiKey;
        GoldBox.Text = _settings.GoldPricePerGram.ToString(CultureInfo.CurrentCulture);
        SilverBox.Text = _settings.SilverPricePerGram.ToString(CultureInfo.CurrentCulture);
        PlatinumBox.Text = _settings.PlatinumPricePerGram.ToString(CultureInfo.CurrentCulture);
        PalladiumBox.Text = _settings.PalladiumPricePerGram.ToString(CultureInfo.CurrentCulture);
        LastUpdatedText.Text = _settings.MetalPricesLastUpdated.HasValue
            ? $"Last updated: {_settings.MetalPricesLastUpdated.Value:g} ({_settings.MetalPriceCurrency})"
            : "Last updated: not yet refreshed";
        SourceNoteText.Text = _settings.MetalPriceSourceNote;
    }

    private bool TryApplyForm()
    {
        if (!TryReadMoney(GoldBox.Text, "Gold", out var gold)) return false;
        if (!TryReadMoney(SilverBox.Text, "Silver", out var silver)) return false;
        if (!TryReadMoney(PlatinumBox.Text, "Platinum", out var platinum)) return false;
        if (!TryReadMoney(PalladiumBox.Text, "Palladium", out var palladium)) return false;

        _settings.MetalPriceProvider = (ProviderBox.SelectedItem?.ToString() ?? "GoldAPI.net").Trim();
        _settings.MetalPriceCurrency = (CurrencyBox.SelectedItem?.ToString() ?? "AUD").Trim().ToUpperInvariant();
        _settings.MetalPriceApiKey = ApiKeyBox.Password.Trim();
        _settings.GoldPricePerGram = gold;
        _settings.SilverPricePerGram = silver;
        _settings.PlatinumPricePerGram = platinum;
        _settings.PalladiumPricePerGram = palladium;
        if (!_settings.MetalPricesLastUpdated.HasValue)
            _settings.MetalPriceSourceNote = "Manual metal prices saved locally.";
        return true;
    }

    private static bool TryReadMoney(string text, string label, out decimal value)
    {
        if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out value) && value >= 0)
            return true;
        MessageBox.Show($"{label} price must be zero or a positive number.", "Metal Prices", MessageBoxButton.OK, MessageBoxImage.Warning);
        return false;
    }

    private async void RefreshLive_Click(object sender, RoutedEventArgs e)
    {
        if (!TryApplyForm()) return;
        try
        {
            System.Windows.Input.Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;
            var snapshot = await MetalPriceService.RefreshLivePricesAsync(_settings);
            _settings.GoldPricePerGram = snapshot.GoldPerGram;
            _settings.SilverPricePerGram = snapshot.SilverPerGram;
            _settings.PlatinumPricePerGram = snapshot.PlatinumPerGram;
            _settings.PalladiumPricePerGram = snapshot.PalladiumPerGram;
            _settings.MetalPriceCurrency = snapshot.Currency;
            _settings.MetalPricesLastUpdated = snapshot.UpdatedAt;
            _settings.MetalPriceSourceNote = snapshot.SourceNote;
            BusinessSettingsService.Save(_settings);
            LoadSettings();
            MessageBox.Show("Live metal prices refreshed and saved.", "Metal Prices", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            ErrorLogService.Log(ex, "Refresh live metal prices");
            MessageBox.Show($"Could not refresh live metal prices. Manual prices are still available.\n\n{ex.Message}\n\nTip: make sure the Provider dropdown matches where your API key came from. GoldAPI.net and GoldAPI.io use different endpoints/authentication.", "Metal Price Refresh", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            System.Windows.Input.Mouse.OverrideCursor = null;
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (!TryApplyForm()) return;
        if (!_settings.MetalPricesLastUpdated.HasValue)
            _settings.MetalPricesLastUpdated = DateTime.Now;
        if (string.IsNullOrWhiteSpace(_settings.MetalPriceSourceNote) || _settings.MetalPriceSourceNote.StartsWith("Manual", StringComparison.OrdinalIgnoreCase))
            _settings.MetalPriceSourceNote = "Manual metal prices saved locally.";
        BusinessSettingsService.Save(_settings);
        DialogResult = true;
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
