using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using Microsoft.EntityFrameworkCore;
using JewelleryBusinessManager.Data;
using JewelleryBusinessManager.Models;
using JewelleryBusinessManager.Services;

namespace JewelleryBusinessManager.Views;

public partial class CustomQuoteBuilderWindow : Window
{
    private CustomQuote _quote = new();
    private readonly List<QuoteOption> _options = new();
    private readonly Dictionary<QuoteOption, List<QuoteOptionStoneLink>> _stoneLinks = new();
    private readonly Dictionary<QuoteOption, List<QuoteOptionMaterialLink>> _materialLinks = new();
    private readonly Dictionary<QuoteOption, List<QuoteOptionExternalDiamondLink>> _externalDiamondLinks = new();
    private List<Stone> _stones = new();
    private List<Material> _materials = new();
    private List<ExternalDiamond> _externalDiamonds = new();
    private bool _loading;
    private bool _refreshingComparison;

    public CustomQuoteBuilderWindow(int? initialQuoteId = null)
    {
        InitializeComponent();
        LoadReferenceData();
        if (initialQuoteId.HasValue && LoadQuote(initialQuoteId.Value))
            return;
        StartNewQuote();
    }

    private void LoadReferenceData()
    {
        using var db = new AppDbContext();

        var customers = new List<Customer> { new Customer { Id = 0, FullName = "Select customer" } };
        customers.AddRange(db.Customers.AsNoTracking().OrderBy(x => x.FullName).ToList());
        CustomerCombo.ItemsSource = customers;

        var quotes = new List<CustomQuote> { new CustomQuote { Id = 0, QuoteCode = "Select existing quote" } };
        quotes.AddRange(db.CustomQuotes.AsNoTracking().OrderByDescending(x => x.UpdatedAt).ToList());
        QuoteCombo.ItemsSource = quotes;
        if (QuoteCombo.SelectedIndex < 0) QuoteCombo.SelectedIndex = 0;

        _stones = new List<Stone> { new Stone { Id = 0, StoneCode = "Select stone", StoneType = string.Empty } };
        _stones.AddRange(db.Stones.AsNoTracking().OrderBy(x => x.StoneCode).ThenBy(x => x.StoneType).ToList());
        _materials = new List<Material> { new Material { Id = 0, Name = "Select material" } };
        _materials.AddRange(db.Materials.AsNoTracking().OrderBy(x => x.MaterialCode).ThenBy(x => x.Name).ToList());
        _externalDiamonds = new List<ExternalDiamond> { new ExternalDiamond { Id = 0, Status = "Select external diamond", Notes = "Placeholder" } };
        _externalDiamonds.AddRange(db.ExternalDiamonds.AsNoTracking().OrderByDescending(x => x.UpdatedAt).ThenBy(x => x.CertificateNumber).ToList());
        StoneCombo.ItemsSource = _stones;
        MaterialCombo.ItemsSource = _materials;
        ExternalDiamondCombo.ItemsSource = _externalDiamonds;
        if (StoneCombo.SelectedIndex < 0) StoneCombo.SelectedIndex = 0;
        if (MaterialCombo.SelectedIndex < 0) MaterialCombo.SelectedIndex = 0;
        if (ExternalDiamondCombo.SelectedIndex < 0) ExternalDiamondCombo.SelectedIndex = 0;
        ExternalDiamondStatusCombo.ItemsSource = new[] { "Proposed", "Customer Interested", "Hold Requested", "Hold Confirmed", "Hold Expiring", "Order Requested", "Ordered", "Received", "Declined", "Released", "Expired" };
        ExternalDiamondStatusCombo.SelectedItem = "Proposed";
    }

    private void StartNewQuote()
    {
        _loading = true;
        var settings = BusinessSettingsService.Load();
        _quote = new CustomQuote
        {
            QuoteCode = $"Q-{DateTime.Now:yyyyMMdd-HHmm}",
            QuoteDate = DateTime.Today,
            ValidUntil = DateTime.Today.AddDays(14),
            DepositPercent = 30m,
            ProposalStatus = "Not Sent",
            Introduction = "Thank you for the opportunity to create this piece. The design options below can be adjusted before approval.",
            Terms = settings.TermsAndConditions
        };
        _options.Clear();
        _stoneLinks.Clear();
        _materialLinks.Clear();
        _externalDiamondLinks.Clear();
        var option = NewOption("Option A", settings);
        _options.Add(option);
        EnsureLinkCollections(option);
        BindQuote();
        _loading = false;
        OptionsList.SelectedIndex = 0;
    }

    private static QuoteOption NewOption(string name, BusinessSettings settings) => new()
    {
        OptionName = name,
        LabourRate = settings.DefaultLabourRate,
        MarkupPercent = settings.DefaultProfitMarginPercent,
        GstPercent = settings.GstRegistered ? settings.GstRatePercent : 0m
    };

    private void EnsureLinkCollections(QuoteOption option)
    {
        if (!_stoneLinks.ContainsKey(option)) _stoneLinks[option] = new List<QuoteOptionStoneLink>();
        if (!_materialLinks.ContainsKey(option)) _materialLinks[option] = new List<QuoteOptionMaterialLink>();
        if (!_externalDiamondLinks.ContainsKey(option)) _externalDiamondLinks[option] = new List<QuoteOptionExternalDiamondLink>();
    }

    private void BindQuote()
    {
        QuoteCodeBox.Text = _quote.QuoteCode;
        TitleBox.Text = _quote.Title;
        ValidUntilPicker.SelectedDate = _quote.ValidUntil;
        DepositPercentBox.Text = _quote.DepositPercent.ToString("0.##");
        IntroductionBox.Text = _quote.Introduction ?? string.Empty;
        CustomerNotesBox.Text = _quote.CustomerNotes ?? string.Empty;
        CustomerCombo.SelectedItem = CustomerCombo.Items.Cast<Customer>().FirstOrDefault(x => _quote.CustomerId.HasValue && x.Id == _quote.CustomerId.Value);
        if (CustomerCombo.SelectedIndex < 0) CustomerCombo.SelectedIndex = 0;
        OptionsList.ItemsSource = null;
        OptionsList.ItemsSource = _options;
        WorkflowStatusText.Text = _quote.Status;
        RefreshQuoteOverview();
    }

    private void PullQuoteFields()
    {
        _quote.QuoteCode = QuoteCodeBox.Text.Trim();
        _quote.Title = TitleBox.Text.Trim();
        var selectedCustomer = CustomerCombo.SelectedItem as Customer;
        _quote.CustomerId = selectedCustomer != null && selectedCustomer.Id > 0 ? selectedCustomer.Id : null;
        _quote.ValidUntil = ValidUntilPicker.SelectedDate;
        _quote.DepositPercent = D(DepositPercentBox.Text);
        _quote.Introduction = IntroductionBox.Text.Trim();
        _quote.CustomerNotes = CustomerNotesBox.Text.Trim();
        PullOptionFields();
        RefreshQuoteOverview();
    }

    private void PullOptionFields()
    {
        if (_loading || OptionsList.SelectedItem is not QuoteOption option) return;
        option.OptionName = string.IsNullOrWhiteSpace(OptionNameBox.Text) ? "Option" : OptionNameBox.Text.Trim();
        option.Description = DescriptionBox.Text.Trim();
        option.MetalDetails = MetalDetailsBox.Text.Trim();
        option.StoneDetails = StoneDetailsBox.Text.Trim();
        option.ImagePath = string.IsNullOrWhiteSpace(OptionImagePathBox.Text) ? null : OptionImagePathBox.Text.Trim();
        option.LabourHours = D(LabourHoursBox.Text);
        option.LabourRate = D(LabourRateBox.Text);
        option.MetalCost = D(MetalCostBox.Text);
        option.StoneCost = D(StoneCostBox.Text);
        option.SettingCost = D(SettingCostBox.Text);
        option.FindingsCost = D(FindingsCostBox.Text);
        option.OtherCost = D(OtherCostBox.Text);
        option.MarkupPercent = D(MarkupBox.Text);
        option.IsRecommended = RecommendedCheck.IsChecked == true;
        Calculate(option);
        RefreshTotals(option);
    }

