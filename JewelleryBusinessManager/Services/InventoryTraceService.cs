using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using JewelleryBusinessManager.Data;
using JewelleryBusinessManager.Models;

namespace JewelleryBusinessManager.Services;

public static class InventoryTraceService
{
    public static string BuildTraceText(AppDbContext db, object selected)
    {
        var type = selected.GetType();
        var idProperty = type.GetProperty("Id");
        if (idProperty == null) return "Selected record has no Id and cannot be traced.";
        var id = (int)idProperty.GetValue(selected)!;

        var sb = new StringBuilder();
        sb.AppendLine("OPALNOVA - TRACEABILITY VIEW");
        sb.AppendLine($"Created: {DateTime.Now:g}");
        sb.AppendLine(new string('=', 78));

        if (type == typeof(Customer)) TraceCustomer(db, id, sb);
        else if (type == typeof(Material)) TraceMaterial(db, id, sb);
        else if (type == typeof(MaterialTransaction)) TraceMaterialTransaction(db, id, sb);
        else if (type == typeof(OpalParcel)) TraceOpalParcel(db, id, sb);
        else if (type == typeof(Stone)) TraceStone(db, id, sb);
        else if (type == typeof(JewelleryItem)) TraceJewelleryItem(db, id, sb);
        else if (type == typeof(Job)) TraceJob(db, id, sb);
        else if (type == typeof(Sale)) TraceSale(db, id, sb);
        else if (type == typeof(Payment)) TracePayment(db, id, sb);
        else if (type == typeof(MarketEvent)) TraceMarketEvent(db, id, sb);
        else if (type == typeof(MarketStock)) TraceMarketStock(db, id, sb);
        else if (type == typeof(ProductionBatch)) TraceProductionBatch(db, id, sb);
        else if (type == typeof(ProductionBatchItem)) TraceProductionBatchItem(db, id, sb);
        else sb.AppendLine($"Trace view is not yet available for {type.Name}.");

        return sb.ToString();
    }

