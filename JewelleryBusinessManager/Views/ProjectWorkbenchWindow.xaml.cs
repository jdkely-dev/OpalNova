using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using JewelleryBusinessManager.Data;
using JewelleryBusinessManager.Models;

namespace JewelleryBusinessManager.Views;

public partial class ProjectWorkbenchWindow : Window
{
    public bool IsHostedInTab { get; set; }
    public event EventHandler? OpenQuotesRequested;
    public event EventHandler? OpenProductionRequested;
    public event EventHandler? OpenPaymentsRequested;
    public event EventHandler? OpenDiamondHoldsRequested;
    public event EventHandler? OpenDiamondSearchRequested;
    public event EventHandler? OpenCustomersRequested;
    public event EventHandler? OpenJobsRequested;
    public event EventHandler? CloseRequested;

    private readonly List<ProjectWorkbenchRow> _rows = new();
    private bool _ready;
    private bool _hasLoadedRows;
    private ProjectWorkbenchRow? _selectedRow;

    public ProjectWorkbenchWindow()
    {
        InitializeComponent();
        _ready = true;
        Loaded += ProjectWorkbenchWindow_Loaded;
        WorkbenchRoot.Loaded += ProjectWorkbenchWindow_Loaded;
    }

    private void ProjectWorkbenchWindow_Loaded(object sender, RoutedEventArgs e)
    {
        if (IsHostedInTab)
        {
            Margin = new Thickness(0);
            HorizontalAlignment = HorizontalAlignment.Stretch;
            VerticalAlignment = VerticalAlignment.Stretch;
        }

        if (_hasLoadedRows) return;
        _hasLoadedRows = true;
        LoadRows();
    }

