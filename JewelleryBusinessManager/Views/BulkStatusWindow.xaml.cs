using System.Windows;

namespace JewelleryBusinessManager.Views;

public partial class BulkStatusWindow : Window
{
    private readonly Type _enumType;

    public object? SelectedStatusValue { get; private set; }
    public string SelectedStatusText => SelectedStatusValue?.ToString() ?? string.Empty;

    public BulkStatusWindow(string recordTypeName, Type enumType, int selectedCount)
    {
        InitializeComponent();
        _enumType = enumType;
        SubtitleText.Text = $"Update {selectedCount} selected {recordTypeName} record(s).";
        StatusCombo.ItemsSource = Enum.GetValues(enumType).Cast<object>().ToList();
        StatusCombo.SelectedIndex = 0;
    }

    private void Apply_Click(object sender, RoutedEventArgs e)
    {
        if (StatusCombo.SelectedItem == null)
        {
            MessageBox.Show("Choose a status before applying.", "Bulk Status Update", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        SelectedStatusValue = Enum.Parse(_enumType, StatusCombo.SelectedItem.ToString() ?? string.Empty);
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
