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
        var quotes = db.CustomQuotes.AsNoTracking().AsEnumerable().Where(q => q.CustomerId == customer.Id).OrderByDescending(q => q.QuoteDate).ToList();
        var quoteIds = quotes.Select(q => q.Id).ToHashSet();
        var quoteOptions = db.QuoteOptions.AsNoTracking().AsEnumerable().Where(o => quoteIds.Contains(o.CustomQuoteId)).ToList();
        var sales = db.Sales.AsNoTracking().AsEnumerable().Where(s => s.CustomerId == customer.Id).OrderByDescending(s => s.SaleDate).ToList();
        var payments = db.Payments.AsNoTracking().AsEnumerable().Where(p => p.CustomerId == customer.Id || jobs.Any(j => p.JobId == j.Id) || sales.Any(s => p.SaleId == s.Id)).OrderByDescending(p => p.PaymentDate).ToList();
        var tasks = db.BusinessTasks.AsNoTracking().AsEnumerable().Where(t => t.CustomerId == customer.Id).OrderBy(t => t.DueDate ?? DateTime.MaxValue).ToList();
        var openTasks = tasks.Where(t => t.Status != BusinessTaskStatus.Completed && t.Status != BusinessTaskStatus.Cancelled).ToList();
        var activeJobs = jobs.Where(j => j.Status != JobStatus.Completed && j.Status != JobStatus.Cancelled).ToList();
        var openQuotes = quotes.Where(q => !q.AcceptedOptionId.HasValue && !q.LinkedJobId.HasValue && !q.Status.Contains("Cancelled", StringComparison.OrdinalIgnoreCase)).ToList();
        var timeline = BuildCustomerTimelineEvents(quotes, quoteOptions, jobs, sales, payments, tasks);
        var lastSale = sales.FirstOrDefault();
        var lastJob = jobs.FirstOrDefault();
        var lastPayment = payments.FirstOrDefault();
        var nextFollowUp = openTasks.OrderBy(t => t.DueDate ?? DateTime.MaxValue).FirstOrDefault();
        var lifetimeSales = sales.Sum(s => s.SaleAmount);
        var outstandingBalance = activeJobs.Sum(j => Math.Max(0, j.BalanceOwing));
        var valueProfile = BuildCustomerValueProfile(customer, jobs, quotes, sales, payments, tasks);
        var segmentProfile = BuildCustomerSegmentProfile(customer, jobs, quotes, sales, tasks, valueProfile);
        var templates = BuildCommunicationTemplates(customer, jobs, quotes, sales, tasks, valueProfile, segmentProfile);

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
        html.AppendLine(Tile("Quotes", quotes.Count.ToString(CultureInfo.InvariantCulture), $"{openQuotes.Count} open"));
        html.AppendLine(Tile("Sales", sales.Count.ToString(CultureInfo.InvariantCulture), Money(lifetimeSales)));
        html.AppendLine(Tile("Payments", payments.Count.ToString(CultureInfo.InvariantCulture), lastPayment == null ? "No payments" : $"Last: {lastPayment.PaymentDate:d}"));
        html.AppendLine(Tile("Open Follow-ups", openTasks.Count.ToString(CultureInfo.InvariantCulture), nextFollowUp?.DueDate?.ToShortDateString() ?? "None due"));
        html.AppendLine(Tile("Outstanding Balance", Money(outstandingBalance), "Active jobs only"));
        html.AppendLine(Tile("Lifetime Value", Money(valueProfile.LifetimeValue), valueProfile.ValueTier));
        html.AppendLine(Tile("Segment", segmentProfile.PrimarySegment, segmentProfile.Confidence));
        html.AppendLine(Tile("Last Activity", MostRecentDate(lastSale?.SaleDate, lastJob?.DateReceived, lastPayment?.PaymentDate), "Sale, job or payment"));
        html.AppendLine("</div>");
        AppendRelationshipGuidance(html, valueProfile, segmentProfile);
        html.AppendLine("<h2>Preferences</h2>");
        html.AppendLine(Row("Ring Sizes", customer.RingSizes ?? string.Empty));
        html.AppendLine(Row("Preferred Metals", customer.PreferredMetals ?? string.Empty));
        html.AppendLine(Row("Preferred Stones", customer.PreferredStones ?? string.Empty));
        html.AppendLine(NotesBlock("Customer Notes", customer.Notes));
        html.AppendLine("<h2>Current Follow-up</h2>");
        html.AppendLine(nextFollowUp == null
            ? "<p>No open customer follow-up task is currently linked to this customer.</p>"
            : $"<p><strong>{Html(nextFollowUp.Title)}</strong><br>Due: {Html(nextFollowUp.DueDate?.ToShortDateString() ?? "No due date")}<br>{Html(nextFollowUp.Description ?? string.Empty)}</p>");
        AppendQuotesTable(html, quotes.Take(8).ToList(), quoteOptions);
        AppendJobsTable(html, jobs.Take(8).ToList());
        AppendSalesTable(html, sales.Take(8).ToList());
        AppendTasksTable(html, openTasks.Take(8).ToList());
        AppendTimelineTable(html, timeline.Take(10).ToList(), "Recent Timeline");
        AppendCommunicationTemplates(html, templates.Take(3).ToList(), "Suggested Message Starters");
        html.AppendLine("</section>");
        html.Append(HtmlFooter());
        File.WriteAllText(path, html.ToString());
        return path;
    }

    public static string CreateCustomerTimeline(Customer customer)
    {
        Directory.CreateDirectory(PrintoutFolder);
        using var db = new AppDbContext();

        var quotes = db.CustomQuotes.AsNoTracking().AsEnumerable().Where(q => q.CustomerId == customer.Id).OrderByDescending(q => q.QuoteDate).ToList();
        var quoteIds = quotes.Select(q => q.Id).ToHashSet();
        var quoteOptions = db.QuoteOptions.AsNoTracking().AsEnumerable().Where(o => quoteIds.Contains(o.CustomQuoteId)).ToList();
        var jobs = db.Jobs.AsNoTracking().AsEnumerable().Where(j => j.CustomerId == customer.Id).OrderByDescending(j => j.DateReceived).ToList();
        var sales = db.Sales.AsNoTracking().AsEnumerable().Where(s => s.CustomerId == customer.Id).OrderByDescending(s => s.SaleDate).ToList();
        var payments = db.Payments.AsNoTracking().AsEnumerable().Where(p => p.CustomerId == customer.Id || jobs.Any(j => p.JobId == j.Id) || sales.Any(s => p.SaleId == s.Id)).OrderByDescending(p => p.PaymentDate).ToList();
        var tasks = db.BusinessTasks.AsNoTracking().AsEnumerable().Where(t => t.CustomerId == customer.Id).ToList();
        var timeline = BuildCustomerTimelineEvents(quotes, quoteOptions, jobs, sales, payments, tasks);
        var activeJobs = jobs.Where(j => j.Status != JobStatus.Completed && j.Status != JobStatus.Cancelled).ToList();
        var openTasks = tasks.Where(t => t.Status != BusinessTaskStatus.Completed && t.Status != BusinessTaskStatus.Cancelled).ToList();
        var outstandingBalance = activeJobs.Sum(j => Math.Max(0, j.BalanceOwing));
        var valueProfile = BuildCustomerValueProfile(customer, jobs, quotes, sales, payments, tasks);
        var segmentProfile = BuildCustomerSegmentProfile(customer, jobs, quotes, sales, tasks, valueProfile);

        var fileName = SafeFileName($"CustomerTimeline_{customer.FullName}_{customer.Id}.html");
        var path = Path.Combine(PrintoutFolder, fileName);

        var html = new StringBuilder();
        html.Append(HtmlHeader("Customer Timeline"));
        html.AppendLine("<section class='card'>");
        html.AppendLine("<h1>Customer Timeline</h1>");
        html.AppendLine($"<p class='small'>Generated {Html(DateTime.Now.ToString("f", CultureInfo.CurrentCulture))}</p>");
        html.AppendLine(Row("Customer", customer.FullName));
        html.AppendLine(Row("Contact", CompactContact(customer)));
        html.AppendLine("<div class='summary-grid'>");
        html.AppendLine(Tile("Timeline Events", timeline.Count.ToString(CultureInfo.InvariantCulture), "quotes, jobs, sales, payments, tasks"));
        html.AppendLine(Tile("Active Jobs", activeJobs.Count.ToString(CultureInfo.InvariantCulture), Money(outstandingBalance)));
        html.AppendLine(Tile("Open Follow-ups", openTasks.Count.ToString(CultureInfo.InvariantCulture), openTasks.OrderBy(t => t.DueDate ?? DateTime.MaxValue).FirstOrDefault()?.DueDate?.ToShortDateString() ?? "None due"));
        html.AppendLine(Tile("Lifetime Value", Money(valueProfile.LifetimeValue), valueProfile.ValueTier));
        html.AppendLine(Tile("Segment", segmentProfile.PrimarySegment, segmentProfile.Confidence));
        html.AppendLine("</div>");
        AppendRelationshipGuidance(html, valueProfile, segmentProfile);
        html.AppendLine("<h2>Preferences</h2>");
        html.AppendLine(Row("Ring Sizes", customer.RingSizes ?? string.Empty));
        html.AppendLine(Row("Preferred Metals", customer.PreferredMetals ?? string.Empty));
        html.AppendLine(Row("Preferred Stones", customer.PreferredStones ?? string.Empty));
        AppendTimelineTable(html, timeline, "Full Customer Timeline");
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
        var quotes = db.CustomQuotes.AsNoTracking().AsEnumerable().ToList();
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
        html.AppendLine("<table><tr><th>Customer</th><th>Contact</th><th>Segment</th><th>Jobs</th><th>Active Jobs</th><th>Sales</th><th>Lifetime Value</th><th>Value Guidance</th><th>Open Follow-ups</th><th>Last Activity</th><th>Next Follow-up</th><th>Suggested Next Step</th><th>Reminder Opportunity</th></tr>");
        foreach (var customer in customers)
        {
            var customerJobs = jobs.Where(j => j.CustomerId == customer.Id).ToList();
            var customerQuotes = quotes.Where(q => q.CustomerId == customer.Id).ToList();
            var customerSales = sales.Where(s => s.CustomerId == customer.Id).ToList();
            var customerPayments = payments.Where(p => p.CustomerId == customer.Id || customerJobs.Any(j => p.JobId == j.Id) || customerSales.Any(s => p.SaleId == s.Id)).ToList();
            var customerTasks = tasks.Where(t => t.CustomerId == customer.Id && t.Status != BusinessTaskStatus.Completed && t.Status != BusinessTaskStatus.Cancelled).OrderBy(t => t.DueDate ?? DateTime.MaxValue).ToList();
            var lastActivity = MostRecentDate(customerSales.Select(s => s.SaleDate).Concat(customerJobs.Select(j => j.DateReceived)).Concat(customerPayments.Select(p => p.PaymentDate)).ToArray());
            var valueProfile = BuildCustomerValueProfile(customer, customerJobs, customerQuotes, customerSales, customerPayments, tasks.Where(t => t.CustomerId == customer.Id).ToList());
            var segmentProfile = BuildCustomerSegmentProfile(customer, customerJobs, customerQuotes, customerSales, tasks.Where(t => t.CustomerId == customer.Id).ToList(), valueProfile);
            html.AppendLine($"<tr><td>{Html(customer.FullName)}</td><td>{Html(CompactContact(customer))}</td><td>{Html(segmentProfile.PrimarySegment)}</td><td>{customerJobs.Count}</td><td>{customerJobs.Count(j => j.Status != JobStatus.Completed && j.Status != JobStatus.Cancelled)}</td><td>{customerSales.Count}</td><td>{Money(valueProfile.LifetimeValue)}</td><td>{Html(valueProfile.ValueTier)}</td><td>{customerTasks.Count}</td><td>{Html(lastActivity)}</td><td>{Html(customerTasks.FirstOrDefault()?.DueDate?.ToShortDateString() ?? string.Empty)}</td><td>{Html(valueProfile.SuggestedNextStep)}</td><td>{Html(segmentProfile.ReminderOpportunity)}</td></tr>");
        }
        html.AppendLine("</table>");
        html.AppendLine("</section>");
        html.Append(HtmlFooter());
        File.WriteAllText(path, html.ToString());
        return path;
    }

    public static string CreateCustomerSegmentReport()
    {
        Directory.CreateDirectory(PrintoutFolder);
        using var db = new AppDbContext();

        var customers = db.Customers.AsNoTracking().AsEnumerable().OrderBy(c => c.FullName).ToList();
        var jobs = db.Jobs.AsNoTracking().AsEnumerable().ToList();
        var quotes = db.CustomQuotes.AsNoTracking().AsEnumerable().ToList();
        var sales = db.Sales.AsNoTracking().AsEnumerable().ToList();
        var tasks = db.BusinessTasks.AsNoTracking().AsEnumerable().ToList();
        var payments = db.Payments.AsNoTracking().AsEnumerable().ToList();
        var path = Path.Combine(PrintoutFolder, $"CustomerSegmentReport_{DateTime.Now:yyyyMMdd_HHmmss}.html");

        var rows = customers.Select(customer =>
        {
            var customerJobs = jobs.Where(j => j.CustomerId == customer.Id).ToList();
            var customerQuotes = quotes.Where(q => q.CustomerId == customer.Id).ToList();
            var customerSales = sales.Where(s => s.CustomerId == customer.Id).ToList();
            var customerPayments = payments.Where(p => p.CustomerId == customer.Id || customerJobs.Any(j => p.JobId == j.Id) || customerSales.Any(s => p.SaleId == s.Id)).ToList();
            var customerTasks = tasks.Where(t => t.CustomerId == customer.Id).ToList();
            var valueProfile = BuildCustomerValueProfile(customer, customerJobs, customerQuotes, customerSales, customerPayments, customerTasks);
            var segmentProfile = BuildCustomerSegmentProfile(customer, customerJobs, customerQuotes, customerSales, customerTasks, valueProfile);
            var nextFollowUp = customerTasks
                .Where(t => t.Status != BusinessTaskStatus.Completed && t.Status != BusinessTaskStatus.Cancelled)
                .OrderBy(t => t.DueDate ?? DateTime.MaxValue)
                .FirstOrDefault();
            var lastActivity = MostRecentDate(customerSales.Select(s => s.SaleDate).Concat(customerJobs.Select(j => j.DateReceived)).Concat(customerPayments.Select(p => p.PaymentDate)).ToArray());
            return new
            {
                Customer = customer,
                Value = valueProfile,
                Segment = segmentProfile,
                NextFollowUp = nextFollowUp,
                LastActivity = lastActivity
            };
        }).ToList();

        var html = new StringBuilder();
        html.Append(HtmlHeader("Customer Segment Report"));
        html.AppendLine("<section class='card'>");
        html.AppendLine("<h1>Customer Segment Report</h1>");
        html.AppendLine($"<p class='small'>Generated {Html(DateTime.Now.ToString("f", CultureInfo.CurrentCulture))}</p>");
        html.AppendLine("<div class='summary-grid'>");
        html.AppendLine(Tile("Customers", customers.Count.ToString(CultureInfo.InvariantCulture), "All customer records"));
        html.AppendLine(Tile("Repeat / High Value", rows.Count(r => r.Value.ValueTier.Contains("repeat", StringComparison.OrdinalIgnoreCase)).ToString(CultureInfo.InvariantCulture), "Repeat guidance"));
        html.AppendLine(Tile("Reminder Opportunities", rows.Count(r => HasSpecificReminderOpportunity(r.Segment.ReminderOpportunity)).ToString(CultureInfo.InvariantCulture), "Notes mention dates, gifts or after-care"));
        html.AppendLine("</div>");
        html.AppendLine("<h2>Segment Overview</h2>");
        html.AppendLine("<table><tr><th>Segment</th><th>Customers</th><th>Total Lifetime Value</th><th>Open Follow-ups</th></tr>");
        foreach (var group in rows.GroupBy(r => r.Segment.PrimarySegment).OrderByDescending(g => g.Count()).ThenBy(g => g.Key))
        {
            html.AppendLine($"<tr><td>{Html(group.Key)}</td><td>{group.Count()}</td><td>{Money(group.Sum(r => r.Value.LifetimeValue))}</td><td>{group.Sum(r => r.Value.OpenTasksCount)}</td></tr>");
        }
        html.AppendLine("</table>");
        html.AppendLine("<h2>Customer Follow-Up Angles</h2>");
        html.AppendLine("<table><tr><th>Customer</th><th>Contact</th><th>Segment</th><th>Confidence</th><th>Rationale</th><th>Value Guidance</th><th>Next Follow-up</th><th>Follow-up Angle</th><th>Reminder Opportunity</th></tr>");
        foreach (var row in rows.OrderBy(r => r.Segment.PrimarySegment).ThenByDescending(r => r.Value.LifetimeValue).ThenBy(r => r.Customer.FullName))
        {
            html.AppendLine($"<tr><td>{Html(row.Customer.FullName)}</td><td>{Html(CompactContact(row.Customer))}</td><td>{Html(row.Segment.PrimarySegment)}</td><td>{Html(row.Segment.Confidence)}</td><td>{Html(row.Segment.Rationale)}</td><td>{Html(row.Value.ValueTier)}</td><td>{Html(row.NextFollowUp?.DueDate?.ToShortDateString() ?? string.Empty)}</td><td>{Html(row.Segment.FollowUpAngle)}</td><td>{Html(row.Segment.ReminderOpportunity)}</td></tr>");
        }
        html.AppendLine("</table>");
        html.AppendLine("</section>");
        html.Append(HtmlFooter());
        File.WriteAllText(path, html.ToString());
        return path;
    }

    public static string CreateCustomerCommunicationTemplates(Customer customer)
    {
        Directory.CreateDirectory(PrintoutFolder);
        using var db = new AppDbContext();

        var jobs = db.Jobs.AsNoTracking().AsEnumerable().Where(j => j.CustomerId == customer.Id).OrderByDescending(j => j.DateReceived).ToList();
        var quotes = db.CustomQuotes.AsNoTracking().AsEnumerable().Where(q => q.CustomerId == customer.Id).OrderByDescending(q => q.QuoteDate).ToList();
        var sales = db.Sales.AsNoTracking().AsEnumerable().Where(s => s.CustomerId == customer.Id).OrderByDescending(s => s.SaleDate).ToList();
        var payments = db.Payments.AsNoTracking().AsEnumerable().Where(p => p.CustomerId == customer.Id || jobs.Any(j => p.JobId == j.Id) || sales.Any(s => p.SaleId == s.Id)).OrderByDescending(p => p.PaymentDate).ToList();
        var tasks = db.BusinessTasks.AsNoTracking().AsEnumerable().Where(t => t.CustomerId == customer.Id).OrderBy(t => t.DueDate ?? DateTime.MaxValue).ToList();
        var valueProfile = BuildCustomerValueProfile(customer, jobs, quotes, sales, payments, tasks);
        var segmentProfile = BuildCustomerSegmentProfile(customer, jobs, quotes, sales, tasks, valueProfile);
        var templates = BuildCommunicationTemplates(customer, jobs, quotes, sales, tasks, valueProfile, segmentProfile);

        var fileName = SafeFileName($"CustomerCommunicationTemplates_{customer.FullName}_{customer.Id}.html");
        var path = Path.Combine(PrintoutFolder, fileName);

        var html = new StringBuilder();
        html.Append(HtmlHeader("Customer Communication Templates"));
        html.AppendLine("<section class='card'>");
        html.AppendLine("<h1>Customer Communication Templates</h1>");
        html.AppendLine($"<p class='small'>Generated {Html(DateTime.Now.ToString("f", CultureInfo.CurrentCulture))}</p>");
        html.AppendLine(Row("Customer", customer.FullName));
        html.AppendLine(Row("Contact", CompactContact(customer)));
        html.AppendLine(Row("Value Guidance", valueProfile.ValueTier));
        html.AppendLine(Row("Customer Segment", $"{segmentProfile.PrimarySegment} ({segmentProfile.Confidence})"));
        html.AppendLine(Row("Segment Follow-up Angle", segmentProfile.FollowUpAngle));
        html.AppendLine(Row("Suggested Next Step", valueProfile.SuggestedNextStep));
        AppendRelationshipGuidance(html, valueProfile, segmentProfile);
        AppendCommunicationTemplates(html, templates, "Message Starters");
        html.AppendLine("</section>");
        html.Append(HtmlFooter());
        File.WriteAllText(path, html.ToString());
        return path;
    }

    public static BusinessTask CreateFollowUpTask(Customer customer)
    {
        using var db = new AppDbContext();
        var jobs = db.Jobs.AsNoTracking().AsEnumerable().Where(j => j.CustomerId == customer.Id).OrderByDescending(j => j.DateReceived).ToList();
        var quotes = db.CustomQuotes.AsNoTracking().AsEnumerable().Where(q => q.CustomerId == customer.Id).OrderByDescending(q => q.QuoteDate).ToList();
        var sales = db.Sales.AsNoTracking().AsEnumerable().Where(s => s.CustomerId == customer.Id).OrderByDescending(s => s.SaleDate).ToList();
        var payments = db.Payments.AsNoTracking().AsEnumerable().Where(p => p.CustomerId == customer.Id || jobs.Any(j => p.JobId == j.Id) || sales.Any(s => p.SaleId == s.Id)).ToList();
        var tasks = db.BusinessTasks.AsNoTracking().AsEnumerable().Where(t => t.CustomerId == customer.Id).ToList();
        var valueProfile = BuildCustomerValueProfile(customer, jobs, quotes, sales, payments, tasks);
        var segmentProfile = BuildCustomerSegmentProfile(customer, jobs, quotes, sales, tasks, valueProfile);
        var templates = BuildCommunicationTemplates(customer, jobs, quotes, sales, tasks, valueProfile, segmentProfile);

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
            FollowUpNotes = BuildFollowUpNotes(customer, valueProfile, segmentProfile, templates),
            ShowOnDashboard = true
        };
    }

    private sealed record CustomerTimelineEvent(DateTime Date, string Type, string Title, string Detail, string Status, decimal? Amount);
    private sealed record CustomerValueProfile(decimal LifetimeValue, decimal PaymentsRecorded, decimal OutstandingBalance, int JobsCount, int ActiveJobsCount, int SalesCount, int OpenQuotesCount, int OpenTasksCount, string ValueTier, string SuggestedNextStep, string RepeatFollowUpSuggestion);
    private sealed record CustomerSegmentProfile(string PrimarySegment, string Confidence, string Rationale, string FollowUpAngle, string ReminderOpportunity);
    private sealed record CommunicationTemplate(string Title, string Body);

    private static CustomerValueProfile BuildCustomerValueProfile(
        Customer customer,
        List<Job> jobs,
        List<CustomQuote> quotes,
        List<Sale> sales,
        List<Payment> payments,
        List<BusinessTask> tasks)
    {
        var lifetimeValue = sales.Sum(s => s.SaleAmount);
        var paymentsRecorded = payments.Sum(p => p.Amount);
        var activeJobs = jobs.Where(j => j.Status != JobStatus.Completed && j.Status != JobStatus.Cancelled).ToList();
        var outstandingBalance = activeJobs.Sum(j => Math.Max(0, j.BalanceOwing));
        var openQuotes = quotes.Where(q => !q.AcceptedOptionId.HasValue && !q.LinkedJobId.HasValue && !q.Status.Contains("Cancelled", StringComparison.OrdinalIgnoreCase)).ToList();
        var openTasks = tasks.Where(t => t.Status != BusinessTaskStatus.Completed && t.Status != BusinessTaskStatus.Cancelled).ToList();
        var lastActivity = MostRecentActivityDate(jobs, sales, payments, tasks);

        var valueTier = lifetimeValue >= 5000m || sales.Count >= 5
            ? "High-value repeat customer"
            : sales.Count >= 2 || jobs.Count >= 2
                ? "Repeat customer"
                : activeJobs.Count > 0
                    ? "Active project customer"
                    : openQuotes.Count > 0
                        ? "Quote / proposal customer"
                        : lifetimeValue > 0m
                            ? "Past customer"
                            : "New / early relationship";

        var suggestedNextStep = openTasks.Count > 0
            ? $"Review {openTasks.Count} open follow-up task(s)."
            : outstandingBalance > 0m
                ? $"Send a balance or handover payment check for {Money(outstandingBalance)}."
                : openQuotes.Count > 0
                    ? "Send a quote or proposal check-in."
                    : activeJobs.Count > 0
                        ? "Send a production progress or handover update."
                        : lifetimeValue > 0m && lastActivity.HasValue && lastActivity.Value.Date <= DateTime.Today.AddDays(-120)
                            ? "Send an after-care or repeat-customer check-in."
                            : lifetimeValue > 0m
                                ? "Keep warm with care advice or a preference-led future design prompt."
                                : "Confirm preferences and invite the next jewellery brief.";

        var repeatFollowUp = lifetimeValue <= 0m
            ? "Ask about preferred stones, metals, ring sizes and upcoming occasions."
            : sales.Count >= 2 || jobs.Count >= 2
                ? "Thank them as a repeat customer and suggest a future clean/check, matching piece or occasion-based design."
                : "Send an after-care check-in and ask whether they would like reminders for cleaning, sizing or future gifts.";

        return new CustomerValueProfile(
            lifetimeValue,
            paymentsRecorded,
            outstandingBalance,
            jobs.Count,
            activeJobs.Count,
            sales.Count,
            openQuotes.Count,
            openTasks.Count,
            valueTier,
            suggestedNextStep,
            repeatFollowUp);
    }

    private static CustomerSegmentProfile BuildCustomerSegmentProfile(
        Customer customer,
        List<Job> jobs,
        List<CustomQuote> quotes,
        List<Sale> sales,
        List<BusinessTask> tasks,
        CustomerValueProfile profile)
    {
        var text = BuildSegmentText(customer, jobs, quotes, sales, tasks);
        var notesText = customer.Notes ?? string.Empty;
        var hasGiftMarker = ContainsAny(notesText, "birthday", "anniversary", "gift", "occasion", "christmas", "valentine", "wedding");
        var hasAfterCareMarker = ContainsAny(text, "after-care", "aftercare", "clean", "polish", "resize", "repair", "check");
        var lastActivity = MostRecentActivityDate(jobs, sales, new List<Payment>(), tasks);

        string reminderOpportunity;
        if (hasGiftMarker)
        {
            reminderOpportunity = "Customer notes mention a birthday, anniversary, gift or occasion. Check timing before contacting.";
        }
        else if (profile.LifetimeValue > 0m && lastActivity.HasValue && lastActivity.Value.Date <= DateTime.Today.AddDays(-90))
        {
            reminderOpportunity = "Past purchase is old enough for an after-care, cleaning or fit check-in.";
        }
        else if (profile.OpenQuotesCount > 0)
        {
            reminderOpportunity = "Open quote or proposal can be followed up before the customer goes cold.";
        }
        else
        {
            reminderOpportunity = "No explicit occasion marker found. Ask permission before adding personal reminder notes.";
        }

        if (ContainsAny(text, "wholesale", "trade", "stockist", "gallery", "retailer"))
        {
            return new CustomerSegmentProfile(
                "Wholesale / Trade",
                "Notes-based",
                "Customer notes or activity mention wholesale, trade, stockist, gallery or retail context.",
                "Focus on supply timing, repeatable styles, margins and reliable delivery expectations.",
                reminderOpportunity);
        }

        if (sales.Any(s => s.SaleLocation == SaleLocation.Market) || ContainsAny(text, "market", "stall", "show", "fair"))
        {
            return new CustomerSegmentProfile(
                "Market Customer",
                "Activity-based",
                "Customer has market sale history or market-related notes.",
                "Use a short, practical follow-up around care, sizing, matching items or the next market appearance.",
                reminderOpportunity);
        }

        if (ContainsAny(text, "collector", "collection", "collecting") ||
            ((profile.SalesCount >= 2 || profile.LifetimeValue >= 2500m) && ContainsAny(text, "opal", "gemstone", "diamond", "sapphire", "ruby", "emerald", "stone")))
        {
            return new CustomerSegmentProfile(
                "Collector",
                "Preference-based",
                "Customer history or preferences suggest repeated interest in stones, opals, diamonds or collection building.",
                "Lead with new stones, provenance, matching pieces and collection-aware design ideas.",
                reminderOpportunity);
        }

        if (jobs.Any(j => j.Type is JobType.CustomOrder or JobType.Remake or JobType.StoneSetting) ||
            quotes.Count > 0 ||
            ContainsAny(text, "custom", "bespoke", "engagement", "wedding", "remake", "setting", "design"))
        {
            return new CustomerSegmentProfile(
                "Custom Customer",
                "Workflow-based",
                "Customer has custom quote, remake, stone-setting or design workflow history.",
                "Focus on design progress, option comparison, budget clarity and next approval steps.",
                reminderOpportunity);
        }

        if (jobs.Any(j => j.Type is JobType.Repair or JobType.Resize or JobType.CleanAndPolish) ||
            ContainsAny(text, "repair", "resize", "clean", "polish", "maintenance"))
        {
            return new CustomerSegmentProfile(
                "Repair Customer",
                "Workflow-based",
                "Customer history includes repair, resize, clean/polish or maintenance work.",
                "Keep communication simple: intake status, timing, approval needs, care advice and pickup readiness.",
                reminderOpportunity);
        }

        if (profile.SalesCount >= 2 || profile.JobsCount >= 2 || profile.ValueTier.Contains("repeat", StringComparison.OrdinalIgnoreCase))
        {
            return new CustomerSegmentProfile(
                "Repeat Customer",
                "Activity-based",
                "Customer has multiple sales, jobs or repeat-value guidance.",
                "Thank them for repeat business and suggest after-care, matching pieces or occasion-led future work.",
                reminderOpportunity);
        }

        if (profile.OpenQuotesCount > 0)
        {
            return new CustomerSegmentProfile(
                "Quote / Enquiry Customer",
                "Workflow-based",
                "Customer has an open quote or proposal without a converted job.",
                "Focus on clarifying the preferred option, budget, timing and approval path.",
                reminderOpportunity);
        }

        return new CustomerSegmentProfile(
            "New / Early Relationship",
            "Default",
            "Not enough linked activity exists yet to infer a stronger segment.",
            "Confirm preferences, contact method, ring size, stone/metal interests and upcoming occasions.",
            reminderOpportunity);
    }

    private static List<CommunicationTemplate> BuildCommunicationTemplates(
        Customer customer,
        List<Job> jobs,
        List<CustomQuote> quotes,
        List<Sale> sales,
        List<BusinessTask> tasks,
        CustomerValueProfile profile,
        CustomerSegmentProfile segment)
    {
        var name = FirstName(customer);
        var preferences = BuildPreferenceSummary(customer);
        var openQuote = quotes.FirstOrDefault(q => !q.AcceptedOptionId.HasValue && !q.LinkedJobId.HasValue && !q.Status.Contains("Cancelled", StringComparison.OrdinalIgnoreCase));
        var activeJob = jobs.FirstOrDefault(j => j.Status != JobStatus.Completed && j.Status != JobStatus.Cancelled);
        var readyJob = jobs.FirstOrDefault(j => j.Status is JobStatus.ReadyForPickup or JobStatus.ReadyToShip);
        var latestSale = sales.FirstOrDefault();
        var nextTask = tasks.Where(t => t.Status != BusinessTaskStatus.Completed && t.Status != BusinessTaskStatus.Cancelled).OrderBy(t => t.DueDate ?? DateTime.MaxValue).FirstOrDefault();

        var templates = new List<CommunicationTemplate>();

        if (openQuote != null)
        {
            templates.Add(new CommunicationTemplate(
                "Quote / proposal check-in",
                $"Hi {name},\n\nI wanted to check in on {openQuote.QuoteCode}. I am happy to adjust the design, stone, metal or budget if you would like to compare another option.\n\nPlease let me know what feels closest to what you had in mind and I can guide the next step.\n\nKind regards"));
        }

        if (activeJob != null)
        {
            templates.Add(new CommunicationTemplate(
                "Production update",
                $"Hi {name},\n\nA quick update on {activeJob.JobCode} {activeJob.JobTitle}: it is currently at {activeJob.Status}. {(activeJob.DueDate.HasValue ? $"The current target date is {activeJob.DueDate.Value:dd MMM yyyy}." : "I will confirm the next timing update as soon as the workshop schedule is clear.")}\n\nI will let you know if anything changes.\n\nKind regards"));
        }

        if (readyJob != null)
        {
            templates.Add(new CommunicationTemplate(
                readyJob.Status == JobStatus.ReadyToShip ? "Shipping handover message" : "Collection handover message",
                $"Hi {name},\n\n{readyJob.JobCode} {readyJob.JobTitle} is ready for {(readyJob.Status == JobStatus.ReadyToShip ? "shipping" : "collection")}.\n\nI have checked the handover details. Please let me know if you need the payment details, collection timing or shipping information resent.\n\nKind regards"));
        }

        if (latestSale != null)
        {
            templates.Add(new CommunicationTemplate(
                "After-care check-in",
                $"Hi {name},\n\nI hope you are enjoying your piece. I wanted to check that everything is fitting and looking as expected.\n\nIf you have any questions about cleaning, care or future adjustments, please let me know.\n\nKind regards"));
        }

        templates.Add(new CommunicationTemplate(
            "Repeat customer / preference prompt",
            $"Hi {name},\n\nI was reviewing your preferences and thought it would be useful to check whether anything has changed for future pieces.\n\n{(string.IsNullOrWhiteSpace(preferences) ? "If you have preferred stones, metals, ring sizes or upcoming occasions, I can keep those in mind." : preferences)}\n\n{profile.RepeatFollowUpSuggestion}\n\nKind regards"));

        templates.Add(new CommunicationTemplate(
            $"{segment.PrimarySegment} follow-up",
            $"Hi {name},\n\nI was reviewing your recent OPALNOVA notes and wanted to check the best next step.\n\n{segment.FollowUpAngle}\n\n{segment.ReminderOpportunity}\n\nKind regards"));

        if (nextTask != null)
        {
            templates.Add(new CommunicationTemplate(
                "Open follow-up context",
                $"Hi {name},\n\nI am following up on: {nextTask.Title}.\n\n{nextTask.FollowUpNotes ?? nextTask.Description ?? "Please let me know what you would like to do next."}\n\nKind regards"));
        }

        return templates;
    }

    private static void AppendRelationshipGuidance(StringBuilder html, CustomerValueProfile profile, CustomerSegmentProfile segment)
    {
        html.AppendLine("<h2>Relationship Guidance</h2>");
        html.AppendLine(Row("Lifetime Value", Money(profile.LifetimeValue)));
        html.AppendLine(Row("Payments Recorded", Money(profile.PaymentsRecorded)));
        html.AppendLine(Row("Value Guidance", profile.ValueTier));
        html.AppendLine(Row("Customer Segment", $"{segment.PrimarySegment} ({segment.Confidence})"));
        html.AppendLine(Row("Segment Rationale", segment.Rationale));
        html.AppendLine(Row("Segment Follow-up Angle", segment.FollowUpAngle));
        html.AppendLine(Row("Reminder Opportunity", segment.ReminderOpportunity));
        html.AppendLine(Row("Suggested Next Step", profile.SuggestedNextStep));
        html.AppendLine(Row("Repeat Follow-up Suggestion", profile.RepeatFollowUpSuggestion));
    }

    private static void AppendCommunicationTemplates(StringBuilder html, List<CommunicationTemplate> templates, string title)
    {
        html.AppendLine($"<h2>{Html(title)}</h2>");
        foreach (var template in templates)
        {
            html.AppendLine("<div class='template'>");
            html.AppendLine($"<h3>{Html(template.Title)}</h3>");
            html.AppendLine($"<pre>{Html(template.Body)}</pre>");
            html.AppendLine("</div>");
        }
    }

    private static string BuildFollowUpNotes(Customer customer, CustomerValueProfile profile, CustomerSegmentProfile segment, List<CommunicationTemplate> templates)
    {
        var sb = new StringBuilder();
        var preferences = BuildPreferenceSummary(customer);
        if (!string.IsNullOrWhiteSpace(preferences))
        {
            sb.AppendLine(preferences);
            sb.AppendLine();
        }

        sb.AppendLine($"Value guidance: {profile.ValueTier}");
        sb.AppendLine($"Customer segment: {segment.PrimarySegment} ({segment.Confidence})");
        sb.AppendLine($"Segment rationale: {segment.Rationale}");
        sb.AppendLine($"Follow-up angle: {segment.FollowUpAngle}");
        sb.AppendLine($"Reminder opportunity: {segment.ReminderOpportunity}");
        sb.AppendLine($"Suggested next step: {profile.SuggestedNextStep}");
        sb.AppendLine($"Repeat follow-up suggestion: {profile.RepeatFollowUpSuggestion}");

        var firstTemplate = templates.FirstOrDefault();
        if (firstTemplate != null)
        {
            sb.AppendLine();
            sb.AppendLine($"Message starter - {firstTemplate.Title}:");
            sb.AppendLine(firstTemplate.Body);
        }

        return sb.ToString().Trim();
    }

    private static DateTime? MostRecentActivityDate(List<Job> jobs, List<Sale> sales, List<Payment> payments, List<BusinessTask> tasks)
    {
        var dates = jobs.Select(j => j.DateReceived)
            .Concat(sales.Select(s => s.SaleDate))
            .Concat(payments.Select(p => p.PaymentDate))
            .Concat(tasks.Select(t => t.CompletedAt ?? t.DueDate ?? t.ReminderDate ?? t.CreatedAt))
            .ToList();
        return dates.Count == 0 ? null : dates.Max();
    }

    private static string BuildSegmentText(Customer customer, List<Job> jobs, List<CustomQuote> quotes, List<Sale> sales, List<BusinessTask> tasks)
    {
        var parts = new List<string?>
        {
            customer.FullName,
            customer.RingSizes,
            customer.PreferredMetals,
            customer.PreferredStones,
            customer.Notes
        };
        parts.AddRange(jobs.Select(j => $"{j.JobCode} {j.JobTitle} {j.Type} {j.Status} {j.DesignNotes} {j.CustomerApprovalNotes} {j.InternalNotes}"));
        parts.AddRange(quotes.Select(q => $"{q.QuoteCode} {q.Title} {q.Status} {q.Occasion} {q.BudgetRange} {q.PreferredMetal} {q.PreferredStone} {q.CustomerNotes} {q.InternalNotes}"));
        parts.AddRange(sales.Select(s => $"{s.SaleLocation} {s.Notes}"));
        parts.AddRange(tasks.Select(t => $"{t.Title} {t.Category} {t.Description} {t.FollowUpNotes}"));
        return string.Join(" ", parts.Where(p => !string.IsNullOrWhiteSpace(p)));
    }

    private static bool ContainsAny(string? text, params string[] terms)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;
        return terms.Any(term => text.Contains(term, StringComparison.OrdinalIgnoreCase));
    }

    private static bool HasSpecificReminderOpportunity(string reminderOpportunity) =>
        !reminderOpportunity.StartsWith("No explicit", StringComparison.OrdinalIgnoreCase);

    private static string FirstName(Customer customer)
    {
        if (string.IsNullOrWhiteSpace(customer.FullName))
            return "there";
        return customer.FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? customer.FullName;
    }

    private static List<CustomerTimelineEvent> BuildCustomerTimelineEvents(
        List<CustomQuote> quotes,
        List<QuoteOption> quoteOptions,
        List<Job> jobs,
        List<Sale> sales,
        List<Payment> payments,
        List<BusinessTask> tasks)
    {
        var events = new List<CustomerTimelineEvent>();

        foreach (var quote in quotes)
        {
            var options = quoteOptions.Where(o => o.CustomQuoteId == quote.Id).ToList();
            var acceptedOption = quote.AcceptedOptionId.HasValue
                ? options.FirstOrDefault(o => o.Id == quote.AcceptedOptionId.Value)
                : null;
            var amount = acceptedOption?.TotalPrice ?? options.OrderByDescending(o => o.TotalPrice).FirstOrDefault()?.TotalPrice;
            events.Add(new CustomerTimelineEvent(
                quote.QuoteDate,
                "Quote",
                quote.ToString(),
                $"Proposal: {quote.ProposalStatus}. Valid until: {quote.ValidUntil?.ToShortDateString() ?? "not set"}. Options: {options.Count}.",
                quote.Status,
                amount));

            if (quote.ProposalSentAt.HasValue)
            {
                events.Add(new CustomerTimelineEvent(
                    quote.ProposalSentAt.Value,
                    "Proposal",
                    $"Proposal sent: {quote.ToString()}",
                    quote.ProposalEmailSubject ?? "Proposal recorded as sent.",
                    quote.ProposalStatus,
                    amount));
            }
        }

        foreach (var job in jobs)
        {
            var price = job.FinalPrice > 0 ? job.FinalPrice : job.QuoteAmount;
            events.Add(new CustomerTimelineEvent(
                job.DateReceived,
                "Job",
                job.ToString(),
                $"Type: {job.Type}. Due: {job.DueDate?.ToShortDateString() ?? "not set"}. Balance: {Money(job.BalanceOwing)}.",
                job.Status.ToString(),
                price));
        }

        foreach (var sale in sales)
        {
            events.Add(new CustomerTimelineEvent(
                sale.SaleDate,
                "Sale",
                sale.ToString(),
                $"Location: {sale.SaleLocation}. Method: {sale.PaymentMethod}. {sale.Notes ?? string.Empty}".Trim(),
                "Completed",
                sale.SaleAmount));
        }

        foreach (var payment in payments)
        {
            events.Add(new CustomerTimelineEvent(
                payment.PaymentDate,
                "Payment",
                $"Payment #{payment.Id}",
                $"Method: {payment.Method}. Reference: {payment.Reference ?? string.Empty}. {payment.Notes ?? string.Empty}".Trim(),
                "Recorded",
                payment.Amount));
        }

        foreach (var task in tasks)
        {
            var taskDate = task.CompletedAt ?? task.DueDate ?? task.ReminderDate ?? task.CreatedAt;
            events.Add(new CustomerTimelineEvent(
                taskDate,
                "Task",
                task.Title,
                task.FollowUpNotes ?? task.Description ?? string.Empty,
                task.Status.ToString(),
                null));
        }

        return events
            .OrderByDescending(e => e.Date)
            .ThenBy(e => e.Type)
            .ToList();
    }

    private static void AppendQuotesTable(StringBuilder html, List<CustomQuote> quotes, List<QuoteOption> quoteOptions)
    {
        html.AppendLine("<h2>Recent Quotes</h2>");
        html.AppendLine("<table><tr><th>Quote</th><th>Status</th><th>Proposal</th><th>Date</th><th>Valid Until</th><th>Options</th><th>Accepted / High Option</th></tr>");
        foreach (var quote in quotes)
        {
            var options = quoteOptions.Where(o => o.CustomQuoteId == quote.Id).ToList();
            var acceptedOption = quote.AcceptedOptionId.HasValue
                ? options.FirstOrDefault(o => o.Id == quote.AcceptedOptionId.Value)
                : null;
            var displayOption = acceptedOption ?? options.OrderByDescending(o => o.TotalPrice).FirstOrDefault();
            html.AppendLine($"<tr><td>{Html(quote.ToString())}</td><td>{Html(quote.Status)}</td><td>{Html(quote.ProposalStatus)}</td><td>{Html(quote.QuoteDate.ToShortDateString())}</td><td>{Html(quote.ValidUntil?.ToShortDateString() ?? string.Empty)}</td><td>{options.Count}</td><td>{Html(displayOption == null ? string.Empty : $"{displayOption.OptionName} {Money(displayOption.TotalPrice)}")}</td></tr>");
        }
        html.AppendLine("</table>");
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

    private static void AppendTimelineTable(StringBuilder html, List<CustomerTimelineEvent> events, string title)
    {
        html.AppendLine($"<h2>{Html(title)}</h2>");
        html.AppendLine("<table><tr><th>Date</th><th>Type</th><th>Title</th><th>Status</th><th>Amount</th><th>Detail</th></tr>");
        foreach (var item in events)
        {
            html.AppendLine($"<tr><td>{Html(item.Date.ToShortDateString())}</td><td>{Html(item.Type)}</td><td>{Html(item.Title)}</td><td>{Html(item.Status)}</td><td>{Html(item.Amount.HasValue ? Money(item.Amount.Value) : string.Empty)}</td><td>{Html(item.Detail)}</td></tr>");
        }
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
        html.AppendLine(".template { border: 1px solid #ddd; background: #fafafa; padding: 12px; margin: 10px 0; }");
        html.AppendLine(".template h3 { margin: 0 0 8px 0; }");
        html.AppendLine(".template pre { white-space: pre-wrap; margin: 0; font-family: Arial, sans-serif; line-height: 1.45; }");
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
