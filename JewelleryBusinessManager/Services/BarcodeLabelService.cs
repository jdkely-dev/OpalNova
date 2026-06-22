using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using JewelleryBusinessManager.Data;
using JewelleryBusinessManager.Models;

namespace JewelleryBusinessManager.Services;

public sealed record ScanLabelItem(string RecordType, int Id, string Code, string Title, string Subtitle, decimal? Price = null, string? Status = null);

public static class BarcodeLabelService
{
    private static readonly Dictionary<char, string> Code39Patterns = new()
    {
        ['0'] = "nnnwwnwnn", ['1'] = "wnnwnnnnw", ['2'] = "nnwwnnnnw", ['3'] = "wnwwnnnnn", ['4'] = "nnnwwnnnw",
        ['5'] = "wnnwwnnnn", ['6'] = "nnwwwnnnn", ['7'] = "nnnwnnwnw", ['8'] = "wnnwnnwnn", ['9'] = "nnwwnnwnn",
        ['A'] = "wnnnnwnnw", ['B'] = "nnwnnwnnw", ['C'] = "wnwnnwnnn", ['D'] = "nnnnwwnnw", ['E'] = "wnnnwwnnn",
        ['F'] = "nnwnwwnnn", ['G'] = "nnnnnwwnw", ['H'] = "wnnnnwwnn", ['I'] = "nnwnnwwnn", ['J'] = "nnnnwwwnn",
        ['K'] = "wnnnnnnww", ['L'] = "nnwnnnnww", ['M'] = "wnwnnnnwn", ['N'] = "nnnnwnnww", ['O'] = "wnnnwnnwn",
        ['P'] = "nnwnwnnwn", ['Q'] = "nnnnnnwww", ['R'] = "wnnnnnwwn", ['S'] = "nnwnnnwwn", ['T'] = "nnnnwnwwn",
        ['U'] = "wwnnnnnnw", ['V'] = "nwwnnnnnw", ['W'] = "wwwnnnnnn", ['X'] = "nwnnwnnnw", ['Y'] = "wwnnwnnnn",
        ['Z'] = "nwwnwnnnn", ['-'] = "nwnnnnwnw", ['.'] = "wwnnnnwnn", [' '] = "nwwnnnwnn", ['$'] = "nwnwnwnnn",
        ['/'] = "nwnwnnnwn", ['+'] = "nwnnnwnwn", ['%'] = "nnnwnwnwn", ['*'] = "nwnnwnwnn"
    };

    public static string GenerateSingleLabel(ScanLabelItem item)
    {
        Directory.CreateDirectory(PrintoutFolder);
        var path = Path.Combine(PrintoutFolder, SafeFileName($"ScanLabel_{item.RecordType}_{item.Code}_{item.Id}.html"));
        File.WriteAllText(path, BuildLabelDocument(new[] { item }, $"Scan Label — {item.Code}", single: true));
        return path;
    }

    public static string GenerateLabelSheet(IEnumerable<ScanLabelItem> items, string title)
    {
        Directory.CreateDirectory(PrintoutFolder);
        var cleanTitle = string.IsNullOrWhiteSpace(title) ? "ScanLabelSheet" : title.Replace(' ', '_');
        var path = Path.Combine(PrintoutFolder, SafeFileName($"{cleanTitle}_{DateTime.Now:yyyyMMdd_HHmmss}.html"));
        File.WriteAllText(path, BuildLabelDocument(items, title, single: false));
        return path;
    }

