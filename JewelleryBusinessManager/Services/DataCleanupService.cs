using System.IO;
using System.Net;
using System.Text;
using JewelleryBusinessManager.Data;
using JewelleryBusinessManager.Models;

namespace JewelleryBusinessManager.Services;

public static class DataCleanupService
{
    private static string PrintoutFolder => BusinessSettingsService.GetPrintoutFolder();

    public static string CreateDataQualityReport()
    {
        Directory.CreateDirectory(PrintoutFolder);
        using var db = new AppDbContext();
        var path = Path.Combine(PrintoutFolder, $"data-quality-report-{DateTime.Now:yyyyMMdd-HHmmss}.html");
        var html = new StringBuilder();
        html.Append(Header("Data Quality Report"));
        html.AppendLine("<section class='hero'><h1>Data Quality Report</h1><p>Checks duplicate codes, missing details, linked-record problems and records that need cleanup before daily use or release packaging.</p></section>");
        html.AppendLine("<section class='grid'>");
        html.Append(SummaryCard("Customers", db.Customers.Count(), CountIncompleteCustomers(db)));
        html.Append(SummaryCard("Materials", db.Materials.Count(), CountMaterialIssues(db)));
        html.Append(SummaryCard("Jewellery", db.JewelleryItems.Count(), CountJewelleryIssues(db)));
        html.Append(SummaryCard("Jobs", db.Jobs.Count(), CountJobIssues(db)));
        html.Append(SummaryCard("Listings", db.OnlineListings.Count(), CountListingIssues(db)));
        html.Append(SummaryCard("Photos", db.PhotoRecords.Count(), CountMissingPhotoFiles(db)));
        html.AppendLine("</section>");
        html.Append(DuplicateSection(db));
        html.Append(MissingDataSection(db));
        html.Append(OrphanSection(db));
        html.AppendLine("<section class='card'><h2>Recommended cleanup order</h2><ol><li>Run Assign Missing Codes from Codes & Labels Studio.</li><li>Fix missing prices/costs on active jewellery.</li><li>Add photos to stock marked NeedsPhotos or listing records with incomplete photo status.</li><li>Review duplicate names/codes before printing labels or importing/exporting stock.</li><li>Create follow-up tasks for unresolved cleanup items.</li></ol></section>");
        html.Append(Footer());
        File.WriteAllText(path, html.ToString());
        return path;
    }

    public static string CreateDuplicateReport()
    {
        Directory.CreateDirectory(PrintoutFolder);
        using var db = new AppDbContext();
        var path = Path.Combine(PrintoutFolder, $"duplicate-finder-{DateTime.Now:yyyyMMdd-HHmmss}.html");
        var html = new StringBuilder();
        html.Append(Header("Duplicate Finder"));
        html.AppendLine("<section class='hero'><h1>Duplicate Finder</h1><p>Potential duplicates are grouped for review. Blank values are ignored.</p></section>");
        html.Append(DuplicateSection(db));
        html.Append(Footer());
        File.WriteAllText(path, html.ToString());
        return path;
    }

    public static string CreateMissingDataReport()
    {
        Directory.CreateDirectory(PrintoutFolder);
        using var db = new AppDbContext();
        var path = Path.Combine(PrintoutFolder, $"missing-data-report-{DateTime.Now:yyyyMMdd-HHmmss}.html");
        var html = new StringBuilder();
        html.Append(Header("Missing Data Report"));
        html.AppendLine("<section class='hero'><h1>Missing Data Report</h1><p>Records that may need photos, pricing, contact details, links, dates or descriptions.</p></section>");
        html.Append(MissingDataSection(db));
        html.Append(Footer());
        File.WriteAllText(path, html.ToString());
        return path;
    }