    private static decimal D(string? value) => decimal.TryParse(value, out var result) ? result : 0m;

    private static void Calculate(QuoteOption option)
    {
        var direct = option.LabourHours * option.LabourRate + option.MetalCost + option.StoneCost + option.SettingCost + option.FindingsCost + option.OtherCost;
        option.Subtotal = decimal.Round(direct * (1m + option.MarkupPercent / 100m), 2);
        option.TotalPrice = decimal.Round(option.Subtotal * (1m + option.GstPercent / 100m), 2);
    }

    private void RefreshTotals(QuoteOption option)
    {
        var labour = option.LabourHours * option.LabourRate;
        CostBreakdownText.Text = $"Labour {labour:C} | Metal/materials {option.MetalCost:C} | Stones {option.StoneCost:C} | Setting {option.SettingCost:C} | Findings {option.FindingsCost:C} | Other {option.OtherCost:C} | Markup {option.MarkupPercent:0.##}%";
        TotalPriceText.Text = option.TotalPrice.ToString("C");
        DepositText.Text = $"Deposit {_quote.DepositPercent:0.##}%: {(option.TotalPrice * _quote.DepositPercent / 100m):C}";
        RefreshLinkedInventory(option);
        RefreshQuoteOverview();
    }

    private void RefreshLinkedInventory(QuoteOption? option)
    {
        if (option == null)
        {
            LinkedStonesList.ItemsSource = null;
            LinkedMaterialsList.ItemsSource = null;
            LinkedExternalDiamondsList.ItemsSource = null;
            ReservationSummaryText.Text = string.Empty;
            SelectedOptionSummaryText.Text = "Select an option to see quote totals and linked stock.";
            RefreshQuoteOverview();
            return;
        }

        EnsureLinkCollections(option);
        LinkedStonesList.ItemsSource = null;
        LinkedStonesList.ItemsSource = _stoneLinks[option];
        LinkedMaterialsList.ItemsSource = null;
        LinkedMaterialsList.ItemsSource = _materialLinks[option];
        LinkedExternalDiamondsList.ItemsSource = null;
        LinkedExternalDiamondsList.ItemsSource = _externalDiamondLinks[option];

        var stoneCount = _stoneLinks[option].Count;
        var materialCount = _materialLinks[option].Count;
        var externalCount = _externalDiamondLinks[option].Count;
        var reserved = _stoneLinks[option].Count(x => x.ReservationStatus == "Reserved") + _materialLinks[option].Count(x => x.ReservationStatus == "Reserved");
        var externalActive = _externalDiamondLinks[option].Count(x => x.LinkStatus is "Hold Confirmed" or "Ordered" or "Received");
        ReservationSummaryText.Text = $"Linked: {stoneCount} owned stone(s), {externalCount} external diamond(s), {materialCount} material line(s). Reserved owned allocations: {reserved}. External supplier active/ordered: {externalActive}.";
        SelectedOptionSummaryText.Text = BuildSelectedOptionSummary(option);
    }

    private void RefreshQuoteOverview()
    {
        if (OptionComparisonGrid == null)
            return;

        foreach (var option in _options)
        {
            EnsureLinkCollections(option);
            Calculate(option);
        }

        QuoteStatusText.Text = BuildQuoteStatusText();
        QuoteExpiryText.Text = BuildQuoteExpiryText();
        QuoteNextActionText.Text = BuildNextActionText();

        var rows = _options.Select(CreateComparisonRow).ToList();
        var selectedOption = OptionsList.SelectedItem as QuoteOption;
        ComparisonSummaryText.Text = rows.Count == 0
            ? "Add at least one option to compare prices, deposits, and linked stock."
            : $"{rows.Count} option(s) | recommended {_options.Count(x => x.IsRecommended)} | accepted {(_quote.AcceptedOptionId.HasValue ? "yes" : "no")}.";

        _refreshingComparison = true;
        OptionComparisonGrid.ItemsSource = rows;
        OptionComparisonGrid.SelectedItem = rows.FirstOrDefault(x => ReferenceEquals(x.Option, selectedOption) || (selectedOption?.Id > 0 && x.Option.Id == selectedOption.Id));
        _refreshingComparison = false;

        if (selectedOption != null)
            SelectedOptionSummaryText.Text = BuildSelectedOptionSummary(selectedOption);
    }

    private string BuildQuoteStatusText()
    {
        var status = string.IsNullOrWhiteSpace(_quote.Status) ? "Draft" : _quote.Status;
        var customer = CustomerCombo.SelectedItem is Customer c && c.Id > 0 ? c.FullName : "No customer selected";
        var code = string.IsNullOrWhiteSpace(_quote.QuoteCode) ? "Unsaved quote" : _quote.QuoteCode;
        var proposal = string.IsNullOrWhiteSpace(_quote.ProposalStatus) ? "Proposal not sent" : $"Proposal {_quote.ProposalStatus}";
        return $"{code} | {status} | {proposal} | {customer}";
    }

    private string BuildQuoteExpiryText()
    {
        if (!_quote.ValidUntil.HasValue)
            return "No quote expiry date is set.";

        var days = (_quote.ValidUntil.Value.Date - DateTime.Today).Days;
        if (days < 0)
            return $"Expired {Math.Abs(days)} day(s) ago. Create a follow-up or update the valid-until date.";
        if (days == 0)
            return "Expires today. Follow up before sending or accepting changes.";
        if (days <= 3)
            return $"Expires in {days} day(s). Consider creating a customer follow-up.";
        return $"Valid until {_quote.ValidUntil.Value:dd MMM yyyy}.";
    }

    private string BuildNextActionText()
    {
        var selected = OptionsList.SelectedItem as QuoteOption;
        if (string.IsNullOrWhiteSpace(_quote.Title))
            return "Enter the project title, customer, and quote details.";
        if (_options.Count == 0)
            return "Add a quote option before previewing or sending a proposal.";
        if (selected == null)
            return "Select an option to edit, compare, recommend, or accept.";
        if (_quote.ValidUntil.HasValue && _quote.ValidUntil.Value.Date < DateTime.Today && _quote.Status is not "Accepted" and not "Converted to Job")
            return "This quote is expired. Update the expiry date or create a follow-up before progressing.";
        if (!_options.Any(x => x.IsRecommended))
            return "Mark the strongest option as recommended, then preview the proposal.";
        if (!string.Equals(_quote.ProposalStatus, "Sent", StringComparison.OrdinalIgnoreCase) && !_quote.AcceptedOptionId.HasValue)
            return "Preview the proposal, then use Send / Record Proposal to prepare the customer message and follow-up.";
        if (!_quote.AcceptedOptionId.HasValue)
            return "The proposal is sent. Record feedback, follow up, then accept the chosen option.";
        if (_quote.AcceptedOptionId == selected.Id && !_quote.LinkedJobId.HasValue)
            return "The selected option is accepted. Create the production job when ready.";
        if (_quote.LinkedJobId.HasValue)
            return "This quote is linked to a production job. Continue through production and payments.";
        return "Select the accepted option or create a follow-up for the customer.";
    }

