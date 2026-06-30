using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using JewelleryBusinessManager.Data;
using JewelleryBusinessManager.Models;
using JewelleryBusinessManager.Services;
using MessageBox = System.Windows.MessageBox;
using MessageBoxButton = System.Windows.MessageBoxButton;
using MessageBoxImage = System.Windows.MessageBoxImage;

namespace JewelleryBusinessManager.Views;

public partial class MarketReconcileWindow : Window
{
    private readonly object? _selectedRecord;
    private List<MarketStock> _selectedMarketStock = new();
    private bool _loading;
    private sealed record LookupItem(int Id, string Display);

    public MarketReconcileWindow(object? selectedRecord)
    {
        InitializeComponent();
        _selectedRecord = selectedRecord;
        LoadMarkets();
        ApplyDefaults();
    }

    private void LoadMarkets()
    {
        using var db = new AppDbContext();
        MarketBox.ItemsSource = db.MarketEvents.AsEnumerable()
            .OrderByDescending(m => m.EventDate)
            .Select(m => new LookupItem(m.Id, $"{m.EventDate:d} {m.Name} - {m.Location}".Trim()))
            .ToList();
    }

    private void ApplyDefaults()
    {
        if (_selectedRecord is MarketEvent market)
            MarketBox.SelectedValue = market.Id;
        else if (_selectedRecord is MarketStock stock)
            MarketBox.SelectedValue = stock.MarketEventId;
        if (MarketBox.SelectedIndex < 0 && MarketBox.Items.Count > 0)
            MarketBox.SelectedIndex = 0;
        LoadSelectedMarket();
    }

    private void MarketBox_SelectionChanged(object sender, SelectionChangedEventArgs e) => LoadSelectedMarket();

    private void LoadSelectedMarket()
    {
        if (MarketBox.SelectedValue is not int marketId)
        {
            MarketInfoText.Text = "Choose a market to reconcile.";
            ReconciliationGuidanceText.Text = string.Empty;
            _selectedMarketStock.Clear();
            return;
        }

        using var db = new AppDbContext();
        var market = db.MarketEvents.Find(marketId);
        if (market == null)
            return;

        _loading = true;
        var stock = db.MarketStocks.Where(ms => ms.MarketEventId == market.Id).AsEnumerable().ToList();
        _selectedMarketStock = stock;
        MarketInfoText.Text = $"Stock records: {stock.Count} | Packed: {stock.Count(s => s.Packed)} | Sold: {stock.Count(s => s.SoldAtMarket)} | Returned/return expected: {stock.Count(s => s.ReturnedToStock || (s.Packed && !s.SoldAtMarket))}";
        OpeningFloatBox.Text = market.OpeningFloat.ToString("0.00", CultureInfo.CurrentCulture);
        CashSalesBox.Text = market.CashSales.ToString("0.00", CultureInfo.CurrentCulture);
        CardSalesBox.Text = market.CardSales.ToString("0.00", CultureInfo.CurrentCulture);
        OtherSalesBox.Text = market.OtherSales.ToString("0.00", CultureInfo.CurrentCulture);
        StallFeeBox.Text = market.StallFee.ToString("0.00", CultureInfo.CurrentCulture);
        TravelCostBox.Text = market.TravelCost.ToString("0.00", CultureInfo.CurrentCulture);
        DisplayCostBox.Text = market.DisplayCost.ToString("0.00", CultureInfo.CurrentCulture);
        OtherCostsBox.Text = market.OtherCosts.ToString("0.00", CultureInfo.CurrentCulture);
        NotesBox.Text = market.ReconciliationNotes ?? string.Empty;
        _loading = false;
        RefreshReconciliationGuidance(market);
    }

    private void MoneyBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_loading || MarketBox.SelectedValue is not int marketId)
            return;

        using var db = new AppDbContext();
        var market = db.MarketEvents.Find(marketId);
        if (market == null)
            return;

        market.CashSales = ParseMoney(CashSalesBox.Text);
        market.CardSales = ParseMoney(CardSalesBox.Text);
        market.OtherSales = ParseMoney(OtherSalesBox.Text);
        RefreshReconciliationGuidance(market);
    }

    private void RefreshReconciliationGuidance(MarketEvent market)
    {
        ReconciliationGuidanceText.Text = MarketProService.BuildMarketReconciliationGuidance(market, _selectedMarketStock);
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (MarketBox.SelectedValue is not int marketId)
        {
            MessageBox.Show("Choose a market first.", "Market Reconcile", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            using var db = new AppDbContext();
            var market = db.MarketEvents.Find(marketId) ?? throw new InvalidOperationException("Market event could not be found.");
            market.OpeningFloat = ParseMoney(OpeningFloatBox.Text);
            market.CashSales = ParseMoney(CashSalesBox.Text);
            market.CardSales = ParseMoney(CardSalesBox.Text);
            market.OtherSales = ParseMoney(OtherSalesBox.Text);
            market.StallFee = ParseMoney(StallFeeBox.Text);
            market.TravelCost = ParseMoney(TravelCostBox.Text);
            market.DisplayCost = ParseMoney(DisplayCostBox.Text);
            market.OtherCosts = ParseMoney(OtherCostsBox.Text);
            market.ReconciliationNotes = NotesBox.Text.Trim();
            db.SaveChanges();

            MarketProService.ReconcileMarket(marketId);
            DialogResult = true;
        }
        catch (Exception ex)
        {
            ErrorLogService.Log(ex, "Market reconcile");
            MessageBox.Show($"Could not reconcile the market.\n\n{ex.Message}", "Market Reconcile", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static decimal ParseMoney(string text)
    {
        if (decimal.TryParse(text, NumberStyles.Currency, CultureInfo.CurrentCulture, out var value))
            return value;
        if (decimal.TryParse(text, NumberStyles.Currency, CultureInfo.InvariantCulture, out value))
            return value;
        return 0m;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