    private void LoadRows()
    {
        using var db = new AppDbContext();
        var today = DateTime.Today;
        var soon = today.AddDays(7);

        var customers = db.Customers.AsNoTracking().ToDictionary(x => x.Id, x => x.FullName);
        var quotes = db.CustomQuotes.AsNoTracking().OrderByDescending(x => x.UpdatedAt).ToList();
        var options = db.QuoteOptions.AsNoTracking().ToList();
        var optionsByQuote = options.GroupBy(x => x.CustomQuoteId).ToDictionary(x => x.Key, x => x.ToList());
        var jobs = db.Jobs.AsNoTracking().OrderBy(x => x.DueDate ?? DateTime.MaxValue).ThenByDescending(x => x.UpdatedAt).ToList();
        var jobsById = jobs.ToDictionary(x => x.Id, x => x);
        var paymentsByJob = db.Payments.AsNoTracking().Where(x => x.JobId.HasValue).AsEnumerable()
            .GroupBy(x => x.JobId!.Value).ToDictionary(x => x.Key, x => x.Sum(p => p.Amount));
        var salesByJob = db.Sales.AsNoTracking().Where(x => x.JobId.HasValue).Select(x => x.JobId!.Value).ToHashSet();
        var tasks = db.BusinessTasks.AsNoTracking().Where(x => x.Status != BusinessTaskStatus.Completed && x.Status != BusinessTaskStatus.Cancelled).ToList();
        var externalDiamonds = db.ExternalDiamonds.AsNoTracking().ToDictionary(x => x.Id, x => x);
        var diamondLinks = db.QuoteOptionExternalDiamondLinks.AsNoTracking().ToList();
        var diamondLinksByOption = diamondLinks.GroupBy(x => x.QuoteOptionId).ToDictionary(x => x.Key, x => x.ToList());
        var quoteByOptionId = options.ToDictionary(x => x.Id, x => x.CustomQuoteId);
        var quoteLookup = quotes.ToDictionary(x => x.Id, x => x);

        _rows.Clear();
        foreach (var quote in quotes)
        {
            optionsByQuote.TryGetValue(quote.Id, out var quoteOptions);
            var acceptedOption = quote.AcceptedOptionId.HasValue ? quoteOptions?.FirstOrDefault(x => x.Id == quote.AcceptedOptionId.Value) : null;
            var topOption = acceptedOption ?? quoteOptions?.OrderByDescending(x => x.TotalPrice).FirstOrDefault();
            var customer = quote.CustomerId.HasValue && customers.TryGetValue(quote.CustomerId.Value, out var customerName) ? customerName : "No customer linked";
            var quoteStatus = quote.Status ?? string.Empty;
            var isExpired = quote.ValidUntil.HasValue && quote.ValidUntil.Value.Date < today && !quote.AcceptedOptionId.HasValue && !quoteStatus.Contains("Accepted", StringComparison.OrdinalIgnoreCase);
            var hasAcceptedOption = quote.AcceptedOptionId.HasValue || quoteStatus.Contains("Accepted", StringComparison.OrdinalIgnoreCase);
            var hasLinkedJob = quote.LinkedJobId.HasValue && jobsById.ContainsKey(quote.LinkedJobId.Value);
            var linkedExternalDiamonds = quoteOptions == null
                ? new List<ExternalDiamond>()
                : quoteOptions.SelectMany(o => diamondLinksByOption.TryGetValue(o.Id, out var links) ? links : Enumerable.Empty<QuoteOptionExternalDiamondLink>())
                    .Select(l => externalDiamonds.TryGetValue(l.ExternalDiamondId, out var d) ? d : null)
                    .Where(d => d != null)
                    .Cast<ExternalDiamond>()
                    .ToList();
            var diamondNeedsAction = linkedExternalDiamonds.Any(DiamondNeedsAction);

            var nextAction = "Open quote builder and continue proposal.";
            var priority = 3;
            var priorityLabel = "Medium";
            var actionKey = "Quote";
            var risk = string.Empty;

            if (isExpired)
            {
                priority = 1;
                priorityLabel = "Urgent";
                nextAction = "Quote has expired — follow up or reprice before sending.";
                risk = "Expired quote: prices, diamond availability or metal costs may no longer be safe.";
                actionKey = "Quote";
            }
            else if (hasAcceptedOption && !hasLinkedJob)
            {
                priority = 1;
                priorityLabel = "Urgent";
                nextAction = "Accepted quote needs to be converted into a production job.";
                actionKey = "Quote";
                risk = "Customer has accepted, but there is no linked job yet.";
            }
            else if (diamondNeedsAction)
            {
                priority = 1;
                priorityLabel = "Urgent";
                nextAction = "External diamond needs hold/order attention.";
                actionKey = "DiamondHolds";
                risk = "Supplier diamond is linked to the quote but is not safely received.";
            }
            else if (hasAcceptedOption && hasLinkedJob)
            {
                priority = 4;
                priorityLabel = "Low";
                nextAction = "Quote is accepted and linked to production. Monitor the production board.";
                actionKey = "Production";
            }
            else if (quote.ValidUntil.HasValue && quote.ValidUntil.Value.Date <= soon)
            {
                priority = 2;
                priorityLabel = "High";
                nextAction = "Quote validity is ending soon — follow up with the customer.";
                actionKey = "Quote";
            }
            else if (string.IsNullOrWhiteSpace(quoteStatus) || quoteStatus.Equals("Draft", StringComparison.OrdinalIgnoreCase))
            {
                nextAction = "Finish the proposal and send it to the customer.";
                actionKey = "Quote";
            }

            _rows.Add(new ProjectWorkbenchRow
            {
                Area = "Quote",
                Priority = priority,
                PriorityLabel = priorityLabel,
                Subject = $"{customer} — {DisplayOrFallback(quote.Title, quote.QuoteCode, "Custom quote")}",
                NextAction = nextAction,
                DueLine = quote.ValidUntil.HasValue ? $"Valid to {quote.ValidUntil.Value:d}" : "No expiry",
                ValueLine = topOption != null && topOption.TotalPrice > 0 ? topOption.TotalPrice.ToString("C") : string.Empty,
                Context = BuildQuoteContext(quote, topOption, linkedExternalDiamonds, hasLinkedJob),
                Risk = risk,
                SuggestedMessage = BuildQuoteMessage(customer, quote, topOption, isExpired),
                PrimaryActionKey = actionKey,
                QuoteId = quote.Id,
                JobId = quote.LinkedJobId,
                CustomerId = quote.CustomerId
            });
        }

        foreach (var job in jobs.Where(x => x.Status != JobStatus.Completed && x.Status != JobStatus.Cancelled))
        {
            var customer = job.CustomerId.HasValue && customers.TryGetValue(job.CustomerId.Value, out var name) ? name : "No customer linked";
            var total = GetJobTotal(job);
            paymentsByJob.TryGetValue(job.Id, out var paidFromPayments);
            var paid = Math.Max(job.DepositPaid, paidFromPayments);
            var balance = Math.Max(0, total - paid);
            var isOverdue = job.DueDate.HasValue && job.DueDate.Value.Date < today;
            var dueSoon = job.DueDate.HasValue && job.DueDate.Value.Date <= soon;
            var nextAction = "Open production board and move the job to the correct stage.";
            var priority = 3;
            var priorityLabel = "Medium";
            var actionKey = "Production";
            var risk = string.Empty;

            if ((job.Status == JobStatus.ReadyForPickup || job.Status == JobStatus.ReadyToShip) && balance > 0)
            {
                priority = 1;
                priorityLabel = "Urgent";
                nextAction = "Ready for handover but balance is still owing.";
                actionKey = "Payments";
                risk = "Do not complete handover before checking payment.";
            }
            else if (job.Status == JobStatus.ReadyForPickup || job.Status == JobStatus.ReadyToShip)
            {
                priority = 2;
                priorityLabel = "High";
                nextAction = "Arrange customer pickup/shipping and complete the sale.";
                actionKey = "Payments";
            }
            else if (isOverdue)
            {
                priority = 1;
                priorityLabel = "Urgent";
                nextAction = "Job is overdue — update status or contact the customer.";
                actionKey = "Production";
                risk = "Overdue job may affect customer confidence.";
            }
            else if (dueSoon)
            {
                priority = 2;
                priorityLabel = "High";
                nextAction = "Job is due soon — confirm bench progress and materials.";
                actionKey = "Production";
            }

            _rows.Add(new ProjectWorkbenchRow
            {
                Area = balance > 0 ? "Payment" : "Production",
                Priority = priority,
                PriorityLabel = priorityLabel,
                Subject = $"{customer} — {DisplayOrFallback(job.JobTitle, job.JobCode, "Job")}",
                NextAction = nextAction,
                DueLine = job.DueDate.HasValue ? job.DueDate.Value.ToString("d") : "No due date",
                ValueLine = total > 0 ? total.ToString("C") : string.Empty,
                Context = $"Job {job.JobCode}\nStatus: {job.Status}\nTotal: {total:C}\nPaid: {paid:C}\nBalance: {balance:C}\nSale created: {(salesByJob.Contains(job.Id) ? "Yes" : "No")}\nNotes: {job.DesignNotes} {job.InternalNotes}".Trim(),
                Risk = risk,
                SuggestedMessage = BuildJobMessage(customer, job, balance),
                PrimaryActionKey = actionKey,
                JobId = job.Id,
                CustomerId = job.CustomerId
            });
        }

        foreach (var diamond in externalDiamonds.Values.Where(DiamondNeedsAction))
        {
            var link = diamondLinks.FirstOrDefault(x => x.ExternalDiamondId == diamond.Id);
            CustomQuote? quote = null;
            QuoteOption? option = null;
            if (link != null && quoteByOptionId.TryGetValue(link.QuoteOptionId, out var quoteId))
            {
                quoteLookup.TryGetValue(quoteId, out quote);
                option = options.FirstOrDefault(x => x.Id == link.QuoteOptionId);
            }
            var customer = quote?.CustomerId is int cid && customers.TryGetValue(cid, out var name) ? name : "No customer linked";
            var isHoldRisk = diamond.HoldExpiresAt.HasValue && diamond.HoldExpiresAt.Value <= DateTime.Now.AddHours(12);
            _rows.Add(new ProjectWorkbenchRow
            {
                Area = "Diamond",
                Priority = isHoldRisk ? 1 : 2,
                PriorityLabel = isHoldRisk ? "Urgent" : "High",
                Subject = $"{customer} — {BuildDiamondSummary(diamond)}",
                NextAction = isHoldRisk ? "Supplier hold is expired or expiring — confirm availability now." : "Supplier diamond needs hold/order update.",
                DueLine = diamond.HoldExpiresAt.HasValue ? $"Hold {diamond.HoldExpiresAt.Value:g}" : diamond.ExpectedArrivalDate.HasValue ? $"ETA {diamond.ExpectedArrivalDate.Value:d}" : diamond.Status,
                ValueLine = diamond.SupplierPrice > 0 ? diamond.SupplierPrice.ToString("C") : string.Empty,
                Context = $"Status: {diamond.Status}\nQuote: {quote?.QuoteCode ?? "Not linked"}\nOption: {option?.OptionName ?? ""}\nSupplier ID: {diamond.SupplierDiamondId}\nCertificate: {diamond.CertificateNumber}\nReference: {diamond.SupplierReference}\nNotes: {diamond.Notes}".Trim(),
                Risk = "External supplier stones can disappear from availability. Hold/order steps should be confirmed before relying on them.",
                SuggestedMessage = BuildDiamondMessage(customer, diamond),
                PrimaryActionKey = "DiamondHolds",
                QuoteId = quote?.Id,
                CustomerId = quote?.CustomerId,
                ExternalDiamondId = diamond.Id
            });
        }

        foreach (var task in tasks.Where(x => x.ShowOnDashboard).OrderBy(x => x.DueDate ?? DateTime.MaxValue).Take(80))
        {
            var customer = task.CustomerId.HasValue && customers.TryGetValue(task.CustomerId.Value, out var name) ? name : string.Empty;
            var jobTitle = task.JobId.HasValue && jobsById.TryGetValue(task.JobId.Value, out var linkedJob) ? linkedJob.JobTitle : string.Empty;
            var isOverdue = task.DueDate.HasValue && task.DueDate.Value.Date < today;
            _rows.Add(new ProjectWorkbenchRow
            {
                Area = "Follow-up",
                Priority = isOverdue ? 1 : task.Priority == BusinessTaskPriority.Urgent ? 1 : task.Priority == BusinessTaskPriority.High ? 2 : 3,
                PriorityLabel = isOverdue || task.Priority == BusinessTaskPriority.Urgent ? "Urgent" : task.Priority == BusinessTaskPriority.High ? "High" : "Medium",
                Subject = DisplayOrFallback(customer, jobTitle, task.Title),
                NextAction = task.Title,
                DueLine = task.DueDate.HasValue ? task.DueDate.Value.ToString("d") : "No due date",
                ValueLine = task.Priority.ToString(),
                Context = $"Task {task.TaskCode}\nCategory: {task.Category}\nStatus: {task.Status}\nDescription: {task.Description}\nFollow-up notes: {task.FollowUpNotes}".Trim(),
                Risk = isOverdue ? "Task is overdue." : string.Empty,
                SuggestedMessage = task.FollowUpNotes ?? task.Description ?? string.Empty,
                PrimaryActionKey = task.JobId.HasValue ? "Jobs" : task.CustomerId.HasValue ? "Customers" : "Quote",
                JobId = task.JobId,
                CustomerId = task.CustomerId
            });
        }

        _rows.Sort((a, b) => a.Priority != b.Priority ? a.Priority.CompareTo(b.Priority) : string.Compare(a.Area, b.Area, StringComparison.OrdinalIgnoreCase));
        ApplyFilter();
    }

