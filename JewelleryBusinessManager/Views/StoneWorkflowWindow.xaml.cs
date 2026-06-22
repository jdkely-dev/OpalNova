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

public partial class StoneWorkflowWindow : Window
{
    private readonly int _stoneId;

    public StoneWorkflowWindow(Stone stone)
    {
        InitializeComponent();
        _stoneId = stone.Id;
        StoneSummaryText.Text = $"{stone.StoneCode} • {stone.StoneType} • {stone.WeightCarats:N2} ct • Current: {stone.Status}";
        var currentStage = OpalWorkflowService.StatusToStage(stone.Status);
        foreach (ComboBoxItem item in StageComboBox.Items)
        {
            if (string.Equals(item.Content?.ToString(), currentStage, StringComparison.OrdinalIgnoreCase))
            {
                StageComboBox.SelectedItem = item;
                break;
            }
        }
        StageComboBox.SelectedIndex = StageComboBox.SelectedIndex < 0 ? 1 : StageComboBox.SelectedIndex;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var stage = (StageComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Loose";
        try
        {
            using var db = new AppDbContext();
            var stone = db.Stones.Find(_stoneId);
            if (stone == null)
            {
                MessageBox.Show("The selected stone could not be found.", "Stone Workflow", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            OpalWorkflowService.ApplyStoneWorkflowStage(db, stone, stage, NotesTextBox.Text);
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            ErrorLogService.Log(ex, "Save stone workflow stage");
            MessageBox.Show($"Could not save the stone workflow stage.\n\n{ex.Message}", "Stone Workflow", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
