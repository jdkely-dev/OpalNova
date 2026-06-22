using System.Windows;
using JewelleryBusinessManager.Services;

namespace JewelleryBusinessManager.Views;

public partial class CustomerDisplayWindow : Window
{
    public CustomerDisplayWindow()
    {
        InitializeComponent();
        var settings = BusinessSettingsService.Load();
        BusinessNameText.Text = settings.BusinessName;
        FooterText.Text = settings.DocumentFooterText;
    }

    public void UpdateDisplay(string itemName, string details, decimal total, string paymentStatus = "")
    {
        ItemNameText.Text = string.IsNullOrWhiteSpace(itemName) ? "Market Checkout" : itemName;
        ItemDetailsText.Text = string.IsNullOrWhiteSpace(paymentStatus) ? details : details + Environment.NewLine + paymentStatus;
        TotalText.Text = total.ToString("C");
    }
}