    public static int CreateCleanupTasks()
    {
        using var db = new AppDbContext();
        var existingTitles = db.BusinessTasks.Select(t => t.Title).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var created = 0;

        foreach (var material in db.Materials.Where(m => m.CurrentQuantity <= m.ReorderLevel).ToList())
        {
            created += AddTaskIfMissing(db, existingTitles, $"Reorder {material.Name}", BusinessTaskCategory.Purchasing, BusinessTaskPriority.High, DateTime.Today.AddDays(1), $"Current quantity {material.CurrentQuantity:0.##} {material.UnitType}; reorder level {material.ReorderLevel:0.##}.", materialId: null);
        }

        foreach (var item in db.JewelleryItems.Where(j => j.Status == StockStatus.NeedsPhotos || j.RetailPrice <= 0 || string.IsNullOrWhiteSpace(j.StockCode)).ToList())
        {
            var reason = item.Status == StockStatus.NeedsPhotos ? "needs photos" : item.RetailPrice <= 0 ? "needs retail price" : "needs stock code";
            created += AddTaskIfMissing(db, existingTitles, $"Cleanup stock record: {item.Name}", BusinessTaskCategory.Inventory, BusinessTaskPriority.Normal, DateTime.Today.AddDays(3), $"Jewellery item {item.StockCode} {reason}.", jewelleryItemId: item.Id);
        }

        foreach (var job in db.Jobs.Where(j => j.Status != JobStatus.Completed && j.Status != JobStatus.Cancelled && j.DueDate.HasValue && j.DueDate.Value.Date <= DateTime.Today.AddDays(7)).ToList())
        {
            created += AddTaskIfMissing(db, existingTitles, $"Follow up job: {job.JobTitle}", BusinessTaskCategory.CustomerFollowUp, BusinessTaskPriority.High, job.DueDate ?? DateTime.Today, $"Job {job.JobCode} is due {job.DueDate:d}.", jobId: job.Id, customerId: job.CustomerId);
        }

        foreach (var listing in db.OnlineListings.Where(l => !l.ListedOnline && (!l.PhotosDone || !l.DescriptionDone || !l.PriceChecked)).ToList())
        {
            created += AddTaskIfMissing(db, existingTitles, $"Finish online listing #{listing.Id}", BusinessTaskCategory.OnlineListing, BusinessTaskPriority.Normal, DateTime.Today.AddDays(5), "Listing needs photos, description, price check or publishing.", jewelleryItemId: listing.JewelleryItemId);
        }

        db.SaveChanges();
        return created;
    }

    private static int AddTaskIfMissing(AppDbContext db, HashSet<string> existingTitles, string title, BusinessTaskCategory category, BusinessTaskPriority priority, DateTime dueDate, string description, int? customerId = null, int? jobId = null, int? jewelleryItemId = null, int? materialId = null)
    {
        if (existingTitles.Contains(title)) return 0;
        var next = db.BusinessTasks.Count() + 1;
        db.BusinessTasks.Add(new BusinessTask
        {
            TaskCode = $"TSK-{next:0000}",
            Title = title,
            Category = category,
            Priority = priority,
            Status = BusinessTaskStatus.ToDo,
            DueDate = dueDate,
            Description = description,
            CustomerId = customerId,
            JobId = jobId,
            JewelleryItemId = jewelleryItemId,
            ShowOnDashboard = true
        });
        existingTitles.Add(title);
        return 1;
    }

