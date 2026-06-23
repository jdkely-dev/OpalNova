using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using JewelleryBusinessManager.Data;
using JewelleryBusinessManager.Models;

namespace JewelleryBusinessManager.Services;

public static class OpalWorkflowService
{
    private static string PrintoutFolder => BusinessSettingsService.GetPrintoutFolder();

    public sealed record ParcelYieldMetrics(
        int StoneCount,
        decimal FinishedCarats,
        decimal EstimatedStoneValue,
        decimal YieldPercent,
        decimal CostPerFinishedCarat,
        decimal EstimatedProfit,
        decimal EstimatedMarginPercent);

    public static ParcelYieldMetrics CalculateParcelYield(AppDbContext db, OpalParcel parcel)
    {
        var stones = db.Stones.AsEnumerable().Where(s => s.OpalParcelId == parcel.Id).ToList();
        var finishedCarats = stones.Sum(s => s.WeightCarats);
        var estimatedValue = stones.Sum(s => s.EstimatedValue);
        var yieldPercent = parcel.StartingWeightCarats > 0 ? finishedCarats / parcel.StartingWeightCarats : 0;
        var costPerCarat = finishedCarats > 0 ? parcel.TotalCost / finishedCarats : 0;
        var profit = estimatedValue - parcel.TotalCost;
        var margin = estimatedValue > 0 ? profit / estimatedValue : 0;
        return new ParcelYieldMetrics(stones.Count, finishedCarats, estimatedValue, yieldPercent, costPerCarat, profit, margin);
    }

    public static string RecalculateParcelYield(AppDbContext db, OpalParcel parcel)
    {
        var metrics = CalculateParcelYield(db, parcel);
        parcel.ActualYieldCarats = metrics.FinishedCarats;
        if (metrics.FinishedCarats > 0 && string.Equals(parcel.Status, "Uncut", StringComparison.OrdinalIgnoreCase))
            parcel.Status = "Partially Cut";
        if (metrics.FinishedCarats > 0 && metrics.StoneCount > 0)
            parcel.UpdatedAt = DateTime.Now;
        db.SaveChanges();

        return BuildParcelSummary(parcel, metrics);
    }

