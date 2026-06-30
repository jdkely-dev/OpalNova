using System.Windows;
using JewelleryBusinessManager.Data;
using JewelleryBusinessManager.Models;
using JewelleryBusinessManager.Services;
using MessageBox = System.Windows.MessageBox;
using MessageBoxButton = System.Windows.MessageBoxButton;
using MessageBoxImage = System.Windows.MessageBoxImage;
using MessageBoxResult = System.Windows.MessageBoxResult;

namespace JewelleryBusinessManager.Views;

public partial class InventoryStatusWindow : Window
{
    private readonly Type _recordType;
    private readonly int _recordId;

    public InventoryStatusWindow(object selectedRecord)
    {
        InitializeComponent();
        _recordType = selectedRecord.GetType();
        _recordId = (int)_recordType.GetProperty("Id")!.GetValue(selectedRecord)!;
        LoadRecord(selectedRecord);
    }

    private void LoadRecord(object selectedRecord)
    {
        RecordText.Text = selectedRecord.ToString() ?? "Selected record";
        if (selectedRecord is JewelleryItem item)
        {
            CurrentStatusText.Text = $"Current jewellery status: {item.Status}";
            CurrentLifecycleText.Text = StockLifecycleService.DescribeStockStatus(item.Status);
            StatusBox.ItemsSource = Enum.GetValues(typeof(StockStatus));
            StatusBox.SelectedItem = item.Status;
            NotesBox.Text = item.Notes ?? string.Empty;
        }
        else if (selectedRecord is Stone stone)
        {
            CurrentStatusText.Text = $"Current stone status: {stone.Status}";
            CurrentLifecycleText.Text = StockLifecycleService.DescribeStoneStatus(stone.Status);
            StatusBox.ItemsSource = Enum.GetValues(typeof(StoneStatus));
            StatusBox.SelectedItem = stone.Status;
            NotesBox.Text = stone.Notes ?? string.Empty;
        }
        RefreshNewStatusLifecycleText();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            using var db = new AppDbContext();
            if (_recordType == typeof(JewelleryItem))
            {
                var item = db.JewelleryItems.Find(_recordId);
                if (item == null) return;
                item.Status = (StockStatus)StatusBox.SelectedItem!;
                item.Notes = MergeNote(item.Notes, NotesBox.Text);
            }
            else if (_recordType == typeof(Stone))
            {
                var stone = db.Stones.Find(_recordId);
                if (stone == null) return;
                stone.Status = (StoneStatus)StatusBox.SelectedItem!;
                stone.Notes = MergeNote(stone.Notes, NotesBox.Text);
            }
            db.SaveChanges();
            DialogResult = true;
        }
        catch (Exception ex)
        {
            ErrorLogService.Log(ex, "Change inventory status");
            MessageBox.Show($"Could not update the status.\n\n{ex.Message}", "Change Status", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static string? MergeNote(string? existing, string text)
    {
        var trimmed = text.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? existing : trimmed;
    }

    private void StatusBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e) => RefreshNewStatusLifecycleText();

    private void RefreshNewStatusLifecycleText()
    {
        NewStatusLifecycleText.Text = StatusBox.SelectedItem switch
        {
            StockStatus status => StockLifecycleService.DescribeStockStatus(status),
            StoneStatus status => StockLifecycleService.DescribeStoneStatus(status),
            _ => "Choose a status to see lifecycle guidance."
        };
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
