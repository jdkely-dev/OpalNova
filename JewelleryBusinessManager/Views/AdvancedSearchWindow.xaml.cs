using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using JewelleryBusinessManager.Data;
using JewelleryBusinessManager.Models;
using JewelleryBusinessManager.Services;
using MessageBox = System.Windows.MessageBox;

namespace JewelleryBusinessManager.Views;

public partial class AdvancedSearchWindow : Window
{
    private sealed record SearchResultEntry(string Section, int Id, string Title, string Summary, object Source);

    private static readonly string[] Sections =
    {
        "All Sections", "Customers", "Suppliers", "Materials", "Material Transactions", "Opal Parcels", "Stones", "Jewellery Stock",
        "Jobs", "Sales", "Payments", "Market Events", "Market Stock", "Production Batches", "Batch Items", "Online Listings",
        "Purchase Orders", "Purchase Order Items", "Tasks", "Photos"
    };

    private static readonly string[] Filters =
    {
        "All Records", "Low Stock", "Jobs Due Soon", "Overdue Jobs", "Needs Photos", "At Market", "Reserved Stock", "Ready To List",
        "Needs Listing Work", "Overdue Tasks", "Due Today", "High Priority", "Open Purchase Orders", "Open Jobs"
    };

    public AdvancedSearchWindow(string initialSection = "All Sections", string initialPreset = "All Records", string initialKeyword = "")
    {
        InitializeComponent();
        SectionCombo.ItemsSource = Sections;
        FilterCombo.ItemsSource = Filters;
        SectionCombo.SelectedItem = Sections.Contains(initialSection) ? initialSection : "All Sections";
        FilterCombo.SelectedItem = Filters.Contains(initialPreset) ? initialPreset : "All Records";
        KeywordBox.Text = initialKeyword ?? string.Empty;
        LoadSavedViews();
        KeywordBox.Focus();
        KeywordBox.SelectAll();
        RunSearch();
    }

    private void LoadSavedViews()
    {
        SavedViewsCombo.ItemsSource = null;
        SavedViewsCombo.ItemsSource = SavedViewService.LoadViews();
    }

    private void Search_Click(object sender, RoutedEventArgs e) => RunSearch();