    public static ScanLabelItem? FromRecord(object record)
    {
        return record switch
        {
            JewelleryItem item => new ScanLabelItem("Jewellery", item.Id, EnsureCode(item.StockCode, "JWL", item.Id), item.Name, $"{item.Type} • {item.Metal} • {item.Status}", item.RetailPrice, item.Status.ToString()),
            Stone stone => new ScanLabelItem("Stone", stone.Id, EnsureCode(stone.StoneCode, "STN", stone.Id), stone.StoneType, $"{stone.WeightCarats:0.###}ct • {stone.Shape} • {stone.Status}", stone.EstimatedValue, stone.Status.ToString()),
            Job job => new ScanLabelItem("Job", job.Id, EnsureCode(job.JobCode, "JOB", job.Id), job.JobTitle, $"{job.Type} • {job.Status} • Due {job.DueDate?.ToShortDateString() ?? "TBC"}", job.FinalPrice > 0 ? job.FinalPrice : job.QuoteAmount, job.Status.ToString()),
            Material material => new ScanLabelItem("Material", material.Id, EnsureCode(material.MaterialCode, "MAT", material.Id), material.Name, $"{material.Category} • {material.CurrentQuantity:0.###} {material.UnitType}", material.PurchaseCost, material.Category.ToString()),
            PurchaseOrder po => new ScanLabelItem("Purchase Order", po.Id, EnsureCode(po.PurchaseOrderCode, "PO", po.Id), po.ToString(), $"{po.Status} • Expected {po.ExpectedDeliveryDate?.ToShortDateString() ?? "TBC"}", po.TotalCost, po.Status.ToString()),
            ProductionBatch batch => new ScanLabelItem("Production Batch", batch.Id, EnsureCode(batch.BatchCode, "BAT", batch.Id), batch.Name, $"{batch.Status} • {batch.CompletedPieces}/{batch.PlannedPieces} complete", batch.EstimatedRetailValue, batch.Status.ToString()),
            BusinessTask task => new ScanLabelItem("Task", task.Id, EnsureCode(task.TaskCode, "TSK", task.Id), task.Title, $"{task.Priority} • {task.Status} • Due {task.DueDate?.ToShortDateString() ?? "TBC"}", null, task.Status.ToString()),
            MarketStock marketStock => new ScanLabelItem("Market Stock", marketStock.Id, EnsureCode($"MKT-{marketStock.Id:0000}", "MKT", marketStock.Id), $"Market Stock #{marketStock.Id}", $"Jewellery Item #{marketStock.JewelleryItemId} • Packed {marketStock.Packed}", marketStock.SalePrice, marketStock.SoldAtMarket ? "Sold" : "Market"),
            _ => null
        };
    }

    public static string AssignMissingCodes()
    {
        using var db = new AppDbContext();
        var updates = new StringBuilder();
        var count = 0;

        foreach (var item in db.JewelleryItems.AsEnumerable().Where(x => string.IsNullOrWhiteSpace(x.StockCode)))
        {
            item.StockCode = NextCode("JWL", item.Id);
            updates.AppendLine($"Jewellery #{item.Id} → {item.StockCode}");
            count++;
        }
        foreach (var stone in db.Stones.AsEnumerable().Where(x => string.IsNullOrWhiteSpace(x.StoneCode)))
        {
            stone.StoneCode = NextCode("STN", stone.Id);
            updates.AppendLine($"Stone #{stone.Id} → {stone.StoneCode}");
            count++;
        }
        foreach (var job in db.Jobs.AsEnumerable().Where(x => string.IsNullOrWhiteSpace(x.JobCode)))
        {
            job.JobCode = NextCode("JOB", job.Id);
            updates.AppendLine($"Job #{job.Id} → {job.JobCode}");
            count++;
        }
        foreach (var material in db.Materials.AsEnumerable().Where(x => string.IsNullOrWhiteSpace(x.MaterialCode)))
        {
            material.MaterialCode = NextCode("MAT", material.Id);
            updates.AppendLine($"Material #{material.Id} → {material.MaterialCode}");
            count++;
        }
        foreach (var batch in db.ProductionBatches.AsEnumerable().Where(x => string.IsNullOrWhiteSpace(x.BatchCode)))
        {
            batch.BatchCode = NextCode("BAT", batch.Id);
            updates.AppendLine($"Batch #{batch.Id} → {batch.BatchCode}");
            count++;
        }
        foreach (var po in db.PurchaseOrders.AsEnumerable().Where(x => string.IsNullOrWhiteSpace(x.PurchaseOrderCode)))
        {
            po.PurchaseOrderCode = NextCode("PO", po.Id);
            updates.AppendLine($"Purchase Order #{po.Id} → {po.PurchaseOrderCode}");
            count++;
        }
        foreach (var task in db.BusinessTasks.AsEnumerable().Where(x => string.IsNullOrWhiteSpace(x.TaskCode)))
        {
            task.TaskCode = NextCode("TSK", task.Id);
            updates.AppendLine($"Task #{task.Id} → {task.TaskCode}");
            count++;
        }

        if (count > 0)
            db.SaveChanges();

        return count == 0 ? "No missing codes were found." : $"Assigned {count} missing codes:\n\n{updates}";
    }

