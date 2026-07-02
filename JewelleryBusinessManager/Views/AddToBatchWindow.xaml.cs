using System.Globalization;
using System.Windows;
using JewelleryBusinessManager.Data;
using JewelleryBusinessManager.Models;
using JewelleryBusinessManager.Services;
using MessageBox = System.Windows.MessageBox;
using MessageBoxButton = System.Windows.MessageBoxButton;
using MessageBoxImage = System.Windows.MessageBoxImage;
using MessageBoxResult = System.Windows.MessageBoxResult;

namespace JewelleryBusinessManager.Views;

public partial class AddToBatchWindow : Window
{
    private readonly object? _selected;

    public AddToBatchWindow(object? selected)
    {
        InitializeComponent();
        _selected = selected;
        LoadBatches();
        PrefillFromSelectedRecord();
    }

    private void LoadBatches()
    {
        using var db = new AppDbContext();
        var batches = new List<BatchOption> { new(0, "Select production batch") };
        batches.AddRange(db.ProductionBatches.AsEnumerable()
            .OrderBy(b => b.Status == ProductionBatchStatus.Completed)
            .ThenBy(b => b.TargetCompletionDate ?? DateTime.MaxValue)
            .ThenBy(b => b.Name)
            .Select(b => new BatchOption(b.Id, $"{b.BatchCode} {b.Name} - {b.Status}".Trim())));

        BatchCombo.ItemsSource = batches;
        BatchCombo.SelectedIndex = 0;
    }

    private void PrefillFromSelectedRecord()
    {
        SelectedRecordText.Text = _selected == null
            ? "Create a planned line item without linking it to an existing record."
            : $"Selected: {_selected}";

        switch (_selected)
        {
            case JewelleryItem item:
                ItemNameBox.Text = item.Name;
                ItemTypeBox.Text = item.Type.ToString();
                PlannedQuantityBox.Text = "1";
                EstimatedCostBox.Text = PricingService.CalculateJewelleryCost(item).ToString(CultureInfo.InvariantCulture);
                EstimatedRetailBox.Text = item.RetailPrice.ToString(CultureInfo.InvariantCulture);
                NotesBox.Text = $"Linked to jewellery stock item {item.StockCode}".Trim();
                break;
            case Stone stone:
                ItemNameBox.Text = stone.ToString();
                ItemTypeBox.Text = "Stone";
                PlannedQuantityBox.Text = "1";
                EstimatedCostBox.Text = stone.EstimatedValue.ToString(CultureInfo.InvariantCulture);
                EstimatedRetailBox.Text = stone.EstimatedValue.ToString(CultureInfo.InvariantCulture);
                NotesBox.Text = "Stone selected for production planning.";
                break;
            case Job job:
                ItemNameBox.Text = job.JobTitle;
                ItemTypeBox.Text = job.Type.ToString();
                PlannedQuantityBox.Text = "1";
                EstimatedCostBox.Text = PricingService.CalculateJobCost(job).ToString(CultureInfo.InvariantCulture);
                EstimatedRetailBox.Text = (job.FinalPrice > 0 ? job.FinalPrice : job.QuoteAmount).ToString(CultureInfo.InvariantCulture);
                NotesBox.Text = $"Linked to job {job.JobCode}".Trim();
                break;
            default:
                ItemNameBox.Text = "Planned piece";
                ItemTypeBox.Text = "Jewellery";
                break;
        }
    }

    private void Add_Click(object sender, RoutedEventArgs e)
    {
        if (BatchCombo.SelectedValue is not int batchId || batchId <= 0)
        {
            MessageBox.Show("Select a production batch first. Create one first if none are available.", "Add To Batch", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            var item = new ProductionBatchItem
            {
                ProductionBatchId = batchId,
                ItemName = ItemNameBox.Text.Trim(),
                ItemType = ItemTypeBox.Text.Trim(),
                PlannedQuantity = ParseDecimal(PlannedQuantityBox.Text, 1),
                EstimatedCost = ParseDecimal(EstimatedCostBox.Text, 0),
                EstimatedRetailValue = ParseDecimal(EstimatedRetailBox.Text, 0),
                Status = "Planned",
                Notes = NotesBox.Text.Trim()
            };

            if (_selected is JewelleryItem jewelleryItem)
                item.JewelleryItemId = jewelleryItem.Id;
            else if (_selected is Stone stone)
                item.StoneId = stone.Id;
            else if (_selected is Job job)
                item.JobId = job.Id;

            if (string.IsNullOrWhiteSpace(item.ItemName))
                throw new InvalidOperationException("Item name is required.");

            using var db = new AppDbContext();
            db.ProductionBatchItems.Add(item);
            db.SaveChanges();
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not add the item to the batch.\n\n{ex.Message}", "Add To Batch", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private static decimal ParseDecimal(string text, decimal fallback)
    {
        return decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var value)
            ? value
            : fallback;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private sealed record BatchOption(int Id, string Label)
    {
        public override string ToString() => string.IsNullOrWhiteSpace(Label) ? "Select production batch" : Label;
    }
}
