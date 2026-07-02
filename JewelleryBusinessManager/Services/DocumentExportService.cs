using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using JewelleryBusinessManager.Data;
using JewelleryBusinessManager.Models;

namespace JewelleryBusinessManager.Services;

public static class DocumentExportService
{
    private static string PrintoutFolder => BusinessSettingsService.GetPrintoutFolder();

    public static string CreateJobCard(Job job)
    {
        Directory.CreateDirectory(PrintoutFolder);
        using var db = new AppDbContext();
        var customer = job.CustomerId.HasValue ? db.Customers.Find(job.CustomerId.Value) : null;
        var fileName = SafeFileName($"JobCard_{job.JobCode}_{job.Id}.html");
        var path = Path.Combine(PrintoutFolder, fileName);

        var html = new StringBuilder();
        html.Append(HtmlHeader("Job Card"));
        html.AppendLine("<section class='card'>");
        html.AppendLine("<h1>Job Card</h1>");
        html.AppendLine(Row("Job", $"{job.JobCode} {job.JobTitle}".Trim()));
        html.AppendLine(Row("Customer", customer?.FullName ?? "Not linked"));
        html.AppendLine(Row("Phone", customer?.Phone ?? string.Empty));
        html.AppendLine(Row("Email", customer?.Email ?? string.Empty));
        html.AppendLine(Row("Type", job.Type.ToString()));
        html.AppendLine(Row("Status", job.Status.ToString()));
        html.AppendLine(Row("Received", job.DateReceived.ToShortDateString()));
        html.AppendLine(Row("Due", job.DueDate?.ToShortDateString() ?? string.Empty));
        html.AppendLine(Row("Quote", Money(job.QuoteAmount)));
        html.AppendLine(Row("Deposit", Money(job.DepositPaid)));
        html.AppendLine(Row("Balance", Money(job.BalanceOwing)));
        html.AppendLine(Row("Labour Cost", Money(job.LabourCost)));
        html.AppendLine(Row("Material Cost", Money(job.MaterialCost)));
        html.AppendLine(Row("Estimated Profit", Money(PricingService.CalculateJobProfit(job))));
        html.AppendLine(Row("Estimated Margin", Percent(PricingService.CalculateProfitMargin(job.FinalPrice > 0 ? job.FinalPrice : job.QuoteAmount, PricingService.CalculateJobCost(job)))));
        html.AppendLine(Row("Final Price", Money(job.FinalPrice)));
        html.AppendLine(NotesBlock("Design Notes", job.DesignNotes));
        html.AppendLine(NotesBlock("Customer Approval", job.CustomerApprovalNotes));
        html.AppendLine(NotesBlock("Internal Bench Notes", job.InternalNotes));
        html.AppendLine("<div class='checkboxes'><p>☐ Materials prepared</p><p>☐ Stone checked</p><p>☐ Making complete</p><p>☐ Quality check</p><p>☐ Customer notified</p><p>☐ Paid / collected / shipped</p></div>");
        html.AppendLine("</section>");
        html.Append(HtmlFooter());
        File.WriteAllText(path, html.ToString());
        return path;
    }

    public static string CreateProductionStageChecklist(Job job)
    {
        Directory.CreateDirectory(PrintoutFolder);
        using var db = new AppDbContext();
        var customer = job.CustomerId.HasValue ? db.Customers.Find(job.CustomerId.Value) : null;
        var quote = db.CustomQuotes.AsEnumerable()
            .Where(q => q.LinkedJobId == job.Id)
            .OrderByDescending(q => q.UpdatedAt)
            .FirstOrDefault();
        var acceptedOption = quote?.AcceptedOptionId.HasValue == true
            ? db.QuoteOptions.Find(quote.AcceptedOptionId.Value)
            : quote == null
                ? null
                : db.QuoteOptions.AsEnumerable()
                    .Where(o => o.CustomQuoteId == quote.Id)
                    .OrderByDescending(o => o.IsRecommended)
                    .ThenByDescending(o => o.UpdatedAt)
                    .FirstOrDefault();
        var materialLinks = acceptedOption == null
            ? new List<QuoteOptionMaterialLink>()
            : db.QuoteOptionMaterialLinks.AsEnumerable().Where(l => l.QuoteOptionId == acceptedOption.Id).ToList();
        var stoneLinks = acceptedOption == null
            ? new List<QuoteOptionStoneLink>()
            : db.QuoteOptionStoneLinks.AsEnumerable().Where(l => l.QuoteOptionId == acceptedOption.Id).ToList();
        var diamondLinks = acceptedOption == null
            ? new List<QuoteOptionExternalDiamondLink>()
            : db.QuoteOptionExternalDiamondLinks.AsEnumerable().Where(l => l.QuoteOptionId == acceptedOption.Id).ToList();
        var payments = db.Payments.AsEnumerable()
            .Where(p => p.JobId == job.Id || (job.CustomerId.HasValue && p.CustomerId == job.CustomerId))
            .OrderBy(p => p.PaymentDate)
            .ToList();
        var tasks = db.BusinessTasks.AsEnumerable()
            .Where(t => t.Status != BusinessTaskStatus.Completed
                && t.Status != BusinessTaskStatus.Cancelled
                && (t.JobId == job.Id || (job.CustomerId.HasValue && t.CustomerId == job.CustomerId)))
            .OrderBy(t => t.DueDate ?? DateTime.MaxValue)
            .ThenByDescending(t => t.Priority)
            .ToList();
        var photos = db.PhotoRecords.AsEnumerable()
            .Where(p => p.EntityType == nameof(Job) && p.EntityId == job.Id)
            .OrderByDescending(p => p.UpdatedAt)
            .ToList();

        var price = JobPrice(job);
        var paid = Math.Max(job.DepositPaid, payments.Sum(p => p.Amount));
        var balance = Math.Max(Math.Max(0, price - paid), Math.Max(0, job.BalanceOwing));
        var checklist = BuildProductionStageChecklist(job, customer, quote, acceptedOption, materialLinks, stoneLinks, diamondLinks, payments, tasks, photos, balance);
        var blockers = checklist.Count(i => i.Status is "Waiting" or "Review");
        var stage = ProductionStageTitle(job.Status);
        var fileName = SafeFileName($"ProductionStageChecklist_{job.JobCode}_{job.Id}_{DateTime.Now:yyyyMMdd-HHmmss}.html");
        var path = Path.Combine(PrintoutFolder, fileName);

        var html = new StringBuilder();
        html.Append(HtmlHeader("Production Stage Checklist"));
        html.AppendLine("<section class='card premium-document'>");
        AppendDocumentHero(html, "Production Stage Checklist", $"{job.JobCode} {job.JobTitle}".Trim(), stage);
        AppendFinancialSummary(html,
            ("Current stage", stage, ProductionStageGuidance(job.Status)),
            ("Due date", job.DueDate?.ToShortDateString() ?? "Not set", ProductionDueGuidance(job)),
            ("Items to review", blockers.ToString(CultureInfo.InvariantCulture), blockers == 0 ? "No blockers detected" : "Review before moving stage"));

        html.AppendLine("<div class='document-columns'>");
        html.AppendLine("<div>");
        html.AppendLine("<h2>Job</h2>");
        html.AppendLine(Row("Job", $"{job.JobCode} {job.JobTitle}".Trim()));
        html.AppendLine(Row("Type", job.Type.ToString()));
        html.AppendLine(Row("Status", job.Status.ToString()));
        html.AppendLine(Row("Received", job.DateReceived.ToShortDateString()));
        html.AppendLine(Row("Due", job.DueDate?.ToShortDateString() ?? "To be confirmed"));
        html.AppendLine(Row("Quote / final price", Money(price)));
        html.AppendLine(Row("Balance", Money(balance)));
        html.AppendLine("</div><div>");
        html.AppendLine("<h2>Customer / Quote</h2>");
        html.AppendLine(Row("Customer", customer?.FullName ?? "Not linked"));
        html.AppendLine(Row("Phone", customer?.Phone ?? string.Empty));
        html.AppendLine(Row("Email", customer?.Email ?? string.Empty));
        html.AppendLine(Row("Quote", quote?.ToString() ?? "No linked quote"));
        html.AppendLine(Row("Proposal status", quote?.ProposalStatus ?? "Not recorded"));
        html.AppendLine(Row("Accepted option", acceptedOption?.OptionName ?? "Not recorded"));
        html.AppendLine("</div></div>");

        AppendDocumentNotice(html, "Recommended next action", BuildProductionRecommendedAction(job, checklist, balance));
        AppendProductionChecklistTable(html, checklist);
        AppendProductionReservationTable(html, materialLinks, stoneLinks, diamondLinks);
        AppendProductionTaskTable(html, tasks);
        AppendProductionPhotoTable(html, photos);
        html.AppendLine(NotesBlock("Design Notes", job.DesignNotes));
        html.AppendLine(NotesBlock("Customer Approval Notes", job.CustomerApprovalNotes));
        html.AppendLine(NotesBlock("Internal Bench Notes", job.InternalNotes));
        html.AppendLine(SignatureBlock("Bench check"));
        html.AppendLine(SignatureBlock("Quality / handover check"));
        html.AppendLine("</section>");
        html.Append(HtmlFooter());
        File.WriteAllText(path, html.ToString());
        return path;
    }

    public static string CreateStockLabel(JewelleryItem item)
    {
        Directory.CreateDirectory(PrintoutFolder);
        using var db = new AppDbContext();
        var stone = item.MainStoneId.HasValue ? db.Stones.Find(item.MainStoneId.Value) : null;
        var fileName = SafeFileName($"StockLabel_{item.StockCode}_{item.Id}.html");
        var path = Path.Combine(PrintoutFolder, fileName);

        var html = new StringBuilder();
        html.Append(HtmlHeader("Stock Label"));
        html.AppendLine("<section class='label'>");
        html.AppendLine($"<h1>{Html(item.StockCode)}</h1>");
        html.AppendLine($"<h2>{Html(item.Name)}</h2>");
        html.AppendLine(Row("Type", item.Type.ToString()));
        html.AppendLine(Row("Metal", item.Metal ?? string.Empty));
        html.AppendLine(Row("Stone", stone == null ? string.Empty : stone.ToString()));
        html.AppendLine(Row("Size", item.RingSize ?? item.ChainLength ?? item.Dimensions ?? string.Empty));
        html.AppendLine($"<div class='price'>{Money(item.RetailPrice)}</div>");
        html.AppendLine($"<p class='small'>Cost: {Money(PricingService.CalculateJewelleryCost(item))} | Profit: {Money(PricingService.CalculateRetailProfit(item))} | Margin: {Percent(PricingService.CalculateProfitMargin(item.RetailPrice, PricingService.CalculateJewelleryCost(item)))}</p>");
        html.AppendLine($"<p class='small'>Status: {Html(item.Status.ToString())}</p>");
        html.AppendLine("</section>");
        html.Append(HtmlFooter());
        File.WriteAllText(path, html.ToString());
        return path;
    }


    public static string CreateCustomerQuote(Job job)
    {
        Directory.CreateDirectory(PrintoutFolder);
        using var db = new AppDbContext();
        var customer = job.CustomerId.HasValue ? db.Customers.Find(job.CustomerId.Value) : null;
        var price = job.FinalPrice > 0 ? job.FinalPrice : job.QuoteAmount;
        var fileName = SafeFileName($"Quote_{job.JobCode}_{job.Id}.html");
        var path = Path.Combine(PrintoutFolder, fileName);

        var html = new StringBuilder();
        html.Append(HtmlHeader("Customer Quote"));
        html.AppendLine("<section class='card'>");
        html.AppendLine("<h1>Customer Quote</h1>");
        html.AppendLine($"<p class='small'>Generated {Html(DateTime.Now.ToString("f", CultureInfo.CurrentCulture))}</p>");
        html.AppendLine(Row("Quote / Job", $"{job.JobCode} {job.JobTitle}".Trim()));
        html.AppendLine(Row("Customer", customer?.FullName ?? "Not linked"));
        html.AppendLine(Row("Phone", customer?.Phone ?? string.Empty));
        html.AppendLine(Row("Email", customer?.Email ?? string.Empty));
        html.AppendLine(Row("Job Type", job.Type.ToString()));
        html.AppendLine(Row("Status", job.Status.ToString()));
        html.AppendLine(Row("Date Received", job.DateReceived.ToShortDateString()));
        html.AppendLine(Row("Due Date", job.DueDate?.ToShortDateString() ?? "To be confirmed"));
        html.AppendLine(NotesBlock("Quoted Work", job.DesignNotes));
        html.AppendLine(Row("Quoted Amount", Money(price)));
        html.AppendLine(Row("Deposit Paid", Money(job.DepositPaid)));
        html.AppendLine(Row("Estimated Balance", Money(Math.Max(0, price - job.DepositPaid))));
        html.AppendLine("<h2>Quote Notes</h2>");
        html.AppendLine("<p>This quote is based on the information currently recorded for the job. Any design changes, additional materials, resizing, repair complications or customer-requested changes may require a revised quote.</p>");
        html.AppendLine(SignatureBlock("Customer approval"));
        html.AppendLine(SignatureBlock("Business approval"));
        html.AppendLine("</section>");
        html.Append(HtmlFooter());
        File.WriteAllText(path, html.ToString());
        return path;
    }

    public static string CreateInvoiceFromJob(Job job)
    {
        Directory.CreateDirectory(PrintoutFolder);
        using var db = new AppDbContext();
        var customer = job.CustomerId.HasValue ? db.Customers.Find(job.CustomerId.Value) : null;
        var payments = db.Payments.AsEnumerable().Where(p => p.JobId == job.Id || (job.CustomerId.HasValue && p.CustomerId == job.CustomerId)).OrderBy(p => p.PaymentDate).ToList();
        var amount = job.FinalPrice > 0 ? job.FinalPrice : job.QuoteAmount;
        var paid = Math.Max(job.DepositPaid, payments.Sum(p => p.Amount));
        var balance = Math.Max(0, amount - paid);
        var fileName = SafeFileName($"Invoice_{job.JobCode}_{job.Id}.html");
        var path = Path.Combine(PrintoutFolder, fileName);

        var html = new StringBuilder();
        var documentTitle = balance <= 0 ? "Customer Receipt" : "Customer Invoice";
        html.Append(HtmlHeader(documentTitle));
        html.AppendLine("<section class='card premium-document'>");
        AppendDocumentHero(html, documentTitle, $"{job.JobCode} {job.JobTitle}".Trim(), balance <= 0 ? "Paid in full" : $"Balance due {Money(balance)}");
        AppendFinancialSummary(html,
            ("Total amount", Money(amount), "Job total"),
            ("Payments recorded", Money(paid), "Linked payments"),
            ("Balance due", Money(balance), balance <= 0 ? "No balance owing" : "Payment required"));
        html.AppendLine("<div class='document-columns'>");
        html.AppendLine("<div>");
        html.AppendLine("<h2>Customer</h2>");
        html.AppendLine(Row("Name", customer?.FullName ?? "Not linked"));
        html.AppendLine(Row("Phone", customer?.Phone ?? string.Empty));
        html.AppendLine(Row("Email", customer?.Email ?? string.Empty));
        html.AppendLine("</div><div>");
        html.AppendLine("<h2>Job / Handover</h2>");
        html.AppendLine(Row("Invoice / Job", $"{job.JobCode} {job.JobTitle}".Trim()));
        html.AppendLine(Row("Description", job.JobTitle));
        html.AppendLine(Row("Status", job.Status.ToString()));
        html.AppendLine(Row("Due date", job.DueDate?.ToShortDateString() ?? "To be confirmed"));
        html.AppendLine("</div></div>");
        html.AppendLine(NotesBlock("Work Notes", job.DesignNotes));
        AppendDocumentNotice(html, "Handover status", BuildJobHandoverStatus(job, balance));
        AppendPaymentsTable(html, payments);
        AppendDocumentNotice(html, "Payment check", balance <= 0
            ? "This document can be used as a paid receipt. Confirm payment method and reference details before handover."
            : "This invoice still has a balance owing. Confirm payment timing before collection, shipping or final handover.");
        html.AppendLine("</section>");
        html.Append(HtmlFooter());
        File.WriteAllText(path, html.ToString());
        return path;
    }

    public static string CreateHandoverConfirmationFromJob(Job job, string? handoverNotes = null)
    {
        Directory.CreateDirectory(PrintoutFolder);
        using var db = new AppDbContext();
        var customer = job.CustomerId.HasValue ? db.Customers.Find(job.CustomerId.Value) : null;
        var payments = db.Payments.AsEnumerable().Where(p => p.JobId == job.Id).OrderBy(p => p.PaymentDate).ToList();
        var amount = job.FinalPrice > 0 ? job.FinalPrice : job.QuoteAmount;
        var paid = Math.Max(job.DepositPaid, payments.Sum(p => p.Amount));
        var balance = Math.Max(Math.Max(0, amount - paid), Math.Max(0, job.BalanceOwing));
        var handoverMode = BuildHandoverMode(job);
        var fileName = SafeFileName($"HandoverConfirmation_{job.JobCode}_{job.Id}.html");
        var path = Path.Combine(PrintoutFolder, fileName);

        var html = new StringBuilder();
        html.Append(HtmlHeader("Handover Confirmation"));
        html.AppendLine("<section class='card premium-document'>");
        AppendDocumentHero(html, "Handover Confirmation", $"{job.JobCode} {job.JobTitle}".Trim(), $"{handoverMode} record");
        AppendFinancialSummary(html,
            ("Total amount", Money(amount), "Job total"),
            ("Payments recorded", Money(paid), "Linked payments"),
            ("Balance due", Money(balance), balance <= 0 ? "No balance owing" : "Check before release"));

        html.AppendLine("<div class='document-columns'>");
        html.AppendLine("<div>");
        html.AppendLine("<h2>Customer</h2>");
        html.AppendLine(Row("Name", customer?.FullName ?? "Not linked"));
        html.AppendLine(Row("Phone", customer?.Phone ?? string.Empty));
        html.AppendLine(Row("Email", customer?.Email ?? string.Empty));
        html.AppendLine("</div><div>");
        html.AppendLine("<h2>Job</h2>");
        html.AppendLine(Row("Job", $"{job.JobCode} {job.JobTitle}".Trim()));
        html.AppendLine(Row("Type", job.Type.ToString()));
        html.AppendLine(Row("Status", job.Status.ToString()));
        html.AppendLine(Row("Due / handover date", job.DueDate?.ToShortDateString() ?? "To be confirmed"));
        html.AppendLine("</div></div>");

        html.AppendLine("<h2>Handover Checklist</h2>");
        html.AppendLine("<table><tr><th>Check</th><th>Status / Notes</th></tr>");
        html.AppendLine($"<tr><td>Handover type</td><td>{Html(handoverMode)}</td></tr>");
        html.AppendLine($"<tr><td>Payment checked</td><td>{Html(balance <= 0 ? "No balance currently owing" : $"Balance still owing: {Money(balance)}")}</td></tr>");
        html.AppendLine("<tr><td>Item condition checked</td><td></td></tr>");
        html.AppendLine(BuildModeSpecificChecklistRow(job));
        html.AppendLine("<tr><td>Customer notified / tracking shared</td><td></td></tr>");
        html.AppendLine("</table>");

        html.AppendLine(NotesBlock("Work Notes", job.DesignNotes));
        html.AppendLine(NotesBlock("Handover Notes", handoverNotes));
        AppendDocumentNotice(html, "Handover status", BuildJobHandoverStatus(job, balance));
        AppendPaymentsTable(html, payments);
        html.AppendLine(SignatureBlock("Customer / recipient"));
        html.AppendLine(SignatureBlock("Business handover"));
        html.AppendLine("</section>");
        html.Append(HtmlFooter());
        File.WriteAllText(path, html.ToString());
        return path;
    }

    public static string CreateReceiptFromSale(Sale sale)
    {
        Directory.CreateDirectory(PrintoutFolder);
        using var db = new AppDbContext();
        var customer = sale.CustomerId.HasValue ? db.Customers.Find(sale.CustomerId.Value) : null;
        var item = sale.JewelleryItemId.HasValue ? db.JewelleryItems.Find(sale.JewelleryItemId.Value) : null;
        var job = sale.JobId.HasValue ? db.Jobs.Find(sale.JobId.Value) : null;
        var payments = db.Payments.AsEnumerable().Where(p => p.SaleId == sale.Id).OrderBy(p => p.PaymentDate).ToList();
        var fileName = SafeFileName($"Receipt_Sale_{sale.Id}_{sale.SaleDate:yyyyMMdd}.html");
        var path = Path.Combine(PrintoutFolder, fileName);

        var html = new StringBuilder();
        var paid = payments.Sum(p => p.Amount);
        var displayPaid = paid > 0 ? paid : sale.SaleAmount;
        var balance = Math.Max(0, sale.SaleAmount - displayPaid);

        html.Append(HtmlHeader("Customer Receipt"));
        html.AppendLine("<section class='card premium-document'>");
        AppendDocumentHero(html, "Customer Receipt", $"Sale #{sale.Id}", balance <= 0 ? "Paid in full" : $"Balance due {Money(balance)}");
        AppendFinancialSummary(html,
            ("Sale amount", Money(sale.SaleAmount), "Recorded sale"),
            ("Payments recorded", Money(displayPaid), paid > 0 ? "Linked payments" : "Sale payment method"),
            ("Balance due", Money(balance), balance <= 0 ? "No balance owing" : "Review sale payments"));
        html.AppendLine("<div class='document-columns'>");
        html.AppendLine("<div>");
        html.AppendLine("<h2>Customer</h2>");
        html.AppendLine(Row("Name", customer?.FullName ?? "Not linked"));
        html.AppendLine(Row("Phone", customer?.Phone ?? string.Empty));
        html.AppendLine(Row("Email", customer?.Email ?? string.Empty));
        html.AppendLine("</div><div>");
        html.AppendLine("<h2>Sale</h2>");
        html.AppendLine(Row("Date", sale.SaleDate.ToShortDateString()));
        html.AppendLine(Row("Item / Job", item?.ToString() ?? job?.ToString() ?? "General sale"));
        html.AppendLine(Row("Sale Location", sale.SaleLocation.ToString()));
        html.AppendLine(Row("Payment Method", sale.PaymentMethod.ToString()));
        html.AppendLine("</div></div>");
        html.AppendLine(NotesBlock("Notes", sale.Notes));
        AppendPaymentsTable(html, payments);
        AppendDocumentNotice(html, "Handover note", "Thank you for your purchase. Confirm collection, shipping or customer handover notes before filing this receipt.");
        html.AppendLine("</section>");
        html.Append(HtmlFooter());
        File.WriteAllText(path, html.ToString());
        return path;
    }

    public static string CreateDepositReceiptFromJob(Job job)
    {
        Directory.CreateDirectory(PrintoutFolder);
        using var db = new AppDbContext();
        var customer = job.CustomerId.HasValue ? db.Customers.Find(job.CustomerId.Value) : null;
        var amount = job.FinalPrice > 0 ? job.FinalPrice : job.QuoteAmount;
        var fileName = SafeFileName($"DepositReceipt_{job.JobCode}_{job.Id}.html");
        var path = Path.Combine(PrintoutFolder, fileName);

        var html = new StringBuilder();
        var balance = Math.Max(0, amount - job.DepositPaid);
        html.Append(HtmlHeader("Deposit Receipt"));
        html.AppendLine("<section class='card premium-document'>");
        AppendDocumentHero(html, "Deposit Receipt", $"{job.JobCode} {job.JobTitle}".Trim(), balance <= 0 ? "Paid in full" : $"Balance remaining {Money(balance)}");
        AppendFinancialSummary(html,
            ("Deposit paid", Money(job.DepositPaid), "Recorded on job"),
            ("Total job amount", Money(amount), "Current job total"),
            ("Balance remaining", Money(balance), balance <= 0 ? "No balance owing" : "Due before handover"));
        html.AppendLine("<div class='document-columns'>");
        html.AppendLine("<div>");
        html.AppendLine("<h2>Customer</h2>");
        html.AppendLine(Row("Name", customer?.FullName ?? "Not linked"));
        html.AppendLine(Row("Phone", customer?.Phone ?? string.Empty));
        html.AppendLine(Row("Email", customer?.Email ?? string.Empty));
        html.AppendLine("</div><div>");
        html.AppendLine("<h2>Job</h2>");
        html.AppendLine(Row("Job", $"{job.JobCode} {job.JobTitle}".Trim()));
        html.AppendLine(Row("Date", DateTime.Today.ToShortDateString()));
        html.AppendLine(Row("Status", job.Status.ToString()));
        html.AppendLine("</div></div>");
        AppendDocumentNotice(html, "Deposit note", "This receipt confirms the deposit currently recorded on the job. Final balance should be checked before collection or shipping.");
        html.AppendLine("</section>");
        html.Append(HtmlFooter());
        File.WriteAllText(path, html.ToString());
        return path;
    }

    public static string CreateDepositReceiptFromPayment(Payment payment)
    {
        Directory.CreateDirectory(PrintoutFolder);
        using var db = new AppDbContext();
        var customer = payment.CustomerId.HasValue ? db.Customers.Find(payment.CustomerId.Value) : null;
        var job = payment.JobId.HasValue ? db.Jobs.Find(payment.JobId.Value) : null;
        var sale = payment.SaleId.HasValue ? db.Sales.Find(payment.SaleId.Value) : null;
        if (customer == null && job?.CustomerId != null)
            customer = db.Customers.Find(job.CustomerId.Value);
        if (customer == null && sale?.CustomerId != null)
            customer = db.Customers.Find(sale.CustomerId.Value);
        var fileName = SafeFileName($"PaymentReceipt_{payment.Id}_{payment.PaymentDate:yyyyMMdd}.html");
        var path = Path.Combine(PrintoutFolder, fileName);

        var html = new StringBuilder();
        var relatedTotal = job != null
            ? job.FinalPrice > 0 ? job.FinalPrice : job.QuoteAmount
            : sale?.SaleAmount ?? payment.Amount;
        var relatedBalance = job != null
            ? job.BalanceOwing
            : sale == null ? 0 : Math.Max(0, sale.SaleAmount - payment.Amount);
        html.Append(HtmlHeader("Payment Receipt"));
        html.AppendLine("<section class='card premium-document'>");
        AppendDocumentHero(html, "Payment Receipt", $"Payment #{payment.Id}", relatedBalance <= 0 ? "Payment recorded" : $"Related balance {Money(relatedBalance)}");
        AppendFinancialSummary(html,
            ("Payment amount", Money(payment.Amount), payment.Method.ToString()),
            ("Related total", Money(relatedTotal), job != null ? "Job total" : sale != null ? "Sale total" : "Payment only"),
            ("Related balance", Money(Math.Max(0, relatedBalance)), relatedBalance <= 0 ? "No balance shown" : "Review before handover"));
        html.AppendLine("<div class='document-columns'>");
        html.AppendLine("<div>");
        html.AppendLine("<h2>Customer</h2>");
        html.AppendLine(Row("Name", customer?.FullName ?? "Not linked"));
        html.AppendLine(Row("Phone", customer?.Phone ?? string.Empty));
        html.AppendLine(Row("Email", customer?.Email ?? string.Empty));
        html.AppendLine("</div><div>");
        html.AppendLine("<h2>Payment</h2>");
        html.AppendLine(Row("Related Job", job?.ToString() ?? string.Empty));
        html.AppendLine(Row("Related Sale", sale == null ? string.Empty : sale.ToString()));
        html.AppendLine(Row("Date", payment.PaymentDate.ToShortDateString()));
        html.AppendLine(Row("Method", payment.Method.ToString()));
        html.AppendLine(Row("Reference", payment.Reference ?? string.Empty));
        html.AppendLine("</div></div>");
        html.AppendLine(NotesBlock("Notes", payment.Notes));
        AppendDocumentNotice(html, "Payment check", "Confirm payment method, reference and related job or sale before handing this receipt to a customer.");
        html.AppendLine("</section>");
        html.Append(HtmlFooter());
        File.WriteAllText(path, html.ToString());
        return path;
    }