    public static List<string> LookupCode(string rawCode)
    {
        var code = NormalizeCode(rawCode);
        using var db = new AppDbContext();
        var results = new List<string>();

        foreach (var item in db.JewelleryItems.AsEnumerable().Where(i => NormalizeCode(i.StockCode) == code || NormalizeCode(EnsureCode(i.StockCode, "JWL", i.Id)) == code))
            results.Add($"Jewellery Stock: {item.StockCode} — {item.Name} — {item.Status} — {item.RetailPrice:C}");
        foreach (var stone in db.Stones.AsEnumerable().Where(s => NormalizeCode(s.StoneCode) == code || NormalizeCode(EnsureCode(s.StoneCode, "STN", s.Id)) == code))
            results.Add($"Stone: {stone.StoneCode} — {stone.StoneType} — {stone.WeightCarats:0.###}ct — {stone.Status}");
        foreach (var job in db.Jobs.AsEnumerable().Where(j => NormalizeCode(j.JobCode) == code || NormalizeCode(EnsureCode(j.JobCode, "JOB", j.Id)) == code))
            results.Add($"Job: {job.JobCode} — {job.JobTitle} — {job.Status} — Due {job.DueDate?.ToShortDateString() ?? "TBC"}");
        foreach (var material in db.Materials.AsEnumerable().Where(m => NormalizeCode(m.MaterialCode) == code || NormalizeCode(EnsureCode(m.MaterialCode, "MAT", m.Id)) == code))
            results.Add($"Material: {material.MaterialCode} — {material.Name} — {material.CurrentQuantity:0.###} {material.UnitType}");
        foreach (var po in db.PurchaseOrders.AsEnumerable().Where(p => NormalizeCode(p.PurchaseOrderCode) == code || NormalizeCode(EnsureCode(p.PurchaseOrderCode, "PO", p.Id)) == code))
            results.Add($"Purchase Order: {po.PurchaseOrderCode} — {po.Status} — {po.TotalCost:C}");
        foreach (var batch in db.ProductionBatches.AsEnumerable().Where(b => NormalizeCode(b.BatchCode) == code || NormalizeCode(EnsureCode(b.BatchCode, "BAT", b.Id)) == code))
            results.Add($"Production Batch: {batch.BatchCode} — {batch.Name} — {batch.Status} — {batch.CompletedPieces}/{batch.PlannedPieces}");
        foreach (var task in db.BusinessTasks.AsEnumerable().Where(t => NormalizeCode(t.TaskCode) == code || NormalizeCode(EnsureCode(t.TaskCode, "TSK", t.Id)) == code))
            results.Add($"Task: {task.TaskCode} — {task.Title} — {task.Status} — Due {task.DueDate?.ToShortDateString() ?? "TBC"}");

        if (code.StartsWith("MKT-", StringComparison.OrdinalIgnoreCase) && int.TryParse(code[4..], out var marketStockId))
        {
            var marketStock = db.MarketStocks.Find(marketStockId);
            if (marketStock != null)
                results.Add($"Market Stock: MKT-{marketStock.Id:0000} — Jewellery Item #{marketStock.JewelleryItemId} — Packed {marketStock.Packed} — Sold {marketStock.SoldAtMarket}");
        }

        return results;
    }

    private static string BuildLabelDocument(IEnumerable<ScanLabelItem> items, string title, bool single)
    {
        var list = items.Where(i => !string.IsNullOrWhiteSpace(i.Code)).ToList();
        var html = new StringBuilder();
        html.AppendLine("<!doctype html>");
        html.AppendLine("<html><head><meta charset='utf-8'>");
        html.AppendLine($"<title>{Html(title)}</title>");
        html.AppendLine("<style>");
        html.AppendLine("body { font-family: Arial, sans-serif; margin: 18px; color: #222; }");
        html.AppendLine("button { margin: 0 0 14px 0; padding: 8px 12px; }");
        html.AppendLine(".sheet { display: grid; grid-template-columns: repeat(auto-fill, minmax(280px, 1fr)); gap: 12px; }");
        html.AppendLine(".single { max-width: 380px; }");
        html.AppendLine(".label { border: 1px solid #222; border-radius: 8px; padding: 12px; break-inside: avoid; min-height: 190px; }");
        html.AppendLine(".code { font-size: 20px; font-weight: bold; letter-spacing: 1px; margin: 6px 0 2px 0; }");
        html.AppendLine(".title { font-size: 15px; font-weight: bold; margin-top: 8px; }");
        html.AppendLine(".sub { font-size: 12px; color: #555; margin-top: 4px; }");
        html.AppendLine(".price { font-size: 18px; font-weight: bold; margin-top: 8px; }");
        html.AppendLine(".hint { font-size: 10px; color: #777; margin-top: 6px; }");
        html.AppendLine(".barcode { display: flex; align-items: flex-end; height: 68px; margin-top: 8px; padding: 4px 2px 0 2px; background: #fff; border: 1px solid #eee; overflow: hidden; }");
        html.AppendLine(".bar { display: inline-block; height: 56px; background: #111; flex: 0 0 auto; }");
        html.AppendLine(".space { display: inline-block; height: 56px; background: #fff; flex: 0 0 auto; }");
        html.AppendLine(".barcode-text { text-align: center; font-family: Consolas, monospace; font-size: 11px; letter-spacing: 1px; margin-top: 2px; }");
        html.AppendLine("@media print { button { display: none; } body { margin: 7mm; } .sheet { gap: 7mm; } .label { page-break-inside: avoid; } }");
        html.AppendLine("</style></head><body>");
        html.AppendLine("<button onclick='window.print()'>Print Labels</button>");
        html.AppendLine($"<h1>{Html(title)}</h1>");
        html.AppendLine($"<p class='hint'>Generated {Html(DateTime.Now.ToString("f", CultureInfo.CurrentCulture))}. These labels use Code 39 barcodes for scanner-friendly stock lookup. QR payload text is included for future QR support.</p>");
        html.AppendLine(single ? "<div class='single'>" : "<div class='sheet'>");
        foreach (var item in list)
            AppendLabel(html, item);
        html.AppendLine("</div></body></html>");
        return html.ToString();
    }