    public static string CreateInventoryAuditReport(AppDbContext db)
    {
        var settings = BusinessSettingsService.Load();
        var folder = BusinessSettingsService.GetPrintoutFolder();
        Directory.CreateDirectory(folder);
        var fileName = $"Inventory-Audit-{DateTime.Now:yyyyMMdd-HHmmss}.html";
        var path = Path.Combine(folder, fileName);

        var lowMaterials = db.Materials.AsEnumerable().Where(m => m.CurrentQuantity <= m.ReorderLevel).OrderBy(m => m.Category).ThenBy(m => m.Name).ToList();
        var recentMovements = db.MaterialTransactions.AsEnumerable().OrderByDescending(t => t.TransactionDate).ThenByDescending(t => t.Id).Take(50).ToList();
        var reservedItems = db.JewelleryItems.AsEnumerable().Where(j => j.Status == StockStatus.Reserved).OrderBy(j => j.StockCode).ToList();
        var atMarketItems = db.JewelleryItems.AsEnumerable().Where(j => j.Status == StockStatus.AtMarket).OrderBy(j => j.StockCode).ToList();
        var dueJobs = db.Jobs.AsEnumerable().Where(j => j.DueDate.HasValue && j.DueDate.Value.Date <= DateTime.Today.AddDays(14) && j.Status != JobStatus.Completed && j.Status != JobStatus.Cancelled).OrderBy(j => j.DueDate).ToList();
        var looseStones = db.Stones.AsEnumerable().Where(s => s.Status == StoneStatus.Loose || s.Status == StoneStatus.Reserved).OrderBy(s => s.StoneCode).ToList();

        var html = new StringBuilder();
        html.AppendLine("<!doctype html><html><head><meta charset='utf-8'>");
        html.AppendLine("<title>Inventory Audit Report</title>");
        html.AppendLine("<style>");
        html.AppendLine("body { font-family: Arial, sans-serif; margin: 24px; color: #222; }");
        html.AppendLine("h1 { margin-bottom: 4px; } h2 { margin-top: 24px; }");
        html.AppendLine(".summary { display: grid; grid-template-columns: repeat(4, 1fr); gap: 10px; margin: 18px 0; }");
        html.AppendLine(".tile { border: 1px solid #ddd; padding: 12px; border-radius: 8px; }");
        html.AppendLine(".big { font-size: 24px; font-weight: bold; }");
        html.AppendLine("table { border-collapse: collapse; width: 100%; font-size: 12px; margin-bottom: 18px; }");
        html.AppendLine("th, td { border: 1px solid #ddd; padding: 7px; text-align: left; vertical-align: top; }");
        html.AppendLine("th { background: #f3f3f3; } .muted { color: #666; } .warn { color: #9a5b00; font-weight: bold; }");
        html.AppendLine("@media print { button { display: none; } body { margin: 8mm; } }");
        html.AppendLine("</style></head><body><button onclick='window.print()'>Print</button>");
        html.AppendLine($"<h1>{Html(settings.BusinessName)} - Inventory Audit Report</h1>");
        html.AppendLine($"<div class='muted'>Created {DateTime.Now:g}</div>");
        html.AppendLine("<div class='summary'>");
        Tile(html, "Low materials", lowMaterials.Count.ToString(CultureInfo.CurrentCulture));
        Tile(html, "Recent movements", recentMovements.Count.ToString(CultureInfo.CurrentCulture));
        Tile(html, "Reserved items", reservedItems.Count.ToString(CultureInfo.CurrentCulture));
        Tile(html, "At market", atMarketItems.Count.ToString(CultureInfo.CurrentCulture));
        html.AppendLine("</div>");

        html.AppendLine("<h2>Low Materials / Reorder List</h2>");
        html.AppendLine("<table><tr><th>Code</th><th>Name</th><th>Category</th><th>Current</th><th>Reorder Level</th><th>Supplier</th><th>Storage</th></tr>");
        foreach (var m in lowMaterials)
        {
            var supplier = m.SupplierId.HasValue ? db.Suppliers.Find(m.SupplierId.Value)?.Name ?? string.Empty : string.Empty;
            html.AppendLine($"<tr><td>{Html(m.MaterialCode)}</td><td>{Html(m.Name)}</td><td>{m.Category}</td><td class='warn'>{m.CurrentQuantity:0.###} {m.UnitType}</td><td>{m.ReorderLevel:0.###}</td><td>{Html(supplier)}</td><td>{Html(m.StorageLocation ?? string.Empty)}</td></tr>");
        }
        html.AppendLine("</table>");

        html.AppendLine("<h2>Recent Material Movements</h2>");
        html.AppendLine("<table><tr><th>Date</th><th>Material</th><th>Qty Change</th><th>Reason</th><th>Job</th><th>Jewellery</th><th>Notes</th></tr>");
        foreach (var t in recentMovements)
        {
            var material = db.Materials.Find(t.MaterialId)?.ToString() ?? $"Material #{t.MaterialId}";
            var job = t.JobId.HasValue ? db.Jobs.Find(t.JobId.Value)?.ToString() ?? string.Empty : string.Empty;
            var item = t.JewelleryItemId.HasValue ? db.JewelleryItems.Find(t.JewelleryItemId.Value)?.ToString() ?? string.Empty : string.Empty;
            html.AppendLine($"<tr><td>{t.TransactionDate:d}</td><td>{Html(material)}</td><td>{t.QuantityChange:0.###}</td><td>{Html(t.Reason)}</td><td>{Html(job)}</td><td>{Html(item)}</td><td>{Html(t.Notes ?? string.Empty)}</td></tr>");
        }
        html.AppendLine("</table>");

        AppendItemsTable(html, "Reserved Jewellery", reservedItems);
        AppendItemsTable(html, "At-Market Jewellery", atMarketItems);

        html.AppendLine("<h2>Jobs Due In Next 14 Days</h2>");
        html.AppendLine("<table><tr><th>Due</th><th>Job</th><th>Status</th><th>Customer</th><th>Balance</th></tr>");
        foreach (var job in dueJobs)
        {
            var customer = job.CustomerId.HasValue ? db.Customers.Find(job.CustomerId.Value)?.FullName ?? string.Empty : string.Empty;
            html.AppendLine($"<tr><td>{job.DueDate:d}</td><td>{Html(job.ToString())}</td><td>{job.Status}</td><td>{Html(customer)}</td><td>{job.BalanceOwing:C}</td></tr>");
        }
        html.AppendLine("</table>");

        html.AppendLine("<h2>Loose / Reserved Stones</h2>");
        html.AppendLine("<table><tr><th>Code</th><th>Type</th><th>Weight</th><th>Status</th><th>Value</th><th>Parcel</th></tr>");
        foreach (var stone in looseStones)
        {
            var parcel = stone.OpalParcelId.HasValue ? db.OpalParcels.Find(stone.OpalParcelId.Value)?.ToString() ?? string.Empty : string.Empty;
            html.AppendLine($"<tr><td>{Html(stone.StoneCode)}</td><td>{Html(stone.StoneType)}</td><td>{stone.WeightCarats:0.###} ct</td><td>{stone.Status}</td><td>{stone.EstimatedValue:C}</td><td>{Html(parcel)}</td></tr>");
        }
        html.AppendLine("</table>");
        html.AppendLine("</body></html>");
        File.WriteAllText(path, html.ToString());
        return path;
    }

