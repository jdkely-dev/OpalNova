using System.IO;
using System.Net;
using System.Text;
using JewelleryBusinessManager.Models;

namespace JewelleryBusinessManager.Services;

public static class CustomQuoteDocumentService
{
    public static string CreateProposal(CustomQuote quote, Customer? customer, IReadOnlyList<QuoteOption> options, IReadOnlyDictionary<int, List<QuoteOptionExternalDiamondLink>>? externalDiamondLinks = null)
    {
        var settings = BusinessSettingsService.Load();
        var folder = BusinessSettingsService.GetPrintoutFolder();
        var safeCode = string.Join("_", (quote.QuoteCode.Length == 0 ? $"Quote_{quote.Id}" : quote.QuoteCode).Split(Path.GetInvalidFileNameChars()));
        var path = Path.Combine(folder, $"{safeCode}_Proposal_{DateTime.Now:yyyyMMdd_HHmmss}.html");
        static string E(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);
        static string Money(decimal value) => value.ToString("C");
        var sb = new StringBuilder();
        sb.Append("<!doctype html><html><head><meta charset='utf-8'><title>Jewellery Proposal</title><style>");
        sb.Append("body{font-family:Segoe UI,Arial;background:#f5f6f8;color:#172033;margin:0;padding:34px}.page{max-width:900px;margin:auto;background:white;padding:44px;border-radius:18px;box-shadow:0 12px 40px #0002}.top{display:flex;justify-content:space-between;border-bottom:3px solid #d9a441;padding-bottom:18px}.brand{font-size:28px;font-weight:750}.muted{color:#687387}.option{border:1px solid #dce1e8;border-radius:15px;padding:22px;margin:18px 0}.recommended{border:2px solid #d9a441}.price{font-size:28px;font-weight:750}.option-img{width:100%;max-height:360px;object-fit:cover;border-radius:14px;margin:12px 0;border:1px solid #dce1e8}.grid{display:grid;grid-template-columns:1fr 1fr;gap:12px}.costs{width:100%;border-collapse:collapse}.costs td{padding:7px;border-bottom:1px solid #edf0f4}.right{text-align:right}.footer{margin-top:34px;border-top:1px solid #ddd;padding-top:18px;font-size:13px;color:#687387}@media print{body{background:white;padding:0}.page{box-shadow:none}} </style></head><body><div class='page'>");
        sb.Append($"<div class='top'><div><div class='brand'>{E(settings.BusinessName)}</div><div class='muted'>{E(settings.OwnerName)} · {E(settings.Email)} · {E(settings.Phone)}</div></div><div class='right'><b>PROPOSAL</b><br>{E(quote.QuoteCode)}<br>{quote.QuoteDate:dd MMM yyyy}</div></div>");
        sb.Append($"<h1>{E(quote.Title)}</h1><p>Prepared for <b>{E(customer?.FullName ?? "Customer")}</b></p>");
        if (!string.IsNullOrWhiteSpace(quote.Introduction)) sb.Append($"<p>{E(quote.Introduction).Replace("\n","<br>")}</p>");
        foreach (var option in options)
        {
            var cls = option.IsRecommended ? "option recommended" : "option";
            sb.Append($"<section class='{cls}'><div class='grid'><div><h2>{E(option.OptionName)}</h2>{(option.IsRecommended ? "<b>Recommended option</b>" : "")}</div><div class='price right'>{Money(option.TotalPrice)}</div></div>");
            var imageDataUri = TryCreateImageDataUri(option.ImagePath);
            if (!string.IsNullOrWhiteSpace(imageDataUri))
                sb.Append($"<img class='option-img' src='{imageDataUri}' alt='{E(option.OptionName)} design image'>");
            sb.Append($"<p>{E(option.Description).Replace("\n","<br>")}</p><div class='grid'><p><b>Metal</b><br>{E(option.MetalDetails)}</p><p><b>Stone</b><br>{E(option.StoneDetails)}</p></div>");
            if (externalDiamondLinks != null && externalDiamondLinks.TryGetValue(option.Id, out var linkedDiamonds) && linkedDiamonds.Count > 0)
            {
                sb.Append("<h3>External supplier diamond option</h3><ul>");
                foreach (var diamond in linkedDiamonds)
                {
                    sb.Append($"<li><b>{E(diamond.DiamondSummarySnapshot)}</b> · {E(diamond.SourceSystemSnapshot)} ID {E(diamond.SupplierDiamondIdSnapshot)} · Lab {E(diamond.LabSnapshot)} · Cert {E(diamond.CertificateNumberSnapshot)} · Status {E(diamond.LinkStatus)}");
                    if (!string.IsNullOrWhiteSpace(diamond.VideoUrlSnapshot)) sb.Append($" · <a href='{E(diamond.VideoUrlSnapshot)}'>Video</a>");
                    if (!string.IsNullOrWhiteSpace(diamond.CertificateUrlSnapshot)) sb.Append($" · <a href='{E(diamond.CertificateUrlSnapshot)}'>Certificate</a>");
                    sb.Append("</li>");
                }
                sb.Append("</ul>");
            }
            sb.Append("<table class='costs'>");
            var labour=option.LabourHours*option.LabourRate;
            foreach(var row in new[]{("Labour",labour),("Metal",option.MetalCost),("Stones",option.StoneCost),("Setting",option.SettingCost),("Findings",option.FindingsCost),("Other",option.OtherCost)}) if(row.Item2!=0) sb.Append($"<tr><td>{row.Item1}</td><td class='right'>{Money(row.Item2)}</td></tr>");
            sb.Append($"<tr><td><b>Total</b></td><td class='right'><b>{Money(option.TotalPrice)}</b></td></tr></table></section>");
        }
        var accepted = options.FirstOrDefault(x=>x.Id==quote.AcceptedOptionId);
        var basis = accepted ?? options.FirstOrDefault();
        if (basis != null) sb.Append($"<h3>Payment schedule</h3><p>Deposit: {quote.DepositPercent:0.##}% ({Money(basis.TotalPrice*quote.DepositPercent/100m)}) · Balance before pickup or shipping.</p>");
        if (quote.ValidUntil.HasValue) sb.Append($"<p><b>Valid until:</b> {quote.ValidUntil:dd MMM yyyy}</p>");
        sb.Append($"<h3>Terms</h3><p>{E(quote.Terms ?? settings.TermsAndConditions).Replace("\n","<br>")}</p><div class='footer'>{E(settings.DocumentFooterText)}</div></div></body></html>");
        File.WriteAllText(path, sb.ToString());
        return path;
    }

    private static string? TryCreateImageDataUri(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return null;

        try
        {
            var extension = Path.GetExtension(path).ToLowerInvariant();
            var mimeType = extension switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".bmp" => "image/bmp",
                ".webp" => "image/webp",
                _ => "application/octet-stream"
            };
            return $"data:{mimeType};base64,{Convert.ToBase64String(File.ReadAllBytes(path))}";
        }
        catch
        {
            return null;
        }
    }
}
