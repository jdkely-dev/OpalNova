using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using JewelleryBusinessManager.Data;
using JewelleryBusinessManager.Models;
using JewelleryBusinessManager.Services;

namespace JewelleryBusinessManager.Views;

public partial class SupplierDiamondWorkflowWindow : Window
{
    public bool IsHostedInTab { get; set; }
    public event EventHandler? OpenSavedRecordsRequested;
    public event EventHandler? CloseRequested;

    private sealed record DiamondWorkflowRow(
        int ExternalDiamondId,
        string Status,
        string DiamondSummary,
        string CertificateNumber,
        decimal SupplierPrice,
        string Currency,
        string QuoteCode,
        string Customer,
        string OptionName,
        string SupplierReference,
        DateTime? HoldExpiresAt,
        DateTime? ExpectedArrivalDate,
        DateTime? ReceivedAt,
        string OwnedStoneCode,
        string Notes);

    private List<DiamondWorkflowRow> _rows = new();
    private bool _ready;

    public SupplierDiamondWorkflowWindow()
    {
        InitializeComponent();
        FilterCombo.SelectedIndex = 0;
        _ready = true;
        Loaded += (_, _) => LoadRows();
    }

    private void LoadRows()
    {
        using var db = new AppDbContext();
        var customers = db.Customers.AsNoTracking().ToDictionary(x => x.Id, x => x.FullName);
        var quotes = db.CustomQuotes.AsNoTracking().ToDictionary(x => x.Id, x => x);
        var options = db.QuoteOptions.AsNoTracking().ToDictionary(x => x.Id, x => x);
        var linksByDiamond = db.QuoteOptionExternalDiamondLinks.AsNoTracking()
            .AsEnumerable()
            .GroupBy(x => x.ExternalDiamondId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.UpdatedAt).ToList());

        var now = DateTime.Now;
        var diamonds = db.ExternalDiamonds.AsNoTracking().AsEnumerable().ToList();
        var ownedStoneCodes = diamonds.ToDictionary(x => x.Id, x => ExternalDiamondInventoryService.FindOwnedStoneCode(db, x));
        _rows = diamonds.Select(d =>
        {
            linksByDiamond.TryGetValue(d.Id, out var links);
            var link = links?.FirstOrDefault();
            QuoteOption? option = null;
            CustomQuote? quote = null;
            if (link != null && options.TryGetValue(link.QuoteOptionId, out option))
                quotes.TryGetValue(option.CustomQuoteId, out quote);

            var status = NormaliseStatus(d);
            if ((status == "Hold Requested" || status == "Hold Confirmed") && d.HoldExpiresAt.HasValue && d.HoldExpiresAt.Value <= now.AddHours(8))
                status = d.HoldExpiresAt.Value < now ? "Expired" : "Hold Expiring";

            var customer = quote?.CustomerId is int customerId && customers.TryGetValue(customerId, out var name) ? name : string.Empty;
            return new DiamondWorkflowRow(
                d.Id,
                status,
                BuildSummary(d),
                d.CertificateNumber,
                d.SupplierPrice,
                d.Currency,
                quote?.QuoteCode ?? string.Empty,
                customer,
                option?.OptionName ?? string.Empty,
                d.SupplierReference,
                d.HoldExpiresAt,
                d.ExpectedArrivalDate,
                d.ReceivedAt,
                ownedStoneCodes.TryGetValue(d.Id, out var stoneCode) ? stoneCode : string.Empty,
                d.Notes ?? string.Empty);
        }).OrderBy(x => SortRank(x.Status)).ThenBy(x => x.HoldExpiresAt ?? DateTime.MaxValue).ThenBy(x => x.DiamondSummary).ToList();