    private static void TraceCustomer(AppDbContext db, int id, StringBuilder sb)
    {
        var customer = db.Customers.Find(id);
        if (customer == null) { sb.AppendLine("Customer not found."); return; }
        Header(sb, "CUSTOMER", customer.ToString());
        Line(sb, "Email", customer.Email);
        Line(sb, "Phone", customer.Phone);
        Line(sb, "Instagram", customer.InstagramHandle);
        var jobs = db.Jobs.AsEnumerable().Where(j => j.CustomerId == id).OrderByDescending(j => j.DateReceived).ToList();
        Section(sb, "Jobs");
        foreach (var job in jobs) sb.AppendLine($"- {job.JobCode} {job.JobTitle} | {job.Status} | Due {job.DueDate:d} | Balance {job.BalanceOwing:C}");
        var sales = db.Sales.AsEnumerable().Where(s => s.CustomerId == id).OrderByDescending(s => s.SaleDate).ToList();
        Section(sb, "Sales");
        foreach (var sale in sales) sb.AppendLine($"- {sale.SaleDate:d} | {sale.SaleAmount:C} | {sale.SaleLocation} | {sale.Notes}");
    }

    private static void TraceMaterial(AppDbContext db, int id, StringBuilder sb)
    {
        var material = db.Materials.Find(id);
        if (material == null) { sb.AppendLine("Material not found."); return; }
        Header(sb, "MATERIAL", material.ToString());
        Line(sb, "Category", material.Category.ToString());
        Line(sb, "Current Quantity", $"{material.CurrentQuantity:0.###} {material.UnitType}");
        Line(sb, "Reorder Level", material.ReorderLevel.ToString("0.###", CultureInfo.CurrentCulture));
        Line(sb, "Storage", material.StorageLocation);
        if (material.SupplierId.HasValue) Line(sb, "Supplier", db.Suppliers.Find(material.SupplierId.Value)?.ToString());
        var transactions = db.MaterialTransactions.AsEnumerable().Where(t => t.MaterialId == id).OrderByDescending(t => t.TransactionDate).ThenByDescending(t => t.Id).ToList();
        Section(sb, "Material Movements");
        foreach (var t in transactions)
        {
            var job = t.JobId.HasValue ? db.Jobs.Find(t.JobId.Value)?.ToString() : null;
            var item = t.JewelleryItemId.HasValue ? db.JewelleryItems.Find(t.JewelleryItemId.Value)?.ToString() : null;
            sb.AppendLine($"- {t.TransactionDate:d} | {t.QuantityChange:0.###} | {t.Reason} | Job: {job ?? "-"} | Item: {item ?? "-"} | {t.Notes}");
        }
    }