    private static int CountIncompleteCustomers(AppDbContext db) => db.Customers.AsEnumerable().Count(c => string.IsNullOrWhiteSpace(c.FullName) || (string.IsNullOrWhiteSpace(c.Phone) && string.IsNullOrWhiteSpace(c.Email) && string.IsNullOrWhiteSpace(c.InstagramHandle)));
    private static int CountMaterialIssues(AppDbContext db) => db.Materials.AsEnumerable().Count(m => string.IsNullOrWhiteSpace(m.Name) || string.IsNullOrWhiteSpace(m.MaterialCode) || m.CurrentQuantity <= m.ReorderLevel || m.CurrentQuantity < 0);
    private static int CountJewelleryIssues(AppDbContext db) => db.JewelleryItems.AsEnumerable().Count(j => string.IsNullOrWhiteSpace(j.StockCode) || string.IsNullOrWhiteSpace(j.Name) || j.RetailPrice <= 0 || j.Status == StockStatus.NeedsPhotos);
    private static int CountJobIssues(AppDbContext db) => db.Jobs.AsEnumerable().Count(j => string.IsNullOrWhiteSpace(j.JobCode) || string.IsNullOrWhiteSpace(j.JobTitle) || (!j.CustomerId.HasValue && j.Type != JobType.MarketPreparation) || (j.Status != JobStatus.Completed && j.Status != JobStatus.Cancelled && j.DueDate.HasValue && j.DueDate.Value.Date < DateTime.Today));
    private static int CountListingIssues(AppDbContext db) => db.OnlineListings.AsEnumerable().Count(l => !l.ListedOnline && (!l.PhotosDone || !l.DescriptionDone || !l.PriceChecked || string.IsNullOrWhiteSpace(l.SeoTitle)));
    private static int CountMissingPhotoFiles(AppDbContext db) => db.PhotoRecords.AsEnumerable().Count(p => string.IsNullOrWhiteSpace(p.FilePath) || !File.Exists(p.FilePath));

    private static string DuplicateSection(AppDbContext db)
    {
        var html = new StringBuilder();
        html.AppendLine("<section class='card'><h2>Potential Duplicates</h2>");
        html.Append(DuplicateTable("Customer names", db.Customers.AsEnumerable().Select(c => (Key: (string?)c.FullName, Label: $"#{c.Id} {c.FullName}"))));
        html.Append(DuplicateTable("Supplier names", db.Suppliers.AsEnumerable().Select(s => (Key: (string?)s.Name, Label: $"#{s.Id} {s.Name}"))));
        html.Append(DuplicateTable("Material codes", db.Materials.AsEnumerable().Select(m => (Key: (string?)m.MaterialCode, Label: $"#{m.Id} {m.MaterialCode} {m.Name}"))));
        html.Append(DuplicateTable("Stone codes", db.Stones.AsEnumerable().Select(s => (Key: (string?)s.StoneCode, Label: $"#{s.Id} {s.StoneCode} {s.StoneType}"))));
        html.Append(DuplicateTable("Stock codes", db.JewelleryItems.AsEnumerable().Select(j => (Key: (string?)j.StockCode, Label: $"#{j.Id} {j.StockCode} {j.Name}"))));
        html.Append(DuplicateTable("Job codes", db.Jobs.AsEnumerable().Select(j => (Key: (string?)j.JobCode, Label: $"#{j.Id} {j.JobCode} {j.JobTitle}"))));
        html.AppendLine("</section>");
        return html.ToString();
    }

    private static string DuplicateTable(string title, IEnumerable<(string? Key, string Label)> values)
    {
        var groups = values.Where(v => !string.IsNullOrWhiteSpace(v.Key)).GroupBy(v => v.Key!.Trim(), StringComparer.OrdinalIgnoreCase).Where(g => g.Count() > 1).ToList();
        if (groups.Count == 0) return $"<p class='ok'>No duplicate {Html(title.ToLowerInvariant())} found.</p>";
        var html = new StringBuilder();
        html.AppendLine($"<h3>{Html(title)}</h3><table><tr><th>Duplicate value</th><th>Records</th></tr>");
        foreach (var group in groups)
            html.AppendLine($"<tr><td>{Html(group.Key)}</td><td>{Html(string.Join(", ", group.Select(g => g.Label)))}</td></tr>");
        html.AppendLine("</table>");
        return html.ToString();
    }