    private OptionComparisonRow CreateComparisonRow(QuoteOption option)
    {
        EnsureLinkCollections(option);
        var status = option.Id > 0 && _quote.AcceptedOptionId == option.Id
            ? "Accepted"
            : option.IsRecommended ? "Recommended" : "Draft";
        return new OptionComparisonRow(
            option,
            option.OptionName,
            option.TotalPrice.ToString("C"),
            (option.TotalPrice * _quote.DepositPercent / 100m).ToString("C"),
            BuildLinkSummary(option),
            string.IsNullOrWhiteSpace(option.ImagePath) ? "No" : "Yes",
            status);
    }

    private string BuildSelectedOptionSummary(QuoteOption option)
    {
        EnsureLinkCollections(option);
        var direct = option.LabourHours * option.LabourRate + option.MetalCost + option.StoneCost + option.SettingCost + option.FindingsCost + option.OtherCost;
        var image = string.IsNullOrWhiteSpace(option.ImagePath) ? "No design image attached." : "Design image attached.";
        return $"{option.OptionName}: {option.TotalPrice:C} total, {(option.TotalPrice * _quote.DepositPercent / 100m):C} deposit, {direct:C} direct cost. {BuildLinkSummary(option)}. {image}";
    }

    private string BuildLinkSummary(QuoteOption option)
    {
        EnsureLinkCollections(option);
        var owned = _stoneLinks[option].Count;
        var materials = _materialLinks[option].Count;
        var external = _externalDiamondLinks[option].Count;
        return $"{owned} stone(s), {external} supplier diamond(s), {materials} material line(s)";
    }

    private void SynchroniseLinkedCosts(QuoteOption option)
    {
        EnsureLinkCollections(option);
        option.StoneCost = _stoneLinks[option].Sum(x => x.UnitCost) + _externalDiamondLinks[option].Sum(x => x.SupplierPrice);
        option.MetalCost = _materialLinks[option].Sum(x => x.Quantity * x.UnitCost);
        Calculate(option);

        _loading = true;
        StoneCostBox.Text = option.StoneCost.ToString("0.##");
        MetalCostBox.Text = option.MetalCost.ToString("0.##");
        _loading = false;
        RefreshTotals(option);
    }

    private void ShowOption(QuoteOption? option)
    {
        _loading = true;
        var enabled = option != null;
        if (option != null)
        {
            EnsureLinkCollections(option);
            OptionNameBox.Text = option.OptionName;
            DescriptionBox.Text = option.Description ?? string.Empty;
            MetalDetailsBox.Text = option.MetalDetails ?? string.Empty;
            StoneDetailsBox.Text = option.StoneDetails ?? string.Empty;
            OptionImagePathBox.Text = option.ImagePath ?? string.Empty;
            LabourHoursBox.Text = option.LabourHours.ToString("0.##");
            LabourRateBox.Text = option.LabourRate.ToString("0.##");
            MetalCostBox.Text = option.MetalCost.ToString("0.##");
            StoneCostBox.Text = option.StoneCost.ToString("0.##");
            SettingCostBox.Text = option.SettingCost.ToString("0.##");
            FindingsCostBox.Text = option.FindingsCost.ToString("0.##");
            OtherCostBox.Text = option.OtherCost.ToString("0.##");
            MarkupBox.Text = option.MarkupPercent.ToString("0.##");
            RecommendedCheck.IsChecked = option.IsRecommended;
            RefreshOptionImage(option);
            RefreshTotals(option);
        }
        else
        {
            RefreshLinkedInventory(null);
            RefreshOptionImage(null);
        }

        foreach (var control in new System.Windows.Controls.Control[] { OptionNameBox, DescriptionBox, MetalDetailsBox, StoneDetailsBox, OptionImagePathBox, AttachImageButton, OpenImageButton, RemoveImageButton, LabourHoursBox, LabourRateBox, MetalCostBox, StoneCostBox, SettingCostBox, FindingsCostBox, OtherCostBox, MarkupBox, RecommendedCheck, StoneCombo, MaterialCombo, LinkedStoneCostBox, MaterialQuantityBox, MaterialUnitCostBox, ExternalDiamondCombo, ExternalDiamondSupplierCostBox, ExternalDiamondStatusCombo })
            control.IsEnabled = enabled;
        _loading = false;
    }

    private void RefreshOptionImage(QuoteOption? option)
    {
        var path = option?.ImagePath;
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            OptionImagePreview.Source = null;
            OptionImageEmptyText.Text = "No image";
            OptionImageEmptyText.Visibility = Visibility.Visible;
            return;
        }