    private static void TraceMaterialTransaction(AppDbContext db, int id, StringBuilder sb)
    {
        var t = db.MaterialTransactions.Find(id);
        if (t == null) { sb.AppendLine("Material transaction not found."); return; }
        Header(sb, "MATERIAL TRANSACTION", $"Movement #{t.Id}");
        Line(sb, "Date", t.TransactionDate.ToShortDateString());
        Line(sb, "Material", db.Materials.Find(t.MaterialId)?.ToString());
        Line(sb, "Quantity Change", t.QuantityChange.ToString("0.###", CultureInfo.CurrentCulture));
        Line(sb, "Reason", t.Reason);
        if (t.JobId.HasValue) Line(sb, "Job", db.Jobs.Find(t.JobId.Value)?.ToString());
        if (t.JewelleryItemId.HasValue) Line(sb, "Jewellery", db.JewelleryItems.Find(t.JewelleryItemId.Value)?.ToString());
        Line(sb, "Notes", t.Notes);
    }

    private static void TraceOpalParcel(AppDbContext db, int id, StringBuilder sb)
    {
        var parcel = db.OpalParcels.Find(id);
        if (parcel == null) { sb.AppendLine("Opal parcel not found."); return; }
        Header(sb, "OPAL PARCEL", parcel.ToString());
        Line(sb, "Supplier", parcel.SupplierId.HasValue ? db.Suppliers.Find(parcel.SupplierId.Value)?.ToString() : null);
        Line(sb, "Purchase Cost", parcel.TotalCost.ToString("C", CultureInfo.CurrentCulture));
        Line(sb, "Starting Weight", parcel.StartingWeightCarats.ToString("0.###", CultureInfo.CurrentCulture));
        Line(sb, "Actual Yield", parcel.ActualYieldCarats.ToString("0.###", CultureInfo.CurrentCulture));
        var stones = db.Stones.AsEnumerable().Where(s => s.OpalParcelId == id).OrderBy(s => s.StoneCode).ToList();
        Section(sb, "Cut Stones From Parcel");
        foreach (var stone in stones) sb.AppendLine($"- {stone.StoneCode} | {stone.WeightCarats:0.###} ct | {stone.Status} | {stone.EstimatedValue:C}");
    }

    private static void TraceStone(AppDbContext db, int id, StringBuilder sb)
    {
        var stone = db.Stones.Find(id);
        if (stone == null) { sb.AppendLine("Stone not found."); return; }
        Header(sb, "STONE", stone.ToString());
        Line(sb, "Weight", $"{stone.WeightCarats:0.###} ct");
        Line(sb, "Status", stone.Status.ToString());
        Line(sb, "Pattern", stone.Pattern);
        Line(sb, "Brightness", stone.Brightness);
        Line(sb, "Body Tone", stone.BodyTone);
        if (stone.OpalParcelId.HasValue) Line(sb, "Source Parcel", db.OpalParcels.Find(stone.OpalParcelId.Value)?.ToString());
        var items = db.JewelleryItems.AsEnumerable().Where(j => j.MainStoneId == id).OrderBy(j => j.StockCode).ToList();
        Section(sb, "Jewellery Using This Stone");
        foreach (var item in items) sb.AppendLine($"- {item.StockCode} {item.Name} | {item.Status} | Retail {item.RetailPrice:C}");
    }

