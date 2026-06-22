using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using Microsoft.EntityFrameworkCore;
using JewelleryBusinessManager.Data;
using JewelleryBusinessManager.Models;

namespace JewelleryBusinessManager.Services;

public static class TaskWorkflowService
{
    private static string PrintoutFolder => BusinessSettingsService.GetPrintoutFolder();

    public static BusinessTask CreateTaskFromSelected(object? selected)
    {
        var task = new BusinessTask
        {
            TaskCode = GenerateTaskCode(),
            DueDate = DateTime.Today,
            ReminderDate = DateTime.Today,
            Priority = BusinessTaskPriority.Normal,
            Status = BusinessTaskStatus.ToDo,
            Category = BusinessTaskCategory.General,
            ShowOnDashboard = true
        };

        switch (selected)
        {
            case Customer customer:
                task.CustomerId = customer.Id;
                task.Category = BusinessTaskCategory.CustomerFollowUp;
                task.Title = $"Follow up with {customer.FullName}".Trim();
                task.Description = "Customer follow-up task created from the customer record.";
                break;
            case Job job:
                task.CustomerId = job.CustomerId;
                task.JobId = job.Id;
                task.Category = BusinessTaskCategory.BenchWork;
                task.Title = string.IsNullOrWhiteSpace(job.JobTitle) ? "Work on job" : job.JobTitle;
                task.DueDate = job.DueDate ?? DateTime.Today.AddDays(1);
                task.ReminderDate = DateTime.Today;
                task.Description = $"Task created from job {job.JobCode} {job.JobTitle}".Trim();
                break;
            case JewelleryItem item:
                task.JewelleryItemId = item.Id;
                task.Category = item.Status == StockStatus.NeedsPhotos ? BusinessTaskCategory.OnlineListing : BusinessTaskCategory.BenchWork;
                task.Title = item.Status == StockStatus.NeedsPhotos ? $"Photograph {item.Name}" : $"Work on {item.Name}";
                task.Description = $"Task created from jewellery item {item.StockCode} {item.Name}".Trim();
                break;
            case Stone stone:
                task.StoneId = stone.Id;
                task.Category = BusinessTaskCategory.OpalCutting;
                task.Title = $"Stone workflow: {stone.StoneCode} {stone.StoneType}".Trim();
                task.Description = "Task created from stone/opal record.";
                break;
            case Material material:
                task.Category = BusinessTaskCategory.Inventory;
                task.Title = $"Check/reorder {material.Name}";
                task.Description = $"Current quantity: {material.CurrentQuantity} {material.UnitType}. Reorder level: {material.ReorderLevel}.";
                break;
            case MarketEvent market:
                task.MarketEventId = market.Id;
                task.Category = BusinessTaskCategory.MarketPrep;
                task.Title = $"Prepare for {market.Name}";
                task.DueDate = market.EventDate.AddDays(-2);
                task.ReminderDate = market.EventDate.AddDays(-3);
                task.Description = "Market preparation task created from the market event.";
                break;
            case ProductionBatch batch:
                task.ProductionBatchId = batch.Id;
                task.Category = BusinessTaskCategory.Production;
                task.Title = $"Progress batch: {batch.Name}";
                task.DueDate = batch.TargetCompletionDate;
                task.Description = "Production batch task created from batch record.";
                break;
            case PurchaseOrder order:
                task.PurchaseOrderId = order.Id;
                task.Category = BusinessTaskCategory.Purchasing;
                task.Title = $"Follow up purchase order {order.PurchaseOrderCode}";
                task.DueDate = order.ExpectedDeliveryDate ?? DateTime.Today.AddDays(7);
                task.Description = "Purchasing follow-up task created from purchase order.";
                break;
            case OnlineListing listing:
                task.JewelleryItemId = listing.JewelleryItemId;
                task.Category = BusinessTaskCategory.OnlineListing;
                task.Title = $"Complete online listing for item #{listing.JewelleryItemId}";
                task.Description = "Online listing task created from listing record.";
                break;
            default:
                task.Title = "New task";
                task.Description = "General business task.";
                break;
        }

        return task;
    }