    public static string CreateRepairIntakeForm(Job job)
    {
        Directory.CreateDirectory(PrintoutFolder);
        using var db = new AppDbContext();
        var customer = job.CustomerId.HasValue ? db.Customers.Find(job.CustomerId.Value) : null;
        var fileName = SafeFileName($"RepairIntake_{job.JobCode}_{job.Id}.html");
        var path = Path.Combine(PrintoutFolder, fileName);

        var html = new StringBuilder();
        html.Append(HtmlHeader("Repair Intake Form"));
        html.AppendLine("<section class='card'>");
        html.AppendLine("<h1>Repair Intake Form</h1>");
        html.AppendLine(Row("Job", $"{job.JobCode} {job.JobTitle}".Trim()));
        html.AppendLine(Row("Customer", customer?.FullName ?? "Not linked"));
        html.AppendLine(Row("Phone", customer?.Phone ?? string.Empty));
        html.AppendLine(Row("Email", customer?.Email ?? string.Empty));
        html.AppendLine(Row("Received", job.DateReceived.ToShortDateString()));
        html.AppendLine(Row("Due", job.DueDate?.ToShortDateString() ?? "To be confirmed"));
        html.AppendLine(Row("Quoted Amount", Money(job.QuoteAmount)));
        html.AppendLine(NotesBlock("Item Condition / Customer Request", job.DesignNotes));
        html.AppendLine("<h2>Repair Intake Checklist</h2>");
        html.AppendLine("<div class='checkboxes'><p>☐ Customer contact confirmed</p><p>☐ Photos taken before work</p><p>☐ Stones checked</p><p>☐ Existing damage recorded</p><p>☐ Quote approved</p><p>☐ Customer understands risks</p></div>");
        html.AppendLine(SignatureBlock("Customer signature"));
        html.AppendLine(SignatureBlock("Received by"));
        html.AppendLine("</section>");
        html.Append(HtmlFooter());
        File.WriteAllText(path, html.ToString());
        return path;
    }

    public static string CreateCustomOrderAgreement(Job job)
    {
        Directory.CreateDirectory(PrintoutFolder);
        using var db = new AppDbContext();
        var customer = job.CustomerId.HasValue ? db.Customers.Find(job.CustomerId.Value) : null;
        var amount = job.FinalPrice > 0 ? job.FinalPrice : job.QuoteAmount;
        var fileName = SafeFileName($"CustomOrderAgreement_{job.JobCode}_{job.Id}.html");
        var path = Path.Combine(PrintoutFolder, fileName);

        var html = new StringBuilder();
        html.Append(HtmlHeader("Custom Order Agreement"));
        html.AppendLine("<section class='card'>");
        html.AppendLine("<h1>Custom Order Agreement</h1>");
        html.AppendLine(Row("Job", $"{job.JobCode} {job.JobTitle}".Trim()));
        html.AppendLine(Row("Customer", customer?.FullName ?? "Not linked"));
        html.AppendLine(Row("Phone", customer?.Phone ?? string.Empty));
        html.AppendLine(Row("Email", customer?.Email ?? string.Empty));
        html.AppendLine(Row("Total / Quote", Money(amount)));
        html.AppendLine(Row("Deposit Paid", Money(job.DepositPaid)));
        html.AppendLine(Row("Balance", Money(Math.Max(0, amount - job.DepositPaid))));
        html.AppendLine(Row("Due Date", job.DueDate?.ToShortDateString() ?? "To be confirmed"));
        html.AppendLine(NotesBlock("Design / Order Details", job.DesignNotes));
        html.AppendLine("<h2>Agreement Notes</h2>");
        html.AppendLine("<p>Custom work is made to the approved design notes above. Changes requested after approval may alter price and completion date. Natural stones and handmade work may include natural variation.</p>");
        html.AppendLine("<p>Deposits, payment timing, collection/shipping arrangements and warranty/repair terms should be confirmed before the customer signs.</p>");
        html.AppendLine(SignatureBlock("Customer signature"));
        html.AppendLine(SignatureBlock("Business signature"));
        html.AppendLine("</section>");
        html.Append(HtmlFooter());
        File.WriteAllText(path, html.ToString());
        return path;
    }

    public static string CreatePaymentSummaryForCustomer(Customer customer)
    {
        Directory.CreateDirectory(PrintoutFolder);
        using var db = new AppDbContext();
        var jobs = db.Jobs.AsEnumerable().Where(j => j.CustomerId == customer.Id).OrderByDescending(j => j.DateReceived).ToList();
        var sales = db.Sales.AsEnumerable().Where(s => s.CustomerId == customer.Id).OrderByDescending(s => s.SaleDate).ToList();
        var payments = db.Payments.AsEnumerable().Where(p => p.CustomerId == customer.Id || jobs.Any(j => p.JobId == j.Id) || sales.Any(s => p.SaleId == s.Id)).OrderByDescending(p => p.PaymentDate).ToList();
        var fileName = SafeFileName($"PaymentSummary_{customer.FullName}_{customer.Id}.html");
        var path = Path.Combine(PrintoutFolder, fileName);

        var html = new StringBuilder();
        html.Append(HtmlHeader("Payment Summary"));
        html.AppendLine("<section class='card'>");
        html.AppendLine("<h1>Payment Summary</h1>");
        html.AppendLine(Row("Customer", customer.FullName));
        html.AppendLine(Row("Phone", customer.Phone ?? string.Empty));
        html.AppendLine(Row("Email", customer.Email ?? string.Empty));
        html.AppendLine(Row("Total Sales", Money(sales.Sum(s => s.SaleAmount))));
        html.AppendLine(Row("Payments Recorded", Money(payments.Sum(p => p.Amount))));
        html.AppendLine(Row("Open Job Balance", Money(jobs.Where(j => j.Status != JobStatus.Completed && j.Status != JobStatus.Cancelled).Sum(j => j.BalanceOwing))));
        AppendPaymentsTable(html, payments);
        html.AppendLine("</section>");
        html.Append(HtmlFooter());
        File.WriteAllText(path, html.ToString());
        return path;
    }

    public static string CreatePaymentSummaryForJob(Job job)
    {
        Directory.CreateDirectory(PrintoutFolder);
        using var db = new AppDbContext();
        var customer = job.CustomerId.HasValue ? db.Customers.Find(job.CustomerId.Value) : null;
        var payments = db.Payments.AsEnumerable().Where(p => p.JobId == job.Id).OrderByDescending(p => p.PaymentDate).ToList();
        var linkedQuote = db.CustomQuotes.AsEnumerable().Where(q => q.LinkedJobId == job.Id).OrderByDescending(q => q.UpdatedAt).FirstOrDefault();
        var schedule = PaymentScheduleService.BuildForJob(job, payments, linkedQuote);
        var amount = job.FinalPrice > 0 ? job.FinalPrice : job.QuoteAmount;
        var fileName = SafeFileName($"PaymentSummary_Job_{job.JobCode}_{job.Id}.html");
        var path = Path.Combine(PrintoutFolder, fileName);

        var html = new StringBuilder();
        html.Append(HtmlHeader("Job Payment Summary"));
        html.AppendLine("<section class='card'>");
        html.AppendLine("<h1>Job Payment Summary</h1>");
        html.AppendLine(Row("Job", $"{job.JobCode} {job.JobTitle}".Trim()));
        html.AppendLine(Row("Customer", customer?.FullName ?? "Not linked"));
        html.AppendLine(Row("Job Amount", Money(amount)));
        html.AppendLine(Row("Payments Recorded", Money(payments.Sum(p => p.Amount))));
        html.AppendLine(Row("Balance Remaining", Money(Math.Max(0, amount - Math.Max(job.DepositPaid, payments.Sum(p => p.Amount))))));
        AppendPaymentScheduleTable(html, schedule);
        AppendPaymentsTable(html, payments);
        html.AppendLine("</section>");
        html.Append(HtmlFooter());
        File.WriteAllText(path, html.ToString());
        return path;
    }

    public static string CreatePaymentSummaryForSale(Sale sale)
    {
        Directory.CreateDirectory(PrintoutFolder);
        using var db = new AppDbContext();
        var customer = sale.CustomerId.HasValue ? db.Customers.Find(sale.CustomerId.Value) : null;
        var payments = db.Payments.AsEnumerable().Where(p => p.SaleId == sale.Id).OrderByDescending(p => p.PaymentDate).ToList();
        var fileName = SafeFileName($"PaymentSummary_Sale_{sale.Id}.html");
        var path = Path.Combine(PrintoutFolder, fileName);

        var html = new StringBuilder();
        html.Append(HtmlHeader("Sale Payment Summary"));
        html.AppendLine("<section class='card'>");
        html.AppendLine("<h1>Sale Payment Summary</h1>");
        html.AppendLine(Row("Sale", sale.ToString()));
        html.AppendLine(Row("Customer", customer?.FullName ?? "Not linked"));
        html.AppendLine(Row("Sale Amount", Money(sale.SaleAmount)));
        html.AppendLine(Row("Payments Recorded", Money(payments.Sum(p => p.Amount))));
        html.AppendLine(Row("Balance Remaining", Money(Math.Max(0, sale.SaleAmount - payments.Sum(p => p.Amount)))));
        AppendPaymentsTable(html, payments);
        html.AppendLine("</section>");
        html.Append(HtmlFooter());
        File.WriteAllText(path, html.ToString());
        return path;
    }

    public static string CreateCustomerHistoryReport(Customer customer)
    {
        Directory.CreateDirectory(PrintoutFolder);
        using var db = new AppDbContext();
        var jobs = db.Jobs.AsEnumerable().Where(j => j.CustomerId == customer.Id).OrderByDescending(j => j.DateReceived).ToList();
        var sales = db.Sales.AsEnumerable().Where(s => s.CustomerId == customer.Id).OrderByDescending(s => s.SaleDate).ToList();
        var payments = db.Payments.AsEnumerable().Where(p => p.CustomerId == customer.Id || jobs.Any(j => p.JobId == j.Id) || sales.Any(s => p.SaleId == s.Id)).OrderByDescending(p => p.PaymentDate).ToList();
        var fileName = SafeFileName($"CustomerHistory_{customer.FullName}_{customer.Id}.html");
        var path = Path.Combine(PrintoutFolder, fileName);

        var html = new StringBuilder();
        html.Append(HtmlHeader("Customer History Report"));
        html.AppendLine("<section class='card'>");
        html.AppendLine("<h1>Customer History Report</h1>");
        html.AppendLine(Row("Customer", customer.FullName));
        html.AppendLine(Row("Phone", customer.Phone ?? string.Empty));
        html.AppendLine(Row("Email", customer.Email ?? string.Empty));
        html.AppendLine(Row("Instagram", customer.InstagramHandle ?? string.Empty));
        html.AppendLine(Row("Ring Sizes", customer.RingSizes ?? string.Empty));
        html.AppendLine(Row("Preferred Metals", customer.PreferredMetals ?? string.Empty));
        html.AppendLine(Row("Preferred Stones", customer.PreferredStones ?? string.Empty));
        html.AppendLine(NotesBlock("Customer Notes", customer.Notes));
        html.AppendLine("<h2>Jobs</h2>");
        html.AppendLine("<table><tr><th>Job</th><th>Status</th><th>Received</th><th>Due</th><th>Amount</th><th>Balance</th></tr>");
        foreach (var job in jobs)
        {
            var amount = job.FinalPrice > 0 ? job.FinalPrice : job.QuoteAmount;
            html.AppendLine($"<tr><td>{Html(job.ToString())}</td><td>{Html(job.Status.ToString())}</td><td>{Html(job.DateReceived.ToShortDateString())}</td><td>{Html(job.DueDate?.ToShortDateString() ?? string.Empty)}</td><td>{Money(amount)}</td><td>{Money(job.BalanceOwing)}</td></tr>");
        }
        html.AppendLine("</table>");
        html.AppendLine("<h2>Sales</h2>");
        html.AppendLine("<table><tr><th>Date</th><th>Amount</th><th>Location</th><th>Method</th><th>Notes</th></tr>");
        foreach (var sale in sales)
            html.AppendLine($"<tr><td>{Html(sale.SaleDate.ToShortDateString())}</td><td>{Money(sale.SaleAmount)}</td><td>{Html(sale.SaleLocation.ToString())}</td><td>{Html(sale.PaymentMethod.ToString())}</td><td>{Html(sale.Notes ?? string.Empty)}</td></tr>");
        html.AppendLine("</table>");
        AppendPaymentsTable(html, payments);
        html.AppendLine("</section>");
        html.Append(HtmlFooter());
        File.WriteAllText(path, html.ToString());
        return path;
    }

    public static string CreateBusinessReport()
    {
        Directory.CreateDirectory(PrintoutFolder);
        using var db = new AppDbContext();
        var path = Path.Combine(PrintoutFolder, $"BusinessReport_{DateTime.Now:yyyyMMdd_HHmmss}.html");

        var stock = db.JewelleryItems.AsEnumerable().ToList();
        var unsoldStock = stock.Where(i => i.Status != StockStatus.Sold).ToList();
        var activeJobs = db.Jobs.AsEnumerable().Where(j => j.Status != JobStatus.Completed && j.Status != JobStatus.Cancelled).ToList();
        var lowMaterials = db.Materials.AsEnumerable().Where(m => m.CurrentQuantity <= m.ReorderLevel).OrderBy(m => m.Name).ToList();
        var sales = db.Sales.AsEnumerable().OrderByDescending(s => s.SaleDate).ToList();
        var recentSales = sales.Take(10).ToList();
        var thisMonthSales = sales.Where(s => s.SaleDate.Month == DateTime.Today.Month && s.SaleDate.Year == DateTime.Today.Year).ToList();
        var stockCost = unsoldStock.Sum(PricingService.CalculateJewelleryCost);
        var stockRetail = unsoldStock.Sum(i => i.RetailPrice);

        var html = new StringBuilder();
        html.Append(HtmlHeader("Business Summary Report"));
        html.AppendLine("<section class='card'>");
        html.AppendLine("<h1>Business Summary Report</h1>");
        html.AppendLine($"<p class='small'>Generated {Html(DateTime.Now.ToString("f", CultureInfo.CurrentCulture))}</p>");
        html.AppendLine(Row("Finished stock items", stock.Count.ToString(CultureInfo.InvariantCulture)));
        html.AppendLine(Row("Unsold stock retail value", Money(stockRetail)));
        html.AppendLine(Row("Unsold stock cost value", Money(stockCost)));
        html.AppendLine(Row("Potential stock profit", Money(stockRetail - stockCost)));
        html.AppendLine(Row("Potential stock margin", Percent(PricingService.CalculateProfitMargin(stockRetail, stockCost))));
        html.AppendLine(Row("Active jobs", activeJobs.Count.ToString(CultureInfo.InvariantCulture)));
        html.AppendLine(Row("Outstanding job balance", Money(activeJobs.Sum(j => j.BalanceOwing))));
        html.AppendLine(Row("Low materials", lowMaterials.Count.ToString(CultureInfo.InvariantCulture)));
        html.AppendLine(Row("Sales this month", Money(thisMonthSales.Sum(s => s.SaleAmount))));
        html.AppendLine(Row("Profit this month", Money(thisMonthSales.Sum(s => s.Profit))));
        html.AppendLine(Row("Margin this month", Percent(PricingService.CalculateProfitMargin(thisMonthSales.Sum(s => s.SaleAmount), thisMonthSales.Sum(s => s.CostOfGoods)))));
        AppendLowMaterialsTable(html, lowMaterials);
        AppendActiveJobsTable(html, activeJobs);
        AppendRecentSalesTable(html, recentSales);
        html.AppendLine("</section>");
        html.Append(HtmlFooter());
        File.WriteAllText(path, html.ToString());
        return path;
    }

    public static string CreateCostingReport()
    {
        Directory.CreateDirectory(PrintoutFolder);
        using var db = new AppDbContext();
        var path = Path.Combine(PrintoutFolder, $"CostingReport_{DateTime.Now:yyyyMMdd_HHmmss}.html");
        var items = db.JewelleryItems.AsEnumerable().OrderBy(i => i.Status).ThenBy(i => i.StockCode).ToList();
        var jobs = db.Jobs.AsEnumerable().OrderBy(j => j.Status).ThenBy(j => j.DueDate ?? DateTime.MaxValue).ToList();
        var targetMargin = BusinessSettingsService.Load().DefaultProfitMarginPercent;

        var html = new StringBuilder();
        html.Append(HtmlHeader("Costing and Profit Report"));
        html.AppendLine("<section class='card'>");
        html.AppendLine("<h1>Costing and Profit Report</h1>");
        html.AppendLine($"<p class='small'>Generated {Html(DateTime.Now.ToString("f", CultureInfo.CurrentCulture))}. Recommended retail uses a {targetMargin:0}% target margin.</p>");
        html.AppendLine("<h2>Jewellery Stock Costing</h2>");
        html.AppendLine("<table><tr><th>Stock</th><th>Status</th><th>Material</th><th>Labour</th><th>Other</th><th>Total Cost</th><th>Retail</th><th>Profit</th><th>Margin</th><th>Markup</th><th>Recommended Retail</th></tr>");
        foreach (var item in items)
        {
            var labour = item.LabourHours * item.LabourRate;
            var cost = PricingService.CalculateJewelleryCost(item);
            var profit = PricingService.CalculateRetailProfit(item);
            html.AppendLine($"<tr><td>{Html(item.ToString())}</td><td>{Html(item.Status.ToString())}</td><td>{Money(item.MaterialCost)}</td><td>{Money(labour)}</td><td>{Money(item.OtherCost)}</td><td>{Money(cost)}</td><td>{Money(item.RetailPrice)}</td><td>{Money(profit)}</td><td>{Percent(PricingService.CalculateProfitMargin(item.RetailPrice, cost))}</td><td>{Percent(PricingService.CalculateMarkup(item.RetailPrice, cost))}</td><td>{Money(PricingService.CalculateRecommendedRetail(cost, targetMargin))}</td></tr>");
        }
        html.AppendLine("</table>");
        html.AppendLine("<h2>Job Costing</h2>");
        html.AppendLine("<table><tr><th>Job</th><th>Status</th><th>Material</th><th>Labour</th><th>Total Cost</th><th>Price</th><th>Profit</th><th>Margin</th><th>Balance</th></tr>");
        foreach (var job in jobs)
        {
            var price = job.FinalPrice > 0 ? job.FinalPrice : job.QuoteAmount;
            var cost = PricingService.CalculateJobCost(job);
            html.AppendLine($"<tr><td>{Html(job.ToString())}</td><td>{Html(job.Status.ToString())}</td><td>{Money(job.MaterialCost)}</td><td>{Money(job.LabourCost)}</td><td>{Money(cost)}</td><td>{Money(price)}</td><td>{Money(price - cost)}</td><td>{Percent(PricingService.CalculateProfitMargin(price, cost))}</td><td>{Money(job.BalanceOwing)}</td></tr>");
        }
        html.AppendLine("</table>");
        html.AppendLine("</section>");
        html.Append(HtmlFooter());
        File.WriteAllText(path, html.ToString());
        return path;
    }

    public static string CreateLowStockReport()
    {
        Directory.CreateDirectory(PrintoutFolder);
        using var db = new AppDbContext();
        var path = Path.Combine(PrintoutFolder, $"LowStockReport_{DateTime.Now:yyyyMMdd_HHmmss}.html");
        var materials = db.Materials.AsEnumerable()
            .OrderBy(m => m.CurrentQuantity > m.ReorderLevel)
            .ThenBy(m => m.Category)
            .ThenBy(m => m.Name)
            .ToList();
        var low = materials.Where(m => m.CurrentQuantity <= m.ReorderLevel).ToList();

        var html = new StringBuilder();
        html.Append(HtmlHeader("Low Material Stock Report"));
        html.AppendLine("<section class='card'>");
        html.AppendLine("<h1>Low Material Stock Report</h1>");
        html.AppendLine(Row("Materials below reorder level", low.Count.ToString(CultureInfo.InvariantCulture)));
        html.AppendLine("<table><tr><th>Material</th><th>Category</th><th>Current Quantity</th><th>Reorder Level</th><th>Shortfall</th><th>Unit</th><th>Storage</th></tr>");
        foreach (var material in low)
        {
            var shortfall = Math.Max(0, material.ReorderLevel - material.CurrentQuantity);
            html.AppendLine($"<tr><td>{Html(material.ToString())}</td><td>{Html(material.Category.ToString())}</td><td>{Number(material.CurrentQuantity)}</td><td>{Number(material.ReorderLevel)}</td><td>{Number(shortfall)}</td><td>{Html(material.UnitType.ToString())}</td><td>{Html(material.StorageLocation ?? string.Empty)}</td></tr>");
        }
        html.AppendLine("</table>");
        html.AppendLine("</section>");
        html.Append(HtmlFooter());
        File.WriteAllText(path, html.ToString());
        return path;
    }

    public static string CreateJobsDueReport()
    {
        Directory.CreateDirectory(PrintoutFolder);
        using var db = new AppDbContext();
        var path = Path.Combine(PrintoutFolder, $"JobsDueReport_{DateTime.Now:yyyyMMdd_HHmmss}.html");
        var jobs = db.Jobs.AsEnumerable()
            .Where(j => j.Status != JobStatus.Completed && j.Status != JobStatus.Cancelled)
            .OrderBy(j => j.DueDate ?? DateTime.MaxValue)
            .ToList();
        var now = DateTime.Today;
        var dueSoon = jobs.Where(j => j.DueDate.HasValue && j.DueDate.Value.Date <= now.AddDays(14)).ToList();

        var html = new StringBuilder();
        html.Append(HtmlHeader("Jobs Due Soon Report"));
        html.AppendLine("<section class='card'>");
        html.AppendLine("<h1>Jobs Due Soon Report</h1>");
        html.AppendLine(Row("Active jobs", jobs.Count.ToString(CultureInfo.InvariantCulture)));
        html.AppendLine(Row("Due within 14 days / overdue", dueSoon.Count.ToString(CultureInfo.InvariantCulture)));
        html.AppendLine("<table><tr><th>Job</th><th>Status</th><th>Due</th><th>Days Left</th><th>Balance</th><th>Price</th><th>Estimated Profit</th></tr>");
        foreach (var job in jobs)
        {
            var days = job.DueDate.HasValue ? (job.DueDate.Value.Date - now).TotalDays.ToString("0", CultureInfo.InvariantCulture) : "No due date";
            var price = job.FinalPrice > 0 ? job.FinalPrice : job.QuoteAmount;
            html.AppendLine($"<tr><td>{Html(job.ToString())}</td><td>{Html(job.Status.ToString())}</td><td>{Html(job.DueDate?.ToShortDateString() ?? string.Empty)}</td><td>{Html(days)}</td><td>{Money(job.BalanceOwing)}</td><td>{Money(price)}</td><td>{Money(PricingService.CalculateJobProfit(job))}</td></tr>");
        }
        html.AppendLine("</table>");
        html.AppendLine("</section>");
        html.Append(HtmlFooter());
        File.WriteAllText(path, html.ToString());
        return path;
    }

