using System.IO;
using System.Net;
using System.Text;
using JewelleryBusinessManager.Models;

namespace JewelleryBusinessManager.Services;

public static class CustomQuoteDocumentService
{
    public static string CreateProposal(
        CustomQuote quote,
        Customer? customer,
        IReadOnlyList<QuoteOption> options,
        IReadOnlyDictionary<int, List<QuoteOptionExternalDiamondLink>>? externalDiamondLinks = null)
    {
        var settings = BusinessSettingsService.Load();
        var folder = BusinessSettingsService.GetPrintoutFolder();
        var safeCode = string.Join("_", (quote.QuoteCode.Length == 0 ? $"Quote_{quote.Id}" : quote.QuoteCode).Split(Path.GetInvalidFileNameChars()));
        var path = Path.Combine(folder, $"{safeCode}_Proposal_{DateTime.Now:yyyyMMdd_HHmmss}.html");

        static string E(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);
        static string Money(decimal value) => value.ToString("C");

        var sb = new StringBuilder();
        sb.Append("<!doctype html><html><head><meta charset='utf-8'><title>Jewellery Proposal</title><style>");
        sb.Append("body{font-family:Segoe UI,Arial;background:#eef2f4;color:#172033;margin:0;padding:34px}.page{max-width:940px;margin:auto;background:white;padding:44px;border-radius:18px;box-shadow:0 18px 55px #0002}.top{display:flex;justify-content:space-between;border-bottom:3px solid #d9a441;padding-bottom:18px}.brand{font-size:29px;font-weight:750}.muted{color:#687387}.hero{padding:26px 0 16px}.hero h1{font-size:34px;margin:0 0 10px}.badge{display:inline-block;background:#fff7e4;border:1px solid #e8c46d;color:#6d4a00;border-radius:999px;padding:6px 11px;font-size:12px;font-weight:700;text-transform:uppercase}.option{border:1px solid #dce1e8;border-radius:15px;padding:22px;margin:18px 0;background:#fff}.recommended{border:2px solid #d9a441;box-shadow:0 8px 22px #d9a44122}.price{font-size:28px;font-weight:750}.option-img{width:100%;max-height:380px;object-fit:cover;border-radius:14px;margin:12px 0;border:1px solid #dce1e8}.grid{display:grid;grid-template-columns:1fr 1fr;gap:12px}.costs,.schedule{width:100%;border-collapse:collapse}.costs td,.schedule td,.schedule th{padding:7px;border-bottom:1px solid #edf0f4}.schedule th{text-align:left;color:#687387;font-size:12px;text-transform:uppercase}.payment,.steps{background:#f8fafc;border:1px solid #dde5ed;border-radius:14px;padding:18px;margin-top:18px}.right{text-align:right}.footer{margin-top:34px;border-top:1px solid #ddd;padding-top:18px;font-size:13px;color:#687387}@media print{body{background:white;padding:0}.page{box-shadow:none;border-radius:0}}");
        sb.Append("</style></head><body><div class='page'>");

        sb.Append($"<div class='top'><div><div class='brand'>{E(settings.BusinessName)}</div><div class='muted'>{E(settings.OwnerName)} &middot; {E(settings.Email)} &middot; {E(settings.Phone)}</div></div><div class='right'><b>PROPOSAL</b><br>{E(quote.QuoteCode)}<br>{quote.QuoteDate:dd MMM yyyy}</div></div>");
        sb.Append($"<div class='hero'><h1>{E(quote.Title)}</h1><p>Prepared for <b>{E(customer?.FullName ?? "Customer")}</b></p>");
        if (!string.IsNullOrWhiteSpace(quote.Introduction))
            sb.Append($"<p>{E(quote.Introduction).Replace("\n", "<br>")}</p>");
        sb.Append("</div>");

        AppendProjectContext(sb, quote, E);

        foreach (var option in options)
            AppendOption(sb, option, externalDiamondLinks, E, Money);

        var accepted = options.FirstOrDefault(x => x.Id == quote.AcceptedOptionId);
        var basis = accepted ?? options.FirstOrDefault(x => x.IsRecommended) ?? options.FirstOrDefault();
        if (basis != null)
        {
            var schedule = PaymentScheduleService.BuildForQuote(quote, basis);
            AppendPaymentSchedule(sb, schedule, E, Money);
        }

        sb.Append("<div class='steps'><h3>Next steps</h3><ol><li>Review each option and let us know which direction feels right.</li><li>Request any changes before approval if the design, stone or budget needs adjustment.</li><li>Once accepted, we will confirm deposit/payment details and move the work into production.</li></ol></div>");

        if (quote.ValidUntil.HasValue)
            sb.Append($"<p><b>Valid until:</b> {quote.ValidUntil:dd MMM yyyy}</p>");

        sb.Append($"<h3>Terms</h3><p>{E(quote.Terms ?? settings.TermsAndConditions).Replace("\n", "<br>")}</p><div class='footer'>{E(settings.DocumentFooterText)}</div></div></body></html>");
        File.WriteAllText(path, sb.ToString());
        return path;
    }