    private static void TraceJewelleryItem(AppDbContext db, int id, StringBuilder sb)
    {
        var item = db.JewelleryItems.Find(id);
        if (item == null) { sb.AppendLine("Jewellery item not found."); return; }
        Header(sb, "JEWELLERY ITEM", item.ToString());
        Line(sb, "Status", item.Status.ToString());
        Line(sb, "Retail", item.RetailPrice.ToString("C", CultureInfo.CurrentCulture));
        Line(sb, "Cost", item.TotalCost.ToString("C", CultureInfo.CurrentCulture));
        if (item.MainStoneId.HasValue) Line(sb, "Main Stone", db.Stones.Find(item.MainStoneId.Value)?.ToString());
        var movements = db.MaterialTransactions.AsEnumerable().Where(t => t.JewelleryItemId == id).OrderByDescending(t => t.TransactionDate).ToList();
        Section(sb, "Linked Material Movements");
        foreach (var t in movements) sb.AppendLine($"- {t.TransactionDate:d} | {db.Materials.Find(t.MaterialId)} | {t.QuantityChange:0.###} | {t.Reason}");
        var sales = db.Sales.AsEnumerable().Where(s => s.JewelleryItemId == id).OrderByDescending(s => s.SaleDate).ToList();
        Section(sb, "Sales");
        foreach (var sale in sales) sb.AppendLine($"- {sale.SaleDate:d} | {sale.SaleAmount:C} | Profit {sale.Profit:C} | {sale.SaleLocation}");
        var marketRows = db.MarketStocks.AsEnumerable().Where(m => m.JewelleryItemId == id).ToList();
        Section(sb, "Market History");
        foreach (var row in marketRows) sb.AppendLine($"- {db.MarketEvents.Find(row.MarketEventId)} | Packed: {row.Packed} | Sold: {row.SoldAtMarket}");
    }

    private static void TraceJob(AppDbContext db, int id, StringBuilder sb)
    {
        var job = db.Jobs.Find(id);
        if (job == null) { sb.AppendLine("Job not found."); return; }
        Header(sb, "JOB", job.ToString());
        if (job.CustomerId.HasValue) Line(sb, "Customer", db.Customers.Find(job.CustomerId.Value)?.ToString());
        Line(sb, "Status", job.Status.ToString());
        Line(sb, "Due Date", job.DueDate?.ToShortDateString());
        Line(sb, "Quote", job.QuoteAmount.ToString("C", CultureInfo.CurrentCulture));
        Line(sb, "Final Price", job.FinalPrice.ToString("C", CultureInfo.CurrentCulture));
        Line(sb, "Balance", job.BalanceOwing.ToString("C", CultureInfo.CurrentCulture));
        var movements = db.MaterialTransactions.AsEnumerable().Where(t => t.JobId == id).OrderByDescending(t => t.TransactionDate).ToList();
        Section(sb, "Material Movements");
        foreach (var t in movements) sb.AppendLine($"- {t.TransactionDate:d} | {db.Materials.Find(t.MaterialId)} | {t.QuantityChange:0.###} | {t.Reason}");
        var payments = db.Payments.AsEnumerable().Where(p => p.JobId == id).OrderByDescending(p => p.PaymentDate).ToList();
        Section(sb, "Payments");
        foreach (var payment in payments) sb.AppendLine($"- {payment.PaymentDate:d} | {payment.Amount:C} | {payment.Method} | {payment.Notes}");
        var sales = db.Sales.AsEnumerable().Where(s => s.JobId == id).OrderByDescending(s => s.SaleDate).ToList();
        Section(sb, "Sales");
        foreach (var sale in sales) sb.AppendLine($"- {sale.SaleDate:d} | {sale.SaleAmount:C} | Profit {sale.Profit:C}");
    }