    public static string CreateProductionCapacityReport()
    {
        Directory.CreateDirectory(PrintoutFolder);
        using var db = new AppDbContext();
        var today = DateTime.Today;
        var path = Path.Combine(PrintoutFolder, $"ProductionCapacityReport_{DateTime.Now:yyyyMMdd_HHmmss}.html");
        var jobs = db.Jobs.AsEnumerable()
            .Where(j => j.Status != JobStatus.Completed && j.Status != JobStatus.Cancelled)
            .OrderBy(j => j.DueDate ?? DateTime.MaxValue)
            .ThenBy(j => j.JobCode)
            .ToList();
        var batches = db.ProductionBatches.AsEnumerable()
            .Where(b => b.Status != ProductionBatchStatus.Completed && b.Status != ProductionBatchStatus.Cancelled)
            .OrderBy(b => b.TargetCompletionDate ?? DateTime.MaxValue)
            .ThenBy(b => b.Name)
            .ToList();

        const decimal weeklyBenchCapacityHours = 32m;
        var overdue = jobs.Where(j => j.DueDate.HasValue && j.DueDate.Value.Date < today).ToList();
        var dueThisWeek = jobs.Where(j => j.DueDate.HasValue && j.DueDate.Value.Date <= today.AddDays(7)).ToList();
        var dueNextWeek = jobs.Where(j => j.DueDate.HasValue && j.DueDate.Value.Date > today.AddDays(7) && j.DueDate.Value.Date <= today.AddDays(14)).ToList();
        var unscheduled = jobs.Where(j => !j.DueDate.HasValue).ToList();
        var thisWeekHours = dueThisWeek.Sum(j => j.LabourHours);
        var missingLabourHours = jobs.Count(j => j.LabourHours <= 0);
        var risk = thisWeekHours > weeklyBenchCapacityHours
            ? "Capacity risk: recorded labour hours due this week exceed the planning capacity."
            : overdue.Count > 0
                ? "Schedule risk: overdue jobs need attention before new work is promised."
                : missingLabourHours > 0
                    ? "Data risk: some active jobs have no labour hours recorded, so capacity is understated."
                    : "No immediate capacity risk detected from recorded due dates and labour hours.";

        var html = new StringBuilder();
        html.Append(HtmlHeader("Production Capacity Report"));
        html.AppendLine("<section class='card'>");
        html.AppendLine("<h1>Production Capacity Snapshot</h1>");
        html.AppendLine($"<p class='small'>Generated {Html(DateTime.Now.ToString("f", CultureInfo.CurrentCulture))}. This no-schema planning snapshot uses existing job due dates, job labour hours and active production batches.</p>");
        html.AppendLine(Row("Active jobs", jobs.Count.ToString(CultureInfo.InvariantCulture)));
        html.AppendLine(Row("Overdue jobs", overdue.Count.ToString(CultureInfo.InvariantCulture)));
        html.AppendLine(Row("Due within 7 days", dueThisWeek.Count.ToString(CultureInfo.InvariantCulture)));
        html.AppendLine(Row("Recorded labour due within 7 days", $"{Number(thisWeekHours)} h / {Number(weeklyBenchCapacityHours)} h planning capacity"));
        html.AppendLine(Row("Jobs missing labour hours", missingLabourHours.ToString(CultureInfo.InvariantCulture)));
        html.AppendLine(Row("Active production batches", batches.Count.ToString(CultureInfo.InvariantCulture)));
        AppendDocumentNotice(html, "Capacity guidance", risk);

        html.AppendLine("<h2>Due-Date Buckets</h2>");
        html.AppendLine("<table><tr><th>Bucket</th><th>Jobs</th><th>Recorded Labour</th><th>Guidance</th></tr>");
        AppendCapacityBucket(html, "Overdue", overdue, "Move, reschedule, or contact the customer before adding more work.");
        AppendCapacityBucket(html, "Due within 7 days", dueThisWeek, thisWeekHours > weeklyBenchCapacityHours ? "Over planning capacity. Prioritise and reschedule lower-risk jobs." : "Review daily sequence and payment/handover blockers.");
        AppendCapacityBucket(html, "Due 8-14 days", dueNextWeek, "Confirm materials, supplier diamonds and customer approvals now.");
        AppendCapacityBucket(html, "No due date", unscheduled, "Set realistic due dates so Alert Centre and Production Board can guide the schedule.");
        html.AppendLine("</table>");

        html.AppendLine("<h2>Active Job Load</h2>");
        html.AppendLine("<table><tr><th>Job</th><th>Status</th><th>Due</th><th>Labour Hours</th><th>Balance</th><th>Planning Note</th></tr>");
        foreach (var job in jobs)
        {
            var note = BuildCapacityNote(job, today);
            html.AppendLine($"<tr><td>{Html(job.ToString())}</td><td>{Html(job.Status.ToString())}</td><td>{Html(job.DueDate?.ToShortDateString() ?? "Not set")}</td><td>{Number(job.LabourHours)}</td><td>{Money(job.BalanceOwing)}</td><td>{Html(note)}</td></tr>");
        }
        html.AppendLine("</table>");

        html.AppendLine("<h2>Active Batch Load</h2>");
        if (batches.Count == 0)
        {
            html.AppendLine("<p class='small'>No active production batches are currently recorded.</p>");
        }
        else
        {
            html.AppendLine("<table><tr><th>Batch</th><th>Status</th><th>Target</th><th>Estimated Labour</th><th>Pieces</th><th>Progress</th></tr>");
            foreach (var batch in batches)
            {
                html.AppendLine($"<tr><td>{Html(batch.ToString())}</td><td>{Html(batch.Status.ToString())}</td><td>{Html(batch.TargetCompletionDate?.ToShortDateString() ?? "Not set")}</td><td>{Number(batch.EstimatedLabourHours)}</td><td>{Number(batch.CompletedPieces)} / {Number(batch.PlannedPieces)}</td><td>{batch.ProgressPercent.ToString("P0", CultureInfo.CurrentCulture)}</td></tr>");
            }
            html.AppendLine("</table>");
        }

        html.AppendLine("<h2>Scheduling Checklist</h2>");
        html.AppendLine("<div class='checkboxes'><p>[ ] Overdue jobs contacted or rescheduled</p><p>[ ] This-week labour load reviewed</p><p>[ ] Jobs missing labour hours estimated</p><p>[ ] Materials and supplier stones checked</p><p>[ ] Payment and handover blockers reviewed</p><p>[ ] New promise dates checked against capacity</p></div>");
        html.AppendLine("</section>");
        html.Append(HtmlFooter());
        File.WriteAllText(path, html.ToString());
        return path;
    }

    public static string CreateMarketPerformanceReport()
    {
        Directory.CreateDirectory(PrintoutFolder);
        using var db = new AppDbContext();
        var path = Path.Combine(PrintoutFolder, $"MarketPerformanceReport_{DateTime.Now:yyyyMMdd_HHmmss}.html");
        var markets = db.MarketEvents.AsEnumerable().OrderByDescending(m => m.EventDate).ToList();
        var marketSales = db.Sales.AsEnumerable().Where(s => s.SaleLocation == SaleLocation.Market).ToList();
        var marketStock = db.MarketStocks.AsEnumerable().ToList();

        var html = new StringBuilder();
        html.Append(HtmlHeader("Market Performance Report"));
        html.AppendLine("<section class='card'>");
        html.AppendLine("<h1>Market Performance Report</h1>");
        html.AppendLine("<table><tr><th>Market</th><th>Date</th><th>Location</th><th>Items Packed</th><th>Items Marked Sold</th><th>Recorded Sales</th><th>Profit</th><th>Stall Fee</th><th>Net After Fee</th></tr>");
        foreach (var market in markets)
        {
            var linkedStock = marketStock.Where(ms => ms.MarketEventId == market.Id).ToList();
            var sameDaySales = marketSales.Where(s => s.SaleDate.Date == market.EventDate.Date).ToList();
            var salesTotal = sameDaySales.Sum(s => s.SaleAmount);
            var profit = sameDaySales.Sum(s => s.Profit);
            html.AppendLine($"<tr><td>{Html(market.Name)}</td><td>{Html(market.EventDate.ToShortDateString())}</td><td>{Html(market.Location ?? string.Empty)}</td><td>{linkedStock.Count}</td><td>{linkedStock.Count(ms => ms.SoldAtMarket)}</td><td>{Money(salesTotal)}</td><td>{Money(profit)}</td><td>{Money(market.StallFee)}</td><td>{Money(profit - market.StallFee)}</td></tr>");
        }
        html.AppendLine("</table>");
        html.AppendLine("<p class='small'>Market sales are matched by sale date and Sale Location = Market. For more exact reporting later, V1.5 can add a direct Market Event selector to each sale.</p>");
        html.AppendLine("</section>");
        html.Append(HtmlFooter());
        File.WriteAllText(path, html.ToString());
        return path;
    }

    public static string CreateOperationsPerformanceReport()
    {
        Directory.CreateDirectory(PrintoutFolder);
        using var db = new AppDbContext();
        var today = DateTime.Today;
        var weekStart = today.AddDays(-6);
        var monthStart = new DateTime(today.Year, today.Month, 1);
        var path = Path.Combine(PrintoutFolder, $"OperationsPerformance_{DateTime.Now:yyyyMMdd_HHmmss}.html");

        var jobs = db.Jobs.AsEnumerable().ToList();
        var batches = db.ProductionBatches.AsEnumerable().ToList();
        var externalDiamonds = db.ExternalDiamonds.AsEnumerable().ToList();
        var marketEvents = db.MarketEvents.AsEnumerable().ToList();
        var marketStock = db.MarketStocks.AsEnumerable().ToList();
        var sales = db.Sales.AsEnumerable().ToList();
        var tasks = db.BusinessTasks.AsEnumerable().ToList();

        var activeJobs = jobs.Where(j => j.Status != JobStatus.Completed && j.Status != JobStatus.Cancelled).ToList();
        var completedThisMonth = jobs.Where(j => j.Status == JobStatus.Completed && j.UpdatedAt.Date >= monthStart && j.UpdatedAt.Date <= today).ToList();
        var completedThisWeek = completedThisMonth.Where(j => j.UpdatedAt.Date >= weekStart).ToList();
        var overdueJobs = activeJobs.Where(j => j.DueDate.HasValue && j.DueDate.Value.Date < today).ToList();
        var dueThisWeek = activeJobs.Where(j => j.DueDate.HasValue && j.DueDate.Value.Date <= today.AddDays(7)).ToList();
        var activeBatchCount = batches.Count(b => b.Status != ProductionBatchStatus.Completed && b.Status != ProductionBatchStatus.Cancelled);
        var marketSales = sales.Where(s => s.SaleLocation == SaleLocation.Market).ToList();
        var monthMarketSales = marketSales.Where(s => s.SaleDate.Date >= monthStart && s.SaleDate.Date <= today).ToList();
        var openTasks = tasks.Where(t => t.Status != BusinessTaskStatus.Completed && t.Status != BusinessTaskStatus.Cancelled).ToList();
        var overdueTasks = openTasks.Count(t => t.DueDate.HasValue && t.DueDate.Value.Date < today);

        var html = new StringBuilder();
        html.Append(HtmlHeader("Operations Performance Report"));
        html.AppendLine("<section class='card'>");
        html.AppendLine("<h1>Operations Performance Report</h1>");
        html.AppendLine($"<p class='small'>Generated {Html(DateTime.Now.ToString("f", CultureInfo.CurrentCulture))}. This V1.92 report is read-only and combines workshop productivity, supplier diamonds, market performance and report rhythm guidance from existing OPALNOVA records.</p>");
        html.AppendLine("<h2>Operations Snapshot</h2>");
        html.AppendLine("<div class='financial-summary'>");
        html.AppendLine(SummaryTile("Active jobs", activeJobs.Count.ToString(CultureInfo.InvariantCulture), $"{dueThisWeek.Count} due within 7 days"));
        html.AppendLine(SummaryTile("Completed this month", completedThisMonth.Count.ToString(CultureInfo.InvariantCulture), $"{completedThisWeek.Count} completed this week"));
        html.AppendLine(SummaryTile("Recorded labour load", $"{Number(activeJobs.Sum(j => j.LabourHours))} h", $"{activeJobs.Count(j => j.LabourHours <= 0)} jobs missing labour"));
        html.AppendLine(SummaryTile("Supplier diamonds", externalDiamonds.Count.ToString(CultureInfo.InvariantCulture), $"{externalDiamonds.Count(d => d.IsHoldExpired)} expired holds"));
        html.AppendLine(SummaryTile("Market sales this month", Money(monthMarketSales.Sum(s => s.SaleAmount)), $"{monthMarketSales.Count} sale records"));
        html.AppendLine(SummaryTile("Open follow-ups", openTasks.Count.ToString(CultureInfo.InvariantCulture), $"{overdueTasks} overdue"));
        html.AppendLine("</div>");

        AppendDocumentNotice(html, "Recommended review rhythm", BuildOperationsReviewGuidance(overdueJobs, activeJobs, externalDiamonds, marketEvents, openTasks));
        AppendWorkshopProductivitySection(html, activeJobs, completedThisWeek, completedThisMonth, batches, today);
        AppendSupplierDiamondPerformanceSection(html, externalDiamonds);
        AppendMarketOperationsSection(html, marketEvents, marketStock, marketSales, today);
        AppendReportRhythmSection(html, today, openTasks);

        html.AppendLine("</section>");
        html.Append(HtmlFooter());
        File.WriteAllText(path, html.ToString());
        return path;
    }

    private static void AppendCapacityBucket(StringBuilder html, string label, List<Job> jobs, string guidance)
    {
        html.AppendLine($"<tr><td>{Html(label)}</td><td>{jobs.Count}</td><td>{Number(jobs.Sum(j => j.LabourHours))} h</td><td>{Html(guidance)}</td></tr>");
    }

    private static string SummaryTile(string label, string value, string hint) =>
        $"<div class='summary-tile'><span>{Html(label)}</span><strong>{Html(value)}</strong><em>{Html(hint)}</em></div>";

    private static string BuildOperationsReviewGuidance(
        List<Job> overdueJobs,
        List<Job> activeJobs,
        List<ExternalDiamond> externalDiamonds,
        List<MarketEvent> marketEvents,
        List<BusinessTask> openTasks)
    {
        if (overdueJobs.Count > 0)
            return $"Start with {overdueJobs.Count} overdue production job(s), then review customer communication and due-date changes.";
        if (externalDiamonds.Any(d => d.IsHoldExpired))
            return "Review expired supplier diamond holds before promising availability or quoting replacements.";
        if (openTasks.Any(t => t.DueDate.HasValue && t.DueDate.Value.Date < DateTime.Today))
            return "Clear overdue follow-ups before creating more scheduled report reminders.";
        if (marketEvents.Any(m => m.EventDate.Date >= DateTime.Today && m.EventDate.Date <= DateTime.Today.AddDays(14)))
            return "A market is coming up within 14 days. Run packing, inventory value and stock ageing reports before selecting stock.";
        if (activeJobs.Any(j => j.LabourHours <= 0))
            return "Record labour-hour estimates on active jobs so capacity and productivity reports become more useful.";
        return "No immediate operations risk detected. Use this report as a weekly planning checkpoint.";
    }

    private static void AppendWorkshopProductivitySection(
        StringBuilder html,
        List<Job> activeJobs,
        List<Job> completedThisWeek,
        List<Job> completedThisMonth,
        List<ProductionBatch> batches,
        DateTime today)
    {
        html.AppendLine("<h2>Workshop Productivity</h2>");
        html.AppendLine(Row("Completed jobs this week", completedThisWeek.Count.ToString(CultureInfo.InvariantCulture)));
        html.AppendLine(Row("Completed jobs this month", completedThisMonth.Count.ToString(CultureInfo.InvariantCulture)));
        html.AppendLine(Row("Completed job labour this month", $"{Number(completedThisMonth.Sum(j => j.LabourHours))} h"));
        html.AppendLine(Row("Estimated completed job profit this month", Money(completedThisMonth.Sum(PricingService.CalculateJobProfit))));
        html.AppendLine(Row("Active jobs missing labour estimate", activeJobs.Count(j => j.LabourHours <= 0).ToString(CultureInfo.InvariantCulture)));
        html.AppendLine(Row("Active production batches", batches.Count(b => b.Status != ProductionBatchStatus.Completed && b.Status != ProductionBatchStatus.Cancelled).ToString(CultureInfo.InvariantCulture)));

        html.AppendLine("<table><tr><th>Status</th><th>Active Jobs</th><th>Recorded Labour</th><th>Outstanding Balance</th><th>Planning Note</th></tr>");
        foreach (var group in activeJobs.GroupBy(j => j.Status).OrderBy(g => g.Key.ToString()))
        {
            var note = group.Any(j => j.DueDate.HasValue && j.DueDate.Value.Date < today)
                ? "Contains overdue work"
                : group.Any(j => j.LabourHours <= 0)
                    ? "Some labour estimates missing"
                    : "Monitor normally";
            html.AppendLine($"<tr><td>{Html(group.Key.ToString())}</td><td>{group.Count()}</td><td>{Number(group.Sum(j => j.LabourHours))} h</td><td>{Money(group.Sum(j => Math.Max(0, j.BalanceOwing)))}</td><td>{Html(note)}</td></tr>");
        }
        html.AppendLine("</table>");

        html.AppendLine("<h3>Recently Completed Jobs</h3>");
        html.AppendLine("<table><tr><th>Job</th><th>Type</th><th>Completed / Updated</th><th>Labour</th><th>Price</th><th>Estimated Profit</th></tr>");
        foreach (var job in completedThisMonth.OrderByDescending(j => j.UpdatedAt).Take(20))
            html.AppendLine($"<tr><td>{Html(job.ToString())}</td><td>{Html(job.Type.ToString())}</td><td>{Html(job.UpdatedAt.ToShortDateString())}</td><td>{Number(job.LabourHours)} h</td><td>{Money(JobPrice(job))}</td><td>{Money(PricingService.CalculateJobProfit(job))}</td></tr>");
        html.AppendLine("</table>");
    }

    private static void AppendSupplierDiamondPerformanceSection(StringBuilder html, List<ExternalDiamond> diamonds)
    {
        html.AppendLine("<h2>Supplier Diamond Performance</h2>");
        html.AppendLine(Row("Saved external diamonds", diamonds.Count.ToString(CultureInfo.InvariantCulture)));
        html.AppendLine(Row("Saved supplier value", Money(diamonds.Sum(d => d.SupplierPrice))));
        html.AppendLine(Row("Estimated retail value", Money(diamonds.Sum(d => d.EstimatedRetailPrice))));
        html.AppendLine(Row("Active / expired holds", $"{diamonds.Count(d => d.HasActiveHold)} active / {diamonds.Count(d => d.IsHoldExpired)} expired"));
        html.AppendLine(Row("Ordered not received", diamonds.Count(d => d.IsOrderedNotReceived).ToString(CultureInfo.InvariantCulture)));

        html.AppendLine("<table><tr><th>Status</th><th>Count</th><th>Supplier Value</th><th>Estimated Retail</th><th>Action</th></tr>");
        foreach (var group in diamonds.GroupBy(d => string.IsNullOrWhiteSpace(d.Status) ? "Unknown" : d.Status).OrderByDescending(g => g.Count()).ThenBy(g => g.Key))
        {
            var action = group.Any(d => d.IsHoldExpired)
                ? "Review expired holds and copy replacement search"
                : group.Any(d => d.IsOrderedNotReceived)
                    ? "Check expected arrival / received intake"
                    : "Monitor availability before quoting";
            html.AppendLine($"<tr><td>{Html(group.Key)}</td><td>{group.Count()}</td><td>{Money(group.Sum(d => d.SupplierPrice))}</td><td>{Money(group.Sum(d => d.EstimatedRetailPrice))}</td><td>{Html(action)}</td></tr>");
        }
        html.AppendLine("</table>");
    }

    private static void AppendMarketOperationsSection(
        StringBuilder html,
        List<MarketEvent> markets,
        List<MarketStock> marketStock,
        List<Sale> marketSales,
        DateTime today)
    {
        var pastMarkets = markets.Where(m => m.EventDate.Date <= today).OrderByDescending(m => m.EventDate).Take(8).ToList();
        var nextMarket = markets.Where(m => m.EventDate.Date >= today).OrderBy(m => m.EventDate).FirstOrDefault();
        html.AppendLine("<h2>Market Performance</h2>");
        html.AppendLine(Row("Recorded market sales", Money(marketSales.Sum(s => s.SaleAmount))));
        html.AppendLine(Row("Recorded market profit", Money(marketSales.Sum(s => s.Profit))));
        html.AppendLine(Row("Next market", nextMarket == null ? "No future market recorded" : $"{nextMarket.Name} - {nextMarket.EventDate:d}"));

        html.AppendLine("<table><tr><th>Market</th><th>Date</th><th>Packed</th><th>Sold</th><th>Returned</th><th>Same-Day Sales</th><th>Net Event Estimate</th><th>Follow-up</th></tr>");
        foreach (var market in pastMarkets)
        {
            var stock = marketStock.Where(ms => ms.MarketEventId == market.Id).ToList();
            var sameDaySales = marketSales.Where(s => s.SaleDate.Date == market.EventDate.Date).ToList();
            var missingReconciliation = !market.LastReconciledAt.HasValue && market.EventDate.Date < today;
            html.AppendLine($"<tr><td>{Html(market.Name)}</td><td>{Html(market.EventDate.ToShortDateString())}</td><td>{stock.Count(s => s.Packed)}</td><td>{stock.Count(s => s.SoldAtMarket)}</td><td>{stock.Count(s => s.ReturnedToStock)}</td><td>{Money(sameDaySales.Sum(s => s.SaleAmount))}</td><td>{Money(market.NetMarketProfit)}</td><td>{Html(missingReconciliation ? "Reconcile event" : "Review stock return/listing")}</td></tr>");
        }
        html.AppendLine("</table>");
    }

    private static void AppendReportRhythmSection(StringBuilder html, DateTime today, List<BusinessTask> openTasks)
    {
        html.AppendLine("<h2>Suggested Report Rhythm</h2>");
        html.AppendLine("<p class='small'>This section is advisory only. Create task reminders from Tasks Studio if you want these reviews on the dashboard.</p>");
        html.AppendLine("<table><tr><th>Cadence</th><th>Reports</th><th>Suggested Next Run</th><th>Why</th></tr>");
        html.AppendLine($"<tr><td>Weekly</td><td>BI Command Report, Operations Performance, Customer Follow-Ups, Outstanding Balances</td><td>{Html(NextWeekday(today, DayOfWeek.Monday).ToShortDateString())}</td><td>Sets production, payment and follow-up priorities for the week.</td></tr>");
        html.AppendLine($"<tr><td>Weekly before markets</td><td>Market Performance, Stock Ageing, Inventory Value, Packing List</td><td>{Html(today.AddDays(7).ToShortDateString())}</td><td>Prevents overpacking and highlights stock to promote or return.</td></tr>");
        html.AppendLine($"<tr><td>Monthly</td><td>Monthly Sales, Profitability, Tax / GST Summary, Customer Segment Report</td><td>{Html(new DateTime(today.Year, today.Month, 1).AddMonths(1).ToShortDateString())}</td><td>Supports bookkeeping, buying and customer relationship planning.</td></tr>");
        html.AppendLine($"<tr><td>Monthly stock review</td><td>Inventory Value, Stock Ageing, Reorder Report, Supplier Diamond Holds</td><td>{Html(new DateTime(today.Year, today.Month, 1).AddMonths(1).AddDays(2).ToShortDateString())}</td><td>Finds low stock, slow-moving stock and supplier diamonds needing action.</td></tr>");
        html.AppendLine("</table>");
        html.AppendLine(Row("Open task pressure", $"{openTasks.Count} open task(s), {openTasks.Count(t => t.DueDate.HasValue && t.DueDate.Value.Date < today)} overdue"));
    }

    private static DateTime NextWeekday(DateTime start, DayOfWeek day)
    {
        var daysToAdd = ((int)day - (int)start.DayOfWeek + 7) % 7;
        if (daysToAdd == 0)
            daysToAdd = 7;
        return start.AddDays(daysToAdd);
    }

    private static string BuildCapacityNote(Job job, DateTime today)
    {
        if (!job.DueDate.HasValue)
            return "No due date set";
        if (job.DueDate.Value.Date < today)
            return "Overdue";
        if (job.LabourHours <= 0)
            return "Labour hours not recorded";
        if (job.Status is JobStatus.AwaitingMaterials)
            return "Check materials or supplier stone blockers";
        if (job.BalanceOwing > 0 && job.Status is JobStatus.ReadyForPickup or JobStatus.ReadyToShip)
            return "Payment balance before handover";
        return job.DueDate.Value.Date <= today.AddDays(7) ? "Schedule this week" : "Upcoming";
    }

    public static string CreateProductionBatchReport(ProductionBatch? selectedBatch = null)
    {
        Directory.CreateDirectory(PrintoutFolder);
        using var db = new AppDbContext();
        var path = Path.Combine(PrintoutFolder, $"ProductionBatchReport_{DateTime.Now:yyyyMMdd_HHmmss}.html");
        var batches = selectedBatch == null
            ? db.ProductionBatches.AsEnumerable().OrderBy(b => b.TargetCompletionDate ?? DateTime.MaxValue).ThenBy(b => b.Name).ToList()
            : db.ProductionBatches.AsEnumerable().Where(b => b.Id == selectedBatch.Id).ToList();
        var items = db.ProductionBatchItems.AsEnumerable().ToList();
        var marketEvents = db.MarketEvents.AsEnumerable().ToDictionary(m => m.Id, m => m);

        var html = new StringBuilder();
        html.Append(HtmlHeader(selectedBatch == null ? "Production Batch Report" : $"Batch Report - {selectedBatch.Name}"));
        html.AppendLine("<section class='card'>");
        html.AppendLine("<h1>Production Batches & Collection Planning</h1>");
        html.AppendLine($"<p class='small'>Generated {Html(DateTime.Now.ToString("f", CultureInfo.CurrentCulture))}</p>");

        var activeBatches = batches.Where(b => b.Status != ProductionBatchStatus.Completed && b.Status != ProductionBatchStatus.Cancelled).ToList();
        html.AppendLine(Row("Batches shown", batches.Count.ToString(CultureInfo.InvariantCulture)));
        html.AppendLine(Row("Active batches", activeBatches.Count.ToString(CultureInfo.InvariantCulture)));
        html.AppendLine(Row("Planned retail value", Money(batches.Sum(b => b.EstimatedRetailValue))));
        html.AppendLine(Row("Estimated material cost", Money(batches.Sum(b => b.EstimatedMaterialCost))));
        html.AppendLine(Row("Average active progress", activeBatches.Count == 0 ? "0%" : activeBatches.Average(b => b.ProgressPercent).ToString("P0", CultureInfo.CurrentCulture)));

        foreach (var batch in batches)
        {
            var linkedItems = items.Where(i => i.ProductionBatchId == batch.Id).OrderBy(i => i.Status).ThenBy(i => i.ItemName).ToList();
            var marketName = batch.MarketEventId.HasValue && marketEvents.TryGetValue(batch.MarketEventId.Value, out var market)
                ? market.ToString()
                : string.Empty;
            var itemCost = linkedItems.Sum(i => i.EstimatedCost);
            var itemRetail = linkedItems.Sum(i => i.EstimatedRetailValue);
            var plannedPieces = linkedItems.Count > 0 ? linkedItems.Sum(i => i.PlannedQuantity) : batch.PlannedPieces;
            var completedPieces = linkedItems.Count > 0 ? linkedItems.Sum(i => i.CompletedQuantity) : batch.CompletedPieces;
            var progress = plannedPieces <= 0 ? 0 : completedPieces / plannedPieces;

            html.AppendLine("<hr>");
            html.AppendLine($"<h2>{Html(batch.ToString())}</h2>");
            html.AppendLine(Row("Collection", batch.CollectionName ?? string.Empty));
            html.AppendLine(Row("Status", batch.Status.ToString()));
            html.AppendLine(Row("Start", batch.StartDate.ToShortDateString()));
            html.AppendLine(Row("Target completion", batch.TargetCompletionDate?.ToShortDateString() ?? string.Empty));
            html.AppendLine(Row("Linked market", marketName));
            html.AppendLine(Row("Pieces", $"{Number(completedPieces)} complete / {Number(plannedPieces)} planned"));
            html.AppendLine(Row("Progress", progress.ToString("P0", CultureInfo.CurrentCulture)));
            html.AppendLine(Row("Batch estimated material cost", Money(batch.EstimatedMaterialCost)));
            html.AppendLine(Row("Linked item estimated cost", Money(itemCost)));
            html.AppendLine(Row("Batch estimated retail", Money(batch.EstimatedRetailValue)));
            html.AppendLine(Row("Linked item estimated retail", Money(itemRetail)));
            html.AppendLine(Row("Estimated profit", Money((batch.EstimatedRetailValue > 0 ? batch.EstimatedRetailValue : itemRetail) - (batch.EstimatedMaterialCost > 0 ? batch.EstimatedMaterialCost : itemCost))));
            html.AppendLine(NotesBlock("Batch notes", batch.Notes));

            if (linkedItems.Count == 0)
            {
                html.AppendLine("<p class='small'>No batch items have been added yet. Use Add To Batch to plan individual pieces, stones, jobs or jewellery items.</p>");
            }
            else
            {
                html.AppendLine("<table><tr><th>Item</th><th>Type</th><th>Status</th><th>Planned</th><th>Complete</th><th>Cost</th><th>Retail</th><th>Profit</th><th>Links</th><th>Notes</th></tr>");
                foreach (var item in linkedItems)
                {
                    var links = new List<string>();
                    if (item.JewelleryItemId.HasValue) links.Add($"Jewellery #{item.JewelleryItemId.Value}");
                    if (item.StoneId.HasValue) links.Add($"Stone #{item.StoneId.Value}");
                    if (item.JobId.HasValue) links.Add($"Job #{item.JobId.Value}");
                    html.AppendLine($"<tr><td>{Html(item.ItemName)}</td><td>{Html(item.ItemType)}</td><td>{Html(item.Status)}</td><td>{Number(item.PlannedQuantity)}</td><td>{Number(item.CompletedQuantity)}</td><td>{Money(item.EstimatedCost)}</td><td>{Money(item.EstimatedRetailValue)}</td><td>{Money(item.EstimatedProfit)}</td><td>{Html(string.Join(", ", links))}</td><td>{Html(item.Notes ?? string.Empty)}</td></tr>");
                }
                html.AppendLine("</table>");
            }
        }

        html.AppendLine("<h2>Planning checklist</h2>");
        html.AppendLine("<div class='checkboxes'><p>☐ Stones selected</p><p>☐ Materials checked</p><p>☐ Missing supplies ordered</p><p>☐ Making complete</p><p>☐ Polishing complete</p><p>☐ Photos complete</p><p>☐ Listings written</p><p>☐ Market stock packed</p></div>");
        html.AppendLine("</section>");
        html.Append(HtmlFooter());
        File.WriteAllText(path, html.ToString());
        return path;
    }


