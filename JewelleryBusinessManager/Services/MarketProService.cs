using System.Diagnostics;
using System.IO;
using System.Text;
using JewelleryBusinessManager.Data;
using JewelleryBusinessManager.Models;

namespace JewelleryBusinessManager.Services;

public static class MarketProService
{
    public static string PrintoutFolder
    {
        get
        {
            var settings = BusinessSettingsService.Load();
            var folder = string.IsNullOrWhiteSpace(settings.PrintoutFolder)
                ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "JewelleryBusinessManager", "Printouts")
                : settings.PrintoutFolder;
            Directory.CreateDirectory(folder);
            return folder;
        }
    }

    public static MarketEvent? GetNearestMarket(AppDbContext db)
    {
        return db.MarketEvents
            .AsEnumerable()
            .OrderBy(m => m.EventDate < DateTime.Today)
            .ThenBy(m => Math.Abs((m.EventDate.Date - DateTime.Today).TotalDays))
            .FirstOrDefault();
    }

    public static void PrepareMarket(int marketEventId)
    {
        using var db = new AppDbContext();
        var market = db.MarketEvents.Find(marketEventId) ?? throw new InvalidOperationException("Market event could not be found.");
        var linkedStock = db.MarketStocks.Where(ms => ms.MarketEventId == market.Id).ToList();
        foreach (var stock in linkedStock.Where(s => s.Packed && s.PackedAt == null))
            stock.PackedAt = DateTime.Now;

        market.ItemsPacked = linkedStock.Count(s => s.Packed);
        if (string.IsNullOrWhiteSpace(market.PackingChecklist))
        {
            market.PackingChecklist = string.Join(Environment.NewLine, new[]
            {
                "☐ Jewellery stock selected and checked",
                "☐ Price tags / labels checked",
                "☐ Display trays and stands packed",
                "☐ Tablecloth packed",
                "☐ Mirror packed",
                "☐ Business cards / care cards packed",
                "☐ Packaging, bags and boxes packed",
                "☐ Payment reader charged",
                "☐ Cash float prepared",
                "☐ Cleaning cloth and basic tools packed",
                "☐ Market stock reconciled after event"
            });
        }
        db.SaveChanges();
    }

    public static void ReconcileMarket(int marketEventId)
    {
        using var db = new AppDbContext();
        var market = db.MarketEvents.Find(marketEventId) ?? throw new InvalidOperationException("Market event could not be found.");
        var linkedStock = db.MarketStocks.Where(ms => ms.MarketEventId == market.Id).ToList();

        market.ItemsPacked = linkedStock.Count(s => s.Packed);
        market.ItemsSold = linkedStock.Count(s => s.SoldAtMarket);
        market.ItemsReturned = linkedStock.Count(s => s.ReturnedToStock || (s.Packed && !s.SoldAtMarket));

        var stockSalesTotal = linkedStock.Sum(s => s.SalePrice);
        var paymentBreakdownTotal = market.CashSales + market.CardSales + market.OtherSales;
        market.TotalSales = stockSalesTotal > 0 ? stockSalesTotal : paymentBreakdownTotal;
        market.LastReconciledAt = DateTime.Now;

        var notes = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(market.ReconciliationNotes))
            notes.AppendLine(market.ReconciliationNotes.Trim());
        notes.AppendLine($"Reconciled {DateTime.Now:g}: Packed {market.ItemsPacked}, sold {market.ItemsSold}, returned {market.ItemsReturned}, takings {(market.TotalTakings > 0 ? market.TotalTakings : market.TotalSales):C}, costs {market.TotalCosts:C}, net {market.NetMarketProfit:C}.");
        market.ReconciliationNotes = notes.ToString().Trim();
        db.SaveChanges();
    }

    public static Sale CreateMarketSale(int marketStockId, decimal salePrice, PaymentMethod paymentMethod, int? customerId, string? notes)
    {
        using var db = new AppDbContext();
        var marketStock = db.MarketStocks.Find(marketStockId) ?? throw new InvalidOperationException("Market stock record could not be found.");
        var item = db.JewelleryItems.Find(marketStock.JewelleryItemId) ?? throw new InvalidOperationException("Linked jewellery item could not be found.");
        var market = db.MarketEvents.Find(marketStock.MarketEventId);

        var sale = new Sale
        {
            JewelleryItemId = item.Id,
            CustomerId = customerId,
            SaleDate = market?.EventDate.Date ?? DateTime.Today,
            SaleAmount = salePrice,
            CostOfGoods = item.TotalCost,
            PaymentMethod = paymentMethod,
            SaleLocation = SaleLocation.Market,
            Notes = string.IsNullOrWhiteSpace(notes)
                ? $"Market sale from {market?.Name ?? "market"}: {item.StockCode} {item.Name}".Trim()
                : notes.Trim()
        };

        db.Sales.Add(sale);
        db.SaveChanges();

        marketStock.SoldAtMarket = true;
        marketStock.SoldAt = DateTime.Now;
        marketStock.ReturnedToStock = false;
        marketStock.SalePrice = salePrice;
        marketStock.PaymentMethodText = paymentMethod.ToString();
        marketStock.SaleId = sale.Id;
        item.Status = StockStatus.Sold;

        if (market != null)
        {
            switch (paymentMethod)
            {
                case PaymentMethod.Cash:
                    market.CashSales += salePrice;
                    break;
                case PaymentMethod.Card:
                    market.CardSales += salePrice;
                    break;
                default:
                    market.OtherSales += salePrice;
                    break;
            }
        }

        db.SaveChanges();
        if (market != null)
            ReconcileMarket(market.Id);
        return sale;
    }

    public static string CreatePackingListReport(int marketEventId)
    {
        PrepareMarket(marketEventId);
        using var db = new AppDbContext();
        var market = db.MarketEvents.Find(marketEventId) ?? throw new InvalidOperationException("Market event could not be found.");
        var stock = db.MarketStocks.Where(ms => ms.MarketEventId == market.Id).AsEnumerable().ToList();
        var items = db.JewelleryItems.AsEnumerable().ToDictionary(i => i.Id, i => i);
        var path = Path.Combine(PrintoutFolder, $"MarketPackingList_{CleanFileName(market.Name)}_{DateTime.Now:yyyyMMdd_HHmmss}.html");

        var html = new StringBuilder();
        html.Append(HtmlHeader($"Market Packing List - {market.Name}"));
        html.AppendLine($"<h1>Market Packing List</h1><h2>{Html(market.Name)}</h2>");
        html.AppendLine($"<p><strong>Date:</strong> {Html(market.EventDate.ToShortDateString())} &nbsp; <strong>Location:</strong> {Html(market.Location ?? string.Empty)}</p>");
        html.AppendLine("<h2>Packing Checklist</h2><div class='box'>");
        foreach (var line in (market.PackingChecklist ?? string.Empty).Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            html.AppendLine($"<p>{Html(line)}</p>");
        html.AppendLine("</div>");
        html.AppendLine("<h2>Stock To Pack</h2><table><tr><th>Packed</th><th>Stock Code</th><th>Item</th><th>Price</th><th>Status</th><th>Notes</th></tr>");
        foreach (var row in stock.OrderBy(s => s.Packed).ThenBy(s => s.Id))
        {
            items.TryGetValue(row.JewelleryItemId, out var item);
            html.AppendLine($"<tr><td>{(row.Packed ? "☑" : "☐")}</td><td>{Html(item?.StockCode ?? string.Empty)}</td><td>{Html(item?.Name ?? $"Item #{row.JewelleryItemId}")}</td><td>{Money(item?.RetailPrice ?? 0)}</td><td>{Html(item?.Status.ToString() ?? string.Empty)}</td><td>{Html(row.Notes ?? string.Empty)}</td></tr>");
        }
        html.AppendLine("</table>");
        html.AppendLine(HtmlFooter());
        File.WriteAllText(path, html.ToString());
        return path;
    }

    public static string CreateReconciliationReport(int marketEventId)
    {
        ReconcileMarket(marketEventId);
        using var db = new AppDbContext();
        var market = db.MarketEvents.Find(marketEventId) ?? throw new InvalidOperationException("Market event could not be found.");
        var stock = db.MarketStocks.Where(ms => ms.MarketEventId == market.Id).AsEnumerable().ToList();
        var items = db.JewelleryItems.AsEnumerable().ToDictionary(i => i.Id, i => i);
        var sales = db.Sales.AsEnumerable().Where(s => s.SaleLocation == SaleLocation.Market && s.SaleDate.Date == market.EventDate.Date).ToList();
        var path = Path.Combine(PrintoutFolder, $"MarketReconciliation_{CleanFileName(market.Name)}_{DateTime.Now:yyyyMMdd_HHmmss}.html");

        var html = new StringBuilder();
        html.Append(HtmlHeader($"Market Reconciliation - {market.Name}"));
        html.AppendLine($"<h1>Market Reconciliation</h1><h2>{Html(market.Name)}</h2>");
        html.AppendLine($"<p><strong>Date:</strong> {Html(market.EventDate.ToShortDateString())} &nbsp; <strong>Location:</strong> {Html(market.Location ?? string.Empty)}</p>");
        html.AppendLine("<div class='grid'>");
        AddMetric(html, "Items Packed", market.ItemsPacked.ToString());
        AddMetric(html, "Items Sold", market.ItemsSold.ToString());
        AddMetric(html, "Items Returned", market.ItemsReturned.ToString());
        AddMetric(html, "Cash Sales", Money(market.CashSales));
        AddMetric(html, "Card Sales", Money(market.CardSales));
        AddMetric(html, "Other Sales", Money(market.OtherSales));
        AddMetric(html, "Total Takings", Money(market.TotalTakings > 0 ? market.TotalTakings : market.TotalSales));
        AddMetric(html, "Costs", Money(market.TotalCosts));
        AddMetric(html, "Net Estimate", Money(market.NetMarketProfit));
        html.AppendLine("</div>");
        html.AppendLine("<h2>Sold / Returned Stock</h2><table><tr><th>Stock</th><th>Item</th><th>Packed</th><th>Sold</th><th>Returned</th><th>Sale Price</th><th>Payment</th></tr>");
        foreach (var row in stock.OrderByDescending(s => s.SoldAtMarket).ThenBy(s => s.ReturnedToStock))
        {
            items.TryGetValue(row.JewelleryItemId, out var item);
            html.AppendLine($"<tr><td>{Html(item?.StockCode ?? string.Empty)}</td><td>{Html(item?.Name ?? $"Item #{row.JewelleryItemId}")}</td><td>{YesNo(row.Packed)}</td><td>{YesNo(row.SoldAtMarket)}</td><td>{YesNo(row.ReturnedToStock)}</td><td>{Money(row.SalePrice)}</td><td>{Html(row.PaymentMethodText ?? string.Empty)}</td></tr>");
        }
        html.AppendLine("</table>");
        html.AppendLine("<h2>Recorded Market Sales On Same Date</h2><table><tr><th>Date</th><th>Amount</th><th>Payment</th><th>Profit</th><th>Notes</th></tr>");
        foreach (var sale in sales)
            html.AppendLine($"<tr><td>{Html(sale.SaleDate.ToShortDateString())}</td><td>{Money(sale.SaleAmount)}</td><td>{Html(sale.PaymentMethod.ToString())}</td><td>{Money(sale.Profit)}</td><td>{Html(sale.Notes ?? string.Empty)}</td></tr>");
        html.AppendLine("</table>");
        html.AppendLine($"<h2>Reconciliation Notes</h2><p>{Html(market.ReconciliationNotes ?? string.Empty).Replace(Environment.NewLine, "<br>")}</p>");
        html.AppendLine(HtmlFooter());
        File.WriteAllText(path, html.ToString());
        return path;
    }

    public static void OpenInDefaultApp(string path)
    {
        Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
    }

    private static void AddMetric(StringBuilder html, string label, string value) => html.AppendLine($"<div class='metric'><span>{Html(label)}</span><strong>{Html(value)}</strong></div>");
    private static string Money(decimal value) => value.ToString("C");
    private static string YesNo(bool value) => value ? "Yes" : "No";
    private static string CleanFileName(string name) => string.Join("_", (string.IsNullOrWhiteSpace(name) ? "Market" : name).Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries)).Trim();
    private static string Html(string value) => System.Net.WebUtility.HtmlEncode(value ?? string.Empty);

    private static string HtmlHeader(string title)
    {
        var settings = BusinessSettingsService.Load();
        var business = string.IsNullOrWhiteSpace(settings.BusinessName) ? "OPALNOVA" : settings.BusinessName;

        var html = new StringBuilder();
        html.AppendLine("<!doctype html>");
        html.AppendLine("<html><head><meta charset=\"utf-8\"><title>" + Html(title) + "</title>");
        html.AppendLine("<style>");
        html.AppendLine("body { font-family: Segoe UI, Arial, sans-serif; margin: 28px; color: #1f2937; }");
        html.AppendLine("h1 { margin-bottom: 0; } h2 { color: #374151; margin-top: 22px; }");
        html.AppendLine("table { border-collapse: collapse; width: 100%; margin-top: 10px; }");
        html.AppendLine("th, td { border: 1px solid #d1d5db; padding: 7px; text-align: left; vertical-align: top; }");
        html.AppendLine("th { background: #f3f4f6; }");
        html.AppendLine(".box { border: 1px solid #d1d5db; padding: 10px 14px; border-radius: 8px; background: #f9fafb; }");
        html.AppendLine(".grid { display: grid; grid-template-columns: repeat(3, 1fr); gap: 10px; margin: 14px 0; }");
        html.AppendLine(".metric { border: 1px solid #d1d5db; border-radius: 8px; padding: 10px; background: #f9fafb; }");
        html.AppendLine(".metric span { display: block; color: #6b7280; font-size: 12px; }");
        html.AppendLine(".metric strong { font-size: 20px; }");
        html.AppendLine(".footer { margin-top: 28px; color: #6b7280; font-size: 12px; }");
        html.AppendLine("@media print { body { margin: 12mm; } }");
        html.AppendLine("</style></head><body>");
        html.AppendLine("<div class='footer'>" + Html(business) + "</div>");
        return html.ToString();
    }

    private static string HtmlFooter()
    {
        var settings = BusinessSettingsService.Load();
        var footer = string.IsNullOrWhiteSpace(settings.DocumentFooterText) ? "Generated by OPALNOVA" : settings.DocumentFooterText;
        return $"<div class='footer'>{Html(footer)}<br>Generated {DateTime.Now:g}</div></body></html>";
    }
}