    private static string MissingDataSection(AppDbContext db)
    {
        var html = new StringBuilder();
        html.AppendLine("<section class='card'><h2>Missing / Incomplete Data</h2>");
        html.Append(IssueList("Customers needing contact detail", db.Customers.AsEnumerable().Where(c => string.IsNullOrWhiteSpace(c.Phone) && string.IsNullOrWhiteSpace(c.Email) && string.IsNullOrWhiteSpace(c.InstagramHandle)).Select(c => $"#{c.Id} {c.FullName}")));
        html.Append(IssueList("Materials below reorder level", db.Materials.AsEnumerable().Where(m => m.CurrentQuantity <= m.ReorderLevel).Select(m => $"{m.MaterialCode} {m.Name}: {m.CurrentQuantity:0.##} / reorder {m.ReorderLevel:0.##} {m.UnitType}")));
        html.Append(IssueList("Jewellery needing retail price", db.JewelleryItems.AsEnumerable().Where(j => j.RetailPrice <= 0).Select(j => $"{j.StockCode} {j.Name}")));
        html.Append(IssueList("Jewellery needing photos", db.JewelleryItems.AsEnumerable().Where(j => j.Status == StockStatus.NeedsPhotos).Select(j => $"{j.StockCode} {j.Name}")));
        html.Append(IssueList("Open jobs overdue", db.Jobs.AsEnumerable().Where(j => j.Status != JobStatus.Completed && j.Status != JobStatus.Cancelled && j.DueDate.HasValue && j.DueDate.Value.Date < DateTime.Today).Select(j => $"{j.JobCode} {j.JobTitle}: due {j.DueDate:d}")));
        html.Append(IssueList("Listings needing work", db.OnlineListings.AsEnumerable().Where(l => !l.ListedOnline && (!l.PhotosDone || !l.DescriptionDone || !l.PriceChecked || string.IsNullOrWhiteSpace(l.SeoTitle))).Select(l => $"Listing #{l.Id}: {l.Platform} {l.SeoTitle}")));
        html.Append(IssueList("Missing photo files", db.PhotoRecords.AsEnumerable().Where(p => string.IsNullOrWhiteSpace(p.FilePath) || !File.Exists(p.FilePath)).Select(p => $"Photo #{p.Id}: {p.FilePath}")));
        html.AppendLine("</section>");
        return html.ToString();
    }

    private static string OrphanSection(AppDbContext db)
    {
        var html = new StringBuilder();
        html.AppendLine("<section class='card'><h2>Linked Record Checks</h2>");
        var customerIds = db.Customers.Select(c => c.Id).ToHashSet();
        var materialIds = db.Materials.Select(m => m.Id).ToHashSet();
        var jobIds = db.Jobs.Select(j => j.Id).ToHashSet();
        var jewelleryIds = db.JewelleryItems.Select(j => j.Id).ToHashSet();
        var stoneIds = db.Stones.Select(s => s.Id).ToHashSet();
        var marketIds = db.MarketEvents.Select(m => m.Id).ToHashSet();
        html.Append(IssueList("Jobs linked to missing customers", db.Jobs.AsEnumerable().Where(j => j.CustomerId.HasValue && !customerIds.Contains(j.CustomerId.Value)).Select(j => $"{j.JobCode} customer #{j.CustomerId}")));
        html.Append(IssueList("Material transactions linked to missing materials", db.MaterialTransactions.AsEnumerable().Where(t => !materialIds.Contains(t.MaterialId)).Select(t => $"Transaction #{t.Id} material #{t.MaterialId}")));
        html.Append(IssueList("Sales linked to missing jewellery/jobs/customers", db.Sales.AsEnumerable().Where(s => (s.JewelleryItemId.HasValue && !jewelleryIds.Contains(s.JewelleryItemId.Value)) || (s.JobId.HasValue && !jobIds.Contains(s.JobId.Value)) || (s.CustomerId.HasValue && !customerIds.Contains(s.CustomerId.Value))).Select(s => $"Sale #{s.Id}")));
        html.Append(IssueList("Jewellery linked to missing stones", db.JewelleryItems.AsEnumerable().Where(j => j.MainStoneId.HasValue && !stoneIds.Contains(j.MainStoneId.Value)).Select(j => $"{j.StockCode} stone #{j.MainStoneId}")));
        html.Append(IssueList("Market stock linked to missing records", db.MarketStocks.AsEnumerable().Where(ms => !marketIds.Contains(ms.MarketEventId) || !jewelleryIds.Contains(ms.JewelleryItemId)).Select(ms => $"MarketStock #{ms.Id}: market #{ms.MarketEventId}, item #{ms.JewelleryItemId}")));
        html.AppendLine("</section>");
        return html.ToString();
    }