    public static void OpenInDefaultApp(string path)
    {
        if (!File.Exists(path)) return;
        Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
    }



    public static string CreateBusinessIntelligenceReport()
    {
        Directory.CreateDirectory(PrintoutFolder);
        using var db = new AppDbContext();
        var path = Path.Combine(PrintoutFolder, $"BusinessIntelligence_{DateTime.Now:yyyyMMdd_HHmmss}.html");
        var today = DateTime.Today;
        var weekStart = today.AddDays(-6);
        var monthStart = new DateTime(today.Year, today.Month, 1);
        var sales = db.Sales.AsEnumerable().ToList();
        var payments = db.Payments.AsEnumerable().ToList();
        var jobs = db.Jobs.AsEnumerable().ToList();
        var quotes = db.CustomQuotes.AsEnumerable().ToList();
        var materials = db.Materials.AsEnumerable().ToList();
        var stones = db.Stones.AsEnumerable().ToList();
        var jewellery = db.JewelleryItems.AsEnumerable().ToList();
        var stoneLinks = db.QuoteOptionStoneLinks.AsEnumerable().ToList();
        var materialLinks = db.QuoteOptionMaterialLinks.AsEnumerable().ToList();
        var tasks = db.BusinessTasks.AsEnumerable().ToList();

        var weekSales = sales.Where(s => s.SaleDate.Date >= weekStart && s.SaleDate.Date <= today).ToList();
        var monthSales = sales.Where(s => s.SaleDate.Date >= monthStart && s.SaleDate.Date <= today).ToList();
        var activeJobs = jobs.Where(j => j.Status != JobStatus.Completed && j.Status != JobStatus.Cancelled).ToList();
        var balances = jobs.Where(j => j.BalanceOwing > 0 && j.Status != JobStatus.Cancelled).ToList();
        var awaitingApproval = quotes.Where(q => q.Status.Contains("Proposal", StringComparison.OrdinalIgnoreCase) || q.Status.Contains("Sent", StringComparison.OrdinalIgnoreCase) || q.Status.Contains("Await", StringComparison.OrdinalIgnoreCase)).ToList();
        var acceptedQuotes = quotes.Count(q => q.AcceptedOptionId.HasValue || q.Status.Contains("Accepted", StringComparison.OrdinalIgnoreCase) || q.LinkedJobId.HasValue);
        var quoteRate = quotes.Count == 0 ? 0m : acceptedQuotes / (decimal)quotes.Count;
        var reservedStoneValue = stoneLinks.Where(l => l.ReservationStatus.Equals("Reserved", StringComparison.OrdinalIgnoreCase)).Sum(l => l.UnitCost);
        var reservedMaterialValue = materialLinks.Where(l => l.ReservationStatus.Equals("Reserved", StringComparison.OrdinalIgnoreCase)).Sum(l => l.LineCost);
        var lowMaterials = materials.Where(m => m.CurrentQuantity <= m.ReorderLevel).ToList();
        var openTasks = tasks.Where(t => t.Status != BusinessTaskStatus.Completed && t.Status != BusinessTaskStatus.Cancelled).ToList();
        var stockValue = jewellery.Where(i => i.Status != StockStatus.Sold).Sum(i => i.RetailPrice);
        var stockCost = jewellery.Where(i => i.Status != StockStatus.Sold).Sum(PricingService.CalculateJewelleryCost);
        var looseStoneValue = stones.Where(s => s.Status != StoneStatus.Sold && s.Status != StoneStatus.SetInJewellery).Sum(s => s.EstimatedValue);
        var materialValue = materials.Sum(m => m.CurrentQuantity * m.PurchaseCost);

        var html = new StringBuilder();
        html.Append(HtmlHeader("Business Intelligence Command Report"));
        html.AppendLine("<section class='card'>");
        html.AppendLine("<h1>Business Intelligence Command Report</h1>");
        html.AppendLine($"<p class='small'>Generated {Html(DateTime.Now.ToString("f", CultureInfo.CurrentCulture))}. This report is read-only and does not change business data.</p>");
        html.AppendLine("<h2>Executive Snapshot</h2>");
        html.AppendLine(Row("Sales last 7 days", Money(weekSales.Sum(s => s.SaleAmount))));
        html.AppendLine(Row("Profit last 7 days", Money(weekSales.Sum(s => s.Profit))));
        html.AppendLine(Row("Sales this month", Money(monthSales.Sum(s => s.SaleAmount))));
        html.AppendLine(Row("Profit this month", Money(monthSales.Sum(s => s.Profit))));
        html.AppendLine(Row("Outstanding balances", Money(balances.Sum(j => j.BalanceOwing))));
        html.AppendLine(Row("Quote conversion rate", Percent(quoteRate)));
        html.AppendLine(Row("Reserved inventory value", Money(reservedStoneValue + reservedMaterialValue)));
        html.AppendLine(Row("Stock retail value", Money(stockValue)));
        html.AppendLine(Row("Stock cost value", Money(stockCost)));
        html.AppendLine(Row("Loose stone value", Money(looseStoneValue)));
        html.AppendLine(Row("Material value", Money(materialValue)));
        html.AppendLine(Row("Open follow-ups / tasks", openTasks.Count.ToString(CultureInfo.InvariantCulture)));
        html.AppendLine(Row("Low-stock materials", lowMaterials.Count.ToString(CultureInfo.InvariantCulture)));
        html.AppendLine(Row("Active jobs", activeJobs.Count.ToString(CultureInfo.InvariantCulture)));
        html.AppendLine(Row("Quotes awaiting approval", awaitingApproval.Count.ToString(CultureInfo.InvariantCulture)));
        AppendSalesSummaryTable(html, "Sales by channel this month", monthSales);
        AppendOutstandingBalancesTable(html, balances.OrderByDescending(j => j.BalanceOwing).Take(20).ToList());
        AppendQuoteConversionTable(html, quotes);
        AppendProductCategoryProfitTable(html, "Profit by product/service category this month", monthSales, jewellery.ToDictionary(i => i.Id), jobs.ToDictionary(j => j.Id));
        AppendProfitableJobTypesTable(html, jobs);
        AppendInventoryValueTable(html, jewellery, stones, materials);
        AppendReservedInventoryTable(html, stoneLinks, materialLinks);
        AppendFollowUpTable(html, openTasks.OrderBy(t => t.DueDate ?? DateTime.MaxValue).Take(25).ToList());
        html.AppendLine("</section>");
        html.Append(HtmlFooter());
        File.WriteAllText(path, html.ToString());
        return path;
    }

    public static string CreateWeeklySalesSummaryReport()
    {
        Directory.CreateDirectory(PrintoutFolder);
        using var db = new AppDbContext();
        var today = DateTime.Today;
        var from = today.AddDays(-6);
        var sales = db.Sales.AsEnumerable().Where(s => s.SaleDate.Date >= from && s.SaleDate.Date <= today).OrderByDescending(s => s.SaleDate).ToList();
        var path = Path.Combine(PrintoutFolder, $"WeeklySalesSummary_{DateTime.Now:yyyyMMdd_HHmmss}.html");
        var html = new StringBuilder();
        html.Append(HtmlHeader("Weekly Sales Summary"));
        html.AppendLine("<section class='card'>");
        html.AppendLine("<h1>Weekly Sales Summary</h1>");
        html.AppendLine(Row("Date range", $"{from:d} to {today:d}"));
        html.AppendLine(Row("Sales", Money(sales.Sum(s => s.SaleAmount))));
        html.AppendLine(Row("Cost of goods", Money(sales.Sum(s => s.CostOfGoods))));
        html.AppendLine(Row("Profit", Money(sales.Sum(s => s.Profit))));
        html.AppendLine(Row("Margin", Percent(PricingService.CalculateProfitMargin(sales.Sum(s => s.SaleAmount), sales.Sum(s => s.CostOfGoods)))));
        AppendSalesSummaryTable(html, "Sales by channel", sales);
        AppendRecentSalesTable(html, sales);
        html.AppendLine("</section>");
        html.Append(HtmlFooter());
        File.WriteAllText(path, html.ToString());
        return path;
    }

    public static string CreateMonthlySalesSummaryReport()
    {
        Directory.CreateDirectory(PrintoutFolder);
        using var db = new AppDbContext();
        var today = DateTime.Today;
        var from = new DateTime(today.Year, today.Month, 1);
        var sales = db.Sales.AsEnumerable().Where(s => s.SaleDate.Date >= from && s.SaleDate.Date <= today).OrderByDescending(s => s.SaleDate).ToList();
        var path = Path.Combine(PrintoutFolder, $"MonthlySalesSummary_{DateTime.Now:yyyyMMdd_HHmmss}.html");
        var html = new StringBuilder();
        html.Append(HtmlHeader("Monthly Sales Summary"));
        html.AppendLine("<section class='card'>");
        html.AppendLine("<h1>Monthly Sales Summary</h1>");
        html.AppendLine(Row("Month", today.ToString("MMMM yyyy", CultureInfo.CurrentCulture)));
        html.AppendLine(Row("Sales", Money(sales.Sum(s => s.SaleAmount))));
        html.AppendLine(Row("Cost of goods", Money(sales.Sum(s => s.CostOfGoods))));
        html.AppendLine(Row("Profit", Money(sales.Sum(s => s.Profit))));
        html.AppendLine(Row("Margin", Percent(PricingService.CalculateProfitMargin(sales.Sum(s => s.SaleAmount), sales.Sum(s => s.CostOfGoods)))));
        AppendSalesSummaryTable(html, "Sales by channel", sales);
        AppendRecentSalesTable(html, sales);
        html.AppendLine("</section>");
        html.Append(HtmlFooter());
        File.WriteAllText(path, html.ToString());
        return path;
    }

    public static string CreateProfitabilityReport()
    {
        Directory.CreateDirectory(PrintoutFolder);
        using var db = new AppDbContext();
        var sales = db.Sales.AsEnumerable().OrderByDescending(s => s.SaleDate).ToList();
        var jobs = db.Jobs.AsEnumerable().OrderBy(j => j.Type).ThenByDescending(JobPrice).ToList();
        var jewelleryById = db.JewelleryItems.AsEnumerable().ToDictionary(i => i.Id);
        var jobsById = jobs.ToDictionary(j => j.Id);
        var reportJobs = jobs.Where(j => j.Status != JobStatus.Cancelled).ToList();
        var jobLinkedSales = sales.Where(s => s.JobId.HasValue && jobsById.ContainsKey(s.JobId.Value)).ToList();
        var stockLinkedSales = sales.Where(s => s.JewelleryItemId.HasValue && jewelleryById.ContainsKey(s.JewelleryItemId.Value)).ToList();
        var unsoldStock = jewelleryById.Values.Where(i => i.Status != StockStatus.Sold).ToList();
        var path = Path.Combine(PrintoutFolder, $"Profitability_{DateTime.Now:yyyyMMdd_HHmmss}.html");

        var html = new StringBuilder();
        html.Append(HtmlHeader("Profitability Report"));
        html.AppendLine("<section class='card'>");
        html.AppendLine("<h1>Profitability Report</h1>");
        html.AppendLine($"<p class='small'>Generated {Html(DateTime.Now.ToString("f", CultureInfo.CurrentCulture))}. This report is read-only and uses recorded sale amounts, cost of goods, jewellery categories and job types.</p>");
        html.AppendLine(Row("Recorded sales", sales.Count.ToString(CultureInfo.InvariantCulture)));
        html.AppendLine(Row("Recorded sales revenue", Money(sales.Sum(s => s.SaleAmount))));
        html.AppendLine(Row("Recorded sales profit", Money(sales.Sum(s => s.Profit))));
        html.AppendLine(Row("Recorded sales margin", Percent(PricingService.CalculateProfitMargin(sales.Sum(s => s.SaleAmount), sales.Sum(s => s.CostOfGoods)))));
        html.AppendLine(Row("Linked stock sales", stockLinkedSales.Count.ToString(CultureInfo.InvariantCulture)));
        html.AppendLine(Row("Linked job sales", jobLinkedSales.Count.ToString(CultureInfo.InvariantCulture)));
        html.AppendLine(Row("Estimated job profit", Money(reportJobs.Sum(PricingService.CalculateJobProfit))));
        html.AppendLine(Row("Potential unsold stock profit", Money(unsoldStock.Sum(PricingService.CalculateRetailProfit))));
        AppendProductCategoryProfitTable(html, "Recorded Profit by Product / Service Category", sales, jewelleryById, jobsById);
        AppendRealisedJobTypeProfitTable(html, jobLinkedSales, jobsById);
        AppendEstimatedJobTypeProfitTable(html, reportJobs);
        AppendProfitabilityDataQualityTable(html, sales, jobs, jewelleryById, jobsById);
        AppendRecentProfitSalesTable(html, sales.Take(40).ToList(), jewelleryById, jobsById);
        html.AppendLine("</section>");
        html.Append(HtmlFooter());
        File.WriteAllText(path, html.ToString());
        return path;
    }

    public static string CreateTaxSummaryReport()
    {
        Directory.CreateDirectory(PrintoutFolder);
        using var db = new AppDbContext();
        var settings = BusinessSettingsService.Load();
        var today = DateTime.Today;
        var sales = db.Sales.AsEnumerable().OrderByDescending(s => s.SaleDate).ToList();
        var payments = db.Payments.AsEnumerable().OrderByDescending(p => p.PaymentDate).ToList();
        var jobs = db.Jobs.AsEnumerable().ToList();
        var salesById = sales.ToDictionary(s => s.Id);
        var jobsById = jobs.ToDictionary(j => j.Id);
        var periods = new[]
        {
            new TaxPeriod("Current month", new DateTime(today.Year, today.Month, 1), today),
            new TaxPeriod("Financial quarter to date", StartOfFinancialQuarter(today), today),
            new TaxPeriod("Financial year to date", StartOfFinancialYear(today), today),
            new TaxPeriod("Last 12 months", today.AddYears(-1).AddDays(1), today)
        };
        var financialYear = periods[2];
        var financialYearSales = SalesInPeriod(sales, financialYear).ToList();
        var financialYearPayments = PaymentsInPeriod(payments, financialYear).ToList();
        var path = Path.Combine(PrintoutFolder, $"TaxSummary_{DateTime.Now:yyyyMMdd_HHmmss}.html");

        var html = new StringBuilder();
        html.Append(HtmlHeader("Tax and GST Summary"));
        html.AppendLine("<section class='card'>");
        html.AppendLine("<h1>Tax and GST Summary</h1>");
        html.AppendLine($"<p class='small'>Generated {Html(DateTime.Now.ToString("f", CultureInfo.CurrentCulture))}. This is a read-only bookkeeping summary, not formal tax advice.</p>");
        html.AppendLine(Row("Tax setting", settings.GstRegistered ? $"{settings.TaxLabel} registered at {settings.GstRatePercent:0.##}%" : $"{settings.TaxLabel} not registered / not enabled"));
        html.AppendLine(Row("Estimate method", settings.GstRegistered ? $"{settings.TaxLabel} is estimated as the tax-inclusive component of recorded sale totals." : $"No {settings.TaxLabel} component is calculated while the business is not marked registered."));
        html.AppendLine(Row("Financial year to date", $"{financialYear.Start:d} to {financialYear.End:d}"));
        html.AppendLine(Row("Current outstanding job balances", Money(jobs.Where(j => j.BalanceOwing > 0 && j.Status != JobStatus.Cancelled).Sum(j => j.BalanceOwing))));
        AppendTaxPeriodSummaryTable(html, periods, sales, payments, jobs, settings);
        AppendTaxSalesLocationTable(html, "Financial Year Sales by Location", financialYearSales, settings);
        AppendTaxPaymentMethodTable(html, "Financial Year Payments by Method", financialYearPayments);
        AppendTaxDataQualityTable(html, sales, payments, jobs, salesById, jobsById);
        AppendRecentTaxSalesTable(html, financialYearSales.Take(80).ToList(), settings);
        html.AppendLine("</section>");
        html.Append(HtmlFooter());
        File.WriteAllText(path, html.ToString());
        return path;
    }

    public static string CreateVisualReportCharts()
    {
        Directory.CreateDirectory(PrintoutFolder);
        using var db = new AppDbContext();
        var today = DateTime.Today;
        var months = RecentMonthStarts(today, 6);
        var sales = db.Sales.AsEnumerable().ToList();
        var payments = db.Payments.AsEnumerable().ToList();
        var jobs = db.Jobs.AsEnumerable().ToList();
        var quotes = db.CustomQuotes.AsEnumerable().ToList();
        var jewellery = db.JewelleryItems.AsEnumerable().ToList();
        var stones = db.Stones.AsEnumerable().ToList();
        var materials = db.Materials.AsEnumerable().ToList();
        var stoneLinks = db.QuoteOptionStoneLinks.AsEnumerable().ToList();
        var materialLinks = db.QuoteOptionMaterialLinks.AsEnumerable().ToList();
        var recentSales = sales.Where(s => s.SaleDate.Date >= months.First()).ToList();
        var recentPayments = payments.Where(p => p.PaymentDate.Date >= months.First()).ToList();
        var recentQuotes = quotes.Where(q => q.QuoteDate.Date >= months.First()).ToList();
        var unsoldJewellery = jewellery.Where(i => i.Status != StockStatus.Sold).ToList();
        var looseStones = stones.Where(s => s.Status != StoneStatus.Sold && s.Status != StoneStatus.SetInJewellery).ToList();
        var materialValue = materials.Sum(m => m.CurrentQuantity * m.PurchaseCost);
        var reservedValue = stoneLinks.Where(l => l.ReservationStatus.Equals("Reserved", StringComparison.OrdinalIgnoreCase)).Sum(l => l.UnitCost)
            + materialLinks.Where(l => l.ReservationStatus.Equals("Reserved", StringComparison.OrdinalIgnoreCase)).Sum(l => l.LineCost);
        var outstandingJobs = jobs.Where(j => j.BalanceOwing > 0 && j.Status != JobStatus.Cancelled).ToList();
        var path = Path.Combine(PrintoutFolder, $"VisualCharts_{DateTime.Now:yyyyMMdd_HHmmss}.html");

        var html = new StringBuilder();
        html.Append(HtmlHeader("Visual Report Charts"));
        html.AppendLine("<section class='card'>");
        html.AppendLine("<h1>Visual Report Charts</h1>");
        html.AppendLine($"<p class='small'>Generated {Html(DateTime.Now.ToString("f", CultureInfo.CurrentCulture))}. Charts are built from local OPALNOVA records and do not require internet access.</p>");
        html.AppendLine(Row("Recent sales", Money(recentSales.Sum(s => s.SaleAmount))));
        html.AppendLine(Row("Recent profit", Money(recentSales.Sum(s => s.Profit))));
        html.AppendLine(Row("Recent payments", Money(recentPayments.Sum(p => p.Amount))));
        html.AppendLine(Row("Quote conversion", Percent(recentQuotes.Count == 0 ? 0 : recentQuotes.Count(IsQuoteConverted) / (decimal)recentQuotes.Count)));
        html.AppendLine(Row("Inventory value", Money(unsoldJewellery.Sum(i => i.RetailPrice) + looseStones.Sum(s => s.EstimatedValue) + materialValue)));
        html.AppendLine(Row("Outstanding balances", Money(outstandingJobs.Sum(j => j.BalanceOwing))));

        AppendHorizontalBarChart(html, "Sales by Month", months.Select(month =>
        {
            var rows = sales.Where(s => s.SaleDate.Year == month.Year && s.SaleDate.Month == month.Month).ToList();
            return new ChartRow(MonthLabel(month), rows.Sum(s => s.SaleAmount), Money(rows.Sum(s => s.SaleAmount)), $"{rows.Count} sale(s), profit {Money(rows.Sum(s => s.Profit))}");
        }).ToList());

        AppendHorizontalBarChart(html, "Profit by Month", months.Select(month =>
        {
            var rows = sales.Where(s => s.SaleDate.Year == month.Year && s.SaleDate.Month == month.Month).ToList();
            return new ChartRow(MonthLabel(month), Math.Max(0, rows.Sum(s => s.Profit)), Money(rows.Sum(s => s.Profit)), $"{rows.Count} sale(s), cost {Money(rows.Sum(s => s.CostOfGoods))}");
        }).ToList());

        AppendHorizontalBarChart(html, "Quote Conversion by Month", months.Select(month =>
        {
            var rows = quotes.Where(q => q.QuoteDate.Year == month.Year && q.QuoteDate.Month == month.Month).ToList();
            var converted = rows.Count(IsQuoteConverted);
            var rate = rows.Count == 0 ? 0 : converted / (decimal)rows.Count;
            return new ChartRow(MonthLabel(month), rate * 100m, Percent(rate), $"{converted} converted from {rows.Count} quote(s)");
        }).ToList());

        AppendHorizontalBarChart(html, "Inventory Value Snapshot", new List<ChartRow>
        {
            new("Finished jewellery retail", unsoldJewellery.Sum(i => i.RetailPrice), Money(unsoldJewellery.Sum(i => i.RetailPrice)), $"{unsoldJewellery.Count} unsold jewellery item(s)"),
            new("Finished jewellery cost", unsoldJewellery.Sum(PricingService.CalculateJewelleryCost), Money(unsoldJewellery.Sum(PricingService.CalculateJewelleryCost)), "Recorded material, labour and other cost"),
            new("Loose stones / opals", looseStones.Sum(s => s.EstimatedValue), Money(looseStones.Sum(s => s.EstimatedValue)), $"{looseStones.Count} available stone record(s)"),
            new("Materials", materialValue, Money(materialValue), $"{materials.Count} material record(s)"),
            new("Reserved inventory", reservedValue, Money(reservedValue), "Reserved quote-linked stones and materials")
        });

        AppendHorizontalBarChart(html, "Payments Received by Month", months.Select(month =>
        {
            var rows = payments.Where(p => p.PaymentDate.Year == month.Year && p.PaymentDate.Month == month.Month).ToList();
            return new ChartRow(MonthLabel(month), rows.Sum(p => p.Amount), Money(rows.Sum(p => p.Amount)), $"{rows.Count} payment(s)");
        }).ToList());

        AppendHorizontalBarChart(html, "Outstanding Balances by Job Status", outstandingJobs.GroupBy(j => j.Status)
            .Select(group => new ChartRow(group.Key.ToString(), group.Sum(j => j.BalanceOwing), Money(group.Sum(j => j.BalanceOwing)), $"{group.Count()} job(s)"))
            .OrderByDescending(row => row.Value)
            .ToList());

        html.AppendLine("</section>");
        html.Append(HtmlFooter());
        File.WriteAllText(path, html.ToString());
        return path;
    }

    public static string CreateOutstandingBalancesReport()
    {
        Directory.CreateDirectory(PrintoutFolder);
        using var db = new AppDbContext();
        var jobs = db.Jobs.AsEnumerable().Where(j => j.BalanceOwing > 0 && j.Status != JobStatus.Cancelled).OrderByDescending(j => j.BalanceOwing).ToList();
        var path = Path.Combine(PrintoutFolder, $"OutstandingBalances_{DateTime.Now:yyyyMMdd_HHmmss}.html");
        var html = new StringBuilder();
        html.Append(HtmlHeader("Outstanding Balances Report"));
        html.AppendLine("<section class='card'>");
        html.AppendLine("<h1>Outstanding Balances Report</h1>");
        html.AppendLine(Row("Jobs with balance owing", jobs.Count.ToString(CultureInfo.InvariantCulture)));
        html.AppendLine(Row("Total balance owing", Money(jobs.Sum(j => j.BalanceOwing))));
        AppendOutstandingBalancesTable(html, jobs);
        html.AppendLine("</section>");
        html.Append(HtmlFooter());
        File.WriteAllText(path, html.ToString());
        return path;
    }

    public static string CreateQuoteConversionReport()
    {
        Directory.CreateDirectory(PrintoutFolder);
        using var db = new AppDbContext();
        var quotes = db.CustomQuotes.AsEnumerable().OrderByDescending(q => q.QuoteDate).ToList();
        var path = Path.Combine(PrintoutFolder, $"QuoteConversion_{DateTime.Now:yyyyMMdd_HHmmss}.html");
        var accepted = quotes.Count(q => q.AcceptedOptionId.HasValue || q.LinkedJobId.HasValue || q.Status.Contains("Accepted", StringComparison.OrdinalIgnoreCase));
        var html = new StringBuilder();
        html.Append(HtmlHeader("Quote Conversion Report"));
        html.AppendLine("<section class='card'>");
        html.AppendLine("<h1>Quote Conversion Report</h1>");
        html.AppendLine(Row("Total quotes", quotes.Count.ToString(CultureInfo.InvariantCulture)));
        html.AppendLine(Row("Accepted / converted", accepted.ToString(CultureInfo.InvariantCulture)));
        html.AppendLine(Row("Conversion rate", Percent(quotes.Count == 0 ? 0 : accepted / (decimal)quotes.Count)));
        AppendQuoteConversionTable(html, quotes);
        html.AppendLine("</section>");
        html.Append(HtmlFooter());
        File.WriteAllText(path, html.ToString());
        return path;
    }

