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
        html.Append(HtmlHeader(balance <= 0 ? "Customer Receipt" : "Customer Invoice"));
        html.AppendLine("<section class='card'>");
        html.AppendLine(balance <= 0 ? "<h1>Customer Receipt</h1>" : "<h1>Customer Invoice</h1>");
        html.AppendLine($"<p class='small'>Generated {Html(DateTime.Now.ToString("f", CultureInfo.CurrentCulture))}</p>");
        html.AppendLine(Row("Invoice / Job", $"{job.JobCode} {job.JobTitle}".Trim()));
        html.AppendLine(Row("Customer", customer?.FullName ?? "Not linked"));
        html.AppendLine(Row("Phone", customer?.Phone ?? string.Empty));
        html.AppendLine(Row("Email", customer?.Email ?? string.Empty));
        html.AppendLine(Row("Description", job.JobTitle));
        html.AppendLine(Row("Total Amount", Money(amount)));
        html.AppendLine(Row("Payments Recorded", Money(paid)));
        html.AppendLine(Row("Balance Due", Money(balance)));
        html.AppendLine(NotesBlock("Work Notes", job.DesignNotes));
        AppendPaymentsTable(html, payments);
        html.AppendLine("<p class='small'>Please check payment method and reference details before giving this document to the customer.</p>");
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
        html.Append(HtmlHeader("Customer Receipt"));
        html.AppendLine("<section class='card'>");
        html.AppendLine("<h1>Customer Receipt</h1>");
        html.AppendLine(Row("Sale", $"Sale #{sale.Id}"));
        html.AppendLine(Row("Date", sale.SaleDate.ToShortDateString()));
        html.AppendLine(Row("Customer", customer?.FullName ?? "Not linked"));
        html.AppendLine(Row("Item / Job", item?.ToString() ?? job?.ToString() ?? "General sale"));
        html.AppendLine(Row("Sale Location", sale.SaleLocation.ToString()));
        html.AppendLine(Row("Payment Method", sale.PaymentMethod.ToString()));
        html.AppendLine(Row("Amount Paid", Money(sale.SaleAmount)));
        html.AppendLine(NotesBlock("Notes", sale.Notes));
        AppendPaymentsTable(html, payments);
        html.AppendLine("<p>Thank you for your purchase.</p>");
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
        html.Append(HtmlHeader("Deposit Receipt"));
        html.AppendLine("<section class='card'>");
        html.AppendLine("<h1>Deposit Receipt</h1>");
        html.AppendLine(Row("Job", $"{job.JobCode} {job.JobTitle}".Trim()));
        html.AppendLine(Row("Customer", customer?.FullName ?? "Not linked"));
        html.AppendLine(Row("Date", DateTime.Today.ToShortDateString()));
        html.AppendLine(Row("Deposit Paid", Money(job.DepositPaid)));
        html.AppendLine(Row("Total Job Amount", Money(amount)));
        html.AppendLine(Row("Balance Remaining", Money(Math.Max(0, amount - job.DepositPaid))));
        html.AppendLine("<p class='small'>This receipt confirms the deposit currently recorded on the job record.</p>");
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
        html.Append(HtmlHeader("Payment Receipt"));
        html.AppendLine("<section class='card'>");
        html.AppendLine("<h1>Payment Receipt</h1>");
        html.AppendLine(Row("Payment", $"Payment #{payment.Id}"));
        html.AppendLine(Row("Customer", customer?.FullName ?? "Not linked"));
        html.AppendLine(Row("Related Job", job?.ToString() ?? string.Empty));
        html.AppendLine(Row("Related Sale", sale == null ? string.Empty : sale.ToString()));
        html.AppendLine(Row("Date", payment.PaymentDate.ToShortDateString()));
        html.AppendLine(Row("Amount", Money(payment.Amount)));
        html.AppendLine(Row("Method", payment.Method.ToString()));
        html.AppendLine(Row("Reference", payment.Reference ?? string.Empty));
        html.AppendLine(NotesBlock("Notes", payment.Notes));
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
        var payments = db.Payments.AsEnumerable().Where(p => p.JobId == job.Id || (job.CustomerId.HasValue && p.CustomerId == job.CustomerId)).OrderByDescending(p => p.PaymentDate).ToList();
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
        html.Append(HtmlHeader(selectedBatch == null ? "Production Batch Report" : $"Batch Report — {selectedBatch.Name}"));
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
        AppendInventoryValueTable(html, jewellery, stones, materials);
        html.AppendLine("<h2>Finished jewellery stock</h2>");
        html.AppendLine("<table><tr><th>Stock</th><th>Status</th><th>Cost</th><th>Retail</th><th>Potential Profit</th><th>Margin</th></tr>");
        foreach (var item in jewellery.OrderBy(i => i.Status).ThenBy(i => i.StockCode))
        {
            var cost = PricingService.CalculateJewelleryCost(item);
            html.AppendLine($"<tr><td>{Html(item.ToString())}</td><td>{Html(item.Status.ToString())}</td><td>{Money(cost)}</td><td>{Money(item.RetailPrice)}</td><td>{Money(item.RetailPrice - cost)}</td><td>{Percent(PricingService.CalculateProfitMargin(item.RetailPrice, cost))}</td></tr>");
        }
        html.AppendLine("</table>");
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
        html.AppendLine("<table><tr><th>Stone</th><th>Type</th><th>Status</th><th>Weight</th><th>Dimensions</th><th>Brightness</th><th>Colours</th><th>Value</th><th>Parcel</th></tr>");
        foreach (var stone in stones)
        {
            parcels.TryGetValue(stone.OpalParcelId ?? 0, out var parcel);
            html.AppendLine($"<tr><td>{Html(stone.ToString())}</td><td>{Html(stone.StoneType)}</td><td>{Html(stone.Status.ToString())}</td><td>{Number(stone.WeightCarats)} ct</td><td>{Html(stone.Dimensions ?? string.Empty)}</td><td>{Html(stone.Brightness ?? string.Empty)}</td><td>{Html(stone.MainColours ?? string.Empty)}</td><td>{Money(stone.EstimatedValue)}</td><td>{Html(parcel?.ToString() ?? string.Empty)}</td></tr>");
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

    private static void AppendReservedInventoryTable(StringBuilder html, List<QuoteOptionStoneLink> stoneLinks, List<QuoteOptionMaterialLink> materialLinks)
    {
        html.AppendLine("<h2>Reserved Inventory</h2>");
        html.AppendLine(Row("Reserved stone value", Money(stoneLinks.Where(l => l.ReservationStatus.Equals("Reserved", StringComparison.OrdinalIgnoreCase)).Sum(l => l.UnitCost))));
        html.AppendLine(Row("Reserved material value", Money(materialLinks.Where(l => l.ReservationStatus.Equals("Reserved", StringComparison.OrdinalIgnoreCase)).Sum(l => l.LineCost))));
        html.AppendLine("<table><tr><th>Type</th><th>Code</th><th>Name / Description</th><th>Quantity</th><th>Value</th><th>Status</th><th>Quote Option</th></tr>");
        foreach (var link in stoneLinks.Where(l => l.ReservationStatus.Equals("Reserved", StringComparison.OrdinalIgnoreCase)).OrderBy(l => l.StoneCodeSnapshot))
            html.AppendLine($"<tr><td>Stone</td><td>{Html(link.StoneCodeSnapshot)}</td><td>{Html(link.DescriptionSnapshot)}</td><td>1</td><td>{Money(link.UnitCost)}</td><td>{Html(link.ReservationStatus)}</td><td>{link.QuoteOptionId}</td></tr>");
        foreach (var link in materialLinks.Where(l => l.ReservationStatus.Equals("Reserved", StringComparison.OrdinalIgnoreCase)).OrderBy(l => l.MaterialNameSnapshot))
            html.AppendLine($"<tr><td>Material</td><td>{Html(link.MaterialCodeSnapshot)}</td><td>{Html(link.MaterialNameSnapshot)}</td><td>{Number(link.Quantity)} {Html(link.UnitTypeSnapshot)}</td><td>{Money(link.LineCost)}</td><td>{Html(link.ReservationStatus)}</td><td>{link.QuoteOptionId}</td></tr>");
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
        html.AppendLine("<h2>Payments</h2>");
        if (payments.Count == 0)
        {
            html.AppendLine("<p class='small'>No linked payment records found.</p>");
            return;
        }

        html.AppendLine("<table><tr><th>Date</th><th>Amount</th><th>Method</th><th>Reference</th><th>Notes</th></tr>");
        foreach (var payment in payments)
            html.AppendLine($"<tr><td>{Html(payment.PaymentDate.ToShortDateString())}</td><td>{Money(payment.Amount)}</td><td>{Html(payment.Method.ToString())}</td><td>{Html(payment.Reference ?? string.Empty)}</td><td>{Html(payment.Notes ?? string.Empty)}</td></tr>");
        html.AppendLine("</table>");
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
        html.AppendLine("@media print { button { display: none; } body { margin: 8mm; } .card { border: none; } .brand { break-inside: avoid; } }");
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
            html.AppendLine($"<div>{Html(settings.TaxLabel)} registered — rate {settings.GstRatePercent:0.##}%</div>");
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
