using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using JewelleryBusinessManager.Models;
using JewelleryBusinessManager.Services;

namespace JewelleryBusinessManager.Views;

public partial class DymoMiniLabelWindow : Window
{
    public DymoMiniLabelWindow(object? selectedRecord)
    {
        InitializeComponent();
        LoadFromRecord(selectedRecord);
        UpdatePreview();
    }

    private void LoadFromRecord(object? record)
    {
        switch (record)
        {
            case JewelleryItem item:
                TitleBox.Text = item.Name;
                CodeBox.Text = item.StockCode;
                PriceBox.Text = item.RetailPrice > 0 ? item.RetailPrice.ToString("C") : string.Empty;
                Line1Box.Text = $"{item.Type} • {item.Metal}".Trim(' ', '•');
                Line2Box.Text = item.Status.ToString();
                break;
            case Stone stone:
                TitleBox.Text = stone.StoneType;
                CodeBox.Text = stone.StoneCode;
                PriceBox.Text = stone.EstimatedValue > 0 ? stone.EstimatedValue.ToString("C") : string.Empty;
                Line1Box.Text = $"{stone.WeightCarats:0.###} ct • {stone.Shape}".Trim(' ', '•');
                Line2Box.Text = stone.Status.ToString();
                break;
            case Job job:
                TitleBox.Text = job.JobTitle;
                CodeBox.Text = job.JobCode;
                PriceBox.Text = job.FinalPrice > 0 ? job.FinalPrice.ToString("C") : job.QuoteAmount.ToString("C");
                Line1Box.Text = job.Type.ToString();
                Line2Box.Text = job.Status.ToString();
                break;
            case Material material:
                TitleBox.Text = material.Name;
                CodeBox.Text = material.MaterialCode;
                PriceBox.Text = $"{material.CurrentQuantity:0.###} {material.UnitType}";
                Line1Box.Text = material.Category.ToString();
                Line2Box.Text = material.StorageLocation ?? string.Empty;
                break;
            default:
                TitleBox.Text = "Jewellery Label";
                CodeBox.Text = "CODE-0001";
                PriceBox.Text = string.Empty;
                Line1Box.Text = string.Empty;
                Line2Box.Text = string.Empty;
                break;
        }
    }

    private void Input_Changed(object sender, RoutedEventArgs e) => UpdatePreview();

    private void UpdatePreview()
    {
        if (!IsLoaded) return;
        PreviewTitle.Text = TitleBox.Text;
        PreviewCode.Text = CodeBox.Text;
        PreviewPrice.Text = PriceBox.Text;
        PreviewLine1.Text = Line1Box.Text;
        PreviewLine2.Text = Line2Box.Text;
        BuildBarcodeBars(CodeBox.Text);
    }

    private void BuildBarcodeBars(string code)
    {
        BarcodePanel.Children.Clear();
        var payload = $"*{SanitiseCode39(code)}*";
        foreach (var ch in payload)
        {
            var pattern = GetCode39Pattern(ch);
            foreach (var bit in pattern)
            {
                BarcodePanel.Children.Add(new Border
                {
                    Width = bit == '1' ? 3 : 1,
                    Height = 44,
                    Background = bit == '1' ? Brushes.Black : Brushes.Transparent,
                    Margin = new Thickness(0.5, 0, 0.5, 0)
                });
            }
            BarcodePanel.Children.Add(new Border { Width = 3, Background = Brushes.Transparent });
        }
    }

    private static string SanitiseCode39(string value)
    {
        var allowed = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ-. $/+%";
        var builder = new StringBuilder();
        foreach (var ch in (value ?? string.Empty).ToUpperInvariant())
        {
            if (allowed.Contains(ch)) builder.Append(ch);
        }
        return builder.Length == 0 ? "LABEL" : builder.ToString();
    }

    private static string GetCode39Pattern(char ch)
    {
        // Compact visual representation. It is designed for readable labels inside the app.
        var value = ch switch
        {
            '*' => 0b100101101,
            '-' => 0b101001011,
            '.' => 0b110010101,
            ' ' => 0b100110101,
            '$' => 0b100100101,
            '/' => 0b100101001,
            '+' => 0b101001001,
            '%' => 0b101010001,
            _ => 0b100000001 | ((ch * 73) & 0b011111110)
        };
        return Convert.ToString(value, 2).PadLeft(9, '0');
    }

    private void Print_Click(object sender, RoutedEventArgs e)
    {
        UpdatePreview();
        var dialog = new System.Windows.Controls.PrintDialog();
        if (dialog.ShowDialog() == true)
        {
            dialog.PrintVisual(LabelPreview, $"Jewellery label {CodeBox.Text}");
        }
    }

    private void SaveHtml_Click(object sender, RoutedEventArgs e)
    {
        var folder = BusinessSettingsService.GetPrintoutFolder();
        var fileName = $"mini-label-{SanitiseCode39(CodeBox.Text).ToLowerInvariant()}-{DateTime.Now:yyyyMMdd-HHmmss}.html";
        var path = Path.Combine(folder, fileName);
        File.WriteAllText(path, BuildHtml());
        Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
    }

    private string BuildHtml()
    {
        var safeCode = System.Net.WebUtility.HtmlEncode(CodeBox.Text);
        var bars = new StringBuilder();
        foreach (var ch in $"*{SanitiseCode39(CodeBox.Text)}*")
        {
            foreach (var bit in GetCode39Pattern(ch))
            {
                bars.Append(bit == '1' ? "<span class='bar wide'></span>" : "<span class='bar gap'></span>");
            }
            bars.Append("<span class='space'></span>");
        }
        var html = new StringBuilder();
        html.AppendLine("<!doctype html>");
        html.AppendLine("<html><head><meta charset='utf-8'>");
        html.AppendLine($"<title>Mini Label {safeCode}</title>");
        html.AppendLine("<style>");
        html.AppendLine("body{font-family:Segoe UI,Arial;margin:20px}");
        html.AppendLine(".label{width:57mm;height:32mm;border:1px solid #111;padding:3mm;box-sizing:border-box;text-align:center}");
        html.AppendLine(".title{font-size:14pt;font-weight:700}");
        html.AppendLine(".price{font-size:12pt;font-weight:700}");
        html.AppendLine(".bar{display:inline-block;height:10mm;vertical-align:top}");
        html.AppendLine(".wide{width:1.1mm;background:#000}");
        html.AppendLine(".gap{width:.45mm;background:transparent}");
        html.AppendLine(".space{display:inline-block;width:1.2mm}");
        html.AppendLine("</style></head>");
        html.AppendLine("<body><div class='label'>");
        html.AppendLine($"<div class='title'>{System.Net.WebUtility.HtmlEncode(TitleBox.Text)}</div>");
        html.AppendLine($"<div>{safeCode}</div>");
        html.AppendLine($"<div class='price'>{System.Net.WebUtility.HtmlEncode(PriceBox.Text)}</div>");
        html.AppendLine($"<div>{bars}</div>");
        html.AppendLine($"<div>{System.Net.WebUtility.HtmlEncode(Line1Box.Text)}</div>");
        html.AppendLine($"<small>{System.Net.WebUtility.HtmlEncode(Line2Box.Text)}</small>");
        html.AppendLine("</div></body></html>");
        return html.ToString();
    }
}