    public static string GenerateTaskCode() => $"TASK-{DateTime.Today:yyyyMMdd}-{DateTime.Now:HHmmss}";

    public static int CreateSuggestedTasks()
    {
        using var db = new AppDbContext();
        var created = 0;

        created += AddTaskForOverdueJobs(db);
        created += AddTaskForJobsDueSoon(db);
        created += AddTasksForLowMaterials(db);
        created += AddTasksForUpcomingMarkets(db);
        created += AddTasksForListingsNeedingWork(db);
        created += AddTasksForPurchaseOrdersDue(db);

        db.SaveChanges();
        return created;
    }

    private static int AddTaskForOverdueJobs(AppDbContext db)
    {
        var created = 0;
        var jobs = db.Jobs.AsNoTracking().AsEnumerable()
            .Where(j => j.DueDate.HasValue && j.DueDate.Value.Date < DateTime.Today && j.Status != JobStatus.Completed && j.Status != JobStatus.Cancelled)
            .ToList();
        foreach (var job in jobs)
        {
            if (TaskExists(db, jobId: job.Id, titleStartsWith: "Overdue job")) continue;
            db.BusinessTasks.Add(new BusinessTask
            {
                TaskCode = GenerateTaskCode(),
                Title = $"Overdue job: {job.JobCode} {job.JobTitle}".Trim(),
                Category = BusinessTaskCategory.BenchWork,
                Priority = BusinessTaskPriority.Urgent,
                Status = BusinessTaskStatus.ToDo,
                DueDate = DateTime.Today,
                ReminderDate = DateTime.Today,
                CustomerId = job.CustomerId,
                JobId = job.Id,
                Description = "Automatically suggested because this active job is past its due date.",
                ShowOnDashboard = true
            });
            created++;
        }
        return created;
    }

    private static int AddTaskForJobsDueSoon(AppDbContext db)
    {
        var created = 0;
        var cutoff = DateTime.Today.AddDays(7);
        var jobs = db.Jobs.AsNoTracking().AsEnumerable()
            .Where(j => j.DueDate.HasValue && j.DueDate.Value.Date >= DateTime.Today && j.DueDate.Value.Date <= cutoff && j.Status != JobStatus.Completed && j.Status != JobStatus.Cancelled)
            .ToList();
        foreach (var job in jobs)
        {
            if (TaskExists(db, jobId: job.Id, titleStartsWith: "Job due soon")) continue;
            db.BusinessTasks.Add(new BusinessTask
            {
                TaskCode = GenerateTaskCode(),
                Title = $"Job due soon: {job.JobCode} {job.JobTitle}".Trim(),
                Category = BusinessTaskCategory.BenchWork,
                Priority = BusinessTaskPriority.High,
                Status = BusinessTaskStatus.ToDo,
                DueDate = job.DueDate,
                ReminderDate = DateTime.Today,
                CustomerId = job.CustomerId,
                JobId = job.Id,
                Description = "Automatically suggested because this job is due within 7 days.",
                ShowOnDashboard = true
            });
            created++;
        }
        return created;
    }

    private static int AddTasksForLowMaterials(AppDbContext db)
    {
        var created = 0;
        var materials = db.Materials.AsNoTracking().AsEnumerable()
            .Where(m => m.CurrentQuantity <= m.ReorderLevel)
            .ToList();
        foreach (var material in materials)
        {
            var title = $"Reorder {material.Name}";
            if (db.BusinessTasks.AsNoTracking().AsEnumerable().Any(t => t.IsOpen && t.Title == title)) continue;
            db.BusinessTasks.Add(new BusinessTask
            {
                TaskCode = GenerateTaskCode(),
                Title = title,
                Category = BusinessTaskCategory.Inventory,
                Priority = BusinessTaskPriority.High,
                Status = BusinessTaskStatus.ToDo,
                DueDate = DateTime.Today.AddDays(2),
                ReminderDate = DateTime.Today,
                Description = $"Low material alert. Current: {material.CurrentQuantity} {material.UnitType}. Reorder level: {material.ReorderLevel}.",
                ShowOnDashboard = true
            });
            created++;
        }
        return created;
    }