    private static void AppendLabel(StringBuilder html, ScanLabelItem item)
    {
        html.AppendLine("<section class='label'>");
        html.AppendLine($"<div class='sub'>{Html(item.RecordType)}</div>");
        html.AppendLine($"<div class='code'>{Html(item.Code)}</div>");
        html.AppendLine(Code39BarcodeHtml(item.Code));
        html.AppendLine($"<div class='title'>{Html(item.Title)}</div>");
        html.AppendLine($"<div class='sub'>{Html(item.Subtitle)}</div>");
        if (item.Price.HasValue && item.Price.Value > 0)
            html.AppendLine($"<div class='price'>{item.Price.Value.ToString("C", CultureInfo.CurrentCulture)}</div>");
        if (!string.IsNullOrWhiteSpace(item.Status))
            html.AppendLine($"<div class='sub'>Status: {Html(item.Status)}</div>");
        html.AppendLine($"<div class='hint'>Scan/enter: {Html(item.Code)} | QR payload: JBM:{Html(item.RecordType)}:{item.Id}:{Html(item.Code)}</div>");
        html.AppendLine("</section>");
    }

    private static string Code39BarcodeHtml(string rawCode)
    {
        var code = NormalizeForCode39(rawCode);
        var full = "*" + code + "*";
        const int narrow = 2;
        const int wide = 5;
        const int gap = 2;
        var html = new StringBuilder();
        html.AppendLine($"<div class='barcode' role='img' aria-label='Barcode {Html(code)}'>");

        foreach (var ch in full)
        {
            if (!Code39Patterns.TryGetValue(ch, out var pattern))
                continue;

            for (var i = 0; i < pattern.Length; i++)
            {
                var width = pattern[i] == 'w' ? wide : narrow;
                var cssClass = i % 2 == 0 ? "bar" : "space";
                html.Append($"<span class='{cssClass}' style='width:{width}px'></span>");
            }

            html.Append($"<span class='space' style='width:{gap}px'></span>");
        }

        html.AppendLine("</div>");
        html.AppendLine($"<div class='barcode-text'>*{Html(code)}*</div>");
        return html.ToString();
    }

    private static string EnsureCode(string? existing, string prefix, int id)
        => string.IsNullOrWhiteSpace(existing) ? NextCode(prefix, id) : existing.Trim();

    private static string NextCode(string prefix, int id) => $"{prefix}-{id:0000}";

    private static string NormalizeCode(string value)
        => NormalizeForCode39(value).Trim('*');

    private static string NormalizeForCode39(string? value)
    {
        var input = (value ?? string.Empty).Trim().ToUpperInvariant();
        var output = new StringBuilder();
        foreach (var ch in input)
        {
            if (Code39Patterns.ContainsKey(ch) && ch != '*')
                output.Append(ch);
            else if (char.IsLetterOrDigit(ch))
                output.Append(ch);
            else if (ch == '_' || ch == ':' || ch == '#')
                output.Append('-');
        }
        var result = output.ToString().Trim('-');
        return string.IsNullOrWhiteSpace(result) ? "UNKNOWN" : result;
    }

    private static string PrintoutFolder => BusinessSettingsService.GetPrintoutFolder();
    private static string Html(string value) => WebUtility.HtmlEncode(value);
    private static string SafeFileName(string value)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            value = value.Replace(c, '_');
        return value;
    }
}