    private static bool DiamondNeedsAction(ExternalDiamond d)
    {
        var status = d.Status ?? string.Empty;
        if (status.Contains("Received", StringComparison.OrdinalIgnoreCase) || status.Contains("Declined", StringComparison.OrdinalIgnoreCase) || status.Contains("Released", StringComparison.OrdinalIgnoreCase))
            return false;
        if (d.HoldExpiresAt.HasValue && d.HoldExpiresAt.Value <= DateTime.Now.AddHours(24))
            return true;
        return status is "Customer Interested" or "Hold Requested" or "Hold Confirmed" or "Hold Expiring" or "Order Requested" or "Ordered";
    }

    private static decimal GetJobTotal(Job job) => job.FinalPrice > 0 ? job.FinalPrice : job.QuoteAmount;

    private static string DisplayOrFallback(params string?[] values)
    {
        foreach (var value in values)
            if (!string.IsNullOrWhiteSpace(value))
                return value.Trim();
        return "Untitled";
    }

    private static string BuildDiamondSummary(ExternalDiamond d)
    {
        var type = d.IsLabGrown ? "Lab-grown" : "Natural";
        return $"{type} {d.Shape} {d.Carat:0.###}ct {d.Color} {d.Clarity} {d.Lab} {d.CertificateNumber}".Replace("  ", " ").Trim();
    }