    public static string BuildParcelSummary(OpalParcel parcel, ParcelYieldMetrics metrics)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Parcel: {parcel.ParcelCode} #{parcel.Id}".Trim());
        sb.AppendLine($"Origin: {parcel.Origin ?? ""}".Trim());
        sb.AppendLine($"Starting weight: {parcel.StartingWeightCarats:N2} ct");
        sb.AppendLine($"Finished stones: {metrics.StoneCount}");
        sb.AppendLine($"Finished carats: {metrics.FinishedCarats:N2} ct");
        sb.AppendLine($"Yield: {metrics.YieldPercent:P1}");
        sb.AppendLine($"Parcel cost: {Money(parcel.TotalCost)}");
        sb.AppendLine($"Cost per finished carat: {Money(metrics.CostPerFinishedCarat)} / ct");
        sb.AppendLine($"Estimated finished stone value: {Money(metrics.EstimatedStoneValue)}");
        sb.AppendLine($"Estimated parcel profit: {Money(metrics.EstimatedProfit)}");
        sb.AppendLine($"Estimated margin: {metrics.EstimatedMarginPercent:P1}");
        return sb.ToString();
    }

    public static string ApplyStoneWorkflowStage(AppDbContext db, Stone stone, string stage, string notes)
    {
        stone.Status = StageToStoneStatus(stage);
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm", CultureInfo.CurrentCulture);
        var line = $"[{timestamp}] Workflow stage: {stage}" + (string.IsNullOrWhiteSpace(notes) ? string.Empty : $" - {notes.Trim()}");
        stone.Notes = string.IsNullOrWhiteSpace(stone.Notes) ? line : stone.Notes + Environment.NewLine + line;
        db.SaveChanges();
        return line;
    }

    public static StoneStatus StageToStoneStatus(string stage)
    {
        return stage switch
        {
            "Rough" => StoneStatus.Rough,
            "Cutting" => StoneStatus.Cutting,
            "Polished" => StoneStatus.Polished,
            "Selected For Design" => StoneStatus.SelectedForDesign,
            "Assigned To Jewellery" => StoneStatus.AssignedToJewellery,
            "Set In Jewellery" => StoneStatus.SetInJewellery,
            "Reserved" => StoneStatus.Reserved,
            "Sold" => StoneStatus.Sold,
            _ => StoneStatus.Loose
        };
    }

    public static string StatusToStage(StoneStatus status)
    {
        return status switch
        {
            StoneStatus.Rough => "Rough",
            StoneStatus.Cutting => "Cutting",
            StoneStatus.Polished => "Polished",
            StoneStatus.SelectedForDesign => "Selected For Design",
            StoneStatus.AssignedToJewellery => "Assigned To Jewellery",
            StoneStatus.SetInJewellery => "Set In Jewellery",
            StoneStatus.Reserved => "Reserved",
            StoneStatus.Sold => "Sold",
            _ => "Loose"
        };
    }

    public static string CreateOpalYieldReport(OpalParcel? selectedParcel = null)
    {
        Directory.CreateDirectory(PrintoutFolder);
        using var db = new AppDbContext();
        var parcels = selectedParcel == null
            ? db.OpalParcels.AsNoTrackingSafe().ToList()
            : db.OpalParcels.AsNoTrackingSafe().Where(p => p.Id == selectedParcel.Id).ToList();

        var fileName = SafeFileName(selectedParcel == null ? $"OpalYieldReport_{DateTime.Today:yyyyMMdd}.html" : $"OpalYield_{selectedParcel.ParcelCode}_{selectedParcel.Id}.html");
        var path = Path.Combine(PrintoutFolder, fileName);
        var settings = BusinessSettingsService.Load();

        var html = new StringBuilder();
        html.AppendLine("<!doctype html><html><head><meta charset='utf-8'>");
        html.AppendLine("<title>Opal Parcel Yield Report</title>");
        html.AppendLine("<style>");
        html.AppendLine("body{font-family:Segoe UI,Arial,sans-serif;margin:28px;color:#172033;background:#fff;}h1{margin-bottom:0;}h2{margin-top:28px;border-bottom:1px solid #d8dee9;padding-bottom:6px;}table{border-collapse:collapse;width:100%;margin-top:12px;}th,td{border:1px solid #d8dee9;padding:7px;text-align:left;}th{background:#f5f7fb;}.muted{color:#596579}.summary{background:#f8fafc;border:1px solid #d8dee9;padding:12px;margin:14px 0}.profit{font-weight:700}.warn{color:#a15c00}@media print{button{display:none}body{margin:14mm}}");
        html.AppendLine("</style></head><body>");
        html.AppendLine($"<h1>{WebUtility.HtmlEncode(settings.BusinessName)} - Opal Parcel Yield Report</h1>");
        html.AppendLine($"<p class='muted'>Created {DateTime.Now:g}</p>");

        if (parcels.Count == 0)
        {
            html.AppendLine("<p>No opal parcels found.</p>");
        }

        foreach (var parcel in parcels)
        {
            var metrics = CalculateParcelYield(db, parcel);
            var stones = db.Stones.AsEnumerable().Where(s => s.OpalParcelId == parcel.Id).OrderBy(s => s.StoneCode).ToList();
            html.AppendLine("<section class='summary'>");
            html.AppendLine($"<h2>{H(parcel.ParcelCode)} {H(parcel.Origin ?? string.Empty)}</h2>");
            html.AppendLine("<table><tbody>");
            html.AppendLine(Row("Status", parcel.Status));
            html.AppendLine(Row("Purchase Date", parcel.PurchaseDate.ToShortDateString()));
            html.AppendLine(Row("Starting Weight", $"{parcel.StartingWeightCarats:N2} ct"));
            html.AppendLine(Row("Finished Carats", $"{metrics.FinishedCarats:N2} ct"));
            html.AppendLine(Row("Yield", metrics.YieldPercent.ToString("P1")));
            html.AppendLine(Row("Parcel Cost", Money(parcel.TotalCost)));
            html.AppendLine(Row("Cost Per Finished Carat", Money(metrics.CostPerFinishedCarat)));
            html.AppendLine(Row("Estimated Finished Stone Value", Money(metrics.EstimatedStoneValue)));
            html.AppendLine(Row("Estimated Parcel Profit", Money(metrics.EstimatedProfit)));
            html.AppendLine(Row("Estimated Margin", metrics.EstimatedMarginPercent.ToString("P1")));
            html.AppendLine("</tbody></table>");
            html.AppendLine("<h3>Cut stones from this parcel</h3>");
            html.AppendLine("<table><thead><tr><th>Stone</th><th>Weight</th><th>Shape</th><th>Body Tone</th><th>Brightness</th><th>Pattern</th><th>Status</th><th>Est. Value</th></tr></thead><tbody>");
            foreach (var stone in stones)
            {
                html.AppendLine("<tr>" +
                    Cell(stone.StoneCode) +
                    Cell($"{stone.WeightCarats:N2} ct") +
                    Cell(stone.Shape) +
                    Cell(stone.BodyTone) +
                    Cell(stone.Brightness) +
                    Cell(stone.Pattern) +
                    Cell(stone.Status.ToString()) +
                    Cell(Money(stone.EstimatedValue)) +
                    "</tr>");
            }
            if (stones.Count == 0)
                html.AppendLine("<tr><td colspan='8' class='warn'>No cut stones linked to this parcel yet.</td></tr>");
            html.AppendLine("</tbody></table>");
            if (!string.IsNullOrWhiteSpace(parcel.Notes))
                html.AppendLine($"<p><b>Notes:</b><br>{H(parcel.Notes).Replace(Environment.NewLine, "<br>")}</p>");
            html.AppendLine("</section>");
        }

        html.AppendLine("</body></html>");
        File.WriteAllText(path, html.ToString());
        return path;
    }

    public static string CreateStoneWorkflowReport()
    {
        Directory.CreateDirectory(PrintoutFolder);
        using var db = new AppDbContext();
        var stones = db.Stones.AsEnumerable().OrderBy(s => s.Status).ThenBy(s => s.StoneCode).ToList();
        var path = Path.Combine(PrintoutFolder, SafeFileName($"StoneWorkflowReport_{DateTime.Today:yyyyMMdd}.html"));
        var settings = BusinessSettingsService.Load();
        var html = new StringBuilder();
        html.AppendLine("<!doctype html><html><head><meta charset='utf-8'><title>Stone Workflow Report</title>");
        html.AppendLine("<style>body{font-family:Segoe UI,Arial,sans-serif;margin:28px;color:#172033;}table{border-collapse:collapse;width:100%;}th,td{border:1px solid #d8dee9;padding:7px;text-align:left;}th{background:#f5f7fb;}h2{margin-top:28px}.muted{color:#596579}@media print{body{margin:14mm}}</style></head><body>");
        html.AppendLine($"<h1>{WebUtility.HtmlEncode(settings.BusinessName)} - Stone Workflow Report</h1><p class='muted'>Created {DateTime.Now:g}</p>");
        foreach (var group in stones.GroupBy(s => s.Status).OrderBy(g => g.Key.ToString()))
        {
            html.AppendLine($"<h2>{H(group.Key.ToString())}</h2><table><thead><tr><th>Stone</th><th>Parcel</th><th>Weight</th><th>Shape</th><th>Colours</th><th>Est. Value</th><th>Notes</th></tr></thead><tbody>");
            foreach (var stone in group)
            {
                var parcel = stone.OpalParcelId.HasValue ? db.OpalParcels.Find(stone.OpalParcelId.Value) : null;
                html.AppendLine("<tr>" + Cell(stone.StoneCode) + Cell(parcel?.ParcelCode) + Cell($"{stone.WeightCarats:N2} ct") + Cell(stone.Shape) + Cell(stone.MainColours) + Cell(Money(stone.EstimatedValue)) + Cell(stone.Notes) + "</tr>");
            }
            html.AppendLine("</tbody></table>");
        }
        html.AppendLine("</body></html>");
        File.WriteAllText(path, html.ToString());
        return path;
    }

    public static void OpenInDefaultApp(string path) => DocumentExportService.OpenInDefaultApp(path);

    private static string Row(string label, string? value) => $"<tr><th>{H(label)}</th><td>{H(value ?? string.Empty)}</td></tr>";
    private static string Cell(string? value) => $"<td>{H(value ?? string.Empty)}</td>";
    private static string H(string value) => WebUtility.HtmlEncode(value);
    private static string Money(decimal value) => value.ToString("C", CultureInfo.CurrentCulture);
    private static string SafeFileName(string value)
    {
        foreach (var invalid in Path.GetInvalidFileNameChars())
            value = value.Replace(invalid, '-');
        return value;
    }
}

internal static class OpalWorkflowQueryableExtensions
{
    public static IQueryable<T> AsNoTrackingSafe<T>(this IQueryable<T> source) where T : class => Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.AsNoTracking(source);
}
