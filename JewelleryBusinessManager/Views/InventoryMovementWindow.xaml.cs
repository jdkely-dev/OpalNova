using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using JewelleryBusinessManager.Data;
using JewelleryBusinessManager.Models;
using JewelleryBusinessManager.Services;
using MessageBox = System.Windows.MessageBox;
using MessageBoxButton = System.Windows.MessageBoxButton;
using MessageBoxImage = System.Windows.MessageBoxImage;
using MessageBoxResult = System.Windows.MessageBoxResult;

namespace JewelleryBusinessManager.Views;

public partial class InventoryMovementWindow : Window
{
    private readonly object? _selectedRecord;

    public InventoryMovementWindow(object? selectedRecord)
    {
        InitializeComponent();
        _selectedRecord = selectedRecord;
        LoadLookups();
        ApplySelectionDefaults();
    }

    private sealed record LookupItem(int? Id, string Display);

    private void LoadLookups()
    {
        using var db = new AppDbContext();

        var materials = new List<LookupItem> { new(null, "Select material") };
        materials.AddRange(db.Materials
            .AsEnumerable()
            .OrderBy(m => m.Name)
            .Select(m => new LookupItem(m.Id, $"{m.MaterialCode} {m.Name} — {m.CurrentQuantity:0.###} {m.UnitType}".Trim())));
        MaterialBox.ItemsSource = materials;

        var jobs = new List<LookupItem> { new(null, "No linked job") };
        jobs.AddRange(db.Jobs.AsEnumerable()
            .Where(j => j.Status != JobStatus.Completed && j.Status != JobStatus.Cancelled)
            .OrderBy(j => j.JobCode)
            .Select(j => new LookupItem(j.Id, $"{j.JobCode} {j.JobTitle} ({j.Status})".Trim())));
        JobBox.ItemsSource = jobs;
        JobBox.SelectedIndex = 0;

        var jewellery = new List<LookupItem> { new(null, "No linked jewellery item") };
        jewellery.AddRange(db.JewelleryItems.AsEnumerable()
            .Where(j => j.Status != StockStatus.Sold)
            .OrderBy(j => j.StockCode)
            .Select(j => new LookupItem(j.Id, $"{j.StockCode} {j.Name} ({j.Status})".Trim())));
        JewelleryBox.ItemsSource = jewellery;
        JewelleryBox.SelectedIndex = 0;
    }

    private void ApplySelectionDefaults()
    {
        if (_selectedRecord is Material material)
            MaterialBox.SelectedValue = material.Id;
        if (_selectedRecord is Job job)
            JobBox.SelectedValue = job.Id;
        if (_selectedRecord is JewelleryItem item)
            JewelleryBox.SelectedValue = item.Id;
        if (MaterialBox.SelectedIndex < 0 && MaterialBox.Items.Count > 0)
            MaterialBox.SelectedIndex = 0;
        UpdateMaterialInfo();
    }

    private void MaterialBox_SelectionChanged(object sender, SelectionChangedEventArgs e) => UpdateMaterialInfo();

    private void UpdateMaterialInfo()
    {
        if (MaterialBox.SelectedValue is not int materialId)
        {
            MaterialInfoText.Text = "Choose the material being received, used or adjusted.";
            return;
        }

        using var db = new AppDbContext();
        var material = db.Materials.Find(materialId);
        if (material == null) return;
        var lowText = material.CurrentQuantity <= material.ReorderLevel ? " • LOW STOCK" : string.Empty;
        MaterialInfoText.Text = $"Current: {material.CurrentQuantity:0.###} {material.UnitType} • Reorder level: {material.ReorderLevel:0.###}{lowText}";
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (MaterialBox.SelectedValue is not int materialId)
        {
            MessageBox.Show("Choose a material first.", "Stock Movement", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!decimal.TryParse(QuantityBox.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out var quantity) || quantity <= 0)
        {
            MessageBox.Show("Quantity must be a positive number.", "Stock Movement", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var movementText = (MovementTypeBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Use / consume material";
        var quantityChange = movementText switch
        {
            string text when text.StartsWith("Use", StringComparison.OrdinalIgnoreCase) => -quantity,
            string text when text.StartsWith("Receive", StringComparison.OrdinalIgnoreCase) => quantity,
            string text when text.StartsWith("Return", StringComparison.OrdinalIgnoreCase) => quantity,
            _ => quantity
        };

        try
        {
            using var db = new AppDbContext();
            var material = db.Materials.Find(materialId);
            if (material == null)
            {
                MessageBox.Show("The selected material could not be found.", "Stock Movement", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var newQuantity = material.CurrentQuantity + quantityChange;
            if (newQuantity < 0)
            {
                var result = MessageBox.Show($"This movement will take {material.Name} below zero ({newQuantity:0.###} {material.UnitType}). Continue anyway?", "Negative stock warning", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result != MessageBoxResult.Yes) return;
            }

            material.CurrentQuantity = newQuantity;
            db.MaterialTransactions.Add(new MaterialTransaction
            {
                MaterialId = material.Id,
                TransactionDate = DateTime.Today,
                QuantityChange = quantityChange,
                Reason = string.IsNullOrWhiteSpace(ReasonBox.Text) ? movementText : ReasonBox.Text.Trim(),
                JobId = JobBox.SelectedValue as int?,
                JewelleryItemId = JewelleryBox.SelectedValue as int?,
                Notes = NotesBox.Text.Trim()
            });

            db.SaveChanges();
            MessageBox.Show($"Movement saved. New quantity: {material.CurrentQuantity:0.###} {material.UnitType}.", "Stock Movement", MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
        }
        catch (Exception ex)
        {
            ErrorLogService.Log(ex, "Save stock movement");
            MessageBox.Show($"Could not save the stock movement.\n\n{ex.Message}", "Stock Movement", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