    private static void AppendPaymentSchedule(
        StringBuilder sb,
        PaymentScheduleSummary schedule,
        Func<string?, string> encode,
        Func<decimal, string> money)
    {
        sb.Append("<div class='payment'><h3>Payment schedule</h3>");
        sb.Append($"<p>{encode(schedule.Guidance)}</p>");
        sb.Append("<table class='schedule'><tr><th>Stage</th><th>Target</th><th>Timing</th><th>Note</th></tr>");
        foreach (var line in schedule.Lines)
            sb.Append($"<tr><td>{encode(line.Stage)}</td><td>{money(line.TargetAmount)}</td><td>{encode(line.DueText)}</td><td>{encode(line.Note)}</td></tr>");
        sb.Append("</table></div>");
    }

    private static void AppendProjectContext(StringBuilder sb, CustomQuote quote, Func<string?, string> encode)
    {
        var rows = new List<(string Label, string Value)>
        {
            ("Occasion", quote.Occasion ?? string.Empty),
            ("Required by", quote.RequiredBy.HasValue ? quote.RequiredBy.Value.ToString("dd MMM yyyy") : string.Empty),
            ("Ring size", quote.RingSize ?? string.Empty),
            ("Budget / target", quote.BudgetRange ?? string.Empty),
            ("Preferred metal", quote.PreferredMetal ?? string.Empty),
            ("Preferred stone", quote.PreferredStone ?? string.Empty),
            ("Customer brief", quote.CustomerNotes ?? string.Empty)
        }
        .Where(x => !string.IsNullOrWhiteSpace(x.Value))
        .ToList();

        if (rows.Count == 0)
            return;

        sb.Append("<div class='payment'><h3>Project details</h3><table class='costs'>");
        foreach (var row in rows)
            sb.Append($"<tr><td>{encode(row.Label)}</td><td>{encode(row.Value).Replace("\n", "<br>")}</td></tr>");
        sb.Append("</table></div>");
    }

    private static void AppendOption(
        StringBuilder sb,
        QuoteOption option,
        IReadOnlyDictionary<int, List<QuoteOptionExternalDiamondLink>>? externalDiamondLinks,
        Func<string?, string> encode,
        Func<decimal, string> money)
    {
        var cls = option.IsRecommended ? "option recommended" : "option";
        sb.Append($"<section class='{cls}'><div class='grid'><div><h2>{encode(option.OptionName)}</h2>{(option.IsRecommended ? "<span class='badge'>Recommended option</span>" : "")}</div><div class='price right'>{money(option.TotalPrice)}</div></div>");

        var imageDataUri = TryCreateImageDataUri(option.ImagePath);
        if (!string.IsNullOrWhiteSpace(imageDataUri))
            sb.Append($"<img class='option-img' src='{imageDataUri}' alt='{encode(option.OptionName)} design image'>");

        sb.Append($"<p>{encode(option.Description).Replace("\n", "<br>")}</p><div class='grid'><p><b>Metal</b><br>{encode(option.MetalDetails)}</p><p><b>Stone</b><br>{encode(option.StoneDetails)}</p></div>");

        if (externalDiamondLinks != null && externalDiamondLinks.TryGetValue(option.Id, out var linkedDiamonds) && linkedDiamonds.Count > 0)
        {
            sb.Append("<h3>External supplier diamond option</h3><ul>");
            foreach (var diamond in linkedDiamonds)
            {
                sb.Append($"<li><b>{encode(diamond.DiamondSummarySnapshot)}</b> &middot; {encode(diamond.SourceSystemSnapshot)} ID {encode(diamond.SupplierDiamondIdSnapshot)} &middot; Lab {encode(diamond.LabSnapshot)} &middot; Cert {encode(diamond.CertificateNumberSnapshot)} &middot; Status {encode(diamond.LinkStatus)}");
                if (!string.IsNullOrWhiteSpace(diamond.VideoUrlSnapshot))
                    sb.Append($" &middot; <a href='{encode(diamond.VideoUrlSnapshot)}'>Video</a>");
                if (!string.IsNullOrWhiteSpace(diamond.CertificateUrlSnapshot))
                    sb.Append($" &middot; <a href='{encode(diamond.CertificateUrlSnapshot)}'>Certificate</a>");
                sb.Append("</li>");
            }
            sb.Append("</ul>");
        }

        sb.Append("<table class='costs'>");
        var labour = option.LabourHours * option.LabourRate;
        var rows = new[]
        {
            ("Labour", labour),
            ("Metal", option.MetalCost),
            ("Stones", option.StoneCost),
            ("Setting", option.SettingCost),
            ("Findings", option.FindingsCost),
            ("Other", option.OtherCost)
        };

        foreach (var row in rows)
        {
            if (row.Item2 != 0m)
                sb.Append($"<tr><td>{row.Item1}</td><td class='right'>{money(row.Item2)}</td></tr>");
        }

        sb.Append($"<tr><td><b>Total</b></td><td class='right'><b>{money(option.TotalPrice)}</b></td></tr></table></section>");
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
