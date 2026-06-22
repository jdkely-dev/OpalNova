using System.Windows;
using Microsoft.EntityFrameworkCore;
using JewelleryBusinessManager.Data;
using JewelleryBusinessManager.Models;
using Forms = System.Windows.Forms;
using MessageBox = System.Windows.MessageBox;
using MessageBoxButton = System.Windows.MessageBoxButton;
using MessageBoxImage = System.Windows.MessageBoxImage;
using MessageBoxResult = System.Windows.MessageBoxResult;

namespace JewelleryBusinessManager.Views;

public partial class MarketOperationsWindow : Window
{
    private readonly List<MarketStockRow> _rows = new();
    private CustomerDisplayWindow? _customerDisplay;

    public MarketOperationsWindow()
    {
        InitializeComponent();
        LoadScreens();
        LoadMarkets();
    }

    protected override void OnClosed(EventArgs e)
    {
        try
        {
            if (_customerDisplay is not null)
            {
                _customerDisplay.Closed -= CustomerDisplay_Closed;
                _customerDisplay.Close();
                _customerDisplay = null;
            }
        }
        catch { }
        base.OnClosed(e);
    }

    private void CustomerDisplay_Closed(object? sender, EventArgs e)
    {
        if (ReferenceEquals(sender, _customerDisplay))
        {
            _customerDisplay = null;
        }
    }

    private CustomerDisplayWindow EnsureCustomerDisplayWindow()
    {
        if (_customerDisplay is null)
        {
            _customerDisplay = new CustomerDisplayWindow();
            _customerDisplay.Closed += CustomerDisplay_Closed;
        }
        return _customerDisplay;
    }

    private void LoadScreens()
    {
        ScreenBox.Items.Clear();
        var screens = Forms.Screen.AllScreens;
        for (var i = 0; i < screens.Length; i++)
        {
            ScreenBox.Items.Add($"Screen {i + 1}: {screens[i].Bounds.Width} x {screens[i].Bounds.Height}{(screens[i].Primary ? " (Primary)" : string.Empty)}");
        }
        if (ScreenBox.Items.Count > 0) ScreenBox.SelectedIndex = screens.Length > 1 ? 1 : 0;
    }

    private void LoadMarkets()
    {
        using var db = new AppDbContext();
        var markets = db.MarketEvents.AsNoTracking().OrderByDescending(m => m.EventDate).ToList();
        MarketBox.ItemsSource = markets;
        if (markets.Count > 0) MarketBox.SelectedIndex = 0;
        RefreshMarketStock();
    }

