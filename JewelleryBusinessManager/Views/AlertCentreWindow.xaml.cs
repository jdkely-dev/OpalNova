using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using JewelleryBusinessManager.Data;
using JewelleryBusinessManager.Models;
using JewelleryBusinessManager.Services;

namespace JewelleryBusinessManager.Views;

public partial class AlertCentreWindow : Window
{
    public bool IsHostedInTab { get; set; }
    public event EventHandler<string>? OpenTargetRequested;
    public event EventHandler? CloseRequested;

    private readonly List<NextActionItem> _actions = new();
    private bool _ready;
    private bool _hasLoadedRows;
    private NextActionItem? _selectedAction;

    public AlertCentreWindow()
    {
        InitializeComponent();
        _ready = true;
        Loaded += AlertCentreWindow_Loaded;
        AlertRoot.Loaded += AlertCentreWindow_Loaded;
    }

    private void AlertCentreWindow_Loaded(object sender, RoutedEventArgs e)
    {
        if (IsHostedInTab)
        {
            Margin = new Thickness(0);
            HorizontalAlignment = HorizontalAlignment.Stretch;
            VerticalAlignment = VerticalAlignment.Stretch;
        }

        if (_hasLoadedRows) return;
        _hasLoadedRows = true;
        LoadActions();
    }

    private void LoadActions()
    {
        using var db = new AppDbContext();
        _actions.Clear();
        _actions.AddRange(NextActionService.BuildActions(db));
        ApplyFilter();
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

        IEnumerable<NextActionItem> query = _actions;
        var filter = GetSelectedFilter();
        query = filter switch
        {
            "Urgent" => query.Where(x => x.PriorityRank == 1),
            "High" => query.Where(x => x.PriorityRank == 2),
            "Quotes" => query.Where(x => x.Area == "Quote"),
            "Production" => query.Where(x => x.Area == "Production"),
            "Payments" => query.Where(x => x.Area == "Payment"),
            "Diamonds" => query.Where(x => x.Area == "Diamond"),
            "Inventory" => query.Where(x => x.Area == "Inventory"),
            "Follow-ups" => query.Where(x => x.Area == "Follow-up"),
            "All alerts" => query,
            _ => query.Where(x => x.IsActionNeeded)
        };

        var search = SearchBox.Text.Trim();
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(x =>
                $"{x.Area} {x.PriorityLabel} {x.Title} {x.Detail} {x.Risk} {x.SuggestedAction} {x.DueLine} {x.ValueLine}"
                    .Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        var selectedKey = _selectedAction?.StableKey;
        var rows = query.ToList();
        UpdateCounts(rows);
        AlertsGrid.ItemsSource = rows;
        _selectedAction = rows.FirstOrDefault(x => x.StableKey == selectedKey) ?? rows.FirstOrDefault();
        AlertsGrid.SelectedItem = _selectedAction;
        var searchSuffix = string.IsNullOrWhiteSpace(search) ? string.Empty : $" - Search: {search}";
        StatusText.Text = $"Showing {rows.Count} of {_actions.Count} alert(s) - Filter: {filter}{searchSuffix}";
        UpdateSelectedDetails();
    }

    private void UpdateCounts(IReadOnlyCollection<NextActionItem> visibleRows)
    {
        ActionNeededCountText.Text = visibleRows.Count(x => x.IsActionNeeded).ToString();
        UrgentCountText.Text = visibleRows.Count(x => x.PriorityRank == 1).ToString();
        QuoteCountText.Text = visibleRows.Count(x => x.Area == "Quote").ToString();
        ProductionCountText.Text = visibleRows.Count(x => x.Area == "Production").ToString();
        PaymentCountText.Text = visibleRows.Count(x => x.Area == "Payment").ToString();
        FollowUpCountText.Text = visibleRows.Count(x => x.Area == "Follow-up").ToString();
    }

    private void UpdateSelectedDetails()
    {
        var row = AlertsGrid.SelectedItem as NextActionItem ?? _selectedAction;
        _selectedAction = row;

        if (row == null)
        {
            SelectedTitleText.Text = "Select an alert";
            SelectedSubtitleText.Text = "Choose a row to see the recommended next action.";
            SuggestedActionText.Text = "No alert selected.";
            DetailText.Text = string.Empty;
            RiskText.Text = string.Empty;
            OpenSelectedButton.IsEnabled = false;
            OpenSelectedButton.Content = "Open Next Step";
            return;
        }

        OpenSelectedButton.IsEnabled = true;
        OpenSelectedButton.Content = row.ActionLabel;
        SelectedTitleText.Text = row.Title;
        SelectedSubtitleText.Text = $"{row.Area} - {row.PriorityLabel} priority - {row.DueLine}";
        SuggestedActionText.Text = row.SuggestedAction;
        DetailText.Text = row.Detail;
        RiskText.Text = row.Risk;
    }

    private void OpenSelected_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedAction == null)
        {
            MessageBox.Show("Select an alert first.", "Alert Centre", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        OpenTargetRequested?.Invoke(this, _selectedAction.TargetKey);
    }

    private void OpenProjectHub_Click(object sender, RoutedEventArgs e) => OpenTargetRequested?.Invoke(this, "Project Workbench");
    private void OpenTasks_Click(object sender, RoutedEventArgs e) => OpenTargetRequested?.Invoke(this, "Tasks");
    private void AlertsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e) => UpdateSelectedDetails();
    private void AlertsGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e) => OpenSelected_Click(sender, e);
    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilter();
    private void FilterCombo_SelectionChanged(object sender, SelectionChangedEventArgs e) => ApplyFilter();
    private void Refresh_Click(object sender, RoutedEventArgs e) => LoadActions();

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        if (IsHostedInTab) CloseRequested?.Invoke(this, EventArgs.Empty);
        else Close();
    }
}
