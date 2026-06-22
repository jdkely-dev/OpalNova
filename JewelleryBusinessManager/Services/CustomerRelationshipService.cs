using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using Microsoft.EntityFrameworkCore;
using JewelleryBusinessManager.Data;
using JewelleryBusinessManager.Models;

namespace JewelleryBusinessManager.Services;

public static class CustomerRelationshipService
{
    private static string PrintoutFolder => BusinessSettingsService.GetPrintoutFolder();

    public static string CreateCustomerSummaryCard(Customer customer)
    {
        Directory.CreateDirectory(PrintoutFolder);
        using var db = new AppDbContext();

        var jobs = db.Jobs.AsNoTracking().AsEnumerable().Where(j => j.CustomerId == customer.Id).OrderByDescending(j => j.DateReceived).ToList();
        var sales = db.Sales.AsNoTracking().AsEnumerable().Where(s => s.CustomerId == customer.Id).OrderByDescending(s => s.SaleDate).ToList();
        var payments = db.Payments.AsNoTracking().AsEnumerable().Where(p => p.CustomerId == customer.Id || jobs.Any(j => p.JobId == j.Id) || sales.Any(s => p.SaleId == s.Id)).OrderByDescending(p => p.PaymentDate).ToList();
        var tasks = db.BusinessTasks.AsNoTracking().AsEnumerable().Where(t => t.CustomerId == customer.Id).OrderBy(t => t.DueDate ?? DateTime.MaxValue).ToList();
        var openTasks = tasks.Where(t => t.Status != BusinessTaskStatus.Completed && t.Status != BusinessTaskStatus.Cancelled).ToList();
        var activeJobs = jobs.Where(j => j.Status != JobStatus.Completed && j.Status != JobStatus.Cancelled).ToList();
        var lastSale = sales.FirstOrDefault();
        var lastJob = jobs.FirstOrDefault();
        var lastPayment = payments.FirstOrDefault();
        var nextFollowUp = openTasks.OrderBy(t => t.DueDate ?? DateTime.MaxValue).FirstOrDefault();
        var lifetimeSales = sales.Sum(s => s.SaleAmount);
        var outstandingBalance = activeJobs.Sum(j => Math.Max(0, j.BalanceOwing));

        var fileName = SafeFileName($"CustomerSummary_{customer.FullName}_{customer.Id}.html");
        var path = Path.Combine(PrintoutFolder, fileName);

        var html = new StringBuilder();
        html.Append(HtmlHeader("Customer Summary Card"));
        html.AppendLine("<section class='card'>");
        html.AppendLine("<h1>Customer Summary Card</h1>");
        html.AppendLine($"<p class='small'>Generated {Html(DateTime.Now.ToString("f", CultureInfo.CurrentCulture))}</p>");
        html.AppendLine(Row("Customer", customer.FullName));
        html.AppendLine(Row("Phone", customer.Phone ?? string.Empty));
        html.AppendLine(Row("Email", customer.Email ?? string.Empty));
        html.AppendLine(Row("Instagram", customer.InstagramHandle ?? string.Empty));
        html.AppendLine(Row("Address", customer.Address ?? string.Empty));
        html.AppendLine("<h2>Relationship Snapshot</h2>");
        html.AppendLine("<div class='summary-grid'>");
        html.AppendLine(Tile("Jobs", jobs.Count.ToString(CultureInfo.InvariantCulture), $"{activeJobs.Count} active"));
        html.AppendLine(Tile("Sales", sales.Count.ToString(CultureInfo.InvariantCulture), Money(lifetimeSales)));
        html.AppendLine(Tile("Payments", payments.Count.ToString(CultureInfo.InvariantCulture), lastPayment == null ? "No payments" : $"Last: {lastPayment.PaymentDate:d}"));
        html.AppendLine(Tile("Open Follow-ups", openTasks.Count.ToString(CultureInfo.InvariantCulture), nextFollowUp?.DueDate?.ToShortDateString() ?? "None due"));
        html.AppendLine(Tile("Outstanding Balance", Money(outstandingBalance), "Active jobs only"));
        html.AppendLine(Tile("Last Activity", MostRecentDate(lastSale?.SaleDate, lastJob?.DateReceived, lastPayment?.PaymentDate), "Sale, job or payment"));
        html.AppendLine("</div>");
        html.AppendLine("<h2>Preferences</h2>");
        html.AppendLine(Row("Ring Sizes", customer.RingSizes ?? string.Empty));
        html.AppendLine(Row("Preferred Metals", customer.PreferredMetals ?? string.Empty));
        html.AppendLine(Row("Preferred Stones", customer.PreferredStones ?? string.Empty));
        html.AppendLine(NotesBlock("Customer Notes", customer.Notes));
        html.AppendLine("<h2>Current Follow-up</h2>");
        html.AppendLine(nextFollowUp == null
            ? "<p>No open customer follow-up task is currently linked to this customer.</p>"
            : $"<p><strong>{Html(nextFollowUp.Title)}</strong><br>Due: {Html(nextFollowUp.DueDate?.ToShortDateString() ?? "No due date")}<br>{Html(nextFollowUp.Description ?? string.Empty)}</p>");
        AppendJobsTable(html, jobs.Take(8).ToList());
        AppendSalesTable(html, sales.Take(8).ToList());
        AppendTasksTable(html, openTasks.Take(8).ToList());
        html.AppendLine("</section>");
        html.Append(HtmlFooter());
        File.WriteAllText(path, html.ToString());
        return path;
    }

