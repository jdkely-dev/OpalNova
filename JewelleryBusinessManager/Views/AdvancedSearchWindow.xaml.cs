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
    private sealed record WorkflowCommand(string Title, string Summary, string TargetSection, string SearchText = "", string FilterPreset = "All Records", string Keywords = "");

    private static readonly string[] Sections =
    {
        "All Sections", "Customers", "Suppliers", "Materials", "Material Transactions", "Opal Parcels", "Stones", "Jewellery Stock",
        "Jobs", "Sales", "Payments", "Market Events", "Market Stock", "Production Batches", "Batch Items", "Online Listings",
        "Purchase Orders", "Purchase Order Items", "Tasks", "Custom Quotes", "Quote Options", "External Diamonds", "Photos", "Workflow Actions"
    };

    private static readonly string[] Filters =
    {
        "All Records", "Low Stock", "Jobs Due Soon", "Overdue Jobs", "Needs Photos", "At Market", "Reserved Stock", "Ready To List",
        "Needs Listing Work", "Overdue Tasks", "Due Today", "High Priority", "Open Purchase Orders", "Open Jobs", "Proposal Follow-Up Due", "Supplier Holds Expiring"
    };

    private static readonly WorkflowCommand[] WorkflowCommands =
    {
        new("Daily priorities", "Open Alert Centre for urgent quote, job, payment, stock, supplier diamond and follow-up alerts.", "Alert Centre", Keywords: "alerts overdue today next actions dashboard"),
        new("Project hub", "Open Project Workbench for quote, job, diamond, payment and follow-up workflow rows.", "Project Workbench", Keywords: "project workbench quote job diamond payment follow up"),
        new("Create or manage quotes", "Open Quotes & Proposals for custom quotes, proposal pipeline and customer proposal actions.", "Quotes & Proposals", Keywords: "quote proposal option comparison approval expiry"),
        new("Proposal pipeline", "Open Custom Workflow Studio where the Proposal Pipeline action is available.", "Custom Workflow Studio", Keywords: "proposal sent prepared follow-up accepted converted pipeline"),
        new("Payment and collection", "Open Payments & Sales for balances, receipts, handover and final collection workflow.", "Payments & Sales", Keywords: "payment balance invoice receipt pickup shipping collection handover"),
        new("Production board", "Open Production for jobs, production board, stage checklist and capacity snapshot.", "Production", Keywords: "production jobs stage checklist capacity workshop"),
        new("Inventory workflow", "Open Inventory for stock, stones, materials, reservations, status changes and stock movement.", "Inventory", Keywords: "inventory stock stone material reservation lifecycle"),
        new("Supplier diamonds", "Open Diamond Supplier Studio for Nivoda search, supplier holds, orders and saved external diamonds.", "Diamond Supplier Studio", Keywords: "nivoda diamond supplier hold order received replacement certificate"),
        new("Reports and charts", "Open Reports for BI, sales, profitability, tax, charts, stock ageing and exports.", "Reports", Keywords: "reports charts sales profit gst excel csv stock ageing"),
        new("Backups and restore", "Open Settings & Backup for backup, restore preview, health check and export bundle.", "Settings & Backup", Keywords: "backup restore health export bundle safety"),
        new("Data integrity check", "Open Settings & Backup where the read-only Data Integrity report is available.", "Settings & Backup", Keywords: "integrity orphan missing files links repair safety"),
        new("Release readiness", "Open Settings & Backup for packaging notes, validation gates and release checklist.", "Settings & Backup", Keywords: "release readiness installer shortcut packaging version update notes"),
        new("Customer relationship", "Open Customer Relationship Studio for timelines, summaries, templates and follow-ups.", "Customer Relationship Studio", Keywords: "customer timeline communication templates lifetime follow up"),
        new("Market operations", "Open Market Studio for prep, sale, reconciliation, packing lists and market reports.", "Market Studio", Keywords: "market pos sale reconcile packing"),
        new("Jeweller tools", "Open ring-size, metal-weight and stone-carat calculators for quick workshop estimates.", "Hardware & POS Studio", Keywords: "ring size metal weight stone carat calculator estimator casting setting"),
        new("Hardware and labels", "Open Hardware & POS Studio for DYMO labels, scan lookup, device capture and market operations.", "Hardware & POS Studio", Keywords: "dymo label barcode camera scale pos"),
        new("Cleanup tools", "Open Data Cleanup Studio for duplicate finding, missing data and bulk status tools.", "Data Cleanup Studio", Keywords: "duplicates cleanup missing data bulk status")
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
            AddSection("Custom Quotes", db.CustomQuotes.AsNoTracking().AsEnumerable());
            AddSection("Quote Options", db.QuoteOptions.AsNoTracking().AsEnumerable());
            AddSection("External Diamonds", db.ExternalDiamonds.AsNoTracking().AsEnumerable());
            AddSection("Photos", db.PhotoRecords.AsNoTracking().AsEnumerable());
            AddWorkflowCommands(section, keyword, results);

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
            "Proposal Follow-Up Due" => section == "Custom Quotes" && record is CustomQuote q && q.ProposalFollowUpDueAt.HasValue && q.ProposalFollowUpDueAt.Value.Date <= today && q.ProposalStatus != "Accepted" && q.ProposalStatus != "Converted",
            "Supplier Holds Expiring" => section == "External Diamonds" && record is ExternalDiamond d && d.HoldExpiresAt.HasValue && d.HoldExpiresAt.Value.Date <= today.AddDays(3) && d.ReleasedAt == null,
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
        CustomQuote q => $"{q.QuoteCode} {q.Title}".Trim(),
        QuoteOption o => o.OptionName,
        ExternalDiamond d => $"{d.SourceSystem} {d.Shape} {d.Carat:0.###}ct {d.Color} {d.Clarity} {d.CertificateNumber}".Trim(),
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
        CustomQuote q => $"{q.Status} • Proposal {q.ProposalStatus} • Follow-up {(q.ProposalFollowUpDueAt.HasValue ? q.ProposalFollowUpDueAt.Value.ToShortDateString() : "not set")}",
        QuoteOption o => $"Quote #{o.CustomQuoteId} • Total {o.TotalPrice:C} • Recommended {(o.IsRecommended ? "yes" : "no")}",
        ExternalDiamond d => $"{d.Status} • {d.Lab} {d.CertificateNumber} • {d.SupplierPrice:C} {d.Currency}",
        _ => record.ToString() ?? string.Empty
    };

    private static void AddWorkflowCommands(string section, string keyword, List<SearchResultEntry> results)
    {
        if (section != "All Sections" && section != "Workflow Actions") return;
        foreach (var command in WorkflowCommands.Where(c => MatchesWorkflowKeyword(c, keyword)))
            results.Add(new SearchResultEntry("Workflow Actions", 0, command.Title, command.Summary, command));
    }

    private static bool MatchesWorkflowKeyword(WorkflowCommand command, string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword)) return true;
        return command.Title.Contains(keyword, StringComparison.OrdinalIgnoreCase)
            || command.Summary.Contains(keyword, StringComparison.OrdinalIgnoreCase)
            || command.TargetSection.Contains(keyword, StringComparison.OrdinalIgnoreCase)
            || command.Keywords.Contains(keyword, StringComparison.OrdinalIgnoreCase);
    }

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
            if (result.Source is WorkflowCommand command)
            {
                main.OpenWorkflowCommand(command.TargetSection, command.SearchText, command.FilterPreset);
                Close();
                return;
            }

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
        if (view.Section is "All Sections" or "Workflow Actions")
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
