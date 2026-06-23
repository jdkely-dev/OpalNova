using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using JewelleryBusinessManager.Data;
using JewelleryBusinessManager.Models;
using JewelleryBusinessManager.Services;
using Microsoft.EntityFrameworkCore;

namespace JewelleryBusinessManager.Views;

public partial class ProposalPipelineWindow : Window
{
    public bool IsHostedInTab { get; set; }
    public event EventHandler<int>? OpenQuoteRequested;
    public event EventHandler? CloseRequested;

    private readonly List<ProposalPipelineRow> _rows = new();
    private bool _ready;
    private bool _hasLoadedRows;
    private ProposalPipelineRow? _selectedRow;

    public ProposalPipelineWindow()
    {
        InitializeComponent();
        _ready = true;
        Loaded += ProposalPipelineWindow_Loaded;
        PipelineRoot.Loaded += ProposalPipelineWindow_Loaded;
    }

    private void ProposalPipelineWindow_Loaded(object sender, RoutedEventArgs e)
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
        var quotes = db.CustomQuotes.AsNoTracking().OrderByDescending(x => x.UpdatedAt).ToList();
        var customers = db.Customers.AsNoTracking().ToDictionary(x => x.Id, x => x);
        var optionsByQuote = db.QuoteOptions.AsNoTracking()
            .AsEnumerable()
            .GroupBy(x => x.CustomQuoteId)
            .ToDictionary(x => x.Key, x => x.OrderByDescending(o => o.IsRecommended).ThenByDescending(o => o.TotalPrice).ToList());

        _rows.Clear();
        foreach (var quote in quotes)
        {
            optionsByQuote.TryGetValue(quote.Id, out var options);
            var displayOption = PickDisplayOption(quote, options ?? new List<QuoteOption>());
            var customer = quote.CustomerId.HasValue && customers.TryGetValue(quote.CustomerId.Value, out var foundCustomer) ? foundCustomer : null;
            _rows.Add(BuildRow(quote, customer, displayOption));
        }