    private static string BuildQuoteContext(CustomQuote quote, QuoteOption? option, IReadOnlyCollection<ExternalDiamond> diamonds, bool hasLinkedJob)
    {
        var diamondText = diamonds.Count == 0 ? "None" : string.Join("; ", diamonds.Select(BuildDiamondSummary));
        return $"Quote: {quote.QuoteCode}\nStatus: {quote.Status}\nRecommended/accepted option: {option?.OptionName ?? "No option selected"}\nOption total: {(option != null ? option.TotalPrice.ToString("C") : "No total")}\nLinked production job: {(hasLinkedJob ? "Yes" : "No")}\nExternal diamonds: {diamondText}\nCustomer notes: {quote.CustomerNotes}\nInternal notes: {quote.InternalNotes}".Trim();
    }

    private static string BuildQuoteMessage(string customer, CustomQuote quote, QuoteOption? option, bool isExpired)
    {
        var greeting = string.IsNullOrWhiteSpace(customer) || customer == "No customer linked" ? "Hi," : $"Hi {customer},";
        if (isExpired)
            return $"{greeting}\n\nI just wanted to follow up on your quote {quote.QuoteCode}. Some supplier and material pricing can change over time, so I can refresh the details for you before we go ahead.\n\nKind regards";
        var price = option != null && option.TotalPrice > 0 ? $" The current selected option is {option.TotalPrice:C}." : string.Empty;
        return $"{greeting}\n\nI have your custom jewellery proposal ready for review.{price} Let me know if you would like any changes, or if you are happy for me to move ahead with the preferred option.\n\nKind regards";
    }

