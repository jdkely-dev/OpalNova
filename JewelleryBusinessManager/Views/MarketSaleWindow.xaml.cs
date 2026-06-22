using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using JewelleryBusinessManager.Data;
using JewelleryBusinessManager.Models;
using JewelleryBusinessManager.Services;
using MessageBox = System.Windows.MessageBox;
using MessageBoxButton = System.Windows.MessageBoxButton;
using MessageBoxImage = System.Windows.MessageBoxImage;
using MessageBoxResult = System.Windows.MessageBoxResult;

namespace JewelleryBusinessManager.Views;

public partial class MarketSaleWindow : Window
{
    private readonly object? _selectedRecord;
    private sealed record LookupItem(int? Id, string Display);

    public MarketSaleWindow(object? selectedRecord)
    {
        InitializeComponent();
        _selectedRecord = selectedRecord;
        LoadLookups();
        ApplyDefaults();
    }

    private void LoadLookups()
    {
        using var db = new AppDbContext();
        var items = db.JewelleryItems.AsEnumerable().ToDictionary(i => i.Id, i => i);
        var markets = db.MarketEvents.AsEnumerable().ToDictionary(m => m.Id, m => m);
        var marketStock = new List<LookupItem> { new(null, "Select market stock item") };
        marketStock.AddRange(db.MarketStocks.AsEnumerable()
            .Where(ms => !ms.SoldAtMarket)
            .OrderBy(ms => markets.TryGetValue(ms.MarketEventId, out var market) ? market.EventDate : DateTime.MaxValue)
            .ThenBy(ms => items.TryGetValue(ms.JewelleryItemId, out var item) ? item.StockCode : string.Empty)
            .Select(ms =>
            {
                items.TryGetValue(ms.JewelleryItemId, out var item);
                markets.TryGetValue(ms.MarketEventId, out var market);
                var display = $"{market?.EventDate:d} {market?.Name} — {item?.StockCode} {item?.Name} — {(item?.RetailPrice ?? 0):C}".Trim();
                return new LookupItem(ms.Id, display);
            }));

        MarketStockBox.ItemsSource = marketStock;

        var customers = new List<LookupItem> { new(null, "No customer selected") };
        customers.AddRange(db.Customers.AsEnumerable().OrderBy(c => c.FullName).Select(c => new LookupItem(c.Id, c.FullName)));
        CustomerBox.ItemsSource = customers;
        CustomerBox.SelectedIndex = 0;

        PaymentMethodBox.ItemsSource = Enum.GetValues(typeof(PaymentMethod));
        PaymentMethodBox.SelectedItem = PaymentMethod.Card;
    }

    private void ApplyDefaults()
    {
        if (_selectedRecord is MarketStock selectedMarketStock)
            MarketStockBox.SelectedValue = selectedMarketStock.Id;
        else if (_selectedRecord is JewelleryItem item)
        {
            using var db = new AppDbContext();
            var linkedMarketStock = db.MarketStocks.AsEnumerable().LastOrDefault(ms => ms.JewelleryItemId == item.Id && !ms.SoldAtMarket);
            if (linkedMarketStock != null)
                MarketStockBox.SelectedValue = linkedMarketStock.Id;
        }
        else if (_selectedRecord is MarketEvent market)
        {
            using var db = new AppDbContext();
            var firstMarketStock = db.MarketStocks.AsEnumerable().FirstOrDefault(ms => ms.MarketEventId == market.Id && !ms.SoldAtMarket);
            if (firstMarketStock != null)
                MarketStockBox.SelectedValue = firstMarketStock.Id;
        }

        if (MarketStockBox.SelectedIndex < 0 && MarketStockBox.Items.Count > 0)
            MarketStockBox.SelectedIndex = 0;
        UpdateStockInfo();
    }

    private void MarketStockBox_SelectionChanged(object sender, SelectionChangedEventArgs e) => UpdateStockInfo();

    private void UpdateStockInfo()
    {
        if (MarketStockBox.SelectedValue is not int marketStockId)
        {
            StockInfoText.Text = "Choose a market stock item to sell.";
            SalePriceBox.Text = string.Empty;
            return;
        }

        using var db = new AppDbContext();
        var stock = db.MarketStocks.Find(marketStockId);
        if (stock == null) return;
        var item = db.JewelleryItems.Find(stock.JewelleryItemId);
        var market = db.MarketEvents.Find(stock.MarketEventId);
        StockInfoText.Text = $"{market?.Name} • {item?.StockCode} {item?.Name} • Retail {item?.RetailPrice:C}";
        if (string.IsNullOrWhiteSpace(SalePriceBox.Text))
            SalePriceBox.Text = (stock.SalePrice > 0 ? stock.SalePrice : item?.RetailPrice ?? 0).ToString("0.00", CultureInfo.CurrentCulture);
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (MarketStockBox.SelectedValue is not int marketStockId)
        {
            MessageBox.Show("Choose a market stock item first.", "Market Sale", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (!decimal.TryParse(SalePriceBox.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out var price) || price <= 0)
        {
            MessageBox.Show("Enter a positive sale price.", "Market Sale", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        var paymentMethod = PaymentMethodBox.SelectedItem is PaymentMethod method ? method : PaymentMethod.Card;
        var customerId = CustomerBox.SelectedValue as int?;

        try
        {
            var sale = MarketProService.CreateMarketSale(marketStockId, price, paymentMethod, customerId, NotesBox.Text);
            MessageBox.Show($"Market sale saved. Sale #{sale.Id} created for {sale.SaleAmount:C}.", "Market Sale", MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
        }
        catch (Exception ex)
        {
            ErrorLogService.Log(ex, "Save market sale");
            MessageBox.Show($"Could not save the market sale.\n\n{ex.Message}", "Market Sale", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