    public static string CreateInventoryValueReport()
    {
        Directory.CreateDirectory(PrintoutFolder);
        using var db = new AppDbContext();
        var jewellery = db.JewelleryItems.AsEnumerable().ToList();
        var stones = db.Stones.AsEnumerable().ToList();
        var materials = db.Materials.AsEnumerable().ToList();
        var path = Path.Combine(PrintoutFolder, $"InventoryValue_{DateTime.Now:yyyyMMdd_HHmmss}.html");
        var html = new StringBuilder();
        html.Append(HtmlHeader("Inventory Value Report"));
        html.AppendLine("<section class='card'>");
        html.AppendLine("<h1>Inventory Value Report</h1>");
        AppendStockLifecycleSummary(html);
        AppendInventoryValueTable(html, jewellery, stones, materials);
        html.AppendLine("<h2>Finished jewellery stock</h2>");
        html.AppendLine("<table><tr><th>Stock</th><th>Status</th><th>Lifecycle</th><th>Cost</th><th>Retail</th><th>Potential Profit</th><th>Margin</th></tr>");
        foreach (var item in jewellery.OrderBy(i => i.Status).ThenBy(i => i.StockCode))
        {
            var cost = PricingService.CalculateJewelleryCost(item);
            html.AppendLine($"<tr><td>{Html(item.ToString())}</td><td>{Html(item.Status.ToString())}</td><td>{Html(StockLifecycleService.DescribeStockStatus(item.Status))}</td><td>{Money(cost)}</td><td>{Money(item.RetailPrice)}</td><td>{Money(item.RetailPrice - cost)}</td><td>{Percent(PricingService.CalculateProfitMargin(item.RetailPrice, cost))}</td></tr>");
        }
        html.AppendLine("</table>");
        html.AppendLine("</section>");
        html.Append(HtmlFooter());
        File.WriteAllText(path, html.ToString());
        return path;
    }

    public static string CreateStockAgeingReport()
    {
        Directory.CreateDirectory(PrintoutFolder);
        using var db = new AppDbContext();
        var today = DateTime.Today;
        var jewelleryRows = db.JewelleryItems.AsEnumerable()
            .Where(i => i.Status != StockStatus.Sold)
            .Select(i => new StockAgeRow(
                "Jewellery",
                i.StockCode,
                i.Name,
                i.Status.ToString(),
                StockLifecycleService.DescribeStockStatus(i.Status),
                InventoryAgeDays(i.CreatedAt, today),
                i.RetailPrice,
                i.CreatedAt,
                i.UpdatedAt));
        var stoneRows = db.Stones.AsEnumerable()
            .Where(s => s.Status != StoneStatus.Sold && s.Status != StoneStatus.SetInJewellery)
            .Select(s => new StockAgeRow(
                "Stone",
                s.StoneCode,
                s.StoneType,
                s.Status.ToString(),
                StockLifecycleService.DescribeStoneStatus(s.Status),
                InventoryAgeDays(s.CreatedAt, today),
                s.EstimatedValue,
                s.CreatedAt,
                s.UpdatedAt));
        var rows = jewelleryRows.Concat(stoneRows).OrderByDescending(r => r.AgeDays).ThenBy(r => r.Type).ThenBy(r => r.Code).ToList();
        var slowMoving = rows.Where(r => r.AgeDays >= 180).ToList();
        var path = Path.Combine(PrintoutFolder, $"StockAgeing_{DateTime.Now:yyyyMMdd_HHmmss}.html");

        var html = new StringBuilder();
        html.Append(HtmlHeader("Stock Ageing and Slow-Moving Inventory"));
        html.AppendLine("<section class='card'>");
        html.AppendLine("<h1>Stock Ageing and Slow-Moving Inventory</h1>");
        html.AppendLine($"<p class='small'>Generated {Html(DateTime.Now.ToString("f", CultureInfo.CurrentCulture))}. This report is read-only and does not change stock status.</p>");
        AppendStockLifecycleSummary(html);
        html.AppendLine(Row("Available stock records", rows.Count.ToString(CultureInfo.InvariantCulture)));
        html.AppendLine(Row("Available stock value", Money(rows.Sum(r => r.Value))));
        html.AppendLine(Row("Slow-moving records (180+ days)", slowMoving.Count.ToString(CultureInfo.InvariantCulture)));
        html.AppendLine(Row("Slow-moving value", Money(slowMoving.Sum(r => r.Value))));
        AppendStockAgeBucketTable(html, rows);
        AppendSlowMovingInventoryTable(html, slowMoving);
        html.AppendLine("</section>");
        html.Append(HtmlFooter());
        File.WriteAllText(path, html.ToString());
        return path;
    }

    public static string CreateInventoryDecisionReport()
    {
        Directory.CreateDirectory(PrintoutFolder);
        using var db = new AppDbContext();
        var today = DateTime.Today;
        var jewellery = db.JewelleryItems.AsEnumerable().ToList();
        var stones = db.Stones.AsEnumerable().ToList();
        var materials = db.Materials.AsEnumerable().ToList();
        var materialTransactions = db.MaterialTransactions.AsEnumerable()
            .OrderByDescending(t => t.TransactionDate)
            .ThenByDescending(t => t.Id)
            .ToList();
        var externalDiamonds = db.ExternalDiamonds.AsEnumerable().ToList();
        var suppliers = db.Suppliers.AsEnumerable().ToDictionary(s => s.Id);
        var openPurchaseOrders = db.PurchaseOrders.AsEnumerable()
            .Where(o => o.Status is PurchaseOrderStatus.Draft or PurchaseOrderStatus.Ordered or PurchaseOrderStatus.PartiallyReceived)
            .OrderBy(o => o.ExpectedDeliveryDate ?? DateTime.MaxValue)
            .ToList();
        var openPurchaseOrderIds = openPurchaseOrders.Select(o => o.Id).ToHashSet();
        var openPurchaseOrderItems = db.PurchaseOrderItems.AsEnumerable()
            .Where(i => openPurchaseOrderIds.Contains(i.PurchaseOrderId))
            .ToList();
        var openOrderByMaterial = openPurchaseOrderItems
            .Where(i => i.MaterialId.HasValue)
            .GroupBy(i => i.MaterialId!.Value)
            .ToDictionary(g => g.Key, g => g.Sum(i => i.OutstandingQuantity));
        var lowMaterials = materials
            .Where(m => m.CurrentQuantity <= m.ReorderLevel)
            .OrderBy(m => openOrderByMaterial.ContainsKey(m.Id))
            .ThenBy(m => m.Category)
            .ThenBy(m => m.Name)
            .ToList();
        var unsoldJewellery = jewellery.Where(i => i.Status != StockStatus.Sold).ToList();
        var availableStones = stones.Where(s => s.Status != StoneStatus.Sold && s.Status != StoneStatus.SetInJewellery).ToList();
        var stockAgeRows = unsoldJewellery.Select(i => new StockAgeRow(
                "Jewellery",
                i.StockCode,
                i.Name,
                i.Status.ToString(),
                StockLifecycleService.DescribeStockStatus(i.Status),
                InventoryAgeDays(i.CreatedAt, today),
                i.RetailPrice,
                i.CreatedAt,
                i.UpdatedAt))
            .Concat(availableStones.Select(s => new StockAgeRow(
                "Stone",
                s.StoneCode,
                s.StoneType,
                s.Status.ToString(),
                StockLifecycleService.DescribeStoneStatus(s.Status),
                InventoryAgeDays(s.CreatedAt, today),
                s.EstimatedValue,
                s.CreatedAt,
                s.UpdatedAt)))
            .OrderByDescending(r => r.AgeDays)
            .ThenBy(r => r.Type)
            .ThenBy(r => r.Code)
            .ToList();
        var slowMoving = stockAgeRows.Where(r => r.AgeDays >= 180).ToList();
        var adjustmentTransactions = materialTransactions
            .Where(IsMaterialAdjustment)
            .Take(80)
            .ToList();
        var materialById = materials.ToDictionary(m => m.Id);
        var path = Path.Combine(PrintoutFolder, $"InventoryDecision_{DateTime.Now:yyyyMMdd_HHmmss}.html");

        var html = new StringBuilder();
        html.Append(HtmlHeader("Inventory Reorder Intelligence"));
        html.AppendLine("<section class='card'>");
        html.AppendLine("<h1>Inventory Reorder Intelligence</h1>");
        html.AppendLine($"<p class='small'>Generated {Html(DateTime.Now.ToString("f", CultureInfo.CurrentCulture))}. This V1.93 report is read-only and combines valuation, reorder, stock ageing, supplier-stock and adjustment-audit signals from existing OPALNOVA records.</p>");
        html.AppendLine("<h2>Inventory Decision Snapshot</h2>");
        html.AppendLine("<div class='financial-summary'>");
        html.AppendLine(SummaryTile("Owned stock value", Money(unsoldJewellery.Sum(i => i.RetailPrice) + availableStones.Sum(s => s.EstimatedValue) + materials.Sum(MaterialCurrentValue)), $"{unsoldJewellery.Count + availableStones.Count + materials.Count} records"));
        html.AppendLine(SummaryTile("Low materials", lowMaterials.Count.ToString(CultureInfo.InvariantCulture), $"{lowMaterials.Count(m => openOrderByMaterial.ContainsKey(m.Id))} already on open PO"));
        html.AppendLine(SummaryTile("Slow-moving value", Money(slowMoving.Sum(r => r.Value)), $"{slowMoving.Count} records over 180 days"));
        html.AppendLine("</div>");
        AppendStockLifecycleSummary(html);
        AppendInventoryValuationByCategoryTable(html, jewellery, stones, materials, externalDiamonds);
        AppendInventoryReorderRecommendationTable(html, lowMaterials, openOrderByMaterial, openPurchaseOrders, openPurchaseOrderItems, suppliers);
        AppendInventorySlowMovingGuidanceTable(html, slowMoving);
        AppendSupplierStockDecisionTable(html, externalDiamonds);
        AppendMaterialAdjustmentAuditTable(html, adjustmentTransactions, materialById);
        html.AppendLine("<h2>Recommended Review Order</h2>");
        html.AppendLine("<div class='checkboxes'><p>[ ] Order materials with no open PO coverage</p><p>[ ] Check incoming purchase orders before reordering covered materials</p><p>[ ] Discount, photograph, relist or reserve slow-moving finished stock</p><p>[ ] Confirm supplier-diamond holds/orders before customer promises</p><p>[ ] Review adjustment transactions for unexplained stock movement</p><p>[ ] Update missing costs before relying on valuation totals</p></div>");
        html.AppendLine("</section>");
        html.Append(HtmlFooter());
        File.WriteAllText(path, html.ToString());
        return path;
    }

    public static string CreateReservedInventoryReport()
    {
        Directory.CreateDirectory(PrintoutFolder);
        using var db = new AppDbContext();
        var stoneLinks = db.QuoteOptionStoneLinks.AsEnumerable().ToList();
        var materialLinks = db.QuoteOptionMaterialLinks.AsEnumerable().ToList();
        var path = Path.Combine(PrintoutFolder, $"ReservedInventory_{DateTime.Now:yyyyMMdd_HHmmss}.html");
        var html = new StringBuilder();
        html.Append(HtmlHeader("Reserved Inventory Report"));
        html.AppendLine("<section class='card'>");
        html.AppendLine("<h1>Reserved Inventory Report</h1>");
        AppendStockLifecycleSummary(html);
        AppendReservedInventoryTable(html, stoneLinks, materialLinks);
        html.AppendLine("</section>");
        html.Append(HtmlFooter());
        File.WriteAllText(path, html.ToString());
        return path;
    }

    public static string CreateCustomerFollowUpInsightReport()
    {
        Directory.CreateDirectory(PrintoutFolder);
        using var db = new AppDbContext();
        var tasks = db.BusinessTasks.AsEnumerable().Where(t => t.Status != BusinessTaskStatus.Completed && t.Status != BusinessTaskStatus.Cancelled).OrderBy(t => t.DueDate ?? DateTime.MaxValue).ToList();
        var path = Path.Combine(PrintoutFolder, $"CustomerFollowUps_{DateTime.Now:yyyyMMdd_HHmmss}.html");
        var html = new StringBuilder();
        html.Append(HtmlHeader("Customer Follow-Up Report"));
        html.AppendLine("<section class='card'>");
        html.AppendLine("<h1>Customer Follow-Up Report</h1>");
        html.AppendLine(Row("Open tasks", tasks.Count.ToString(CultureInfo.InvariantCulture)));
        html.AppendLine(Row("Overdue", tasks.Count(t => t.DueDate.HasValue && t.DueDate.Value.Date < DateTime.Today).ToString(CultureInfo.InvariantCulture)));
        AppendFollowUpTable(html, tasks);
        html.AppendLine("</section>");
        html.Append(HtmlFooter());
        File.WriteAllText(path, html.ToString());
        return path;
    }

    public static string CreateOpalStoneStockReport()
    {
        Directory.CreateDirectory(PrintoutFolder);
        using var db = new AppDbContext();
        var stones = db.Stones.AsEnumerable().OrderBy(s => s.Status).ThenBy(s => s.StoneCode).ToList();
        var parcels = db.OpalParcels.AsEnumerable().ToDictionary(p => p.Id, p => p);
        var path = Path.Combine(PrintoutFolder, $"OpalStoneStock_{DateTime.Now:yyyyMMdd_HHmmss}.html");
        var html = new StringBuilder();
        html.Append(HtmlHeader("Opal and Stone Stock Report"));
        html.AppendLine("<section class='card'>");
        html.AppendLine("<h1>Opal and Stone Stock Report</h1>");
        html.AppendLine(Row("Stone records", stones.Count.ToString(CultureInfo.InvariantCulture)));
        html.AppendLine(Row("Loose / available value", Money(stones.Where(s => s.Status == StoneStatus.Loose || s.Status == StoneStatus.Polished || s.Status == StoneStatus.Rough).Sum(s => s.EstimatedValue))));
        html.AppendLine(Row("Reserved stones", stones.Count(s => s.Status == StoneStatus.Reserved || s.Status == StoneStatus.SelectedForDesign).ToString(CultureInfo.InvariantCulture)));
        AppendStockLifecycleSummary(html);
        html.AppendLine("<table><tr><th>Stone</th><th>Type</th><th>Status</th><th>Lifecycle</th><th>Weight</th><th>Dimensions</th><th>Brightness</th><th>Colours</th><th>Value</th><th>Parcel</th></tr>");
        foreach (var stone in stones)
        {
            parcels.TryGetValue(stone.OpalParcelId ?? 0, out var parcel);
            html.AppendLine($"<tr><td>{Html(stone.ToString())}</td><td>{Html(stone.StoneType)}</td><td>{Html(stone.Status.ToString())}</td><td>{Html(StockLifecycleService.DescribeStoneStatus(stone.Status))}</td><td>{Number(stone.WeightCarats)} ct</td><td>{Html(stone.Dimensions ?? string.Empty)}</td><td>{Html(stone.Brightness ?? string.Empty)}</td><td>{Html(stone.MainColours ?? string.Empty)}</td><td>{Money(stone.EstimatedValue)}</td><td>{Html(parcel?.ToString() ?? string.Empty)}</td></tr>");
        }
        html.AppendLine("</table>");
        html.AppendLine("</section>");
        html.Append(HtmlFooter());
        File.WriteAllText(path, html.ToString());
        return path;
    }

    public static string ExportBusinessIntelligenceCsvBundle()
    {
        Directory.CreateDirectory(PrintoutFolder);
        using var db = new AppDbContext();
        var folder = Path.Combine(PrintoutFolder, $"OPALNOVA_BI_CSV_{DateTime.Now:yyyyMMdd_HHmmss}");
        Directory.CreateDirectory(folder);
        WriteCsv(Path.Combine(folder, "sales_summary.csv"), new[] { "Date", "Amount", "CostOfGoods", "Profit", "Location", "Method", "JobId", "CustomerId" }, db.Sales.AsEnumerable().OrderByDescending(s => s.SaleDate).Select(s => new[] { s.SaleDate.ToShortDateString(), s.SaleAmount.ToString(CultureInfo.InvariantCulture), s.CostOfGoods.ToString(CultureInfo.InvariantCulture), s.Profit.ToString(CultureInfo.InvariantCulture), s.SaleLocation.ToString(), s.PaymentMethod.ToString(), s.JobId?.ToString(CultureInfo.InvariantCulture) ?? string.Empty, s.CustomerId?.ToString(CultureInfo.InvariantCulture) ?? string.Empty }));
        WriteCsv(Path.Combine(folder, "outstanding_balances.csv"), new[] { "JobCode", "JobTitle", "Status", "DueDate", "QuoteAmount", "FinalPrice", "DepositPaid", "BalanceOwing", "CustomerId" }, db.Jobs.AsEnumerable().Where(j => j.BalanceOwing > 0).OrderByDescending(j => j.BalanceOwing).Select(j => new[] { j.JobCode, j.JobTitle, j.Status.ToString(), j.DueDate?.ToShortDateString() ?? string.Empty, j.QuoteAmount.ToString(CultureInfo.InvariantCulture), j.FinalPrice.ToString(CultureInfo.InvariantCulture), j.DepositPaid.ToString(CultureInfo.InvariantCulture), j.BalanceOwing.ToString(CultureInfo.InvariantCulture), j.CustomerId?.ToString(CultureInfo.InvariantCulture) ?? string.Empty }));
        WriteCsv(Path.Combine(folder, "quote_conversion.csv"), new[] { "QuoteCode", "Title", "Status", "QuoteDate", "ValidUntil", "AcceptedOptionId", "LinkedJobId", "CustomerId" }, db.CustomQuotes.AsEnumerable().OrderByDescending(q => q.QuoteDate).Select(q => new[] { q.QuoteCode, q.Title, q.Status, q.QuoteDate.ToShortDateString(), q.ValidUntil?.ToShortDateString() ?? string.Empty, q.AcceptedOptionId?.ToString(CultureInfo.InvariantCulture) ?? string.Empty, q.LinkedJobId?.ToString(CultureInfo.InvariantCulture) ?? string.Empty, q.CustomerId?.ToString(CultureInfo.InvariantCulture) ?? string.Empty }));
        WriteCsv(Path.Combine(folder, "inventory_value.csv"), new[] { "Type", "Code", "Name", "Status", "Quantity", "Unit", "CostOrValue", "Retail" }, db.JewelleryItems.AsEnumerable().Select(i => new[] { "Jewellery", i.StockCode, i.Name, i.Status.ToString(), "1", "Piece", PricingService.CalculateJewelleryCost(i).ToString(CultureInfo.InvariantCulture), i.RetailPrice.ToString(CultureInfo.InvariantCulture) }).Concat(db.Stones.AsEnumerable().Select(s => new[] { "Stone", s.StoneCode, s.StoneType, s.Status.ToString(), s.WeightCarats.ToString(CultureInfo.InvariantCulture), "Carats", s.EstimatedValue.ToString(CultureInfo.InvariantCulture), s.EstimatedValue.ToString(CultureInfo.InvariantCulture) })).Concat(db.Materials.AsEnumerable().Select(m => new[] { "Material", m.MaterialCode, m.Name, m.Category.ToString(), m.CurrentQuantity.ToString(CultureInfo.InvariantCulture), m.UnitType.ToString(), (m.CurrentQuantity * m.PurchaseCost).ToString(CultureInfo.InvariantCulture), string.Empty })));
        WriteCsv(Path.Combine(folder, "reserved_inventory.csv"), new[] { "Type", "Code", "Name", "Quantity", "Unit", "Value", "ReservationStatus", "QuoteOptionId" }, db.QuoteOptionStoneLinks.AsEnumerable().Select(l => new[] { "Stone", l.StoneCodeSnapshot, l.DescriptionSnapshot, "1", "Stone", l.UnitCost.ToString(CultureInfo.InvariantCulture), l.ReservationStatus, l.QuoteOptionId.ToString(CultureInfo.InvariantCulture) }).Concat(db.QuoteOptionMaterialLinks.AsEnumerable().Select(l => new[] { "Material", l.MaterialCodeSnapshot, l.MaterialNameSnapshot, l.Quantity.ToString(CultureInfo.InvariantCulture), l.UnitTypeSnapshot, l.LineCost.ToString(CultureInfo.InvariantCulture), l.ReservationStatus, l.QuoteOptionId.ToString(CultureInfo.InvariantCulture) })));
        var index = Path.Combine(folder, "OPEN_ME_BUSINESS_INTELLIGENCE_EXPORT.html");
        var html = new StringBuilder();
        html.Append(HtmlHeader("Business Intelligence CSV Export"));
        html.AppendLine("<section class='card'>");
        html.AppendLine("<h1>Business Intelligence CSV Export</h1>");
        html.AppendLine("<p>The CSV files have been exported into this folder for spreadsheet review.</p>");
        html.AppendLine("<ul><li>sales_summary.csv</li><li>outstanding_balances.csv</li><li>quote_conversion.csv</li><li>inventory_value.csv</li><li>reserved_inventory.csv</li></ul>");
        html.AppendLine("</section>");
        html.Append(HtmlFooter());
        File.WriteAllText(index, html.ToString());
        return index;
    }

    public static string ExportBusinessIntelligenceExcelWorkbook()
    {
        Directory.CreateDirectory(PrintoutFolder);
        using var db = new AppDbContext();
        var folder = Path.Combine(PrintoutFolder, $"OPALNOVA_BI_EXCEL_{DateTime.Now:yyyyMMdd_HHmmss}");
        Directory.CreateDirectory(folder);
        var workbookPath = Path.Combine(folder, "OPALNOVA_Business_Intelligence.xls");

        var sales = db.Sales.AsEnumerable().OrderByDescending(s => s.SaleDate).ToList();
        var jobs = db.Jobs.AsEnumerable().OrderByDescending(j => j.UpdatedAt).ToList();
        var quotes = db.CustomQuotes.AsEnumerable().OrderByDescending(q => q.QuoteDate).ToList();
        var jewellery = db.JewelleryItems.AsEnumerable().OrderBy(i => i.Status).ThenBy(i => i.StockCode).ToList();
        var stones = db.Stones.AsEnumerable().OrderBy(s => s.Status).ThenBy(s => s.StoneCode).ToList();
        var materials = db.Materials.AsEnumerable().OrderBy(m => m.Category).ThenBy(m => m.Name).ToList();
        var stoneLinks = db.QuoteOptionStoneLinks.AsEnumerable().OrderBy(l => l.ReservationStatus).ThenBy(l => l.StoneCodeSnapshot).ToList();
        var materialLinks = db.QuoteOptionMaterialLinks.AsEnumerable().OrderBy(l => l.ReservationStatus).ThenBy(l => l.MaterialNameSnapshot).ToList();
        var tasks = db.BusinessTasks.AsEnumerable().OrderBy(t => t.Status).ThenBy(t => t.DueDate ?? DateTime.MaxValue).ToList();
        var externalDiamonds = db.ExternalDiamonds.AsEnumerable().OrderBy(d => d.Status).ThenBy(d => d.CertificateNumber).ToList();

        var activeJobs = jobs.Where(j => j.Status != JobStatus.Completed && j.Status != JobStatus.Cancelled).ToList();
        var balances = jobs.Where(j => j.BalanceOwing > 0m && j.Status != JobStatus.Cancelled).ToList();
        var acceptedQuotes = quotes.Count(q => q.AcceptedOptionId.HasValue || q.Status.Contains("Accepted", StringComparison.OrdinalIgnoreCase) || q.LinkedJobId.HasValue);
        var reservedStoneValue = stoneLinks.Where(l => l.ReservationStatus.Equals("Reserved", StringComparison.OrdinalIgnoreCase)).Sum(l => l.UnitCost);
        var reservedMaterialValue = materialLinks.Where(l => l.ReservationStatus.Equals("Reserved", StringComparison.OrdinalIgnoreCase)).Sum(l => l.LineCost);
        var openTasks = tasks.Where(t => t.Status != BusinessTaskStatus.Completed && t.Status != BusinessTaskStatus.Cancelled).ToList();

        var sheets = new List<ExcelWorksheet>
        {
            new("Summary", new[] { "Metric", "Value" }, new[]
            {
                Row("Generated", DateTime.Now),
                Row("Sales total", sales.Sum(s => s.SaleAmount)),
                Row("Profit total", sales.Sum(s => s.Profit)),
                Row("Outstanding balances", balances.Sum(j => j.BalanceOwing)),
                Row("Quote count", quotes.Count),
                Row("Accepted / converted quotes", acceptedQuotes),
                Row("Quote conversion rate", quotes.Count == 0 ? 0m : acceptedQuotes / (decimal)quotes.Count),
                Row("Active jobs", activeJobs.Count),
                Row("Reserved inventory value", reservedStoneValue + reservedMaterialValue),
                Row("Open tasks", openTasks.Count),
                Row("External diamonds", externalDiamonds.Count)
            }),
            new("Sales", new[] { "Date", "Amount", "CostOfGoods", "Profit", "Location", "Method", "JobId", "CustomerId", "Notes" },
                sales.Select(s => Row(s.SaleDate, s.SaleAmount, s.CostOfGoods, s.Profit, s.SaleLocation, s.PaymentMethod, s.JobId, s.CustomerId, s.Notes))),
            new("Outstanding Balances", new[] { "JobCode", "JobTitle", "Status", "DueDate", "QuoteAmount", "FinalPrice", "DepositPaid", "BalanceOwing", "CustomerId" },
                balances.OrderByDescending(j => j.BalanceOwing).Select(j => Row(j.JobCode, j.JobTitle, j.Status, j.DueDate, j.QuoteAmount, j.FinalPrice, j.DepositPaid, j.BalanceOwing, j.CustomerId))),
            new("Quotes", new[] { "QuoteCode", "Title", "Status", "ProposalStatus", "QuoteDate", "ValidUntil", "ProposalSentAt", "AcceptedOptionId", "LinkedJobId", "CustomerId" },
                quotes.Select(q => Row(q.QuoteCode, q.Title, q.Status, q.ProposalStatus, q.QuoteDate, q.ValidUntil, q.ProposalSentAt, q.AcceptedOptionId, q.LinkedJobId, q.CustomerId))),
            new("Inventory Value", new[] { "Type", "Code", "Name", "Status", "Quantity", "Unit", "CostOrValue", "Retail" },
                jewellery.Select(i => Row("Jewellery", i.StockCode, i.Name, i.Status, 1, "Piece", PricingService.CalculateJewelleryCost(i), i.RetailPrice))
                    .Concat(stones.Select(s => Row("Stone", s.StoneCode, s.StoneType, s.Status, s.WeightCarats, "Carats", s.EstimatedValue, s.EstimatedValue)))
                    .Concat(materials.Select(m => Row("Material", m.MaterialCode, m.Name, m.Category, m.CurrentQuantity, m.UnitType, m.CurrentQuantity * m.PurchaseCost, string.Empty)))),
            new("Reserved Inventory", new[] { "Type", "Code", "Name", "Quantity", "Unit", "Value", "ReservationStatus", "QuoteOptionId" },
                stoneLinks.Select(l => Row("Stone", l.StoneCodeSnapshot, l.DescriptionSnapshot, 1, "Stone", l.UnitCost, l.ReservationStatus, l.QuoteOptionId))
                    .Concat(materialLinks.Select(l => Row("Material", l.MaterialCodeSnapshot, l.MaterialNameSnapshot, l.Quantity, l.UnitTypeSnapshot, l.LineCost, l.ReservationStatus, l.QuoteOptionId)))),
            new("Tasks", new[] { "TaskCode", "Title", "Category", "Priority", "Status", "DueDate", "CustomerId", "JobId", "Description" },
                tasks.Select(t => Row(t.TaskCode, t.Title, t.Category, t.Priority, t.Status, t.DueDate, t.CustomerId, t.JobId, t.Description))),
            new("External Diamonds", new[] { "Source", "SupplierDiamondId", "Status", "Shape", "Carat", "Color", "Clarity", "Lab", "Certificate", "SupplierPrice", "Currency", "EstimatedRetail", "HoldExpiresAt", "ExpectedArrival", "ReceivedAt" },
                externalDiamonds.Select(d => Row(d.SourceSystem, d.SupplierDiamondId, d.Status, d.Shape, d.Carat, d.Color, d.Clarity, d.Lab, d.CertificateNumber, d.SupplierPrice, d.Currency, d.EstimatedRetailPrice, d.HoldExpiresAt, d.ExpectedArrivalDate, d.ReceivedAt)))
        };

        WriteExcelWorkbook(workbookPath, sheets);

        var index = Path.Combine(folder, "OPEN_ME_BUSINESS_INTELLIGENCE_EXCEL_EXPORT.html");
        var html = new StringBuilder();
        html.Append(HtmlHeader("Business Intelligence Excel Export"));
        html.AppendLine("<section class='card'>");
        html.AppendLine("<h1>Business Intelligence Excel Export</h1>");
        html.AppendLine("<p>An Excel-compatible workbook has been exported for spreadsheet review.</p>");
        html.AppendLine($"<p><a href='{Html(Path.GetFileName(workbookPath))}'>Open OPALNOVA_Business_Intelligence.xls</a></p>");
        html.AppendLine("<p class='small'>Workbook sheets: Summary, Sales, Outstanding Balances, Quotes, Inventory Value, Reserved Inventory, Tasks, External Diamonds.</p>");
        html.AppendLine("</section>");
        html.Append(HtmlFooter());
        File.WriteAllText(index, html.ToString());
        return index;
    }