    private static string BuildJobMessage(string customer, Job job, decimal balance)
    {
        var greeting = string.IsNullOrWhiteSpace(customer) || customer == "No customer linked" ? "Hi," : $"Hi {customer},";
        if (job.Status == JobStatus.ReadyForPickup || job.Status == JobStatus.ReadyToShip)
            return balance > 0
                ? $"{greeting}\n\nYour jewellery is ready. The remaining balance is {balance:C}. Once that is finalised, we can arrange collection or shipping.\n\nKind regards"
                : $"{greeting}\n\nYour jewellery is ready. We can now arrange collection or shipping at a time that suits you.\n\nKind regards";
        return $"{greeting}\n\nJust a quick update on your job {job.JobCode}: it is currently at the {job.Status} stage. I will keep you updated as it progresses.\n\nKind regards";
    }

    private static string BuildDiamondMessage(string customer, ExternalDiamond diamond)
    {
        var greeting = string.IsNullOrWhiteSpace(customer) || customer == "No customer linked" ? "Hi," : $"Hi {customer},";
        return $"{greeting}\n\nThe diamond option we discussed is currently marked as {diamond.Status}. I am checking supplier availability/hold details so we can avoid losing the stone before confirming the design.\n\nKind regards";
    }

    private void UpdateCounts(IEnumerable<ProjectWorkbenchRow> rows)
    {
        var visibleRows = rows as IReadOnlyCollection<ProjectWorkbenchRow> ?? rows.ToList();
        NeedsActionCountText.Text = visibleRows.Count(x => x.Priority <= 2).ToString();
        QuoteCountText.Text = visibleRows.Count(x => x.Area == "Quote").ToString();
        ProductionCountText.Text = visibleRows.Count(x => x.Area == "Production").ToString();
        PaymentCountText.Text = visibleRows.Count(x => x.Area == "Payment").ToString();
        DiamondCountText.Text = visibleRows.Count(x => x.Area == "Diamond").ToString();
        FollowUpCountText.Text = visibleRows.Count(x => x.Area == "Follow-up").ToString();
    }


