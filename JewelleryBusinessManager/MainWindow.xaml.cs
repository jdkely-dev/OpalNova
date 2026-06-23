using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using WpfButton = System.Windows.Controls.Button;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using JewelleryBusinessManager.Data;
using JewelleryBusinessManager.Models;
using JewelleryBusinessManager.Services;
using JewelleryBusinessManager.Views;
using MessageBox = System.Windows.MessageBox;
using MessageBoxButton = System.Windows.MessageBoxButton;
using MessageBoxImage = System.Windows.MessageBoxImage;
using MessageBoxResult = System.Windows.MessageBoxResult;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace JewelleryBusinessManager;

public partial class MainWindow : Window
{
    private readonly Dictionary<string, Type> _sectionTypes = new()
    {
        ["Customers"] = typeof(Customer),
        ["Suppliers"] = typeof(Supplier),
        ["Materials"] = typeof(Material),
        ["Material Transactions"] = typeof(MaterialTransaction),
        ["Opal Parcels"] = typeof(OpalParcel),
        ["Stones"] = typeof(Stone),
        ["Jewellery Stock"] = typeof(JewelleryItem),
        ["Jobs"] = typeof(Job),
        ["Sales"] = typeof(Sale),
        ["Payments"] = typeof(Payment),
        ["Market Events"] = typeof(MarketEvent),
        ["Market Stock"] = typeof(MarketStock),
        ["Production Batches"] = typeof(ProductionBatch),
        ["Batch Items"] = typeof(ProductionBatchItem),
        ["Online Listings"] = typeof(OnlineListing),
        ["Purchase Orders"] = typeof(PurchaseOrder),
        ["Purchase Order Items"] = typeof(PurchaseOrderItem),
        ["Tasks"] = typeof(BusinessTask),
        ["Photos"] = typeof(PhotoRecord),
        ["Custom Quotes"] = typeof(CustomQuote),
        ["Quote Options"] = typeof(QuoteOption),
        ["External Diamonds"] = typeof(ExternalDiamond),
    };

    private readonly HashSet<string> _toolSections = new()
    {
        "Project Workbench",
        "Alert Centre",
        "Quotes & Proposals",
        "Production",
        "Payments & Sales",
        "Inventory",
        "Diamonds",
        "Reports",
        "Settings & Backup",
        "Custom Workflow Studio",
        "Diamond Supplier Studio",
        "Pricing Studio",
        "Inventory Studio",
        "Purchasing Studio",
        "Production & Opal Studio",
        "Market Studio",
        "Online Selling Studio",
        "Tasks Studio",
        "Codes & Labels Studio",
        "Documents Studio",
        "Reports Studio",
        "Safety & Data Studio",
        "Hardware & POS Studio",
        "Customer Relationship Studio",
        "Data Cleanup Studio"
    };

    private sealed record ToolAction(string Title, string Description, RoutedEventHandler Handler);
    private sealed record SetupReadinessRow(string Status, string Title, string Detail, string TargetKey, bool IsComplete, bool CountsTowardProgress = true);
    private string _dashboardSetupTarget = "settings";
    private sealed record EntitySelectionOption(string Label, object Entity)
    {
        public override string ToString() => string.IsNullOrWhiteSpace(Label) ? "Select record" : Label;
    }
    private sealed record WorkspaceTabState(string Key, Window? HostWindow, Action? OnClosed);

    private Window? _hoverHelpWindow;
    private string? _hoverHelpKey;
    private bool _hoverHelpPinned;
    private FrameworkElement? _hoverHelpSource;

    private static readonly string[] QuickFilterPresets =
    {
        "All Records", "Low Stock", "Jobs Due Soon", "Overdue Jobs", "Needs Photos", "At Market", "Reserved Stock",
        "Ready To List", "Needs Listing Work", "Overdue Tasks", "Due Today", "High Priority", "Open Purchase Orders", "Open Jobs"
    };