    private static int AddTasksForUpcomingMarkets(AppDbContext db)
    {
        var created = 0;
        var markets = db.MarketEvents.AsNoTracking().AsEnumerable()
            .Where(m => m.EventDate.Date >= DateTime.Today && m.EventDate.Date <= DateTime.Today.AddDays(14))
            .ToList();
        foreach (var market in markets)
        {
            if (TaskExists(db, marketEventId: market.Id, titleStartsWith: "Prepare for market")) continue;
            db.BusinessTasks.Add(new BusinessTask
            {
                TaskCode = GenerateTaskCode(),
                Title = $"Prepare for market: {market.Name}",
                Category = BusinessTaskCategory.MarketPrep,
                Priority = BusinessTaskPriority.High,
                Status = BusinessTaskStatus.ToDo,
                DueDate = market.EventDate.AddDays(-2),
                ReminderDate = market.EventDate.AddDays(-3),
                MarketEventId = market.Id,
                Description = "Automatically suggested for an upcoming market event.",
                ShowOnDashboard = true
            });
            created++;
        }
        return created;
    }

    private static int AddTasksForListingsNeedingWork(AppDbContext db)
    {
        var created = 0;
        var listings = db.OnlineListings.AsNoTracking().AsEnumerable()
            .Where(l => l.Status == OnlineListingStatus.NeedsPhotos || l.Status == OnlineListingStatus.NeedsDescription || l.Status == OnlineListingStatus.ReadyToList)
            .Take(20)
            .ToList();
        foreach (var listing in listings)
        {
            var title = listing.Status == OnlineListingStatus.ReadyToList
                ? $"Publish online listing #{listing.Id}"
                : $"Finish online listing #{listing.Id}";
            if (db.BusinessTasks.AsNoTracking().AsEnumerable().Any(t => t.IsOpen && t.Title == title)) continue;
            db.BusinessTasks.Add(new BusinessTask
            {
                TaskCode = GenerateTaskCode(),
                Title = title,
                Category = BusinessTaskCategory.OnlineListing,
                Priority = BusinessTaskPriority.Normal,
                Status = BusinessTaskStatus.ToDo,
                DueDate = DateTime.Today.AddDays(3),
                ReminderDate = DateTime.Today.AddDays(1),
                JewelleryItemId = listing.JewelleryItemId,
                Description = $"Automatically suggested from online listing status: {listing.Status}.",
                ShowOnDashboard = true
            });
            created++;
        }
        return created;
    }

    private static int AddTasksForPurchaseOrdersDue(AppDbContext db)
    {
        var created = 0;
        var orders = db.PurchaseOrders.AsNoTracking().AsEnumerable()
            .Where(p => p.ExpectedDeliveryDate.HasValue && p.ExpectedDeliveryDate.Value.Date <= DateTime.Today.AddDays(7) && p.Status is PurchaseOrderStatus.Ordered or PurchaseOrderStatus.PartiallyReceived)
            .ToList();
        foreach (var order in orders)
        {
            if (TaskExists(db, purchaseOrderId: order.Id, titleStartsWith: "Follow up purchase order")) continue;
            db.BusinessTasks.Add(new BusinessTask
            {
                TaskCode = GenerateTaskCode(),
                Title = $"Follow up purchase order: {order.PurchaseOrderCode}",
                Category = BusinessTaskCategory.Purchasing,
                Priority = BusinessTaskPriority.Normal,
                Status = BusinessTaskStatus.ToDo,
                DueDate = order.ExpectedDeliveryDate,
                ReminderDate = DateTime.Today,
                PurchaseOrderId = order.Id,
                Description = "Automatically suggested because this purchase order is expected soon or may need follow-up.",
                ShowOnDashboard = true
            });
            created++;
        }
        return created;
    }

    private static bool TaskExists(AppDbContext db, int? jobId = null, int? marketEventId = null, int? purchaseOrderId = null, string titleStartsWith = "")
    {
        return db.BusinessTasks.AsNoTracking().AsEnumerable().Any(t =>
            t.IsOpen &&
            (!jobId.HasValue || t.JobId == jobId) &&
            (!marketEventId.HasValue || t.MarketEventId == marketEventId) &&
            (!purchaseOrderId.HasValue || t.PurchaseOrderId == purchaseOrderId) &&
            (string.IsNullOrWhiteSpace(titleStartsWith) || t.Title.StartsWith(titleStartsWith, StringComparison.OrdinalIgnoreCase)));
    }