    private string GetSelectedFilter()
    {
        if (FilterCombo.SelectedItem is ComboBoxItem item)
            return item.Content?.ToString() ?? "Action needed";
        return FilterCombo.SelectedItem?.ToString() ?? FilterCombo.Text ?? "Action needed";
    }

    private void ApplyFilter()
    {
        if (!_ready) return;
        IEnumerable<ProjectWorkbenchRow> query = _rows;
        var filter = GetSelectedFilter();
        query = filter switch
        {
            "Quotes" => query.Where(x => x.Area == "Quote"),
            "Production" => query.Where(x => x.Area == "Production"),
            "Payments" => query.Where(x => x.Area == "Payment"),
            "Diamonds" => query.Where(x => x.Area == "Diamond"),
            "Follow-ups" => query.Where(x => x.Area == "Follow-up"),
            "All projects" => query,
            _ => query.Where(x => x.Priority <= 2)
        };
        var search = SearchBox.Text.Trim();
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(x => $"{x.Area} {x.Subject} {x.NextAction} {x.DueLine} {x.ValueLine} {x.Context}".Contains(search, StringComparison.OrdinalIgnoreCase));
        }
        var selectedKey = _selectedRow?.StableKey;
        var rows = query.ToList();
        UpdateCounts(rows);
        ProjectsGrid.ItemsSource = rows;
        _selectedRow = rows.FirstOrDefault(x => x.StableKey == selectedKey) ?? rows.FirstOrDefault();
        ProjectsGrid.SelectedItem = _selectedRow;
        var searchSuffix = string.IsNullOrWhiteSpace(search) ? string.Empty : $" - Search: {search}";
        StatusText.Text = $"Showing {rows.Count} of {_rows.Count} workflow item(s) - Filter: {filter}{searchSuffix}";
        UpdateSelectedDetails();
    }

    private void UpdateSelectedDetails()
    {
        var row = ProjectsGrid.SelectedItem as ProjectWorkbenchRow ?? _selectedRow;
        _selectedRow = row;
        if (row == null)
        {
            SelectedTitleText.Text = "Select a project";
            SelectedSubtitleText.Text = "Choose a row to see context, risk warnings and the next recommended action.";
            NextActionText.Text = "No project selected.";
            RiskText.Text = string.Empty;
            ContextText.Text = string.Empty;
            SuggestedMessageBox.Text = string.Empty;
            PrimaryActionButton.IsEnabled = false;
            return;
        }
        PrimaryActionButton.IsEnabled = true;
        SelectedTitleText.Text = row.Subject;
        SelectedSubtitleText.Text = $"{row.Area} • {row.PriorityLabel} priority • {row.DueLine}";
        NextActionText.Text = row.NextAction;
        RiskText.Text = row.Risk;
        ContextText.Text = row.Context;
        SuggestedMessageBox.Text = row.SuggestedMessage;
        PrimaryActionButton.Content = row.PrimaryActionKey switch
        {
            "Quote" => "Open Quote Workflow",
            "Production" => "Open Production Board",
            "Payments" => "Open Payment Workflow",
            "DiamondHolds" => "Open Diamond Holds",
            "DiamondSearch" => "Open Diamond Search",
            "Customers" => "Open Customers",
            "Jobs" => "Open Jobs",
            _ => "Open Next Step"
        };
    }

    private void PrimaryAction_Click(object sender, RoutedEventArgs e)
    {
        switch (_selectedRow?.PrimaryActionKey)
        {
            case "Production": OpenProductionRequested?.Invoke(this, EventArgs.Empty); break;
            case "Payments": OpenPaymentsRequested?.Invoke(this, EventArgs.Empty); break;
            case "DiamondHolds": OpenDiamondHoldsRequested?.Invoke(this, EventArgs.Empty); break;
            case "DiamondSearch": OpenDiamondSearchRequested?.Invoke(this, EventArgs.Empty); break;
            case "Customers": OpenCustomersRequested?.Invoke(this, EventArgs.Empty); break;
            case "Jobs": OpenJobsRequested?.Invoke(this, EventArgs.Empty); break;
            default: OpenQuotesRequested?.Invoke(this, EventArgs.Empty); break;
        }
    }

    private void CreateFollowUp_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedRow == null)
        {
            MessageBox.Show("Select a project first.", "Project Workbench", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        using var db = new AppDbContext();
        var code = $"TASK-{DateTime.Now:yyyyMMdd-HHmmss}";
        var task = new BusinessTask
        {
            TaskCode = code,
            Title = $"Follow up: {_selectedRow.Subject}",
            Category = BusinessTaskCategory.CustomerFollowUp,
            Priority = _selectedRow.Priority <= 1 ? BusinessTaskPriority.High : BusinessTaskPriority.Normal,
            Status = BusinessTaskStatus.ToDo,
            DueDate = DateTime.Today.AddDays(_selectedRow.Priority <= 1 ? 1 : 2),
            ReminderDate = DateTime.Today.AddDays(1),
            CustomerId = _selectedRow.CustomerId,
            JobId = _selectedRow.JobId,
            Description = _selectedRow.NextAction,
            FollowUpNotes = SuggestedMessageBox.Text.Trim(),
            ShowOnDashboard = true
        };
        db.BusinessTasks.Add(task);
        db.SaveChanges();
        StatusText.Text = $"Created follow-up task {code}.";
        MessageBox.Show("Follow-up task created and added to the dashboard/work queue.", "Project Workbench", MessageBoxButton.OK, MessageBoxImage.Information);
        LoadRows();
    }

    private void CopyMessage_Click(object sender, RoutedEventArgs e)
    {
        var text = SuggestedMessageBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            MessageBox.Show("There is no suggested message to copy.", "Project Workbench", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        Clipboard.SetText(text);
        StatusText.Text = "Suggested message copied to clipboard.";
    }

    private void ProjectsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e) => UpdateSelectedDetails();
    private void ProjectsGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e) => PrimaryAction_Click(sender, e);
    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilter();
    private void FilterCombo_SelectionChanged(object sender, SelectionChangedEventArgs e) => ApplyFilter();
    private void Refresh_Click(object sender, RoutedEventArgs e) => LoadRows();
    private void OpenQuotes_Click(object sender, RoutedEventArgs e) => OpenQuotesRequested?.Invoke(this, EventArgs.Empty);
    private void OpenProduction_Click(object sender, RoutedEventArgs e) => OpenProductionRequested?.Invoke(this, EventArgs.Empty);
    private void OpenPayments_Click(object sender, RoutedEventArgs e) => OpenPaymentsRequested?.Invoke(this, EventArgs.Empty);
    private void OpenDiamonds_Click(object sender, RoutedEventArgs e) => OpenDiamondHoldsRequested?.Invoke(this, EventArgs.Empty);
    private void OpenDiamondSearch_Click(object sender, RoutedEventArgs e) => OpenDiamondSearchRequested?.Invoke(this, EventArgs.Empty);
    private void OpenCustomers_Click(object sender, RoutedEventArgs e) => OpenCustomersRequested?.Invoke(this, EventArgs.Empty);
    private void OpenJobs_Click(object sender, RoutedEventArgs e) => OpenJobsRequested?.Invoke(this, EventArgs.Empty);

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        if (IsHostedInTab) CloseRequested?.Invoke(this, EventArgs.Empty);
        else Close();
    }

    private sealed class ProjectWorkbenchRow
    {
        public string Area { get; set; } = string.Empty;
        public int Priority { get; set; } = 3;
        public string PriorityLabel { get; set; } = "Medium";
        public string Subject { get; set; } = string.Empty;
        public string NextAction { get; set; } = string.Empty;
        public string DueLine { get; set; } = string.Empty;
        public string ValueLine { get; set; } = string.Empty;
        public string Context { get; set; } = string.Empty;
        public string Risk { get; set; } = string.Empty;
        public string SuggestedMessage { get; set; } = string.Empty;
        public string PrimaryActionKey { get; set; } = "Quote";
        public int? QuoteId { get; set; }
        public int? JobId { get; set; }
        public int? CustomerId { get; set; }
        public int? ExternalDiamondId { get; set; }
        public string StableKey => $"{Area}:{QuoteId}:{JobId}:{CustomerId}:{ExternalDiamondId}:{Subject}";
    }
}