        try
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.UriSource = new Uri(path, UriKind.Absolute);
            image.EndInit();
            image.Freeze();
            OptionImagePreview.Source = image;
            OptionImageEmptyText.Visibility = Visibility.Collapsed;
        }
        catch
        {
            OptionImagePreview.Source = null;
            OptionImageEmptyText.Text = "Image unavailable";
            OptionImageEmptyText.Visibility = Visibility.Visible;
        }
    }

    private void SaveQuote()
    {
        PullQuoteFields();
        if (string.IsNullOrWhiteSpace(_quote.Title)) throw new InvalidOperationException("Enter a quote title or project name.");

        using var db = new AppDbContext();
        using var transaction = db.Database.BeginTransaction();
        if (_quote.Id == 0)
        {
            db.CustomQuotes.Add(_quote);
            db.SaveChanges();
        }
        else
        {
            db.CustomQuotes.Update(_quote);
        }

        foreach (var option in _options)
        {
            option.CustomQuoteId = _quote.Id;
            Calculate(option);
            if (option.Id == 0) db.QuoteOptions.Add(option); else db.QuoteOptions.Update(option);
            db.SaveChanges();

            EnsureLinkCollections(option);
            foreach (var link in _stoneLinks[option])
            {
                link.QuoteOptionId = option.Id;
                if (link.Id == 0) db.QuoteOptionStoneLinks.Add(link); else db.QuoteOptionStoneLinks.Update(link);
            }
            foreach (var link in _materialLinks[option])
            {
                link.QuoteOptionId = option.Id;
                if (link.Id == 0) db.QuoteOptionMaterialLinks.Add(link); else db.QuoteOptionMaterialLinks.Update(link);
            }
            foreach (var link in _externalDiamondLinks[option])
            {
                link.QuoteOptionId = option.Id;
                if (link.Id == 0) db.QuoteOptionExternalDiamondLinks.Add(link); else db.QuoteOptionExternalDiamondLinks.Update(link);
            }
        }

        var existingOptionIds = db.QuoteOptions.Where(x => x.CustomQuoteId == _quote.Id).Select(x => x.Id).ToList();
        var retainedOptionIds = _options.Where(x => x.Id > 0).Select(x => x.Id).ToList();
        var removedOptionIds = existingOptionIds.Except(retainedOptionIds).ToList();
        if (removedOptionIds.Count > 0)
        {
            db.QuoteOptionStoneLinks.RemoveRange(db.QuoteOptionStoneLinks.Where(x => removedOptionIds.Contains(x.QuoteOptionId)));
            db.QuoteOptionMaterialLinks.RemoveRange(db.QuoteOptionMaterialLinks.Where(x => removedOptionIds.Contains(x.QuoteOptionId)));
            db.QuoteOptionExternalDiamondLinks.RemoveRange(db.QuoteOptionExternalDiamondLinks.Where(x => removedOptionIds.Contains(x.QuoteOptionId)));
            db.QuoteOptions.RemoveRange(db.QuoteOptions.Where(x => removedOptionIds.Contains(x.Id)));
        }

        foreach (var option in _options.Where(x => x.Id > 0))
        {
            var retainedStoneIds = _stoneLinks[option].Where(x => x.Id > 0).Select(x => x.Id).ToList();
            var retainedMaterialIds = _materialLinks[option].Where(x => x.Id > 0).Select(x => x.Id).ToList();
            var retainedExternalIds = _externalDiamondLinks[option].Where(x => x.Id > 0).Select(x => x.Id).ToList();
            db.QuoteOptionStoneLinks.RemoveRange(db.QuoteOptionStoneLinks.Where(x => x.QuoteOptionId == option.Id && !retainedStoneIds.Contains(x.Id)));
            db.QuoteOptionMaterialLinks.RemoveRange(db.QuoteOptionMaterialLinks.Where(x => x.QuoteOptionId == option.Id && !retainedMaterialIds.Contains(x.Id)));
            db.QuoteOptionExternalDiamondLinks.RemoveRange(db.QuoteOptionExternalDiamondLinks.Where(x => x.QuoteOptionId == option.Id && !retainedExternalIds.Contains(x.Id)));
        }

        db.SaveChanges();
        transaction.Commit();
        LoadReferenceData();
        CustomerCombo.SelectedItem = CustomerCombo.Items.Cast<Customer>().FirstOrDefault(x => _quote.CustomerId.HasValue && x.Id == _quote.CustomerId.Value)
            ?? CustomerCombo.Items.Cast<Customer>().FirstOrDefault(x => x.Id == 0);
        WorkflowStatusText.Text = $"Saved {_quote.QuoteCode}";
        RefreshQuoteOverview();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        try { SaveQuote(); }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Save quote", MessageBoxButton.OK, MessageBoxImage.Warning); }
    }

    private void NewQuote_Click(object sender, RoutedEventArgs e) => StartNewQuote();

    private void AddOption_Click(object sender, RoutedEventArgs e)
    {
        PullOptionFields();
        var option = NewOption($"Option {(char)('A' + _options.Count)}", BusinessSettingsService.Load());
        _options.Add(option);
        EnsureLinkCollections(option);
        OptionsList.Items.Refresh();
        OptionsList.SelectedItem = option;
        RefreshQuoteOverview();
    }

    private void DuplicateOption_Click(object sender, RoutedEventArgs e)
    {
        PullOptionFields();
        if (OptionsList.SelectedItem is not QuoteOption source) return;
        var option = new QuoteOption
        {
            OptionName = source.OptionName + " Copy",
            Description = source.Description,
            MetalDetails = source.MetalDetails,
            StoneDetails = source.StoneDetails,
            ImagePath = source.ImagePath,
            LabourHours = source.LabourHours,
            LabourRate = source.LabourRate,
            MetalCost = source.MetalCost,
            StoneCost = source.StoneCost,
            SettingCost = source.SettingCost,
            FindingsCost = source.FindingsCost,
            OtherCost = source.OtherCost,
            MarkupPercent = source.MarkupPercent,
            GstPercent = source.GstPercent
        };
        Calculate(option);
        _options.Add(option);
        EnsureLinkCollections(option);
        foreach (var link in _stoneLinks[source])
            _stoneLinks[option].Add(new QuoteOptionStoneLink { StoneId = link.StoneId, StoneCodeSnapshot = link.StoneCodeSnapshot, DescriptionSnapshot = link.DescriptionSnapshot, UnitCost = link.UnitCost, ReservationStatus = "Proposed" });
        foreach (var link in _materialLinks[source])
            _materialLinks[option].Add(new QuoteOptionMaterialLink { MaterialId = link.MaterialId, MaterialCodeSnapshot = link.MaterialCodeSnapshot, MaterialNameSnapshot = link.MaterialNameSnapshot, Quantity = link.Quantity, UnitCost = link.UnitCost, UnitTypeSnapshot = link.UnitTypeSnapshot, ReservationStatus = "Proposed" });
        foreach (var link in _externalDiamondLinks[source])
            _externalDiamondLinks[option].Add(new QuoteOptionExternalDiamondLink { ExternalDiamondId = link.ExternalDiamondId, SourceSystemSnapshot = link.SourceSystemSnapshot, SupplierDiamondIdSnapshot = link.SupplierDiamondIdSnapshot, DiamondSummarySnapshot = link.DiamondSummarySnapshot, LabSnapshot = link.LabSnapshot, CertificateNumberSnapshot = link.CertificateNumberSnapshot, SupplierPrice = link.SupplierPrice, Currency = link.Currency, RetailPriceSnapshot = link.RetailPriceSnapshot, VideoUrlSnapshot = link.VideoUrlSnapshot, CertificateUrlSnapshot = link.CertificateUrlSnapshot, LinkStatus = "Proposed" });
        OptionsList.Items.Refresh();
        OptionsList.SelectedItem = option;
        RefreshQuoteOverview();
    }

    private void RemoveOption_Click(object sender, RoutedEventArgs e)
    {
        if (OptionsList.SelectedItem is QuoteOption option && _options.Count > 1)
        {
            if (_stoneLinks.GetValueOrDefault(option)?.Any(x => x.ReservationStatus == "Reserved") == true || _materialLinks.GetValueOrDefault(option)?.Any(x => x.ReservationStatus == "Reserved") == true || _externalDiamondLinks.GetValueOrDefault(option)?.Any(x => x.LinkStatus is "Hold Confirmed" or "Ordered" or "Received") == true)
            {
                MessageBox.Show("Release reservations or active external diamond commitments before removing this option.", "Reserved inventory", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            _options.Remove(option);
            _stoneLinks.Remove(option);
            _materialLinks.Remove(option);
            _externalDiamondLinks.Remove(option);
            OptionsList.Items.Refresh();
            OptionsList.SelectedIndex = 0;
            RefreshQuoteOverview();
        }
    }

    private void OptionsList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e) => ShowOption(OptionsList.SelectedItem as QuoteOption);
    private void OptionText_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e) { if (!_loading && IsLoaded) { PullOptionFields(); OptionsList.Items.Refresh(); } }
    private void CostField_TextChanged(object sender, RoutedEventArgs e) { if (!_loading && IsLoaded) { _quote.DepositPercent = D(DepositPercentBox.Text); PullOptionFields(); } }

    private void RecommendOption_Click(object sender, RoutedEventArgs e)
    {
        if (OptionsList.SelectedItem is not QuoteOption selectedOption)
            return;

        PullOptionFields();
        foreach (var option in _options)
            option.IsRecommended = ReferenceEquals(option, selectedOption);

        RecommendedCheck.IsChecked = true;
        OptionsList.Items.Refresh();
        WorkflowStatusText.Text = $"Recommended: {selectedOption.OptionName}";
        RefreshQuoteOverview();
    }

    private void OptionComparisonGrid_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (_refreshingComparison || _loading || OptionComparisonGrid.SelectedItem is not OptionComparisonRow row)
            return;

        OptionsList.SelectedItem = row.Option;
    }

    private void AttachOptionImage_Click(object sender, RoutedEventArgs e)
    {
        if (OptionsList.SelectedItem is not QuoteOption option)
            return;

        var dialog = new OpenFileDialog
        {
            Title = "Choose design image",
            Filter = "Image files|*.jpg;*.jpeg;*.png;*.bmp;*.gif;*.webp|All files|*.*",
            Multiselect = false
        };

        if (dialog.ShowDialog(this) != true)
            return;

        if (!PhotoStorageService.LooksLikeImage(dialog.FileName))
        {
            MessageBox.Show("Choose a JPG, PNG, BMP, GIF or WebP image.", "Design image", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var ownerId = option.Id > 0 ? option.Id : Math.Max(_quote.Id, 0);
            option.ImagePath = PhotoStorageService.CopyPhotoToAppFolder(dialog.FileName, "QuoteOption", ownerId);
            OptionImagePathBox.Text = option.ImagePath;
            RefreshOptionImage(option);
            RefreshQuoteOverview();
            WorkflowStatusText.Text = $"Attached image to {option.OptionName}";
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Design image", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void RemoveOptionImage_Click(object sender, RoutedEventArgs e)
    {
        if (OptionsList.SelectedItem is not QuoteOption option)
            return;

        option.ImagePath = null;
        OptionImagePathBox.Text = string.Empty;
        RefreshOptionImage(option);
        RefreshQuoteOverview();
        WorkflowStatusText.Text = $"Removed image from {option.OptionName}";
    }

    private void OpenOptionImage_Click(object sender, RoutedEventArgs e)
    {
        if (OptionsList.SelectedItem is not QuoteOption option || string.IsNullOrWhiteSpace(option.ImagePath) || !File.Exists(option.ImagePath))
        {
            MessageBox.Show("No design image is attached to this option.", "Design image", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        Process.Start(new ProcessStartInfo(option.ImagePath) { UseShellExecute = true });
    }

    private void CreateQuoteFollowUp_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            SaveQuote();
            using var db = new AppDbContext();
            var title = $"Follow up quote {_quote.QuoteCode}";
            var duplicate = db.BusinessTasks.AsNoTracking().AsEnumerable().Any(t =>
                t.IsOpen &&
                string.Equals(t.Title, title, StringComparison.OrdinalIgnoreCase) &&
                t.CustomerId == _quote.CustomerId);
            if (duplicate)
            {
                MessageBox.Show("An open follow-up already exists for this quote.", "Quote follow-up", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dueDate = GetSuggestedFollowUpDate();
            var task = new BusinessTask
            {
                TaskCode = TaskWorkflowService.GenerateTaskCode(),
                Title = title,
                Category = BusinessTaskCategory.CustomerFollowUp,
                Priority = dueDate.Date <= DateTime.Today ? BusinessTaskPriority.High : BusinessTaskPriority.Normal,
                Status = BusinessTaskStatus.ToDo,
                DueDate = dueDate,
                ReminderDate = dueDate,
                CustomerId = _quote.CustomerId,
                Description = $"{BuildNextActionText()}\n\nQuote: {_quote.QuoteCode} {_quote.Title}".Trim(),
                ShowOnDashboard = true
            };
            db.BusinessTasks.Add(task);
            db.SaveChanges();
            WorkflowStatusText.Text = $"Created follow-up {task.TaskCode}";
            MessageBox.Show($"Created follow-up task {task.TaskCode} due {task.DueDate:dd MMM yyyy}.", "Quote follow-up", MessageBoxButton.OK, MessageBoxImage.Information);
            RefreshQuoteOverview();
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Quote follow-up", MessageBoxButton.OK, MessageBoxImage.Warning); }
    }

    private DateTime GetSuggestedFollowUpDate()
    {
        if (!_quote.ValidUntil.HasValue)
            return DateTime.Today.AddDays(2);

        var days = (_quote.ValidUntil.Value.Date - DateTime.Today).Days;
        if (days <= 0)
            return DateTime.Today;
        if (days <= 3)
            return _quote.ValidUntil.Value.Date;
        return DateTime.Today.AddDays(2);
    }

    private bool LoadQuote(int quoteId)
    {
        if (quoteId <= 0) return false;
        using var db = new AppDbContext();
        var quote = db.CustomQuotes.AsNoTracking().FirstOrDefault(x => x.Id == quoteId);
        if (quote == null) return false;

        _quote = quote;
        _options.Clear();
        _stoneLinks.Clear();
        _materialLinks.Clear();
        _externalDiamondLinks.Clear();
        _options.AddRange(db.QuoteOptions.AsNoTracking().Where(x => x.CustomQuoteId == _quote.Id).OrderBy(x => x.Id));
        foreach (var option in _options)
        {
            _stoneLinks[option] = db.QuoteOptionStoneLinks.AsNoTracking().Where(x => x.QuoteOptionId == option.Id).OrderBy(x => x.Id).ToList();
            _materialLinks[option] = db.QuoteOptionMaterialLinks.AsNoTracking().Where(x => x.QuoteOptionId == option.Id).OrderBy(x => x.Id).ToList();
            _externalDiamondLinks[option] = db.QuoteOptionExternalDiamondLinks.AsNoTracking().Where(x => x.QuoteOptionId == option.Id).OrderBy(x => x.Id).ToList();
        }
        _loading = true;
        QuoteCombo.SelectedItem = QuoteCombo.Items.Cast<CustomQuote>().FirstOrDefault(x => x.Id == _quote.Id);
        BindQuote();
        _loading = false;
        OptionsList.SelectedIndex = _options.Count > 0 ? 0 : -1;
        RefreshQuoteOverview();
        return true;
    }

    private void QuoteCombo_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (_loading || QuoteCombo.SelectedItem is not CustomQuote selected || selected.Id <= 0) return;
        LoadQuote(selected.Id);
    }

    private void StoneCombo_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (_loading || StoneCombo.SelectedItem is not Stone stone || stone.Id <= 0) return;
        LinkedStoneCostBox.Text = stone.EstimatedValue.ToString("0.##");
    }

    private void MaterialCombo_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (_loading || MaterialCombo.SelectedItem is not Material material || material.Id <= 0) return;
        MaterialUnitCostBox.Text = material.PurchaseCost.ToString("0.##");
        UpdateMaterialAvailability(material);
    }

    private void UpdateMaterialAvailability(Material material)
    {
        using var db = new AppDbContext();
        var reserved = db.QuoteOptionMaterialLinks.AsNoTracking().Where(x => x.MaterialId == material.Id && x.ReservationStatus == "Reserved").Select(x => x.Quantity).ToList().Sum();
        MaterialAvailabilityText.Text = $"On hand {material.CurrentQuantity:0.###} {material.UnitType}; reserved {reserved:0.###}; available {(material.CurrentQuantity - reserved):0.###}.";
    }

    private void LinkStone_Click(object sender, RoutedEventArgs e)
    {
        if (OptionsList.SelectedItem is not QuoteOption option || StoneCombo.SelectedItem is not Stone stone || stone.Id <= 0) return;
        EnsureLinkCollections(option);
        if (_stoneLinks[option].Any(x => x.StoneId == stone.Id))
        {
            MessageBox.Show("That stone is already linked to this option.", "Link stone", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        _stoneLinks[option].Add(new QuoteOptionStoneLink
        {
            StoneId = stone.Id,
            StoneCodeSnapshot = stone.StoneCode,
            DescriptionSnapshot = $"{stone.StoneType} {stone.Shape} {stone.WeightCarats:0.###}ct".Trim(),
            UnitCost = D(LinkedStoneCostBox.Text),
            ReservationStatus = "Proposed"
        });
        SynchroniseLinkedCosts(option);
    }

    private void RemoveLinkedStone_Click(object sender, RoutedEventArgs e)
    {
        if (OptionsList.SelectedItem is not QuoteOption option || LinkedStonesList.SelectedItem is not QuoteOptionStoneLink link) return;
        if (link.ReservationStatus == "Reserved")
        {
            MessageBox.Show("Release reservations before removing a reserved stone.", "Reserved stone", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        _stoneLinks[option].Remove(link);
        SynchroniseLinkedCosts(option);
    }

    private void LinkMaterial_Click(object sender, RoutedEventArgs e)
    {
        if (OptionsList.SelectedItem is not QuoteOption option || MaterialCombo.SelectedItem is not Material material || material.Id <= 0) return;
        var quantity = D(MaterialQuantityBox.Text);
        if (quantity <= 0m)
        {
            MessageBox.Show("Enter a material quantity greater than zero.", "Link material", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        EnsureLinkCollections(option);
        var existing = _materialLinks[option].FirstOrDefault(x => x.MaterialId == material.Id && x.ReservationStatus != "Reserved");
        if (existing == null)
        {
            _materialLinks[option].Add(new QuoteOptionMaterialLink
            {
                MaterialId = material.Id,
                MaterialCodeSnapshot = material.MaterialCode,
                MaterialNameSnapshot = material.Name,
                Quantity = quantity,
                UnitCost = D(MaterialUnitCostBox.Text),
                UnitTypeSnapshot = material.UnitType.ToString(),
                ReservationStatus = "Proposed"
            });
        }
        else
        {
            existing.Quantity += quantity;
            existing.UnitCost = D(MaterialUnitCostBox.Text);
        }
        SynchroniseLinkedCosts(option);
        UpdateMaterialAvailability(material);
    }

    private void RemoveLinkedMaterial_Click(object sender, RoutedEventArgs e)
    {
        if (OptionsList.SelectedItem is not QuoteOption option || LinkedMaterialsList.SelectedItem is not QuoteOptionMaterialLink link) return;
        if (link.ReservationStatus == "Reserved")
        {
            MessageBox.Show("Release reservations before removing reserved material.", "Reserved material", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        _materialLinks[option].Remove(link);
        SynchroniseLinkedCosts(option);
    }


    private void ExternalDiamondCombo_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (_loading || ExternalDiamondCombo.SelectedItem is not ExternalDiamond diamond || diamond.Id <= 0)
        {
            ExternalDiamondSummaryText.Text = "Select an external supplier diamond to link it to this option.";
            return;
        }
        ExternalDiamondSupplierCostBox.Text = diamond.SupplierPrice.ToString("0.##");
        ExternalDiamondStatusCombo.SelectedItem = string.IsNullOrWhiteSpace(diamond.Status) || diamond.Status == "Saved" || diamond.Status == "Search Result" ? "Proposed" : diamond.Status;
        ExternalDiamondSummaryText.Text = BuildExternalDiamondSummary(diamond);
    }

    private static string BuildExternalDiamondSummary(ExternalDiamond diamond)
    {
        var type = diamond.IsLabGrown ? "Lab-grown" : "Natural";
        var summary = $"{type} {diamond.Shape} {diamond.Carat:0.###}ct {diamond.Color} {diamond.Clarity} {diamond.Cut}".Replace("  ", " ").Trim();
        var cert = string.IsNullOrWhiteSpace(diamond.CertificateNumber) ? "" : $" | {diamond.Lab} cert {diamond.CertificateNumber}";
        var supplier = string.IsNullOrWhiteSpace(diamond.SupplierDiamondId) ? "" : $" | ID {diamond.SupplierDiamondId}";
        return summary + cert + supplier;
    }

    private void LinkExternalDiamond_Click(object sender, RoutedEventArgs e)
    {
        if (OptionsList.SelectedItem is not QuoteOption option || ExternalDiamondCombo.SelectedItem is not ExternalDiamond diamond || diamond.Id <= 0) return;
        EnsureLinkCollections(option);
        if (_externalDiamondLinks[option].Any(x => x.ExternalDiamondId == diamond.Id))
        {
            MessageBox.Show("That external diamond is already linked to this option.", "Link external diamond", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        _externalDiamondLinks[option].Add(new QuoteOptionExternalDiamondLink
        {
            ExternalDiamondId = diamond.Id,
            SourceSystemSnapshot = string.IsNullOrWhiteSpace(diamond.SourceSystem) ? "Nivoda" : diamond.SourceSystem,
            SupplierDiamondIdSnapshot = diamond.SupplierDiamondId,
            DiamondSummarySnapshot = BuildExternalDiamondSummary(diamond),
            LabSnapshot = diamond.Lab,
            CertificateNumberSnapshot = diamond.CertificateNumber,
            SupplierPrice = D(ExternalDiamondSupplierCostBox.Text),
            Currency = string.IsNullOrWhiteSpace(diamond.Currency) ? "AUD" : diamond.Currency,
            RetailPriceSnapshot = diamond.EstimatedRetailPrice,
            VideoUrlSnapshot = diamond.VideoUrl,
            CertificateUrlSnapshot = diamond.CertificateUrl,
            LinkStatus = ExternalDiamondStatusCombo.SelectedItem?.ToString() ?? "Proposed"
        });
        SynchroniseLinkedCosts(option);
    }

    private void RemoveExternalDiamond_Click(object sender, RoutedEventArgs e)
    {
        if (OptionsList.SelectedItem is not QuoteOption option || LinkedExternalDiamondsList.SelectedItem is not QuoteOptionExternalDiamondLink link) return;
        if (link.LinkStatus is "Hold Confirmed" or "Ordered" or "Received")
        {
            MessageBox.Show("Change this external diamond away from Hold Confirmed / Ordered / Received before removing it.", "External diamond", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        _externalDiamondLinks[option].Remove(link);
        SynchroniseLinkedCosts(option);
    }

    private void UpdateExternalDiamondStatus_Click(object sender, RoutedEventArgs e)
    {
        if (OptionsList.SelectedItem is not QuoteOption option || LinkedExternalDiamondsList.SelectedItem is not QuoteOptionExternalDiamondLink link) return;
        link.LinkStatus = ExternalDiamondStatusCombo.SelectedItem?.ToString() ?? "Proposed";
        RefreshLinkedInventory(option);
        RefreshQuoteOverview();
    }

    private void Preview_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var path = GenerateProposalFile(recordPrepared: true, out _);
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Proposal", MessageBoxButton.OK, MessageBoxImage.Error); }
    }

    private void SendProposal_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var path = GenerateProposalFile(recordPrepared: true, out var customer);
            var settings = BusinessSettingsService.Load();
            var subject = MergeProposalTemplate(settings.ProposalEmailSubjectTemplate, customer, path);
            var message = MergeProposalTemplate(settings.ProposalEmailMessageTemplate, customer, path);
            var followUpDate = GetSuggestedProposalFollowUpDate();

            var window = new SendProposalWindow(
                _quote.QuoteCode,
                _quote.Title,
                customer?.FullName,
                string.IsNullOrWhiteSpace(_quote.ProposalEmailTo) ? customer?.Email : _quote.ProposalEmailTo,
                path,
                string.IsNullOrWhiteSpace(_quote.ProposalEmailSubject) ? subject : _quote.ProposalEmailSubject,
                string.IsNullOrWhiteSpace(_quote.ProposalEmailMessage) ? message : _quote.ProposalEmailMessage,
                _quote.ProposalFollowUpDueAt ?? followUpDate)
            {
                Owner = this
            };

            if (window.ShowDialog() != true)
                return;

            using var db = new AppDbContext();
            _quote.ProposalStatus = "Sent";
            _quote.ProposalSentAt = DateTime.Now;
            _quote.ProposalFollowUpDueAt = window.FollowUpDueDate;
            _quote.ProposalLastPath = path;
            _quote.ProposalEmailTo = window.EmailTo;
            _quote.ProposalEmailSubject = window.EmailSubject;
            _quote.ProposalEmailMessage = window.EmailMessage;
            if (!IsAcceptedOrConvertedStatus(_quote.Status))
                _quote.Status = "Proposal Sent";

            db.CustomQuotes.Update(_quote);
            if (window.CreateFollowUp)
                CreateSentProposalFollowUp(db, window.FollowUpDueDate ?? followUpDate);
            db.SaveChanges();

            WorkflowStatusText.Text = $"Proposal recorded as sent to {window.EmailTo}";
            RefreshQuoteOverview();
            MessageBox.Show("Proposal recorded as sent. The email draft can be sent from your mail app, and the follow-up is ready on the dashboard.", "Send proposal", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Send proposal", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private string GenerateProposalFile(bool recordPrepared, out Customer? customer)
    {
        SaveQuote();
        using var db = new AppDbContext();
        customer = _quote.CustomerId.HasValue ? db.Customers.AsNoTracking().FirstOrDefault(x => x.Id == _quote.CustomerId) : null;
        var optionIds = _options.Where(o => o.Id > 0).Select(o => o.Id).ToList();
        var externalLinks = db.QuoteOptionExternalDiamondLinks
            .AsNoTracking()
            .Where(x => optionIds.Contains(x.QuoteOptionId))
            .ToList()
            .GroupBy(x => x.QuoteOptionId)
            .ToDictionary(g => g.Key, g => g.ToList());
        var path = CustomQuoteDocumentService.CreateProposal(_quote, customer, _options, externalLinks);

        _quote.ProposalLastGeneratedAt = DateTime.Now;
        _quote.ProposalLastPath = path;
        if (recordPrepared && (string.IsNullOrWhiteSpace(_quote.ProposalStatus) || _quote.ProposalStatus == "Not Sent"))
            _quote.ProposalStatus = "Prepared";
        if (recordPrepared && string.Equals(_quote.Status, "Draft", StringComparison.OrdinalIgnoreCase))
            _quote.Status = "Proposal Prepared";

        db.CustomQuotes.Update(_quote);
        db.SaveChanges();
        RefreshQuoteOverview();
        return path;
    }

    private string MergeProposalTemplate(string template, Customer? customer, string proposalPath)
    {
        var settings = BusinessSettingsService.Load();
        var basis = GetProposalBasisOption();
        var deposit = basis == null ? 0m : basis.TotalPrice * _quote.DepositPercent / 100m;
        return (template ?? string.Empty)
            .Replace("{CustomerName}", string.IsNullOrWhiteSpace(customer?.FullName) ? "there" : customer.FullName)
            .Replace("{QuoteCode}", _quote.QuoteCode)
            .Replace("{QuoteTitle}", _quote.Title)
            .Replace("{BusinessName}", settings.BusinessName)
            .Replace("{ProposalLink}", proposalPath)
            .Replace("{ProposalPath}", proposalPath)
            .Replace("{DepositAmount}", deposit.ToString("C"))
            .Replace("{ValidUntil}", _quote.ValidUntil?.ToString("dd MMM yyyy") ?? string.Empty);
    }

    private QuoteOption? GetProposalBasisOption()
    {
        return _options.FirstOrDefault(x => x.Id > 0 && _quote.AcceptedOptionId == x.Id)
            ?? _options.FirstOrDefault(x => x.IsRecommended)
            ?? _options.FirstOrDefault();
    }

    private DateTime GetSuggestedProposalFollowUpDate()
    {
        if (_quote.ValidUntil.HasValue)
        {
            var expiry = _quote.ValidUntil.Value.Date;
            if (expiry <= DateTime.Today.AddDays(2))
                return expiry < DateTime.Today ? DateTime.Today : expiry;
        }

        return DateTime.Today.AddDays(3);
    }

    private void CreateSentProposalFollowUp(AppDbContext db, DateTime dueDate)
    {
        var title = $"Follow up sent proposal {_quote.QuoteCode}";
        var duplicate = db.BusinessTasks.AsNoTracking().AsEnumerable().Any(t =>
            t.IsOpen &&
            string.Equals(t.Title, title, StringComparison.OrdinalIgnoreCase) &&
            t.CustomerId == _quote.CustomerId);
        if (duplicate)
            return;

        db.BusinessTasks.Add(new BusinessTask
        {
            TaskCode = TaskWorkflowService.GenerateTaskCode(),
            Title = title,
            Category = BusinessTaskCategory.CustomerFollowUp,
            Priority = dueDate.Date <= DateTime.Today ? BusinessTaskPriority.High : BusinessTaskPriority.Normal,
            Status = BusinessTaskStatus.ToDo,
            DueDate = dueDate,
            ReminderDate = dueDate,
            CustomerId = _quote.CustomerId,
            Description = $"Check whether the customer has reviewed proposal {_quote.QuoteCode} for {_quote.Title}.\n\nLast proposal file: {_quote.ProposalLastPath}".Trim(),
            ShowOnDashboard = true
        });
    }

    private static bool IsAcceptedOrConvertedStatus(string? status)
    {
        return string.Equals(status, "Accepted", StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, "Converted to Job", StringComparison.OrdinalIgnoreCase);
    }

    private void AcceptOption_Click(object sender, RoutedEventArgs e)
    {
        if (OptionsList.SelectedItem is not QuoteOption selectedOption) return;
        try
        {
            SaveQuote();
            using var db = new AppDbContext();
            using var transaction = db.Database.BeginTransaction();

            var optionIds = db.QuoteOptions.Where(x => x.CustomQuoteId == _quote.Id).Select(x => x.Id).ToList();
            var selectedStoneLinks = db.QuoteOptionStoneLinks.Where(x => x.QuoteOptionId == selectedOption.Id).ToList();
            foreach (var link in selectedStoneLinks)
            {
                var conflict = db.QuoteOptionStoneLinks.AsNoTracking().FirstOrDefault(x => x.StoneId == link.StoneId && x.ReservationStatus == "Reserved" && x.QuoteOptionId != selectedOption.Id);
                if (conflict != null)
                    throw new InvalidOperationException($"Stone {link.StoneCodeSnapshot} is already reserved by another accepted quote.");
            }

            var selectedMaterialLinks = db.QuoteOptionMaterialLinks.Where(x => x.QuoteOptionId == selectedOption.Id).ToList();
            foreach (var link in selectedMaterialLinks)
            {
                var material = db.Materials.AsNoTracking().FirstOrDefault(x => x.Id == link.MaterialId) ?? throw new InvalidOperationException($"Material {link.MaterialNameSnapshot} no longer exists.");
                var reservedElsewhere = db.QuoteOptionMaterialLinks.AsNoTracking().Where(x => x.MaterialId == link.MaterialId && x.ReservationStatus == "Reserved" && x.QuoteOptionId != selectedOption.Id).Select(x => x.Quantity).ToList().Sum();
                if (material.CurrentQuantity - reservedElsewhere < link.Quantity)
                    throw new InvalidOperationException($"Not enough {material.Name} is available. Required {link.Quantity:0.###}; available {(material.CurrentQuantity - reservedElsewhere):0.###} {material.UnitType}.");
            }

            var allStoneLinks = db.QuoteOptionStoneLinks.Where(x => optionIds.Contains(x.QuoteOptionId)).ToList();
            var allMaterialLinks = db.QuoteOptionMaterialLinks.Where(x => optionIds.Contains(x.QuoteOptionId)).ToList();
            var allExternalLinks = db.QuoteOptionExternalDiamondLinks.Where(x => optionIds.Contains(x.QuoteOptionId)).ToList();
            foreach (var link in allStoneLinks) link.ReservationStatus = link.QuoteOptionId == selectedOption.Id ? "Reserved" : "Proposed";
            foreach (var link in allMaterialLinks) link.ReservationStatus = link.QuoteOptionId == selectedOption.Id ? "Reserved" : "Proposed";
            foreach (var link in allExternalLinks)
            {
                if (link.QuoteOptionId == selectedOption.Id && link.LinkStatus == "Proposed") link.LinkStatus = "Customer Interested";
                if (link.QuoteOptionId != selectedOption.Id && link.LinkStatus == "Customer Interested") link.LinkStatus = "Proposed";
            }
            foreach (var link in allExternalLinks.Where(x => x.QuoteOptionId == selectedOption.Id))
            {
                var diamond = db.ExternalDiamonds.FirstOrDefault(x => x.Id == link.ExternalDiamondId);
                if (diamond != null && (diamond.Status == "Saved" || diamond.Status == "Search Result" || diamond.Status == "Proposed")) diamond.Status = link.LinkStatus;
            }

            _quote.AcceptedOptionId = selectedOption.Id;
            _quote.Status = "Accepted";
            _quote.ProposalStatus = "Accepted";
            db.CustomQuotes.Update(_quote);
            db.SaveChanges();
            transaction.Commit();
            ReloadCurrentQuoteLinks(db);
            WorkflowStatusText.Text = $"Accepted and reserved: {selectedOption.OptionName}";
            RefreshQuoteOverview();
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Accept option", MessageBoxButton.OK, MessageBoxImage.Error); }
    }

    private void ReleaseReservations_Click(object sender, RoutedEventArgs e)
    {
        if (_quote.Id == 0) return;
        try
        {
            using var db = new AppDbContext();
            var optionIds = db.QuoteOptions.Where(x => x.CustomQuoteId == _quote.Id).Select(x => x.Id).ToList();
            var stones = db.QuoteOptionStoneLinks.Where(x => optionIds.Contains(x.QuoteOptionId) && x.ReservationStatus == "Reserved").ToList();
            var materials = db.QuoteOptionMaterialLinks.Where(x => optionIds.Contains(x.QuoteOptionId) && x.ReservationStatus == "Reserved").ToList();
            var externals = db.QuoteOptionExternalDiamondLinks.Where(x => optionIds.Contains(x.QuoteOptionId) && x.LinkStatus == "Customer Interested").ToList();
            foreach (var link in stones) link.ReservationStatus = "Proposed";
            foreach (var link in materials) link.ReservationStatus = "Proposed";
            foreach (var link in externals) link.LinkStatus = "Proposed";
            _quote.AcceptedOptionId = null;
            _quote.Status = "Draft";
            if (string.Equals(_quote.ProposalStatus, "Accepted", StringComparison.OrdinalIgnoreCase))
                _quote.ProposalStatus = _quote.ProposalSentAt.HasValue ? "Sent" : "Prepared";
            db.CustomQuotes.Update(_quote);
            db.SaveChanges();
            ReloadCurrentQuoteLinks(db);
            WorkflowStatusText.Text = "Reservations released";
            RefreshQuoteOverview();
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Release reservations", MessageBoxButton.OK, MessageBoxImage.Error); }
    }

    private void ReloadCurrentQuoteLinks(AppDbContext db)
    {
        foreach (var option in _options)
        {
            _stoneLinks[option] = db.QuoteOptionStoneLinks.AsNoTracking().Where(x => x.QuoteOptionId == option.Id).OrderBy(x => x.Id).ToList();
            _materialLinks[option] = db.QuoteOptionMaterialLinks.AsNoTracking().Where(x => x.QuoteOptionId == option.Id).OrderBy(x => x.Id).ToList();
            _externalDiamondLinks[option] = db.QuoteOptionExternalDiamondLinks.AsNoTracking().Where(x => x.QuoteOptionId == option.Id).OrderBy(x => x.Id).ToList();
        }
        RefreshLinkedInventory(OptionsList.SelectedItem as QuoteOption);
    }

    private void CreateJob_Click(object sender, RoutedEventArgs e)
    {
        if (OptionsList.SelectedItem is not QuoteOption selectedOption)
        {
            MessageBox.Show("Select the accepted option first.");
            return;
        }
        if (_quote.AcceptedOptionId != selectedOption.Id)
        {
            MessageBox.Show("Accept and reserve this option before creating the production job.", "Production job", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        try
        {
            SaveQuote();
            using var db = new AppDbContext();
            Job job;
            if (_quote.LinkedJobId.HasValue) job = db.Jobs.FirstOrDefault(x => x.Id == _quote.LinkedJobId.Value) ?? new Job(); else job = new Job();

            var stoneLines = db.QuoteOptionStoneLinks.AsNoTracking().Where(x => x.QuoteOptionId == selectedOption.Id).ToList();
            var materialLines = db.QuoteOptionMaterialLinks.AsNoTracking().Where(x => x.QuoteOptionId == selectedOption.Id).ToList();
            var externalDiamondLines = db.QuoteOptionExternalDiamondLinks.AsNoTracking().Where(x => x.QuoteOptionId == selectedOption.Id).ToList();
            var allocationText = string.Join("\n", stoneLines.Select(x => $"Reserved stone: {x.StoneCodeSnapshot} {x.DescriptionSnapshot}")
                .Concat(externalDiamondLines.Select(x => $"External diamond: {x.DiamondSummarySnapshot} | {x.SourceSystemSnapshot} ID {x.SupplierDiamondIdSnapshot} | cert {x.CertificateNumberSnapshot} | {x.LinkStatus}"))
                .Concat(materialLines.Select(x => $"Reserved material: {x.MaterialCodeSnapshot} {x.MaterialNameSnapshot} - {x.Quantity:0.###} {x.UnitTypeSnapshot}")));

            job.JobCode = string.IsNullOrWhiteSpace(job.JobCode) ? $"JOB-{DateTime.Now:yyyyMMdd-HHmm}" : job.JobCode;
            job.CustomerId = _quote.CustomerId;
            job.JobTitle = _quote.Title;
            job.Type = JobType.CustomOrder;
            job.Status = JobStatus.Approved;
            job.DateReceived = DateTime.Today;
            job.DueDate = _quote.ValidUntil?.AddDays(28);
            job.QuoteAmount = selectedOption.TotalPrice;
            job.FinalPrice = selectedOption.TotalPrice;
            job.BalanceOwing = selectedOption.TotalPrice - job.DepositPaid;
            job.LabourHours = selectedOption.LabourHours;
            job.LabourCost = selectedOption.LabourHours * selectedOption.LabourRate;
            job.MaterialCost = selectedOption.MetalCost + selectedOption.StoneCost + selectedOption.SettingCost + selectedOption.FindingsCost + selectedOption.OtherCost;
            job.DesignNotes = $"{selectedOption.OptionName}\n{selectedOption.Description}\nMetal: {selectedOption.MetalDetails}\nStone: {selectedOption.StoneDetails}\n\nInventory allocations:\n{allocationText}";
            job.CustomerApprovalNotes = $"Accepted from quote {_quote.QuoteCode}. Owned inventory reservations retained until production completion or manual release. External diamonds must be confirmed/ordered through the supplier before setting.";
            if (job.Id == 0) db.Jobs.Add(job);
            db.SaveChanges();

            _quote.LinkedJobId = job.Id;
            _quote.AcceptedOptionId = selectedOption.Id;
            _quote.Status = "Converted to Job";
            _quote.ProposalStatus = "Converted to Job";
            db.CustomQuotes.Update(_quote);
            db.SaveChanges();
            WorkflowStatusText.Text = $"Created {job.JobCode}";
            RefreshQuoteOverview();
            MessageBox.Show($"Production job {job.JobCode} is ready in Jobs. Linked inventory remains reserved.", "Workflow complete", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Create job", MessageBoxButton.OK, MessageBoxImage.Error); }
    }

    private sealed record OptionComparisonRow(QuoteOption Option, string Name, string Total, string Deposit, string LinkSummary, string Image, string Status);
}