    public static string CreateWorkQueueReport()
    {
        Directory.CreateDirectory(PrintoutFolder);
        using var db = new AppDbContext();
        var tasks = db.BusinessTasks.AsNoTracking().AsEnumerable()
            .Where(t => t.IsOpen && t.ShowOnDashboard)
            .OrderByDescending(t => t.Priority)
            .ThenBy(t => t.DueDate ?? DateTime.MaxValue)
            .ThenBy(t => t.Title)
            .ToList();

        var path = Path.Combine(PrintoutFolder, $"WorkQueue-{DateTime.Now:yyyyMMdd-HHmmss}.html");
        var html = new StringBuilder();
        AppendHeader(html, "Today's Work Queue");
        html.AppendLine("<section class='card'>");
        html.AppendLine("<h1>Today's Work Queue</h1>");
        html.AppendLine($"<p class='small'>Generated {Html(DateTime.Now.ToString("f"))}</p>");
        AppendTaskTable(html, "Overdue", tasks.Where(t => t.IsOverdue));
        AppendTaskTable(html, "Due Today", tasks.Where(t => t.DueDate.HasValue && t.DueDate.Value.Date == DateTime.Today));
        AppendTaskTable(html, "High Priority", tasks.Where(t => (t.Priority == BusinessTaskPriority.High || t.Priority == BusinessTaskPriority.Urgent) && !t.IsOverdue));
        AppendTaskTable(html, "Upcoming", tasks.Where(t => !t.IsOverdue && (!t.DueDate.HasValue || t.DueDate.Value.Date > DateTime.Today)).Take(30));
        html.AppendLine("</section>");
        AppendFooter(html);
        File.WriteAllText(path, html.ToString());
        return path;
    }

    public static string CreateTaskReport()
    {
        Directory.CreateDirectory(PrintoutFolder);
        using var db = new AppDbContext();
        var tasks = db.BusinessTasks.AsNoTracking().AsEnumerable().ToList();
        var open = tasks.Where(t => t.IsOpen).ToList();
        var path = Path.Combine(PrintoutFolder, $"TaskReport-{DateTime.Now:yyyyMMdd-HHmmss}.html");

        var html = new StringBuilder();
        AppendHeader(html, "Task & Reminder Report");
        html.AppendLine("<section class='card'>");
        html.AppendLine("<h1>Task & Reminder Report</h1>");
        html.AppendLine($"<p class='small'>Generated {Html(DateTime.Now.ToString("f"))}</p>");
        html.AppendLine("<div class='summary'>");
        html.AppendLine(SummaryBox("Open Tasks", open.Count.ToString()));
        html.AppendLine(SummaryBox("Overdue", open.Count(t => t.IsOverdue).ToString()));
        html.AppendLine(SummaryBox("Due Today", open.Count(t => t.DueDate.HasValue && t.DueDate.Value.Date == DateTime.Today).ToString()));
        html.AppendLine(SummaryBox("High / Urgent", open.Count(t => t.Priority is BusinessTaskPriority.High or BusinessTaskPriority.Urgent).ToString()));
        html.AppendLine("</div>");
        foreach (var group in open.GroupBy(t => t.Category).OrderBy(g => g.Key.ToString()))
            AppendTaskTable(html, group.Key.ToString(), group.OrderByDescending(t => t.Priority).ThenBy(t => t.DueDate ?? DateTime.MaxValue));
        html.AppendLine("</section>");
        AppendFooter(html);
        File.WriteAllText(path, html.ToString());
        return path;
    }

    public static void OpenInDefaultApp(string path)
    {
        Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
    }

