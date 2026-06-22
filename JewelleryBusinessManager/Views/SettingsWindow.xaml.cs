using System.Globalization;
using System.Windows;
using Microsoft.Win32;
using JewelleryBusinessManager.Models;
using JewelleryBusinessManager.Services;
using MessageBox = System.Windows.MessageBox;
using MessageBoxButton = System.Windows.MessageBoxButton;
using MessageBoxImage = System.Windows.MessageBoxImage;
using MessageBoxResult = System.Windows.MessageBoxResult;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace JewelleryBusinessManager.Views;

public partial class SettingsWindow : Window
{
    private BusinessSettings _settings;

    public SettingsWindow()
    {
        InitializeComponent();
        _settings = BusinessSettingsService.Load();
        LoadSettingsIntoForm();
    }

    private void LoadSettingsIntoForm()
    {
        BusinessNameBox.Text = _settings.BusinessName;
        OwnerNameBox.Text = _settings.OwnerName;
        AbnBox.Text = _settings.Abn;
        PhoneBox.Text = _settings.Phone;
        EmailBox.Text = _settings.Email;
        WebsiteBox.Text = _settings.Website;
        AddressBox.Text = _settings.Address;
        LogoPathBox.Text = _settings.LogoPath;
        DefaultLabourRateBox.Text = _settings.DefaultLabourRate.ToString(CultureInfo.CurrentCulture);
        DefaultProfitMarginBox.Text = _settings.DefaultProfitMarginPercent.ToString(CultureInfo.CurrentCulture);
        GstRegisteredBox.IsChecked = _settings.GstRegistered;
        GstRateBox.Text = _settings.GstRatePercent.ToString(CultureInfo.CurrentCulture);
        TaxLabelBox.Text = _settings.TaxLabel;
        FooterTextBox.Text = _settings.DocumentFooterText;
        TermsBox.Text = _settings.TermsAndConditions;
        PrintoutFolderBox.Text = _settings.PrintoutFolder;
        BackupFolderBox.Text = _settings.BackupFolder;
    }

    private bool TryUpdateSettingsFromForm()
    {
        if (!decimal.TryParse(DefaultLabourRateBox.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out var labourRate) || labourRate < 0)
        {
            MessageBox.Show("Default labour rate must be a positive number.", "Settings", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        if (!decimal.TryParse(DefaultProfitMarginBox.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out var profitMargin) || profitMargin <= 0 || profitMargin >= 100)
        {
            MessageBox.Show("Default profit margin must be greater than 0 and less than 100.", "Settings", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        if (!decimal.TryParse(GstRateBox.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out var gstRate) || gstRate < 0)
        {
            MessageBox.Show("GST rate must be zero or a positive number.", "Settings", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        _settings.BusinessName = BusinessNameBox.Text.Trim();
        _settings.OwnerName = OwnerNameBox.Text.Trim();
        _settings.Abn = AbnBox.Text.Trim();
        _settings.Phone = PhoneBox.Text.Trim();
        _settings.Email = EmailBox.Text.Trim();
        _settings.Website = WebsiteBox.Text.Trim();
        _settings.Address = AddressBox.Text.Trim();
        _settings.LogoPath = LogoPathBox.Text.Trim();
        _settings.DefaultLabourRate = labourRate;
        _settings.DefaultProfitMarginPercent = profitMargin;
        _settings.GstRegistered = GstRegisteredBox.IsChecked == true;
        _settings.GstRatePercent = gstRate;
        _settings.TaxLabel = TaxLabelBox.Text.Trim();
        _settings.DocumentFooterText = FooterTextBox.Text.Trim();
        _settings.TermsAndConditions = TermsBox.Text.Trim();
        _settings.PrintoutFolder = PrintoutFolderBox.Text.Trim();
        _settings.BackupFolder = BackupFolderBox.Text.Trim();
        return true;
    }

    private void BrowseLogo_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Choose business logo",
            Filter = "Image files|*.jpg;*.jpeg;*.png;*.bmp;*.gif;*.webp|All files|*.*"
        };
        if (dialog.ShowDialog(this) == true)
            LogoPathBox.Text = dialog.FileName;
    }

    private void BrowsePrintoutFolder_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog { Title = "Choose printout folder" };
        if (dialog.ShowDialog(this) == true)
            PrintoutFolderBox.Text = dialog.FolderName;
    }

    private void BrowseBackupFolder_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog { Title = "Choose backup folder" };
        if (dialog.ShowDialog(this) == true)
            BackupFolderBox.Text = dialog.FolderName;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (!TryUpdateSettingsFromForm()) return;
        BusinessSettingsService.Save(_settings);
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