    private sealed record ExcelWorksheet(string Name, string[] Headers, IEnumerable<object?[]> Rows);

    private static object?[] Row(params object?[] values) => values;

    private static void WriteExcelWorkbook(string path, IEnumerable<ExcelWorksheet> worksheets)
    {
        var xml = new StringBuilder();
        xml.AppendLine("<?xml version=\"1.0\"?>");
        xml.AppendLine("<?mso-application progid=\"Excel.Sheet\"?>");
        xml.AppendLine("<Workbook xmlns=\"urn:schemas-microsoft-com:office:spreadsheet\" xmlns:ss=\"urn:schemas-microsoft-com:office:spreadsheet\">");
        xml.AppendLine("<Styles><Style ss:ID=\"Header\"><Font ss:Bold=\"1\"/><Interior ss:Color=\"#D9EAF7\" ss:Pattern=\"Solid\"/></Style></Styles>");
        foreach (var sheet in worksheets)
            AppendWorksheet(xml, sheet);
        xml.AppendLine("</Workbook>");
        File.WriteAllText(path, xml.ToString(), Encoding.UTF8);
    }

    private static void AppendWorksheet(StringBuilder xml, ExcelWorksheet sheet)
    {
        xml.AppendLine($"<Worksheet ss:Name=\"{Html(SanitizeWorksheetName(sheet.Name))}\"><Table>");
        xml.Append("<Row>");
        foreach (var header in sheet.Headers)
            xml.Append($"<Cell ss:StyleID=\"Header\"><Data ss:Type=\"String\">{Html(header)}</Data></Cell>");
        xml.AppendLine("</Row>");

        foreach (var row in sheet.Rows)
        {
            xml.Append("<Row>");
            foreach (var value in row)
                AppendExcelCell(xml, value);
            xml.AppendLine("</Row>");
        }

        xml.AppendLine("</Table></Worksheet>");
    }

    private static void AppendExcelCell(StringBuilder xml, object? value)
    {
        if (value == null)
        {
            xml.Append("<Cell><Data ss:Type=\"String\"></Data></Cell>");
            return;
        }

        var type = Nullable.GetUnderlyingType(value.GetType()) ?? value.GetType();
        if (type.IsEnum)
        {
            xml.Append($"<Cell><Data ss:Type=\"String\">{Html(value.ToString() ?? string.Empty)}</Data></Cell>");
            return;
        }

        switch (value)
        {
            case decimal decimalValue:
                xml.Append($"<Cell><Data ss:Type=\"Number\">{decimalValue.ToString(CultureInfo.InvariantCulture)}</Data></Cell>");
                break;
            case double doubleValue:
                xml.Append($"<Cell><Data ss:Type=\"Number\">{doubleValue.ToString(CultureInfo.InvariantCulture)}</Data></Cell>");
                break;
            case float floatValue:
                xml.Append($"<Cell><Data ss:Type=\"Number\">{floatValue.ToString(CultureInfo.InvariantCulture)}</Data></Cell>");
                break;
            case int or long or short or byte:
                xml.Append($"<Cell><Data ss:Type=\"Number\">{Convert.ToString(value, CultureInfo.InvariantCulture)}</Data></Cell>");
                break;
            case DateTime dateValue:
                var dateText = dateValue.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                xml.Append($"<Cell><Data ss:Type=\"String\">{Html(dateText)}</Data></Cell>");
                break;
            case bool boolValue:
                xml.Append($"<Cell><Data ss:Type=\"String\">{(boolValue ? "TRUE" : "FALSE")}</Data></Cell>");
                break;
            default:
                xml.Append($"<Cell><Data ss:Type=\"String\">{Html(Convert.ToString(value, CultureInfo.CurrentCulture) ?? string.Empty)}</Data></Cell>");
                break;
        }
    }

    private static string SanitizeWorksheetName(string name)
    {
        var cleaned = new string(name.Select(ch => ch is ':' or '\\' or '/' or '?' or '*' or '[' or ']' ? ' ' : ch).ToArray()).Trim();
        if (string.IsNullOrWhiteSpace(cleaned))
            cleaned = "Sheet";
        return cleaned.Length > 31 ? cleaned[..31] : cleaned;
    }

    private static void AppendSalesSummaryTable(StringBuilder html, string title, List<Sale> sales)
    {
        html.AppendLine($"<h2>{Html(title)}</h2>");
        html.AppendLine("<table><tr><th>Location</th><th>Sales</th><th>Cost</th><th>Profit</th><th>Margin</th><th>Count</th></tr>");
        foreach (var group in sales.GroupBy(s => s.SaleLocation).OrderByDescending(g => g.Sum(s => s.SaleAmount)))
        {
            var revenue = group.Sum(s => s.SaleAmount);
            var cost = group.Sum(s => s.CostOfGoods);
            html.AppendLine($"<tr><td>{Html(group.Key.ToString())}</td><td>{Money(revenue)}</td><td>{Money(cost)}</td><td>{Money(revenue - cost)}</td><td>{Percent(PricingService.CalculateProfitMargin(revenue, cost))}</td><td>{group.Count()}</td></tr>");
        }
        html.AppendLine("</table>");
    }

    private static void AppendOutstandingBalancesTable(StringBuilder html, List<Job> jobs)
    {
        html.AppendLine("<h2>Outstanding Balances</h2>");
        html.AppendLine("<table><tr><th>Job</th><th>Status</th><th>Due</th><th>Price</th><th>Deposit</th><th>Balance</th></tr>");
        foreach (var job in jobs)
        {
            var price = job.FinalPrice > 0 ? job.FinalPrice : job.QuoteAmount;
            html.AppendLine($"<tr><td>{Html(job.ToString())}</td><td>{Html(job.Status.ToString())}</td><td>{Html(job.DueDate?.ToShortDateString() ?? string.Empty)}</td><td>{Money(price)}</td><td>{Money(job.DepositPaid)}</td><td>{Money(job.BalanceOwing)}</td></tr>");
        }
        html.AppendLine("</table>");
    }

    private static void AppendQuoteConversionTable(StringBuilder html, List<CustomQuote> quotes)
    {
        html.AppendLine("<h2>Quote Conversion</h2>");
        html.AppendLine("<table><tr><th>Quote</th><th>Status</th><th>Date</th><th>Valid Until</th><th>Accepted Option</th><th>Linked Job</th></tr>");
        foreach (var quote in quotes.OrderByDescending(q => q.QuoteDate).Take(40))
            html.AppendLine($"<tr><td>{Html(quote.ToString())}</td><td>{Html(quote.Status)}</td><td>{Html(quote.QuoteDate.ToShortDateString())}</td><td>{Html(quote.ValidUntil?.ToShortDateString() ?? string.Empty)}</td><td>{Html(quote.AcceptedOptionId?.ToString(CultureInfo.InvariantCulture) ?? string.Empty)}</td><td>{Html(quote.LinkedJobId?.ToString(CultureInfo.InvariantCulture) ?? string.Empty)}</td></tr>");
        html.AppendLine("</table>");
    }

    private sealed record ProfitCategoryKey(string Category, string Source);

    private static void AppendProductCategoryProfitTable(StringBuilder html, string title, List<Sale> sales, Dictionary<int, JewelleryItem> jewelleryById, Dictionary<int, Job> jobsById)
    {
        html.AppendLine($"<h2>{Html(title)}</h2>");
        if (sales.Count == 0)
        {
            html.AppendLine("<p>No recorded sales are available for profitability grouping yet.</p>");
            return;
        }

        html.AppendLine("<table><tr><th>Category</th><th>Source</th><th>Sales</th><th>Revenue</th><th>Cost</th><th>Profit</th><th>Margin</th><th>Average Sale</th></tr>");
        foreach (var group in sales.GroupBy(s => ClassifySaleCategory(s, jewelleryById, jobsById)).OrderByDescending(g => g.Sum(s => s.Profit)))
        {
            var revenue = group.Sum(s => s.SaleAmount);
            var cost = group.Sum(s => s.CostOfGoods);
            var count = group.Count();
            html.AppendLine($"<tr><td>{Html(group.Key.Category)}</td><td>{Html(group.Key.Source)}</td><td>{count}</td><td>{Money(revenue)}</td><td>{Money(cost)}</td><td>{Money(revenue - cost)}</td><td>{Percent(PricingService.CalculateProfitMargin(revenue, cost))}</td><td>{Money(count == 0 ? 0 : revenue / count)}</td></tr>");
        }
        html.AppendLine("</table>");
    }

    private static void AppendRealisedJobTypeProfitTable(StringBuilder html, List<Sale> jobLinkedSales, Dictionary<int, Job> jobsById)
    {
        html.AppendLine("<h2>Recorded Profit by Job Type</h2>");
        if (jobLinkedSales.Count == 0)
        {
            html.AppendLine("<p>No sales are currently linked to job records. Use Payment & Collection when handing over custom work so job-type profit can be measured.</p>");
            return;
        }

        html.AppendLine("<table><tr><th>Job Type</th><th>Sales</th><th>Revenue</th><th>Cost</th><th>Profit</th><th>Margin</th><th>Average Sale</th></tr>");
        foreach (var group in jobLinkedSales.GroupBy(s => jobsById[s.JobId!.Value].Type).OrderByDescending(g => g.Sum(s => s.Profit)))
        {
            var revenue = group.Sum(s => s.SaleAmount);
            var cost = group.Sum(s => s.CostOfGoods);
            var count = group.Count();
            html.AppendLine($"<tr><td>{Html(group.Key.ToString())}</td><td>{count}</td><td>{Money(revenue)}</td><td>{Money(cost)}</td><td>{Money(revenue - cost)}</td><td>{Percent(PricingService.CalculateProfitMargin(revenue, cost))}</td><td>{Money(count == 0 ? 0 : revenue / count)}</td></tr>");
        }
        html.AppendLine("</table>");
    }

    private static void AppendEstimatedJobTypeProfitTable(StringBuilder html, List<Job> jobs)
    {
        html.AppendLine("<h2>Estimated Job Profit by Job Type</h2>");
        if (jobs.Count == 0)
        {
            html.AppendLine("<p>No non-cancelled jobs are available for estimated job profitability yet.</p>");
            return;
        }

        html.AppendLine("<table><tr><th>Job Type</th><th>Jobs</th><th>Completed</th><th>Open</th><th>Quoted / Final Revenue</th><th>Estimated Cost</th><th>Estimated Profit</th><th>Margin</th><th>Average Profit</th></tr>");
        foreach (var group in jobs.GroupBy(j => j.Type).OrderByDescending(g => g.Sum(PricingService.CalculateJobProfit)))
        {
            var revenue = group.Sum(JobPrice);
            var cost = group.Sum(PricingService.CalculateJobCost);
            var profit = revenue - cost;
            var count = group.Count();
            html.AppendLine($"<tr><td>{Html(group.Key.ToString())}</td><td>{count}</td><td>{group.Count(j => j.Status == JobStatus.Completed)}</td><td>{group.Count(j => j.Status != JobStatus.Completed)}</td><td>{Money(revenue)}</td><td>{Money(cost)}</td><td>{Money(profit)}</td><td>{Percent(PricingService.CalculateProfitMargin(revenue, cost))}</td><td>{Money(count == 0 ? 0 : profit / count)}</td></tr>");
        }
        html.AppendLine("</table>");
    }

    private static void AppendProfitabilityDataQualityTable(StringBuilder html, List<Sale> sales, List<Job> jobs, Dictionary<int, JewelleryItem> jewelleryById, Dictionary<int, Job> jobsById)
    {
        var rows = new (string Check, int Count, string Reason)[]
        {
            ("Unlinked sales", sales.Count(s => !s.JobId.HasValue && !s.JewelleryItemId.HasValue), "These cannot be assigned to a stock category or job type."),
            ("Missing stock links", sales.Count(s => s.JewelleryItemId.HasValue && !jewelleryById.ContainsKey(s.JewelleryItemId.Value)), "These sales point to stock records that are no longer found."),
            ("Missing job links", sales.Count(s => s.JobId.HasValue && !jobsById.ContainsKey(s.JobId.Value)), "These sales point to job records that are no longer found."),
            ("Sales with no cost of goods", sales.Count(s => s.SaleAmount > 0 && s.CostOfGoods <= 0), "Profit is overstated if material, stock or job cost is missing."),
            ("Jobs with no price", jobs.Count(j => j.Status != JobStatus.Cancelled && JobPrice(j) <= 0), "Estimated job profit needs a quote amount or final price."),
            ("Priced jobs with no recorded cost", jobs.Count(j => j.Status != JobStatus.Cancelled && JobPrice(j) > 0 && PricingService.CalculateJobCost(j) <= 0), "Estimated job profit is unreliable if labour/material costs are missing.")
        };

        html.AppendLine("<h2>Profit Reporting Data Checks</h2>");
        html.AppendLine("<table><tr><th>Check</th><th>Count</th><th>Why It Matters</th></tr>");
        foreach (var row in rows)
            html.AppendLine($"<tr><td>{Html(row.Check)}</td><td>{row.Count}</td><td>{Html(row.Reason)}</td></tr>");
        html.AppendLine("</table>");
    }

    private static void AppendRecentProfitSalesTable(StringBuilder html, List<Sale> recentSales, Dictionary<int, JewelleryItem> jewelleryById, Dictionary<int, Job> jobsById)
    {
        html.AppendLine("<h2>Recent Sales Profit Detail</h2>");
        if (recentSales.Count == 0)
        {
            html.AppendLine("<p>No recent sales have been recorded yet.</p>");
            return;
        }

        html.AppendLine("<table><tr><th>Date</th><th>Sale</th><th>Category</th><th>Source</th><th>Revenue</th><th>Cost</th><th>Profit</th><th>Margin</th></tr>");
        foreach (var sale in recentSales)
        {
            var category = ClassifySaleCategory(sale, jewelleryById, jobsById);
            html.AppendLine($"<tr><td>{Html(sale.SaleDate.ToShortDateString())}</td><td>{Html(DescribeSaleSource(sale, jewelleryById, jobsById))}</td><td>{Html(category.Category)}</td><td>{Html(category.Source)}</td><td>{Money(sale.SaleAmount)}</td><td>{Money(sale.CostOfGoods)}</td><td>{Money(sale.Profit)}</td><td>{Percent(PricingService.CalculateProfitMargin(sale.SaleAmount, sale.CostOfGoods))}</td></tr>");
        }
        html.AppendLine("</table>");
    }

    private sealed record TaxPeriod(string Name, DateTime Start, DateTime End);

    private static void AppendTaxPeriodSummaryTable(StringBuilder html, TaxPeriod[] periods, List<Sale> sales, List<Payment> payments, List<Job> jobs, BusinessSettings settings)
    {
        html.AppendLine("<h2>Tax Period Summary</h2>");
        html.AppendLine("<table><tr><th>Period</th><th>Date Range</th><th>Sales</th><th>Estimated Tax</th><th>Net Sales Ex Tax</th><th>Cost</th><th>Profit</th><th>Payments Received</th><th>Current Job Balances</th><th>Sales Count</th><th>Payment Count</th></tr>");
        foreach (var period in periods)
        {
            var periodSales = SalesInPeriod(sales, period).ToList();
            var periodPayments = PaymentsInPeriod(payments, period).ToList();
            var periodJobs = jobs.Where(j => j.DateReceived.Date >= period.Start.Date && j.DateReceived.Date <= period.End.Date && j.Status != JobStatus.Cancelled).ToList();
            var salesTotal = periodSales.Sum(s => s.SaleAmount);
            var tax = periodSales.Sum(s => TaxComponent(s.SaleAmount, settings));
            var cost = periodSales.Sum(s => s.CostOfGoods);
            html.AppendLine($"<tr><td>{Html(period.Name)}</td><td>{Html($"{period.Start:d} to {period.End:d}")}</td><td>{Money(salesTotal)}</td><td>{Money(tax)}</td><td>{Money(salesTotal - tax)}</td><td>{Money(cost)}</td><td>{Money(periodSales.Sum(s => s.Profit))}</td><td>{Money(periodPayments.Sum(p => p.Amount))}</td><td>{Money(periodJobs.Sum(j => j.BalanceOwing))}</td><td>{periodSales.Count}</td><td>{periodPayments.Count}</td></tr>");
        }
        html.AppendLine("</table>");
        html.AppendLine("<p class='small'>Current job balances are the balances currently recorded on jobs received inside each period; OPALNOVA does not reconstruct historical balances.</p>");
    }

    private static void AppendTaxSalesLocationTable(StringBuilder html, string title, List<Sale> sales, BusinessSettings settings)
    {
        html.AppendLine($"<h2>{Html(title)}</h2>");
        if (sales.Count == 0)
        {
            html.AppendLine("<p>No sales are available for this period.</p>");
            return;
        }

        html.AppendLine("<table><tr><th>Location</th><th>Sales</th><th>Estimated Tax</th><th>Net Sales Ex Tax</th><th>Cost</th><th>Profit</th><th>Count</th></tr>");
        foreach (var group in sales.GroupBy(s => s.SaleLocation).OrderByDescending(g => g.Sum(s => s.SaleAmount)))
        {
            var salesTotal = group.Sum(s => s.SaleAmount);
            var tax = group.Sum(s => TaxComponent(s.SaleAmount, settings));
            var cost = group.Sum(s => s.CostOfGoods);
            html.AppendLine($"<tr><td>{Html(group.Key.ToString())}</td><td>{Money(salesTotal)}</td><td>{Money(tax)}</td><td>{Money(salesTotal - tax)}</td><td>{Money(cost)}</td><td>{Money(salesTotal - cost)}</td><td>{group.Count()}</td></tr>");
        }
        html.AppendLine("</table>");
    }

    private static void AppendTaxPaymentMethodTable(StringBuilder html, string title, List<Payment> payments)
    {
        html.AppendLine($"<h2>{Html(title)}</h2>");
        if (payments.Count == 0)
        {
            html.AppendLine("<p>No payments are available for this period.</p>");
            return;
        }

        html.AppendLine("<table><tr><th>Method</th><th>Payments</th><th>Total Received</th><th>Linked to Sales</th><th>Linked to Jobs</th><th>Unlinked</th></tr>");
        foreach (var group in payments.GroupBy(p => p.Method).OrderByDescending(g => g.Sum(p => p.Amount)))
            html.AppendLine($"<tr><td>{Html(group.Key.ToString())}</td><td>{group.Count()}</td><td>{Money(group.Sum(p => p.Amount))}</td><td>{group.Count(p => p.SaleId.HasValue)}</td><td>{group.Count(p => p.JobId.HasValue)}</td><td>{group.Count(p => !p.SaleId.HasValue && !p.JobId.HasValue)}</td></tr>");
        html.AppendLine("</table>");
    }

    private static void AppendTaxDataQualityTable(StringBuilder html, List<Sale> sales, List<Payment> payments, List<Job> jobs, Dictionary<int, Sale> salesById, Dictionary<int, Job> jobsById)
    {
        var rows = new (string Check, int Count, string Reason)[]
        {
            ("Sales with zero amount", sales.Count(s => s.SaleAmount <= 0), "Tax and revenue summaries depend on positive sale totals."),
            ("Sales with no customer", sales.Count(s => !s.CustomerId.HasValue), "Customer linkage is useful for invoice, receipt and bookkeeping review."),
            ("Payments with no sale or job link", payments.Count(p => !p.SaleId.HasValue && !p.JobId.HasValue), "Unlinked payments can be hard to reconcile to sales, invoices or deposits."),
            ("Payments linked to missing sale", payments.Count(p => p.SaleId.HasValue && !salesById.ContainsKey(p.SaleId.Value)), "These payments point to sale records that are no longer found."),
            ("Payments linked to missing job", payments.Count(p => p.JobId.HasValue && !jobsById.ContainsKey(p.JobId.Value)), "These payments point to job records that are no longer found."),
            ("Open balances on active jobs", jobs.Count(j => j.BalanceOwing > 0 && j.Status != JobStatus.Cancelled), "Outstanding balances may need collection before final handover or bookkeeping close.")
        };

        html.AppendLine("<h2>Tax / Payment Data Checks</h2>");
        html.AppendLine("<table><tr><th>Check</th><th>Count</th><th>Why It Matters</th></tr>");
        foreach (var row in rows)
            html.AppendLine($"<tr><td>{Html(row.Check)}</td><td>{row.Count}</td><td>{Html(row.Reason)}</td></tr>");
        html.AppendLine("</table>");
    }