    private static void AppendTaskTable(StringBuilder html, string heading, IEnumerable<BusinessTask> rows)
    {
        var list = rows.ToList();
        html.AppendLine($"<h2>{Html(heading)} <span class='small'>({list.Count})</span></h2>");
        if (list.Count == 0)
        {
            html.AppendLine("<p class='small'>No tasks in this group.</p>");
            return;
        }

        html.AppendLine("<table><thead><tr><th>Priority</th><th>Due</th><th>Task</th><th>Status</th><th>Linked</th><th>Notes</th></tr></thead><tbody>");
        foreach (var task in list)
        {
            html.AppendLine("<tr>");
            html.AppendLine($"<td>{Html(task.Priority.ToString())}</td>");
            html.AppendLine($"<td>{Html(task.DueDate?.ToShortDateString() ?? "")}</td>");
            html.AppendLine($"<td><b>{Html(task.Title)}</b><br><span class='small'>{Html(task.TaskCode)}</span></td>");
            html.AppendLine($"<td>{Html(task.Status.ToString())}</td>");
            html.AppendLine($"<td>{Html(GetLinkedSummary(task))}</td>");
            html.AppendLine($"<td>{Html(task.Description ?? task.FollowUpNotes ?? string.Empty)}</td>");
            html.AppendLine("</tr>");
        }
        html.AppendLine("</tbody></table>");
    }

    private static string GetLinkedSummary(BusinessTask task)
    {
        var parts = new List<string>();
        if (task.CustomerId.HasValue) parts.Add($"Customer #{task.CustomerId}");
        if (task.JobId.HasValue) parts.Add($"Job #{task.JobId}");
        if (task.JewelleryItemId.HasValue) parts.Add($"Stock #{task.JewelleryItemId}");
        if (task.StoneId.HasValue) parts.Add($"Stone #{task.StoneId}");
        if (task.MarketEventId.HasValue) parts.Add($"Market #{task.MarketEventId}");
        if (task.ProductionBatchId.HasValue) parts.Add($"Batch #{task.ProductionBatchId}");
        if (task.PurchaseOrderId.HasValue) parts.Add($"PO #{task.PurchaseOrderId}");
        return parts.Count == 0 ? "General" : string.Join(", ", parts);
    }

    private static string SummaryBox(string label, string value)
    {
        return $"<div class='box'><span>{Html(label)}</span><strong>{Html(value)}</strong></div>";
    }

    private static void AppendHeader(StringBuilder html, string title)
    {
        var settings = BusinessSettingsService.Load();
        html.AppendLine("<!doctype html>");
        html.AppendLine("<html><head><meta charset='utf-8'>");
        html.AppendLine($"<title>{Html(title)}</title>");
        html.AppendLine("<style>");
        html.AppendLine("body { font-family: Segoe UI, Arial, sans-serif; margin: 32px; color: #1f2937; line-height: 1.45; }");
        html.AppendLine("h1 { margin-bottom: 4px; } h2 { margin-top: 24px; color: #111827; }");
        html.AppendLine(".card { border: 1px solid #d1d5db; border-radius: 12px; padding: 22px; }");
        html.AppendLine(".small { color: #6b7280; font-size: 12px; } table { width: 100%; border-collapse: collapse; margin-top: 8px; }");
        html.AppendLine("th, td { text-align: left; border-bottom: 1px solid #e5e7eb; padding: 8px; vertical-align: top; } th { background: #f3f4f6; }");
        html.AppendLine(".summary { display: flex; flex-wrap: wrap; gap: 10px; margin: 14px 0; } .box { border: 1px solid #d1d5db; border-radius: 10px; padding: 10px 14px; min-width: 140px; } .box span { display: block; color: #6b7280; font-size: 12px; } .box strong { font-size: 22px; }");
        html.AppendLine("@media print { body { margin: 12mm; } }");
        html.AppendLine("</style></head><body>");
        html.AppendLine($"<p class='small'>{Html(settings.BusinessName)} • {Html(settings.Email)} • {Html(settings.Phone)}</p>");
    }

    private static void AppendFooter(StringBuilder html)
    {
        var settings = BusinessSettingsService.Load();
        html.AppendLine($"<p class='small'>{Html(settings.DocumentFooterText)}</p>");
        html.AppendLine("</body></html>");
    }

    private static string Html(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);
}