    private static string IssueList(string title, IEnumerable<string> issues)
    {
        var list = issues.Where(i => !string.IsNullOrWhiteSpace(i)).Take(80).ToList();
        if (list.Count == 0) return $"<p class='ok'>✓ {Html(title)}: no issues found.</p>";
        var html = new StringBuilder();
        html.AppendLine($"<h3>{Html(title)} <span class='badge'>{list.Count}</span></h3><ul>");
        foreach (var item in list) html.AppendLine($"<li>{Html(item)}</li>");
        html.AppendLine("</ul>");
        return html.ToString();
    }

    private static string SummaryCard(string title, int total, int issues) => $"<div class='summary'><div class='label'>{Html(title)}</div><div class='value'>{total}</div><div class='{(issues == 0 ? "good" : "warn")}'>{issues} issue(s)</div></div>";

    private static string Header(string title)
    {
        var settings = BusinessSettingsService.Load();
        var business = string.IsNullOrWhiteSpace(settings.BusinessName) ? "OPALNOVA" : settings.BusinessName;
        var sb = new StringBuilder();
        sb.AppendLine("<!doctype html><html><head><meta charset='utf-8'>");
        sb.AppendLine($"<title>{Html(title)}</title>");
        sb.AppendLine("<style>");
        sb.AppendLine("body{font-family:Segoe UI,Arial,sans-serif;margin:32px;background:#f8fafc;color:#111827;line-height:1.45}");
        sb.AppendLine(".hero{background:#111827;color:#f8fafc;border-radius:18px;padding:24px;margin-bottom:18px}.hero h1{margin:0 0 8px;font-size:30px}.hero p{margin:0;color:#cbd5e1}");
        sb.AppendLine(".brand{color:#b45309;font-weight:700;margin-bottom:10px}.grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(160px,1fr));gap:12px;margin:16px 0}.summary,.card{background:white;border:1px solid #e5e7eb;border-radius:16px;padding:16px;box-shadow:0 6px 20px rgba(15,23,42,.08)}");
        sb.AppendLine(".summary .label{text-transform:uppercase;font-size:11px;color:#64748b;font-weight:700}.summary .value{font-size:28px;font-weight:800;margin:4px 0}.good,.ok{color:#047857;font-weight:700}.warn{color:#b45309;font-weight:700}.badge{font-size:12px;background:#fef3c7;color:#92400e;border-radius:999px;padding:2px 8px}table{width:100%;border-collapse:collapse;margin:8px 0 14px}th,td{border-bottom:1px solid #e5e7eb;padding:9px;text-align:left}th{background:#f1f5f9;color:#334155}h2{margin-top:4px}h3{margin-top:18px}li{margin:5px 0}.footer{margin-top:20px;color:#64748b;font-size:12px}");
        sb.AppendLine("</style></head><body>");
        sb.AppendLine($"<div class='brand'>{Html(business)} · Generated {Html(DateTime.Now.ToString("f"))}</div>");
        return sb.ToString();
    }

    private static string Footer() => "<div class='footer'>Review issues before bulk editing. Reports are advisory and do not change data unless you use a bulk action.</div></body></html>";
    private static string Html(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);
}