        ExpiringCountText.Text = _rows.Count(x => x.Status is "Hold Expiring" or "Expired").ToString();
        NotOrderedCountText.Text = _rows.Count(x => IsApprovedButNotOrdered(x.Status)).ToString();
        OrderedCountText.Text = _rows.Count(x => (x.Status == "Order Requested" || x.Status == "Ordered") && !x.ReceivedAt.HasValue).ToString();
        ReceivedCountText.Text = _rows.Count(x => x.Status is "Received" or "Converted To Owned Inventory").ToString();
        ApplyFilter();
    }

    private static string BuildSummary(ExternalDiamond d)
    {
        var type = d.IsLabGrown ? "LG" : "Natural";
        return $"{type} {d.Shape} {d.Carat:0.###}ct {d.Color} {d.Clarity} {d.Cut} {d.Lab}".Replace("  ", " ").Trim();
    }

    private static string NormaliseStatus(ExternalDiamond d)
    {
        if (string.IsNullOrWhiteSpace(d.Status) || d.Status is "Saved" or "Search Result")
            return "Saved";
        return d.Status;
    }

    private static bool IsApprovedButNotOrdered(string status) => status is "Customer Interested" or "Hold Requested" or "Hold Confirmed" or "Hold Expiring" or "Expired";

    private static int SortRank(string status) => status switch
    {
        "Expired" => 0,
        "Hold Expiring" => 1,
        "Customer Interested" => 2,
        "Hold Requested" => 3,
        "Hold Confirmed" => 4,
            "Order Requested" => 5,
            "Ordered" => 6,
            "Received" => 7,
            "Converted To Owned Inventory" => 8,
            "Declined" or "Released" => 9,
            _ => 9
        };

    private void ApplyFilter()
    {
        if (!_ready || DiamondsGrid == null) return;
        IEnumerable<DiamondWorkflowRow> query = _rows;
        var filter = (FilterCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Action needed";
        query = filter switch
        {
            "Holds expiring" => query.Where(x => x.Status is "Hold Expiring" or "Expired"),
            "Ordered not received" => query.Where(x => (x.Status == "Order Requested" || x.Status == "Ordered") && !x.ReceivedAt.HasValue),
            "Customer approved not ordered" => query.Where(x => IsApprovedButNotOrdered(x.Status)),
            "Received" => query.Where(x => x.Status is "Received" or "Converted To Owned Inventory"),
            "All saved external diamonds" => query,
            _ => query.Where(x => x.Status is "Customer Interested" or "Hold Requested" or "Hold Confirmed" or "Hold Expiring" or "Expired" or "Order Requested" or "Ordered")
        };

        var search = SearchBox.Text.Trim();
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(x => $"{x.Status} {x.DiamondSummary} {x.CertificateNumber} {x.QuoteCode} {x.Customer} {x.SupplierReference} {x.OwnedStoneCode}".Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        var list = query.ToList();
        DiamondsGrid.ItemsSource = list;
        StatusText.Text = $"{list.Count} supplier diamond record(s) shown.";
        if (list.Count > 0 && DiamondsGrid.SelectedItem == null)
            DiamondsGrid.SelectedIndex = 0;
    }

    private DiamondWorkflowRow? SelectedRow() => DiamondsGrid.SelectedItem as DiamondWorkflowRow;

    private void DiamondsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var row = SelectedRow();
        if (row == null)
        {
            SelectedDiamondText.Text = "Select a diamond row to manage supplier actions.";
            return;
        }

        var ownedLine = string.IsNullOrWhiteSpace(row.OwnedStoneCode) ? string.Empty : $"\nOwned stone: {row.OwnedStoneCode}";
        SelectedDiamondText.Text = $"{row.DiamondSummary}\nCert: {row.CertificateNumber}\nQuote: {row.QuoteCode} {row.OptionName}\nCustomer: {row.Customer}\nStatus: {row.Status}{ownedLine}";
        SupplierReferenceBox.Text = row.SupplierReference;
        HoldExpiryPicker.SelectedDate = row.HoldExpiresAt?.Date;
        ExpectedArrivalPicker.SelectedDate = row.ExpectedArrivalDate?.Date;
        ActionNotesBox.Text = row.Notes;
    }

    private void RequestHold_Click(object sender, RoutedEventArgs e) => UpdateSelected("Hold Requested", d =>
    {
        d.HoldRequestedAt = DateTime.Now;
        d.HoldExpiresAt = EndOfDay(HoldExpiryPicker.SelectedDate) ?? DateTime.Now.AddHours(24);
        AppendNote(d, "Hold requested");
    });

    private void ConfirmHold_Click(object sender, RoutedEventArgs e) => UpdateSelected("Hold Confirmed", d =>
    {
        d.HoldConfirmedAt = DateTime.Now;
        d.HoldExpiresAt = EndOfDay(HoldExpiryPicker.SelectedDate) ?? d.HoldExpiresAt ?? DateTime.Now.AddHours(24);
        AppendNote(d, "Hold confirmed");
    });

    private void OrderDiamond_Click(object sender, RoutedEventArgs e) => UpdateSelected("Ordered", d =>
    {
        d.OrderRequestedAt ??= DateTime.Now;
        d.OrderedAt = DateTime.Now;
        d.ExpectedArrivalDate = ExpectedArrivalPicker.SelectedDate;
        AppendNote(d, "Diamond ordered / order requested");
    });

    private void MarkReceived_Click(object sender, RoutedEventArgs e) => UpdateSelected("Received", d =>
    {
        d.ReceivedAt = DateTime.Now;
        d.ExpectedArrivalDate ??= DateTime.Today;
        AppendNote(d, "Diamond received");
    });

    private void ConvertToInventory_Click(object sender, RoutedEventArgs e)
    {
        var row = SelectedRow();
        if (row == null)
        {
            MessageBox.Show("Select an external diamond first.", "Supplier Diamond Workflow", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (row.Status != "Received" && row.Status != "Converted To Owned Inventory" && !row.ReceivedAt.HasValue)
        {
            MessageBox.Show("Mark the supplier diamond as received before converting it into owned inventory.", "Convert To Inventory", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var prompt = string.IsNullOrWhiteSpace(row.OwnedStoneCode)
            ? $"Create an owned loose-stone record from {row.DiamondSummary}?"
            : $"This supplier diamond already appears linked to owned stone {row.OwnedStoneCode}. Re-link status now?";
        if (MessageBox.Show(prompt, "Convert Supplier Diamond", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;

        try
        {
            var result = ExternalDiamondInventoryService.ConvertReceivedDiamondToOwnedStone(row.ExternalDiamondId);
            LoadRows();
            StatusText.Text = result.Message;
            MessageBox.Show(result.Message, "Convert To Inventory", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Convert To Inventory", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ReleaseDiamond_Click(object sender, RoutedEventArgs e) => UpdateSelected("Released", d =>
    {
        d.ReleasedAt = DateTime.Now;
        AppendNote(d, "Diamond declined / released");
    });

    private void UpdateSelected(string status, Action<ExternalDiamond> update)
    {
        var row = SelectedRow();
        if (row == null)
        {
            MessageBox.Show("Select an external diamond first.", "Supplier Diamond Workflow", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        using var db = new AppDbContext();
        var diamond = db.ExternalDiamonds.FirstOrDefault(x => x.Id == row.ExternalDiamondId);
        if (diamond == null)
        {
            MessageBox.Show("This external diamond could not be found.", "Supplier Diamond Workflow", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        diamond.Status = status;
        diamond.SupplierReference = SupplierReferenceBox.Text.Trim();
        diamond.UpdatedAt = DateTime.Now;
        update(diamond);

        var links = db.QuoteOptionExternalDiamondLinks.Where(x => x.ExternalDiamondId == diamond.Id).ToList();
        foreach (var link in links)
        {
            link.LinkStatus = status;
            link.UpdatedAt = DateTime.Now;
        }

        db.SaveChanges();
        LoadRows();
        StatusText.Text = $"Updated {BuildSummary(diamond)} to {status}.";
    }


    private static DateTime? EndOfDay(DateTime? date) => date?.Date.AddHours(23).AddMinutes(59);

    private void AppendNote(ExternalDiamond diamond, string action)
    {
        var note = ActionNotesBox.Text.Trim();
        var stamped = $"[{DateTime.Now:g}] {action}" + (string.IsNullOrWhiteSpace(note) ? string.Empty : $": {note}");
        diamond.Notes = string.IsNullOrWhiteSpace(diamond.Notes) ? stamped : diamond.Notes + Environment.NewLine + stamped;
    }

    private void CreateReminderTask_Click(object sender, RoutedEventArgs e)
    {
        var row = SelectedRow();
        if (row == null)
        {
            MessageBox.Show("Select an external diamond first.", "Supplier Diamond Workflow", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        using var db = new AppDbContext();
        var task = new BusinessTask
        {
            TaskCode = $"DIA-{DateTime.Now:yyyyMMdd-HHmm}",
            Title = $"Supplier diamond follow-up — {row.DiamondSummary}",
            Category = BusinessTaskCategory.Purchasing,
            Priority = row.Status is "Expired" or "Hold Expiring" or "Customer Interested" ? BusinessTaskPriority.High : BusinessTaskPriority.Normal,
            Status = BusinessTaskStatus.ToDo,
            DueDate = row.HoldExpiresAt?.Date ?? row.ExpectedArrivalDate?.Date ?? DateTime.Today.AddDays(1),
            ReminderDate = DateTime.Today,
            Description = $"External diamond {row.DiamondSummary}\nCertificate: {row.CertificateNumber}\nQuote: {row.QuoteCode}\nCustomer: {row.Customer}\nCurrent status: {row.Status}\nSupplier reference: {row.SupplierReference}\nNotes: {ActionNotesBox.Text.Trim()}",
            ShowOnDashboard = true
        };
        db.BusinessTasks.Add(task);
        db.SaveChanges();
        StatusText.Text = $"Created reminder task {task.TaskCode}.";
        MessageBox.Show($"Reminder task {task.TaskCode} created.", "Supplier Diamond Workflow", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void Refresh_Click(object sender, RoutedEventArgs e) => LoadRows();
    private void FilterCombo_SelectionChanged(object sender, SelectionChangedEventArgs e) => ApplyFilter();
    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilter();
    private void SavedRecords_Click(object sender, RoutedEventArgs e)
    {
        if (IsHostedInTab)
        {
            OpenSavedRecordsRequested?.Invoke(this, EventArgs.Empty);
            return;
        }

        DialogResult = true;
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        if (IsHostedInTab)
        {
            CloseRequested?.Invoke(this, EventArgs.Empty);
            return;
        }

        Close();
    }
}