    private static void TraceSale(AppDbContext db, int id, StringBuilder sb)
    {
        var sale = db.Sales.Find(id);
        if (sale == null) { sb.AppendLine("Sale not found."); return; }
        Header(sb, "SALE", $"Sale #{sale.Id}");
        Line(sb, "Date", sale.SaleDate.ToShortDateString());
        Line(sb, "Amount", sale.SaleAmount.ToString("C", CultureInfo.CurrentCulture));
        Line(sb, "Profit", sale.Profit.ToString("C", CultureInfo.CurrentCulture));
        if (sale.CustomerId.HasValue) Line(sb, "Customer", db.Customers.Find(sale.CustomerId.Value)?.ToString());
        if (sale.JobId.HasValue) Line(sb, "Job", db.Jobs.Find(sale.JobId.Value)?.ToString());
        if (sale.JewelleryItemId.HasValue) Line(sb, "Jewellery", db.JewelleryItems.Find(sale.JewelleryItemId.Value)?.ToString());
    }

    private static void TracePayment(AppDbContext db, int id, StringBuilder sb)
    {
        var payment = db.Payments.Find(id);
        if (payment == null) { sb.AppendLine("Payment not found."); return; }
        Header(sb, "PAYMENT", $"Payment #{payment.Id}");
        Line(sb, "Date", payment.PaymentDate.ToShortDateString());
        Line(sb, "Amount", payment.Amount.ToString("C", CultureInfo.CurrentCulture));
        Line(sb, "Method", payment.Method.ToString());
        if (payment.JobId.HasValue) Line(sb, "Job", db.Jobs.Find(payment.JobId.Value)?.ToString());
        if (payment.SaleId.HasValue) Line(sb, "Sale", $"Sale #{payment.SaleId.Value}");
    }

    private static void TraceMarketEvent(AppDbContext db, int id, StringBuilder sb)
    {
        var market = db.MarketEvents.Find(id);
        if (market == null) { sb.AppendLine("Market event not found."); return; }
        Header(sb, "MARKET EVENT", market.ToString());
        var stock = db.MarketStocks.AsEnumerable().Where(m => m.MarketEventId == id).ToList();
        Line(sb, "Stock Count", stock.Count.ToString(CultureInfo.CurrentCulture));
        Section(sb, "Market Stock");
        foreach (var row in stock) sb.AppendLine($"- {db.JewelleryItems.Find(row.JewelleryItemId)} | Packed: {row.Packed} | Sold: {row.SoldAtMarket}");
    }

    private static void TraceMarketStock(AppDbContext db, int id, StringBuilder sb)
    {
        var row = db.MarketStocks.Find(id);
        if (row == null) { sb.AppendLine("Market stock row not found."); return; }
        Header(sb, "MARKET STOCK", $"Market Stock #{row.Id}");
        Line(sb, "Market", db.MarketEvents.Find(row.MarketEventId)?.ToString());
        Line(sb, "Jewellery", db.JewelleryItems.Find(row.JewelleryItemId)?.ToString());
        Line(sb, "Packed", row.Packed.ToString());
        Line(sb, "Sold At Market", row.SoldAtMarket.ToString());
    }


    private static void TraceProductionBatch(AppDbContext db, int id, StringBuilder sb)
    {
        var batch = db.ProductionBatches.Find(id);
        if (batch == null) { sb.AppendLine("Production batch not found."); return; }
        Header(sb, "PRODUCTION BATCH", batch.ToString());
        Line(sb, "Collection", batch.CollectionName);
        Line(sb, "Status", batch.Status.ToString());
        Line(sb, "Start", batch.StartDate.ToShortDateString());
        Line(sb, "Target", batch.TargetCompletionDate?.ToShortDateString());
        if (batch.MarketEventId.HasValue) Line(sb, "Linked Market", db.MarketEvents.Find(batch.MarketEventId.Value)?.ToString());
        Line(sb, "Progress", $"{batch.CompletedPieces}/{batch.PlannedPieces} pieces ({batch.ProgressPercent:P0})");
        Line(sb, "Estimated Retail", batch.EstimatedRetailValue.ToString("C", CultureInfo.CurrentCulture));
        Line(sb, "Estimated Material Cost", batch.EstimatedMaterialCost.ToString("C", CultureInfo.CurrentCulture));
        var items = db.ProductionBatchItems.AsEnumerable().Where(i => i.ProductionBatchId == id).OrderBy(i => i.Status).ThenBy(i => i.ItemName).ToList();
        Section(sb, "Batch Items");
        foreach (var item in items)
        {
            var links = new List<string>();
            if (item.JewelleryItemId.HasValue) links.Add($"Jewellery: {db.JewelleryItems.Find(item.JewelleryItemId.Value)}");
            if (item.StoneId.HasValue) links.Add($"Stone: {db.Stones.Find(item.StoneId.Value)}");
            if (item.JobId.HasValue) links.Add($"Job: {db.Jobs.Find(item.JobId.Value)}");
            sb.AppendLine($"- {item.ItemName} | {item.Status} | {item.CompletedQuantity:0.###}/{item.PlannedQuantity:0.###} | Retail {item.EstimatedRetailValue:C} | {string.Join("; ", links)}");
        }
    }