    private void RunSearch()
    {
        try
        {
            using var db = new AppDbContext();
            var section = SectionCombo.SelectedItem?.ToString() ?? "All Sections";
            var keyword = KeywordBox.Text.Trim();
            var preset = FilterCombo.SelectedItem?.ToString() ?? "All Records";
            var results = new List<SearchResultEntry>();

            void AddSection<T>(string name, IEnumerable<T> records) where T : BaseEntity
            {
                if (section != "All Sections" && section != name) return;
                foreach (var record in records.Cast<object>().Where(r => MatchesKeyword(r, keyword)).Where(r => MatchesPreset(name, r, preset)))
                {
                    var id = (int)(record.GetType().GetProperty("Id")?.GetValue(record) ?? 0);
                    results.Add(new SearchResultEntry(name, id, BuildTitle(record), BuildSummary(record), record));
                }
            }

            AddSection("Customers", db.Customers.AsNoTracking().AsEnumerable());
            AddSection("Suppliers", db.Suppliers.AsNoTracking().AsEnumerable());
            AddSection("Materials", db.Materials.AsNoTracking().AsEnumerable());
            AddSection("Material Transactions", db.MaterialTransactions.AsNoTracking().AsEnumerable());
            AddSection("Opal Parcels", db.OpalParcels.AsNoTracking().AsEnumerable());
            AddSection("Stones", db.Stones.AsNoTracking().AsEnumerable());
            AddSection("Jewellery Stock", db.JewelleryItems.AsNoTracking().AsEnumerable());
            AddSection("Jobs", db.Jobs.AsNoTracking().AsEnumerable());
            AddSection("Sales", db.Sales.AsNoTracking().AsEnumerable());
            AddSection("Payments", db.Payments.AsNoTracking().AsEnumerable());
            AddSection("Market Events", db.MarketEvents.AsNoTracking().AsEnumerable());
            AddSection("Market Stock", db.MarketStocks.AsNoTracking().AsEnumerable());
            AddSection("Production Batches", db.ProductionBatches.AsNoTracking().AsEnumerable());
            AddSection("Batch Items", db.ProductionBatchItems.AsNoTracking().AsEnumerable());
            AddSection("Online Listings", db.OnlineListings.AsNoTracking().AsEnumerable());
            AddSection("Purchase Orders", db.PurchaseOrders.AsNoTracking().AsEnumerable());
            AddSection("Purchase Order Items", db.PurchaseOrderItems.AsNoTracking().AsEnumerable());
            AddSection("Tasks", db.BusinessTasks.AsNoTracking().AsEnumerable());
            AddSection("Photos", db.PhotoRecords.AsNoTracking().AsEnumerable());

            var ordered = results.OrderBy(r => r.Section).ThenBy(r => r.Title).Take(500).ToList();
            ResultsGrid.ItemsSource = ordered;
            ResultSummaryText.Text = $"{ordered.Count} result(s) shown" + (results.Count > ordered.Count ? $" from {results.Count} matches. Narrow the search to see more." : ".");
            StatusText.Text = $"Search complete • Section: {section} • Filter: {preset}";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Advanced search failed.\n\n{ex.Message}", "Advanced Search", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static bool MatchesKeyword(object record, string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword)) return true;
        if ((record.ToString() ?? string.Empty).Contains(keyword, StringComparison.OrdinalIgnoreCase)) return true;
        return record.GetType().GetProperties()
            .Any(p => (p.GetValue(record)?.ToString() ?? string.Empty).Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }

    private static bool MatchesPreset(string section, object record, string preset)
    {
        if (string.IsNullOrWhiteSpace(preset) || preset == "All Records") return true;
        var today = DateTime.Today;
        return preset switch
        {
            "Low Stock" => section == "Materials" && record is Material m && m.CurrentQuantity <= m.ReorderLevel,
            "Jobs Due Soon" => section == "Jobs" && record is Job j && j.Status != JobStatus.Completed && j.Status != JobStatus.Cancelled && j.DueDate.HasValue && j.DueDate.Value.Date <= today.AddDays(14),
            "Overdue Jobs" => section == "Jobs" && record is Job j && j.Status != JobStatus.Completed && j.Status != JobStatus.Cancelled && j.DueDate.HasValue && j.DueDate.Value.Date < today,
            "Needs Photos" => section == "Jewellery Stock" && record is JewelleryItem ji && ji.Status == StockStatus.NeedsPhotos,
            "At Market" => section == "Jewellery Stock" && record is JewelleryItem ji && ji.Status == StockStatus.AtMarket,
            "Reserved Stock" => section == "Jewellery Stock" && record is JewelleryItem ji && ji.Status == StockStatus.Reserved,
            "Ready To List" => section == "Online Listings" && record is OnlineListing l && (l.Status == OnlineListingStatus.ReadyToList || (l.PhotosDone && l.DescriptionDone && l.PriceChecked && !l.ListedOnline)),
            "Needs Listing Work" => section == "Online Listings" && record is OnlineListing l && (!l.PhotosDone || !l.DescriptionDone || !l.PriceChecked || !l.ListedOnline),
            "Overdue Tasks" => section == "Tasks" && record is BusinessTask t && t.IsOverdue,
            "Due Today" => section == "Tasks" && record is BusinessTask t && t.IsOpen && t.DueDate.HasValue && t.DueDate.Value.Date == today,
            "High Priority" => section == "Tasks" && record is BusinessTask t && t.IsOpen && (t.Priority == BusinessTaskPriority.High || t.Priority == BusinessTaskPriority.Urgent),
            "Open Purchase Orders" => section == "Purchase Orders" && record is PurchaseOrder p && p.Status is PurchaseOrderStatus.Draft or PurchaseOrderStatus.Ordered or PurchaseOrderStatus.PartiallyReceived,
            "Open Jobs" => section == "Jobs" && record is Job oj && oj.Status != JobStatus.Completed && oj.Status != JobStatus.Cancelled,
            _ => true
        };
    }

    private static string BuildTitle(object record) => record switch
    {
        Customer c => c.FullName,
        Supplier s => s.Name,
        Material m => $"{m.MaterialCode} {m.Name}".Trim(),
        Stone s => $"{s.StoneCode} {s.StoneType}".Trim(),
        OpalParcel p => p.ParcelCode,
        JewelleryItem j => $"{j.StockCode} {j.Name}".Trim(),
        Job j => $"{j.JobCode} {j.JobTitle}".Trim(),
        Sale s => $"Sale #{s.Id} {s.SaleAmount:C}",
        Payment p => $"Payment #{p.Id} {p.Amount:C}",
        MarketEvent m => m.Name,
        MarketStock m => $"Market Stock #{m.Id}",
        ProductionBatch b => $"{b.BatchCode} {b.Name}".Trim(),
        ProductionBatchItem i => i.ItemName,
        OnlineListing l => string.IsNullOrWhiteSpace(l.SeoTitle) ? $"Listing #{l.Id}" : l.SeoTitle,
        PurchaseOrder p => p.PurchaseOrderCode,
        PurchaseOrderItem i => i.ItemName,
        BusinessTask t => $"{t.TaskCode} {t.Title}".Trim(),
        PhotoRecord p => string.IsNullOrWhiteSpace(p.Caption) ? $"Photo #{p.Id}" : p.Caption,
        _ => record.ToString() ?? record.GetType().Name
    };

    private static string BuildSummary(object record) => record switch
    {
        Material m => $"{m.Category} • {m.CurrentQuantity:N2} {m.UnitType} • Reorder {m.ReorderLevel:N2}",
        JewelleryItem j => $"{j.Type} • {j.Status} • {j.RetailPrice:C}",
        Job j => $"{j.Type} • {j.Status} • Due {(j.DueDate.HasValue ? j.DueDate.Value.ToShortDateString() : "not set")}",
        Stone s => $"{s.Status} • {s.WeightCarats:N2} ct • {s.EstimatedValue:C}",
        OnlineListing l => $"{l.Platform} • {l.Status} • Photos {(l.PhotosDone ? "done" : "needed")}",
        BusinessTask t => $"{t.Status} • {t.Priority} • Due {(t.DueDate.HasValue ? t.DueDate.Value.ToShortDateString() : "not set")}",
        PurchaseOrder p => $"{p.Status} • {p.TotalCost:C} • Expected {(p.ExpectedDeliveryDate.HasValue ? p.ExpectedDeliveryDate.Value.ToShortDateString() : "not set")}",
        MarketEvent m => $"{m.EventDate:d} • {m.Location} • Net {m.NetMarketProfit:C}",
        Sale s => $"{s.SaleDate:d} • {s.PaymentMethod} • Profit {s.Profit:C}",
        _ => record.ToString() ?? string.Empty
    };

    private void OpenSelected_Click(object sender, RoutedEventArgs e) => OpenSelectedResult();
    private void ResultsGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e) => OpenSelectedResult();

    private void OpenSelectedResult()
    {
        if (ResultsGrid.SelectedItem is not SearchResultEntry result)
        {
            MessageBox.Show("Select a result first.", "Advanced Search", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (Owner is JewelleryBusinessManager.MainWindow main)
        {
            main.OpenSearchResult(result.Section, result.Id);
            Close();
        }
    }

    private void SaveView_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            SavedViewService.SaveView(new SavedSearchView
            {
                Name = SavedViewNameBox.Text.Trim(),
                Section = SectionCombo.SelectedItem?.ToString() ?? "All Sections",
                SearchText = KeywordBox.Text.Trim(),
                FilterPreset = FilterCombo.SelectedItem?.ToString() ?? "All Records",
                UpdatedAt = DateTime.Now
            });
            LoadSavedViews();
            StatusText.Text = "Saved view updated.";
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Saved Views", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void ApplySavedView_Click(object sender, RoutedEventArgs e)
    {
        if (SavedViewsCombo.SelectedItem is not SavedSearchView view)
        {
            MessageBox.Show("Select a saved view first.", "Saved Views", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (view.Section == "All Sections")
        {
            SectionCombo.SelectedItem = view.Section;
            KeywordBox.Text = view.SearchText;
            FilterCombo.SelectedItem = view.FilterPreset;
            RunSearch();
            return;
        }
        if (Owner is JewelleryBusinessManager.MainWindow main)
        {
            main.ApplySavedView(view.Section, view.SearchText, view.FilterPreset);
            Close();
        }
    }

    private void DeleteSavedView_Click(object sender, RoutedEventArgs e)
    {
        if (SavedViewsCombo.SelectedItem is not SavedSearchView view) return;
        SavedViewService.DeleteView(view.Name);
        LoadSavedViews();
        StatusText.Text = "Saved view deleted.";
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