    public static string CreateCustomerRelationshipReport()
    {
        Directory.CreateDirectory(PrintoutFolder);
        using var db = new AppDbContext();

        var customers = db.Customers.AsNoTracking().AsEnumerable().OrderBy(c => c.FullName).ToList();
        var jobs = db.Jobs.AsNoTracking().AsEnumerable().ToList();
        var sales = db.Sales.AsNoTracking().AsEnumerable().ToList();
        var tasks = db.BusinessTasks.AsNoTracking().AsEnumerable().ToList();
        var payments = db.Payments.AsNoTracking().AsEnumerable().ToList();
        var path = Path.Combine(PrintoutFolder, $"CustomerRelationshipReport_{DateTime.Now:yyyyMMdd_HHmmss}.html");

        var html = new StringBuilder();
        html.Append(HtmlHeader("Customer Relationship Report"));
        html.AppendLine("<section class='card'>");
        html.AppendLine("<h1>Customer Relationship Report</h1>");
        html.AppendLine($"<p class='small'>Generated {Html(DateTime.Now.ToString("f", CultureInfo.CurrentCulture))}</p>");
        html.AppendLine(Row("Customers", customers.Count.ToString(CultureInfo.InvariantCulture)));
        html.AppendLine(Row("Customers with open follow-ups", customers.Count(c => tasks.Any(t => t.CustomerId == c.Id && t.Status != BusinessTaskStatus.Completed && t.Status != BusinessTaskStatus.Cancelled)).ToString(CultureInfo.InvariantCulture)));
        html.AppendLine(Row("Customers with active jobs", customers.Count(c => jobs.Any(j => j.CustomerId == c.Id && j.Status != JobStatus.Completed && j.Status != JobStatus.Cancelled)).ToString(CultureInfo.InvariantCulture)));
        html.AppendLine("<h2>Customer Overview</h2>");
        html.AppendLine("<table><tr><th>Customer</th><th>Contact</th><th>Jobs</th><th>Active Jobs</th><th>Sales</th><th>Total Sales</th><th>Open Follow-ups</th><th>Last Activity</th><th>Next Follow-up</th></tr>");
        foreach (var customer in customers)
        {
            var customerJobs = jobs.Where(j => j.CustomerId == customer.Id).ToList();
            var customerSales = sales.Where(s => s.CustomerId == customer.Id).ToList();
            var customerPayments = payments.Where(p => p.CustomerId == customer.Id || customerJobs.Any(j => p.JobId == j.Id) || customerSales.Any(s => p.SaleId == s.Id)).ToList();
            var customerTasks = tasks.Where(t => t.CustomerId == customer.Id && t.Status != BusinessTaskStatus.Completed && t.Status != BusinessTaskStatus.Cancelled).OrderBy(t => t.DueDate ?? DateTime.MaxValue).ToList();
            var lastActivity = MostRecentDate(customerSales.Select(s => s.SaleDate).Concat(customerJobs.Select(j => j.DateReceived)).Concat(customerPayments.Select(p => p.PaymentDate)).ToArray());
            html.AppendLine($"<tr><td>{Html(customer.FullName)}</td><td>{Html(CompactContact(customer))}</td><td>{customerJobs.Count}</td><td>{customerJobs.Count(j => j.Status != JobStatus.Completed && j.Status != JobStatus.Cancelled)}</td><td>{customerSales.Count}</td><td>{Money(customerSales.Sum(s => s.SaleAmount))}</td><td>{customerTasks.Count}</td><td>{Html(lastActivity)}</td><td>{Html(customerTasks.FirstOrDefault()?.DueDate?.ToShortDateString() ?? string.Empty)}</td></tr>");
        }
        html.AppendLine("</table>");
        html.AppendLine("</section>");
        html.Append(HtmlFooter());
        File.WriteAllText(path, html.ToString());
        return path;
    }