    private static void TraceProductionBatchItem(AppDbContext db, int id, StringBuilder sb)
    {
        var item = db.ProductionBatchItems.Find(id);
        if (item == null) { sb.AppendLine("Production batch item not found."); return; }
        Header(sb, "PRODUCTION BATCH ITEM", item.ToString());
        Line(sb, "Batch", db.ProductionBatches.Find(item.ProductionBatchId)?.ToString());
        Line(sb, "Type", item.ItemType);
        Line(sb, "Status", item.Status);
        Line(sb, "Progress", $"{item.CompletedQuantity:0.###}/{item.PlannedQuantity:0.###} ({item.ProgressPercent:P0})");
        Line(sb, "Estimated Cost", item.EstimatedCost.ToString("C", CultureInfo.CurrentCulture));
        Line(sb, "Estimated Retail", item.EstimatedRetailValue.ToString("C", CultureInfo.CurrentCulture));
        if (item.JewelleryItemId.HasValue) Line(sb, "Jewellery", db.JewelleryItems.Find(item.JewelleryItemId.Value)?.ToString());
        if (item.StoneId.HasValue) Line(sb, "Stone", db.Stones.Find(item.StoneId.Value)?.ToString());
        if (item.JobId.HasValue) Line(sb, "Job", db.Jobs.Find(item.JobId.Value)?.ToString());
        Line(sb, "Notes", item.Notes);
    }

    private static void Header(StringBuilder sb, string kind, string? title)
    {
        sb.AppendLine($"{kind}: {title}");
        sb.AppendLine(new string('-', 78));
    }

    private static void Section(StringBuilder sb, string title)
    {
        sb.AppendLine();
        sb.AppendLine(title.ToUpperInvariant());
        sb.AppendLine(new string('-', title.Length));
    }

    private static void Line(StringBuilder sb, string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value)) sb.AppendLine($"{key}: {value}");
    }

    private static void Tile(StringBuilder html, string title, string value)
    {
        html.AppendLine($"<div class='tile'><div class='muted'>{Html(title)}</div><div class='big'>{Html(value)}</div></div>");
    }

    private static void AppendItemsTable(StringBuilder html, string title, List<JewelleryItem> items)
    {
        html.AppendLine($"<h2>{Html(title)}</h2>");
        html.AppendLine("<table><tr><th>Stock Code</th><th>Name</th><th>Type</th><th>Metal</th><th>Retail</th><th>Status</th></tr>");
        foreach (var item in items)
            html.AppendLine($"<tr><td>{Html(item.StockCode)}</td><td>{Html(item.Name)}</td><td>{item.Type}</td><td>{Html(item.Metal ?? string.Empty)}</td><td>{item.RetailPrice:C}</td><td>{item.Status}</td></tr>");
        html.AppendLine("</table>");
    }

    private static string Html(string value) => WebUtility.HtmlEncode(value);
}