        ApplyFilter();
    }

    private static QuoteOption? PickDisplayOption(CustomQuote quote, IReadOnlyList<QuoteOption> options)
    {
        if (options.Count == 0) return null;
        if (quote.AcceptedOptionId.HasValue)
        {
            var accepted = options.FirstOrDefault(x => x.Id == quote.AcceptedOptionId.Value);
            if (accepted != null) return accepted;
        }

        return options.FirstOrDefault(x => x.IsRecommended) ?? options.OrderByDescending(x => x.TotalPrice).FirstOrDefault();
    }

    private static ProposalPipelineRow BuildRow(CustomQuote quote, Customer? customer, QuoteOption? option)
    {
        var accepted = quote.AcceptedOptionId.HasValue || quote.ProposalStatus.Equals("Accepted", StringComparison.OrdinalIgnoreCase);
        var converted = quote.LinkedJobId.HasValue || quote.ProposalStatus.Equals("Converted to Job", StringComparison.OrdinalIgnoreCase);
        var prepared = quote.ProposalLastGeneratedAt.HasValue || !string.IsNullOrWhiteSpace(quote.ProposalLastPath) || quote.ProposalStatus.Equals("Prepared", StringComparison.OrdinalIgnoreCase);
        var sent = quote.ProposalSentAt.HasValue || quote.ProposalStatus.Contains("Sent", StringComparison.OrdinalIgnoreCase);
        var followUpDue = sent && !accepted && !converted && quote.ProposalFollowUpDueAt.HasValue && quote.ProposalFollowUpDueAt.Value.Date <= DateTime.Today;
        var expired = !accepted && !converted && quote.ValidUntil.HasValue && quote.ValidUntil.Value.Date < DateTime.Today;

        var stage = converted ? "Converted"
            : accepted ? "Accepted"
            : followUpDue ? "Follow-up Due"
            : sent ? "Sent"
            : prepared ? "Prepared"
            : "Not Sent";

        var action = stage switch
        {
            "Converted" => "Monitor production and payment handover.",
            "Accepted" => "Create or review the linked production job.",
            "Follow-up Due" => "Follow up with the customer and record the outcome.",
            "Sent" => "Wait for customer reply or create a dated follow-up.",
            "Prepared" => "Open the quote and use Send / Record to send the proposal.",
            _ when expired => "Refresh the quote before preparing or sending a proposal.",
            _ => "Preview the proposal, then prepare the customer message."
        };

        var risk = followUpDue ? "Follow-up is due now."
            : expired ? "Quote validity has expired."
            : prepared && !sent ? "Proposal is prepared but not recorded as sent."
            : string.Empty;

        var priorityRank = followUpDue || expired ? 1
            : prepared && !sent ? 2
            : accepted && !converted ? 2
            : 3;

        var followUpLine = quote.ProposalFollowUpDueAt.HasValue ? quote.ProposalFollowUpDueAt.Value.ToString("dd MMM yyyy") : "Not set";
        var valueLine = option == null ? string.Empty : option.TotalPrice.ToString("C");
        var customerName = customer?.FullName ?? "No customer linked";
        var recipient = FirstText(quote.ProposalEmailTo, customer?.Email);
        var title = string.IsNullOrWhiteSpace(quote.Title) ? "Untitled quote" : quote.Title;
        var fileAvailable = !string.IsNullOrWhiteSpace(quote.ProposalLastPath) && File.Exists(quote.ProposalLastPath);
        var emailAvailable = !string.IsNullOrWhiteSpace(quote.ProposalEmailSubject) || !string.IsNullOrWhiteSpace(quote.ProposalEmailMessage);
        var contextLine = BuildContextLine(quote);

        return new ProposalPipelineRow(
            quote.Id,
            quote.QuoteCode,
            title,
            customerName,
            recipient,
            stage,
            PriorityLabel(priorityRank),
            priorityRank,
            action,
            risk,
            followUpLine,
            valueLine,
            contextLine,
            quote.Status,
            quote.ProposalStatus,
            quote.ValidUntil,
            quote.ProposalLastGeneratedAt,
            quote.ProposalSentAt,
            quote.ProposalFollowUpDueAt,
            quote.ProposalLastPath ?? string.Empty,
            recipient,
            quote.ProposalEmailSubject ?? string.Empty,
            quote.ProposalEmailMessage ?? string.Empty,
            option?.OptionName ?? string.Empty,
            option?.TotalPrice ?? 0m,
            fileAvailable,
            emailAvailable,
            priorityRank <= 2);
    }

    private static string BuildContextLine(CustomQuote quote)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(quote.Occasion)) parts.Add($"Occasion: {quote.Occasion}");
        if (quote.RequiredBy.HasValue) parts.Add($"Required by: {quote.RequiredBy.Value:dd MMM yyyy}");
        if (!string.IsNullOrWhiteSpace(quote.RingSize)) parts.Add($"Ring size: {quote.RingSize}");
        if (!string.IsNullOrWhiteSpace(quote.BudgetRange)) parts.Add($"Budget: {quote.BudgetRange}");
        if (!string.IsNullOrWhiteSpace(quote.PreferredMetal)) parts.Add($"Metal: {quote.PreferredMetal}");
        if (!string.IsNullOrWhiteSpace(quote.PreferredStone)) parts.Add($"Stone: {quote.PreferredStone}");
        return string.Join(" | ", parts);
    }

    private static string FirstText(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
                return value.Trim();
        }

        return string.Empty;
    }

    private static string PriorityLabel(int priorityRank) => priorityRank switch
    {
        1 => "Urgent",
        2 => "High",
        _ => "Normal"
    };

    private string GetSelectedFilter()
    {
        if (FilterCombo.SelectedItem is ComboBoxItem item)
            return item.Content?.ToString() ?? "Action needed";
        return FilterCombo.SelectedItem?.ToString() ?? FilterCombo.Text ?? "Action needed";
    }

    private void ApplyFilter()
    {
        if (!_ready) return;

        IEnumerable<ProposalPipelineRow> query = _rows;
        var filter = GetSelectedFilter();
        query = filter switch
        {
            "Prepared not sent" => query.Where(x => x.Stage == "Prepared"),
            "Follow-up due" => query.Where(x => x.Stage == "Follow-up Due"),
            "Sent" => query.Where(x => x.Stage is "Sent" or "Follow-up Due"),
            "Accepted" => query.Where(x => x.Stage == "Accepted"),
            "Converted" => query.Where(x => x.Stage == "Converted"),
            "All proposals" => query,
            _ => query.Where(x => x.ActionNeeded)
        };

        var search = SearchBox.Text.Trim();
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(x =>
                $"{x.PriorityLabel} {x.Stage} {x.QuoteCode} {x.CustomerName} {x.Title} {x.EmailTo} {x.NextAction} {x.ProposalStatus} {x.QuoteStatus} {x.ContextLine}"
                    .Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        var selectedKey = _selectedRow?.StableKey;
        var rows = query
            .OrderBy(x => x.PriorityRank)
            .ThenBy(x => x.ProposalFollowUpDueAt ?? x.ValidUntil ?? DateTime.MaxValue)
            .ThenByDescending(x => x.ProposalSentAt ?? x.ProposalLastGeneratedAt ?? DateTime.MinValue)
            .ToList();

        UpdateCounts(rows);
        PipelineGrid.ItemsSource = rows;
        _selectedRow = rows.FirstOrDefault(x => x.StableKey == selectedKey) ?? rows.FirstOrDefault();
        PipelineGrid.SelectedItem = _selectedRow;
        var searchSuffix = string.IsNullOrWhiteSpace(search) ? string.Empty : $" - Search: {search}";
        StatusText.Text = $"Showing {rows.Count} of {_rows.Count} proposal(s) - Filter: {filter}{searchSuffix}";
        UpdateSelectedDetails();
    }

    private void UpdateCounts(IReadOnlyCollection<ProposalPipelineRow> visibleRows)
    {
        ActionNeededCountText.Text = visibleRows.Count(x => x.ActionNeeded).ToString();
        PreparedCountText.Text = visibleRows.Count(x => x.Stage == "Prepared").ToString();
        SentCountText.Text = visibleRows.Count(x => x.Stage is "Sent" or "Follow-up Due").ToString();
        FollowUpDueCountText.Text = visibleRows.Count(x => x.Stage == "Follow-up Due").ToString();
        AcceptedCountText.Text = visibleRows.Count(x => x.Stage == "Accepted").ToString();
        OpenValueText.Text = visibleRows.Where(x => x.Stage is not "Converted").Sum(x => x.OptionTotal).ToString("C0");
    }

    private void UpdateSelectedDetails()
    {
        var row = PipelineGrid.SelectedItem as ProposalPipelineRow ?? _selectedRow;
        _selectedRow = row;

        if (row == null)
        {
            SelectedTitleText.Text = "Select a proposal";
            SelectedSubtitleText.Text = "Choose a row to see proposal state, draft details and next step.";
            NextActionText.Text = "No proposal selected.";
            RiskText.Text = string.Empty;
            DetailText.Text = string.Empty;
            SetActionButtons(false);
            return;
        }

        SelectedTitleText.Text = $"{row.QuoteCode} - {row.Title}";
        SelectedSubtitleText.Text = $"{row.CustomerName} - {row.Stage} - {row.PriorityLabel} priority";
        NextActionText.Text = row.NextAction;
        RiskText.Text = row.Risk;
        DetailText.Text =
            $"Quote status: {row.QuoteStatus}\n" +
            $"Proposal status: {row.ProposalStatus}\n" +
            $"Valid until: {FormatDate(row.ValidUntil)}\n" +
            $"Generated: {FormatDateTime(row.ProposalLastGeneratedAt)}\n" +
            $"Sent: {FormatDateTime(row.ProposalSentAt)}\n" +
            $"Follow-up due: {row.FollowUpLine}\n" +
            $"Email to: {row.EmailTo}\n" +
            $"Project context: {(string.IsNullOrWhiteSpace(row.ContextLine) ? "Not recorded" : row.ContextLine)}\n" +
            $"Display option: {row.OptionName} {row.ValueLine}\n" +
            $"Proposal file: {(row.ProposalFileAvailable ? row.ProposalPath : "Not available")}";
        SetActionButtons(true);
    }

    private static string FormatDate(DateTime? value) => value.HasValue ? value.Value.ToString("dd MMM yyyy") : "Not set";
    private static string FormatDateTime(DateTime? value) => value.HasValue ? value.Value.ToString("dd MMM yyyy h:mm tt") : "Not recorded";

    private void SetActionButtons(bool hasSelection)
    {
        OpenQuoteButton.IsEnabled = hasSelection;
        OpenProposalButton.IsEnabled = hasSelection && _selectedRow?.ProposalFileAvailable == true;
        CopyDraftButton.IsEnabled = hasSelection && _selectedRow?.EmailDraftAvailable == true;
        CreateFollowUpButton.IsEnabled = hasSelection;
    }

    private void OpenQuote_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedRow == null)
        {
            MessageBox.Show("Select a proposal first.", "Proposal Pipeline", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        OpenQuoteRequested?.Invoke(this, _selectedRow.QuoteId);
    }

    private void OpenProposal_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedRow == null || !_selectedRow.ProposalFileAvailable)
        {
            MessageBox.Show("The selected proposal file is not available.", "Proposal Pipeline", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        Process.Start(new ProcessStartInfo(_selectedRow.ProposalPath) { UseShellExecute = true });
    }

    private void CopyDraft_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedRow == null || !_selectedRow.EmailDraftAvailable)
        {
            MessageBox.Show("The selected proposal does not have a recorded email draft.", "Proposal Pipeline", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var draft = $"To: {_selectedRow.EmailTo}\nSubject: {_selectedRow.EmailSubject}\n\n{_selectedRow.EmailMessage}\n\nProposal file: {_selectedRow.ProposalPath}".Trim();
        Clipboard.SetText(draft);
        StatusText.Text = $"Copied email draft for {_selectedRow.QuoteCode}.";
    }

    private void CreateFollowUp_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedRow == null)
        {
            MessageBox.Show("Select a proposal first.", "Proposal Pipeline", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        using var db = new AppDbContext();
        var quote = db.CustomQuotes.FirstOrDefault(x => x.Id == _selectedRow.QuoteId);
        if (quote == null)
        {
            MessageBox.Show("The selected quote could not be found.", "Proposal Pipeline", MessageBoxButton.OK, MessageBoxImage.Warning);
            LoadRows();
            return;
        }

        var title = quote.ProposalSentAt.HasValue || quote.ProposalStatus.Contains("Sent", StringComparison.OrdinalIgnoreCase)
            ? $"Follow up sent proposal {quote.QuoteCode}"
            : $"Send proposal {quote.QuoteCode}";
        var duplicate = db.BusinessTasks.AsNoTracking().AsEnumerable().Any(t =>
            t.IsOpen &&
            string.Equals(t.Title, title, StringComparison.OrdinalIgnoreCase) &&
            t.CustomerId == quote.CustomerId);
        if (duplicate)
        {
            MessageBox.Show("An open proposal follow-up already exists for this quote.", "Proposal Pipeline", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dueDate = quote.ProposalFollowUpDueAt?.Date ?? DateTime.Today.AddDays(2);
        if (!quote.ProposalFollowUpDueAt.HasValue)
            quote.ProposalFollowUpDueAt = dueDate;

        var task = new BusinessTask
        {
            TaskCode = TaskWorkflowService.GenerateTaskCode(),
            Title = title,
            Category = BusinessTaskCategory.CustomerFollowUp,
            Priority = dueDate <= DateTime.Today ? BusinessTaskPriority.High : BusinessTaskPriority.Normal,
            Status = BusinessTaskStatus.ToDo,
            DueDate = dueDate,
            ReminderDate = dueDate,
            CustomerId = quote.CustomerId,
            Description = $"{_selectedRow.NextAction}\n\nQuote: {quote.QuoteCode} {quote.Title}\nProposal file: {quote.ProposalLastPath}".Trim(),
            ShowOnDashboard = true
        };
        db.BusinessTasks.Add(task);
        db.SaveChanges();
        StatusText.Text = $"Created follow-up {task.TaskCode} for {quote.QuoteCode}.";
        LoadRows();
        MessageBox.Show($"Created follow-up task {task.TaskCode} due {task.DueDate:dd MMM yyyy}.", "Proposal Pipeline", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void PipelineGrid_SelectionChanged(object sender, SelectionChangedEventArgs e) => UpdateSelectedDetails();
    private void PipelineGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e) => OpenQuote_Click(sender, e);
    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilter();
    private void FilterCombo_SelectionChanged(object sender, SelectionChangedEventArgs e) => ApplyFilter();
    private void Refresh_Click(object sender, RoutedEventArgs e) => LoadRows();

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        if (IsHostedInTab) CloseRequested?.Invoke(this, EventArgs.Empty);
        else Close();
    }

    private sealed record ProposalPipelineRow(
        int QuoteId,
        string QuoteCode,
        string Title,
        string CustomerName,
        string EmailTo,
        string Stage,
        string PriorityLabel,
        int PriorityRank,
        string NextAction,
        string Risk,
        string FollowUpLine,
        string ValueLine,
        string ContextLine,
        string QuoteStatus,
        string ProposalStatus,
        DateTime? ValidUntil,
        DateTime? ProposalLastGeneratedAt,
        DateTime? ProposalSentAt,
        DateTime? ProposalFollowUpDueAt,
        string ProposalPath,
        string ProposalEmailTo,
        string EmailSubject,
        string EmailMessage,
        string OptionName,
        decimal OptionTotal,
        bool ProposalFileAvailable,
        bool EmailDraftAvailable,
        bool ActionNeeded)
    {
        public string StableKey => QuoteId.ToString();
    }
}