    private static readonly Dictionary<string, string> QuickFilterHomeSections = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Low Stock"] = "Materials",
        ["Jobs Due Soon"] = "Jobs",
        ["Overdue Jobs"] = "Jobs",
        ["Needs Photos"] = "Jewellery Stock",
        ["At Market"] = "Jewellery Stock",
        ["Reserved Stock"] = "Jewellery Stock",
        ["Ready To List"] = "Online Listings",
        ["Needs Listing Work"] = "Online Listings",
        ["Overdue Tasks"] = "Tasks",
        ["Due Today"] = "Tasks",
        ["High Priority"] = "Tasks",
        ["Open Purchase Orders"] = "Purchase Orders",
        ["Open Jobs"] = "Jobs"
    };

    private bool _isUpdatingTopFilter;
    private bool _suppressNavigationSearchReset;

    private string _currentSection = "Dashboard";
    private string CurrentSection => _currentSection;
    private IList? _currentRecords;
    private string? _currentReportPath;
    private string _activeFilterPreset = "All Records";
    private readonly DispatcherTimer _searchDebounceTimer;
    private DateTime _lastRefreshStarted = DateTime.MinValue;

    public MainWindow()
    {
        InitializeComponent();
        AddHandler(WpfButton.ClickEvent, new RoutedEventHandler(WorkspaceTabCloseButtonRouted_Click), true);
        _searchDebounceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(240) };
        _searchDebounceTimer.Tick += (_, _) =>
        {
            _searchDebounceTimer.Stop();
            RefreshCurrentSection();
        };
        _isUpdatingTopFilter = true;
        TopFilterCombo.ItemsSource = QuickFilterPresets;
        TopFilterCombo.SelectedItem = "All Records";
        _isUpdatingTopFilter = false;
        LoadDashboard();
    }

    private void NavigationTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        // WPF can raise selection changes while InitializeComponent is still building named controls.
        // Ignore that early event; the constructor loads the dashboard after InitializeComponent completes.
        if (!IsLoaded) return;
        if (e.NewValue is not TreeViewItem item) return;
        var section = item.Header?.ToString() ?? string.Empty;
        if (section != "Dashboard" && !_sectionTypes.ContainsKey(section) && !_toolSections.Contains(section)) return;
        _currentSection = section;
        if (!_suppressNavigationSearchReset)
        {
            SearchBox.Text = string.Empty;
            SetTopFilter("All Records");
        }
        RefreshCurrentSection();
    }

    private void SelectNavigationSection(string section, bool preserveSearchAndFilter = false)
    {
        if (section != "Dashboard" && !_sectionTypes.ContainsKey(section) && !_toolSections.Contains(section)) return;
        _currentSection = section;
        _suppressNavigationSearchReset = preserveSearchAndFilter;
        try
        {
            if (!SelectTreeViewItemByHeader(NavigationTree, section))
            {
                RefreshCurrentSection();
            }
        }
        finally
        {
            _suppressNavigationSearchReset = false;
        }
    }

    private static bool SelectTreeViewItemByHeader(ItemsControl parent, string header)
    {
        foreach (var itemObject in parent.Items)
        {
            if (itemObject is not TreeViewItem item) continue;
            if ((item.Header?.ToString() ?? string.Empty) == header)
            {
                item.IsSelected = true;
                item.BringIntoView();
                return true;
            }
            if (SelectTreeViewItemByHeader(item, header))
            {
                item.IsExpanded = true;
                return true;
            }
        }
        return false;
    }

    private void Refresh_Click(object sender, RoutedEventArgs e) => RefreshCurrentSection();

    private void DashboardTile_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Border tile || tile.Tag is not string target || string.IsNullOrWhiteSpace(target))
            return;

        if (target == "Alert Centre")
        {
            AlertCentre_Click(sender, e);
            e.Handled = true;
            return;
        }

        SelectNavigationSection(target);
        StatusText.Text = $"Opened {target} from dashboard tile.";
        e.Handled = true;
    }

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        var window = new SettingsWindow { Owner = this };
        if (window.ShowDialog() == true)
        {
            RefreshCurrentSection();
            MessageBox.Show("Business settings saved. New printouts and backups will use the updated details.", "Settings", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void MenuNavigate_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement element || element.Tag is not string section || string.IsNullOrWhiteSpace(section))
        {
            return;
        }

        SelectNavigationSection(section);
        StatusText.Text = $"Opened {section} from the top menu.";
    }

    private void ToolbarNewQuote_Click(object sender, RoutedEventArgs e) => CustomQuoteBuilder_Click(sender, e);

    private void ToolbarDiamondSearch_Click(object sender, RoutedEventArgs e) => DiamondSupplier_Click(sender, e);

    private void ToolbarReports_Click(object sender, RoutedEventArgs e) => SelectNavigationSection("Reports");

    private void WorkspaceTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded || e.Source != WorkspaceTabs) return;
        if (WorkspaceTabs.SelectedItem is TabItem tab)
            StatusText.Text = $"Active tab: {GetTabTitle(tab)}";
    }

    private TabItem OpenWindowInWorkspaceTab(string title, Window window, string key, Action? onClosed = null)
    {
        foreach (var item in WorkspaceTabs.Items)
        {
            if (item is TabItem existing && existing.Tag is WorkspaceTabState state && state.Key == key)
            {
                WorkspaceTabs.Visibility = Visibility.Visible;
                WorkspaceTabs.SelectedItem = existing;
                ShowWorkspaceTabsOnly();
                StatusText.Text = $"Switched to existing tab: {title}.";
                return existing;
            }
        }

        var content = window.Content;
        window.Content = null;
        return OpenContentInWorkspaceTab(title, content, key, window, onClosed);
    }

    private TabItem OpenContentInWorkspaceTab(string title, object? content, string key, Window? hostWindow = null, Action? onClosed = null)
    {
        content = PrepareHostedWorkspaceContent(content);
        var tab = new TabItem
        {
            Content = content,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            VerticalContentAlignment = VerticalAlignment.Stretch,
            Tag = new WorkspaceTabState(key, hostWindow, onClosed)
        };
        tab.Header = CreateWorkspaceTabHeader(tab, title);
        WorkspaceTabs.Items.Add(tab);
        WorkspaceTabs.Visibility = Visibility.Visible;
        WorkspaceTabs.SelectedItem = tab;
        ShowWorkspaceTabsOnly();
        StatusText.Text = $"Opened tab: {title}. Use the tab close button when finished.";
        return tab;
    }


    private static object? PrepareHostedWorkspaceContent(object? content)
    {
        if (content is FrameworkElement element)
        {
            element.Margin = new Thickness(0);
            element.HorizontalAlignment = HorizontalAlignment.Stretch;
            element.VerticalAlignment = VerticalAlignment.Stretch;
        }
        return content;
    }

    private FrameworkElement CreateWorkspaceTabHeader(TabItem tab, string title)
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        panel.Children.Add(new TextBlock
        {
            Text = title,
            MaxWidth = 190,
            TextTrimming = TextTrimming.CharacterEllipsis,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 6, 0)
        });
        var close = new WpfButton
        {
            Content = "x",
            Tag = tab,
            Width = 32,
            MinWidth = 32,
            Height = 32,
            MinHeight = 32,
            Padding = new Thickness(0),
            FontSize = 14,
            ToolTip = "Close this workspace tab",
            Focusable = false
        };
        close.PreviewMouseLeftButtonDown += CloseWorkspaceTab_PreviewMouseLeftButtonDown;
        close.Click += CloseWorkspaceTab_Click;
        panel.Children.Add(close);
        return panel;
    }

    private static string GetTabTitle(TabItem tab)
    {
        if (tab.Header is StackPanel panel && panel.Children.Count > 0 && panel.Children[0] is TextBlock text)
            return text.Text;
        return "Workspace";
    }


    private void CloseWorkspaceTab_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is WpfButton { Tag: TabItem tab })
        {
            CloseWorkspaceTab(tab);
            e.Handled = true;
        }
    }

    private void WorkspaceTabCloseButtonRouted_Click(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is WpfButton { Tag: TabItem tab })
        {
            CloseWorkspaceTab(tab);
            e.Handled = true;
        }
    }

    private void CloseWorkspaceTab_Click(object sender, RoutedEventArgs e)
    {
        if (sender is WpfButton { Tag: TabItem tab })
        {
            CloseWorkspaceTab(tab);
            e.Handled = true;
        }
    }

    private void CloseWorkspaceTab(TabItem? tab)
    {
        if (tab == null) return;
        if (!WorkspaceTabs.Items.Contains(tab)) return;

        var title = GetTabTitle(tab);
        var state = tab.Tag as WorkspaceTabState;
        var closedSelectedTab = Equals(WorkspaceTabs.SelectedItem, tab);

        tab.Content = null;
        WorkspaceTabs.Items.Remove(tab);

        if (state?.HostWindow?.IsVisible == true)
        {
            try { state.HostWindow.Close(); } catch { }
        }

        if (WorkspaceTabs.Items.Count > 0)
        {
            if (closedSelectedTab)
                WorkspaceTabs.SelectedIndex = Math.Max(0, Math.Min(WorkspaceTabs.SelectedIndex, WorkspaceTabs.Items.Count - 1));

            StatusText.Text = $"Closed tab: {title}.";
            return;
        }

        WorkspaceTabs.Visibility = Visibility.Collapsed;
        try
        {
            if (state?.OnClosed != null)
                state.OnClosed.Invoke();
            else
                RefreshAfterWorkspaceTabClosed();
        }
        catch
        {
            LoadDashboard();
        }

        StatusText.Text = $"Closed tab: {title}.";
    }

    private void RefreshAfterWorkspaceTabClosed()
    {
        if (CurrentSection == "Project Workbench")
        {
            SelectNavigationSection("Dashboard");
            return;
        }

        RefreshCurrentSection();
    }

    private void ShowWorkspaceTabsOnly()
    {
        DashboardPanel.Visibility = Visibility.Collapsed;
        RecordWorkspacePanel.Visibility = Visibility.Collapsed;
        ToolWorkspacePanel.Visibility = Visibility.Collapsed;
        ReportPanel.Visibility = Visibility.Collapsed;
        WorkspaceTabs.Visibility = Visibility.Visible;
        CurrentPageTitleText.Text = "Workspace";
        CurrentPageHintText.Text = "";
    }

    private void OpenEntityEditorTab(string title, object entity, bool isNewRecord)
    {
        var editor = new EditEntityWindow(entity) { IsHostedInTab = true };
        TabItem? tab = null;
        editor.Saved += (_, _) =>
        {
            using var db = new AppDbContext();
            try
            {
                ApplyBusinessRules(db, entity, isNewRecord);
                if (isNewRecord)
                    db.Add(entity);
                else
                    db.Update(entity);
                db.SaveChanges();
                RecalculateParentPurchaseOrderAfterSave(entity);
                CloseWorkspaceTab(tab);
                RefreshCurrentSection();
                StatusText.Text = isNewRecord ? "Record added and workspace refreshed." : "Record updated and workspace refreshed.";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not save the record.\n\n{ex.Message}", "Database error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        };
        editor.Cancelled += (_, _) => CloseWorkspaceTab(tab);
        var content = editor.Content;
        editor.Content = null;
        var key = $"entity:{CurrentSection}:{(isNewRecord ? "new" : entity.GetType().GetProperty("Id")?.GetValue(entity)?.ToString() ?? Guid.NewGuid().ToString())}";
        tab = OpenContentInWorkspaceTab(title, content, key, editor);
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!IsLoaded) return;
        _searchDebounceTimer.Stop();
        _searchDebounceTimer.Start();
        StatusText.Text = string.IsNullOrWhiteSpace(SearchBox.Text)
            ? $"Search cleared. Showing {CurrentSection} with filter {_activeFilterPreset}."
            : $"Filtering {CurrentSection} for '{SearchBox.Text}' with filter {_activeFilterPreset}. Press Enter or Search All for global results.";
    }

    private void SearchBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        OpenAdvancedSearch(CurrentSection == "Dashboard" || _toolSections.Contains(CurrentSection) ? "All Sections" : CurrentSection, _activeFilterPreset, SearchBox.Text.Trim());
        e.Handled = true;
    }

    private void SearchAll_Click(object sender, RoutedEventArgs e)
    {
        OpenAdvancedSearch("All Sections", _activeFilterPreset, SearchBox.Text.Trim());
    }

    private void ClearSearch_Click(object sender, RoutedEventArgs e)
    {
        SearchBox.Text = string.Empty;
        SetTopFilter("All Records");
        RefreshCurrentSection();
        StatusText.Text = "Search and filter cleared.";
    }

    private void TopFilterCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingTopFilter || !IsLoaded) return;
        var preset = TopFilterCombo.SelectedItem?.ToString() ?? "All Records";
        _activeFilterPreset = preset;
        if (preset != "All Records" && QuickFilterHomeSections.TryGetValue(preset, out var targetSection) && CurrentSection != targetSection)
        {
            SelectNavigationSection(targetSection, preserveSearchAndFilter: true);
        }
        else
        {
            RefreshCurrentSection();
        }
        StatusText.Text = preset == "All Records"
            ? "Quick filter cleared."
            : $"Quick filter applied: {preset}. Use Search All for global results or type to narrow this page.";
    }

    private void SetTopFilter(string preset)
    {
        _activeFilterPreset = string.IsNullOrWhiteSpace(preset) ? "All Records" : preset;
        if (TopFilterCombo == null) return;
        _isUpdatingTopFilter = true;
        TopFilterCombo.SelectedItem = QuickFilterPresets.Contains(_activeFilterPreset) ? _activeFilterPreset : "All Records";
        _isUpdatingTopFilter = false;
    }

    private void MainWindow_KeyDown(object sender, KeyEventArgs e)
    {
        if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.F)
        {
            SearchBox.Focus();
            SearchBox.SelectAll();
            StatusText.Text = "Search ready. Type to filter this page, choose a filter, or press Enter/Search All for global results.";
            e.Handled = true;
            return;
        }

        if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.N)
        {
            Add_Click(sender, new RoutedEventArgs());
            e.Handled = true;
            return;
        }

        if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.E)
        {
            Edit_Click(sender, new RoutedEventArgs());
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Delete && Keyboard.Modifiers == ModifierKeys.None)
        {
            Delete_Click(sender, new RoutedEventArgs());
            e.Handled = true;
            return;
        }

        if (e.Key == Key.F5)
        {
            RefreshCurrentSection();
            StatusText.Text = $"Refreshed {CurrentSection}.";
            e.Handled = true;
            return;
        }

        if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.B)
        {
            Backup_Click(sender, new RoutedEventArgs());
            e.Handled = true;
            return;
        }

        if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.P)
        {
            OpenReportHtml_Click(sender, new RoutedEventArgs());
            e.Handled = true;
        }
    }

    private void RecordsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded) return;
        UpdateRecordPreview(RecordsGrid.SelectedItem);
    }

    private void OpenReportInApp(string path, string title)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            MessageBox.Show($"The generated report file could not be found.\n\n{path}", title, MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        _currentReportPath = path;
        if (ToolWorkspacePanel.Visibility == Visibility.Visible)
        {
            ToolPreviewTitleText.Text = title;
            ToolPreviewHintText.Text = $"Generated HTML: {path}";
            ToolPreviewBrowser.Navigate(new Uri(path));
            ShowToolPreviewPage();
            StatusText.Text = $"Previewing {title} in the tool workspace. Use Setup / Inputs to return to tool options, or Open HTML / Print for browser printing.";
            return;
        }

        DashboardPanel.Visibility = Visibility.Collapsed;
        RecordWorkspacePanel.Visibility = Visibility.Collapsed;
        ToolWorkspacePanel.Visibility = Visibility.Collapsed;
        ReportPanel.Visibility = Visibility.Visible;
        ReportTitleText.Text = title;
        ReportPathText.Text = $"Generated HTML: {path}";
        ReportBrowser.Navigate(new Uri(path));
        StatusText.Text = $"Viewing {title} inside the app. Use Open HTML / Print for browser printing.";
    }

    private void CloseReport_Click(object sender, RoutedEventArgs e)
    {
        ReportPanel.Visibility = Visibility.Collapsed;
        RefreshCurrentSection();
    }

    private void OpenReportHtml_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_currentReportPath) || !File.Exists(_currentReportPath))
        {
            MessageBox.Show("No generated report is currently available to open.", "Report Viewer", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        Process.Start(new ProcessStartInfo(_currentReportPath) { UseShellExecute = true });
    }

    private void OpenReportFolder_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_currentReportPath) || !File.Exists(_currentReportPath))
        {
            MessageBox.Show("No generated report folder is currently available to open.", "Report Viewer", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        var directory = Path.GetDirectoryName(_currentReportPath);
        if (!string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory))
            Process.Start(new ProcessStartInfo(directory) { UseShellExecute = true });
    }


    private void ToolSetupTab_Click(object sender, RoutedEventArgs e)
    {
        ShowToolSetupPage();
        StatusText.Text = "Showing tool setup and input controls.";
    }

    private void ToolPreviewTab_Click(object sender, RoutedEventArgs e)
    {
        ShowToolPreviewPage();
        StatusText.Text = string.IsNullOrWhiteSpace(_currentReportPath)
            ? "No generated preview is available yet. Use Setup / Inputs to run a tool first."
            : "Showing generated preview/result.";
    }

    private void ShowToolSetupPage()
    {
        ToolInputScrollViewer.Visibility = Visibility.Visible;
        ToolPreviewBrowser.Visibility = Visibility.Collapsed;
        ToolPreviewEmptyPanel.Visibility = Visibility.Collapsed;
        UpdateToolWorkspaceTabs(showingSetup: true);
    }

    private void ShowToolPreviewPage()
    {
        ToolInputScrollViewer.Visibility = Visibility.Collapsed;

        if (!string.IsNullOrWhiteSpace(_currentReportPath) && File.Exists(_currentReportPath))
        {
            ToolPreviewBrowser.Visibility = Visibility.Visible;
            ToolPreviewEmptyPanel.Visibility = Visibility.Collapsed;
        }
        else
        {
            ToolPreviewBrowser.Visibility = Visibility.Collapsed;
            ToolPreviewEmptyPanel.Visibility = Visibility.Visible;
        }

        UpdateToolWorkspaceTabs(showingSetup: false);
    }

    private void UpdateToolWorkspaceTabs(bool showingSetup)
    {
        if (ToolSetupTabButton is not null)
        {
            ToolSetupTabButton.Opacity = showingSetup ? 1.0 : 0.72;
            ToolSetupTabButton.Style = (Style)FindResource(showingSetup ? "AccentButtonStyle" : "ToolbarButtonStyle");
        }

        if (ToolPreviewTabButton is not null)
        {
            ToolPreviewTabButton.Opacity = showingSetup ? 0.72 : 1.0;
            ToolPreviewTabButton.Style = (Style)FindResource(showingSetup ? "ToolbarButtonStyle" : "AccentButtonStyle");
        }
    }


    private static string GetSectionHint(string section) => section switch
    {
        "Customers" => "Customer details, preferences, sizing notes and order history links.",
        "Suppliers" => "Supplier contacts for materials, opal parcels and purchasing.",
        "Materials" => "Raw materials, findings, packaging and reorder levels.",
        "Material Transactions" => "Inventory movement history created from stock usage and purchase receiving.",
        "Opal Parcels" => "Rough parcel tracking, cost, weight and yield history.",
        "Stones" => "Cut stones, opal workflow stages, carats, value and jewellery links.",
        "Jewellery Stock" => "Finished jewellery, pricing, statuses, listing and market tracking.",
        "Jobs" => "Custom orders, repairs, quotes, deposits, balances and bench workflow.",
        "Sales" => "Sales records linked to jobs, jewellery stock and customers.",
        "Payments" => "Deposits and payments linked to jobs and sales.",
        "Market Events" => "Market dates, costs, packing, takings and reconciliation.",
        "Market Stock" => "Stock assigned to market events, packed, sold or returned.",
        "Production Batches" => "Collections, making runs and planned stock builds.",
        "Batch Items" => "Individual planned or linked items inside production batches.",
        "Online Listings" => "Website, marketplace and social listing workflow content.",
        "Purchase Orders" => "Supplier purchase orders, order status and receiving workflow.",
        "Purchase Order Items" => "Line items for supplier orders and material receiving.",
        "Tasks" => "Reminders, work queue items and linked follow-ups.",
        "Photos" => "Stored photo references linked to stock, stones, parcels and jobs.",
        _ => "Browse, search and manage records in this section."
    };

    private void LoadToolWorkspace(string section)
    {
        CurrentPageTitleText.Text = section;
        CurrentPageHintText.Text = "Tool workspace mode: choose a sub-function on the left and preview reports/documents on the right.";
        DashboardPanel.Visibility = Visibility.Collapsed;
        RecordWorkspacePanel.Visibility = Visibility.Collapsed;
        ReportPanel.Visibility = Visibility.Collapsed;
        ToolWorkspacePanel.Visibility = Visibility.Visible;

        ToolPageTitleText.Text = section;
        ToolPageSubtitleText.Text = GetToolPageDescription(section);
        _currentReportPath = null;
        ToolPreviewTitleText.Text = "Setup / Inputs";
        ToolPreviewHintText.Text = "Choose an action on the left, then select any required records, dropdowns or options here before generating a preview/result.";
        ToolPreviewBrowser.Visibility = Visibility.Collapsed;
        ToolPreviewEmptyPanel.Visibility = Visibility.Collapsed;
        ToolInputPanel.Children.Clear();
        ToolInputPanel.Children.Add(CreateInfoCard("Tool Setup", "Choose a sub-function on the left. Tools that need dropdowns, selected records or options will show those inputs here. Generated reports and documents will appear on the Preview / Result page."));
        ShowToolSetupPage();
        ToolActionList.Children.Clear();

        var actionNumber = 1;
        foreach (var action in GetToolActions(section))
        {
            var card = new Border
            {
                Background = (System.Windows.Media.Brush)FindResource("CardAltBackgroundBrush"),
                BorderBrush = (System.Windows.Media.Brush)FindResource("BorderBrushSoft"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(16),
                Padding = new Thickness(10),
                Margin = new Thickness(0, 0, 0, 5),
                Effect = (System.Windows.Media.Effects.Effect)FindResource("CardShadowEffect")
            };

            var outer = new Grid();
            outer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            outer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            outer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var numberBadge = new Border
            {
                Width = 28,
                Height = 28,
                CornerRadius = new CornerRadius(10),
                Background = (System.Windows.Media.Brush)FindResource("AccentGradientBrush"),
                Margin = new Thickness(0, 3, 10, 0),
                VerticalAlignment = VerticalAlignment.Top
            };
            numberBadge.Child = new TextBlock
            {
                Text = actionNumber.ToString("00"),
                Foreground = System.Windows.Media.Brushes.Black,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 11
            };
            outer.Children.Add(numberBadge);

            var stack = new StackPanel();
            Grid.SetColumn(stack, 1);
            var helpKey = $"{section}|{action.Title}";
            var button = new WpfButton
            {
                Content = CreateActionButtonContent(action.Title, helpKey),
                Style = (Style)FindResource("AccentButtonStyle"),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(0, 0, 0, 5),
                ToolTip = action.Description
            };
            button.Click += action.Handler;
            stack.Children.Add(button);
            stack.Children.Add(new TextBlock
            {
                Text = action.Description,
                Foreground = (System.Windows.Media.Brush)FindResource("MutedTextBrush"),
                TextWrapping = TextWrapping.Wrap,
                FontSize = 11,
                LineHeight = 15
            });
            outer.Children.Add(stack);

            card.Child = outer;
            ToolActionList.Children.Add(card);
            actionNumber++;
        }

        StatusText.Text = $"Opened {section}. Select a compact action on the left; generated output previews on the right.";
    }

    private Grid CreateActionButtonContent(string title, string helpKey)
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var titleText = new TextBlock
        {
            Text = title,
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis,
            FontWeight = FontWeights.SemiBold
        };
        Grid.SetColumn(titleText, 0);
        grid.Children.Add(titleText);

        var helpBadge = CreateHoverHelpBadge(helpKey);
        Grid.SetColumn(helpBadge, 1);
        grid.Children.Add(helpBadge);

        return grid;
    }

    private Border CreateHoverHelpBadge(string helpKey)
    {
        var badge = new Border
        {
            Tag = helpKey,
            Width = 22,
            Height = 22,
            CornerRadius = new CornerRadius(11),
            Background = (System.Windows.Media.Brush)FindResource("CardBackgroundBrush"),
            BorderBrush = (System.Windows.Media.Brush)FindResource("BorderBrushSoft"),
            BorderThickness = new Thickness(1),
            Opacity = 0.68,
            Margin = new Thickness(12, -1, 0, -1),
            VerticalAlignment = VerticalAlignment.Center,
            Cursor = Cursors.Help,
            ToolTip = "Hover to preview; click to keep the mini guide open"
        };
        badge.Child = new TextBlock
        {
            Text = "?",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            FontSize = 12,
            FontWeight = FontWeights.Bold,
            Foreground = (System.Windows.Media.Brush)FindResource("SecondaryTextBrush")
        };
        badge.MouseEnter += HelpBadge_MouseEnter;
        badge.MouseLeave += HelpBadge_MouseLeave;
        badge.PreviewMouseLeftButtonDown += (_, e) => e.Handled = true;
        badge.PreviewMouseLeftButtonUp += HelpBadge_PreviewMouseLeftButtonUp;
        return badge;
    }

    private static string GetToolPageDescription(string section) => section switch
    {
        "Project Workbench" => "Guided next-action hub across quotes, proposals, external diamonds, production jobs, balances and customer follow-ups.",
        "Alert Centre" => "Prioritised alerts and next actions across quotes, production jobs, payments, supplier diamonds, inventory and follow-ups.",
        "Quotes & Proposals" => "Create customer quotes, compare options, generate proposals and move accepted work into production.",
        "Production" => "Run the workshop board, job handover steps, batch work and opal/stone production tools.",
        "Payments & Sales" => "Record payments, create handover paperwork, finish sales and review money that needs attention.",
        "Inventory" => "Manage finished stock, materials, stones, opals, purchasing and stock movement from one simplified home.",
        "Diamonds" => "Search Nivoda, save external diamonds, link them to quotes and track holds, orders and arrivals.",
        "Reports" => "Open the main business intelligence reports and exports without searching through every specialist report button.",
        "Settings & Backup" => "Backups, restore, health checks, settings, user guide and safe data maintenance.",
        "Custom Workflow Studio" => "Multi-option custom quotes, customer proposals, acceptance and conversion into production jobs.",
        "Diamond Supplier Studio" => "Nivoda supplier API search, connection test and saved external diamond records.",
        "Pricing Studio" => "Metal prices, live pricing refresh and retail pricing calculations.",
        "Inventory Studio" => "Stock movements, status changes, traceability and inventory reports.",
        "Purchasing Studio" => "Supplier orders, reorder suggestions and receiving material stock.",
        "Production & Opal Studio" => "Production batches, opal parcel yield, stone workflow and opal reports.",
        "Market Studio" => "Market preparation, packing, market sales, reconciliation and market reports.",
        "Online Selling Studio" => "Create listings, generate content, run listing checklists and listing reports.",
        "Tasks Studio" => "Daily work queue, follow-ups, task completion and reminder reports.",
        "Codes & Labels Studio" => "Barcode labels, scan lookup, label sheets and missing-code assignment.",
        "Documents Studio" => "Customer-facing documents, job cards, stock labels and sales paperwork.",
        "Reports Studio" => "Business intelligence, sales summaries, profitability, quote conversion, outstanding balances, inventory value, reservations and classic reports.",
        "Safety & Data Studio" => "Backups, restore, health checks, data bundles, imports and error logs.",
        "Hardware & POS Studio" => "DYMO mini labels, USB camera/photo capture, precision scale capture and market POS display tools.",
        "Customer Relationship Studio" => "Customer summary cards, follow-up creation, history and relationship overview reports.",
        "Data Cleanup Studio" => "Find duplicate or incomplete records, run data quality checks and apply safe bulk actions to selected records.",
        _ => "Choose a tool."
    };

    private IEnumerable<ToolAction> GetToolActions(string section) => section switch
    {
        "Project Workbench" => new ToolAction[]
        {
            new("Open Project Workbench", "Open the guided next-action hub for every active quote, job, supplier diamond, payment and follow-up.", ProjectWorkbench_Click),
            new("Alert Centre", "Open urgent and high-priority next actions in a focused alert list.", AlertCentre_Click),
            new("Custom Quote Builder", "Create or update a multi-option quote from the current workflow.", CustomQuoteBuilder_Click),
            new("Production Board", "Open the visual workshop pipeline.", ProductionBoard_Click),
            new("Payment & Collection", "Finish payment, pickup, shipping and sale creation.", PaymentCollection_Click),
            new("Supplier Holds & Orders", "Protect customer-approved supplier diamonds with hold/order tracking.", SupplierDiamondWorkflow_Click),
        },
        "Alert Centre" => new ToolAction[]
        {
            new("Open Alert Centre", "Review urgent quote, job, payment, supplier diamond, stock and follow-up alerts.", AlertCentre_Click),
            new("Project Workbench", "Open the broader guided next-action hub with message/context detail.", ProjectWorkbench_Click),
            new("Custom Quote Builder", "Open quote proposals and sent-proposal follow-ups.", CustomQuoteBuilder_Click),
            new("Production Board", "Open current production warnings and due jobs.", ProductionBoard_Click),
            new("Payment & Collection", "Open balances, handovers and receipts.", PaymentCollection_Click),
            new("Supplier Holds & Orders", "Open supplier diamond hold/order warnings.", SupplierDiamondWorkflow_Click),
            new("Tasks", "Open the task and follow-up queue.", (_, _) => SelectNavigationSection("Tasks")),
        },
        "Quotes & Proposals" => new ToolAction[]
        {
            new("Custom Quote Builder", "Create multi-option customer quotes, live totals, proposals and accepted jobs.", CustomQuoteBuilder_Click),
            new("Quote Register", "Review saved custom quotes, draft proposals, accepted options and converted jobs.", CustomQuoteRegister_Click),
            new("Customers", "Open customer records before creating a quote or follow-up.", (_, _) => SelectNavigationSection("Customers")),
            new("Nivoda Diamond Search", "Find supplier diamonds for a design option.", DiamondSupplier_Click),
            new("Supplier Holds & Orders", "Track external diamond holds and orders linked to quotes.", SupplierDiamondWorkflow_Click),
            new("Production Board", "Move accepted quotes through the workshop pipeline.", ProductionBoard_Click),
        },
        "Production" => new ToolAction[]
        {
            new("Production Board", "Open the visual job pipeline for current bench work.", ProductionBoard_Click),
            new("Jobs", "Open job records for detailed editing.", (_, _) => SelectNavigationSection("Jobs")),
            new("Payment & Collection", "Finish pickup, shipping, receipts and balances from completed jobs.", PaymentCollection_Click),
            new("Production Batches", "Open batch records and collection/making runs.", (_, _) => SelectNavigationSection("Production Batches")),
            new("Stone Workflow", "Move a stone through rough, cutting, polished, set and sold stages.", StoneWorkflowSetup_Click),
            new("Opal Report", "Preview opal parcel and stone workflow reporting.", OpalReport_Click),
        },
        "Payments & Sales" => new ToolAction[]
        {
            new("Payment & Collection", "Record deposits, balances, handover and sales from jobs.", PaymentCollection_Click),
            new("Sales", "Open sale records.", (_, _) => SelectNavigationSection("Sales")),
            new("Payments", "Open payment records.", (_, _) => SelectNavigationSection("Payments")),
            new("Invoice / Receipt", "Preview customer invoice or receipt paperwork.", InvoiceReceiptSetup_Click),
            new("Outstanding Balances", "Preview jobs with balances owing and handover priorities.", OutstandingBalancesReport_Click),
            new("Monthly Sales", "Preview sales and profit for the current month.", MonthlySalesReport_Click),
        },
        "Inventory" => new ToolAction[]
        {
            new("Jewellery Stock", "Open finished jewellery stock records.", (_, _) => SelectNavigationSection("Jewellery Stock")),
            new("Stones", "Open owned loose stone and opal records.", (_, _) => SelectNavigationSection("Stones")),
            new("Materials", "Open metal and material records.", (_, _) => SelectNavigationSection("Materials")),
            new("Stock Movement", "Receive, use, adjust or return material quantities.", StockMovementSetup_Click),
            new("Change Status", "Quickly update jewellery or stone status.", ChangeInventoryStatusSetup_Click),
            new("Purchase Orders", "Open supplier purchase order records.", (_, _) => SelectNavigationSection("Purchase Orders")),
            new("Inventory Value", "Preview inventory value across stock, stones and materials.", InventoryValueReport_Click),
        },
        "Diamonds" => new ToolAction[]
        {
            new("Nivoda Diamond Search", "Search external supplier diamonds and save useful results.", DiamondSupplier_Click),
            new("Saved External Diamonds", "Open saved external diamond records.", ExternalDiamondRegister_Click),
            new("Supplier Holds & Orders", "Track holds, orders, arrivals, releases and expiring supplier diamonds.", SupplierDiamondWorkflow_Click),
            new("Custom Quote Builder", "Link saved external diamonds to quote options.", CustomQuoteBuilder_Click),
            new("External Diamonds", "Open the raw external diamond record list.", (_, _) => SelectNavigationSection("External Diamonds")),
        },
        "Reports" => new ToolAction[]
        {
            new("BI Command Report", "Preview the full business intelligence report.", BusinessIntelligenceReport_Click),
            new("Weekly Sales", "Preview sales, profit and channels for the last 7 days.", WeeklySalesReport_Click),
            new("Monthly Sales", "Preview current month sales, profit and channels.", MonthlySalesReport_Click),
            new("Profitability", "Preview profit by product, service category and job type.", ProfitabilityReport_Click),
            new("Outstanding Balances", "Preview jobs with customer balances owing.", OutstandingBalancesReport_Click),
            new("Quote Conversion", "Preview quote status, acceptance and conversion performance.", QuoteConversionReport_Click),
            new("Inventory Value", "Preview stock, stone and material value tied up in inventory.", InventoryValueReport_Click),
            new("Stock Ageing", "Preview older unsold stock and slow-moving inventory value.", StockAgeingReport_Click),
            new("Export BI CSV", "Export spreadsheet-ready reporting data.", ExportBusinessIntelligenceCsv_Click),
            new("Export BI Excel", "Export one Excel-compatible workbook for business review.", ExportBusinessIntelligenceExcel_Click),
            new("Classic Reports Studio", "Open the full specialist reports list.", (_, _) => SelectNavigationSection("Reports Studio")),
        },
        "Settings & Backup" => new ToolAction[]
        {
            new("Open Settings", "Edit business details, logo, default pricing and document folders.", Settings_Click),
            new("Create Backup", "Create a safe database backup.", Backup_Click),
            new("Restore Backup", "Stage a validated restore from a backup database or export bundle.", RestoreBackup_Click),
            new("Health Check", "Check database connection, record counts, photos, low stock and overdue jobs.", HealthCheck_Click),
            new("Export Bundle", "Create a private ZIP with database snapshot, settings, photos and CSV exports.", ExportBundle_Click),
            new("User Guide", "Preview the built-in user guide.", UserGuide_Click),
            new("Release Notes", "Open the current OPALNOVA release notes.", ReleaseNotes_Click),
            new("Data Cleanup", "Open data quality and bulk-cleanup tools.", (_, _) => SelectNavigationSection("Data Cleanup Studio")),
        },
        "Custom Workflow Studio" => new ToolAction[]
        {
            new("Custom Quote Builder", "Build multiple design options, compare live costs, generate a proposal and convert the accepted option into a production job.", CustomQuoteBuilder_Click),
            new("Nivoda Diamond Search", "Search external diamond stock and save selected supplier diamonds for quote planning.", DiamondSupplier_Click),
            new("Supplier Holds & Orders", "Protect customer-approved supplier diamonds with hold, order, arrival and release tracking.", SupplierDiamondWorkflow_Click),
            new("Quote Register", "Open the custom quote records and review draft, accepted and converted quotes.", CustomQuoteRegister_Click),
            new("Production Board", "Move accepted work through approval, materials, bench work, setting, polishing, quality check and collection.", ProductionBoard_Click),
            new("Payment & Collection", "Record deposits and balances, prepare pickup or shipping, create receipts, create a sale and complete finished jobs.", PaymentCollection_Click),
        },
        "Diamond Supplier Studio" => new ToolAction[]
        {
            new("Nivoda Diamond Search", "Search Nivoda supplier diamonds using user-entered credentials and save external diamond records.", DiamondSupplier_Click),
            new("Supplier Holds & Orders", "Track linked external diamonds through hold requested, hold confirmed, ordered, received or released.", SupplierDiamondWorkflow_Click),
            new("Saved External Diamonds", "Open saved supplier diamond records that are not yet owned inventory.", ExternalDiamondRegister_Click),
        },
        "Pricing Studio" => new ToolAction[]
        {
            new("Metal Prices", "Update manual or live gold, silver, platinum and palladium prices.", MetalPrices_Click),
            new("Pricing Helper", "Estimate metal, stone, labour, total cost, margin and recommended retail.", PricingHelper_Click),
        },
        "Inventory Studio" => new ToolAction[]
        {
            new("Stock Movement", "Receive, use, adjust or return material quantities.", StockMovementSetup_Click),
            new("Change Status", "Quickly update jewellery or stone status.", ChangeInventoryStatusSetup_Click),
            new("Trace Selected", "View links between customer, job, material, stone, stock, sale and market records.", TraceSelectedSetup_Click),
            new("Inventory Report", "Generate an inventory audit and low-stock report preview.", InventoryReport_Click),
        },
        "Purchasing Studio" => new ToolAction[]
        {
            new("New Purchase Order", "Create a new supplier purchase order.", NewPurchaseOrder_Click),
            new("Reorder Suggestions", "Create draft orders from low-stock materials.", ReorderSuggestions_Click),
            new("Mark Ordered", "Move a draft purchase order to ordered status.", MarkPurchaseOrderOrderedSetup_Click),
            new("Receive Purchase Order", "Receive supplier stock into materials and transactions.", ReceivePurchaseOrderSetup_Click),
            new("Purchase Order Printout", "Preview a printable purchase order.", PurchaseOrderPrintoutSetup_Click),
            new("Reorder Report", "Preview supplier reorder suggestions and open purchasing needs.", ReorderReport_Click),
        },
        "Production & Opal Studio" => new ToolAction[]
        {
            new("Production Board", "Open the visual workshop job pipeline with due dates, customer links and quick status movement.", ProductionBoard_Click),
            new("Payment & Collection", "Finish the handover stage with balance tracking, pickup/shipping status and sale creation.", PaymentCollection_Click),
            new("New Batch", "Create a production batch or collection plan.", NewBatch_Click),
            new("Add To Batch", "Add selected jewellery, stone or job to a production batch.", AddToBatchSetup_Click),
            new("Batch Progress", "Review and update batch progress.", BatchProgressSetup_Click),
            new("Batch Report", "Preview production batch status and planned value.", BatchReportSetup_Click),
            new("Parcel Yield", "Calculate opal parcel yield, cost per finished carat and estimated profit.", ParcelYieldSetup_Click),
            new("Stone Workflow", "Move a stone through rough, cutting, polished, set and sold stages.", StoneWorkflowSetup_Click),
            new("Opal Report", "Preview opal parcel, yield and stone workflow reporting.", OpalReport_Click),
        },
        "Market Studio" => new ToolAction[]
        {
            new("Market Prep", "Prepare a selected market event and stock list.", MarketPrepSetup_Click),
            new("Market Sale", "Record a sale against market stock.", MarketSaleSetup_Click),
            new("Reconcile Market", "Enter takings, costs, packed/sold/returned figures and notes.", ReconcileMarketSetup_Click),
            new("Packing List", "Preview a printable market packing list.", MarketPackingListSetup_Click),
            new("Reconciliation Report", "Preview a market performance and reconciliation report.", MarketReconciliationReportSetup_Click),
        },
        "Online Selling Studio" => new ToolAction[]
        {
            new("Create Listing", "Create an online listing record from selected jewellery stock.", CreateListingSetup_Click),
            new("Generate Content", "Generate starter SEO title, descriptions, caption and hashtags.", GenerateListingContentSetup_Click),
            new("Listing Checklist", "Preview a listing readiness checklist.", ListingChecklistSetup_Click),
            new("Listing Report", "Preview items needing photos, description, price check or listing.", ListingReport_Click),
        },
        "Tasks Studio" => new ToolAction[]
        {
            new("New Task", "Create a standalone or linked task/reminder.", NewTaskSetup_Click),
            new("Work Queue", "Preview overdue, due today, high-priority and upcoming tasks.", WorkQueue_Click),
            new("Complete Task", "Mark the selected task completed.", CompleteTaskSetup_Click),
            new("Create Follow Ups", "Generate suggested tasks from overdue jobs, low stock, listings and markets.", CreateFollowUps_Click),
            new("Task Report", "Preview task and reminder reports by category.", TaskReport_Click),
        },
        "Customer Relationship Studio" => new ToolAction[]
        {
            new("Customer Summary Card", "Preview one customer with contact details, preferences, jobs, sales, payments and next follow-up.", CustomerSummaryCardSetup_Click),
            new("Customer Timeline", "Preview one customer's quotes, proposals, jobs, sales, payments and follow-ups in date order.", CustomerTimelineSetup_Click),
            new("Create Customer Follow-Up", "Create a linked follow-up task for the selected customer.", CustomerFollowUpSetup_Click),
            new("Customer History", "Preview the existing detailed customer purchase/job history report.", CustomerHistorySetup_Click),
            new("Relationship Report", "Preview all customers with sales, active jobs, open follow-ups and last activity.", CustomerRelationshipReport_Click),
        },
        "Codes & Labels Studio" => new ToolAction[]
        {
            new("Scan / Lookup Code", "Scan or type a code to find the matching record.", ScanLookup_Click),
            new("Generate Selected Scan Label", "Generate a barcode label for the selected record.", GenerateSelectedScanLabelSetup_Click),
            new("Generate Label Sheet", "Generate a label sheet for the active section.", GenerateScanLabelSheetSetup_Click),
            new("Assign Missing Codes", "Auto-fill missing stock, stone, job, material, PO, batch and task codes.", AssignMissingCodes_Click),
        },
        "Documents Studio" => new ToolAction[]
        {
            new("Custom Quote Builder", "Build a multi-option customer proposal and convert the accepted option into a production job.", CustomQuoteBuilder_Click),
            new("Payment & Collection", "Record job payments, generate invoice/receipt paperwork and finish customer handover.", PaymentCollection_Click),
            new("Job Card", "Preview a bench job card for the selected job.", JobCardSetup_Click),
            new("Stock Label", "Preview a printable stock/price label.", StockLabelSetup_Click),
            new("Quote", "Preview a customer quote for the selected job.", QuoteSetup_Click),
            new("Invoice / Receipt", "Preview an invoice or receipt from a job or sale.", InvoiceReceiptSetup_Click),
            new("Deposit Receipt", "Preview a deposit receipt for a job or payment.", DepositReceiptSetup_Click),
            new("Repair Form", "Preview a repair intake form.", RepairFormSetup_Click),
            new("Agreement", "Preview a custom order agreement.", AgreementSetup_Click),
            new("Payment Summary", "Preview a payment summary for a customer, job or sale.", PaymentSummarySetup_Click),
            new("Customer History", "Preview a customer purchase/job history report.", CustomerHistorySetup_Click),
        },
        "Reports Studio" => new ToolAction[]
        {
            new("BI Command Report", "Preview the full business intelligence dashboard report: sales, profit, quotes, balances, inventory value, reservations and follow-ups.", BusinessIntelligenceReport_Click),
            new("Weekly Sales", "Preview sales, profit, margin and channel performance for the last 7 days.", WeeklySalesReport_Click),
            new("Monthly Sales", "Preview sales, profit, margin and channel performance for the current month.", MonthlySalesReport_Click),
            new("Profitability", "Preview profit by product/service category and job type, with data quality checks.", ProfitabilityReport_Click),
            new("Outstanding Balances", "Preview jobs with balances owing and handover/payment priorities.", OutstandingBalancesReport_Click),
            new("Quote Conversion", "Preview quote status, acceptance and conversion performance.", QuoteConversionReport_Click),
            new("Inventory Value", "Preview finished jewellery, loose stone and material value tied up in stock.", InventoryValueReport_Click),
            new("Stock Ageing", "Preview unsold jewellery and loose stones by age band and slow-moving value.", StockAgeingReport_Click),
            new("Reserved Inventory", "Preview stones and materials reserved against accepted quote options.", ReservedInventoryReport_Click),
            new("Customer Follow-Ups", "Preview open follow-ups and tasks by priority and due date.", CustomerFollowUpInsightReport_Click),
            new("Opal / Stone Stock", "Preview stone inventory, opal values, weights and statuses.", OpalStoneStockReport_Click),
            new("Export BI CSV", "Export sales, balances, quotes, inventory and reservation data to spreadsheet-ready CSV files.", ExportBusinessIntelligenceCsv_Click),
            new("Export BI Excel", "Export sales, balances, quotes, inventory, tasks and supplier diamonds to one Excel-compatible workbook.", ExportBusinessIntelligenceExcel_Click),
            new("Business Report", "Preview the existing high-level business report.", BusinessReport_Click),
            new("Costing Report", "Preview pricing, profit and margin reports.", CostingReport_Click),
            new("Low Stock", "Preview materials below reorder level.", LowStockReport_Click),
            new("Jobs Due", "Preview jobs due soon or overdue.", JobsDueReport_Click),
            new("Market Report", "Preview market event performance.", MarketReport_Click),
            new("Inventory Report", "Preview inventory audit and movements.", InventoryReport_Click),
            new("Reorder Report", "Preview purchasing and reorder needs.", ReorderReport_Click),
            new("Task Report", "Preview task and reminder status.", TaskReport_Click),
        },
        "Safety & Data Studio" => new ToolAction[]
        {
            new("Create Backup", "Create a safe database backup.", Backup_Click),
            new("Restore Backup", "Stage a validated restore from a backup database or export bundle.", RestoreBackup_Click),
            new("Health Check", "Check database connection, record counts, photos, low stock and overdue jobs.", HealthCheck_Click),
            new("Export Bundle", "Create a private ZIP with database snapshot, settings, photos and CSV exports.", ExportBundle_Click),
            new("Import CSV", "Import rows into the active record section from matching CSV headers.", ImportCsv_Click),
            new("Error Log", "Open a copy of the application error log.", ErrorLog_Click),
            new("User Guide", "Preview the built-in user guide.", UserGuide_Click),
            new("Release Notes", "Open the current OPALNOVA release notes.", ReleaseNotes_Click),
            new("About", "Show version, paths and app information.", About_Click),
        },
        "Hardware & POS Studio" => new ToolAction[]
        {
            new("DYMO Mini Label", "Create and print a mini label for the selected stock, stone, job or material using the Windows print dialog.", DymoMiniLabelSetup_Click),
            new("Camera & Scale Capture", "Import USB camera photos and pull scale readings into selected materials or stones.", DeviceCaptureSetup_Click),
            new("Market Operations Window", "Open a separate market POS/operator window with optional customer display on another monitor.", MarketOperationsWindow_Click),
            new("Device Setup Notes", "Preview setup notes for DYMO printers, USB scales, camera capture and multi-display market mode.", DeviceSetupNotes_Click),
        },
        "Data Cleanup Studio" => new ToolAction[]
        {
            new("Data Quality Report", "Preview a full data quality audit: duplicates, missing details, broken links and cleanup priorities.", DataQualityReport_Click),
            new("Duplicate Finder", "Find duplicate customer names, supplier names, stock codes, stone codes, job codes and material codes.", DuplicateFinder_Click),
            new("Missing Data Report", "Find missing prices, photos, contact details, listing content and overdue records.", MissingDataReport_Click),
            new("Bulk Status Update", "Update the status of selected records in the current table, such as stock, stones, jobs, listings, tasks or purchase orders.", BulkStatusUpdate_Click),
            new("Bulk Add Selected To Market", "Add selected jewellery stock records to the next upcoming market and mark them At Market.", BulkAddSelectedToMarket_Click),
            new("Create Cleanup Tasks", "Create follow-up tasks from low-stock materials, overdue jobs, listings needing work and stock needing cleanup.", CreateCleanupTasks_Click),
        },
        _ => Array.Empty<ToolAction>()
    };

    private void HelpBadge_MouseEnter(object sender, MouseEventArgs e)
    {
        if (_hoverHelpPinned)
        {
            return;
        }

        if (sender is FrameworkElement element && element.Tag is string key)
        {
            ShowHoverHelpWindow(key, element, pinned: false);
        }
    }

    private void HelpBadge_MouseLeave(object sender, MouseEventArgs e)
    {
        if (!_hoverHelpPinned && sender is FrameworkElement element && ReferenceEquals(_hoverHelpSource, element))
        {
            CloseHoverHelpWindow();
        }
    }

    private void HelpBadge_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;

        if (sender is not FrameworkElement element || element.Tag is not string key)
        {
            return;
        }

        if (_hoverHelpWindow != null && _hoverHelpWindow.IsVisible && _hoverHelpKey == key)
        {
            if (_hoverHelpPinned)
            {
                CloseHoverHelpWindow();
            }
            else
            {
                _hoverHelpPinned = true;
                _hoverHelpSource = element;
                _hoverHelpWindow.Title = _hoverHelpWindow.Title.Replace(" — preview", " — pinned");
            }
            return;
        }

        CloseHoverHelpWindow();
        ShowHoverHelpWindow(key, element, pinned: true);
    }

    private void CloseHoverHelpWindow()
    {
        var window = _hoverHelpWindow;
        _hoverHelpWindow = null;
        _hoverHelpKey = null;
        _hoverHelpPinned = false;
        _hoverHelpSource = null;
        window?.Close();
    }

    private void ShowHoverHelpWindow(string key, FrameworkElement sourceElement, bool pinned = false)
    {
        if (_hoverHelpWindow != null && _hoverHelpKey == key && _hoverHelpWindow.IsVisible)
        {
            if (pinned)
            {
                _hoverHelpPinned = true;
                _hoverHelpSource = sourceElement;
                _hoverHelpWindow.Title = _hoverHelpWindow.Title.Replace(" — preview", " — pinned");
            }
            return;
        }

        CloseHoverHelpWindow();
        _hoverHelpKey = key;
        _hoverHelpPinned = pinned;
        _hoverHelpSource = sourceElement;

        var guide = GetHelpGuide(key);
        var helpWindow = new Window
        {
            Title = $"OPALNOVA Mini Guide — {guide.Title} — {(pinned ? "pinned" : "preview")}",
            Owner = this,
            Width = 390,
            Height = 360,
            MinWidth = 340,
            MinHeight = 280,
            WindowStartupLocation = WindowStartupLocation.Manual,
            ResizeMode = ResizeMode.CanResize,
            ShowInTaskbar = false,
            Topmost = true,
            Background = (System.Windows.Media.Brush)FindResource("PanelBackgroundBrush")
        };

        helpWindow.Closed += (_, _) =>
        {
            if (ReferenceEquals(_hoverHelpWindow, helpWindow))
            {
                _hoverHelpWindow = null;
                _hoverHelpKey = null;
                _hoverHelpPinned = false;
                _hoverHelpSource = null;
            }
        };

        var screenPoint = sourceElement.PointToScreen(new Point(sourceElement.ActualWidth + 10, -8));
        var workArea = SystemParameters.WorkArea;
        var left = Math.Min(screenPoint.X, workArea.Right - helpWindow.Width - 12);
        var top = Math.Min(screenPoint.Y, workArea.Bottom - helpWindow.Height - 12);
        helpWindow.Left = Math.Max(workArea.Left + 12, left);
        helpWindow.Top = Math.Max(workArea.Top + 12, top);

        var root = new Border
        {
            Padding = new Thickness(14),
            Background = (System.Windows.Media.Brush)FindResource("PanelBackgroundBrush"),
            BorderBrush = (System.Windows.Media.Brush)FindResource("BorderBrushSoft"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(14)
        };

        var layout = new DockPanel();
        var closeButton = new WpfButton
        {
            Content = "Close",
            Style = (Style)FindResource("ToolbarButtonStyle"),
            HorizontalAlignment = HorizontalAlignment.Right,
            MinWidth = 70,
            Margin = new Thickness(0, 10, 0, 0)
        };
        closeButton.Click += (_, _) => helpWindow.Close();
        DockPanel.SetDock(closeButton, Dock.Bottom);
        layout.Children.Add(closeButton);

        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        var stack = new StackPanel();
        stack.Children.Add(new TextBlock
        {
            Text = guide.Title,
            FontSize = 20,
            FontWeight = FontWeights.Bold,
            Foreground = (System.Windows.Media.Brush)FindResource("PrimaryTextBrush"),
            TextWrapping = TextWrapping.Wrap
        });
        stack.Children.Add(new TextBlock
        {
            Text = guide.Purpose,
            FontSize = 12,
            Foreground = (System.Windows.Media.Brush)FindResource("MutedTextBrush"),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 4, 0, 10)
        });
        stack.Children.Add(CreateCompactHelpText("How to use", guide.HowToUse));
        stack.Children.Add(CreateCompactHelpText("Good habit", guide.GoodHabit));
        stack.Children.Add(CreateCompactHelpText("Watch out", guide.WatchOut));
        scroll.Content = stack;
        layout.Children.Add(scroll);
        root.Child = layout;
        helpWindow.Content = root;
        _hoverHelpWindow = helpWindow;
        helpWindow.Show();
    }

    private Border CreateCompactHelpText(string heading, string body)
    {
        var border = new Border
        {
            Background = (System.Windows.Media.Brush)FindResource("CardBackgroundBrush"),
            BorderBrush = (System.Windows.Media.Brush)FindResource("BorderBrushSoft"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(10),
            Margin = new Thickness(0, 0, 0, 8)
        };
        var stack = new StackPanel();
        stack.Children.Add(new TextBlock
        {
            Text = heading,
            FontSize = 13,
            FontWeight = FontWeights.Bold,
            Foreground = (System.Windows.Media.Brush)FindResource("PrimaryTextBrush")
        });
        stack.Children.Add(new TextBlock
        {
            Text = body,
            FontSize = 12,
            Foreground = (System.Windows.Media.Brush)FindResource("SecondaryTextBrush"),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 3, 0, 0)
        });
        border.Child = stack;
        return border;
    }

    private void HelpButton_Click(object sender, RoutedEventArgs e)
    {
        var key = sender is FrameworkElement element && element.Tag is string tag
            ? tag
            : $"{CurrentSection}|Overview";

        ShowFloatingHelpWindow(key);
    }

    private void ShowFloatingHelpWindow(string key)
    {
        var guide = GetHelpGuide(key);
        var helpWindow = new Window
        {
            Title = $"OPALNOVA Help — {guide.Title}",
            Owner = this,
            Width = 420,
            Height = 540,
            MinWidth = 360,
            MinHeight = 420,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            ResizeMode = ResizeMode.CanResize,
            ShowInTaskbar = false,
            Topmost = false,
            Background = (System.Windows.Media.Brush)FindResource("PanelBackgroundBrush")
        };

        var root = new Border
        {
            Padding = new Thickness(18),
            Background = (System.Windows.Media.Brush)FindResource("PanelBackgroundBrush")
        };

        var layout = new DockPanel();
        var closeButton = new WpfButton
        {
            Content = "Close",
            Style = (Style)FindResource("AccentButtonStyle"),
            HorizontalAlignment = HorizontalAlignment.Right,
            MinWidth = 84,
            Margin = new Thickness(0, 14, 0, 0)
        };
        closeButton.Click += (_, _) => helpWindow.Close();
        DockPanel.SetDock(closeButton, Dock.Bottom);
        layout.Children.Add(closeButton);

        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        var stack = new StackPanel();
        stack.Children.Add(new TextBlock
        {
            Text = guide.Title,
            FontSize = 24,
            FontWeight = FontWeights.Bold,
            Foreground = (System.Windows.Media.Brush)FindResource("PrimaryTextBrush"),
            TextWrapping = TextWrapping.Wrap
        });
        stack.Children.Add(new TextBlock
        {
            Text = guide.Purpose,
            FontSize = 13,
            Foreground = (System.Windows.Media.Brush)FindResource("MutedTextBrush"),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 6, 0, 14)
        });
        stack.Children.Add(CreateHelpSection("What this does", guide.WhatItDoes));
        stack.Children.Add(CreateHelpSection("How to use it", guide.HowToUse));
        stack.Children.Add(CreateHelpSection("Good habit", guide.GoodHabit));
        stack.Children.Add(CreateHelpSection("Watch out for", guide.WatchOut));
        scroll.Content = stack;
        layout.Children.Add(scroll);
        root.Child = layout;
        helpWindow.Content = root;
        helpWindow.Show();
    }

    private Border CreateHelpSection(string heading, string body)
    {
        var border = new Border
        {
            Background = (System.Windows.Media.Brush)FindResource("CardBackgroundBrush"),
            BorderBrush = (System.Windows.Media.Brush)FindResource("BorderBrushSoft"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(14),
            Padding = new Thickness(12),
            Margin = new Thickness(0, 0, 0, 10)
        };
        var stack = new StackPanel();
        stack.Children.Add(new TextBlock
        {
            Text = heading,
            FontSize = 15,
            FontWeight = FontWeights.Bold,
            Foreground = (System.Windows.Media.Brush)FindResource("PrimaryTextBrush"),
            Margin = new Thickness(0, 0, 0, 5)
        });
        stack.Children.Add(new TextBlock
        {
            Text = body,
            FontSize = 12.5,
            Foreground = (System.Windows.Media.Brush)FindResource("SecondaryTextBrush"),
            TextWrapping = TextWrapping.Wrap,
            LineHeight = 18
        });
        border.Child = stack;
        return border;
    }

    private sealed record HelpGuide(string Title, string Purpose, string WhatItDoes, string HowToUse, string GoodHabit, string WatchOut);

    private HelpGuide GetHelpGuide(string key)
    {
        var parts = key.Split('|', 2);
        var section = parts.Length > 0 ? parts[0] : CurrentSection;
        var action = parts.Length > 1 ? parts[1] : "Overview";
        var mapKey = $"{section}|{action}";

        if (HelpGuides.TryGetValue(mapKey, out var exact)) return exact;
        if (SectionHelpGuides.TryGetValue(section, out var sectionGuide)) return sectionGuide;

        return new HelpGuide(
            action == "Overview" ? "Current Page Help" : action,
            "A quick guide for the current OPALNOVA workspace.",
            "This area helps you manage records, generate documents, preview reports or prepare daily jewellery business work.",
            "Choose the related workspace from the left navigation, select the record you want when needed, then use the main action button. Generated files usually appear in the Preview / Result panel first.",
            "Create a backup before large changes, imports, restores or bulk updates.",
            "Most actions depend on the selected record or active section. If a button does not do what you expect, check the current section and selected row first.");
    }

    private static readonly Dictionary<string, HelpGuide> SectionHelpGuides = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Quotes & Proposals"] = new("Quotes & Proposals", "Main quote workflow home.", "Keeps the quote, proposal, supplier diamond and production-start actions together so quoting does not require hunting through specialist tools.", "Start with Custom Quote Builder, use Quote Register to return to existing quotes, and use supplier diamond actions only when a quote needs an external stone.", "Keep the customer and quote option details clean before generating a proposal.", "Supplier diamonds remain external until held, ordered and received. Do not treat them as owned stock."),
        ["Production"] = new("Production", "Workshop workflow home.", "Opens the production board, job records, batch tools and opal/stone production actions from one place.", "Use Production Board for daily work and job records only when detailed editing is required.", "Check overdue and due-this-week work each morning.", "Moving job statuses too quickly can skip payment, collection or quality-check steps."),
        ["Payments & Sales"] = new("Payments & Sales", "Handover and money workflow home.", "Groups payment collection, sales records, payment records, receipts and balance reports.", "Use Payment & Collection for most finished jobs, then use Sales or Payments records only for detailed auditing.", "Check Outstanding Balances before marking work complete.", "Avoid creating duplicate sales from the same completed job."),
        ["Inventory"] = new("Inventory", "Stock and material workflow home.", "Brings finished jewellery, stones, materials, stock movements, purchase orders and inventory value into one simplified place.", "Open records for normal updates and use Stock Movement/Change Status for transaction-style updates.", "Record material movements immediately and keep unique stones traceable.", "Bulk status or movement changes affect availability and quote reservations."),
        ["Diamonds"] = new("Diamonds", "External supplier diamond workflow home.", "Keeps Nivoda search, saved external diamonds, holds/orders and quote linking together.", "Search broadly, save useful diamonds, link one to a quote, then request hold/order once the customer is serious.", "Use hold expiry and order alerts to avoid losing supplier stones after quoting.", "Search results can become unavailable. Confirm with the supplier before promising availability."),
        ["Reports"] = new("Reports", "Simplified reporting home.", "Shows the most useful business intelligence reports without exposing every specialist report at once.", "Start with BI Command Report, then use weekly/monthly sales, balances, conversion or inventory value as needed.", "Run a weekly report rhythm so slow-moving stock, balances and follow-ups are caught early.", "Reports are only accurate if prices, statuses, payments and dates are kept updated."),
        ["Settings & Backup"] = new("Settings & Backup", "Admin and safety home.", "Groups settings, backups, restore, health checks, export bundles, user guide and cleanup access.", "Create Backup before imports, restores, data cleanup or large workflow changes.", "Keep at least one backup outside the computer.", "Restore and cleanup tools can change a lot of data. Read prompts carefully."),
        ["Dashboard"] = new("Dashboard", "Your business overview screen.", "Shows important totals and clickable tiles for jobs, sales, stock, materials, markets, tasks and online listing work.", "Click a tile to jump to the related records. Use Search All at the top when you are looking for a customer, code, job or stock item across the whole app.", "Use this screen at the start of the day to check overdue tasks, jobs due soon and low stock.", "Dashboard totals are only useful if records are kept up to date."),
        ["Diamond Supplier Studio"] = new("Diamond Supplier Studio", "External diamond supply search.", "Connects OPALNOVA to the Nivoda supplier API so diamonds can be searched and saved separately from owned stones.", "Enter credentials, test the connection, search by shape/carat/lab and save useful rows as External Diamond records.", "Use this for custom quotes where the customer wants a diamond you do not hold in stock yet.", "Supplier availability can change quickly. Confirm holds/orders in Nivoda before promising a stone to a customer."),
        ["Pricing Studio"] = new("Pricing Studio", "Pricing and margin tools.", "Helps update metal prices and estimate retail pricing from materials, stones, labour, markup and GST-style pricing assumptions.", "Open Pricing Helper, enter costs and labour, then use the result as a guide before creating a quote or sale.", "Keep your gold, silver and labour assumptions updated before quoting customers.", "Treat calculated pricing as a guide. Always check unusual designs, high-risk repairs or rare opals manually."),
        ["Inventory Studio"] = new("Inventory Studio", "Stock control and traceability.", "Handles material movements, jewellery/stone status changes, trace reports and inventory audits.", "Select the relevant material, stone or jewellery record first, then choose the tool you need.", "Record stock movements when they happen so cost and quantity figures stay reliable.", "Bulk or status changes can make records look sold, reserved or unavailable. Check the selected record before applying."),
        ["Purchasing Studio"] = new("Purchasing Studio", "Supplier ordering workflow.", "Creates purchase orders, suggests reorders and receives ordered stock into materials.", "Use reorder suggestions for low materials, create or mark orders as ordered, then receive them when stock arrives.", "Enter supplier and material details before relying on reorder suggestions.", "Receiving a purchase order may update stock quantities. Confirm quantities and units first."),
        ["Production & Opal Studio"] = new("Production & Opal Studio", "Making, batches and opal workflow.", "Tracks production batches, opal parcel yield and stone stage movement from rough through polished, set or sold.", "Create batches for collections, add items or stones, then use reports to monitor progress and value.", "Photograph and code stones early so they remain traceable through cutting, setting and sale.", "Yield and profit estimates depend on accurate rough cost, finished weight and status updates."),
        ["Market Studio"] = new("Market Studio", "Market preparation and takings.", "Prepares packing lists, records market sales and reconciles packed, sold and returned stock.", "Create a market event, add stock, print packing lists, then reconcile after the market.", "Reconcile the same day while takings and returned stock are fresh.", "Check that market stock is assigned to the correct event before recording sales."),
        ["Online Selling Studio"] = new("Online Selling Studio", "Online listing workflow.", "Creates listing records, generates starter content and checks readiness for website, marketplace or social listings.", "Select finished stock, create a listing, generate content, then complete photos, pricing and publish checks.", "Review generated content before publishing so it matches the exact stone, metal and condition.", "Generated descriptions are a starting point, not a substitute for accurate gemstone disclosure."),
        ["Tasks Studio"] = new("Tasks Studio", "Daily reminders and follow-ups.", "Creates tasks, completes tasks, previews the work queue and suggests follow-ups from business records.", "Use Work Queue each morning, then create or complete linked tasks as jobs, customers and stock move forward.", "Link tasks to customers or jobs whenever possible so follow-ups have context.", "Creating many suggested follow-ups can clutter the queue if you do not review them."),
        ["Codes & Labels Studio"] = new("Codes & Labels Studio", "Codes, barcode lookup and labels.", "Finds records by code, creates labels and fills missing codes for stock, stones, jobs, materials and tasks.", "Use Scan / Lookup for existing labels. Use selected label tools after choosing the record you want to label.", "Label valuable stones and stock as soon as they enter your system.", "Auto-assign missing codes only after backing up, especially if you already use a handwritten code system."),
        ["Documents Studio"] = new("Documents Studio", "Customer-facing paperwork.", "Generates job cards, labels, quotes, invoices, receipts, repair forms, agreements and payment summaries.", "Select the relevant customer, job, sale or stock record, then generate the document and open it for printing.", "Preview before printing or sending so customer details, prices and dates are correct.", "Documents reflect the current record data. Update records before creating final paperwork."),
        ["Reports Studio"] = new("Reports Studio", "Business reporting.", "Creates reports for business overview, costing, low stock, jobs due, market performance, inventory, purchasing and tasks.", "Choose the report you need and review it in Preview / Result. Use Open HTML / Print for a printable version.", "Run reports weekly so issues are found before they become urgent.", "Reports depend on clean data. Missing prices, dates or statuses can distort results."),
        ["Safety & Data Studio"] = new("Safety & Data Studio", "Backups, exports and recovery.", "Creates backups, restores from validated files, checks database health, exports bundles, imports CSV and opens logs/help.", "Create Backup before risky work. Use Health Check when something looks wrong. Use Export Bundle when moving or archiving data.", "Keep a backup outside the computer, such as an external drive or cloud folder.", "Restore and import tools can change data. Read prompts carefully and avoid using them while another copy of OPALNOVA is running."),
        ["Hardware & POS Studio"] = new("Hardware & POS Studio", "Printers, camera/scale capture and market POS.", "Supports DYMO-style labels, camera/photo capture, scale notes and a market operations display.", "Set up devices in Windows first, then use OPALNOVA to print labels, capture photos or open market operations tools.", "Test printers and labels before a market day.", "Hardware behaviour depends on Windows drivers, printer setup and connected device compatibility."),
        ["Customer Relationship Studio"] = new("Customer Relationship Studio", "Customer history and follow-up tools.", "Shows customer summaries, history, open follow-ups, jobs, sales, payments and relationship reports.", "Select a customer, open the summary or history, then create a follow-up task when action is needed.", "Record preferences like ring sizes, favourite stones and budget notes to improve repeat customer service.", "Customer reports are only as useful as the linked jobs, sales and payments entered."),
        ["Customer Relationship Studio|Customer Timeline"] = new("Customer Timeline", "See one customer's activity trail.", "Combines existing quotes, proposal sent events, jobs, sales, payments and customer tasks into one dated report.", "Use it before contacting a repeat customer so you can see recent work and open actions in one place.", "Create a follow-up from the same studio when the timeline shows a next step.", "The timeline reflects linked records only; unlinked payments or jobs will not appear."),
        ["Data Cleanup Studio"] = new("Data Cleanup Studio", "Data quality and safe cleanup.", "Finds duplicates, missing information and selected records that need status or market cleanup.", "Run reports first, review the affected records, then apply selected bulk actions only when you are confident.", "Backup before data cleanup or bulk updates.", "Cleanup actions can change many records at once. Avoid rushing these tools."),
    };

    private static readonly Dictionary<string, HelpGuide> HelpGuides = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Diamond Supplier Studio|Nivoda Diamond Search"] = new("Nivoda Diamond Search", "Search supplier diamonds.", "Uses the Nivoda GraphQL endpoint with username/password authentication to retrieve matching diamond rows.", "Start with broad filters, then tighten carat/lab/shape once the result set is working.", "If GraphQL errors appear, open GraphiQL and check whether the schema field names have changed.", "Search results are external supplier options, not physical owned inventory."),
        ["Diamond Supplier Studio|Saved External Diamonds"] = new("Saved External Diamonds", "Review saved supplier stones.", "Shows external diamond records saved from the API search.", "Use this to compare supplier stones, prices, certificates and quote candidates later.", "Saved external diamonds should move through hold/order/received statuses before being treated as in-house stock.", "Saving a result does not reserve it with the supplier."),
        ["Pricing Studio|Metal Prices"] = new("Metal Prices", "Update metal prices used by pricing tools.", "Lets you refresh or enter current gold, silver, platinum and palladium price assumptions.", "Open the tool, review the values, update them if needed, then refresh pricing calculations.", "Update before quoting or repricing stock.", "Market metal prices move. Old prices can make quotes inaccurate."),
        ["Pricing Studio|Pricing Helper"] = new("Pricing Helper", "Estimate retail pricing.", "Combines material, stone, labour and margin assumptions into a suggested retail figure.", "Enter the cost inputs, labour time and markup settings, then compare the suggested price with your judgement.", "Use consistent labour rates and write down why you override the suggestion.", "High-value opals, custom risk and difficult repairs may need manual pricing."),
        ["Inventory Studio|Stock Movement"] = new("Stock Movement", "Record material stock in or out.", "Adds a transaction for receiving, using, adjusting or returning material quantities.", "Select the material, choose movement type and enter quantity/notes.", "Record small adjustments immediately so stock figures stay trustworthy.", "Double-check units such as grams, carats, pieces or metres."),
        ["Inventory Studio|Change Status"] = new("Change Status", "Change stock or stone status.", "Moves jewellery or stones between statuses such as available, reserved, sold, at market or in production.", "Select a record, choose the new status, then save.", "Use status updates instead of deleting records when items move through the business.", "Wrong status can hide items from the workflow you expect."),
        ["Customer Relationship Studio|Create Customer Follow-Up"] = new("Create Customer Follow-Up", "Make a reminder linked to a customer.", "Creates a task for calling, messaging, quote checking, pickup reminders or after-sale follow-up.", "Select a customer, enter the reason and due date, then save the generated task.", "Use follow-ups after every quote, custom job and important sale.", "Do not put sensitive private information in notes unless you really need it for business service."),
        ["Documents Studio|Quote"] = new("Quote", "Create a customer quote preview.", "Generates quote paperwork from a selected job, price and customer information.", "Select the job/customer, confirm prices and details, generate preview, then open or print the HTML output.", "Always check expiry date, deposit terms and item details before sending.", "Quotes can create expectations. Make sure labour, stone and metal details are accurate."),

        ["Custom Workflow Studio|Custom Quote Builder"] = new("Custom Quote Builder", "Create one quote with several design and price options.", "Combines customer requirements, labour, metal, stones, setting, findings, markup and deposit information in one connected workflow.", "Choose or create a quote, add options, enter costs, save, preview the proposal, accept the chosen option, then create the production job.", "Save before previewing or converting. Review the live total and customer details carefully.", "The accepted option preserves the quoted price when the job is created."),
        ["Custom Workflow Studio|Quote Register"] = new("Quote Register", "Review custom quotes already saved in OPALNOVA.", "Shows quote records so you can return to drafts or check workflow status.", "Open the register, locate the quote, then reopen it from Custom Quote Builder.", "Keep quote codes unique and descriptive.", "Use statuses to distinguish drafts, accepted proposals and converted jobs."),
        ["Custom Workflow Studio|Production Board"] = new("Production Board", "See every active job in its workshop stage.", "Shows customer, quote, due date, balance and production status in a visual pipeline.", "Select a card, then move it forward or back; double-click to edit the full job.", "Set realistic due dates so overdue highlighting is useful.", "Accepted quote jobs appear automatically because the board reads the existing Jobs register."),
        ["Custom Workflow Studio|Payment & Collection"] = new("Payment & Collection", "Complete the customer handover stage.", "Records deposits and progress payments, calculates balances, creates invoice/receipt paperwork, creates pickup reminders, marks jobs ready/collected/shipped and creates the final sale record.", "Select a job, record any payment, generate the receipt, then mark the handover status when the item is collected or shipped.", "Use this as the final checkpoint before closing a custom job.", "Check the balance and customer details before marking a job complete."),
        ["Production & Opal Studio|Production Board"] = new("Production Board", "Run the daily workshop from a visual job pipeline.", "Groups jobs by stage and highlights overdue work.", "Filter, select, move or edit job cards as work progresses.", "Move jobs only when the physical work has reached that stage.", "New setting, polishing and quality-check stages preserve existing job data."),
        ["Production & Opal Studio|Payment & Collection"] = new("Payment & Collection", "Finish jobs cleanly.", "Shows handover-stage jobs with total, paid and balance figures, then helps create receipts, sale records and complete statuses.", "Open it near the end of production, select the finished job, take payment, then mark collection or shipping.", "Do not complete the job until the item has physically left or is ready to archive.", "A remaining balance warning can be overridden, so read it carefully."),
        ["Documents Studio|Custom Quote Builder"] = new("Custom Quote Builder", "Create a professional multi-option jewellery proposal.", "Builds connected quote options and customer-facing proposal output.", "Add costs and descriptions, save, then preview the proposal.", "Check all prices and terms before sending.", "Accepted options can become production jobs without retyping information."),        ["Documents Studio|Payment & Collection"] = new("Payment & Collection", "Payments, receipts and handover paperwork.", "Brings payment entry, invoice/receipt generation, sale creation and final collection/shipping status into one place.", "Select a job, enter payment details, generate paperwork, then mark the job ready, collected or shipped.", "Use it after quality check so the business record matches the customer handover.", "Make sure payment method and reference are correct before printing receipts."),
        ["Documents Studio|Invoice / Receipt"] = new("Invoice / Receipt", "Create sales paperwork.", "Generates an invoice or receipt from a sale or job-related payment.", "Select the job or sale, generate the document, preview it, then print or save.", "Create receipts as soon as payment is received.", "Make sure the paid amount and balance are correct before handing it to a customer."),
        ["Reports Studio|BI Command Report"] = new("BI Command Report", "One report for the whole business.", "Combines sales, profit, balances, quote conversion, inventory value, reserved inventory and open follow-ups into one command report.", "Open it weekly or before planning production, then use the highlighted sections to decide what needs action first.", "Use this report as your Monday morning business check.", "The report is only as accurate as the prices, costs, statuses and links entered in OPALNOVA."),
        ["Reports Studio|Weekly Sales"] = new("Weekly Sales", "Review the last 7 days.", "Shows weekly sales, cost of goods, profit, margin and channel performance.", "Run it after markets, online drops or custom-job handovers.", "Compare weekly sales to the jobs and listings that created them.", "Sales without cost-of-goods entries can make profit look higher than reality."),
        ["Reports Studio|Monthly Sales"] = new("Monthly Sales", "Review the current month.", "Shows month-to-date sales, cost of goods, profit, margin and channel performance.", "Use it before ordering materials, paying bills or planning new stock.", "Look for channels with strong profit, not just high turnover.", "Incomplete sale records or missing costs will affect the result."),
        ["Reports Studio|Profitability"] = new("Profitability", "Review what actually makes money.", "Shows recorded sales profit by product/service category, recorded job-sales profit by job type, estimated job profit by job type, and data-quality checks for missing links or costs.", "Run it before deciding what to make, quote, discount or promote next.", "Use profit, margin and average sale together; high turnover does not always mean strong profit.", "Unlinked sales and zero-cost sales can make categories look wrong until the records are cleaned up."),
        ["Reports Studio|Outstanding Balances"] = new("Outstanding Balances", "Find money still owed.", "Lists jobs with balances owing so collection, shipping and completion are not missed.", "Check it before marking jobs complete and before customer pickup days.", "Use Payment & Collection to record payments and clear balances.", "A balance may be wrong if payments were not linked to the job."),
        ["Reports Studio|Quote Conversion"] = new("Quote Conversion", "Measure quote performance.", "Shows quote statuses, accepted options, linked jobs and the overall conversion rate.", "Use it to follow up draft or sent quotes and improve pricing or proposal wording.", "A quote only counts as converted when it is marked accepted, has an accepted option or is linked to a job.", "Old draft quotes can lower the conversion rate unless they are cancelled or closed."),
        ["Reports Studio|Inventory Value"] = new("Inventory Value", "See money tied up in stock.", "Summarises finished jewellery, loose stones and materials by cost, value and retail potential.", "Use it before buying more stock or preparing for a market.", "This helps prevent cash being trapped in slow-moving inventory.", "Estimated values depend on the values you recorded for stones, materials and finished pieces."),
        ["Reports Studio|Stock Ageing"] = new("Stock Ageing", "Find slow-moving stock.", "Groups unsold jewellery and available loose stones by age band and lists records older than 180 days.", "Use it before buying more stock, planning sales, or choosing pieces for markets and online listing.", "Ageing is read-only and does not change inventory status.", "Age is based on the record creation date, so imported legacy records may need manual interpretation."),
        ["Reports Studio|Reserved Inventory"] = new("Reserved Inventory", "See committed stones and materials.", "Lists inventory reserved against accepted quote options so the same stone or material is not accidentally promised twice.", "Run it before starting production or changing an accepted custom job.", "Release reservations when a quote is cancelled or redesigned.", "This is reservation value, not physical stock consumption."),
        ["Reports Studio|Customer Follow-Ups"] = new("Customer Follow-Ups", "Keep customer actions visible.", "Lists open tasks and reminders by due date, priority, customer and job link.", "Use it daily to decide who needs a message, pickup reminder, approval request or after-sale follow-up.", "Good follow-ups turn quotes into jobs and customers into repeat customers.", "Sensitive notes should be kept professional and necessary."),
        ["Reports Studio|Opal / Stone Stock"] = new("Opal / Stone Stock", "Audit loose stones and opals.", "Shows stone codes, types, statuses, weights, dimensions, brightness, colour notes, values and parcel links.", "Use it when selecting stones for new designs or checking what can be listed or set.", "Keep weights, values and statuses up to date as stones are cut, reserved, set or sold.", "The report cannot judge stone quality; it reflects recorded information."),
        ["Reports Studio|Export BI CSV"] = new("Export BI CSV", "Export report data to spreadsheets.", "Creates spreadsheet-ready CSV files for sales, balances, quotes, inventory value and reserved inventory.", "Run it when you want deeper spreadsheet analysis or a copy for bookkeeping review.", "Keep exported files private because they contain business and customer-linked data.", "CSV exports are snapshots. Re-export after major data changes."),
        ["Reports Studio|Export BI Excel"] = new("Export BI Excel", "Export one workbook.", "Creates an Excel-compatible workbook with summary, sales, balances, quotes, inventory value, reservations, tasks and external diamond sheets.", "Run it when you want one spreadsheet file for business review or bookkeeping discussion.", "Keep exported files private because they contain business and customer-linked data.", "Excel exports are snapshots. Re-export after major data changes."),
        ["Safety & Data Studio|Create Backup"] = new("Create Backup", "Protect your business data.", "Creates a safe copy of the local SQLite database.", "Click Create Backup and save it in your backup location. Use Ctrl+B as a shortcut where available.", "Back up before imports, restores, bulk changes and every important work session.", "A backup on the same computer does not protect you from device loss or drive failure."),
        ["Safety & Data Studio|Restore Backup"] = new("Restore Backup", "Recover from a backup.", "Stages and validates a restore file before replacing active data.", "Choose the backup or export bundle, read prompts carefully, and restart the app if instructed.", "Only restore when you know the backup is the correct version.", "Restore can overwrite current work. Create a backup of the current state first."),
        ["Safety & Data Studio|Release Notes"] = new("Release Notes", "Review current changes.", "Creates a local HTML release-notes page covering the current workflow builds.", "Open it after upgrades or before handoff testing so the current build scope is clear.", "Use it with the version-specific testing checklists.", "Release notes summarize the app build; they are not a backup or data export."),
    };

    private void RefreshCurrentSection()
    {
        _lastRefreshStarted = DateTime.Now;
        Mouse.OverrideCursor = Cursors.Wait;
        try
        {
            if (CurrentSection == "Project Workbench")
                ProjectWorkbench_Click(this, new RoutedEventArgs());
            else if (CurrentSection == "Dashboard")
                LoadDashboard();
            else if (_toolSections.Contains(CurrentSection))
                LoadToolWorkspace(CurrentSection);
            else
                LoadRecords(CurrentSection);
        }
        finally
        {
            Mouse.OverrideCursor = null;
        }
    }

    private void LoadDashboard()
    {
        CurrentPageTitleText.Text = "Dashboard";
        CurrentPageHintText.Text = "Dashboard Command Centre for quotes, overdue work, handover, balances, stock alerts and one-click daily actions.";
        DashboardPanel.Visibility = Visibility.Visible;
        RecordWorkspacePanel.Visibility = Visibility.Collapsed;
        ToolWorkspacePanel.Visibility = Visibility.Collapsed;
        ReportPanel.Visibility = Visibility.Collapsed;
        using var db = new AppDbContext();
        var today = DateTime.Today;
        var dueCutoff = today.AddDays(7);
        var weekStart = today.AddDays(-(((int)today.DayOfWeek + 6) % 7));
        var weekEnd = weekStart.AddDays(7);
        var activeJobsQuery = db.Jobs.AsNoTracking().Where(j => j.Status != JobStatus.Completed && j.Status != JobStatus.Cancelled);
        var activeJobsList = activeJobsQuery.AsEnumerable().ToList();
        var monthSales = db.Sales.AsNoTracking().AsEnumerable().Where(s => s.SaleDate.Month == today.Month && s.SaleDate.Year == today.Year).ToList();
        var weekSales = db.Sales.AsNoTracking().AsEnumerable().Where(s => s.SaleDate.Date >= weekStart && s.SaleDate.Date < weekEnd).ToList();
        var nextActions = NextActionService.BuildActions(db, today);

        NextActionsText.Text = nextActions.Count(x => x.IsActionNeeded).ToString();
        QuotesAwaitingText.Text = db.CustomQuotes.AsNoTracking().AsEnumerable()
            .Count(q => !string.Equals(q.Status, "Accepted", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(q.Status, "Converted", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(q.Status, "Cancelled", StringComparison.OrdinalIgnoreCase)
                && q.AcceptedOptionId == null)
            .ToString();
        OverdueJobsText.Text = activeJobsList.Count(j => j.DueDate.HasValue && j.DueDate.Value.Date < today).ToString();
        JobsDueThisWeekText.Text = activeJobsList.Count(j => j.DueDate.HasValue && j.DueDate.Value.Date >= today && j.DueDate.Value.Date <= dueCutoff).ToString();
        ReadyCollectionText.Text = activeJobsList.Count(j => j.Status == JobStatus.ReadyForPickup).ToString();
        ReadyShipText.Text = activeJobsList.Count(j => j.Status == JobStatus.ReadyToShip).ToString();
        UnpaidBalancesText.Text = activeJobsList.Count(j => j.BalanceOwing > 0).ToString();
        var reservedStoneLinks = db.QuoteOptionStoneLinks.AsNoTracking().Count(x => x.ReservationStatus == "Reserved");
        var reservedMaterialLinks = db.QuoteOptionMaterialLinks.AsNoTracking().Count(x => x.ReservationStatus == "Reserved");
        ReservedInventoryText.Text = (reservedStoneLinks + reservedMaterialLinks).ToString();
        var externalDiamondRows = db.ExternalDiamonds.AsNoTracking().AsEnumerable().ToList();
        var externalLinks = db.QuoteOptionExternalDiamondLinks.AsNoTracking().AsEnumerable().ToList();
        ExternalHoldsExpiringText.Text = externalDiamondRows.Count(d => (d.Status == "Hold Requested" || d.Status == "Hold Confirmed" || d.Status == "Hold Expiring") && d.HoldExpiresAt.HasValue && d.HoldExpiresAt.Value <= DateTime.Now.AddHours(24)).ToString();
        ExternalDiamondsOrderedText.Text = externalDiamondRows.Count(d => (d.Status == "Order Requested" || d.Status == "Ordered") && !d.ReceivedAt.HasValue).ToString();
        ExternalApprovedNotOrderedText.Text = externalDiamondRows.Count(d => d.Status is "Customer Interested" or "Hold Requested" or "Hold Confirmed" or "Hold Expiring" or "Expired").ToString();
        ExternalDiamondQuoteLinksText.Text = externalLinks.Count.ToString();
        LowStockCommandText.Text = db.Materials.Count(m => m.CurrentQuantity <= m.ReorderLevel).ToString();
        FollowUpsText.Text = db.BusinessTasks.AsNoTracking().AsEnumerable().Count(t => t.IsOpen && t.Category == BusinessTaskCategory.CustomerFollowUp).ToString();
        RecentPaymentsText.Text = db.Payments.AsNoTracking().AsEnumerable().Count(p => p.PaymentDate.Date >= today.AddDays(-7)).ToString();
        SalesThisWeekText.Text = weekSales.Sum(s => s.SaleAmount).ToString("C");
        SalesThisMonthText.Text = monthSales.Sum(s => s.SaleAmount).ToString("C");

        ActiveJobsText.Text = activeJobsList.Count.ToString();
        StockText.Text = db.JewelleryItems.Count(j => j.Status != StockStatus.Sold).ToString();
        LowMaterialsText.Text = db.Materials.Count(m => m.CurrentQuantity <= m.ReorderLevel).ToString();
        LooseStonesText.Text = db.Stones.Count(s => s.Status == StoneStatus.Loose).ToString();
        // SQLite has limited native decimal aggregation support through EF Core.
        // Materialising before summing keeps the dashboard reliable for currency fields.
        BalanceText.Text = db.Jobs.AsNoTracking().AsEnumerable().Sum(j => j.BalanceOwing).ToString("C");
        var unsoldItems = db.JewelleryItems.AsNoTracking()
            .Where(j => j.Status != StockStatus.Sold)
            .AsEnumerable()
            .ToList();
        StockValueText.Text = unsoldItems.Sum(j => j.RetailPrice).ToString("C");
        StockCostText.Text = unsoldItems.Sum(PricingService.CalculateJewelleryCost).ToString("C");
        MonthlyProfitText.Text = monthSales.Sum(s => s.Profit).ToString("C");
        var settings = BusinessSettingsService.Load();
        GoldPerGramText.Text = settings.GoldPricePerGram > 0 ? $"{settings.GoldPricePerGram:C}" : "Not set";
        MetalPriceUpdatedText.Text = settings.MetalPricesLastUpdated.HasValue
            ? $"{settings.MetalPriceCurrency} • {settings.MetalPricesLastUpdated.Value:g}"
            : "Open Metal Prices to set rates";
        ReservedStockText.Text = db.JewelleryItems.Count(j => j.Status == StockStatus.Reserved).ToString();
        AtMarketText.Text = db.JewelleryItems.Count(j => j.Status == StockStatus.AtMarket).ToString();
        JobsDueSoonText.Text = activeJobsList.Count(j => j.DueDate.HasValue && j.DueDate.Value.Date <= dueCutoff).ToString();
        RecentMovementsText.Text = db.MaterialTransactions.Count(t => t.TransactionDate >= DateTime.Today.AddDays(-30)).ToString();
        var activeBatches = db.ProductionBatches.AsNoTracking()
            .AsEnumerable()
            .Where(b => b.Status != ProductionBatchStatus.Completed && b.Status != ProductionBatchStatus.Cancelled)
            .ToList();
        ActiveBatchesText.Text = activeBatches.Count.ToString();
        BatchRetailValueText.Text = activeBatches.Sum(b => b.EstimatedRetailValue).ToString("C");
        BatchProgressText.Text = activeBatches.Count == 0 ? "0%" : activeBatches.Average(b => b.ProgressPercent).ToString("P0");
        var parcels = db.OpalParcels.AsNoTracking().AsEnumerable().ToList();
        var totalRoughCarats = parcels.Sum(p => p.StartingWeightCarats);
        var totalFinishedCarats = db.Stones.AsNoTracking().AsEnumerable().Where(s => s.OpalParcelId.HasValue).Sum(s => s.WeightCarats);
        OpalYieldText.Text = totalRoughCarats > 0 ? (totalFinishedCarats / totalRoughCarats).ToString("P0") : "0%";
        var totalParcelCost = parcels.Sum(p => p.TotalCost);
        var totalLinkedStoneValue = db.Stones.AsNoTracking().AsEnumerable().Where(s => s.OpalParcelId.HasValue).Sum(s => s.EstimatedValue);
        ParcelProfitText.Text = (totalLinkedStoneValue - totalParcelCost).ToString("C");
        var onlineListings = db.OnlineListings.AsNoTracking().AsEnumerable().ToList();
        var unsoldStockIds = db.JewelleryItems.AsNoTracking().AsEnumerable()
            .Where(i => i.Status != StockStatus.Sold)
            .Select(i => i.Id)
            .ToHashSet();
        NeedsListingText.Text = unsoldStockIds.Count(id => onlineListings.All(l => l.JewelleryItemId != id)).ToString();
        ReadyToListText.Text = onlineListings.Count(l => l.Status == OnlineListingStatus.ReadyToList).ToString();
        ListedOnlineText.Text = onlineListings.Count(l => l.Status == OnlineListingStatus.Listed || l.ListedOnline).ToString();
        UpcomingMarketsText.Text = db.MarketEvents.Count(m => m.EventDate.Date >= DateTime.Today).ToString();
        var marketStockRecords = db.MarketStocks.AsNoTracking().AsEnumerable().ToList();
        MarketStockCountText.Text = marketStockRecords.Count.ToString();
        MarketPackedText.Text = marketStockRecords.Count(m => m.Packed).ToString();
        var marketNetEstimate = db.MarketEvents.AsNoTracking().AsEnumerable().Sum(m => m.NetMarketProfit);
        MarketNetText.Text = marketNetEstimate.ToString("C");
        var openPurchaseOrders = db.PurchaseOrders.AsNoTracking().AsEnumerable()
            .Where(p => p.Status is PurchaseOrderStatus.Draft or PurchaseOrderStatus.Ordered or PurchaseOrderStatus.PartiallyReceived)
            .ToList();
        OpenPurchaseOrdersText.Text = openPurchaseOrders.Count.ToString();
        PurchaseOrdersDueText.Text = openPurchaseOrders.Count(p => p.ExpectedDeliveryDate.HasValue && p.ExpectedDeliveryDate.Value.Date <= DateTime.Today.AddDays(14)).ToString();
        ReorderSuggestionsText.Text = db.Materials.AsNoTracking().AsEnumerable().Count(m => m.CurrentQuantity <= m.ReorderLevel).ToString();
        SupplierOrderValueText.Text = openPurchaseOrders.Sum(p => p.TotalCost).ToString("C");
        var businessTasks = db.BusinessTasks.AsNoTracking().AsEnumerable().ToList();
        var openTasks = businessTasks.Where(t => t.IsOpen && t.ShowOnDashboard).ToList();
        OpenTasksText.Text = openTasks.Count.ToString();
        TasksDueTodayText.Text = openTasks.Count(t => t.DueDate.HasValue && t.DueDate.Value.Date == DateTime.Today).ToString();
        OverdueTasksText.Text = openTasks.Count(t => t.IsOverdue).ToString();
        HighPriorityTasksText.Text = openTasks.Count(t => t.Priority == BusinessTaskPriority.High || t.Priority == BusinessTaskPriority.Urgent).ToString();
        RefreshSetupReadiness(db, settings, today);
        RefreshDashboardDataSafety(settings, today);
        StatusText.Text = $"Database: {DatabaseBootstrapper.DatabasePath}";
    }

    private void RefreshSetupReadiness(AppDbContext db, BusinessSettings settings, DateTime today)
    {
        var rows = new List<SetupReadinessRow>();
        var defaultSettings = BusinessSettingsService.CreateDefaultSettings();
        var hasBusinessProfile = !string.IsNullOrWhiteSpace(settings.BusinessName)
            && !string.Equals(settings.BusinessName, defaultSettings.BusinessName, StringComparison.OrdinalIgnoreCase)
            && (!string.IsNullOrWhiteSpace(settings.Email) || !string.IsNullOrWhiteSpace(settings.Phone));
        rows.Add(CreateSetupRow(
            hasBusinessProfile,
            "Business profile",
            hasBusinessProfile ? $"{settings.BusinessName} has contact details for documents." : "Add business name plus phone or email for customer paperwork.",
            "settings"));

        var hasPricingDefaults = settings.DefaultLabourRate > 0m
            && settings.DefaultProfitMarginPercent > 0m
            && settings.GstRatePercent >= 0m
            && !string.IsNullOrWhiteSpace(settings.TaxLabel);
        rows.Add(CreateSetupRow(
            hasPricingDefaults,
            "Labour, margin and tax defaults",
            hasPricingDefaults ? $"Labour {settings.DefaultLabourRate:C}/hr, margin {settings.DefaultProfitMarginPercent:0.##}%, {settings.TaxLabel} {settings.GstRatePercent:0.##}%." : "Set labour rate, default margin and tax label before serious quoting.",
            "settings"));

        var hasMetalPricing = settings.GoldPricePerGram > 0m
            || settings.SilverPricePerGram > 0m
            || settings.PlatinumPricePerGram > 0m
            || settings.PalladiumPricePerGram > 0m;
        rows.Add(CreateSetupRow(
            hasMetalPricing,
            "Metal price assumptions",
            hasMetalPricing ? $"Metal prices are set. Last update: {settings.MetalPricesLastUpdated?.ToString("g") ?? "manual"}." : "Enter current metal assumptions so pricing tools are not quoting from blanks.",
            "metal"));

        var hasProposalTemplates = !string.IsNullOrWhiteSpace(settings.TermsAndConditions)
            && !string.IsNullOrWhiteSpace(settings.DocumentFooterText)
            && !string.IsNullOrWhiteSpace(settings.ProposalEmailSubjectTemplate)
            && !string.IsNullOrWhiteSpace(settings.ProposalEmailMessageTemplate);
        rows.Add(CreateSetupRow(
            hasProposalTemplates,
            "Proposal templates",
            hasProposalTemplates ? "Terms, footer and proposal email draft templates are ready." : "Review proposal terms, footer and email templates before sending customer proposals.",
            "settings"));

        var customerCount = db.Customers.AsNoTracking().Count();
        rows.Add(CreateSetupRow(
            customerCount > 0,
            "Client list",
            customerCount > 0 ? $"{customerCount} customer record(s) available." : "Add or import at least one customer so quote workflows can start cleanly.",
            "customers"));

        var quoteCount = db.CustomQuotes.AsNoTracking().Count();
        rows.Add(CreateSetupRow(
            quoteCount > 0,
            "First quote",
            quoteCount > 0 ? $"{quoteCount} custom quote(s) created." : "Create the first custom quote with at least one option.",
            "quote"));

        var proposalSentCount = db.CustomQuotes.AsNoTracking().AsEnumerable().Count(q =>
            q.ProposalSentAt.HasValue
            || q.ProposalStatus is "Sent" or "Accepted" or "Converted to Job"
            || q.Status is "Proposal Sent" or "Accepted" or "Converted to Job");
        rows.Add(CreateSetupRow(
            proposalSentCount > 0,
            "First proposal sent",
            proposalSentCount > 0 ? $"{proposalSentCount} proposal(s) have been recorded as sent or accepted." : "Use Send / Record Proposal from the quote workspace to complete the first proposal milestone.",
            "quote"));

        var jobCount = db.Jobs.AsNoTracking().Count();
        rows.Add(CreateSetupRow(
            jobCount > 0,
            "Production workflow",
            jobCount > 0 ? $"{jobCount} job record(s) exist." : "Convert an accepted quote option into a production job when ready.",
            "production"));

        var latestBackup = GetLatestBackup(settings);
        var backupIsFresh = latestBackup != null && latestBackup.LastWriteTime.Date >= today.AddDays(-7);
        rows.Add(CreateSetupRow(
            backupIsFresh,
            "Backup health",
            latestBackup == null ? "Create a backup before more data entry." : backupIsFresh ? $"Latest backup: {latestBackup.LastWriteTime:g}." : $"Latest backup is older than 7 days: {latestBackup.LastWriteTime:g}.",
            "backup"));

        var hasNivodaCredentials = !string.IsNullOrWhiteSpace(settings.NivodaUsername)
            && !string.IsNullOrWhiteSpace(settings.NivodaPassword);
        rows.Add(new SetupReadinessRow(
            hasNivodaCredentials ? "Ready" : "Optional",
            "Supplier diamond connection",
            hasNivodaCredentials ? $"Nivoda credentials are entered. Last test: {settings.NivodaLastConnectionTestAt?.ToString("g") ?? "not tested"}." : "Enter supplier credentials only if external diamond quoting is part of your workflow.",
            "supplier",
            hasNivodaCredentials,
            CountsTowardProgress: false));

        var requiredRows = rows.Where(r => r.CountsTowardProgress).ToList();
        var completedRequired = requiredRows.Count(r => r.IsComplete);
        var percent = requiredRows.Count == 0 ? 100 : completedRequired * 100.0 / requiredRows.Count;
        SetupReadinessProgress.Value = percent;
        SetupReadinessSummaryText.Text = $"{completedRequired} of {requiredRows.Count} setup milestones complete ({percent:0}%).";
        SetupReadinessItems.ItemsSource = rows;

        var next = requiredRows.FirstOrDefault(r => !r.IsComplete) ?? rows.FirstOrDefault(r => !r.IsComplete);
        if (next == null)
        {
            _dashboardSetupTarget = "project";
            SetupNextActionText.Text = "Setup is ready. Keep using the dashboard for daily quote, production, payment and stock actions.";
            SetupNextActionButton.Content = "Open Project Workbench";
        }
        else
        {
            _dashboardSetupTarget = next.TargetKey;
            SetupNextActionText.Text = next.Detail;
            SetupNextActionButton.Content = $"Open {next.Title}";
        }
    }

    private static SetupReadinessRow CreateSetupRow(bool complete, string title, string detail, string targetKey)
    {
        return new SetupReadinessRow(complete ? "Complete" : "Open", title, detail, targetKey, complete);
    }

    private void RefreshDashboardDataSafety(BusinessSettings settings, DateTime today)
    {
        var latestBackup = GetLatestBackup(settings);
        var backupFolder = string.IsNullOrWhiteSpace(settings.BackupFolder)
            ? BusinessSettingsService.GetBackupFolder()
            : settings.BackupFolder;
        var backupIsFresh = latestBackup != null && latestBackup.LastWriteTime.Date >= today.AddDays(-7);

        BackupHealthStatusText.Text = latestBackup == null
            ? "Backup needed"
            : backupIsFresh ? "Backup current" : "Backup older than 7 days";
        BackupHealthStatusText.Foreground = (System.Windows.Media.Brush)FindResource(backupIsFresh ? "AccentBrush" : "WarningBrush");
        BackupHealthDetailText.Text = latestBackup == null
            ? "No OPALNOVA backup was found in the configured backup folder."
            : $"Latest backup: {latestBackup.LastWriteTime:g} ({FormatFileSize(latestBackup.Length)}).";
        BackupFolderText.Text = $"Backup folder: {backupFolder}";

        if (File.Exists(DatabaseBootstrapper.DatabasePath))
        {
            var databaseFile = new FileInfo(DatabaseBootstrapper.DatabasePath);
            DatabaseHealthText.Text = $"Active database: {FormatFileSize(databaseFile.Length)} at {DatabaseBootstrapper.DatabasePath}";
        }
        else
        {
            DatabaseHealthText.Text = $"Active database file not found yet: {DatabaseBootstrapper.DatabasePath}";
        }

        PendingRestoreText.Text = File.Exists(DatabaseBootstrapper.PendingRestorePath)
            ? $"Restore staged for next startup: {DatabaseBootstrapper.PendingRestorePath}"
            : "No restore is currently staged.";
    }

    private static string FormatFileSize(long bytes)
    {
        if (bytes >= 1024L * 1024L * 1024L)
            return $"{bytes / (1024d * 1024d * 1024d):0.##} GB";
        if (bytes >= 1024L * 1024L)
            return $"{bytes / (1024d * 1024d):0.##} MB";
        if (bytes >= 1024L)
            return $"{bytes / 1024d:0.##} KB";
        return $"{bytes:N0} bytes";
    }

    private static FileInfo? GetLatestBackup(BusinessSettings settings)
    {
        var folder = string.IsNullOrWhiteSpace(settings.BackupFolder)
            ? BusinessSettingsService.GetBackupFolder()
            : settings.BackupFolder;
        if (!Directory.Exists(folder))
            return null;

        return Directory.EnumerateFiles(folder, "jbm-backup-*.db")
            .Select(path => new FileInfo(path))
            .Where(file => file.Exists)
            .OrderByDescending(file => file.LastWriteTime)
            .FirstOrDefault();
    }

    private void LoadRecords(string section)
    {
        if (!_sectionTypes.TryGetValue(section, out var type)) return;
        CurrentPageTitleText.Text = section;
        CurrentPageHintText.Text = GetSectionHint(section);
        DashboardPanel.Visibility = Visibility.Collapsed;
        RecordWorkspacePanel.Visibility = Visibility.Visible;
        ToolWorkspacePanel.Visibility = Visibility.Collapsed;
        ReportPanel.Visibility = Visibility.Collapsed;

        using var db = new AppDbContext();
        var setMethod = typeof(DbContext).GetMethods()
            .Single(m => m.Name == nameof(DbContext.Set) && m.IsGenericMethodDefinition && m.GetParameters().Length == 0);
        var query = (IQueryable)setMethod.MakeGenericMethod(type).Invoke(db, null)!;
        var noTrackingQuery = query;
        if (typeof(BaseEntity).IsAssignableFrom(type))
        {
            var asNoTrackingMethod = typeof(EntityFrameworkQueryableExtensions).GetMethods()
                .Single(m => m.Name == nameof(EntityFrameworkQueryableExtensions.AsNoTracking) && m.IsGenericMethodDefinition && m.GetParameters().Length == 1);
            noTrackingQuery = (IQueryable)asNoTrackingMethod.MakeGenericMethod(type).Invoke(null, new object[] { query })!;
        }
        var list = noTrackingQuery.Cast<object>().ToList();
        var search = SearchBox.Text.Trim();
        if (!string.IsNullOrWhiteSpace(search))
        {
            list = list.Where(x => x.ToString()!.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                x.GetType().GetProperties().Any(p => (p.GetValue(x)?.ToString() ?? string.Empty).Contains(search, StringComparison.OrdinalIgnoreCase))).ToList();
        }
        list = ApplyQuickFilter(section, list, _activeFilterPreset);
        _currentRecords = list;
        RecordsGrid.SelectedItem = null;
        RecordsGrid.ItemsSource = list;
        RecordEmptyStatePanel.Visibility = list.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        RecordEmptyStateText.Text = string.IsNullOrWhiteSpace(search)
            ? $"No {section.ToLowerInvariant()} records yet. Use Add to create the first one."
            : $"No {section.ToLowerInvariant()} records match '{search}'. Clear or change the search box.";
        UpdateRecordPreview(RecordsGrid.SelectedItem);
        var elapsed = DateTime.Now - _lastRefreshStarted;
        StatusText.Text = $"{section}: {list.Count} record(s) • Filter: {_activeFilterPreset} • Loaded in {elapsed.TotalMilliseconds:0} ms • Ctrl+N add • Ctrl+E edit • Delete remove";
    }




    public void OpenSearchResult(string section, int recordId)
    {
        if (string.IsNullOrWhiteSpace(section) || recordId <= 0) return;
        SetTopFilter("All Records");
        SearchBox.Text = string.Empty;
        SelectNavigationSection(section);
        Dispatcher.BeginInvoke(new Action(() => SelectRecordById(recordId)));
    }

    public void ApplySavedView(string section, string searchText, string filterPreset)
    {
        if (string.IsNullOrWhiteSpace(section)) return;
        SetTopFilter(string.IsNullOrWhiteSpace(filterPreset) ? "All Records" : filterPreset);
        SearchBox.Text = searchText ?? string.Empty;
        SelectNavigationSection(section, preserveSearchAndFilter: true);
        RefreshCurrentSection();
        StatusText.Text = $"Applied saved view: {section} • {_activeFilterPreset}";
    }

    private void SelectRecordById(int recordId)
    {
        if (_currentRecords == null) return;
        foreach (var item in _currentRecords)
        {
            var id = GetRecordId(item!);
            if (id == recordId)
            {
                RecordsGrid.SelectedItem = item;
                RecordsGrid.ScrollIntoView(item);
                UpdateRecordPreview(item);
                return;
            }
        }
    }

    private static List<object> ApplyQuickFilter(string section, List<object> list, string preset)
    {
        if (string.IsNullOrWhiteSpace(preset) || preset == "All Records") return list;
        var today = DateTime.Today;
        return preset switch
        {
            "Low Stock" => section == "Materials" ? list.Where(x => x is Material m && m.CurrentQuantity <= m.ReorderLevel).ToList() : list,
            "Jobs Due Soon" => section == "Jobs" ? list.Where(x => x is Job j && j.Status != JobStatus.Completed && j.Status != JobStatus.Cancelled && j.DueDate.HasValue && j.DueDate.Value.Date <= today.AddDays(14)).ToList() : list,
            "Overdue Jobs" => section == "Jobs" ? list.Where(x => x is Job j && j.Status != JobStatus.Completed && j.Status != JobStatus.Cancelled && j.DueDate.HasValue && j.DueDate.Value.Date < today).ToList() : list,
            "Needs Photos" => section == "Jewellery Stock" ? list.Where(x => x is JewelleryItem j && j.Status == StockStatus.NeedsPhotos).ToList() : list,
            "At Market" => section == "Jewellery Stock" ? list.Where(x => x is JewelleryItem j && j.Status == StockStatus.AtMarket).ToList() : list,
            "Reserved Stock" => section == "Jewellery Stock" ? list.Where(x => x is JewelleryItem j && j.Status == StockStatus.Reserved).ToList() : list,
            "Ready To List" => section == "Online Listings" ? list.Where(x => x is OnlineListing l && (l.Status == OnlineListingStatus.ReadyToList || (l.PhotosDone && l.DescriptionDone && l.PriceChecked && !l.ListedOnline))).ToList() : list,
            "Needs Listing Work" => section == "Online Listings" ? list.Where(x => x is OnlineListing l && (!l.PhotosDone || !l.DescriptionDone || !l.PriceChecked || !l.ListedOnline)).ToList() : list,
            "Overdue Tasks" => section == "Tasks" ? list.Where(x => x is BusinessTask t && t.IsOverdue).ToList() : list,
            "Due Today" => section == "Tasks" ? list.Where(x => x is BusinessTask t && t.IsOpen && t.DueDate.HasValue && t.DueDate.Value.Date == today).ToList() : list,
            "High Priority" => section == "Tasks" ? list.Where(x => x is BusinessTask t && t.IsOpen && (t.Priority == BusinessTaskPriority.High || t.Priority == BusinessTaskPriority.Urgent)).ToList() : list,
            "Open Purchase Orders" => section == "Purchase Orders" ? list.Where(x => x is PurchaseOrder p && p.Status is PurchaseOrderStatus.Draft or PurchaseOrderStatus.Ordered or PurchaseOrderStatus.PartiallyReceived).ToList() : list,
            "Open Jobs" => section == "Jobs" ? list.Where(x => x is Job j && j.Status != JobStatus.Completed && j.Status != JobStatus.Cancelled).ToList() : list,
            _ => list
        };
    }

    private void OpenAdvancedSearch(string initialSection = "All Sections", string initialPreset = "All Records", string initialKeyword = "")
    {
        var window = new AdvancedSearchWindow(initialSection, initialPreset, initialKeyword) { Owner = this };
        window.ShowDialog();
    }

    private void GlobalSearch_Click(object sender, RoutedEventArgs e) => OpenAdvancedSearch();
    private void SavedViews_Click(object sender, RoutedEventArgs e) => OpenAdvancedSearch(CurrentSection, _activeFilterPreset);

    private void LowStockFilter_Click(object sender, RoutedEventArgs e) => ApplySavedView("Materials", string.Empty, "Low Stock");
    private void JobsDueFilter_Click(object sender, RoutedEventArgs e) => ApplySavedView("Jobs", string.Empty, "Jobs Due Soon");
    private void NeedsPhotosFilter_Click(object sender, RoutedEventArgs e) => ApplySavedView("Jewellery Stock", string.Empty, "Needs Photos");
    private void ReadyToListFilter_Click(object sender, RoutedEventArgs e) => ApplySavedView("Online Listings", string.Empty, "Ready To List");
    private void AtMarketFilter_Click(object sender, RoutedEventArgs e) => ApplySavedView("Jewellery Stock", string.Empty, "At Market");
    private void OverdueTasksFilter_Click(object sender, RoutedEventArgs e) => ApplySavedView("Tasks", string.Empty, "Overdue Tasks");

    private void UpdateRecordPreview(object? record)
    {
        if (record == null)
        {
            DetailTypeText.Text = "Record Preview";
            DetailTitleText.Text = "Select a record";
            DetailStatusText.Text = "Click any row to see photos, key fields, linked records and quick actions.";
            DetailSummaryPanel.Children.Clear();
            DetailFieldsStack.Children.Clear();
            DetailLinksStack.Children.Clear();
            DetailActivityStack.Children.Clear();
            DetailPhotoImage.Source = null;
            DetailPhotoImage.Visibility = Visibility.Collapsed;
            DetailPhotoEmptyPanel.Visibility = Visibility.Visible;
            AddPreviewLine(DetailFieldsStack, "Ready", "Select a row from the table on the left.");
            return;
        }

        var type = record.GetType();
        var id = GetRecordId(record);
        DetailTypeText.Text = GetFriendlyTypeName(type);
        DetailTitleText.Text = GetPreviewTitle(record);
        DetailStatusText.Text = BuildPreviewSubtitle(record);
        DetailSummaryPanel.Children.Clear();
        DetailFieldsStack.Children.Clear();
        DetailLinksStack.Children.Clear();
        DetailActivityStack.Children.Clear();
        LoadPreviewPhoto(type.Name, id);

        foreach (var summary in GetPreviewSummary(record).Take(4))
            AddSummaryChip(summary.Label, summary.Value, summary.Hint);

        foreach (var entry in GetPreviewFields(record).Take(8))
            AddPreviewLine(DetailFieldsStack, entry.Label, entry.Value);

        using var db = new AppDbContext();
        foreach (var entry in GetLinkedPreviewLines(db, record).Take(10))
            AddPreviewLine(DetailLinksStack, entry.Label, entry.Value);

        foreach (var entry in GetRecentPreviewLines(db, record).Take(8))
            AddPreviewLine(DetailActivityStack, entry.Label, entry.Value);

        if (DetailLinksStack.Children.Count == 0)
            AddPreviewLine(DetailLinksStack, "Links", "No linked records found yet.");
        if (DetailActivityStack.Children.Count == 0)
            AddPreviewLine(DetailActivityStack, "Activity", "No recent activity found yet.");
    }

    private static int GetRecordId(object record)
    {
        var property = record.GetType().GetProperty("Id");
        return property?.GetValue(record) is int id ? id : 0;
    }

    private static string GetFriendlyTypeName(Type type) => type.Name switch
    {
        nameof(JewelleryItem) => "Jewellery Stock Item",
        nameof(OpalParcel) => "Opal Parcel",
        nameof(MaterialTransaction) => "Material Movement",
        nameof(MarketEvent) => "Market Event",
        nameof(MarketStock) => "Market Stock",
        nameof(ProductionBatch) => "Production Batch",
        nameof(ProductionBatchItem) => "Batch Item",
        nameof(OnlineListing) => "Online Listing",
        nameof(PurchaseOrder) => "Purchase Order",
        nameof(PurchaseOrderItem) => "Purchase Order Item",
        nameof(BusinessTask) => "Task / Reminder",
        nameof(PhotoRecord) => "Photo Record",
        _ => type.Name
    };

    private static string GetPreviewTitle(object record) => record switch
    {
        Customer c => c.FullName,
        Supplier s => s.Name,
        Material m => string.IsNullOrWhiteSpace(m.MaterialCode) ? m.Name : $"{m.MaterialCode} — {m.Name}",
        Stone s => string.IsNullOrWhiteSpace(s.StoneCode) ? $"{s.StoneType} stone #{s.Id}" : s.StoneCode,
        OpalParcel p => string.IsNullOrWhiteSpace(p.ParcelCode) ? $"Opal parcel #{p.Id}" : p.ParcelCode,
        JewelleryItem j => string.IsNullOrWhiteSpace(j.StockCode) ? j.Name : $"{j.StockCode} — {j.Name}",
        Job j => string.IsNullOrWhiteSpace(j.JobCode) ? j.JobTitle : $"{j.JobCode} — {j.JobTitle}",
        Sale s => $"Sale #{s.Id} — {s.SaleAmount:C}",
        Payment p => $"Payment #{p.Id} — {p.Amount:C}",
        MarketEvent m => m.Name,
        MarketStock m => $"Market stock #{m.Id}",
        ProductionBatch b => string.IsNullOrWhiteSpace(b.BatchCode) ? b.Name : $"{b.BatchCode} — {b.Name}",
        ProductionBatchItem i => i.ItemName,
        OnlineListing l => string.IsNullOrWhiteSpace(l.SeoTitle) ? $"Listing #{l.Id}" : l.SeoTitle,
        PurchaseOrder p => string.IsNullOrWhiteSpace(p.PurchaseOrderCode) ? $"Purchase order #{p.Id}" : p.PurchaseOrderCode,
        PurchaseOrderItem i => i.ItemName,
        BusinessTask t => string.IsNullOrWhiteSpace(t.TaskCode) ? t.Title : $"{t.TaskCode} — {t.Title}",
        PhotoRecord p => string.IsNullOrWhiteSpace(p.Caption) ? $"Photo #{p.Id}" : p.Caption,
        _ => record.ToString() ?? record.GetType().Name
    };

    private static string BuildPreviewSubtitle(object record) => record switch
    {
        Customer c => $"{c.Email} {c.Phone}".Trim(),
        Material m => $"{m.Category} • {m.CurrentQuantity:N2} {m.UnitType} on hand • Reorder at {m.ReorderLevel:N2}",
        Stone s => $"{s.StoneType} • {s.WeightCarats:N2} ct • {s.Status}",
        OpalParcel p => $"{p.StartingWeightCarats:N2} ct rough • Cost {p.TotalCost:C}",
        JewelleryItem j => $"{j.Type} • {j.Status} • Retail {j.RetailPrice:C}",
        Job j => $"{j.Type} • {j.Status} • Due {(j.DueDate.HasValue ? j.DueDate.Value.ToShortDateString() : "not set")}",
        Sale s => $"{s.SaleDate:d} • {s.PaymentMethod} • Profit {s.Profit:C}",
        MarketEvent m => $"{m.EventDate:d} • {m.Location} • Net {m.NetMarketProfit:C}",
        ProductionBatch b => $"{b.Status} • {b.CompletedPieces}/{b.PlannedPieces} pieces • {b.ProgressPercent:P0}",
        OnlineListing l => $"{l.Platform} • {l.Status} • Photos: {(l.PhotosDone ? "done" : "needed")}",
        PurchaseOrder p => $"{p.Status} • Total {p.TotalCost:C} • Expected {(p.ExpectedDeliveryDate.HasValue ? p.ExpectedDeliveryDate.Value.ToShortDateString() : "not set")}",
        BusinessTask t => $"{t.Status} • {t.Priority} priority • Due {(t.DueDate.HasValue ? t.DueDate.Value.ToShortDateString() : "not set")}",
        _ => "Preview, linked records and recent activity"
    };

    private void LoadPreviewPhoto(string entityType, int entityId)
    {
        try
        {
            using var db = new AppDbContext();
            var photo = db.PhotoRecords.AsNoTracking()
                .AsEnumerable()
                .FirstOrDefault(p => p.EntityType == entityType && p.EntityId == entityId && File.Exists(p.FilePath));
            if (photo == null)
            {
                DetailPhotoImage.Source = null;
                DetailPhotoImage.Visibility = Visibility.Collapsed;
                DetailPhotoEmptyPanel.Visibility = Visibility.Visible;
                return;
            }

            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.UriSource = new Uri(photo.FilePath);
            image.EndInit();
            image.Freeze();
            DetailPhotoImage.Source = image;
            DetailPhotoImage.Visibility = Visibility.Visible;
            DetailPhotoEmptyPanel.Visibility = Visibility.Collapsed;
        }
        catch
        {
            DetailPhotoImage.Source = null;
            DetailPhotoImage.Visibility = Visibility.Collapsed;
            DetailPhotoEmptyPanel.Visibility = Visibility.Visible;
        }
    }

    private static IEnumerable<(string Label, string Value)> GetPreviewFields(object record)
    {
        var priorityNames = new[]
        {
            "Status", "CurrentQuantity", "ReorderLevel", "RetailPrice", "TotalCost", "EstimatedProfit", "SaleAmount", "Profit", "QuoteAmount",
            "DepositPaid", "BalanceOwing", "WeightCarats", "EstimatedValue", "BodyTone", "Brightness", "Pattern",
            "DueDate", "EventDate", "StartDate", "TargetCompletionDate", "ExpectedDeliveryDate", "ListingStatus", "Platform",
            "Priority", "Category", "PaymentMethod", "Location", "Notes"
        };

        var properties = record.GetType().GetProperties()
            .Where(p => p.CanRead && p.GetIndexParameters().Length == 0 && p.Name != "Id")
            .OrderBy(p => Array.IndexOf(priorityNames, p.Name) >= 0 ? Array.IndexOf(priorityNames, p.Name) : 100)
            .ThenBy(p => p.Name);

        foreach (var property in properties)
        {
            var value = property.GetValue(record);
            if (value == null) continue;
            var text = FormatPreviewValue(value);
            if (string.IsNullOrWhiteSpace(text) || text == "0" || text == "$0.00" || text == "False") continue;
            yield return (SplitPascalCase(property.Name), text);
        }
    }

    private static string FormatPreviewValue(object value) => value switch
    {
        DateTime d => d == default ? string.Empty : d.ToString("d MMM yyyy"),
        decimal dec => dec.ToString("C"),
        double dbl => dbl.ToString("N2"),
        float flt => flt.ToString("N2"),
        bool b => b ? "Yes" : "False",
        _ => value.ToString() ?? string.Empty
    };

    private static string SplitPascalCase(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return text;
        return System.Text.RegularExpressions.Regex.Replace(text, "(?<!^)([A-Z])", " $1");
    }

    private static IEnumerable<(string Label, string Value)> GetLinkedPreviewLines(AppDbContext db, object record)
    {
        switch (record)
        {
            case Customer c:
                yield return ("Jobs", db.Jobs.Count(j => j.CustomerId == c.Id).ToString());
                yield return ("Sales", db.Sales.Count(s => s.CustomerId == c.Id).ToString());
                break;
            case Material m:
                yield return ("Supplier", m.SupplierId.HasValue ? db.Suppliers.Find(m.SupplierId.Value)?.Name ?? $"Supplier #{m.SupplierId}" : "Not linked");
                yield return ("Movements", db.MaterialTransactions.Count(t => t.MaterialId == m.Id).ToString());
                break;
            case Stone s:
                yield return ("Opal Parcel", s.OpalParcelId.HasValue ? db.OpalParcels.Find(s.OpalParcelId.Value)?.ParcelCode ?? $"Parcel #{s.OpalParcelId}" : "Not linked");
                yield return ("Jewellery Items", db.JewelleryItems.Count(j => j.MainStoneId == s.Id).ToString());
                break;
            case OpalParcel p:
                yield return ("Linked Stones", db.Stones.Count(s => s.OpalParcelId == p.Id).ToString());
                break;
            case JewelleryItem j:
                yield return ("Main Stone", j.MainStoneId.HasValue ? db.Stones.Find(j.MainStoneId.Value)?.StoneCode ?? $"Stone #{j.MainStoneId}" : "Not linked");
                yield return ("Sales", db.Sales.Count(s => s.JewelleryItemId == j.Id).ToString());
                yield return ("Market Records", db.MarketStocks.Count(m => m.JewelleryItemId == j.Id).ToString());
                break;
            case Job j:
                yield return ("Customer", j.CustomerId.HasValue ? db.Customers.Find(j.CustomerId.Value)?.FullName ?? $"Customer #{j.CustomerId}" : "Not linked");
                yield return ("Payments", db.Payments.Count(p => p.JobId == j.Id).ToString());
                yield return ("Sales", db.Sales.Count(s => s.JobId == j.Id).ToString());
                break;
            case Sale s:
                yield return ("Customer", s.CustomerId.HasValue ? db.Customers.Find(s.CustomerId.Value)?.FullName ?? $"Customer #{s.CustomerId}" : "Not linked");
                yield return ("Job", s.JobId.HasValue ? db.Jobs.Find(s.JobId.Value)?.JobCode ?? $"Job #{s.JobId}" : "Not linked");
                yield return ("Jewellery Item", s.JewelleryItemId.HasValue ? db.JewelleryItems.Find(s.JewelleryItemId.Value)?.StockCode ?? $"Item #{s.JewelleryItemId}" : "Not linked");
                break;
            case MarketEvent m:
                yield return ("Market Stock", db.MarketStocks.Count(s => s.MarketEventId == m.Id).ToString());
                yield return ("Recorded Sales", db.Sales.Count(s => s.SaleLocation == SaleLocation.Market).ToString());
                break;
            case ProductionBatch b:
                yield return ("Batch Items", db.ProductionBatchItems.Count(i => i.ProductionBatchId == b.Id).ToString());
                break;
            case OnlineListing l:
                yield return ("Jewellery Item", l.JewelleryItemId > 0 ? db.JewelleryItems.Find(l.JewelleryItemId)?.StockCode ?? $"Item #{l.JewelleryItemId}" : "Not linked");
                break;
            case PurchaseOrder p:
                yield return ("Supplier", p.SupplierId.HasValue ? db.Suppliers.Find(p.SupplierId.Value)?.Name ?? $"Supplier #{p.SupplierId}" : "Not linked");
                yield return ("Line Items", db.PurchaseOrderItems.Count(i => i.PurchaseOrderId == p.Id).ToString());
                break;
            case PurchaseOrderItem i:
                yield return ("Purchase Order", db.PurchaseOrders.Find(i.PurchaseOrderId)?.PurchaseOrderCode ?? $"PO #{i.PurchaseOrderId}");
                yield return ("Material", i.MaterialId.HasValue ? db.Materials.Find(i.MaterialId.Value)?.Name ?? $"Material #{i.MaterialId}" : "Not linked");
                break;
            case BusinessTask t:
                if (t.CustomerId.HasValue) yield return ("Customer", db.Customers.Find(t.CustomerId.Value)?.FullName ?? $"Customer #{t.CustomerId}");
                if (t.JobId.HasValue) yield return ("Job", db.Jobs.Find(t.JobId.Value)?.JobCode ?? $"Job #{t.JobId}");
                if (t.JewelleryItemId.HasValue) yield return ("Jewellery Item", db.JewelleryItems.Find(t.JewelleryItemId.Value)?.StockCode ?? $"Item #{t.JewelleryItemId}");
                break;
        }
    }

    private static IEnumerable<(string Label, string Value)> GetRecentPreviewLines(AppDbContext db, object record)
    {
        var id = GetRecordId(record);
        var typeName = record.GetType().Name;
        var photos = db.PhotoRecords.Count(p => p.EntityType == typeName && p.EntityId == id);
        if (photos > 0) yield return ("Photos", photos.ToString());

        switch (record)
        {
            case Material m:
                foreach (var tx in db.MaterialTransactions.AsNoTracking().AsEnumerable().Where(t => t.MaterialId == m.Id).OrderByDescending(t => t.TransactionDate).Take(3))
                    yield return (tx.TransactionDate.ToString("d MMM"), $"{tx.Reason}: {tx.QuantityChange:N2} {m.UnitType}");
                break;
            case JewelleryItem j:
                foreach (var sale in db.Sales.AsNoTracking().AsEnumerable().Where(s => s.JewelleryItemId == j.Id).OrderByDescending(s => s.SaleDate).Take(3))
                    yield return (sale.SaleDate.ToString("d MMM"), $"Sold for {sale.SaleAmount:C}");
                break;
            case Job j:
                foreach (var payment in db.Payments.AsNoTracking().AsEnumerable().Where(p => p.JobId == j.Id).OrderByDescending(p => p.PaymentDate).Take(3))
                    yield return (payment.PaymentDate.ToString("d MMM"), $"Payment {payment.Amount:C}");
                break;
            case Customer c:
                var recentJob = db.Jobs.AsNoTracking().AsEnumerable().Where(j => j.CustomerId == c.Id).OrderByDescending(j => j.DateReceived).FirstOrDefault();
                if (recentJob != null) yield return ("Recent job", recentJob.JobTitle);
                var recentSale = db.Sales.AsNoTracking().AsEnumerable().Where(s => s.CustomerId == c.Id).OrderByDescending(s => s.SaleDate).FirstOrDefault();
                if (recentSale != null) yield return ("Recent sale", recentSale.SaleAmount.ToString("C"));
                break;
        }
    }

    private void AddSummaryChip(string label, string value, string hint)
    {
        var border = new Border
        {
            Background = (System.Windows.Media.Brush)FindResource("CardBackgroundBrush"),
            BorderBrush = (System.Windows.Media.Brush)FindResource("AccentBrush"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(14),
            Padding = new Thickness(12, 10, 12, 10),
            Margin = new Thickness(0, 0, 8, 8),
            MinWidth = 120,
            MaxWidth = 190
        };

        var stack = new StackPanel();
        stack.Children.Add(new TextBlock
        {
            Text = label.ToUpperInvariant(),
            Foreground = (System.Windows.Media.Brush)FindResource("MutedTextBrush"),
            FontSize = 10,
            FontWeight = FontWeights.Bold,
            TextWrapping = TextWrapping.Wrap
        });
        stack.Children.Add(new TextBlock
        {
            Text = value,
            Foreground = (System.Windows.Media.Brush)FindResource("PrimaryTextBrush"),
            FontSize = 20,
            FontWeight = FontWeights.Bold,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 2, 0, 0)
        });
        if (!string.IsNullOrWhiteSpace(hint))
        {
            stack.Children.Add(new TextBlock
            {
                Text = hint,
                Foreground = (System.Windows.Media.Brush)FindResource("SecondaryTextBrush"),
                FontSize = 11,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 2, 0, 0)
            });
        }
        border.Child = stack;
        DetailSummaryPanel.Children.Add(border);
    }

    private static IEnumerable<(string Label, string Value, string Hint)> GetPreviewSummary(object record)
    {
        switch (record)
        {
            case JewelleryItem j:
                yield return ("Status", j.Status.ToString(), "current stock stage");
                yield return ("Retail", j.RetailPrice.ToString("C"), "selling price");
                yield return ("Profit", j.EstimatedProfit.ToString("C"), "before fees/tax");
                if (!string.IsNullOrWhiteSpace(j.StockCode)) yield return ("Code", j.StockCode, "scan/label code");
                break;
            case Job j:
                yield return ("Status", j.Status.ToString(), "job workflow");
                yield return ("Due", j.DueDate.HasValue ? j.DueDate.Value.ToString("d MMM") : "Not set", "deadline");
                yield return ("Balance", j.BalanceOwing.ToString("C"), "remaining owing");
                yield return ("Quote", j.QuoteAmount.ToString("C"), "quoted amount");
                break;
            case Material m:
                yield return ("On Hand", $"{m.CurrentQuantity:N2}", m.UnitType.ToString());
                yield return ("Reorder", $"{m.ReorderLevel:N2}", "alert level");
                yield return ("Unit Cost", (m.CurrentQuantity > 0 ? (m.PurchaseCost / m.CurrentQuantity) : m.PurchaseCost).ToString("C"), "per unit");
                yield return ("Category", m.Category.ToString(), "material group");
                break;
            case Stone s:
                yield return ("Status", s.Status.ToString(), "stone workflow");
                yield return ("Carats", s.WeightCarats.ToString("N2"), "finished weight");
                yield return ("Value", s.EstimatedValue.ToString("C"), "estimated");
                if (!string.IsNullOrWhiteSpace(s.StoneCode)) yield return ("Code", s.StoneCode, "stone code");
                break;
            case Customer c:
                yield return ("Customer", c.FullName, "contact record");
                if (!string.IsNullOrWhiteSpace(c.Phone)) yield return ("Phone", c.Phone, "primary contact");
                if (!string.IsNullOrWhiteSpace(c.InstagramHandle)) yield return ("Instagram", c.InstagramHandle, "social contact");
                break;
            case Sale s:
                yield return ("Sale", s.SaleAmount.ToString("C"), "amount received");
                yield return ("Profit", s.Profit.ToString("C"), "estimated");
                yield return ("Date", s.SaleDate.ToString("d MMM"), "sold");
                yield return ("Method", s.PaymentMethod.ToString(), "payment");
                break;
            case PurchaseOrder p:
                yield return ("Status", p.Status.ToString(), "supplier workflow");
                yield return ("Total", p.TotalCost.ToString("C"), "order value");
                yield return ("Expected", p.ExpectedDeliveryDate.HasValue ? p.ExpectedDeliveryDate.Value.ToString("d MMM") : "Not set", "delivery");
                break;
            case ProductionBatch b:
                yield return ("Status", b.Status.ToString(), "batch stage");
                yield return ("Planned", b.PlannedPieces.ToString(), "pieces");
                yield return ("Completed", b.CompletedPieces.ToString(), "pieces");
                yield return ("Retail", b.EstimatedRetailValue.ToString("C"), "expected value");
                break;
            case OnlineListing l:
                yield return ("Status", l.Status.ToString(), "online workflow");
                yield return ("Platform", l.Platform.ToString(), "channel");
                yield return ("Photos", l.PhotosDone ? "Done" : "Needed", "listing prep");
                yield return ("Listed", l.ListedOnline ? "Yes" : "No", "published");
                break;
            case BusinessTask t:
                yield return ("Status", t.Status.ToString(), "task stage");
                yield return ("Priority", t.Priority.ToString(), "importance");
                yield return ("Due", t.DueDate.HasValue ? t.DueDate.Value.ToString("d MMM") : "Not set", "deadline");
                yield return ("Category", t.Category.ToString(), "task type");
                break;
            default:
                var id = GetRecordId(record);
                yield return ("Record", id > 0 ? $"#{id}" : "Selected", "current selection");
                break;
        }
    }

    private void AddPreviewLine(StackPanel panel, string label, string value)
    {
        var border = new Border
        {
            Background = (System.Windows.Media.Brush)FindResource("CardAltBackgroundBrush"),
            BorderBrush = (System.Windows.Media.Brush)FindResource("BorderBrushSoft"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(10, 8, 10, 8),
            Margin = new Thickness(0, 0, 0, 7)
        };

        var stack = new StackPanel();
        stack.Children.Add(new TextBlock
        {
            Text = label.ToUpperInvariant(),
            Foreground = (System.Windows.Media.Brush)FindResource("MutedTextBrush"),
            FontSize = 10,
            FontWeight = FontWeights.Bold,
            TextWrapping = TextWrapping.Wrap
        });
        stack.Children.Add(new TextBlock
        {
            Text = value,
            Foreground = (System.Windows.Media.Brush)FindResource("PrimaryTextBrush"),
            FontSize = 14,
            FontWeight = FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 2, 0, 0)
        });
        border.Child = stack;
        panel.Children.Add(border);
    }

    private void ScanLookup_Click(object sender, RoutedEventArgs e)
    {
        var window = new ScanLookupWindow { Owner = this };
        window.ShowDialog();
    }

    private void GenerateSelectedScanLabel_Click(object sender, RoutedEventArgs e)
    {
        var selected = RecordsGrid.SelectedItem;
        if (selected == null)
        {
            MessageBox.Show("Select a jewellery item, stone, job, material, purchase order, batch, task or market stock record first.", "Scan Label", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var label = BarcodeLabelService.FromRecord(selected);
        if (label == null)
        {
            MessageBox.Show("The selected record type does not currently support scan labels.", "Scan Label", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            var path = BarcodeLabelService.GenerateSingleLabel(label);
            OpenReportInApp(path, "Scan Label");
        }
        catch (Exception ex)
        {
            ErrorLogService.Log(ex, "Generate selected scan label");
            MessageBox.Show($"Could not generate the scan label.\n\n{ex.Message}", "Scan Label", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void GenerateScanLabelSheet_Click(object sender, RoutedEventArgs e)
    {
        if (_currentRecords == null || _currentRecords.Count == 0)
        {
            MessageBox.Show("Open a supported section with records before generating a label sheet.", "Scan Label Sheet", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var labels = _currentRecords.Cast<object>()
            .Select(BarcodeLabelService.FromRecord)
            .Where(label => label != null)
            .Cast<ScanLabelItem>()
            .ToList();

        if (labels.Count == 0)
        {
            MessageBox.Show("The active section does not currently support scan label sheets.", "Scan Label Sheet", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            var path = BarcodeLabelService.GenerateLabelSheet(labels, $"{CurrentSection} Scan Label Sheet");
            OpenReportInApp(path, "Scan Label Sheet");
        }
        catch (Exception ex)
        {
            ErrorLogService.Log(ex, "Generate scan label sheet");
            MessageBox.Show($"Could not generate the label sheet.\n\n{ex.Message}", "Scan Label Sheet", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void AssignMissingCodes_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var result = BarcodeLabelService.AssignMissingCodes();
            RefreshCurrentSection();
            MessageBox.Show(result, "Assign Missing Codes", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            ErrorLogService.Log(ex, "Assign missing codes");
            MessageBox.Show($"Could not assign missing codes.\n\n{ex.Message}", "Assign Missing Codes", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Add_Click(object sender, RoutedEventArgs e)
    {
        if (!_sectionTypes.TryGetValue(CurrentSection, out var type)) return;
        var entity = Activator.CreateInstance(type)!;
        OpenEntityEditorTab($"New {SplitPascalCase(type.Name)}", entity, isNewRecord: true);
    }

    private void Edit_Click(object sender, RoutedEventArgs e)
    {
        var selected = RecordsGrid.SelectedItem;
        if (selected == null || !_sectionTypes.TryGetValue(CurrentSection, out var type)) return;
        var id = (int)type.GetProperty("Id")!.GetValue(selected)!;
        using var db = new AppDbContext();
        var entity = db.Find(type, id);
        if (entity == null) return;
        OpenEntityEditorTab($"Edit {SplitPascalCase(type.Name)} #{id}", entity, isNewRecord: false);
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        var selected = RecordsGrid.SelectedItem;
        if (selected == null || !_sectionTypes.TryGetValue(CurrentSection, out var type)) return;
        if (MessageBox.Show("Delete selected record?", "Confirm delete", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        var id = (int)type.GetProperty("Id")!.GetValue(selected)!;
        using var db = new AppDbContext();
        var entity = db.Find(type, id);
        if (entity == null) return;
        try
        {
            var deletedPurchaseOrderId = entity is PurchaseOrderItem deletedItem ? deletedItem.PurchaseOrderId : 0;
            db.Remove(entity);
            db.SaveChanges();
            if (deletedPurchaseOrderId > 0)
                RecalculatePurchaseOrderTotalsById(deletedPurchaseOrderId);
            RefreshCurrentSection();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not delete the record. It may be linked to another record.\n\n{ex.Message}", "Database error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Export_Click(object sender, RoutedEventArgs e)
    {
        if (_currentRecords == null || _currentRecords.Count == 0) return;
        try
        {
            var firstType = _currentRecords[0]!.GetType();
            var path = CsvExportService.ExportObjects(_currentRecords.Cast<object>().ToList(), firstType, CurrentSection.Replace(" ", "-"));
            MessageBox.Show($"Export created:\n{path}", "CSV Export", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not create the CSV export.\n\n{ex.Message}", "Export error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void AddPhoto_Click(object sender, RoutedEventArgs e)
    {
        var selected = RecordsGrid.SelectedItem;
        if (selected == null || CurrentSection == "Dashboard")
        {
            MessageBox.Show("Select a record first, then click Add Photo.", "Add Photo", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var selectedType = selected.GetType();
        if (selectedType == typeof(PhotoRecord))
        {
            MessageBox.Show("Use Edit on the photo record to change its file path or caption.", "Add Photo", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var id = (int)selectedType.GetProperty("Id")!.GetValue(selected)!;
        var dialog = new OpenFileDialog
        {
            Title = "Choose jewellery, stone, job, or material photo",
            Filter = "Image files|*.jpg;*.jpeg;*.png;*.bmp;*.gif;*.webp|All files|*.*"
        };

        if (dialog.ShowDialog(this) != true) return;

        try
        {
            var storedPath = PhotoStorageService.CopyPhotoToAppFolder(dialog.FileName, selectedType.Name, id);
            using var db = new AppDbContext();
            db.PhotoRecords.Add(new PhotoRecord
            {
                EntityType = selectedType.Name,
                EntityId = id,
                FilePath = storedPath,
                Caption = $"Photo for {selectedType.Name} #{id}"
            });
            db.SaveChanges();
            MessageBox.Show($"Photo attached and copied to app storage:\n{storedPath}", "Photo added", MessageBoxButton.OK, MessageBoxImage.Information);
            RefreshCurrentSection();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not attach the photo.\n\n{ex.Message}", "Photo error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }


    private void StockMovement_Click(object sender, RoutedEventArgs e)
    {
        var window = new InventoryMovementWindow(RecordsGrid.SelectedItem) { Owner = this };
        if (window.ShowDialog() == true)
            RefreshCurrentSection();
    }

    private void ChangeInventoryStatus_Click(object sender, RoutedEventArgs e)
    {
        var selected = RecordsGrid.SelectedItem;
        if (selected is not JewelleryItem and not Stone)
        {
            MessageBox.Show("Select a jewellery stock item or stone first, then click Change Status.", "Change Status", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var window = new InventoryStatusWindow(selected) { Owner = this };
        if (window.ShowDialog() == true)
            RefreshCurrentSection();
    }

    private void TraceSelected_Click(object sender, RoutedEventArgs e)
    {
        var selected = RecordsGrid.SelectedItem;
        if (selected == null || CurrentSection == "Dashboard")
        {
            MessageBox.Show("Select a customer, material, stone, jewellery item, job, sale or market record first.", "Trace Selected", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            using var db = new AppDbContext();
            var trace = InventoryTraceService.BuildTraceText(db, selected);
            var window = new TraceabilityWindow(trace) { Owner = this };
            window.ShowDialog();
        }
        catch (Exception ex)
        {
            ErrorLogService.Log(ex, "Trace selected inventory record");
            MessageBox.Show($"Could not create the trace view.\n\n{ex.Message}", "Trace Selected", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void InventoryReport_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            using var db = new AppDbContext();
            var path = InventoryTraceService.CreateInventoryAuditReport(db);
            OpenReportInApp(path, "Inventory Report");
        }
        catch (Exception ex)
        {
            ErrorLogService.Log(ex, "Create inventory audit report");
            MessageBox.Show($"Could not create the inventory report.\n\n{ex.Message}", "Inventory Report", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }


    private void NewBatch_Click(object sender, RoutedEventArgs e)
    {
        var batch = new ProductionBatch
        {
            BatchCode = $"BATCH-{DateTime.Today:yyyyMMdd}",
            Name = "New production batch",
            StartDate = DateTime.Today,
            TargetCompletionDate = DateTime.Today.AddDays(14),
            Status = ProductionBatchStatus.Planned
        };

        var window = new EditEntityWindow(batch) { Owner = this };
        if (window.ShowDialog() != true) return;

        try
        {
            using var db = new AppDbContext();
            db.ProductionBatches.Add(batch);
            db.SaveChanges();
            SelectNavigationSection("Production Batches");
            MessageBox.Show("Production batch created. Add planned or linked pieces with Add To Batch.", "New Batch", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not create the production batch.\n\n{ex.Message}", "New Batch", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void AddToBatch_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            using var db = new AppDbContext();
            if (!db.ProductionBatches.Any())
            {
                MessageBox.Show("Create a production batch first, then add planned items to it.", "Add To Batch", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not check production batches.\n\n{ex.Message}", "Add To Batch", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        var window = new AddToBatchWindow(RecordsGrid.SelectedItem) { Owner = this };
        if (window.ShowDialog() == true)
        {
            RefreshCurrentSection();
            MessageBox.Show("Item added to the production batch.", "Add To Batch", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void BatchProgress_Click(object sender, RoutedEventArgs e)
    {
        var selectedBatch = RecordsGrid.SelectedItem as ProductionBatch;
        if (selectedBatch == null && RecordsGrid.SelectedItem is ProductionBatchItem selectedBatchItem)
        {
            using var lookupDb = new AppDbContext();
            selectedBatch = lookupDb.ProductionBatches.Find(selectedBatchItem.ProductionBatchId);
        }

        if (selectedBatch == null)
        {
            MessageBox.Show("Select a production batch or batch item first, then click Batch Progress.", "Batch Progress", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            using var db = new AppDbContext();
            var batch = db.ProductionBatches.Find(selectedBatch.Id);
            if (batch == null) return;
            var items = db.ProductionBatchItems.AsEnumerable().Where(i => i.ProductionBatchId == batch.Id).ToList();
            if (items.Count > 0)
            {
                batch.PlannedPieces = (int)Math.Round(items.Sum(i => i.PlannedQuantity), MidpointRounding.AwayFromZero);
                batch.CompletedPieces = (int)Math.Round(items.Sum(i => i.CompletedQuantity), MidpointRounding.AwayFromZero);
                if (batch.EstimatedMaterialCost <= 0) batch.EstimatedMaterialCost = items.Sum(i => i.EstimatedCost);
                if (batch.EstimatedRetailValue <= 0) batch.EstimatedRetailValue = items.Sum(i => i.EstimatedRetailValue);
                if (batch.CompletedPieces >= batch.PlannedPieces && batch.PlannedPieces > 0)
                    batch.Status = ProductionBatchStatus.Completed;
                else if (batch.CompletedPieces > 0)
                    batch.Status = ProductionBatchStatus.InProgress;
            }
            db.SaveChanges();
            RefreshCurrentSection();
            MessageBox.Show($"Batch progress updated: {batch.CompletedPieces}/{batch.PlannedPieces} pieces complete.", "Batch Progress", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not update batch progress.\n\n{ex.Message}", "Batch Progress", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BatchReport_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            using var db = new AppDbContext();
            ProductionBatch? selectedBatch = null;
            if (RecordsGrid.SelectedItem is ProductionBatch batch)
                selectedBatch = db.ProductionBatches.Find(batch.Id);
            else if (RecordsGrid.SelectedItem is ProductionBatchItem item)
                selectedBatch = db.ProductionBatches.Find(item.ProductionBatchId);

            var path = DocumentExportService.CreateProductionBatchReport(selectedBatch);
            OpenReportInApp(path, "Batch Report");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not create the batch report.\n\n{ex.Message}", "Batch Report", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }



    private void ParcelYield_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            using var db = new AppDbContext();
            OpalParcel? parcel = null;

            if (RecordsGrid.SelectedItem is OpalParcel selectedParcel)
                parcel = db.OpalParcels.Find(selectedParcel.Id);
            else if (RecordsGrid.SelectedItem is Stone selectedStone && selectedStone.OpalParcelId.HasValue)
                parcel = db.OpalParcels.Find(selectedStone.OpalParcelId.Value);

            if (parcel == null)
            {
                MessageBox.Show("Select an opal parcel or a stone linked to an opal parcel, then click Parcel Yield.", "Parcel Yield", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var summary = OpalWorkflowService.RecalculateParcelYield(db, parcel);
            RefreshCurrentSection();
            MessageBox.Show(summary, "Parcel Yield Updated", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            ErrorLogService.Log(ex, "Recalculate opal parcel yield");
            MessageBox.Show($"Could not recalculate parcel yield.\n\n{ex.Message}", "Parcel Yield", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void StoneWorkflow_Click(object sender, RoutedEventArgs e)
    {
        if (RecordsGrid.SelectedItem is not Stone selectedStone)
        {
            MessageBox.Show("Select a stone first, then click Stone Workflow.", "Stone Workflow", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            using var db = new AppDbContext();
            var stone = db.Stones.Find(selectedStone.Id);
            if (stone == null) return;
            var window = new StoneWorkflowWindow(stone) { Owner = this };
            if (window.ShowDialog() == true)
                RefreshCurrentSection();
        }
        catch (Exception ex)
        {
            ErrorLogService.Log(ex, "Open stone workflow window");
            MessageBox.Show($"Could not open stone workflow.\n\n{ex.Message}", "Stone Workflow", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OpalReport_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            using var db = new AppDbContext();
            OpalParcel? selectedParcel = null;
            if (RecordsGrid.SelectedItem is OpalParcel parcel)
                selectedParcel = db.OpalParcels.Find(parcel.Id);
            else if (RecordsGrid.SelectedItem is Stone stone && stone.OpalParcelId.HasValue)
                selectedParcel = db.OpalParcels.Find(stone.OpalParcelId.Value);

            string path;
            if (selectedParcel != null)
            {
                path = OpalWorkflowService.CreateOpalYieldReport(selectedParcel);
            }
            else if (CurrentSection == "Stones")
            {
                path = OpalWorkflowService.CreateStoneWorkflowReport();
            }
            else
            {
                path = OpalWorkflowService.CreateOpalYieldReport();
            }

            OpenReportInApp(path, "Opal Report");
        }
        catch (Exception ex)
        {
            ErrorLogService.Log(ex, "Create opal workflow report");
            MessageBox.Show($"Could not create the opal report.\n\n{ex.Message}", "Opal Report", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }


    private void CreateListing_Click(object sender, RoutedEventArgs e)
    {
        if (RecordsGrid.SelectedItem is not JewelleryItem && RecordsGrid.SelectedItem is not OnlineListing)
        {
            MessageBox.Show("Select a jewellery stock item or existing online listing first, then click Create Listing.", "Create Listing", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            using var db = new AppDbContext();
            OnlineListing? listing;
            if (RecordsGrid.SelectedItem is OnlineListing selectedListing)
            {
                listing = db.OnlineListings.Find(selectedListing.Id);
                if (listing == null) return;
            }
            else
            {
                var item = db.JewelleryItems.Find(((JewelleryItem)RecordsGrid.SelectedItem).Id);
                if (item == null) return;
                listing = OnlineListingService.CreateOrUpdateListing(db, item);
            }

            OnlineListingService.UpdateChecklistDerivedStatus(listing);
            db.SaveChanges();
            RefreshCurrentSection();
            MessageBox.Show("Online listing tracker created/updated. Open Online Listings to edit platform, URL, checklist and content fields.", "Create Listing", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            ErrorLogService.Log(ex, "Create online listing");
            MessageBox.Show($"Could not create the listing tracker.\n\n{ex.Message}", "Create Listing", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void GenerateListingContent_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            using var db = new AppDbContext();
            OnlineListing? listing = null;

            if (RecordsGrid.SelectedItem is OnlineListing selectedListing)
                listing = db.OnlineListings.Find(selectedListing.Id);
            else if (RecordsGrid.SelectedItem is JewelleryItem selectedItem)
            {
                var item = db.JewelleryItems.Find(selectedItem.Id);
                if (item != null)
                    listing = OnlineListingService.CreateOrUpdateListing(db, item);
            }

            if (listing == null)
            {
                MessageBox.Show("Select an online listing or jewellery stock item first, then click Generate Content.", "Generate Content", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            OnlineListingService.GenerateContent(db, listing, overwriteExisting: false);
            db.SaveChanges();
            RefreshCurrentSection();
            MessageBox.Show("Listing content generated. Existing manually written content was preserved.", "Generate Content", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            ErrorLogService.Log(ex, "Generate online listing content");
            MessageBox.Show($"Could not generate listing content.\n\n{ex.Message}", "Generate Content", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ListingChecklist_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            using var db = new AppDbContext();
            OnlineListing? listing = null;

            if (RecordsGrid.SelectedItem is OnlineListing selectedListing)
                listing = db.OnlineListings.Find(selectedListing.Id);
            else if (RecordsGrid.SelectedItem is JewelleryItem selectedItem)
            {
                var item = db.JewelleryItems.Find(selectedItem.Id);
                if (item != null)
                    listing = OnlineListingService.CreateOrUpdateListing(db, item);
            }

            if (listing == null)
            {
                MessageBox.Show("Select an online listing or jewellery stock item first, then click Listing Checklist.", "Listing Checklist", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            OnlineListingService.UpdateChecklistDerivedStatus(listing);
            db.SaveChanges();
            var path = OnlineListingService.CreateListingChecklist(listing);
            OpenReportInApp(path, "Listing Checklist");
        }
        catch (Exception ex)
        {
            ErrorLogService.Log(ex, "Create online listing checklist");
            MessageBox.Show($"Could not create listing checklist.\n\n{ex.Message}", "Listing Checklist", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ListingReport_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var path = OnlineListingService.CreateOnlineListingReport();
            OpenReportInApp(path, "Listing Report");
        }
        catch (Exception ex)
        {
            ErrorLogService.Log(ex, "Create online listing report");
            MessageBox.Show($"Could not create listing report.\n\n{ex.Message}", "Listing Report", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }


    private void AdvanceJob_Click(object sender, RoutedEventArgs e)
    {
        if (RecordsGrid.SelectedItem is not Job selectedJob)
        {
            MessageBox.Show("Select a job first, then click Advance Job.", "Advance Job", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        using var db = new AppDbContext();
        var activeJob = db.Jobs.Find(selectedJob.Id);
        if (activeJob == null) return;

        var nextStatus = GetNextJobStatus(activeJob.Status);
        if (nextStatus == activeJob.Status)
        {
            MessageBox.Show("This job is already at its final workflow status.", "Advance Job", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (MessageBox.Show($"Move job '{activeJob}' from {activeJob.Status} to {nextStatus}?", "Advance Job", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;

        activeJob.Status = nextStatus;
        ApplyBusinessRules(db, activeJob, isNewRecord: false);
        db.SaveChanges();
        RefreshCurrentSection();
        MessageBox.Show($"Job moved to {nextStatus}.", "Advance Job", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private static JobStatus GetNextJobStatus(JobStatus status) => status switch
    {
        JobStatus.Enquiry => JobStatus.Quoted,
        JobStatus.Quoted => JobStatus.Approved,
        JobStatus.Approved => JobStatus.DepositPaid,
        JobStatus.DepositPaid => JobStatus.InProgress,
        JobStatus.AwaitingMaterials => JobStatus.InProgress,
        JobStatus.InProgress => JobStatus.AwaitingCustomerApproval,
        JobStatus.AwaitingCustomerApproval => JobStatus.ReadyForPickup,
        JobStatus.ReadyForPickup => JobStatus.Completed,
        JobStatus.ReadyToShip => JobStatus.Completed,
        _ => status
    };

    private void CreateSale_Click(object sender, RoutedEventArgs e)
    {
        var selected = RecordsGrid.SelectedItem;
        if (selected is not JewelleryItem and not Job)
        {
            MessageBox.Show("Select a jewellery stock item or job first, then click Create Sale.", "Create Sale", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        using var db = new AppDbContext();
        var sale = new Sale();

        if (selected is JewelleryItem selectedItem)
        {
            var item = db.JewelleryItems.Find(selectedItem.Id);
            if (item == null) return;
            sale.JewelleryItemId = item.Id;
            sale.SaleAmount = item.RetailPrice;
            sale.CostOfGoods = item.TotalCost;
            sale.Notes = $"Created from jewellery stock item {item.StockCode} {item.Name}".Trim();
        }
        else if (selected is Job selectedJob)
        {
            var linkedJob = db.Jobs.Find(selectedJob.Id);
            if (linkedJob == null) return;
            sale.JobId = linkedJob.Id;
            sale.CustomerId = linkedJob.CustomerId;
            sale.SaleAmount = linkedJob.FinalPrice > 0 ? linkedJob.FinalPrice : linkedJob.QuoteAmount;
            sale.CostOfGoods = linkedJob.MaterialCost + linkedJob.LabourCost;
            sale.SaleLocation = SaleLocation.CustomOrder;
            sale.Notes = $"Created from job {linkedJob.JobCode} {linkedJob.JobTitle}".Trim();
        }

        var window = new EditEntityWindow(sale) { Owner = this };
        if (window.ShowDialog() != true) return;

        try
        {
            ApplyBusinessRules(db, sale, isNewRecord: true);
            db.Sales.Add(sale);
            db.SaveChanges();
            RefreshCurrentSection();
            MessageBox.Show("Sale created. Linked jewellery/jobs were updated where applicable.", "Create Sale", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not create the sale.\n\n{ex.Message}", "Create Sale error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void AddToMarket_Click(object sender, RoutedEventArgs e)
    {
        if (RecordsGrid.SelectedItem is not JewelleryItem selectedItem)
        {
            MessageBox.Show("Select a jewellery stock item first, then click Add To Market.", "Add To Market", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        using var db = new AppDbContext();
        var market = db.MarketEvents
            .AsEnumerable()
            .OrderBy(m => m.EventDate < DateTime.Today)
            .ThenBy(m => Math.Abs((m.EventDate.Date - DateTime.Today).TotalDays))
            .FirstOrDefault();

        if (market == null)
        {
            MessageBox.Show("Add a Market Event first. Then select jewellery stock and click Add To Market.", "Add To Market", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var alreadyAdded = db.MarketStocks.Any(x => x.MarketEventId == market.Id && x.JewelleryItemId == selectedItem.Id);
        if (alreadyAdded)
        {
            MessageBox.Show($"This item is already linked to market '{market}'.", "Add To Market", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (MessageBox.Show($"Add '{selectedItem}' to market '{market}'?", "Add To Market", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;

        db.MarketStocks.Add(new MarketStock
        {
            MarketEventId = market.Id,
            JewelleryItemId = selectedItem.Id,
            Packed = false,
            SoldAtMarket = false,
            Notes = "Added from Jewellery Stock workflow button."
        });

        var item = db.JewelleryItems.Find(selectedItem.Id);
        if (item != null && item.Status != StockStatus.Sold)
            item.Status = StockStatus.AtMarket;

        db.SaveChanges();
        RefreshCurrentSection();
        MessageBox.Show($"Added to market '{market}'.", "Add To Market", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private static void ApplyBusinessRules(AppDbContext db, object entity, bool isNewRecord)
    {
        var settings = BusinessSettingsService.Load();

        if (entity is JewelleryItem jewelleryItem)
        {
            if (jewelleryItem.LabourRate <= 0)
                jewelleryItem.LabourRate = settings.DefaultLabourRate;
        }

        if (entity is Job currentJob)
        {
            if (currentJob.LabourCost <= 0 && currentJob.LabourHours > 0)
                currentJob.LabourCost = currentJob.LabourHours * settings.DefaultLabourRate;
            var invoiceAmount = currentJob.FinalPrice > 0 ? currentJob.FinalPrice : currentJob.QuoteAmount;
            currentJob.BalanceOwing = Math.Max(0, invoiceAmount - currentJob.DepositPaid);
            return;
        }

        if (entity is Sale sale)
        {
            if (sale.JewelleryItemId.HasValue)
            {
                var item = db.JewelleryItems.Find(sale.JewelleryItemId.Value);
                if (item != null)
                {
                    if (sale.SaleAmount <= 0)
                        sale.SaleAmount = item.RetailPrice;
                    if (sale.CostOfGoods <= 0)
                        sale.CostOfGoods = item.TotalCost;
                    item.Status = StockStatus.Sold;
                }
            }

            if (sale.JobId.HasValue)
            {
                var linkedJob = db.Jobs.Find(sale.JobId.Value);
                if (linkedJob != null)
                {
                    if (sale.CustomerId == null)
                        sale.CustomerId = linkedJob.CustomerId;
                    if (sale.SaleAmount <= 0)
                        sale.SaleAmount = linkedJob.FinalPrice > 0 ? linkedJob.FinalPrice : linkedJob.QuoteAmount;
                    if (sale.CostOfGoods <= 0)
                        sale.CostOfGoods = linkedJob.MaterialCost + linkedJob.LabourCost;
                    linkedJob.Status = JobStatus.Completed;
                    linkedJob.BalanceOwing = 0;
                }
            }
        }

        if (entity is OnlineListing onlineListing)
        {
            OnlineListingService.UpdateChecklistDerivedStatus(onlineListing);
            if (onlineListing.ListedOnline && onlineListing.JewelleryItemId.HasValue)
            {
                var item = db.JewelleryItems.Find(onlineListing.JewelleryItemId.Value);
                if (item != null && item.Status != StockStatus.Sold)
                    item.Status = StockStatus.ListedOnline;
            }
        }

        if (entity is BusinessTask businessTask)
        {
            if (string.IsNullOrWhiteSpace(businessTask.TaskCode))
                businessTask.TaskCode = TaskWorkflowService.GenerateTaskCode();
            if (businessTask.Status == BusinessTaskStatus.Completed && !businessTask.CompletedAt.HasValue)
                businessTask.CompletedAt = DateTime.Now;
            if (businessTask.Status != BusinessTaskStatus.Completed)
                businessTask.CompletedAt = null;
        }

        if (entity is PurchaseOrder purchaseOrder)
        {
            if (string.IsNullOrWhiteSpace(purchaseOrder.PurchaseOrderCode))
                purchaseOrder.PurchaseOrderCode = $"PO-{DateTime.Today:yyyyMMdd}-{DateTime.Now:HHmmss}";
            if (!isNewRecord)
                PurchaseOrderService.RecalculatePurchaseOrderTotals(db, purchaseOrder.Id);
        }

        if (entity is PurchaseOrderItem purchaseOrderItem)
        {
            purchaseOrderItem.LineTotal = purchaseOrderItem.OrderedQuantity * purchaseOrderItem.UnitCost;
            if (string.IsNullOrWhiteSpace(purchaseOrderItem.ItemName) && purchaseOrderItem.MaterialId.HasValue)
            {
                var material = db.Materials.Find(purchaseOrderItem.MaterialId.Value);
                if (material != null)
                {
                    purchaseOrderItem.ItemName = material.Name;
                    purchaseOrderItem.UnitType = material.UnitType.ToString();
                }
            }
        }

    }



    private PurchaseOrder? GetSelectedPurchaseOrder(AppDbContext db)
    {
        if (RecordsGrid.SelectedItem is PurchaseOrder selectedOrder)
            return db.PurchaseOrders.Find(selectedOrder.Id);
        if (RecordsGrid.SelectedItem is PurchaseOrderItem selectedItem)
            return db.PurchaseOrders.Find(selectedItem.PurchaseOrderId);
        return db.PurchaseOrders
            .AsEnumerable()
            .Where(p => p.Status is PurchaseOrderStatus.Draft or PurchaseOrderStatus.Ordered or PurchaseOrderStatus.PartiallyReceived)
            .OrderBy(p => p.ExpectedDeliveryDate ?? DateTime.MaxValue)
            .FirstOrDefault();
    }

    private void NewTask_Click(object sender, RoutedEventArgs e)
    {
        var task = TaskWorkflowService.CreateTaskFromSelected(RecordsGrid.SelectedItem);
        var window = new EditEntityWindow(task) { Owner = this };
        if (window.ShowDialog() != true) return;

        using var db = new AppDbContext();
        try
        {
            ApplyBusinessRules(db, task, isNewRecord: true);
            db.BusinessTasks.Add(task);
            db.SaveChanges();
            SelectNavigationSection("Tasks");
            MessageBox.Show("Task created and added to your work queue.", "New Task", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            ErrorLogService.Log(ex, "Create task");
            MessageBox.Show($"Could not create the task.\n\n{ex.Message}", "Task error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CompleteTask_Click(object sender, RoutedEventArgs e)
    {
        if (RecordsGrid.SelectedItem is not BusinessTask selectedTask)
        {
            MessageBox.Show("Select a task first, then click Complete Task.", "Complete Task", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        using var db = new AppDbContext();
        var task = db.BusinessTasks.Find(selectedTask.Id);
        if (task == null) return;

        if (MessageBox.Show($"Mark '{task.Title}' as completed?", "Complete Task", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;

        task.Status = BusinessTaskStatus.Completed;
        task.CompletedAt = DateTime.Now;
        db.SaveChanges();
        RefreshCurrentSection();
        MessageBox.Show("Task completed.", "Complete Task", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void WorkQueue_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var path = TaskWorkflowService.CreateWorkQueueReport();
            OpenReportInApp(path, "Work Queue");
        }
        catch (Exception ex)
        {
            ErrorLogService.Log(ex, "Create work queue");
            MessageBox.Show($"Could not create the work queue.\n\n{ex.Message}", "Work Queue", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CreateFollowUps_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var count = TaskWorkflowService.CreateSuggestedTasks();
            SelectNavigationSection("Tasks");
            MessageBox.Show(count == 0
                ? "No new suggested follow-up tasks were needed."
                : $"Created {count} suggested follow-up task(s).",
                "Create Follow Ups", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            ErrorLogService.Log(ex, "Create suggested follow-up tasks");
            MessageBox.Show($"Could not create suggested tasks.\n\n{ex.Message}", "Create Follow Ups", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void TaskReport_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var path = TaskWorkflowService.CreateTaskReport();
            OpenReportInApp(path, "Task Report");
        }
        catch (Exception ex)
        {
            ErrorLogService.Log(ex, "Create task report");
            MessageBox.Show($"Could not create the task report.\n\n{ex.Message}", "Task Report", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void NewPurchaseOrder_Click(object sender, RoutedEventArgs e)
    {
        var order = new PurchaseOrder
        {
            PurchaseOrderCode = $"PO-{DateTime.Today:yyyyMMdd}-{DateTime.Now:HHmmss}",
            OrderDate = DateTime.Today,
            ExpectedDeliveryDate = DateTime.Today.AddDays(7),
            Status = PurchaseOrderStatus.Draft,
            Notes = "Review supplier, items, quantities and costs before ordering."
        };
        var window = new EditEntityWindow(order) { Owner = this };
        if (window.ShowDialog() != true) return;
        try
        {
            using var db = new AppDbContext();
            ApplyBusinessRules(db, order, isNewRecord: true);
            db.PurchaseOrders.Add(order);
            db.SaveChanges();
            SelectNavigationSection("Purchase Orders");
            MessageBox.Show("Purchase order created. Add line items from Purchase Order Items or use Reorder Suggestions.", "Purchase Order", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not create the purchase order.\n\n{ex.Message}", "Purchase Order", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ReorderSuggestions_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            int? supplierId = null;
            if (RecordsGrid.SelectedItem is Supplier selectedSupplier)
                supplierId = selectedSupplier.Id;
            if (RecordsGrid.SelectedItem is Material selectedMaterial)
                supplierId = selectedMaterial.SupplierId;

            var order = PurchaseOrderService.CreateDraftPurchaseOrderFromLowStock(supplierId);
            SelectNavigationSection("Purchase Orders");
            RefreshCurrentSection();
            MessageBox.Show($"Draft purchase order created from low stock items:\n{order.PurchaseOrderCode}\n\nOpen Purchase Order Items to review quantities and unit costs.", "Reorder Suggestions", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not create reorder suggestions.\n\n{ex.Message}", "Reorder Suggestions", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void MarkPurchaseOrderOrdered_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            using var db = new AppDbContext();
            var order = GetSelectedPurchaseOrder(db);
            if (order == null)
            {
                MessageBox.Show("Select a Purchase Order first.", "Mark Ordered", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            PurchaseOrderService.MarkPurchaseOrderOrdered(order.Id);
            RefreshCurrentSection();
            MessageBox.Show($"{order.PurchaseOrderCode} marked as ordered.", "Purchase Order", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not mark the purchase order as ordered.\n\n{ex.Message}", "Purchase Order", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ReceivePurchaseOrder_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            using var db = new AppDbContext();
            var order = GetSelectedPurchaseOrder(db);
            if (order == null)
            {
                MessageBox.Show("Select a Purchase Order or Purchase Order Item first.", "Receive Purchase Order", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (MessageBox.Show($"Receive all outstanding items for {order.PurchaseOrderCode}?\n\nThis will increase linked material quantities and create material transactions.", "Receive Purchase Order", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;
            var receiveResult = PurchaseOrderService.ReceivePurchaseOrder(order.Id);
            RefreshCurrentSection();
            LoadDashboard();
            MessageBox.Show(receiveResult.ToUserMessage(), "Receive Purchase Order", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not receive the purchase order.\n\n{ex.Message}", "Receive Purchase Order", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void PurchaseOrderPrintout_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            using var db = new AppDbContext();
            var order = GetSelectedPurchaseOrder(db);
            if (order == null)
            {
                MessageBox.Show("Select a Purchase Order first.", "Purchase Order Printout", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            var path = PurchaseOrderService.CreatePurchaseOrderDocument(order.Id);
            OpenReportInApp(path, "Purchase Order Printout");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not create the purchase order document.\n\n{ex.Message}", "Purchase Order Printout", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ReorderReport_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var path = PurchaseOrderService.CreateReorderReport();
            OpenReportInApp(path, "Reorder Report");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not create the reorder report.\n\n{ex.Message}", "Reorder Report", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }


    private MarketEvent? GetSelectedOrNearestMarket(AppDbContext db)
    {
        if (RecordsGrid.SelectedItem is MarketEvent selectedMarket)
            return db.MarketEvents.Find(selectedMarket.Id);
        if (RecordsGrid.SelectedItem is MarketStock selectedStock)
            return db.MarketEvents.Find(selectedStock.MarketEventId);
        return MarketProService.GetNearestMarket(db);
    }

    private void MarketPrep_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            using var db = new AppDbContext();
            var market = GetSelectedOrNearestMarket(db);
            if (market == null)
            {
                MessageBox.Show("Create or select a Market Event first.", "Market Prep", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            MarketProService.PrepareMarket(market.Id);
            RefreshCurrentSection();
            MessageBox.Show($"Market prep updated for {market.Name}. Use Packing List to print the checklist.", "Market Prep", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            ErrorLogService.Log(ex, "Market prep");
            MessageBox.Show($"Could not prepare the market.\n\n{ex.Message}", "Market Prep", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void MarketSale_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var window = new MarketSaleWindow(RecordsGrid.SelectedItem) { Owner = this };
            if (window.ShowDialog() == true)
                RefreshCurrentSection();
        }
        catch (Exception ex)
        {
            ErrorLogService.Log(ex, "Open market sale");
            MessageBox.Show($"Could not open the market sale window.\n\n{ex.Message}", "Market Sale", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ReconcileMarket_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var window = new MarketReconcileWindow(RecordsGrid.SelectedItem) { Owner = this };
            if (window.ShowDialog() == true)
            {
                RefreshCurrentSection();
                MessageBox.Show("Market reconciliation saved.", "Reconcile Market", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            ErrorLogService.Log(ex, "Open market reconciliation");
            MessageBox.Show($"Could not open market reconciliation.\n\n{ex.Message}", "Reconcile Market", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void MarketPackingList_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            using var db = new AppDbContext();
            var market = GetSelectedOrNearestMarket(db);
            if (market == null)
            {
                MessageBox.Show("Create or select a Market Event first.", "Market Packing List", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            var path = MarketProService.CreatePackingListReport(market.Id);
            OpenReportInApp(path, "Market Packing List");
        }
        catch (Exception ex)
        {
            ErrorLogService.Log(ex, "Market packing list");
            MessageBox.Show($"Could not create the packing list.\n\n{ex.Message}", "Market Packing List", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void MarketReconciliationReport_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            using var db = new AppDbContext();
            var market = GetSelectedOrNearestMarket(db);
            if (market == null)
            {
                MessageBox.Show("Create or select a Market Event first.", "Market Reconciliation Report", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            var path = MarketProService.CreateReconciliationReport(market.Id);
            OpenReportInApp(path, "Market Reconciliation Report");
        }
        catch (Exception ex)
        {
            ErrorLogService.Log(ex, "Market reconciliation report");
            MessageBox.Show($"Could not create the market reconciliation report.\n\n{ex.Message}", "Market Reconciliation Report", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void JobCard_Click(object sender, RoutedEventArgs e)
    {
        if (RecordsGrid.SelectedItem is not Job selectedJob)
        {
            MessageBox.Show("Select a job first, then click Job Card.", "Job Card", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            using var db = new AppDbContext();
            var jobForCard = db.Jobs.Find(selectedJob.Id);
            if (jobForCard == null) return;
            var path = DocumentExportService.CreateJobCard(jobForCard);
            OpenReportInApp(path, "Job Card");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not create the job card.\n\n{ex.Message}", "Job Card error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void StockLabel_Click(object sender, RoutedEventArgs e)
    {
        if (RecordsGrid.SelectedItem is not JewelleryItem selectedItem)
        {
            MessageBox.Show("Select a jewellery stock item first, then click Stock Label.", "Stock Label", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            using var db = new AppDbContext();
            var itemForLabel = db.JewelleryItems.Find(selectedItem.Id);
            if (itemForLabel == null) return;
            var path = DocumentExportService.CreateStockLabel(itemForLabel);
            OpenReportInApp(path, "Stock Label");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not create the stock label.\n\n{ex.Message}", "Stock Label error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }


    private void Quote_Click(object sender, RoutedEventArgs e)
    {
        if (RecordsGrid.SelectedItem is not Job selectedJob)
        {
            MessageBox.Show("Select a job first, then click Quote.", "Quote", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            using var db = new AppDbContext();
            var jobForQuote = db.Jobs.Find(selectedJob.Id);
            if (jobForQuote == null) return;
            var path = DocumentExportService.CreateCustomerQuote(jobForQuote);
            OpenReportInApp(path, "Quote");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not create the quote.\n\n{ex.Message}", "Quote error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void InvoiceReceipt_Click(object sender, RoutedEventArgs e)
    {
        var selected = RecordsGrid.SelectedItem;
        if (selected is not Job and not Sale)
        {
            MessageBox.Show("Select a job or sale first, then click Invoice/Receipt.", "Invoice/Receipt", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            using var db = new AppDbContext();
            string path;
            if (selected is Job selectedJob)
            {
                var jobForInvoice = db.Jobs.Find(selectedJob.Id);
                if (jobForInvoice == null) return;
                path = DocumentExportService.CreateInvoiceFromJob(jobForInvoice);
            }
            else
            {
                var selectedSale = (Sale)selected;
                var saleForReceipt = db.Sales.Find(selectedSale.Id);
                if (saleForReceipt == null) return;
                path = DocumentExportService.CreateReceiptFromSale(saleForReceipt);
            }

            OpenReportInApp(path, "Invoice/Receipt");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not create the invoice/receipt.\n\n{ex.Message}", "Invoice/Receipt error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void DepositReceipt_Click(object sender, RoutedEventArgs e)
    {
        var selected = RecordsGrid.SelectedItem;
        if (selected is not Job and not Payment)
        {
            MessageBox.Show("Select a job or payment first, then click Deposit Receipt.", "Deposit Receipt", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            using var db = new AppDbContext();
            string path;
            if (selected is Job selectedJob)
            {
                var jobForReceipt = db.Jobs.Find(selectedJob.Id);
                if (jobForReceipt == null) return;
                path = DocumentExportService.CreateDepositReceiptFromJob(jobForReceipt);
            }
            else
            {
                var selectedPayment = (Payment)selected;
                var paymentForReceipt = db.Payments.Find(selectedPayment.Id);
                if (paymentForReceipt == null) return;
                path = DocumentExportService.CreateDepositReceiptFromPayment(paymentForReceipt);
            }

            OpenReportInApp(path, "Deposit Receipt");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not create the deposit receipt.\n\n{ex.Message}", "Deposit Receipt error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void RepairForm_Click(object sender, RoutedEventArgs e)
    {
        if (RecordsGrid.SelectedItem is not Job selectedJob)
        {
            MessageBox.Show("Select a repair job first, then click Repair Form.", "Repair Form", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            using var db = new AppDbContext();
            var jobForForm = db.Jobs.Find(selectedJob.Id);
            if (jobForForm == null) return;
            var path = DocumentExportService.CreateRepairIntakeForm(jobForForm);
            OpenReportInApp(path, "Repair Form");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not create the repair intake form.\n\n{ex.Message}", "Repair Form error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Agreement_Click(object sender, RoutedEventArgs e)
    {
        if (RecordsGrid.SelectedItem is not Job selectedJob)
        {
            MessageBox.Show("Select a custom job first, then click Agreement.", "Agreement", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            using var db = new AppDbContext();
            var jobForAgreement = db.Jobs.Find(selectedJob.Id);
            if (jobForAgreement == null) return;
            var path = DocumentExportService.CreateCustomOrderAgreement(jobForAgreement);
            OpenReportInApp(path, "Agreement");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not create the custom order agreement.\n\n{ex.Message}", "Agreement error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void PaymentSummary_Click(object sender, RoutedEventArgs e)
    {
        var selected = RecordsGrid.SelectedItem;
        if (selected is not Customer and not Job and not Sale)
        {
            MessageBox.Show("Select a customer, job or sale first, then click Payment Summary.", "Payment Summary", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            using var db = new AppDbContext();
            string path;
            if (selected is Customer selectedCustomer)
            {
                var customer = db.Customers.Find(selectedCustomer.Id);
                if (customer == null) return;
                path = DocumentExportService.CreatePaymentSummaryForCustomer(customer);
            }
            else if (selected is Job selectedJob)
            {
                var paymentSummaryJob = db.Jobs.Find(selectedJob.Id);
                if (paymentSummaryJob == null) return;
                path = DocumentExportService.CreatePaymentSummaryForJob(paymentSummaryJob);
            }
            else
            {
                var selectedSale = (Sale)selected;
                var sale = db.Sales.Find(selectedSale.Id);
                if (sale == null) return;
                path = DocumentExportService.CreatePaymentSummaryForSale(sale);
            }

            OpenReportInApp(path, "Payment Summary");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not create the payment summary.\n\n{ex.Message}", "Payment Summary error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CustomerHistory_Click(object sender, RoutedEventArgs e)
    {
        if (RecordsGrid.SelectedItem is not Customer selectedCustomer)
        {
            MessageBox.Show("Select a customer first, then click Customer History.", "Customer History", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            using var db = new AppDbContext();
            var customer = db.Customers.Find(selectedCustomer.Id);
            if (customer == null) return;
            var path = DocumentExportService.CreateCustomerHistoryReport(customer);
            OpenReportInApp(path, "Customer History");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not create the customer history report.\n\n{ex.Message}", "Customer History error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CustomerSummaryCard_Click(object sender, RoutedEventArgs e)
    {
        if (RecordsGrid.SelectedItem is not Customer selectedCustomer)
        {
            MessageBox.Show("Select a customer first, then click Customer Summary Card.", "Customer Summary Card", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            using var db = new AppDbContext();
            var customer = db.Customers.Find(selectedCustomer.Id);
            if (customer == null) return;
            var path = CustomerRelationshipService.CreateCustomerSummaryCard(customer);
            OpenReportInApp(path, "Customer Summary Card");
        }
        catch (Exception ex)
        {
            ErrorLogService.Log(ex, "Create customer summary card");
            MessageBox.Show($"Could not create the customer summary card.\n\n{ex.Message}", "Customer Summary Card", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CustomerTimeline_Click(object sender, RoutedEventArgs e)
    {
        if (RecordsGrid.SelectedItem is not Customer selectedCustomer)
        {
            MessageBox.Show("Select a customer first, then click Customer Timeline.", "Customer Timeline", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            using var db = new AppDbContext();
            var customer = db.Customers.Find(selectedCustomer.Id);
            if (customer == null) return;
            var path = CustomerRelationshipService.CreateCustomerTimeline(customer);
            OpenReportInApp(path, "Customer Timeline");
        }
        catch (Exception ex)
        {
            ErrorLogService.Log(ex, "Create customer timeline");
            MessageBox.Show($"Could not create the customer timeline.\n\n{ex.Message}", "Customer Timeline", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CustomerFollowUp_Click(object sender, RoutedEventArgs e)
    {
        if (RecordsGrid.SelectedItem is not Customer selectedCustomer)
        {
            MessageBox.Show("Select a customer first, then click Create Customer Follow-Up.", "Customer Follow-Up", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        using var db = new AppDbContext();
        var customer = db.Customers.Find(selectedCustomer.Id);
        if (customer == null) return;

        var task = CustomerRelationshipService.CreateFollowUpTask(customer);
        var window = new EditEntityWindow(task) { Owner = this };
        if (window.ShowDialog() != true) return;

        try
        {
            ApplyBusinessRules(db, task, isNewRecord: true);
            db.BusinessTasks.Add(task);
            db.SaveChanges();
            SelectNavigationSection("Tasks");
            MessageBox.Show("Customer follow-up task created and linked to the customer.", "Customer Follow-Up", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            ErrorLogService.Log(ex, "Create customer follow-up task");
            MessageBox.Show($"Could not create the customer follow-up task.\n\n{ex.Message}", "Customer Follow-Up", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CustomerRelationshipReport_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var path = CustomerRelationshipService.CreateCustomerRelationshipReport();
            OpenReportInApp(path, "Customer Relationship Report");
        }
        catch (Exception ex)
        {
            ErrorLogService.Log(ex, "Create customer relationship report");
            MessageBox.Show($"Could not create the customer relationship report.\n\n{ex.Message}", "Customer Relationship Report", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BusinessReport_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var path = DocumentExportService.CreateBusinessReport();
            OpenReportInApp(path, "Business Report");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not create the business report.\n\n{ex.Message}", "Business Report error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }


    private void CostingReport_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var path = DocumentExportService.CreateCostingReport();
            OpenReportInApp(path, "Costing Report");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not create the costing report.\n\n{ex.Message}", "Costing Report error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void LowStockReport_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var path = DocumentExportService.CreateLowStockReport();
            OpenReportInApp(path, "Low Stock Report");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not create the low stock report.\n\n{ex.Message}", "Low Stock Report error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void JobsDueReport_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var path = DocumentExportService.CreateJobsDueReport();
            OpenReportInApp(path, "Jobs Due Report");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not create the jobs due report.\n\n{ex.Message}", "Jobs Due Report error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void MarketReport_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var path = DocumentExportService.CreateMarketPerformanceReport();
            OpenReportInApp(path, "Market Report");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not create the market report.\n\n{ex.Message}", "Market Report error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }



    private void BusinessIntelligenceReport_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var path = DocumentExportService.CreateBusinessIntelligenceReport();
            OpenReportInApp(path, "Business Intelligence Command Report");
        }
        catch (Exception ex)
        {
            ErrorLogService.Log(ex, "Create business intelligence report");
            MessageBox.Show($"Could not create the business intelligence report.\n\n{ex.Message}", "Business Intelligence error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void WeeklySalesReport_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var path = DocumentExportService.CreateWeeklySalesSummaryReport();
            OpenReportInApp(path, "Weekly Sales Summary");
        }
        catch (Exception ex)
        {
            ErrorLogService.Log(ex, "Create weekly sales report");
            MessageBox.Show($"Could not create the weekly sales report.\n\n{ex.Message}", "Weekly Sales error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void MonthlySalesReport_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var path = DocumentExportService.CreateMonthlySalesSummaryReport();
            OpenReportInApp(path, "Monthly Sales Summary");
        }
        catch (Exception ex)
        {
            ErrorLogService.Log(ex, "Create monthly sales report");
            MessageBox.Show($"Could not create the monthly sales report.\n\n{ex.Message}", "Monthly Sales error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ProfitabilityReport_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var path = DocumentExportService.CreateProfitabilityReport();
            OpenReportInApp(path, "Profitability Report");
        }
        catch (Exception ex)
        {
            ErrorLogService.Log(ex, "Create profitability report");
            MessageBox.Show($"Could not create the profitability report.\n\n{ex.Message}", "Profitability error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OutstandingBalancesReport_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var path = DocumentExportService.CreateOutstandingBalancesReport();
            OpenReportInApp(path, "Outstanding Balances Report");
        }
        catch (Exception ex)
        {
            ErrorLogService.Log(ex, "Create outstanding balances report");
            MessageBox.Show($"Could not create the outstanding balances report.\n\n{ex.Message}", "Outstanding Balances error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void QuoteConversionReport_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var path = DocumentExportService.CreateQuoteConversionReport();
            OpenReportInApp(path, "Quote Conversion Report");
        }
        catch (Exception ex)
        {
            ErrorLogService.Log(ex, "Create quote conversion report");
            MessageBox.Show($"Could not create the quote conversion report.\n\n{ex.Message}", "Quote Conversion error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void InventoryValueReport_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var path = DocumentExportService.CreateInventoryValueReport();
            OpenReportInApp(path, "Inventory Value Report");
        }
        catch (Exception ex)
        {
            ErrorLogService.Log(ex, "Create inventory value report");
            MessageBox.Show($"Could not create the inventory value report.\n\n{ex.Message}", "Inventory Value error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void StockAgeingReport_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var path = DocumentExportService.CreateStockAgeingReport();
            OpenReportInApp(path, "Stock Ageing Report");
        }
        catch (Exception ex)
        {
            ErrorLogService.Log(ex, "Create stock ageing report");
            MessageBox.Show($"Could not create the stock ageing report.\n\n{ex.Message}", "Stock Ageing error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ReservedInventoryReport_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var path = DocumentExportService.CreateReservedInventoryReport();
            OpenReportInApp(path, "Reserved Inventory Report");
        }
        catch (Exception ex)
        {
            ErrorLogService.Log(ex, "Create reserved inventory report");
            MessageBox.Show($"Could not create the reserved inventory report.\n\n{ex.Message}", "Reserved Inventory error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CustomerFollowUpInsightReport_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var path = DocumentExportService.CreateCustomerFollowUpInsightReport();
            OpenReportInApp(path, "Customer Follow-Up Report");
        }
        catch (Exception ex)
        {
            ErrorLogService.Log(ex, "Create customer follow-up insight report");
            MessageBox.Show($"Could not create the customer follow-up report.\n\n{ex.Message}", "Customer Follow-Up Report error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OpalStoneStockReport_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var path = DocumentExportService.CreateOpalStoneStockReport();
            OpenReportInApp(path, "Opal and Stone Stock Report");
        }
        catch (Exception ex)
        {
            ErrorLogService.Log(ex, "Create opal and stone stock report");
            MessageBox.Show($"Could not create the opal and stone stock report.\n\n{ex.Message}", "Opal / Stone Stock error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ExportBusinessIntelligenceCsv_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var path = DocumentExportService.ExportBusinessIntelligenceCsvBundle();
            OpenReportInApp(path, "Business Intelligence CSV Export");
        }
        catch (Exception ex)
        {
            ErrorLogService.Log(ex, "Export business intelligence CSV");
            MessageBox.Show($"Could not export the business intelligence CSV files.\n\n{ex.Message}", "BI CSV Export error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ExportBusinessIntelligenceExcel_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var path = DocumentExportService.ExportBusinessIntelligenceExcelWorkbook();
            OpenReportInApp(path, "Business Intelligence Excel Export");
        }
        catch (Exception ex)
        {
            ErrorLogService.Log(ex, "Export business intelligence Excel workbook");
            MessageBox.Show($"Could not export the business intelligence Excel workbook.\n\n{ex.Message}", "BI Excel Export error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void MetalPrices_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var window = new MetalPricesWindow { Owner = this };
            if (window.ShowDialog() == true)
                RefreshCurrentSection();
        }
        catch (Exception ex)
        {
            ErrorLogService.Log(ex, "Open metal prices");
            MessageBox.Show($"Could not open metal prices.\n\n{ex.Message}", "Metal Prices", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void PricingHelper_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var window = new PricingHelperWindow { Owner = this };
            window.ShowDialog();
        }
        catch (Exception ex)
        {
            ErrorLogService.Log(ex, "Open pricing helper");
            MessageBox.Show($"Could not open the pricing helper.\n\n{ex.Message}", "Pricing Helper", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }


    private void DashboardProductionBoard_Click(object sender, RoutedEventArgs e) => ProductionBoard_Click(sender, e);

    private void DashboardPaymentCollection_Click(object sender, RoutedEventArgs e) => PaymentCollection_Click(sender, e);

    private void DashboardCustomQuotes_Click(object sender, RoutedEventArgs e) => CustomQuoteBuilder_Click(sender, e);

    private void DashboardDiamondWorkflow_Click(object sender, RoutedEventArgs e) => SupplierDiamondWorkflow_Click(sender, e);

    private void DashboardFollowUps_Click(object sender, RoutedEventArgs e)
    {
        SetTopFilter("All Records");
        SearchBox.Text = string.Empty;
        SelectNavigationSection("Tasks");
        StatusText.Text = "Opened Tasks for customer follow-ups. Use the filter/search box to narrow the work queue.";
    }

    private void DashboardSetupNextAction_Click(object sender, RoutedEventArgs e)
    {
        switch (_dashboardSetupTarget)
        {
            case "settings":
                Settings_Click(sender, e);
                break;
            case "metal":
                MetalPrices_Click(sender, e);
                break;
            case "customers":
                SelectNavigationSection("Customers");
                StatusText.Text = "Opened Customers from setup readiness.";
                break;
            case "quote":
                CustomQuoteBuilder_Click(sender, e);
                break;
            case "production":
                ProductionBoard_Click(sender, e);
                break;
            case "backup":
                Backup_Click(sender, e);
                break;
            case "supplier":
                SupplierDiamondWorkflow_Click(sender, e);
                break;
            default:
                ProjectWorkbench_Click(sender, e);
                break;
        }
    }

    private void DashboardCreateBackup_Click(object sender, RoutedEventArgs e) => Backup_Click(sender, e);

    private void DashboardWeeklyReport_Click(object sender, RoutedEventArgs e) => BusinessIntelligenceReport_Click(sender, e);

    private void DashboardAlertCentre_Click(object sender, RoutedEventArgs e) => AlertCentre_Click(sender, e);

    private void RestoreBackup_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Choose a OPALNOVA backup database",
            Filter = "Jewellery backups|*.db;*.zip|SQLite database backup|*.db|Data bundle ZIP|*.zip|All files|*.*"
        };
        if (dialog.ShowDialog(this) != true) return;

        try
        {
            var preview = DataSafetyService.PreviewRestoreSource(dialog.FileName);
            if (MessageBox.Show($"{preview}\n\nStage this restore for the next OPALNOVA startup?", "Restore Backup Preview", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                return;

            var restoreMessage = DataSafetyService.RestoreDatabaseFromBackup(dialog.FileName);
            MessageBox.Show(restoreMessage, "Restore Backup Staged", MessageBoxButton.OK, MessageBoxImage.Information);
            if (CurrentSection == "Dashboard")
                LoadDashboard();
        }
        catch (Exception ex)
        {
            ErrorLogService.Log(ex, "Restore backup");
            MessageBox.Show($"Could not restore the backup.\n\n{ex.Message}\n\nActive database path:\n{DatabaseBootstrapper.DatabasePath}", "Restore Backup error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void HealthCheck_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var report = DataSafetyService.RunDatabaseHealthCheck();
            DataSafetyService.OpenTextReport("Database-Health-Check", report);
            MessageBox.Show(report, "Database Health Check", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            ErrorLogService.Log(ex, "Database health check");
            MessageBox.Show($"Could not complete the database health check.\n\n{ex.Message}", "Health Check error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ExportBundle_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var path = DataSafetyService.CreateFullDataBundle();
            MessageBox.Show($"Full data bundle created:\n{path}\n\nKeep this ZIP private and store a copy somewhere safe.", "Export Bundle", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            ErrorLogService.Log(ex, "Export full data bundle");
            MessageBox.Show($"Could not create the full data bundle.\n\n{ex.Message}", "Export Bundle error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ImportCsv_Click(object sender, RoutedEventArgs e)
    {
        if (CurrentSection == "Dashboard" || !_sectionTypes.TryGetValue(CurrentSection, out var entityType))
        {
            MessageBox.Show("Choose a data section first, then click Import CSV.", "Import CSV", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dialog = new OpenFileDialog
        {
            Title = $"Import CSV into {CurrentSection}",
            Filter = "CSV files|*.csv|All files|*.*"
        };
        if (dialog.ShowDialog(this) != true) return;

        if (MessageBox.Show($"Import rows from this CSV into {CurrentSection}?\n\nThe CSV should have column headers matching the app field names. Id values are ignored so imported rows become new records.", "Import CSV", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;

        try
        {
            var count = DataSafetyService.ImportCsvIntoSection(dialog.FileName, entityType);
            RefreshCurrentSection();
            MessageBox.Show($"Imported {count} row(s) into {CurrentSection}.", "Import CSV", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            ErrorLogService.Log(ex, $"Import CSV into {CurrentSection}");
            MessageBox.Show($"Could not import the CSV.\n\n{ex.Message}", "Import CSV error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ErrorLog_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var log = ErrorLogService.ReadLog();
            DataSafetyService.OpenTextReport("Error-Log-Copy", log);
            MessageBox.Show($"Error log opened.\n\nLocation:\n{ErrorLogService.LogPath}", "Error Log", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not open the error log.\n\n{ex.Message}", "Error Log error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void UserGuide_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var path = DataSafetyService.CreateUserGuide();
            OpenReportInApp(path, "Generated Report");
        }
        catch (Exception ex)
        {
            ErrorLogService.Log(ex, "Open user guide");
            MessageBox.Show($"Could not open the user guide.\n\n{ex.Message}", "User Guide error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ReleaseNotes_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var path = DataSafetyService.CreateReleaseNotes();
            OpenReportInApp(path, "OPALNOVA Release Notes");
        }
        catch (Exception ex)
        {
            ErrorLogService.Log(ex, "Open release notes");
            MessageBox.Show($"Could not open the release notes.\n\n{ex.Message}", "Release Notes error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void About_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            MessageBox.Show(DataSafetyService.CreateAboutText(), "About OPALNOVA", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            ErrorLogService.Log(ex, "About screen");
            MessageBox.Show($"Could not show app information.\n\n{ex.Message}", "About error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }


    private void RecalculateParentPurchaseOrderAfterSave(object entity)
    {
        try
        {
            if (entity is PurchaseOrderItem item && item.PurchaseOrderId > 0)
            {
                RecalculatePurchaseOrderTotalsById(item.PurchaseOrderId);
            }
            else if (entity is PurchaseOrder order && order.Id > 0)
            {
                RecalculatePurchaseOrderTotalsById(order.Id);
            }
        }
        catch (Exception ex)
        {
            ErrorLogService.Log(ex, "Recalculate purchase order totals after save");
        }
    }

    private void RecalculatePurchaseOrderTotalsById(int purchaseOrderId)
    {
        using var db = new AppDbContext();
        PurchaseOrderService.RecalculatePurchaseOrderTotals(db, purchaseOrderId);
        db.SaveChanges();
    }



    private void DataQualityReport_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var path = DataCleanupService.CreateDataQualityReport();
            OpenReportInApp(path, "Data Quality Report");
        }
        catch (Exception ex)
        {
            ErrorLogService.Log(ex, "Create data quality report");
            MessageBox.Show($"Could not create the data quality report.\n\n{ex.Message}", "Data Cleanup", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void DuplicateFinder_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var path = DataCleanupService.CreateDuplicateReport();
            OpenReportInApp(path, "Duplicate Finder");
        }
        catch (Exception ex)
        {
            ErrorLogService.Log(ex, "Create duplicate finder report");
            MessageBox.Show($"Could not create the duplicate finder report.\n\n{ex.Message}", "Data Cleanup", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void MissingDataReport_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var path = DataCleanupService.CreateMissingDataReport();
            OpenReportInApp(path, "Missing Data Report");
        }
        catch (Exception ex)
        {
            ErrorLogService.Log(ex, "Create missing data report");
            MessageBox.Show($"Could not create the missing data report.\n\n{ex.Message}", "Data Cleanup", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private List<object> GetSelectedRecordObjects()
    {
        return RecordsGrid.SelectedItems.Cast<object>().Where(x => x is not null).ToList();
    }

    private void PrepareInteractiveToolPanel(string title, string hint)
    {
        ToolPreviewTitleText.Text = title;
        ToolPreviewHintText.Text = hint;
        ToolPreviewBrowser.Visibility = Visibility.Collapsed;
        ToolPreviewEmptyPanel.Visibility = Visibility.Collapsed;
        ToolInputPanel.Children.Clear();
        _currentReportPath = null;
        ShowToolSetupPage();
        StatusText.Text = title + " ready on the Setup / Inputs page.";
    }

    private void ShowToolMessagePanel(string title, string message, string hint)
    {
        PrepareInteractiveToolPanel(title, hint);
        ToolInputPanel.Children.Add(CreateInfoCard(title, message));
    }

    private TextBlock CreateLabel(string text)
    {
        return new TextBlock
        {
            Text = text.ToUpperInvariant(),
            FontSize = 11,
            FontWeight = FontWeights.Bold,
            Foreground = (System.Windows.Media.Brush)FindResource("MutedTextBrush"),
            Margin = new Thickness(0, 6, 0, 4)
        };
    }

    private Border CreateInfoCard(string title, string body)
    {
        var stack = new StackPanel();
        stack.Children.Add(new TextBlock
        {
            Text = title,
            FontSize = 18,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 0, 0, 6)
        });
        stack.Children.Add(new TextBlock
        {
            Text = body,
            Foreground = (System.Windows.Media.Brush)FindResource("SecondaryTextBrush"),
            TextWrapping = TextWrapping.Wrap,
            LineHeight = 18
        });

        return new Border
        {
            Background = (System.Windows.Media.Brush)FindResource("CardBackgroundBrush"),
            BorderBrush = (System.Windows.Media.Brush)FindResource("BorderBrushSoft"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(14),
            Padding = new Thickness(14),
            Margin = new Thickness(0, 0, 0, 14),
            Child = stack
        };
    }

    private static string GetEntityDisplayText(object item)
    {
        static string? Value(object source, params string[] names)
        {
            foreach (var name in names)
            {
                var property = source.GetType().GetProperty(name);
                var value = property?.GetValue(source);
                if (value != null && !string.IsNullOrWhiteSpace(value.ToString())) return value.ToString();
            }
            return null;
        }

        var label = Value(item, "FullName", "Name", "ItemName", "SeoTitle", "Title", "JobTitle", "JobCode", "StockCode", "StoneCode", "MaterialCode", "TaskCode", "PurchaseOrderCode", "BatchCode", "MarketName") ?? item.GetType().Name;
        var id = item is BaseEntity entity ? $" #{entity.Id}" : string.Empty;
        return $"{label}{id}";
    }

    private void BulkStatusUpdate_Click(object sender, RoutedEventArgs e)
    {
        ShowBulkStatusSelectorPanel();
    }

    private static readonly string[] BulkStatusSections =
    {
        "Jewellery Stock",
        "Stones",
        "Jobs",
        "Online Listings",
        "Tasks",
        "Purchase Orders",
        "Production Batches"
    };

    private void ShowBulkStatusSelectorPanel()
    {
        PrepareInteractiveToolPanel("Bulk Status Update", "Choose the record type, select the records to update, choose the new status, then apply the change.");
        ToolInputPanel.Children.Add(CreateInfoCard("Select Records Here", "Use the controls below to choose the exact records for this bulk action. You no longer need to rely only on rows selected in the main table."));

        ToolInputPanel.Children.Add(CreateLabel("Record Type"));
        var sectionCombo = new ComboBox
        {
            ItemsSource = BulkStatusSections,
            SelectedItem = BulkStatusSections.Contains(CurrentSection) ? CurrentSection : "Jewellery Stock",
            MinHeight = 38,
            Margin = new Thickness(0, 6, 0, 12)
        };
        ToolInputPanel.Children.Add(sectionCombo);

        ToolInputPanel.Children.Add(CreateLabel("Records To Update"));
        var recordList = CreateRecordSelectionListBox();
        ToolInputPanel.Children.Add(recordList);

        ToolInputPanel.Children.Add(CreateLabel("New Status"));
        var statusCombo = new ComboBox
        {
            MinHeight = 38,
            Margin = new Thickness(0, 6, 0, 12)
        };
        ToolInputPanel.Children.Add(statusCombo);

        var selectedCountText = new TextBlock
        {
            Text = "Select one or more records above.",
            Foreground = (System.Windows.Media.Brush)FindResource("MutedTextBrush"),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 14)
        };
        ToolInputPanel.Children.Add(selectedCountText);

        void RefreshRecordsForSection(string section)
        {
            try
            {
                var options = LoadSelectionOptionsForSection(section);
                recordList.ItemsSource = options;
                var type = _sectionTypes[section];
                var statusProperty = type.GetProperties().FirstOrDefault(p => p.Name == "Status" && p.PropertyType.IsEnum);
                statusCombo.ItemsSource = statusProperty != null ? Enum.GetValues(statusProperty.PropertyType).Cast<object>().ToList() : new List<object>();
                statusCombo.SelectedIndex = statusCombo.Items.Count > 0 ? 0 : -1;
                PreselectMatchingOptions(recordList, options, GetSelectedRecordObjects());
                selectedCountText.Text = $"{recordList.SelectedItems.Count} selected from {section}. Use Ctrl/Shift-click to choose multiple records.";
            }
            catch (Exception ex)
            {
                ErrorLogService.Log(ex, "Load bulk status selector records");
                selectedCountText.Text = "Could not load records for this selector. Check the error log for details.";
            }
        }

        sectionCombo.SelectionChanged += (_, _) =>
        {
            if (sectionCombo.SelectedItem is string section)
                RefreshRecordsForSection(section);
        };
        recordList.SelectionChanged += (_, _) =>
        {
            selectedCountText.Text = $"{recordList.SelectedItems.Count} selected. Choose a status, then apply the update.";
        };

        RefreshRecordsForSection(sectionCombo.SelectedItem?.ToString() ?? "Jewellery Stock");

        var applyButton = new WpfButton
        {
            Content = "Apply Bulk Status Update",
            Style = (Style)FindResource("AccentButtonStyle"),
            MinHeight = 42,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            ToolTip = "Apply the selected status to all records selected in this Setup / Inputs panel."
        };
        applyButton.Click += (_, _) =>
        {
            var selectedOptions = recordList.SelectedItems.Cast<EntitySelectionOption>().ToList();
            if (sectionCombo.SelectedItem is not string section || !_sectionTypes.TryGetValue(section, out var selectedType))
            {
                MessageBox.Show("Choose a record type first.", "Bulk Status Update", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var statusProperty = selectedType.GetProperties().FirstOrDefault(p => p.Name == "Status" && p.PropertyType.IsEnum);
            if (statusProperty == null)
            {
                MessageBox.Show("The selected record type does not support status updates.", "Bulk Status Update", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            ApplyBulkStatusFromToolPanel(selectedOptions.Select(o => o.Entity).ToList(), selectedType, statusProperty, statusCombo.SelectedItem);
        };
        ToolInputPanel.Children.Add(applyButton);
    }

    private ListBox CreateRecordSelectionListBox()
    {
        return new ListBox
        {
            SelectionMode = SelectionMode.Extended,
            DisplayMemberPath = nameof(EntitySelectionOption.Label),
            MinHeight = 220,
            MaxHeight = 320,
            Margin = new Thickness(0, 6, 0, 12),
            ToolTip = "Select one or more records. Use Ctrl-click or Shift-click for multi-select."
        };
    }

    private List<EntitySelectionOption> LoadSelectionOptionsForSection(string section)
    {
        return LoadEntitiesForSection(section)
            .Select(entity => new EntitySelectionOption(GetEntityDisplayText(entity), entity))
            .OrderBy(option => option.Label)
            .ToList();
    }

    private List<object> LoadEntitiesForSection(string section)
    {
        using var db = new AppDbContext();
        return section switch
        {
            "Customers" => db.Customers.AsNoTracking().OrderBy(x => x.FullName).Cast<object>().ToList(),
            "Suppliers" => db.Suppliers.AsNoTracking().OrderBy(x => x.Name).Cast<object>().ToList(),
            "Materials" => db.Materials.AsNoTracking().OrderBy(x => x.MaterialCode).ThenBy(x => x.Name).Cast<object>().ToList(),
            "Material Transactions" => db.MaterialTransactions.AsNoTracking().OrderByDescending(x => x.TransactionDate).Cast<object>().ToList(),
            "Opal Parcels" => db.OpalParcels.AsNoTracking().OrderBy(x => x.ParcelCode).Cast<object>().ToList(),
            "Stones" => db.Stones.AsNoTracking().OrderBy(x => x.StoneCode).Cast<object>().ToList(),
            "Jewellery Stock" => db.JewelleryItems.AsNoTracking().OrderBy(x => x.StockCode).ThenBy(x => x.Name).Cast<object>().ToList(),
            "Jobs" => db.Jobs.AsNoTracking().OrderByDescending(x => x.DateReceived).Cast<object>().ToList(),
            "Sales" => db.Sales.AsNoTracking().OrderByDescending(x => x.SaleDate).Cast<object>().ToList(),
            "Payments" => db.Payments.AsNoTracking().OrderByDescending(x => x.PaymentDate).Cast<object>().ToList(),
            "Market Events" => db.MarketEvents.AsNoTracking().OrderBy(x => x.EventDate).Cast<object>().ToList(),
            "Market Stock" => db.MarketStocks.AsNoTracking().OrderByDescending(x => x.Id).Cast<object>().ToList(),
            "Production Batches" => db.ProductionBatches.AsNoTracking().OrderByDescending(x => x.StartDate).Cast<object>().ToList(),
            "Batch Items" => db.ProductionBatchItems.AsNoTracking().OrderByDescending(x => x.Id).Cast<object>().ToList(),
            "Online Listings" => db.OnlineListings.AsNoTracking().OrderBy(x => x.SeoTitle).Cast<object>().ToList(),
            "Purchase Orders" => db.PurchaseOrders.AsNoTracking().OrderByDescending(x => x.OrderDate).Cast<object>().ToList(),
            "Purchase Order Items" => db.PurchaseOrderItems.AsNoTracking().OrderByDescending(x => x.Id).Cast<object>().ToList(),
            "Tasks" => db.BusinessTasks.AsNoTracking().OrderBy(x => x.DueDate).ThenBy(x => x.Title).Cast<object>().ToList(),
            "Photos" => db.PhotoRecords.AsNoTracking().OrderByDescending(x => x.Id).Cast<object>().ToList(),
            _ => new List<object>()
        };
    }


    private ComboBox CreateSectionCombo(IEnumerable<string> sections, string? preferredSection = null)
    {
        var sectionList = sections.ToList();
        var combo = new ComboBox
        {
            ItemsSource = sectionList,
            SelectedItem = sectionList.Contains(preferredSection ?? string.Empty) ? preferredSection : sectionList.FirstOrDefault(),
            MinHeight = 38,
            Margin = new Thickness(0, 6, 0, 12)
        };
        return combo;
    }

    private ComboBox CreateSingleRecordCombo()
    {
        return new ComboBox
        {
            DisplayMemberPath = nameof(EntitySelectionOption.Label),
            MinHeight = 38,
            Margin = new Thickness(0, 6, 0, 12),
            ToolTip = "Choose the record this tool should use."
        };
    }

    private void PreselectMatchingComboOption(ComboBox comboBox, List<EntitySelectionOption> options, object? previousSelection)
    {
        if (previousSelection is not BaseEntity selectedEntity) return;
        var match = options.FirstOrDefault(option => option.Entity is BaseEntity entity && option.Entity.GetType() == previousSelection.GetType() && entity.Id == selectedEntity.Id);
        if (match != null) comboBox.SelectedItem = match;
    }

    private static string SingularSectionLabel(string section) => section switch
    {
        "Customers" => "customer",
        "Suppliers" => "supplier",
        "Materials" => "material",
        "Material Transactions" => "material transaction",
        "Opal Parcels" => "opal parcel",
        "Stones" => "stone",
        "Jewellery Stock" => "jewellery item",
        "Jobs" => "job",
        "Sales" => "sale",
        "Payments" => "payment",
        "Market Events" => "market event",
        "Market Stock" => "market stock item",
        "Production Batches" => "production batch",
        "Batch Items" => "batch item",
        "Online Listings" => "online listing",
        "Purchase Orders" => "purchase order",
        "Purchase Order Items" => "purchase order item",
        "Tasks" => "task",
        "Photos" => "photo",
        _ => section.TrimEnd('s').ToLowerInvariant()
    };

    private void SelectRecordForTool(object? entity)
    {
        RecordsGrid.SelectedItem = entity;
        if (entity is BaseEntity baseEntity)
        {
            UpdateRecordPreview(entity);
            StatusText.Text = $"Selected {GetEntityDisplayText(entity)} for this tool.";
        }
    }

    private void ShowSingleRecordToolPanel(string title, string hint, IEnumerable<string> sections, RoutedEventHandler action, string buttonText, bool allowNoRecord = false)
    {
        PrepareInteractiveToolPanel(title, hint);
        ToolInputPanel.Children.Add(CreateInfoCard("Choose Required Record", "Select the record this tool should use, then run the action. The generated result will appear on the Preview / Result page when applicable."));

        var sectionList = sections.ToList();
        ToolInputPanel.Children.Add(CreateLabel("Record Type"));
        var sectionCombo = CreateSectionCombo(sectionList, sectionList.Contains(CurrentSection) ? CurrentSection : sectionList.FirstOrDefault());
        ToolInputPanel.Children.Add(sectionCombo);

        ToolInputPanel.Children.Add(CreateLabel("Record"));
        var recordCombo = CreateSingleRecordCombo();
        ToolInputPanel.Children.Add(recordCombo);

        var help = new TextBlock
        {
            Foreground = (System.Windows.Media.Brush)FindResource("MutedTextBrush"),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 14)
        };
        ToolInputPanel.Children.Add(help);

        void RefreshRecordCombo()
        {
            if (sectionCombo.SelectedItem is not string section) return;
            var options = LoadSelectionOptionsForSection(section);
            var displayOptions = new List<EntitySelectionOption> { new($"Select {SingularSectionLabel(section)}", null!) };
            displayOptions.AddRange(options);
            recordCombo.ItemsSource = displayOptions;
            recordCombo.SelectedIndex = 0;
            PreselectMatchingComboOption(recordCombo, displayOptions, RecordsGrid.SelectedItem);
            help.Text = options.Count == 0
                ? $"No {section} records are available yet. Create a record first, then return to this tool."
                : $"{options.Count} {section} record(s) available. Choose one, then run the tool.";
        }

        sectionCombo.SelectionChanged += (_, _) => RefreshRecordCombo();
        RefreshRecordCombo();

        var runButton = new WpfButton
        {
            Content = buttonText,
            Style = (Style)FindResource("AccentButtonStyle"),
            MinHeight = 42,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            ToolTip = buttonText
        };
        runButton.Click += (sender, args) =>
        {
            var selectedOption = recordCombo.SelectedItem as EntitySelectionOption;
            if (!allowNoRecord && selectedOption?.Entity == null)
            {
                MessageBox.Show("Choose a record before running this tool.", title, MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            SelectRecordForTool(selectedOption?.Entity);
            action(sender, args);
        };
        ToolInputPanel.Children.Add(runButton);
    }

    private void ShowSectionToolPanel(string title, string hint, IEnumerable<string> sections, Action<string> action, string buttonText)
    {
        PrepareInteractiveToolPanel(title, hint);
        ToolInputPanel.Children.Add(CreateInfoCard("Choose Section", "Select the section this tool should use, then generate the result."));
        ToolInputPanel.Children.Add(CreateLabel("Section"));
        var sectionCombo = CreateSectionCombo(sections, sections.Contains(CurrentSection) ? CurrentSection : sections.FirstOrDefault());
        ToolInputPanel.Children.Add(sectionCombo);
        var runButton = new WpfButton
        {
            Content = buttonText,
            Style = (Style)FindResource("AccentButtonStyle"),
            MinHeight = 42,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        runButton.Click += (_, _) =>
        {
            if (sectionCombo.SelectedItem is not string section)
            {
                MessageBox.Show("Choose a section first.", title, MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            action(section);
        };
        ToolInputPanel.Children.Add(runButton);
    }

    private void GenerateLabelSheetForSection(string section)
    {
        var labels = LoadEntitiesForSection(section)
            .Select(BarcodeLabelService.FromRecord)
            .Where(label => label != null)
            .Cast<ScanLabelItem>()
            .ToList();
        if (labels.Count == 0)
        {
            MessageBox.Show($"{section} does not have supported scan-label records.", "Scan Label Sheet", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        try
        {
            var path = BarcodeLabelService.GenerateLabelSheet(labels, $"{section} Scan Label Sheet");
            OpenReportInApp(path, "Scan Label Sheet");
        }
        catch (Exception ex)
        {
            ErrorLogService.Log(ex, "Generate scan label sheet from setup panel");
            MessageBox.Show($"Could not generate the label sheet.\n\n{ex.Message}", "Scan Label Sheet", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static readonly string[] AnyTraceSections =
    {
        "Customers", "Materials", "Opal Parcels", "Stones", "Jewellery Stock", "Jobs", "Sales", "Market Events", "Market Stock", "Purchase Orders", "Tasks"
    };

    private static readonly string[] ScanLabelSections =
    {
        "Jewellery Stock", "Stones", "Jobs", "Materials", "Purchase Orders", "Production Batches", "Tasks", "Market Stock"
    };

    private static readonly string[] TaskLinkSections =
    {
        "Customers", "Jobs", "Jewellery Stock", "Stones", "Market Events", "Production Batches", "Purchase Orders", "Online Listings"
    };

    private void StockMovementSetup_Click(object sender, RoutedEventArgs e) =>
        ShowSingleRecordToolPanel("Stock Movement", "Choose a material, job, or jewellery item before recording an inventory movement.", new[] { "Materials", "Jobs", "Jewellery Stock" }, StockMovement_Click, "Open Stock Movement");

    private void ChangeInventoryStatusSetup_Click(object sender, RoutedEventArgs e) =>
        ShowSingleRecordToolPanel("Change Status", "Choose a jewellery item or stone, then select its new status.", new[] { "Jewellery Stock", "Stones" }, ChangeInventoryStatus_Click, "Open Change Status");

    private void TraceSelectedSetup_Click(object sender, RoutedEventArgs e) =>
        ShowSingleRecordToolPanel("Trace Selected", "Choose any supported record to view its linked records and activity trail.", AnyTraceSections, TraceSelected_Click, "Build Trace View");

    private void MarkPurchaseOrderOrderedSetup_Click(object sender, RoutedEventArgs e) =>
        ShowSingleRecordToolPanel("Mark Purchase Order Ordered", "Choose a purchase order, then mark it as ordered.", new[] { "Purchase Orders", "Purchase Order Items" }, MarkPurchaseOrderOrdered_Click, "Mark Ordered");

    private void ReceivePurchaseOrderSetup_Click(object sender, RoutedEventArgs e) =>
        ShowSingleRecordToolPanel("Receive Purchase Order", "Choose the purchase order being received. The receive workflow will update materials and material transactions.", new[] { "Purchase Orders", "Purchase Order Items" }, ReceivePurchaseOrder_Click, "Receive Purchase Order");

    private void PurchaseOrderPrintoutSetup_Click(object sender, RoutedEventArgs e) =>
        ShowSingleRecordToolPanel("Purchase Order Printout", "Choose a purchase order to preview and print.", new[] { "Purchase Orders", "Purchase Order Items" }, PurchaseOrderPrintout_Click, "Generate Purchase Order Printout");

    private void AddToBatchSetup_Click(object sender, RoutedEventArgs e) =>
        ShowSingleRecordToolPanel("Add To Batch", "Choose a jewellery item, stone, or job to add to a production batch.", new[] { "Jewellery Stock", "Stones", "Jobs" }, AddToBatch_Click, "Open Add To Batch");

    private void BatchProgressSetup_Click(object sender, RoutedEventArgs e) =>
        ShowSingleRecordToolPanel("Batch Progress", "Choose a production batch or batch item to update progress.", new[] { "Production Batches", "Batch Items" }, BatchProgress_Click, "Update Batch Progress");

    private void BatchReportSetup_Click(object sender, RoutedEventArgs e) =>
        ShowSingleRecordToolPanel("Batch Report", "Choose a production batch or batch item for a focused batch report.", new[] { "Production Batches", "Batch Items" }, BatchReport_Click, "Generate Batch Report");

    private void ParcelYieldSetup_Click(object sender, RoutedEventArgs e) =>
        ShowSingleRecordToolPanel("Parcel Yield", "Choose an opal parcel or linked stone to recalculate yield and cost per finished carat.", new[] { "Opal Parcels", "Stones" }, ParcelYield_Click, "Calculate Parcel Yield");

    private void StoneWorkflowSetup_Click(object sender, RoutedEventArgs e) =>
        ShowSingleRecordToolPanel("Stone Workflow", "Choose a stone and move it through rough, cutting, polished, set, reserved or sold stages.", new[] { "Stones" }, StoneWorkflow_Click, "Open Stone Workflow");

    private void MarketPrepSetup_Click(object sender, RoutedEventArgs e) =>
        ShowSingleRecordToolPanel("Market Prep", "Choose a market event or market stock record to prepare the market workflow.", new[] { "Market Events", "Market Stock" }, MarketPrep_Click, "Open Market Prep");

    private void MarketSaleSetup_Click(object sender, RoutedEventArgs e) =>
        ShowSingleRecordToolPanel("Market Sale", "Choose market stock, a market event or jewellery item before recording a market sale.", new[] { "Market Stock", "Market Events", "Jewellery Stock" }, MarketSale_Click, "Open Market Sale");

    private void ReconcileMarketSetup_Click(object sender, RoutedEventArgs e) =>
        ShowSingleRecordToolPanel("Reconcile Market", "Choose a market event or stock record, then enter takings, costs and reconciliation notes.", new[] { "Market Events", "Market Stock" }, ReconcileMarket_Click, "Open Reconcile Market");

    private void MarketPackingListSetup_Click(object sender, RoutedEventArgs e) =>
        ShowSingleRecordToolPanel("Market Packing List", "Choose the market event to preview a packing list.", new[] { "Market Events", "Market Stock" }, MarketPackingList_Click, "Generate Packing List");

    private void MarketReconciliationReportSetup_Click(object sender, RoutedEventArgs e) =>
        ShowSingleRecordToolPanel("Market Reconciliation Report", "Choose the market event to preview reconciliation results.", new[] { "Market Events", "Market Stock" }, MarketReconciliationReport_Click, "Generate Reconciliation Report");

    private void CreateListingSetup_Click(object sender, RoutedEventArgs e) =>
        ShowSingleRecordToolPanel("Create Listing", "Choose finished jewellery stock to create an online listing record.", new[] { "Jewellery Stock" }, CreateListing_Click, "Create Listing");

    private void GenerateListingContentSetup_Click(object sender, RoutedEventArgs e) =>
        ShowSingleRecordToolPanel("Generate Listing Content", "Choose a jewellery item or online listing to generate SEO title, descriptions, caption and hashtags.", new[] { "Jewellery Stock", "Online Listings" }, GenerateListingContent_Click, "Generate Content");

    private void ListingChecklistSetup_Click(object sender, RoutedEventArgs e) =>
        ShowSingleRecordToolPanel("Listing Checklist", "Choose a listing or jewellery item to preview its listing checklist.", new[] { "Online Listings", "Jewellery Stock" }, ListingChecklist_Click, "Generate Listing Checklist");

    private void NewTaskSetup_Click(object sender, RoutedEventArgs e) =>
        ShowSingleRecordToolPanel("New Task", "Optionally choose a record to link to the new task.", TaskLinkSections, NewTask_Click, "Create Linked Task", allowNoRecord: true);

    private void CompleteTaskSetup_Click(object sender, RoutedEventArgs e) =>
        ShowSingleRecordToolPanel("Complete Task", "Choose a task to mark completed.", new[] { "Tasks" }, CompleteTask_Click, "Complete Task");

    private void GenerateSelectedScanLabelSetup_Click(object sender, RoutedEventArgs e) =>
        ShowSingleRecordToolPanel("Generate Scan Label", "Choose a record to generate a barcode/scan label.", ScanLabelSections, GenerateSelectedScanLabel_Click, "Generate Selected Scan Label");

    private void GenerateScanLabelSheetSetup_Click(object sender, RoutedEventArgs e) =>
        ShowSectionToolPanel("Generate Label Sheet", "Choose the section to generate a full scan label sheet for.", ScanLabelSections, GenerateLabelSheetForSection, "Generate Label Sheet");

    private void JobCardSetup_Click(object sender, RoutedEventArgs e) =>
        ShowSingleRecordToolPanel("Job Card", "Choose a job to preview a bench job card.", new[] { "Jobs" }, JobCard_Click, "Generate Job Card");

    private void StockLabelSetup_Click(object sender, RoutedEventArgs e) =>
        ShowSingleRecordToolPanel("Stock Label", "Choose a jewellery stock item for a printable price/stock label.", new[] { "Jewellery Stock" }, StockLabel_Click, "Generate Stock Label");

    private void AlertCentre_Click(object sender, RoutedEventArgs e)
    {
        var window = new AlertCentreWindow { IsHostedInTab = true };
        TabItem? tab = null;
        window.OpenTargetRequested += (_, target) => OpenNextActionTarget(target);
        window.CloseRequested += (_, _) => CloseWorkspaceTab(tab);
        tab = OpenWindowInWorkspaceTab("Alert Centre", window, "workflow:alert-centre", RefreshAfterWorkspaceTabClosed);
    }

    private void OpenNextActionTarget(string target)
    {
        switch (target)
        {
            case "Project Workbench":
                ProjectWorkbench_Click(this, new RoutedEventArgs());
                break;
            case "Custom Quotes":
                CustomQuoteBuilder_Click(this, new RoutedEventArgs());
                break;
            case "Production":
                ProductionBoard_Click(this, new RoutedEventArgs());
                break;
            case "Payments":
                PaymentCollection_Click(this, new RoutedEventArgs());
                break;
            case "Diamond Holds":
                SupplierDiamondWorkflow_Click(this, new RoutedEventArgs());
                break;
            case "Diamond Search":
                DiamondSupplier_Click(this, new RoutedEventArgs());
                break;
            case "Materials":
            case "Tasks":
            case "Customers":
            case "Jobs":
            case "External Diamonds":
                SelectNavigationSection(target);
                StatusText.Text = $"Opened {target} from Alert Centre.";
                break;
            default:
                if (_sectionTypes.ContainsKey(target) || _toolSections.Contains(target))
                {
                    SelectNavigationSection(target);
                    StatusText.Text = $"Opened {target} from Alert Centre.";
                }
                else
                {
                    ProjectWorkbench_Click(this, new RoutedEventArgs());
                }
                break;
        }
    }

    private void ProjectWorkbench_Click(object sender, RoutedEventArgs e)
    {
        var window = new ProjectWorkbenchWindow { IsHostedInTab = true };
        TabItem? tab = null;
        window.OpenQuotesRequested += (_, _) => CustomQuoteBuilder_Click(sender, e);
        window.OpenProductionRequested += (_, _) => ProductionBoard_Click(sender, e);
        window.OpenPaymentsRequested += (_, _) => PaymentCollection_Click(sender, e);
        window.OpenDiamondHoldsRequested += (_, _) => SupplierDiamondWorkflow_Click(sender, e);
        window.OpenDiamondSearchRequested += (_, _) => DiamondSupplier_Click(sender, e);
        window.OpenCustomersRequested += (_, _) => SelectNavigationSection("Customers");
        window.OpenJobsRequested += (_, _) => SelectNavigationSection("Jobs");
        window.CloseRequested += (_, _) => CloseWorkspaceTab(tab);
        tab = OpenWindowInWorkspaceTab("Project Workbench", window, "workflow:project-workbench", RefreshAfterWorkspaceTabClosed);
    }

    private void ProductionBoard_Click(object sender, RoutedEventArgs e)
    {
        var window = new ProductionBoardWindow();
        OpenWindowInWorkspaceTab("Production Board", window, "workflow:production-board", RefreshAfterWorkspaceTabClosed);
    }

    private void CustomQuoteBuilder_Click(object sender, RoutedEventArgs e)
    {
        var window = new CustomQuoteBuilderWindow();
        OpenWindowInWorkspaceTab("Custom Quotes", window, "workflow:custom-quotes", RefreshAfterWorkspaceTabClosed);
    }

    private void DiamondSupplier_Click(object sender, RoutedEventArgs e)
    {
        var window = new DiamondSupplierWindow { IsHostedInTab = true };
        TabItem? tab = null;
        window.OpenSavedRecordsRequested += (_, _) =>
        {
            CloseWorkspaceTab(tab);
            SelectNavigationSection("External Diamonds");
        };
        window.CloseRequested += (_, _) => CloseWorkspaceTab(tab);
        tab = OpenWindowInWorkspaceTab("Diamond Search", window, "workflow:diamond-search", RefreshAfterWorkspaceTabClosed);
    }

    private void SupplierDiamondWorkflow_Click(object sender, RoutedEventArgs e)
    {
        var window = new SupplierDiamondWorkflowWindow { IsHostedInTab = true };
        TabItem? tab = null;
        window.OpenSavedRecordsRequested += (_, _) =>
        {
            CloseWorkspaceTab(tab);
            SelectNavigationSection("External Diamonds");
        };
        window.CloseRequested += (_, _) => CloseWorkspaceTab(tab);
        tab = OpenWindowInWorkspaceTab("Diamond Holds", window, "workflow:diamond-holds", RefreshAfterWorkspaceTabClosed);
    }

    private void ExternalDiamondRegister_Click(object sender, RoutedEventArgs e)
    {
        SelectNavigationSection("External Diamonds");
    }

    private void PaymentCollection_Click(object sender, RoutedEventArgs e)
    {
        var window = new PaymentCollectionWindow();
        OpenWindowInWorkspaceTab("Payment & Collection", window, "workflow:payment-collection", RefreshAfterWorkspaceTabClosed);
    }

    private void CustomQuoteRegister_Click(object sender, RoutedEventArgs e)
    {
        SelectNavigationSection("Custom Quotes");
    }

    private void QuoteSetup_Click(object sender, RoutedEventArgs e) =>
        ShowSingleRecordToolPanel("Quote", "Choose a job to preview a customer quote.", new[] { "Jobs" }, Quote_Click, "Generate Quote");

    private void InvoiceReceiptSetup_Click(object sender, RoutedEventArgs e) =>
        ShowSingleRecordToolPanel("Invoice / Receipt", "Choose a job or sale to preview an invoice/receipt.", new[] { "Jobs", "Sales" }, InvoiceReceipt_Click, "Generate Invoice / Receipt");

    private void DepositReceiptSetup_Click(object sender, RoutedEventArgs e) =>
        ShowSingleRecordToolPanel("Deposit Receipt", "Choose a job or payment for a deposit receipt.", new[] { "Jobs", "Payments" }, DepositReceipt_Click, "Generate Deposit Receipt");

    private void RepairFormSetup_Click(object sender, RoutedEventArgs e) =>
        ShowSingleRecordToolPanel("Repair Form", "Choose a repair job to preview a repair intake form.", new[] { "Jobs" }, RepairForm_Click, "Generate Repair Form");

    private void AgreementSetup_Click(object sender, RoutedEventArgs e) =>
        ShowSingleRecordToolPanel("Agreement", "Choose a job to preview a custom order agreement.", new[] { "Jobs" }, Agreement_Click, "Generate Agreement");

    private void PaymentSummarySetup_Click(object sender, RoutedEventArgs e) =>
        ShowSingleRecordToolPanel("Payment Summary", "Choose a customer, job or sale to preview a payment summary.", new[] { "Customers", "Jobs", "Sales" }, PaymentSummary_Click, "Generate Payment Summary");

    private void CustomerHistorySetup_Click(object sender, RoutedEventArgs e) =>
        ShowSingleRecordToolPanel("Customer History", "Choose a customer to preview their purchase/job history.", new[] { "Customers" }, CustomerHistory_Click, "Generate Customer History");

    private void CustomerSummaryCardSetup_Click(object sender, RoutedEventArgs e) =>
        ShowSingleRecordToolPanel("Customer Summary Card", "Choose a customer to preview their contact details, preferences, work history, sales and next follow-up.", new[] { "Customers" }, CustomerSummaryCard_Click, "Generate Customer Summary Card");

    private void CustomerTimelineSetup_Click(object sender, RoutedEventArgs e) =>
        ShowSingleRecordToolPanel("Customer Timeline", "Choose a customer to preview quotes, proposal events, jobs, sales, payments and follow-ups in date order.", new[] { "Customers" }, CustomerTimeline_Click, "Generate Customer Timeline");

    private void CustomerFollowUpSetup_Click(object sender, RoutedEventArgs e) =>
        ShowSingleRecordToolPanel("Create Customer Follow-Up", "Choose a customer and create a linked follow-up task. You can edit the due date, reason and notes before saving.", new[] { "Customers" }, CustomerFollowUp_Click, "Create Customer Follow-Up");

    private void DymoMiniLabelSetup_Click(object sender, RoutedEventArgs e) =>
        ShowSingleRecordToolPanel("DYMO Mini Label", "Choose a stock item, stone, job or material to create a mini label.", new[] { "Jewellery Stock", "Stones", "Jobs", "Materials" }, DymoMiniLabel_Click, "Open DYMO Mini Label");

    private void DeviceCaptureSetup_Click(object sender, RoutedEventArgs e) =>
        ShowSingleRecordToolPanel("Camera & Scale Capture", "Choose a material or stone before applying scale readings or photos.", new[] { "Materials", "Stones" }, DeviceCapture_Click, "Open Camera & Scale Capture");

    private static void PreselectMatchingOptions(ListBox listBox, List<EntitySelectionOption> options, List<object> previousSelection)
    {
        var selectedIds = previousSelection.OfType<BaseEntity>().Select(x => (Type: x.GetType(), x.Id)).ToHashSet();
        if (selectedIds.Count == 0) return;
        foreach (var option in options)
        {
            if (option.Entity is BaseEntity entity && selectedIds.Contains((option.Entity.GetType(), entity.Id)))
                listBox.SelectedItems.Add(option);
        }
    }

    private bool TryPrepareBulkStatusSelection(List<object> selected, out Type firstType, out System.Reflection.PropertyInfo? statusProperty, out string message)
    {
        firstType = selected.FirstOrDefault()?.GetType() ?? typeof(object);
        statusProperty = null;
        if (selected.Count == 0)
        {
            message = "No records are currently selected.";
            return false;
        }

        var selectedType = selected[0].GetType();
        if (selected.Any(x => x.GetType() != selectedType))
        {
            message = "Bulk status update can only update one record type at a time.";
            return false;
        }

        firstType = selectedType;
        statusProperty = selectedType.GetProperties().FirstOrDefault(p => p.Name == "Status" && p.PropertyType.IsEnum);
        if (statusProperty == null)
        {
            message = $"{CurrentSection} records do not have a supported Status field for bulk update.";
            return false;
        }

        message = string.Empty;
        return true;
    }

    private void ShowBulkStatusToolPanel(List<object> selected, Type firstType, System.Reflection.PropertyInfo statusProperty)
    {
        // Kept for backwards compatibility with any existing callers. The main UI now uses ShowBulkStatusSelectorPanel.
        ShowBulkStatusSelectorPanel();
    }

    private void ApplyBulkStatusFromToolPanel(List<object> selected, Type firstType, System.Reflection.PropertyInfo statusProperty, object? selectedStatus)
    {
        if (selectedStatus == null)
        {
            MessageBox.Show("Choose a status before applying.", "Bulk Status Update", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (MessageBox.Show($"Update {selected.Count} selected {CurrentSection} record(s) to status '{selectedStatus}'?", "Confirm Bulk Update", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;

        try
        {
            using var db = new AppDbContext();
            var updated = 0;
            foreach (var row in selected)
            {
                if (row is not BaseEntity entity) continue;
                var tracked = db.Find(firstType, entity.Id);
                if (tracked == null) continue;
                statusProperty.SetValue(tracked, selectedStatus);
                updated++;
            }
            db.SaveChanges();
            RefreshCurrentSection();
            StatusText.Text = $"Updated {updated} record(s) to {selectedStatus}.";
            ShowToolMessagePanel("Bulk Status Update Complete", $"Updated {updated} selected record(s) to {selectedStatus}.", "Return to the record section to review the updated statuses, or select another group and run the tool again.");
        }
        catch (Exception ex)
        {
            ErrorLogService.Log(ex, "Bulk status update");
            MessageBox.Show($"Could not complete the bulk status update.\n\n{ex.Message}", "Bulk Status Update", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BulkAddSelectedToMarket_Click(object sender, RoutedEventArgs e)
    {
        ShowBulkAddToMarketSelectorPanel();
    }

    private void ShowBulkAddToMarketSelectorPanel()
    {
        PrepareInteractiveToolPanel("Bulk Add Stock To Market", "Choose the jewellery stock items and the destination market from dropdown/list selectors, then apply the change.");
        ToolInputPanel.Children.Add(CreateInfoCard("Select Stock Here", "Use this Setup page to choose exactly which jewellery items should go to a market. You can still preselect rows in Jewellery Stock first, but it is no longer required."));

        try
        {
            using var db = new AppDbContext();
            var marketOptions = db.MarketEvents.AsNoTracking()
                .OrderBy(m => m.EventDate < DateTime.Today ? 1 : 0)
                .ThenBy(m => m.EventDate)
                .ToList();
            var stockOptions = db.JewelleryItems.AsNoTracking()
                .Where(j => j.Status != StockStatus.Sold)
                .OrderBy(j => j.StockCode)
                .ThenBy(j => j.Name)
                .Cast<object>()
                .Select(entity => new EntitySelectionOption(GetEntityDisplayText(entity), entity))
                .ToList();

            if (marketOptions.Count == 0)
            {
                ToolInputPanel.Children.Add(CreateInfoCard("No Market Events", "Create a Market Event first, then return to this tool to add selected jewellery stock."));
                return;
            }

            ToolInputPanel.Children.Add(CreateLabel("Target Market Event"));
            var marketCombo = new ComboBox
            {
                ItemsSource = marketOptions,
                SelectedItem = marketOptions.FirstOrDefault(m => m.EventDate.Date >= DateTime.Today) ?? marketOptions.FirstOrDefault(),
                MinHeight = 38,
                Margin = new Thickness(0, 6, 0, 12)
            };
            ToolInputPanel.Children.Add(marketCombo);

            ToolInputPanel.Children.Add(CreateLabel("Jewellery Stock To Add"));
            var stockList = CreateRecordSelectionListBox();
            stockList.ItemsSource = stockOptions;
            ToolInputPanel.Children.Add(stockList);

            PreselectMatchingOptions(stockList, stockOptions, GetSelectedRecordObjects());

            var countText = new TextBlock
            {
                Text = $"{stockList.SelectedItems.Count} selected. Use Ctrl/Shift-click to choose multiple stock items.",
                Foreground = (System.Windows.Media.Brush)FindResource("MutedTextBrush"),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 14)
            };
            ToolInputPanel.Children.Add(countText);
            stockList.SelectionChanged += (_, _) => countText.Text = $"{stockList.SelectedItems.Count} selected. Choose a market, then apply the update.";

            var applyButton = new WpfButton
            {
                Content = "Add Selected Stock To Market",
                Style = (Style)FindResource("AccentButtonStyle"),
                MinHeight = 42,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                ToolTip = "Create market stock records and set selected jewellery to At Market."
            };
            applyButton.Click += (_, _) =>
            {
                var selectedJewellery = stockList.SelectedItems.Cast<EntitySelectionOption>()
                    .Select(o => o.Entity)
                    .OfType<JewelleryItem>()
                    .ToList();
                ApplyBulkAddToMarketFromToolPanel(selectedJewellery, marketCombo.SelectedItem as MarketEvent);
            };
            ToolInputPanel.Children.Add(applyButton);
        }
        catch (Exception ex)
        {
            ErrorLogService.Log(ex, "Prepare in-workspace bulk add selected jewellery to market");
            MessageBox.Show($"Could not prepare the market stock selector.\n\n{ex.Message}", "Bulk Add To Market", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ShowBulkAddToMarketToolPanel(List<JewelleryItem> selectedJewellery, List<MarketEvent> markets)
    {
        // Kept for backwards compatibility with any existing callers. The main UI now uses ShowBulkAddToMarketSelectorPanel.
        ShowBulkAddToMarketSelectorPanel();
    }

    private void ApplyBulkAddToMarketFromToolPanel(List<JewelleryItem> selectedJewellery, MarketEvent? selectedMarket)
    {
        if (selectedMarket == null)
        {
            MessageBox.Show("Choose a market event before applying.", "Bulk Add To Market", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (MessageBox.Show($"Add {selectedJewellery.Count} selected stock item(s) to market '{selectedMarket.Name}' on {selectedMarket.EventDate:d}?", "Bulk Add To Market", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;

        try
        {
            using var db = new AppDbContext();
            var market = db.MarketEvents.Find(selectedMarket.Id);
            if (market == null)
            {
                MessageBox.Show("The selected market event could not be found. Refresh and try again.", "Bulk Add To Market", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var added = 0;
            foreach (var item in selectedJewellery)
            {
                var trackedItem = db.JewelleryItems.Find(item.Id);
                if (trackedItem == null) continue;
                var exists = db.MarketStocks.Any(ms => ms.MarketEventId == market.Id && ms.JewelleryItemId == trackedItem.Id);
                if (!exists)
                {
                    db.MarketStocks.Add(new MarketStock
                    {
                        MarketEventId = market.Id,
                        JewelleryItemId = trackedItem.Id,
                        Packed = false,
                        ReturnedToStock = false,
                        SoldAtMarket = false,
                        Notes = "Added by bulk cleanup/action tool."
                    });
                    added++;
                }
                trackedItem.Status = StockStatus.AtMarket;
            }
            db.SaveChanges();
            RefreshCurrentSection();
            StatusText.Text = $"Added {added} item(s) to {market.Name}.";
            ShowToolMessagePanel("Bulk Add To Market Complete", $"Added {added} item(s) to {market.Name}. Existing market links were skipped. Selected jewellery statuses were set to At Market.", "Open Market Stock or Market Studio to pack, sell, or reconcile these items.");
        }
        catch (Exception ex)
        {
            ErrorLogService.Log(ex, "Bulk add selected jewellery to market");
            MessageBox.Show($"Could not bulk add stock to market.\n\n{ex.Message}", "Bulk Add To Market", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CreateCleanupTasks_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var created = DataCleanupService.CreateCleanupTasks();
            RefreshCurrentSection();
            MessageBox.Show(created == 0
                ? "No new cleanup tasks were needed. Existing matching tasks were left unchanged."
                : $"Created {created} cleanup/follow-up task(s). Open Tasks to review them.",
                "Create Cleanup Tasks", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            ErrorLogService.Log(ex, "Create cleanup tasks");
            MessageBox.Show($"Could not create cleanup tasks.\n\n{ex.Message}", "Create Cleanup Tasks", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Backup_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var path = BackupService.CreateBackup();
            MessageBox.Show($"Backup created:\n{path}", "Backup", MessageBoxButton.OK, MessageBoxImage.Information);
            if (CurrentSection == "Dashboard")
                LoadDashboard();
        }
        catch (Exception ex)
        {
            ErrorLogService.Log(ex, "Create database backup");
            MessageBox.Show($"Could not create the database backup.\n\n{ex.Message}", "Backup error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void DymoMiniLabel_Click(object sender, RoutedEventArgs e)
    {
        var window = new DymoMiniLabelWindow(RecordsGrid.SelectedItem) { Owner = this };
        window.ShowDialog();
    }

    private void DeviceCapture_Click(object sender, RoutedEventArgs e)
    {
        var selected = RecordsGrid.SelectedItem;
        if (selected is null)
        {
            MessageBox.Show("For best results, select a Material or Stone before opening Camera & Scale Capture. You can still open the tool and use manual capture guidance.", "Device Capture", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        var window = new DeviceCaptureWindow(selected) { Owner = this };
        window.ShowDialog();
        RefreshCurrentSection();
    }

    private void MarketOperationsWindow_Click(object sender, RoutedEventArgs e)
    {
        var window = new MarketOperationsWindow { Owner = this };
        window.Show();
    }

    private void DeviceSetupNotes_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var folder = BusinessSettingsService.GetPrintoutFolder();
            var path = Path.Combine(folder, $"device-setup-notes-{DateTime.Now:yyyyMMdd-HHmmss}.html");
            var html = """
<!doctype html>
<html><head><meta charset='utf-8'><title>Device Setup Notes</title>
<style>
body{font-family:Segoe UI,Arial,sans-serif;margin:32px;color:#111827;line-height:1.45} h1{color:#111827} h2{margin-top:28px;color:#7c5a13} .card{border:1px solid #ddd;border-radius:12px;padding:16px;margin:14px 0;background:#fafafa} code{background:#f3f4f6;padding:2px 5px;border-radius:4px}
</style></head><body>
<h1>OPALNOVA — Device Setup Notes</h1>
<div class='card'><h2>DYMO / mini label printing</h2><p>Install DYMO Connect or the printer driver first. Use Hardware & POS Studio → DYMO Mini Label, then choose the DYMO printer in the Windows print dialog. The app uses Windows printing so it can also print to other small label printers.</p></div>
<div class='card'><h2>Precision scales</h2><p>Serial/COM scales can be read through Hardware & POS Studio → Camera & Scale Capture. Choose the COM port and baud rate, then press the scale send/print button. Keyboard-wedge scales can type directly into the reading box. HID-only scales may need manufacturer software or wedge software to expose a keyboard/serial reading.</p></div>
<div class='card'><h2>USB camera workflow</h2><p>Use Open Windows Camera to capture a photo, then Import Latest Camera Roll Photo. You can also import a photo file directly and link it to the selected stock, stone, material, opal parcel or job.</p></div>
<div class='card'><h2>Market multi-display mode</h2><p>Use Hardware & POS Studio → Market Operations Window. Keep that window on the operator screen, then open Customer Display on the second monitor. The main app can remain open for inventory or workshop operations separately.</p></div>
</body></html>
""";
            File.WriteAllText(path, html);
            OpenReportInApp(path, "Device Setup Notes");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not create device setup notes.\n\n{ex.Message}", "Device Setup", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

}
