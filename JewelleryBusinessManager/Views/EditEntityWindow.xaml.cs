using System.Globalization;
using System.Reflection;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using WpfComboBox = System.Windows.Controls.ComboBox;
using WpfTextBox = System.Windows.Controls.TextBox;
using WpfImage = System.Windows.Controls.Image;
using WpfPanel = System.Windows.Controls.Panel;
using WpfButton = System.Windows.Controls.Button;
using WpfCheckBox = System.Windows.Controls.CheckBox;
using System.Windows.Media.Imaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using JewelleryBusinessManager.Data;
using JewelleryBusinessManager.Models;
using JewelleryBusinessManager.Services;
using MessageBox = System.Windows.MessageBox;
using MessageBoxButton = System.Windows.MessageBoxButton;
using MessageBoxImage = System.Windows.MessageBoxImage;
using MessageBoxResult = System.Windows.MessageBoxResult;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace JewelleryBusinessManager.Views;

public partial class EditEntityWindow : Window
{
    private readonly object _entity;
    private readonly Dictionary<PropertyInfo, FrameworkElement> _controls = new();

    public bool IsHostedInTab { get; set; }
    public event EventHandler? Saved;
    public event EventHandler? Cancelled;

    public EditEntityWindow(object entity)
    {
        InitializeComponent();
        _entity = entity;
        Title = $"Edit {entity.GetType().Name}";
        BuildForm();
    }

    private void BuildForm()
    {
        FormGrid.ColumnDefinitions.Clear();
        FormGrid.RowDefinitions.Clear();
        FormGrid.Children.Clear();

        // Compact two-column editor layout:
        // [label][input]  gap  [label][input]
        // Long text/file fields still span the full width so notes remain readable.
        FormGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
        FormGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        FormGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
        FormGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
        FormGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var props = _entity.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite && p.Name is not "Id" and not "CreatedAt" and not "UpdatedAt")
            .Where(p => !p.GetGetMethod()!.IsVirtual)
            .OrderBy(GetPropertyDisplayOrder)
            .ThenBy(p => p.Name)
            .ToList();