    public static BusinessTask CreateFollowUpTask(Customer customer)
    {
        return new BusinessTask
        {
            TaskCode = TaskWorkflowService.GenerateTaskCode(),
            Title = string.IsNullOrWhiteSpace(customer.FullName) ? "Customer follow-up" : $"Follow up with {customer.FullName}",
            Category = BusinessTaskCategory.CustomerFollowUp,
            Status = BusinessTaskStatus.ToDo,
            Priority = BusinessTaskPriority.Normal,
            DueDate = DateTime.Today.AddDays(2),
            ReminderDate = DateTime.Today.AddDays(1),
            CustomerId = customer.Id,
            Description = "Customer relationship follow-up. Add the reason, next step, quote reminder, collection reminder or after-sale check-in details.",
            FollowUpNotes = BuildPreferenceSummary(customer),
            ShowOnDashboard = true
        };
    }

    private static void AppendJobsTable(StringBuilder html, List<Job> jobs)
    {
        html.AppendLine("<h2>Recent Jobs</h2>");
        html.AppendLine("<table><tr><th>Job</th><th>Status</th><th>Received</th><th>Due</th><th>Quote</th><th>Balance</th></tr>");
        foreach (var job in jobs)
        {
            var price = job.FinalPrice > 0 ? job.FinalPrice : job.QuoteAmount;
            html.AppendLine($"<tr><td>{Html(job.ToString())}</td><td>{Html(job.Status.ToString())}</td><td>{Html(job.DateReceived.ToShortDateString())}</td><td>{Html(job.DueDate?.ToShortDateString() ?? string.Empty)}</td><td>{Money(price)}</td><td>{Money(job.BalanceOwing)}</td></tr>");
        }
        html.AppendLine("</table>");
    }

    private static void AppendSalesTable(StringBuilder html, List<Sale> sales)
    {
        html.AppendLine("<h2>Recent Sales</h2>");
        html.AppendLine("<table><tr><th>Date</th><th>Amount</th><th>Location</th><th>Method</th><th>Notes</th></tr>");
        foreach (var sale in sales)
            html.AppendLine($"<tr><td>{Html(sale.SaleDate.ToShortDateString())}</td><td>{Money(sale.SaleAmount)}</td><td>{Html(sale.SaleLocation.ToString())}</td><td>{Html(sale.PaymentMethod.ToString())}</td><td>{Html(sale.Notes ?? string.Empty)}</td></tr>");
        html.AppendLine("</table>");
    }

    private static void AppendTasksTable(StringBuilder html, List<BusinessTask> tasks)
    {
        html.AppendLine("<h2>Open Customer Tasks</h2>");
        html.AppendLine("<table><tr><th>Task</th><th>Priority</th><th>Due</th><th>Status</th><th>Notes</th></tr>");
        foreach (var task in tasks)
            html.AppendLine($"<tr><td>{Html(task.Title)}</td><td>{Html(task.Priority.ToString())}</td><td>{Html(task.DueDate?.ToShortDateString() ?? string.Empty)}</td><td>{Html(task.Status.ToString())}</td><td>{Html(task.FollowUpNotes ?? task.Description ?? string.Empty)}</td></tr>");
        html.AppendLine("</table>");
    }

    private static string BuildPreferenceSummary(Customer customer)
    {
        var parts = new[]
        {
            string.IsNullOrWhiteSpace(customer.RingSizes) ? null : $"Ring sizes: {customer.RingSizes}",
            string.IsNullOrWhiteSpace(customer.PreferredMetals) ? null : $"Preferred metals: {customer.PreferredMetals}",
            string.IsNullOrWhiteSpace(customer.PreferredStones) ? null : $"Preferred stones: {customer.PreferredStones}",
            string.IsNullOrWhiteSpace(customer.Notes) ? null : $"Notes: {customer.Notes}"
        }.Where(x => !string.IsNullOrWhiteSpace(x));
        return string.Join(Environment.NewLine, parts);
    }

    private static string MostRecentDate(params DateTime?[] dates)
    {
        var date = dates.Where(d => d.HasValue).Select(d => d!.Value.Date).DefaultIfEmpty(DateTime.MinValue).Max();
        return date == DateTime.MinValue ? "No activity" : date.ToShortDateString();
    }

    private static string MostRecentDate(params DateTime[] dates)
    {
        if (dates.Length == 0) return "No activity";
        var date = dates.Select(d => d.Date).DefaultIfEmpty(DateTime.MinValue).Max();
        return date == DateTime.MinValue ? "No activity" : date.ToShortDateString();
    }

    private static string CompactContact(Customer customer)
    {
        if (!string.IsNullOrWhiteSpace(customer.Phone)) return customer.Phone!;
        if (!string.IsNullOrWhiteSpace(customer.Email)) return customer.Email!;
        if (!string.IsNullOrWhiteSpace(customer.InstagramHandle)) return customer.InstagramHandle!;
        return string.Empty;
    }