    private static void AppendRecentTaxSalesTable(StringBuilder html, List<Sale> sales, BusinessSettings settings)
    {
        html.AppendLine("<h2>Financial Year Sales Detail</h2>");
        if (sales.Count == 0)
        {
            html.AppendLine("<p>No sales are available for the financial year to date.</p>");
            return;
        }

        html.AppendLine("<table><tr><th>Date</th><th>Sale</th><th>Location</th><th>Payment Method</th><th>Gross Sales</th><th>Estimated Tax</th><th>Net Ex Tax</th><th>Cost</th><th>Profit</th></tr>");
        foreach (var sale in sales)
        {
            var tax = TaxComponent(sale.SaleAmount, settings);
            html.AppendLine($"<tr><td>{Html(sale.SaleDate.ToShortDateString())}</td><td>{Html($"Sale #{sale.Id}")}</td><td>{Html(sale.SaleLocation.ToString())}</td><td>{Html(sale.PaymentMethod.ToString())}</td><td>{Money(sale.SaleAmount)}</td><td>{Money(tax)}</td><td>{Money(sale.SaleAmount - tax)}</td><td>{Money(sale.CostOfGoods)}</td><td>{Money(sale.Profit)}</td></tr>");
        }
        html.AppendLine("</table>");
        if (sales.Count >= 80)
            html.AppendLine("<p class='small'>Showing the latest 80 financial-year sales.</p>");
    }

    private sealed record ProductionChecklistItem(string Check, string Status, string Detail);

    private sealed record ChartRow(string Label, decimal Value, string DisplayValue, string Detail);

    private static List<ProductionChecklistItem> BuildProductionStageChecklist(
        Job job,
        Customer? customer,
        CustomQuote? quote,
        QuoteOption? acceptedOption,
        List<QuoteOptionMaterialLink> materialLinks,
        List<QuoteOptionStoneLink> stoneLinks,
        List<QuoteOptionExternalDiamondLink> diamondLinks,
        List<Payment> payments,
        List<BusinessTask> tasks,
        List<PhotoRecord> photos,
        decimal balance)
    {
        var items = new List<ProductionChecklistItem>
        {
            new("Customer linked", customer == null ? "Review" : "Ready", customer == null ? "Link the job to a customer before production communication." : $"{customer.FullName} is linked."),
            new("Customer contact", HasCustomerContact(customer) ? "Ready" : "Review", HasCustomerContact(customer) ? "At least one phone or email is recorded." : "Add phone or email before sending updates."),
            new("Due date", job.DueDate.HasValue ? (job.DueDate.Value.Date < DateTime.Today && job.Status is not JobStatus.Completed and not JobStatus.Cancelled ? "Waiting" : "Ready") : "Review", ProductionDueGuidance(job)),
            new("Design notes", string.IsNullOrWhiteSpace(job.DesignNotes) ? "Review" : "Ready", string.IsNullOrWhiteSpace(job.DesignNotes) ? "Add enough design/repair detail for bench work." : "Design notes are recorded."),
            new("Linked quote", quote == null ? "Review" : "Ready", quote == null ? "No linked custom quote was found." : $"{quote.QuoteCode} - {quote.Status}, proposal {quote.ProposalStatus}."),
            new("Accepted option", acceptedOption == null ? "Review" : "Ready", acceptedOption == null ? "No accepted or recommended quote option found." : $"{acceptedOption.OptionName} at {Money(acceptedOption.TotalPrice)}."),
            new("Payment position", balance > 0 && job.Status is JobStatus.ReadyForPickup or JobStatus.ReadyToShip or JobStatus.Completed ? "Waiting" : "Ready", balance <= 0 ? "No balance currently owing from recorded payments." : $"Balance still owing: {Money(balance)}."),
            new("Linked payments", payments.Count > 0 || job.DepositPaid > 0 ? "Ready" : "Review", payments.Count > 0 ? $"{payments.Count} linked payment record(s)." : job.DepositPaid > 0 ? "Deposit is recorded on the job." : "No linked payment records found."),
            new("Job photos/files", photos.Count > 0 ? "Ready" : "Review", photos.Count > 0 ? $"{photos.Count} job photo/file record(s) linked." : "Attach design, before-work, progress or completion photos where useful."),
            new("Open tasks", tasks.Count == 0 ? "Ready" : tasks.Any(t => t.IsOverdue) ? "Waiting" : "Review", tasks.Count == 0 ? "No open linked tasks." : $"{tasks.Count} open linked task(s), {tasks.Count(t => t.IsOverdue)} overdue.")
        };

        AddStageSpecificChecks(items, job, quote, materialLinks, stoneLinks, diamondLinks, balance);
        return items;
    }

    private static void AddStageSpecificChecks(
        List<ProductionChecklistItem> items,
        Job job,
        CustomQuote? quote,
        List<QuoteOptionMaterialLink> materialLinks,
        List<QuoteOptionStoneLink> stoneLinks,
        List<QuoteOptionExternalDiamondLink> diamondLinks,
        decimal balance)
    {
        var materialReservations = materialLinks.Count(l => l.ReservationStatus.Equals("Reserved", StringComparison.OrdinalIgnoreCase) || l.ReservationStatus.Equals("Consumed", StringComparison.OrdinalIgnoreCase));
        var stoneReservations = stoneLinks.Count(l => l.ReservationStatus.Equals("Reserved", StringComparison.OrdinalIgnoreCase) || l.ReservationStatus.Equals("Consumed", StringComparison.OrdinalIgnoreCase));
        var supplierWaiting = diamondLinks.Any(l => !l.LinkStatus.Contains("Received", StringComparison.OrdinalIgnoreCase)
            && !l.LinkStatus.Contains("Converted", StringComparison.OrdinalIgnoreCase)
            && !l.LinkStatus.Contains("Released", StringComparison.OrdinalIgnoreCase));

        switch (job.Status)
        {
            case JobStatus.Enquiry:
            case JobStatus.Quoted:
                items.Add(new("Customer approval", quote?.AcceptedOptionId.HasValue == true ? "Ready" : "Waiting", quote?.AcceptedOptionId.HasValue == true ? "Accepted quote option is recorded." : "Wait for customer approval before production commitment."));
                break;
            case JobStatus.Approved:
            case JobStatus.DepositPaid:
                items.Add(new("Production planning", materialLinks.Count + stoneLinks.Count + diamondLinks.Count > 0 ? "Ready" : "Review", "Check required materials, stones, supplier diamonds and bench notes before moving to materials or production."));
                break;
            case JobStatus.AwaitingMaterials:
                items.Add(new("Reserved stock", materialReservations + stoneReservations > 0 ? "Ready" : "Waiting", materialReservations + stoneReservations > 0 ? $"{materialReservations} material and {stoneReservations} stone reservation(s) ready or consumed." : "No reserved material or stone links found for the accepted option."));
                items.Add(new("Supplier diamonds", supplierWaiting ? "Waiting" : "Ready", supplierWaiting ? "One or more linked supplier diamonds still needs hold/order/receive action." : "No unresolved supplier diamond wait detected."));
                break;
            case JobStatus.InProgress:
            case JobStatus.Setting:
            case JobStatus.Polishing:
                items.Add(new("Bench stage notes", string.IsNullOrWhiteSpace(job.InternalNotes) ? "Review" : "Ready", string.IsNullOrWhiteSpace(job.InternalNotes) ? "Add bench progress notes if the job needs handover between makers." : "Internal bench notes are recorded."));
                break;
            case JobStatus.QualityCheck:
                items.Add(new("Quality-control review", string.IsNullOrWhiteSpace(job.InternalNotes) ? "Review" : "Ready", "Confirm stone security, finish, size, cleanliness, packaging and customer-facing presentation."));
                items.Add(new("Handover payment", balance > 0 ? "Waiting" : "Ready", balance > 0 ? $"Collect or schedule remaining balance of {Money(balance)} before release." : "Payment is clear for handover."));
                break;
            case JobStatus.AwaitingCustomerApproval:
                items.Add(new("Customer decision", string.IsNullOrWhiteSpace(job.CustomerApprovalNotes) ? "Waiting" : "Review", string.IsNullOrWhiteSpace(job.CustomerApprovalNotes) ? "Record what the customer needs to approve or answer." : "Customer approval notes are recorded; confirm latest decision."));
                break;
            case JobStatus.ReadyForPickup:
            case JobStatus.ReadyToShip:
                items.Add(new("Final handover", balance > 0 ? "Waiting" : "Ready", balance > 0 ? "Balance remains before collection/shipping." : "Ready for receipt, collection/shipping confirmation and final follow-up."));
                break;
        }
    }

    private static bool HasCustomerContact(Customer? customer)
        => customer != null && (!string.IsNullOrWhiteSpace(customer.Phone) || !string.IsNullOrWhiteSpace(customer.Email));

    private static string BuildProductionRecommendedAction(Job job, List<ProductionChecklistItem> checklist, decimal balance)
    {
        var firstWaiting = checklist.FirstOrDefault(i => i.Status == "Waiting");
        if (firstWaiting != null)
            return $"{firstWaiting.Check}: {firstWaiting.Detail}";

        if (balance > 0 && job.Status is JobStatus.QualityCheck or JobStatus.ReadyForPickup or JobStatus.ReadyToShip)
            return $"Confirm payment plan or collect {Money(balance)} before release.";

        return job.Status switch
        {
            JobStatus.Enquiry or JobStatus.Quoted => "Confirm quote acceptance, required date and customer expectations before moving forward.",
            JobStatus.Approved or JobStatus.DepositPaid => "Confirm materials, supplier stones and production notes, then move to materials or bench work.",
            JobStatus.AwaitingMaterials => "Resolve material, stone or supplier waits before moving into production.",
            JobStatus.InProgress => "Continue bench work and record progress notes if another person may pick up the job.",
            JobStatus.Setting => "Complete setting checks, then move to polishing when secure.",
            JobStatus.Polishing => "Finish polish and presentation, then move to quality check.",
            JobStatus.QualityCheck => "Complete quality control, document final condition, then move to collection or shipping.",
            JobStatus.AwaitingCustomerApproval => "Contact the customer and record the approval decision.",
            JobStatus.ReadyForPickup => "Prepare receipt, handover confirmation, care notes and customer collection.",
            JobStatus.ReadyToShip => "Confirm address, postage method, tracking and insurance before dispatch.",
            JobStatus.Completed => "No production action required; keep documents and photos with the job record.",
            JobStatus.Cancelled => "No production action required unless refund, release or cleanup tasks remain.",
            _ => "Review job details and move to the next appropriate production stage."
        };
    }

    private static string ProductionStageTitle(JobStatus status) => status switch
    {
        JobStatus.Enquiry => "Enquiry",
        JobStatus.Quoted => "Awaiting Approval",
        JobStatus.Approved => "Approved",
        JobStatus.DepositPaid => "Deposit Paid",
        JobStatus.AwaitingMaterials => "Materials Required",
        JobStatus.InProgress => "In Production",
        JobStatus.Setting => "Setting",
        JobStatus.Polishing => "Polishing",
        JobStatus.QualityCheck => "Quality Check",
        JobStatus.AwaitingCustomerApproval => "Customer Check",
        JobStatus.ReadyForPickup => "Ready for Collection",
        JobStatus.ReadyToShip => "Ready to Ship",
        JobStatus.Completed => "Completed",
        JobStatus.Cancelled => "Cancelled",
        _ => status.ToString()
    };

    private static string ProductionStageGuidance(JobStatus status) => status switch
    {
        JobStatus.Enquiry or JobStatus.Quoted => "Customer and quote stage",
        JobStatus.Approved or JobStatus.DepositPaid => "Planning and deposit stage",
        JobStatus.AwaitingMaterials => "Waiting on stock or supplier input",
        JobStatus.InProgress or JobStatus.Setting or JobStatus.Polishing => "Active workshop stage",
        JobStatus.QualityCheck => "Final inspection stage",
        JobStatus.AwaitingCustomerApproval => "Waiting on customer decision",
        JobStatus.ReadyForPickup or JobStatus.ReadyToShip => "Handover stage",
        JobStatus.Completed => "Closed job",
        JobStatus.Cancelled => "Closed without completion",
        _ => "Review stage"
    };

    private static string ProductionDueGuidance(Job job)
    {
        if (!job.DueDate.HasValue)
            return "Set a due date if the customer expects timing.";
        if (job.Status is JobStatus.Completed or JobStatus.Cancelled)
            return "Closed job.";
        var days = (job.DueDate.Value.Date - DateTime.Today).Days;
        return days < 0 ? $"{Math.Abs(days)} day(s) overdue." : days == 0 ? "Due today." : $"{days} day(s) remaining.";
    }

    private static void AppendProductionChecklistTable(StringBuilder html, List<ProductionChecklistItem> items)
    {
        html.AppendLine("<h2>Stage Readiness Checklist</h2>");
        html.AppendLine("<table><tr><th>Check</th><th>Status</th><th>Detail</th></tr>");
        foreach (var item in items)
            html.AppendLine($"<tr><td>{Html(item.Check)}</td><td>{Html(item.Status)}</td><td>{Html(item.Detail)}</td></tr>");
        html.AppendLine("</table>");
    }

    private static void AppendProductionReservationTable(StringBuilder html, List<QuoteOptionMaterialLink> materialLinks, List<QuoteOptionStoneLink> stoneLinks, List<QuoteOptionExternalDiamondLink> diamondLinks)
    {
        html.AppendLine("<h2>Linked Materials, Stones and Supplier Diamonds</h2>");
        if (materialLinks.Count + stoneLinks.Count + diamondLinks.Count == 0)
        {
            html.AppendLine("<p>No quote-option material, stone or supplier diamond links were found for this job.</p>");
            return;
        }

        html.AppendLine("<table><tr><th>Type</th><th>Code / Source</th><th>Description</th><th>Quantity</th><th>Value</th><th>Status</th></tr>");
        foreach (var link in materialLinks)
            html.AppendLine($"<tr><td>Material</td><td>{Html(link.MaterialCodeSnapshot)}</td><td>{Html(link.MaterialNameSnapshot)}</td><td>{Number(link.Quantity)} {Html(link.UnitTypeSnapshot)}</td><td>{Money(link.LineCost)}</td><td>{Html(link.ReservationStatus)}</td></tr>");
        foreach (var link in stoneLinks)
            html.AppendLine($"<tr><td>Stone</td><td>{Html(link.StoneCodeSnapshot)}</td><td>{Html(link.DescriptionSnapshot)}</td><td>1</td><td>{Money(link.UnitCost)}</td><td>{Html(link.ReservationStatus)}</td></tr>");
        foreach (var link in diamondLinks)
            html.AppendLine($"<tr><td>Supplier diamond</td><td>{Html(link.SourceSystemSnapshot)} {Html(link.SupplierDiamondIdSnapshot)}</td><td>{Html(link.DiamondSummarySnapshot)}</td><td>1</td><td>{Money(link.RetailPriceSnapshot > 0 ? link.RetailPriceSnapshot : link.SupplierPrice)}</td><td>{Html(link.LinkStatus)}</td></tr>");
        html.AppendLine("</table>");
    }

    private static void AppendProductionTaskTable(StringBuilder html, List<BusinessTask> tasks)
    {
        html.AppendLine("<h2>Open Linked Tasks</h2>");
        if (tasks.Count == 0)
        {
            html.AppendLine("<p>No open linked tasks.</p>");
            return;
        }

        html.AppendLine("<table><tr><th>Task</th><th>Priority</th><th>Status</th><th>Due</th><th>Notes</th></tr>");
        foreach (var task in tasks)
            html.AppendLine($"<tr><td>{Html(task.ToString())}</td><td>{Html(task.Priority.ToString())}</td><td>{Html(task.Status.ToString())}</td><td>{Html(task.DueDate?.ToShortDateString() ?? string.Empty)}</td><td>{Html(task.FollowUpNotes ?? task.Description ?? string.Empty)}</td></tr>");
        html.AppendLine("</table>");
    }

    private static void AppendProductionPhotoTable(StringBuilder html, List<PhotoRecord> photos)
    {
        html.AppendLine("<h2>Linked Job Photos / Files</h2>");
        if (photos.Count == 0)
        {
            html.AppendLine("<p>No job photos or files are linked yet.</p>");
            return;
        }

        html.AppendLine("<table><tr><th>Caption</th><th>File</th><th>Status</th></tr>");
        foreach (var photo in photos)
        {
            var exists = !string.IsNullOrWhiteSpace(photo.FilePath) && File.Exists(photo.FilePath);
            html.AppendLine($"<tr><td>{Html(photo.Caption ?? $"Photo #{photo.Id}")}</td><td>{Html(photo.FilePath)}</td><td>{Html(exists ? "File found" : "Missing file")}</td></tr>");
        }
        html.AppendLine("</table>");
    }

    private static void AppendHorizontalBarChart(StringBuilder html, string title, List<ChartRow> rows)
    {
        html.AppendLine($"<h2>{Html(title)}</h2>");
        if (rows.Count == 0)
        {
            html.AppendLine("<p>No data is available for this chart yet.</p>");
            return;
        }

        var maxValue = rows.Max(r => r.Value);
        if (maxValue <= 0)
            maxValue = 1m;

        html.AppendLine("<div class='chart-table'>");
        foreach (var row in rows)
        {
            var width = Math.Clamp(row.Value / maxValue * 100m, 0m, 100m);
            html.AppendLine("<div class='chart-row'>");
            html.AppendLine($"<div class='chart-label'>{Html(row.Label)}</div>");
            html.AppendLine("<div class='chart-track'>");
            html.AppendLine($"<div class='chart-fill' style='width:{width.ToString("0.##", CultureInfo.InvariantCulture)}%'></div>");
            html.AppendLine("</div>");
            html.AppendLine($"<div class='chart-value'>{Html(row.DisplayValue)}</div>");
            html.AppendLine($"<div class='chart-detail'>{Html(row.Detail)}</div>");
            html.AppendLine("</div>");
        }
        html.AppendLine("</div>");
    }

    private static void AppendProfitableJobTypesTable(StringBuilder html, List<Job> jobs)
    {
        html.AppendLine("<h2>Most Profitable Job Types</h2>");
        html.AppendLine("<table><tr><th>Job Type</th><th>Jobs</th><th>Revenue</th><th>Cost</th><th>Profit</th><th>Margin</th></tr>");
        foreach (var group in jobs.GroupBy(j => j.Type).OrderByDescending(g => g.Sum(j => (j.FinalPrice > 0 ? j.FinalPrice : j.QuoteAmount) - PricingService.CalculateJobCost(j))))
        {
            var revenue = group.Sum(j => j.FinalPrice > 0 ? j.FinalPrice : j.QuoteAmount);
            var cost = group.Sum(PricingService.CalculateJobCost);
            html.AppendLine($"<tr><td>{Html(group.Key.ToString())}</td><td>{group.Count()}</td><td>{Money(revenue)}</td><td>{Money(cost)}</td><td>{Money(revenue - cost)}</td><td>{Percent(PricingService.CalculateProfitMargin(revenue, cost))}</td></tr>");
        }
        html.AppendLine("</table>");
    }

    private static void AppendStockLifecycleSummary(StringBuilder html)
    {
        html.AppendLine("<h2>Stock Lifecycle Guide</h2>");
        html.AppendLine("<table><tr><th>Lifecycle</th><th>Meaning</th></tr>");
        foreach (var row in StockLifecycleService.SummaryRows)
            html.AppendLine($"<tr><td>{Html(row.Label)}</td><td>{Html(row.Guidance)}</td></tr>");
        html.AppendLine("</table>");
    }

    private static void AppendInventoryValueTable(StringBuilder html, List<JewelleryItem> jewellery, List<Stone> stones, List<Material> materials)
    {
        var unsold = jewellery.Where(i => i.Status != StockStatus.Sold).ToList();
        html.AppendLine("<h2>Inventory Value</h2>");
        html.AppendLine("<table><tr><th>Category</th><th>Records</th><th>Cost / Value</th><th>Retail / Estimated Value</th></tr>");
        html.AppendLine($"<tr><td>Finished jewellery</td><td>{unsold.Count}</td><td>{Money(unsold.Sum(PricingService.CalculateJewelleryCost))}</td><td>{Money(unsold.Sum(i => i.RetailPrice))}</td></tr>");
        html.AppendLine($"<tr><td>Loose stones / opals</td><td>{stones.Count(s => s.Status != StoneStatus.Sold && s.Status != StoneStatus.SetInJewellery)}</td><td>{Money(stones.Where(s => s.Status != StoneStatus.Sold && s.Status != StoneStatus.SetInJewellery).Sum(s => s.EstimatedValue))}</td><td>{Money(stones.Where(s => s.Status != StoneStatus.Sold && s.Status != StoneStatus.SetInJewellery).Sum(s => s.EstimatedValue))}</td></tr>");
        html.AppendLine($"<tr><td>Materials</td><td>{materials.Count}</td><td>{Money(materials.Sum(m => m.CurrentQuantity * m.PurchaseCost))}</td><td>{Money(materials.Sum(m => m.CurrentQuantity * m.PurchaseCost))}</td></tr>");
        html.AppendLine("</table>");
    }

    private static void AppendInventoryValuationByCategoryTable(StringBuilder html, List<JewelleryItem> jewellery, List<Stone> stones, List<Material> materials, List<ExternalDiamond> externalDiamonds)
    {
        html.AppendLine("<h2>Valuation By Category</h2>");
        html.AppendLine("<table><tr><th>Category</th><th>Lifecycle</th><th>Records</th><th>Cost / Value</th><th>Retail / Decision Value</th><th>Guidance</th></tr>");

        foreach (var group in jewellery.Where(i => i.Status != StockStatus.Sold).GroupBy(i => i.Type).OrderByDescending(g => g.Sum(i => i.RetailPrice)))
        {
            var cost = group.Sum(PricingService.CalculateJewelleryCost);
            var retail = group.Sum(i => i.RetailPrice);
            html.AppendLine($"<tr><td>Jewellery - {Html(group.Key.ToString())}</td><td>Owned stock</td><td>{group.Count()}</td><td>{Money(cost)}</td><td>{Money(retail)}</td><td>{Html(CategoryValuationGuidance(retail, cost, group.Count(i => i.Status is StockStatus.NeedsPhotos or StockStatus.InProgress)))}</td></tr>");
        }

        foreach (var group in stones.Where(s => s.Status != StoneStatus.Sold && s.Status != StoneStatus.SetInJewellery).GroupBy(s => string.IsNullOrWhiteSpace(s.StoneType) ? "Stone" : s.StoneType).OrderByDescending(g => g.Sum(s => s.EstimatedValue)))
            html.AppendLine($"<tr><td>Stone - {Html(group.Key)}</td><td>Owned stone</td><td>{group.Count()}</td><td>{Money(group.Sum(s => s.EstimatedValue))}</td><td>{Money(group.Sum(s => s.EstimatedValue))}</td><td>{Html(group.Any(s => s.Status is StoneStatus.Reserved or StoneStatus.SelectedForDesign or StoneStatus.AssignedToJewellery) ? "Check reservation state before offering these stones again." : "Available for quote planning, listing or production selection.")}</td></tr>");

        foreach (var group in materials.GroupBy(m => m.Category).OrderByDescending(g => g.Sum(MaterialCurrentValue)))
            html.AppendLine($"<tr><td>Material - {Html(group.Key.ToString())}</td><td>Owned material</td><td>{group.Count()}</td><td>{Money(group.Sum(MaterialCurrentValue))}</td><td>{Money(group.Sum(MaterialCurrentValue))}</td><td>{Html(group.Any(m => m.CurrentQuantity <= m.ReorderLevel) ? "Low-stock lines exist in this category; check reorder recommendations." : "Quantity appears above reorder level for recorded lines.")}</td></tr>");

        var supplierOpen = externalDiamonds.Where(d => !string.Equals(d.Status, "Converted To Owned Inventory", StringComparison.OrdinalIgnoreCase) && !string.Equals(d.Status, "Released", StringComparison.OrdinalIgnoreCase)).ToList();
        if (supplierOpen.Count > 0)
            html.AppendLine($"<tr><td>External supplier diamonds</td><td>Supplier stock</td><td>{supplierOpen.Count}</td><td>{Money(supplierOpen.Sum(d => d.SupplierPrice))}</td><td>{Money(supplierOpen.Sum(d => d.EstimatedRetailPrice > 0 ? d.EstimatedRetailPrice : d.SupplierPrice))}</td><td>Supplier stock is not owned inventory until received and converted.</td></tr>");

        html.AppendLine("</table>");
    }

    private static void AppendInventoryReorderRecommendationTable(StringBuilder html, List<Material> lowMaterials, Dictionary<int, decimal> openOrderByMaterial, List<PurchaseOrder> openOrders, List<PurchaseOrderItem> openOrderItems, Dictionary<int, Supplier> suppliers)
    {
        html.AppendLine("<h2>Low-Stock Reorder Recommendations</h2>");
        if (lowMaterials.Count == 0)
        {
            html.AppendLine("<p>No materials are currently at or below reorder level.</p>");
            return;
        }

        var orderById = openOrders.ToDictionary(o => o.Id);
        html.AppendLine("<table><tr><th>Material</th><th>Category</th><th>Current</th><th>Reorder Level</th><th>Suggested Qty</th><th>Incoming Qty</th><th>Supplier</th><th>Recommended Action</th></tr>");
        foreach (var material in lowMaterials)
        {
            var incoming = openOrderByMaterial.TryGetValue(material.Id, out var incomingQty) ? incomingQty : 0m;
            var supplier = material.SupplierId.HasValue && suppliers.TryGetValue(material.SupplierId.Value, out var s) ? s.Name : string.Empty;
            var linkedOrders = openOrderItems
                .Where(i => i.MaterialId == material.Id && orderById.ContainsKey(i.PurchaseOrderId))
                .Select(i => orderById[i.PurchaseOrderId].PurchaseOrderCode)
                .Distinct()
                .ToList();
            var action = incoming > 0
                ? $"Check incoming order {string.Join(", ", linkedOrders)} before ordering again."
                : material.CurrentQuantity <= 0
                    ? "Order urgently or confirm an acceptable substitute."
                    : "Add to the next supplier order or adjust reorder level if this is no longer stocked.";
            html.AppendLine($"<tr><td>{Html(material.ToString())}</td><td>{Html(material.Category.ToString())}</td><td>{Number(material.CurrentQuantity)} {Html(material.UnitType.ToString())}</td><td>{Number(material.ReorderLevel)}</td><td>{Number(SuggestedMaterialReorderQuantity(material))}</td><td>{Number(incoming)}</td><td>{Html(supplier)}</td><td>{Html(action)}</td></tr>");
        }
        html.AppendLine("</table>");
    }

    private static void AppendInventorySlowMovingGuidanceTable(StringBuilder html, List<StockAgeRow> slowMoving)
    {
        html.AppendLine("<h2>Slow-Moving Stock Guidance</h2>");
        if (slowMoving.Count == 0)
        {
            html.AppendLine("<p>No unsold jewellery or available loose stones are currently older than 180 days.</p>");
            return;
        }

        html.AppendLine("<table><tr><th>Type</th><th>Code</th><th>Name</th><th>Status</th><th>Age</th><th>Value</th><th>Suggested Action</th></tr>");
        foreach (var row in slowMoving.OrderByDescending(r => r.AgeDays).ThenByDescending(r => r.Value).Take(50))
            html.AppendLine($"<tr><td>{Html(row.Type)}</td><td>{Html(row.Code)}</td><td>{Html(row.Name)}</td><td>{Html(row.Status)}</td><td>{row.AgeDays} days</td><td>{Money(row.Value)}</td><td>{Html(SlowMovingGuidance(row))}</td></tr>");
        html.AppendLine("</table>");
        if (slowMoving.Count > 50)
            html.AppendLine($"<p class='small'>Showing the 50 oldest/highest-value records from {slowMoving.Count} slow-moving items.</p>");
    }

    private static void AppendSupplierStockDecisionTable(StringBuilder html, List<ExternalDiamond> externalDiamonds)
    {
        html.AppendLine("<h2>Supplier Diamond Decision State</h2>");
        if (externalDiamonds.Count == 0)
        {
            html.AppendLine("<p>No saved external supplier diamonds are recorded yet.</p>");
            return;
        }

        html.AppendLine("<table><tr><th>Status</th><th>Records</th><th>Supplier Cost</th><th>Retail Estimate</th><th>Next Check</th></tr>");
        foreach (var group in externalDiamonds.GroupBy(d => string.IsNullOrWhiteSpace(d.Status) ? "No status" : d.Status).OrderByDescending(g => g.Count()))
        {
            var status = group.Key;
            var nextCheck = status.Contains("Hold", StringComparison.OrdinalIgnoreCase)
                ? "Confirm hold expiry and customer decision."
                : status.Contains("Order", StringComparison.OrdinalIgnoreCase)
                    ? "Confirm supplier order, ETA and received-diamond intake."
                    : status.Contains("Received", StringComparison.OrdinalIgnoreCase)
                        ? "Convert to owned inventory only if physically purchased."
                        : status.Contains("Converted", StringComparison.OrdinalIgnoreCase)
                            ? "Review owned stone record for valuation and availability."
                            : "Refresh availability and price before quoting.";
            html.AppendLine($"<tr><td>{Html(status)}</td><td>{group.Count()}</td><td>{Money(group.Sum(d => d.SupplierPrice))}</td><td>{Money(group.Sum(d => d.EstimatedRetailPrice > 0 ? d.EstimatedRetailPrice : d.SupplierPrice))}</td><td>{Html(nextCheck)}</td></tr>");
        }
        html.AppendLine("</table>");
    }

    private static void AppendMaterialAdjustmentAuditTable(StringBuilder html, List<MaterialTransaction> adjustmentTransactions, Dictionary<int, Material> materialById)
    {
        html.AppendLine("<h2>Material Adjustment Audit</h2>");
        if (adjustmentTransactions.Count == 0)
        {
            html.AppendLine("<p>No recent adjustment-style material transactions were found. Use Stock Movement for receive, use, adjust and return entries so quantity changes remain traceable.</p>");
            return;
        }

        html.AppendLine("<table><tr><th>Date</th><th>Material</th><th>Quantity Change</th><th>Reason</th><th>Linked Job / Stock</th><th>Notes</th></tr>");
        foreach (var tx in adjustmentTransactions)
        {
            var material = materialById.TryGetValue(tx.MaterialId, out var m) ? m : null;
            var linked = tx.JobId.HasValue ? $"Job #{tx.JobId}" : tx.JewelleryItemId.HasValue ? $"Stock #{tx.JewelleryItemId}" : "Not linked";
            html.AppendLine($"<tr><td>{Html(tx.TransactionDate.ToShortDateString())}</td><td>{Html(material?.ToString() ?? $"Material #{tx.MaterialId}")}</td><td>{Number(tx.QuantityChange)} {Html(material?.UnitType.ToString() ?? string.Empty)}</td><td>{Html(tx.Reason)}</td><td>{Html(linked)}</td><td>{Html(tx.Notes ?? string.Empty)}</td></tr>");
        }
        html.AppendLine("</table>");
    }

    private sealed record StockAgeRow(string Type, string Code, string Name, string Status, string Lifecycle, int AgeDays, decimal Value, DateTime CreatedAt, DateTime UpdatedAt);

    private static void AppendStockAgeBucketTable(StringBuilder html, List<StockAgeRow> rows)
    {
        var buckets = new[]
        {
            ("0-30 days", 0, 30),
            ("31-90 days", 31, 90),
            ("91-180 days", 91, 180),
            ("181-365 days", 181, 365),
            ("365+ days", 366, int.MaxValue)
        };

        html.AppendLine("<h2>Age Bands</h2>");
        html.AppendLine("<table><tr><th>Age Band</th><th>Records</th><th>Jewellery</th><th>Stones</th><th>Estimated Value</th></tr>");
        foreach (var bucket in buckets)
        {
            var bucketRows = rows.Where(r => r.AgeDays >= bucket.Item2 && r.AgeDays <= bucket.Item3).ToList();
            html.AppendLine($"<tr><td>{Html(bucket.Item1)}</td><td>{bucketRows.Count}</td><td>{bucketRows.Count(r => r.Type == "Jewellery")}</td><td>{bucketRows.Count(r => r.Type == "Stone")}</td><td>{Money(bucketRows.Sum(r => r.Value))}</td></tr>");
        }
        html.AppendLine("</table>");
    }

    private static void AppendSlowMovingInventoryTable(StringBuilder html, List<StockAgeRow> rows)
    {
        html.AppendLine("<h2>Slow-Moving Inventory (180+ Days)</h2>");
        if (rows.Count == 0)
        {
            html.AppendLine("<p>No jewellery or loose stones are currently older than 180 days.</p>");
            return;
        }

        html.AppendLine("<table><tr><th>Type</th><th>Code</th><th>Name</th><th>Status</th><th>Lifecycle</th><th>Age</th><th>Value</th><th>Created</th><th>Updated</th></tr>");
        foreach (var row in rows.OrderByDescending(r => r.AgeDays).ThenByDescending(r => r.Value).Take(120))
            html.AppendLine($"<tr><td>{Html(row.Type)}</td><td>{Html(row.Code)}</td><td>{Html(row.Name)}</td><td>{Html(row.Status)}</td><td>{Html(row.Lifecycle)}</td><td>{row.AgeDays} days</td><td>{Money(row.Value)}</td><td>{Html(row.CreatedAt.ToShortDateString())}</td><td>{Html(row.UpdatedAt.ToShortDateString())}</td></tr>");
        html.AppendLine("</table>");
        if (rows.Count > 120)
            html.AppendLine($"<p class='small'>Showing the 120 oldest records from {rows.Count} slow-moving items.</p>");
    }

    private static string CategoryValuationGuidance(decimal retail, decimal cost, int needsWorkCount)
    {
        if (needsWorkCount > 0)
            return $"{needsWorkCount} records need photos or finishing before this value can work.";
        if (retail <= 0)
            return "Retail price is missing; update valuation before buying more stock in this category.";
        if (cost <= 0)
            return "Cost is missing; profit and valuation are less reliable.";
        return "Review sell-through before adding more pieces in this category.";
    }

    private static decimal MaterialUnitCost(Material material)
        => material.CurrentQuantity > 0 && material.PurchaseCost > 0
            ? material.PurchaseCost / material.CurrentQuantity
            : material.PurchaseCost;

    private static decimal MaterialCurrentValue(Material material)
        => Math.Max(0, material.CurrentQuantity) * MaterialUnitCost(material);

    private static decimal SuggestedMaterialReorderQuantity(Material material)
    {
        var target = Math.Max(material.ReorderLevel * 2, material.ReorderLevel + 1);
        return Math.Max(1, target - material.CurrentQuantity);
    }

    private static bool IsMaterialAdjustment(MaterialTransaction transaction)
    {
        var reason = transaction.Reason ?? string.Empty;
        if (reason.Contains("adjust", StringComparison.OrdinalIgnoreCase))
            return true;
        if (!transaction.JobId.HasValue && !transaction.JewelleryItemId.HasValue && transaction.QuantityChange < 0)
            return true;
        return false;
    }

    private static string SlowMovingGuidance(StockAgeRow row)
    {
        if (row.Status.Contains("NeedsPhotos", StringComparison.OrdinalIgnoreCase))
            return "Photograph and list before discounting.";
        if (row.Status.Contains("AtMarket", StringComparison.OrdinalIgnoreCase))
            return "Check market sell-through and return-to-stock state.";
        if (row.Status.Contains("Reserved", StringComparison.OrdinalIgnoreCase))
            return "Confirm the reservation is still valid or release it.";
        if (row.Value <= 0)
            return "Update valuation before deciding whether to discount, remake or archive.";
        return row.AgeDays >= 365 ? "Consider relisting, repricing, remaking or archiving." : "Review photos, description, price and customer fit.";
    }

    private static int InventoryAgeDays(DateTime createdAt, DateTime today)
    {
        var date = createdAt == default ? today : createdAt.Date;
        if (date > today)
            date = today;
        return (today - date).Days;
    }

    private static decimal JobPrice(Job job) => job.FinalPrice > 0 ? job.FinalPrice : job.QuoteAmount;

    private static DateTime StartOfFinancialYear(DateTime date)
        => date.Month >= 7 ? new DateTime(date.Year, 7, 1) : new DateTime(date.Year - 1, 7, 1);

    private static DateTime StartOfFinancialQuarter(DateTime date)
    {
        var financialYearStart = StartOfFinancialYear(date);
        var monthsIntoFinancialYear = ((date.Year - financialYearStart.Year) * 12) + date.Month - financialYearStart.Month;
        var quarterStartOffset = Math.Max(0, monthsIntoFinancialYear / 3 * 3);
        return financialYearStart.AddMonths(quarterStartOffset);
    }

    private static IEnumerable<Sale> SalesInPeriod(IEnumerable<Sale> sales, TaxPeriod period)
        => sales.Where(s => s.SaleDate.Date >= period.Start.Date && s.SaleDate.Date <= period.End.Date);

    private static IEnumerable<Payment> PaymentsInPeriod(IEnumerable<Payment> payments, TaxPeriod period)
        => payments.Where(p => p.PaymentDate.Date >= period.Start.Date && p.PaymentDate.Date <= period.End.Date);

    private static decimal TaxComponent(decimal grossAmount, BusinessSettings settings)
    {
        if (!settings.GstRegistered || settings.GstRatePercent <= 0 || grossAmount <= 0)
            return 0m;
        return Math.Round(grossAmount * settings.GstRatePercent / (100m + settings.GstRatePercent), 2);
    }

    private static List<DateTime> RecentMonthStarts(DateTime today, int monthCount)
    {
        var currentMonth = new DateTime(today.Year, today.Month, 1);
        return Enumerable.Range(0, monthCount)
            .Select(offset => currentMonth.AddMonths(offset - monthCount + 1))
            .ToList();
    }

    private static string MonthLabel(DateTime month)
        => month.ToString("MMM yy", CultureInfo.CurrentCulture);

    private static bool IsQuoteConverted(CustomQuote quote)
        => quote.AcceptedOptionId.HasValue
            || quote.LinkedJobId.HasValue
            || quote.Status.Contains("Accepted", StringComparison.OrdinalIgnoreCase)
            || quote.Status.Contains("Converted", StringComparison.OrdinalIgnoreCase);

    private static ProfitCategoryKey ClassifySaleCategory(Sale sale, Dictionary<int, JewelleryItem> jewelleryById, Dictionary<int, Job> jobsById)
    {
        if (sale.JewelleryItemId.HasValue && jewelleryById.TryGetValue(sale.JewelleryItemId.Value, out var item))
            return new ProfitCategoryKey(item.Type.ToString(), "Jewellery stock");
        if (sale.JobId.HasValue && jobsById.TryGetValue(sale.JobId.Value, out var job))
            return new ProfitCategoryKey(job.Type.ToString(), "Job work");
        if (sale.JewelleryItemId.HasValue)
            return new ProfitCategoryKey("Missing stock link", "Needs cleanup");
        if (sale.JobId.HasValue)
            return new ProfitCategoryKey("Missing job link", "Needs cleanup");
        return new ProfitCategoryKey(sale.SaleLocation.ToString(), "Unlinked sale");
    }

    private static string DescribeSaleSource(Sale sale, Dictionary<int, JewelleryItem> jewelleryById, Dictionary<int, Job> jobsById)
    {
        if (sale.JewelleryItemId.HasValue && jewelleryById.TryGetValue(sale.JewelleryItemId.Value, out var item))
            return item.ToString();
        if (sale.JobId.HasValue && jobsById.TryGetValue(sale.JobId.Value, out var job))
            return job.ToString();
        if (!string.IsNullOrWhiteSpace(sale.Notes))
            return sale.Notes;
        return $"Sale #{sale.Id}";
    }

    private static void AppendReservedInventoryTable(StringBuilder html, List<QuoteOptionStoneLink> stoneLinks, List<QuoteOptionMaterialLink> materialLinks)
    {
        html.AppendLine("<h2>Reserved Inventory</h2>");
        html.AppendLine(Row("Reserved stone value", Money(stoneLinks.Where(l => l.ReservationStatus.Equals("Reserved", StringComparison.OrdinalIgnoreCase)).Sum(l => l.UnitCost))));
        html.AppendLine(Row("Reserved material value", Money(materialLinks.Where(l => l.ReservationStatus.Equals("Reserved", StringComparison.OrdinalIgnoreCase)).Sum(l => l.LineCost))));
        html.AppendLine("<table><tr><th>Type</th><th>Code</th><th>Name / Description</th><th>Quantity</th><th>Value</th><th>Status</th><th>Lifecycle</th><th>Quote Option</th></tr>");
        foreach (var link in stoneLinks.Where(l => l.ReservationStatus.Equals("Reserved", StringComparison.OrdinalIgnoreCase)).OrderBy(l => l.StoneCodeSnapshot))
            html.AppendLine($"<tr><td>Stone</td><td>{Html(link.StoneCodeSnapshot)}</td><td>{Html(link.DescriptionSnapshot)}</td><td>1</td><td>{Money(link.UnitCost)}</td><td>{Html(link.ReservationStatus)}</td><td>{Html(StockLifecycleService.DescribeReservationStatus(link.ReservationStatus))}</td><td>{link.QuoteOptionId}</td></tr>");
        foreach (var link in materialLinks.Where(l => l.ReservationStatus.Equals("Reserved", StringComparison.OrdinalIgnoreCase)).OrderBy(l => l.MaterialNameSnapshot))
            html.AppendLine($"<tr><td>Material</td><td>{Html(link.MaterialCodeSnapshot)}</td><td>{Html(link.MaterialNameSnapshot)}</td><td>{Number(link.Quantity)} {Html(link.UnitTypeSnapshot)}</td><td>{Money(link.LineCost)}</td><td>{Html(link.ReservationStatus)}</td><td>{Html(StockLifecycleService.DescribeReservationStatus(link.ReservationStatus))}</td><td>{link.QuoteOptionId}</td></tr>");
        html.AppendLine("</table>");
    }

    private static void AppendFollowUpTable(StringBuilder html, List<BusinessTask> tasks)
    {
        html.AppendLine("<h2>Open Follow-Ups and Tasks</h2>");
        html.AppendLine("<table><tr><th>Task</th><th>Category</th><th>Priority</th><th>Status</th><th>Due</th><th>Customer</th><th>Job</th></tr>");
        foreach (var task in tasks)
            html.AppendLine($"<tr><td>{Html(task.ToString())}</td><td>{Html(task.Category.ToString())}</td><td>{Html(task.Priority.ToString())}</td><td>{Html(task.Status.ToString())}</td><td>{Html(task.DueDate?.ToShortDateString() ?? string.Empty)}</td><td>{Html(task.CustomerId?.ToString(CultureInfo.InvariantCulture) ?? string.Empty)}</td><td>{Html(task.JobId?.ToString(CultureInfo.InvariantCulture) ?? string.Empty)}</td></tr>");
        html.AppendLine("</table>");
    }

    private static void WriteCsv(string path, string[] headers, IEnumerable<string[]> rows)
    {
        var csv = new StringBuilder();
        csv.AppendLine(string.Join(",", headers.Select(Csv)));
        foreach (var row in rows)
            csv.AppendLine(string.Join(",", row.Select(Csv)));
        File.WriteAllText(path, csv.ToString());
    }

    private static string Csv(string value)
    {
        value ??= string.Empty;
        return $"\"{value.Replace("\"", "\"\"")}\"";
    }

    private static void AppendPaymentsTable(StringBuilder html, List<Payment> payments)
    {
        html.AppendLine("<h2>Payment History</h2>");
        if (payments.Count == 0)
        {
            html.AppendLine("<p class='notice'>No linked payment records found.</p>");
            return;
        }

        html.AppendLine("<table class='payment-ledger'><tr><th>Date</th><th>Amount</th><th>Method</th><th>Reference</th><th>Notes</th></tr>");
        foreach (var payment in payments)
            html.AppendLine($"<tr><td>{Html(payment.PaymentDate.ToShortDateString())}</td><td>{Money(payment.Amount)}</td><td>{Html(payment.Method.ToString())}</td><td>{Html(payment.Reference ?? string.Empty)}</td><td>{Html(payment.Notes ?? string.Empty)}</td></tr>");
        html.AppendLine("</table>");
    }

    private static void AppendPaymentScheduleTable(StringBuilder html, PaymentScheduleSummary schedule)
    {
        html.AppendLine("<h2>Payment Schedule</h2>");
        html.AppendLine($"<p class='notice'>{Html(schedule.Guidance)}</p>");
        html.AppendLine("<table><tr><th>Stage</th><th>Target</th><th>Paid</th><th>Remaining</th><th>Due</th><th>Status</th></tr>");
        foreach (var line in schedule.Lines)
            html.AppendLine($"<tr><td>{Html(line.Stage)}</td><td>{Money(line.TargetAmount)}</td><td>{Money(line.PaidAmount)}</td><td>{Money(line.RemainingAmount)}</td><td>{Html(line.DueText)}</td><td>{Html(line.Status)}</td></tr>");
        html.AppendLine("</table>");
    }

    private static void AppendDocumentHero(StringBuilder html, string title, string reference, string status)
    {
        html.AppendLine("<div class='document-hero'>");
        html.AppendLine("<div>");
        html.AppendLine("<p class='document-kicker'>OPALNOVA document</p>");
        html.AppendLine($"<h1>{Html(title)}</h1>");
        html.AppendLine($"<p class='document-reference'>{Html(reference)}</p>");
        html.AppendLine($"<p class='small'>Generated {Html(DateTime.Now.ToString("f", CultureInfo.CurrentCulture))}</p>");
        html.AppendLine("</div>");
        html.AppendLine($"<div class='document-status'>{Html(status)}</div>");
        html.AppendLine("</div>");
    }

    private static void AppendFinancialSummary(StringBuilder html, params (string Label, string Value, string Hint)[] items)
    {
        html.AppendLine("<div class='financial-summary'>");
        foreach (var item in items)
        {
            html.AppendLine("<div class='summary-tile'>");
            html.AppendLine($"<span>{Html(item.Label)}</span>");
            html.AppendLine($"<strong>{Html(item.Value)}</strong>");
            html.AppendLine($"<em>{Html(item.Hint)}</em>");
            html.AppendLine("</div>");
        }
        html.AppendLine("</div>");
    }

    private static void AppendDocumentNotice(StringBuilder html, string title, string text)
    {
        html.AppendLine("<div class='notice'>");
        html.AppendLine($"<strong>{Html(title)}</strong>");
        html.AppendLine($"<p>{Html(text)}</p>");
        html.AppendLine("</div>");
    }

    private static string BuildJobHandoverStatus(Job job, decimal balance)
    {
        var paymentText = balance <= 0 ? "No balance is currently owing." : $"Balance still owing: {Money(balance)}.";
        var statusText = job.Status switch
        {
            JobStatus.ReadyForPickup => "Ready for customer pickup. Confirm identity, payment and item condition at handover.",
            JobStatus.ReadyToShip => "Ready to ship. Confirm address, postage method, insurance and tracking before dispatch.",
            JobStatus.Completed => "Job is marked complete. Keep this document with the final customer handover record.",
            JobStatus.Cancelled => "Job is cancelled. Check whether this document should be issued before sending.",
            _ => $"Current job status: {job.Status}. Confirm handover readiness before collection or shipping."
        };

        return $"{statusText} {paymentText}";
    }

    private static string BuildHandoverMode(Job job)
    {
        return job.Status switch
        {
            JobStatus.ReadyToShip => "Shipping",
            JobStatus.ReadyForPickup => "Collection",
            JobStatus.Completed => "Completed handover",
            _ => "Collection / shipping"
        };
    }

    private static string BuildModeSpecificChecklistRow(Job job)
    {
        var check = job.Status == JobStatus.ReadyToShip
            ? "Shipping address / tracking checked"
            : "Customer identity / collection checked";
        return $"<tr><td>{Html(check)}</td><td></td></tr>";
    }

    private static string SignatureBlock(string label)
    {
        return $"<div class='signature'><strong>{Html(label)}:</strong><span></span><strong>Date:</strong><span></span></div>";
    }

    private static void AppendLowMaterialsTable(StringBuilder html, List<Material> lowMaterials)
    {
        html.AppendLine("<h2>Low Materials</h2>");
        html.AppendLine("<table><tr><th>Material</th><th>Quantity</th><th>Reorder Level</th><th>Unit</th></tr>");
        foreach (var material in lowMaterials)
            html.AppendLine($"<tr><td>{Html(material.ToString())}</td><td>{Number(material.CurrentQuantity)}</td><td>{Number(material.ReorderLevel)}</td><td>{Html(material.UnitType.ToString())}</td></tr>");
        html.AppendLine("</table>");
    }

    private static void AppendActiveJobsTable(StringBuilder html, List<Job> activeJobs)
    {
        html.AppendLine("<h2>Active Jobs</h2>");
        html.AppendLine("<table><tr><th>Job</th><th>Status</th><th>Due</th><th>Balance</th><th>Estimated Profit</th></tr>");
        foreach (var job in activeJobs.OrderBy(j => j.DueDate ?? DateTime.MaxValue))
            html.AppendLine($"<tr><td>{Html(job.ToString())}</td><td>{Html(job.Status.ToString())}</td><td>{Html(job.DueDate?.ToShortDateString() ?? string.Empty)}</td><td>{Money(job.BalanceOwing)}</td><td>{Money(PricingService.CalculateJobProfit(job))}</td></tr>");
        html.AppendLine("</table>");
    }

    private static void AppendRecentSalesTable(StringBuilder html, List<Sale> recentSales)
    {
        html.AppendLine("<h2>Recent Sales</h2>");
        html.AppendLine("<table><tr><th>Date</th><th>Amount</th><th>Cost</th><th>Profit</th><th>Margin</th><th>Location</th></tr>");
        foreach (var sale in recentSales)
            html.AppendLine($"<tr><td>{Html(sale.SaleDate.ToShortDateString())}</td><td>{Money(sale.SaleAmount)}</td><td>{Money(sale.CostOfGoods)}</td><td>{Money(sale.Profit)}</td><td>{Percent(PricingService.CalculateProfitMargin(sale.SaleAmount, sale.CostOfGoods))}</td><td>{Html(sale.SaleLocation.ToString())}</td></tr>");
        html.AppendLine("</table>");
    }

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
        html.AppendLine(".premium-document { border-color: #d5c49a; box-shadow: 0 8px 22px rgba(17,24,39,.08); }");
        html.AppendLine(".document-hero { display: flex; justify-content: space-between; gap: 20px; align-items: flex-start; border-bottom: 2px solid #d5c49a; padding-bottom: 16px; margin-bottom: 18px; }");
        html.AppendLine(".document-kicker { margin: 0 0 6px 0; color: #7c5a13; font-size: 11px; letter-spacing: .08em; text-transform: uppercase; font-weight: bold; }");
        html.AppendLine(".document-reference { margin: 0; color: #4b5563; font-size: 14px; }");
        html.AppendLine(".document-status { min-width: 180px; border: 1px solid #d5c49a; background: #fff8e5; color: #4b3510; border-radius: 8px; padding: 12px; text-align: center; font-weight: bold; }");
        html.AppendLine(".financial-summary { display: grid; grid-template-columns: repeat(3, minmax(0, 1fr)); gap: 12px; margin: 16px 0 20px 0; }");
        html.AppendLine(".summary-tile { border: 1px solid #ddd; background: #fafafa; border-radius: 8px; padding: 12px; }");
        html.AppendLine(".summary-tile span { display: block; color: #555; font-size: 12px; font-weight: bold; }");
        html.AppendLine(".summary-tile strong { display: block; font-size: 24px; margin: 6px 0; }");
        html.AppendLine(".summary-tile em { display: block; color: #666; font-size: 11px; font-style: normal; }");
        html.AppendLine(".document-columns { display: grid; grid-template-columns: 1fr 1fr; gap: 22px; margin: 12px 0 18px 0; }");
        html.AppendLine(".notice { border: 1px solid #ddd; background: #f8fafc; border-left: 4px solid #d5c49a; padding: 10px 12px; margin: 12px 0; color: #333; }");
        html.AppendLine(".notice p { margin: 6px 0 0 0; }");
        html.AppendLine(".payment-ledger td:nth-child(2), .payment-ledger th:nth-child(2) { text-align: right; }");
        html.AppendLine(".chart-table { margin: 8px 0 22px 0; display: grid; gap: 8px; }");
        html.AppendLine(".chart-row { display: grid; grid-template-columns: 170px minmax(160px, 1fr) 110px minmax(160px, 1fr); gap: 10px; align-items: center; font-size: 12px; }");
        html.AppendLine(".chart-label { font-weight: bold; color: #1f2937; }");
        html.AppendLine(".chart-track { height: 16px; background: #eef2f7; border: 1px solid #d1d5db; border-radius: 999px; overflow: hidden; }");
        html.AppendLine(".chart-fill { height: 100%; background: #1f4f5f; }");
        html.AppendLine(".chart-value { font-weight: bold; text-align: right; white-space: nowrap; }");
        html.AppendLine(".chart-detail { color: #555; }");
        html.AppendLine(".label { width: 360px; border: 1px solid #222; padding: 16px; }");
        html.AppendLine("h1 { margin: 0 0 12px 0; }");
        html.AppendLine("h2 { margin: 18px 0 8px 0; }");
        html.AppendLine(".row { display: flex; border-bottom: 1px solid #eee; padding: 6px 0; }");
        html.AppendLine(".key { width: 210px; font-weight: bold; }");
        html.AppendLine(".value { flex: 1; }");
        html.AppendLine(".notes { white-space: pre-wrap; border: 1px solid #ddd; min-height: 54px; padding: 8px; margin-bottom: 10px; }");
        html.AppendLine(".checkboxes { columns: 2; margin-top: 20px; }");
        html.AppendLine(".price { font-size: 30px; font-weight: bold; margin: 14px 0; }");
        html.AppendLine(".small { font-size: 12px; color: #555; }");
        html.AppendLine(".signature { display: grid; grid-template-columns: 170px 1fr 60px 160px; gap: 10px; align-items: end; margin-top: 24px; }");
        html.AppendLine(".signature span { border-bottom: 1px solid #333; min-height: 22px; }");
        html.AppendLine("table { border-collapse: collapse; width: 100%; margin-bottom: 18px; font-size: 12px; }");
        html.AppendLine("th, td { border: 1px solid #ddd; padding: 7px; text-align: left; }");
        html.AppendLine("th { background: #f2f2f2; }");
        html.AppendLine("@media print { button { display: none; } body { margin: 8mm; } .card { border: none; } .brand, .document-hero, .financial-summary { break-inside: avoid; } }");
        html.AppendLine("</style>");
        html.AppendLine("</head>");
        html.AppendLine("<body>");
        html.AppendLine("<button onclick=\"window.print()\">Print</button>");
        html.AppendLine("<div class='brand'>");
        if (!string.IsNullOrWhiteSpace(settings.LogoPath) && File.Exists(settings.LogoPath))
        {
            var logoUri = new Uri(settings.LogoPath).AbsoluteUri;
            html.AppendLine($"<img class='logo' src='{Html(logoUri)}' alt='Business logo'>");
        }
        html.AppendLine("<div>");
        html.AppendLine($"<h1 class='brand-title'>{Html(settings.BusinessName)}</h1>");
        html.AppendLine("<div class='brand-details'>");
        AppendIfPresent(html, "Owner", settings.OwnerName);
        AppendIfPresent(html, "ABN", settings.Abn);
        AppendIfPresent(html, "Phone", settings.Phone);
        AppendIfPresent(html, "Email", settings.Email);
        AppendIfPresent(html, "Website", settings.Website);
        AppendIfPresent(html, "Address", settings.Address);
        if (settings.GstRegistered)
            html.AppendLine($"<div>{Html(settings.TaxLabel)} registered - rate {settings.GstRatePercent:0.##}%</div>");
        html.AppendLine("</div>");
        html.AppendLine("</div>");
        html.AppendLine("</div>");
        return html.ToString();
    }

    private static string HtmlFooter()
    {
        var settings = BusinessSettingsService.Load();
        var footer = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(settings.DocumentFooterText) || !string.IsNullOrWhiteSpace(settings.TermsAndConditions))
        {
            footer.AppendLine("<div class='footer'>");
            if (!string.IsNullOrWhiteSpace(settings.DocumentFooterText))
                footer.AppendLine(Html(settings.DocumentFooterText));
            if (!string.IsNullOrWhiteSpace(settings.TermsAndConditions))
            {
                footer.AppendLine("<br><strong>Terms:</strong><br>");
                footer.AppendLine(Html(settings.TermsAndConditions));
            }
            footer.AppendLine("</div>");
        }
        footer.AppendLine("</body></html>");
        return footer.ToString();
    }

    private static void AppendIfPresent(StringBuilder html, string label, string value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            html.AppendLine($"<div>{Html(label)}: {Html(value)}</div>");
    }
    private static string Row(string key, string value) => $"<div class='row'><div class='key'>{Html(key)}</div><div class='value'>{Html(value)}</div></div>";
    private static string NotesBlock(string title, string? notes) => $"<h2>{Html(title)}</h2><div class='notes'>{Html(notes ?? string.Empty)}</div>";
    private static string Html(string value) => WebUtility.HtmlEncode(value);
    private static string Money(decimal amount) => amount.ToString("C", CultureInfo.CurrentCulture);
    private static string Number(decimal amount) => amount.ToString("0.###", CultureInfo.CurrentCulture);
    private static string Percent(decimal value) => value.ToString("P1", CultureInfo.CurrentCulture);
    private static string SafeFileName(string value)
    {
        foreach (var invalid in Path.GetInvalidFileNameChars())
            value = value.Replace(invalid, '_');
        return value;
    }
}