        var helperText = GetWorkflowHelperText(_entity.GetType());
        var currentRow = 0;
        var fieldSlot = 0; // 0 = left side of row, 1 = right side of row already expected
        if (!string.IsNullOrWhiteSpace(helperText))
        {
            FormGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            var helper = new Border
            {
                BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(51, 65, 85)),
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(23, 32, 51)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(6),
                Margin = new Thickness(3, 0, 3, 5),
                Child = new TextBlock
                {
                    Text = helperText,
                    TextWrapping = TextWrapping.Wrap,
                    FontSize = 11,
                    FontStyle = FontStyles.Italic,
                    Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(216, 206, 190))
                }
            };
            Grid.SetRow(helper, currentRow);
            Grid.SetColumn(helper, 0);
            Grid.SetColumnSpan(helper, 5);
            FormGrid.Children.Add(helper);
            currentRow++;
        }

        string? lastGroup = null;
        foreach (var prop in props)
        {
            var groupName = GetPropertyGroupName(_entity.GetType(), prop.Name);
            if (!string.Equals(groupName, lastGroup, StringComparison.Ordinal))
            {
                if (fieldSlot != 0)
                {
                    currentRow++;
                    fieldSlot = 0;
                }

                AddGroupHeader(groupName, currentRow);
                currentRow++;
                lastGroup = groupName;
            }

            var control = CreateControl(prop);
            AddEditorField(prop, control, IsFullWidthEditorField(prop), ref currentRow, ref fieldSlot);
            _controls[prop] = control;
        }

        if (fieldSlot != 0)
        {
            currentRow++;
            fieldSlot = 0;
        }

        AddJobPaymentHistoryPanel(currentRow);
    }

    private void AddEditorField(PropertyInfo prop, FrameworkElement control, bool fullWidth, ref int currentRow, ref int fieldSlot)
    {
        var label = new TextBlock
        {
            Text = GetEditorLabel(prop.Name),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(2, 2, 5, 2),
            FontSize = 11,
            FontWeight = FontWeights.SemiBold,
            TextTrimming = TextTrimming.CharacterEllipsis
        };

        if (fullWidth)
        {
            if (fieldSlot != 0)
            {
                currentRow++;
                fieldSlot = 0;
            }

            FormGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            Grid.SetRow(label, currentRow);
            Grid.SetColumn(label, 0);
            FormGrid.Children.Add(label);

            Grid.SetRow(control, currentRow);
            Grid.SetColumn(control, 1);
            Grid.SetColumnSpan(control, 4);
            FormGrid.Children.Add(control);
            currentRow++;
            return;
        }

        if (fieldSlot == 0)
        {
            FormGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            Grid.SetRow(label, currentRow);
            Grid.SetColumn(label, 0);
            FormGrid.Children.Add(label);

            Grid.SetRow(control, currentRow);
            Grid.SetColumn(control, 1);
            FormGrid.Children.Add(control);
            fieldSlot = 1;
        }
        else
        {
            Grid.SetRow(label, currentRow);
            Grid.SetColumn(label, 3);
            FormGrid.Children.Add(label);

            Grid.SetRow(control, currentRow);
            Grid.SetColumn(control, 4);
            FormGrid.Children.Add(control);
            currentRow++;
            fieldSlot = 0;
        }
    }

    private static bool IsFullWidthEditorField(PropertyInfo prop)
    {
        var name = prop.Name;
        if (name.Equals("FilePath", StringComparison.OrdinalIgnoreCase))
            return true;

        return name.Contains("Notes", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Address", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Description", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Terms", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Caption", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Hashtags", StringComparison.OrdinalIgnoreCase)
            || name.EndsWith("Url", StringComparison.OrdinalIgnoreCase)
            || name.EndsWith("Path", StringComparison.OrdinalIgnoreCase);
    }

    private void AddGroupHeader(string groupName, int row)
    {
        FormGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        var header = new TextBlock
        {
            Text = groupName,
            FontWeight = FontWeights.SemiBold,
            FontSize = 12,
            Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(184, 138, 35)),
            Margin = new Thickness(2, 4, 2, 1)
        };
        Grid.SetRow(header, row);
        Grid.SetColumn(header, 0);
        Grid.SetColumnSpan(header, 5);
        FormGrid.Children.Add(header);
    }

    private static int GetPropertyDisplayOrder(PropertyInfo prop)
    {
        var name = prop.Name;
        return name switch
        {
            "JobCode" or "StockCode" or "StoneCode" or "MaterialCode" or "ParcelCode" or "BatchCode" or "TaskCode" => 0,
            "FullName" or "Name" or "JobTitle" or "StoneType" or "Title" => 1,
            "CustomerId" or "SupplierId" or "OpalParcelId" or "MainStoneId" or "JewelleryItemId" or "JobId" or "MarketEventId" or "MaterialId" or "ProductionBatchId" or "StoneId" or "PurchaseOrderId" => 2,
            "Type" or "Category" or "Status" => 3,
            "DateReceived" or "DueDate" or "ReminderDate" or "CompletedAt" or "PurchaseDate" or "DateMade" or "SaleDate" or "PaymentDate" or "TransactionDate" or "EventDate" or "StartDate" or "TargetCompletionDate" => 4,
            "RetailPrice" or "WholesalePrice" or "QuoteAmount" or "DepositPaid" or "BalanceOwing" or "FinalPrice" or "SaleAmount" or "CostOfGoods" => 5,
            "MaterialCost" or "LabourHours" or "LabourRate" or "LabourCost" or "OtherCost" or "PurchaseCost" or "CurrentQuantity" or "ReorderLevel" or "EstimatedMaterialCost" or "EstimatedLabourHours" or "EstimatedCost" => 6,
            "BodyTone" or "Brightness" or "Pattern" or "WeightCarats" or "Dimensions" or "Shape" => 7,
            _ when name.Contains("Notes", StringComparison.OrdinalIgnoreCase) => 99,
            _ => 50
        };
    }


    private static string GetPropertyGroupName(Type entityType, string propertyName)
    {
        if (entityType == typeof(JewelleryItem))
        {
            return propertyName switch
            {
                "StockCode" or "Name" or "Type" or "Status" or "DateMade" => "Item Details",
                "MainStoneId" or "Metal" or "RingSize" or "ChainLength" or "Dimensions" => "Materials, Stone & Sizing",
                "MaterialCost" or "LabourHours" or "LabourRate" or "OtherCost" or "RetailPrice" or "WholesalePrice" => "Pricing & Costing",
                _ => "Notes"
            };
        }

        if (entityType == typeof(Stone))
        {
            return propertyName switch
            {
                "StoneCode" or "StoneType" or "Status" or "OpalParcelId" => "Stone Identity",
                "WeightCarats" or "Shape" or "Dimensions" => "Size & Shape",
                "BodyTone" or "Brightness" or "Pattern" or "BaseColour" or "MainColours" or "EstimatedValue" => "Opal / Gem Details",
                _ => "Notes"
            };
        }

        if (entityType == typeof(Job))
        {
            return propertyName switch
            {
                "JobCode" or "JobTitle" or "CustomerId" or "Type" or "Status" or "DateReceived" or "DueDate" => "Job Details",
                "QuoteAmount" or "DepositPaid" or "BalanceOwing" or "FinalPrice" => "Quote & Payment",
                "LabourHours" or "LabourCost" or "MaterialCost" => "Costing",
                _ => "Notes & Approval"
            };
        }

        if (entityType == typeof(Material))
        {
            return propertyName switch
            {
                "MaterialCode" or "Name" or "Category" or "SupplierId" => "Material Details",
                "PurchaseCost" or "CurrentQuantity" or "UnitType" or "ReorderLevel" or "StorageLocation" => "Stock Control",
                _ => "Notes"
            };
        }

        if (entityType == typeof(Sale))
        {
            return propertyName switch
            {
                "JewelleryItemId" or "JobId" or "CustomerId" or "SaleDate" => "Sale Link",
                "SaleAmount" or "CostOfGoods" or "PaymentMethod" or "SaleLocation" => "Payment & Profit",
                _ => "Notes"
            };
        }


        if (entityType == typeof(ProductionBatch))
        {
            return propertyName switch
            {
                "BatchCode" or "Name" or "CollectionName" or "Status" => "Batch Details",
                "StartDate" or "TargetCompletionDate" or "MarketEventId" => "Schedule & Market",
                "PlannedPieces" or "CompletedPieces" or "EstimatedMaterialCost" or "EstimatedLabourHours" or "EstimatedRetailValue" => "Targets & Costing",
                _ => "Notes"
            };
        }

        if (entityType == typeof(ProductionBatchItem))
        {
            return propertyName switch
            {
                "ProductionBatchId" or "ItemName" or "ItemType" or "Status" => "Batch Item",
                "PurchaseOrderCode" or "SupplierId" or "OrderDate" or "ExpectedDeliveryDate" or "Status" => "Purchase Order",
                "PurchaseOrderId" or "MaterialId" or "ItemName" or "UnitType" => "Purchase Order Item",
                "JewelleryItemId" or "StoneId" or "JobId" => "Linked Records",
                "PlannedQuantity" or "CompletedQuantity" or "EstimatedCost" or "EstimatedRetailValue" => "Progress & Value",
                _ => "Notes"
            };
        }


        if (entityType == typeof(OnlineListing))
        {
            return propertyName switch
            {
                "JewelleryItemId" or "Platform" or "Status" or "PhotoStatus" or "ListingDate" or "ListingUrl" => "Listing Details",
                "PhotosDone" or "DescriptionDone" or "PriceChecked" or "ListedOnline" or "SharedToSocial" => "Listing Checklist",
                "SeoTitle" or "ShortDescription" or "LongDescription" or "InstagramCaption" or "Hashtags" => "Listing Content",
                _ => "Notes"
            };
        }

        if (entityType == typeof(BusinessTask))
        {
            return propertyName switch
            {
                "TaskCode" or "Title" or "Category" or "Status" or "Priority" => "Task Details",
                "DueDate" or "ReminderDate" or "CompletedAt" or "ShowOnDashboard" => "Schedule & Dashboard",
                "CustomerId" or "JobId" or "JewelleryItemId" or "StoneId" or "MarketEventId" or "ProductionBatchId" or "PurchaseOrderId" => "Linked Records",
                _ => "Notes"
            };
        }

        if (entityType == typeof(MarketEvent) || entityType == typeof(MarketStock))
            return propertyName.Contains("Notes", StringComparison.OrdinalIgnoreCase) ? "Notes" : "Market Details";

        if (entityType == typeof(PhotoRecord))
            return propertyName == "FilePath" ? "Photo File" : "Photo Link";

        return propertyName.Contains("Notes", StringComparison.OrdinalIgnoreCase) ? "Notes" : "Details";
    }

    private static string GetWorkflowHelperText(Type entityType)
    {
        if (entityType == typeof(Job))
            return "Job workflow: create the enquiry, add quote/deposit details, then use the main-window Advance Job button to move it through quoted, approved, in progress, ready and completed. Saved jobs show their partial payment ledger below.";
        if (entityType == typeof(JewelleryItem))
            return "Jewellery stock workflow: link the main stone where relevant, add making costs and retail price, then use Create Sale or Add To Market from the main window.";
        if (entityType == typeof(Stone))
            return "Stone workflow: link to an opal parcel when possible, record body tone/brightness/pattern, then link the stone to a jewellery item when it is set.";
        if (entityType == typeof(MaterialTransaction))
            return "Material transactions record additions or usage. Use positive quantities for received stock and negative quantities for material used in jobs or jewellery pieces.";
        if (entityType == typeof(OnlineListing))
            return "Online listing workflow: link a jewellery item, complete photos/content/price checks, add the platform URL when listed, then use Listing Checklist or Listing Report from the main window.";
        if (entityType == typeof(Sale))
            return "Sale workflow: link to a jewellery item or job. V1.2 can automatically fill cost/sale details when a sale is created using the main-window Create Sale button.";
        if (entityType == typeof(MarketStock))
            return "Market stock links jewellery pieces to a market event. Use Packed as a checklist and Sold At Market after the event.";
        if (entityType == typeof(ProductionBatch))
            return "Production batches are for collections, making runs, market stock builds and website restocks. Set planned/completed pieces, target dates, expected costs and expected retail value.";
        if (entityType == typeof(ProductionBatchItem))
            return "Batch items are the planned or linked pieces inside a production batch. Link jewellery, stones or jobs where possible, then update completed quantity as work progresses.";
        if (entityType == typeof(BusinessTask))
            return "Task workflow: link tasks to customers, jobs, jewellery, stones, markets, batches or purchase orders. Use Complete Task from the main window when finished, and Work Queue for today/overdue priorities.";
        return string.Empty;
    }

    private void AddJobPaymentHistoryPanel(int row)
    {
        if (_entity is not Job job || job.Id <= 0)
            return;

        using var db = new AppDbContext();
        var storedJob = db.Jobs.AsNoTracking().FirstOrDefault(x => x.Id == job.Id) ?? job;
        var customer = storedJob.CustomerId.HasValue
            ? db.Customers.AsNoTracking().FirstOrDefault(x => x.Id == storedJob.CustomerId.Value)
            : null;
        var payments = db.Payments.AsNoTracking()
            .Where(x => x.JobId == storedJob.Id)
            .OrderByDescending(x => x.PaymentDate)
            .ThenByDescending(x => x.Id)
            .ToList();
        var hasSale = db.Sales.AsNoTracking().Any(x => x.JobId == storedJob.Id);
        var total = storedJob.FinalPrice > 0 ? storedJob.FinalPrice : storedJob.QuoteAmount;
        var recordedPaymentTotal = payments.Sum(x => x.Amount);
        var paid = Math.Max(storedJob.DepositPaid, recordedPaymentTotal);
        var balance = Math.Max(0m, total - paid);

        FormGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        var panel = new Border
        {
            Background = Brush(13, 20, 34),
            BorderBrush = Brush(51, 65, 85),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(10),
            Margin = new Thickness(2, 10, 2, 2)
        };

        var stack = new StackPanel();
        panel.Child = stack;

        var title = new TextBlock
        {
            Text = "Job Payment History",
            FontSize = 13,
            FontWeight = FontWeights.SemiBold,
            Foreground = Brush(184, 138, 46),
            Margin = new Thickness(0, 0, 0, 3)
        };
        stack.Children.Add(title);

        var subtitle = new TextBlock
        {
            Text = $"{storedJob.JobCode} {storedJob.JobTitle}".Trim() + $" | Customer: {customer?.FullName ?? "Not linked"} | Sale created: {(hasSale ? "Yes" : "No")}",
            FontSize = 11,
            Foreground = Brush(216, 206, 190),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 8)
        };
        stack.Children.Add(subtitle);

        var summary = new UniformGrid
        {
            Columns = 4,
            Margin = new Thickness(0, 0, 0, 8)
        };
        summary.Children.Add(CreatePaymentSummaryTile("Total", total.ToString("C", CultureInfo.CurrentCulture)));
        summary.Children.Add(CreatePaymentSummaryTile("Paid", paid.ToString("C", CultureInfo.CurrentCulture)));
        summary.Children.Add(CreatePaymentSummaryTile("Balance", balance.ToString("C", CultureInfo.CurrentCulture)));
        summary.Children.Add(CreatePaymentSummaryTile("Payments", payments.Count.ToString(CultureInfo.CurrentCulture)));
        stack.Children.Add(summary);

        if (payments.Count == 0)
        {
            stack.Children.Add(new TextBlock
            {
                Text = storedJob.DepositPaid > 0
                    ? $"No ledger rows yet. Deposit Paid field currently records {storedJob.DepositPaid.ToString("C", CultureInfo.CurrentCulture)}."
                    : "No payment ledger rows have been recorded for this job yet.",
                Foreground = Brush(216, 206, 190),
                FontSize = 11,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 2, 0, 0)
            });
        }
        else
        {
            stack.Children.Add(CreatePaymentHistoryGrid(payments));
        }

        Grid.SetRow(panel, row);
        Grid.SetColumn(panel, 0);
        Grid.SetColumnSpan(panel, 5);
        FormGrid.Children.Add(panel);
    }

    private static Border CreatePaymentSummaryTile(string label, string value)
    {
        var stack = new StackPanel();
        stack.Children.Add(new TextBlock
        {
            Text = label,
            Foreground = Brush(216, 206, 190),
            FontSize = 10
        });
        stack.Children.Add(new TextBlock
        {
            Text = value,
            Foreground = Brush(239, 234, 224),
            FontSize = 15,
            FontWeight = FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap
        });

        return new Border
        {
            Background = Brush(31, 41, 55),
            BorderBrush = Brush(51, 65, 85),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(8),
            Margin = new Thickness(0, 0, 6, 0),
            Child = stack
        };
    }

    private static DataGrid CreatePaymentHistoryGrid(IReadOnlyCollection<Payment> payments)
    {
        var rows = payments
            .Select(x => new JobPaymentHistoryRow(
                x.PaymentDate.ToString("d", CultureInfo.CurrentCulture),
                x.Amount.ToString("C", CultureInfo.CurrentCulture),
                x.Method.ToString(),
                x.Reference ?? string.Empty,
                x.Notes ?? string.Empty))
            .ToList();

        return new DataGrid
        {
            ItemsSource = rows,
            AutoGenerateColumns = false,
            IsReadOnly = true,
            CanUserAddRows = false,
            CanUserDeleteRows = false,
            Height = 150,
            Background = Brush(4, 8, 19),
            Foreground = Brush(239, 234, 224),
            BorderBrush = Brush(51, 65, 85),
            GridLinesVisibility = DataGridGridLinesVisibility.Horizontal,
            HeadersVisibility = DataGridHeadersVisibility.Column,
            RowBackground = Brush(4, 8, 19),
            AlternatingRowBackground = Brush(13, 20, 34),
            HorizontalGridLinesBrush = Brush(51, 65, 85),
            Columns =
            {
                new DataGridTextColumn { Header = "Date", Binding = new Binding(nameof(JobPaymentHistoryRow.DateText)), Width = new DataGridLength(95) },
                new DataGridTextColumn { Header = "Amount", Binding = new Binding(nameof(JobPaymentHistoryRow.AmountText)), Width = new DataGridLength(105) },
                new DataGridTextColumn { Header = "Method", Binding = new Binding(nameof(JobPaymentHistoryRow.MethodText)), Width = new DataGridLength(110) },
                new DataGridTextColumn { Header = "Reference", Binding = new Binding(nameof(JobPaymentHistoryRow.Reference)), Width = new DataGridLength(150) },
                new DataGridTextColumn { Header = "Notes", Binding = new Binding(nameof(JobPaymentHistoryRow.Notes)), Width = new DataGridLength(1, DataGridLengthUnitType.Star) }
            }
        };
    }

    private static SolidColorBrush Brush(byte r, byte g, byte b) => new(Color.FromRgb(r, g, b));

    private FrameworkElement CreateControl(PropertyInfo prop)
    {
        var type = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
        var value = prop.GetValue(_entity);

        if (TryCreateLookupComboBox(prop, value, out var lookupComboBox))
            return lookupComboBox;

        if (type.IsEnum)
        {
            var cb = new WpfComboBox { ItemsSource = Enum.GetValues(type), SelectedItem = value ?? Enum.GetValues(type).GetValue(0), Margin = new Thickness(2) };
            return cb;
        }

        if (type == typeof(bool))
        {
            return new WpfCheckBox { IsChecked = value as bool? ?? false, Margin = new Thickness(2) };
        }

        if (type == typeof(DateTime))
        {
            return new DatePicker { SelectedDate = value as DateTime?, Margin = new Thickness(2) };
        }

        if (prop.Name.Equals("FilePath", StringComparison.OrdinalIgnoreCase))
        {
            return CreateFilePicker(value?.ToString() ?? string.Empty);
        }

        if (prop.Name.Contains("Notes", StringComparison.OrdinalIgnoreCase) || prop.Name.Contains("Address", StringComparison.OrdinalIgnoreCase))
        {
            return new WpfTextBox { Text = value?.ToString() ?? string.Empty, AcceptsReturn = true, Height = 48, TextWrapping = TextWrapping.Wrap, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Margin = new Thickness(2) };
        }

        return new WpfTextBox { Text = value?.ToString() ?? string.Empty, Margin = new Thickness(2) };
    }

    private static StackPanel CreateFilePicker(string initialPath)
    {
        var panel = new StackPanel { Margin = new Thickness(2) };
        var row = new DockPanel();
        var browseButton = new WpfButton { Content = "Browse...", MinWidth = 74, Padding = new Thickness(9, 5, 9, 5), Margin = new Thickness(3, 0, 0, 0), MinHeight = 34, VerticalAlignment = VerticalAlignment.Center };
        DockPanel.SetDock(browseButton, Dock.Right);
        var pathBox = new WpfTextBox { Text = initialPath };
        row.Children.Add(browseButton);
        row.Children.Add(pathBox);

        var preview = new WpfImage { MaxHeight = 140, Stretch = System.Windows.Media.Stretch.Uniform, Margin = new Thickness(0, 8, 0, 0) };
        UpdatePreview(preview, initialPath);

        browseButton.Click += (_, _) =>
        {
            var dialog = new OpenFileDialog
            {
                Title = "Choose image file",
                Filter = "Image files|*.jpg;*.jpeg;*.png;*.bmp;*.gif;*.webp|All files|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                pathBox.Text = dialog.FileName;
                UpdatePreview(preview, dialog.FileName);
            }
        };

        pathBox.TextChanged += (_, _) => UpdatePreview(preview, pathBox.Text);
        panel.Children.Add(row);
        panel.Children.Add(preview);
        return panel;
    }

    private static void UpdatePreview(WpfImage image, string path)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path) || !PhotoStorageService.LooksLikeImage(path) || !System.IO.File.Exists(path))
            {
                image.Source = null;
                return;
            }

            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.UriSource = new Uri(path, UriKind.Absolute);
            bitmap.EndInit();
            image.Source = bitmap;
        }
        catch
        {
            image.Source = null;
        }
    }

    private static bool TryCreateLookupComboBox(PropertyInfo prop, object? value, out WpfComboBox comboBox)
    {
        comboBox = new WpfComboBox
        {
            Margin = new Thickness(2),
            MinWidth = 180,
            IsTextSearchEnabled = true,
            Tag = "Lookup",
            ToolTip = "This is a dropdown selector. Choose an existing linked record from the list."
        };

        if (!IsLookupProperty(prop.Name))
            return false;

        var lookupOptions = GetLookupOptions(prop.Name, prop.PropertyType);
        comboBox.ItemsSource = lookupOptions;
        comboBox.DisplayMemberPath = nameof(LookupOption.Label);
        comboBox.SelectedValuePath = nameof(LookupOption.Id);
        comboBox.SelectedValue = value;

        if (comboBox.SelectedIndex < 0 && lookupOptions.Count > 0)
            comboBox.SelectedIndex = 0;

        return true;
    }

    private static bool IsLookupProperty(string propertyName) => propertyName switch
    {
        "CustomerId" or "SupplierId" or "MaterialId" or "OpalParcelId" or
        "MainStoneId" or "JobId" or "JewelleryItemId" or "SaleId" or
        "MarketEventId" or "ProductionBatchId" or "StoneId" or "PurchaseOrderId" => true,
        _ => false
    };

    private static List<LookupOption> GetLookupOptions(string propertyName, Type propertyType)
    {
        var options = propertyName switch
        {
            "CustomerId" => QueryOptions<Customer>(),
            "SupplierId" => QueryOptions<Supplier>(),
            "MaterialId" => QueryOptions<Material>(),
            "OpalParcelId" => QueryOptions<OpalParcel>(),
            "MainStoneId" => QueryOptions<Stone>(),
            "JobId" => QueryOptions<Job>(),
            "JewelleryItemId" => QueryOptions<JewelleryItem>(),
            "SaleId" => QueryOptions<Sale>(),
            "MarketEventId" => QueryOptions<MarketEvent>(),
            "ProductionBatchId" => QueryOptions<ProductionBatch>(),
            "StoneId" => QueryOptions<Stone>(),
            "PurchaseOrderId" => QueryOptions<PurchaseOrder>(),
            _ => []
        };

        options.Insert(0, new LookupOption(null, $"Select {LookupFriendlyName(propertyName)}"));
        return options;
    }

    private static string LookupFriendlyName(string propertyName) => propertyName switch
    {
        "CustomerId" => "customer",
        "SupplierId" => "supplier",
        "MaterialId" => "material",
        "OpalParcelId" => "opal parcel",
        "MainStoneId" => "main stone",
        "StoneId" => "stone",
        "JobId" => "job",
        "JewelleryItemId" => "jewellery item",
        "SaleId" => "sale",
        "MarketEventId" => "market event",
        "ProductionBatchId" => "production batch",
        "PurchaseOrderId" => "purchase order",
        _ => SplitName(propertyName).Replace(" Id", string.Empty).ToLowerInvariant()
    };

    private static string GetEditorLabel(string propertyName)
    {
        if (!IsLookupProperty(propertyName))
            return SplitName(propertyName);

        var friendlyName = LookupFriendlyName(propertyName);
        return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(friendlyName);
    }

    private static List<LookupOption> QueryOptions<T>() where T : BaseEntity
    {
        using var db = new AppDbContext();
        return db.Set<T>().AsEnumerable()
            .OrderBy(x => x.ToString())
            .Select(x => new LookupOption(x.Id, x.ToString() ?? $"#{x.Id}"))
            .ToList();
    }

    public bool TryApplyChanges()
    {
        try
        {
            foreach (var (prop, control) in _controls)
            {
                var value = ConvertControlValue(prop, control);
                prop.SetValue(_entity, value);
            }
            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not save the record. Check numeric/date fields.\n\n{ex.Message}", "Save error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (!TryApplyChanges()) return;

        if (IsHostedInTab)
        {
            Saved?.Invoke(this, EventArgs.Empty);
            return;
        }

        DialogResult = true;
        Close();
    }

    private static object? ConvertControlValue(PropertyInfo prop, FrameworkElement control)
    {
        var nullableType = Nullable.GetUnderlyingType(prop.PropertyType);
        var targetType = nullableType ?? prop.PropertyType;
        var isNullable = nullableType != null || !targetType.IsValueType;

        if (control is StackPanel panel && prop.Name.Equals("FilePath", StringComparison.OrdinalIgnoreCase))
        {
            var textBox = FindChildTextBox(panel);
            return textBox?.Text ?? string.Empty;
        }

        if (control is WpfTextBox tb)
        {
            if (targetType == typeof(string))
                return tb.Text;

            if (string.IsNullOrWhiteSpace(tb.Text))
                return isNullable ? null : Activator.CreateInstance(targetType);

            if (targetType == typeof(int))
                return int.Parse(tb.Text, CultureInfo.InvariantCulture);
            if (targetType == typeof(decimal))
                return decimal.Parse(tb.Text, CultureInfo.InvariantCulture);

            return Convert.ChangeType(tb.Text, targetType, CultureInfo.InvariantCulture);
        }

        if (control is WpfComboBox cb)
        {
            if (targetType == typeof(int))
            {
                if (cb.SelectedValue is int selectedId)
                    return selectedId;

                if (isNullable)
                    return null;

                throw new InvalidOperationException($"Please choose a value for {SplitName(prop.Name)}. Add the linked record first if the dropdown is empty.");
            }
            return cb.SelectedItem;
        }

        if (control is WpfCheckBox chk)
            return chk.IsChecked ?? false;

        if (control is DatePicker dp)
        {
            if (dp.SelectedDate.HasValue)
                return dp.SelectedDate.Value;
            return isNullable ? null : DateTime.Today;
        }

        return null;
    }

    private static WpfTextBox? FindChildTextBox(WpfPanel panel)
    {
        foreach (var child in panel.Children)
        {
            if (child is WpfTextBox textBox)
                return textBox;
            if (child is WpfPanel childPanel)
            {
                var result = FindChildTextBox(childPanel);
                if (result != null) return result;
            }
        }
        return null;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        if (IsHostedInTab)
        {
            Cancelled?.Invoke(this, EventArgs.Empty);
            return;
        }

        DialogResult = false;
        Close();
    }

    private static string SplitName(string name)
    {
        return System.Text.RegularExpressions.Regex.Replace(name, "([a-z])([A-Z])", "$1 $2");
    }

    private sealed record LookupOption(int? Id, string Label)
    {
        public override string ToString() => string.IsNullOrWhiteSpace(Label) ? "Select" : Label;
    }

    private sealed record JobPaymentHistoryRow(string DateText, string AmountText, string MethodText, string Reference, string Notes);
}