    private static string Tile(string title, string value, string note) => $"<div class='tile'><span>{Html(title)}</span><strong>{Html(value)}</strong><em>{Html(note)}</em></div>";
    private static string Row(string key, string value) => $"<div class='row'><div class='key'>{Html(key)}</div><div class='value'>{Html(value)}</div></div>";
    private static string NotesBlock(string title, string? value) => $"<h2>{Html(title)}</h2><div class='notes'>{Html(value ?? string.Empty)}</div>";
    private static string Money(decimal value) => value.ToString("C", CultureInfo.CurrentCulture);
    private static string Html(string value) => WebUtility.HtmlEncode(value);
    private static string SafeFileName(string name) => string.Join("_", name.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries)).Trim();

    private static string HtmlHeader(string title)
    {
        var settings = BusinessSettingsService.Load();
        var html = new StringBuilder();
        html.AppendLine("<!doctype html>");
        html.AppendLine("<html>");
        html.AppendLine("<head>");
        html.AppendLine("<meta charset=\"utf-8\">");
        html.AppendLine($"<title>{Html(title)}</title>");
        html.AppendLine("<style>");
        html.AppendLine("body { font-family: Arial, sans-serif; margin: 24px; color: #222; }");
        html.AppendLine(".brand { display: flex; gap: 16px; align-items: center; border-bottom: 2px solid #222; padding-bottom: 12px; margin-bottom: 18px; }");
        html.AppendLine(".logo { max-width: 120px; max-height: 90px; object-fit: contain; }");
        html.AppendLine(".brand-title { margin: 0; font-size: 28px; }");
        html.AppendLine(".brand-details { font-size: 12px; color: #444; line-height: 1.4; }");
        html.AppendLine(".footer { margin-top: 24px; border-top: 1px solid #ccc; padding-top: 10px; font-size: 11px; color: #555; white-space: pre-wrap; }");
        html.AppendLine(".card { max-width: 1120px; border: 1px solid #ccc; padding: 22px; }");
        html.AppendLine("h1 { margin: 0 0 12px 0; }");
        html.AppendLine("h2 { margin: 20px 0 8px 0; }");
        html.AppendLine(".row { display: flex; border-bottom: 1px solid #eee; padding: 6px 0; }");
        html.AppendLine(".key { width: 210px; font-weight: bold; }");
        html.AppendLine(".value { flex: 1; }");
        html.AppendLine(".notes { white-space: pre-wrap; border: 1px solid #ddd; min-height: 54px; padding: 8px; margin-bottom: 10px; }");
        html.AppendLine(".summary-grid { display: grid; grid-template-columns: repeat(3, 1fr); gap: 12px; margin: 12px 0 18px 0; }");
        html.AppendLine(".tile { border: 1px solid #ddd; padding: 12px; background: #fafafa; }");
        html.AppendLine(".tile span { display: block; font-size: 12px; color: #555; }");
        html.AppendLine(".tile strong { display: block; font-size: 22px; margin: 6px 0; }");
        html.AppendLine(".tile em { display: block; font-size: 11px; color: #555; font-style: normal; }");
        html.AppendLine(".small { font-size: 12px; color: #555; }");
        html.AppendLine("table { border-collapse: collapse; width: 100%; margin-bottom: 18px; font-size: 12px; }");
        html.AppendLine("th, td { border: 1px solid #ddd; padding: 7px; text-align: left; }");
        html.AppendLine("th { background: #f2f2f2; }");
        html.AppendLine("@media print { button { display: none; } body { margin: 8mm; } .card { border: none; } .brand { break-inside: avoid; } }");
        html.AppendLine("</style>");
        html.AppendLine("</head>");
        html.AppendLine("<body>");
        html.AppendLine("<div class='brand'>");
        if (!string.IsNullOrWhiteSpace(settings.LogoPath) && File.Exists(settings.LogoPath))
            html.AppendLine($"<img class='logo' src='{Html(settings.LogoPath)}' alt='Logo'>");
        html.AppendLine("<div>");
        html.AppendLine($"<h1 class='brand-title'>{Html(settings.BusinessName)}</h1>");
        html.AppendLine($"<div class='brand-details'>{Html(settings.OwnerName)}<br>{Html(settings.Email)}<br>{Html(settings.Phone)}<br>{Html(settings.Website)}</div>");
        html.AppendLine("</div></div>");
        return html.ToString();
    }

    private static string HtmlFooter()
    {
        var settings = BusinessSettingsService.Load();
        return $"<div class='footer'>{Html(settings.DocumentFooterText)}</div></body></html>";
    }
}