    private void MarketBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e) => RefreshMarketStock();
    private void SearchBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e) => ApplyFilter();
    private void Refresh_Click(object sender, RoutedEventArgs e) => RefreshMarketStock();

    private MarketEvent? SelectedMarket => MarketBox.SelectedItem as MarketEvent;
    private MarketStockRow? SelectedRow => MarketStockGrid.SelectedItem as MarketStockRow;

    private void RefreshMarketStock()
    {
        _rows.Clear();
        if (SelectedMarket is null)
        {
            MarketStockGrid.ItemsSource = null;
            MarketSummaryText.Text = "No market selected.";
            return;
        }

        using var db = new AppDbContext();
        var rows = from ms in db.MarketStocks.AsNoTracking()
                   join item in db.JewelleryItems.AsNoTracking() on ms.JewelleryItemId equals item.Id
                   where ms.MarketEventId == SelectedMarket.Id
                   orderby item.StockCode
                   select new MarketStockRow
                   {
                       MarketStockId = ms.Id,
                       JewelleryItemId = item.Id,
                       StockCode = item.StockCode,
                       ItemName = item.Name,
                       RetailPrice = item.RetailPrice,
                       Packed = ms.Packed,
                       SoldAtMarket = ms.SoldAtMarket,
                       ReturnedToStock = ms.ReturnedToStock,
                       PaymentMethod = ms.PaymentMethodText ?? string.Empty
                   };
        _rows.AddRange(rows.ToList());
        ApplyFilter();
        MarketSummaryText.Text = $"{SelectedMarket.Name} • {_rows.Count} items • {_rows.Count(r => r.Packed)} packed • {_rows.Count(r => r.SoldAtMarket)} sold";
    }

    private void ApplyFilter()
    {
        var term = SearchBox.Text?.Trim().ToLowerInvariant() ?? string.Empty;
        MarketStockGrid.ItemsSource = string.IsNullOrWhiteSpace(term)
            ? _rows.ToList()
            : _rows.Where(r => (r.StockCode + " " + r.ItemName).ToLowerInvariant().Contains(term)).ToList();
    }

    private void MarketStockGrid_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (SelectedRow is null) return;
        SelectedItemText.Text = $"{SelectedRow.StockCode} — {SelectedRow.ItemName}";
        SalePriceBox.Text = SelectedRow.RetailPrice.ToString("0.00");
        UpdateCustomerDisplay(previewOnly: true);
    }

    private void OpenCustomerDisplay_Click(object sender, RoutedEventArgs e)
    {
        var display = EnsureCustomerDisplayWindow();
        var index = Math.Max(0, ScreenBox.SelectedIndex);
        var screens = Forms.Screen.AllScreens;
        if (index >= screens.Length) index = 0;
        var bounds = screens[index].WorkingArea;
        display.Left = bounds.Left;
        display.Top = bounds.Top;
        display.Width = bounds.Width;
        display.Height = bounds.Height;
        display.WindowState = WindowState.Normal;
        if (!display.IsVisible)
        {
            display.Show();
        }
        display.Activate();
        UpdateCustomerDisplay(previewOnly: true);
    }

    private void ClearCustomerDisplay_Click(object sender, RoutedEventArgs e)
    {
        _customerDisplay?.UpdateDisplay("Market Checkout", "Select an item when the customer is ready.", 0m);
    }

    private void PreviewSale_Click(object sender, RoutedEventArgs e) => UpdateCustomerDisplay(previewOnly: true);

    private void UpdateCustomerDisplay(bool previewOnly)
    {
        if (_customerDisplay is null || !_customerDisplay.IsVisible || SelectedRow is null) return;
        var price = decimal.TryParse(SalePriceBox.Text, out var parsed) ? parsed : SelectedRow.RetailPrice;
        var detail = $"{SelectedRow.StockCode} • {SelectedRow.ItemName}";
        _customerDisplay.UpdateDisplay(SelectedRow.ItemName, detail, price, previewOnly ? "Preview" : "Payment received — thank you");
    }

    private void RecordSale_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedRow is null)
        {
            MessageBox.Show("Select a market stock item first.", "Market Sale", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (!decimal.TryParse(SalePriceBox.Text, out var salePrice))
        {
            MessageBox.Show("Enter a valid sale price.", "Market Sale", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        var paymentText = (PaymentBox.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString() ?? "Card";
        using var db = new AppDbContext();
        var marketStock = db.MarketStocks.Find(SelectedRow.MarketStockId);
        var item = db.JewelleryItems.Find(SelectedRow.JewelleryItemId);
        if (marketStock is null || item is null) return;
        var sale = new Sale
        {
            JewelleryItemId = item.Id,
            SaleDate = DateTime.Today,
            SaleAmount = salePrice,
            PaymentMethod = paymentText == "Cash" ? PaymentMethod.Cash : paymentText == "Bank Transfer" ? PaymentMethod.BankTransfer : PaymentMethod.Card,
            SaleLocation = SaleLocation.Market,
            CostOfGoods = item.TotalCost,
            Notes = $"Market sale recorded from Market Operations for market #{marketStock.MarketEventId}."
        };
        db.Sales.Add(sale);
        db.SaveChanges();
        marketStock.SoldAtMarket = true;
        marketStock.SoldAt = DateTime.Now;
        marketStock.SalePrice = salePrice;
        marketStock.PaymentMethodText = paymentText;
        marketStock.SaleId = sale.Id;
        item.Status = StockStatus.Sold;
        db.SaveChanges();
        UpdateCustomerDisplay(previewOnly: false);
        StatusText.Text = $"Recorded sale: {item.StockCode} {item.Name} for {salePrice:C}.";
        RefreshMarketStock();
    }

    private void MarkPacked_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedRow is null) return;
        using var db = new AppDbContext();
        var marketStock = db.MarketStocks.Find(SelectedRow.MarketStockId);
        if (marketStock is null) return;
        marketStock.Packed = true;
        marketStock.PackedAt = DateTime.Now;
        db.SaveChanges();
        RefreshMarketStock();
    }

    private void ReturnToStock_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedRow is null) return;
        using var db = new AppDbContext();
        var marketStock = db.MarketStocks.Find(SelectedRow.MarketStockId);
        var item = db.JewelleryItems.Find(SelectedRow.JewelleryItemId);
        if (marketStock is null || item is null) return;
        marketStock.ReturnedToStock = true;
        item.Status = StockStatus.InStock;
        db.SaveChanges();
        RefreshMarketStock();
    }

    private sealed class MarketStockRow
    {
        public int MarketStockId { get; set; }
        public int JewelleryItemId { get; set; }
        public string StockCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public decimal RetailPrice { get; set; }
        public bool Packed { get; set; }
        public bool SoldAtMarket { get; set; }
        public bool ReturnedToStock { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
    }
}
