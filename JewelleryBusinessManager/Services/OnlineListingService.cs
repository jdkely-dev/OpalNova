using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using JewelleryBusinessManager.Data;
using JewelleryBusinessManager.Models;

namespace JewelleryBusinessManager.Services;

public static class OnlineListingService
{
    private static string PrintoutFolder => BusinessSettingsService.GetPrintoutFolder();

    public static OnlineListing CreateOrUpdateListing(AppDbContext db, JewelleryItem item)
    {
        var listing = db.OnlineListings.FirstOrDefault(x => x.JewelleryItemId == item.Id);
        if (listing == null)
        {
            listing = new OnlineListing
            {
                JewelleryItemId = item.Id,
                Platform = "Website",
                Status = item.Status == StockStatus.NeedsPhotos ? OnlineListingStatus.NeedsPhotos : OnlineListingStatus.NotStarted,
                PhotoStatus = item.Status == StockStatus.NeedsPhotos ? ListingPhotoStatus.NeedsPhotos : ListingPhotoStatus.NotStarted,
                Notes = $"Created from jewellery stock item {item.StockCode} {item.Name}".Trim()
            };
            db.OnlineListings.Add(listing);
        }

        GenerateContent(db, listing, overwriteExisting: false);
        UpdateChecklistDerivedStatus(listing);
        return listing;
    }

    public static void GenerateContent(AppDbContext db, OnlineListing listing, bool overwriteExisting)
    {
        var item = listing.JewelleryItemId.HasValue ? db.JewelleryItems.Find(listing.JewelleryItemId.Value) : null;
        var stone = item?.MainStoneId.HasValue == true ? db.Stones.Find(item.MainStoneId.Value) : null;
        var name = item?.Name ?? "Jewellery piece";
        var type = item?.Type.ToString().ToLowerInvariant() ?? "jewellery piece";
        var metal = string.IsNullOrWhiteSpace(item?.Metal) ? "handmade metal" : item!.Metal!;
        var stoneText = stone == null ? "" : $" featuring {stone.StoneType} ({stone.WeightCarats:0.###} ct)";
        var opalDetails = stone == null ? "" : BuildStoneDetails(stone);
        var priceText = item == null || item.RetailPrice <= 0 ? "" : $" Listed price: {item.RetailPrice.ToString("C", CultureInfo.CurrentCulture)}.";

        var seoTitle = $"{name} | {metal} {type}{(stone == null ? "" : " with " + stone.StoneType)}";
        var shortDescription = $"Handmade {metal} {type}{stoneText}. {opalDetails}".Trim();
        var longDescription = new StringBuilder();
        longDescription.AppendLine(shortDescription);
        longDescription.AppendLine();
        longDescription.AppendLine("This piece is individually made and recorded in the studio inventory, with materials, stone details and pricing tracked for accurate stock control.");
        if (!string.IsNullOrWhiteSpace(item?.Dimensions)) longDescription.AppendLine($"Dimensions: {item.Dimensions}");
        if (!string.IsNullOrWhiteSpace(item?.RingSize)) longDescription.AppendLine($"Ring size: {item.RingSize}");
        if (!string.IsNullOrWhiteSpace(item?.ChainLength)) longDescription.AppendLine($"Chain length: {item.ChainLength}");
        if (stone != null) longDescription.AppendLine($"Stone: {stone.StoneType}, {stone.WeightCarats:0.###} ct, {stone.Shape}. {opalDetails}");
        longDescription.AppendLine(priceText);
        var caption = $"New piece ready for listing: {name}. Handmade {metal} {type}{stoneText}.".Trim();
        var hashtags = BuildHashtags(item, stone);

        if (overwriteExisting || string.IsNullOrWhiteSpace(listing.SeoTitle)) listing.SeoTitle = seoTitle;
        if (overwriteExisting || string.IsNullOrWhiteSpace(listing.ShortDescription)) listing.ShortDescription = shortDescription;
        if (overwriteExisting || string.IsNullOrWhiteSpace(listing.LongDescription)) listing.LongDescription = longDescription.ToString().Trim();
        if (overwriteExisting || string.IsNullOrWhiteSpace(listing.InstagramCaption)) listing.InstagramCaption = caption;
        if (overwriteExisting || string.IsNullOrWhiteSpace(listing.Hashtags)) listing.Hashtags = hashtags;
        listing.DescriptionDone = !string.IsNullOrWhiteSpace(listing.ShortDescription) && !string.IsNullOrWhiteSpace(listing.LongDescription);
        UpdateChecklistDerivedStatus(listing);
    }

    public static void UpdateChecklistDerivedStatus(OnlineListing listing)
    {
        if (listing.ListedOnline)
        {
            listing.Status = OnlineListingStatus.Listed;
            if (!listing.ListingDate.HasValue) listing.ListingDate = DateTime.Today;
            return;
        }

        if (!listing.PhotosDone)
        {
            listing.Status = OnlineListingStatus.NeedsPhotos;
            if (listing.PhotoStatus == ListingPhotoStatus.NotStarted)
                listing.PhotoStatus = ListingPhotoStatus.NeedsPhotos;
            return;
        }

        if (!listing.DescriptionDone)
        {
            listing.Status = OnlineListingStatus.NeedsDescription;
            return;
        }

        if (listing.PriceChecked)
            listing.Status = OnlineListingStatus.ReadyToList;
    }

    public static string CreateListingChecklist(OnlineListing listing)
    {
        Directory.CreateDirectory(PrintoutFolder);
        using var db = new AppDbContext();
        var item = listing.JewelleryItemId.HasValue ? db.JewelleryItems.Find(listing.JewelleryItemId.Value) : null;
        var stone = item?.MainStoneId.HasValue == true ? db.Stones.Find(item.MainStoneId.Value) : null;
        var path = Path.Combine(PrintoutFolder, SafeFileName($"ListingChecklist_{listing.Id}_{DateTime.Now:yyyyMMdd-HHmmss}.html"));

        var html = new StringBuilder();
        html.Append(HtmlHeader("Online Listing Checklist"));
        html.AppendLine("<section class='card'>");
        html.AppendLine("<h1>Online Listing Checklist</h1>");
        html.AppendLine(Row("Listing", listing.ToString()));
        html.AppendLine(Row("Platform", listing.Platform));
        html.AppendLine(Row("Status", listing.Status.ToString()));
        html.AppendLine(Row("Photo Status", listing.PhotoStatus.ToString()));
        html.AppendLine(Row("Jewellery Item", item?.ToString() ?? "Not linked"));
        html.AppendLine(Row("Stone", stone?.ToString() ?? "Not linked"));
        html.AppendLine("<h2>Checklist</h2>");
        html.AppendLine(Check("Photos complete", listing.PhotosDone));
        html.AppendLine(Check("Description written", listing.DescriptionDone));
        html.AppendLine(Check("Price checked", listing.PriceChecked));
        html.AppendLine(Check("Listed online", listing.ListedOnline));
        html.AppendLine(Check("Shared to social", listing.SharedToSocial));
        html.AppendLine("<h2>Content</h2>");
        html.AppendLine(Row("SEO Title", listing.SeoTitle ?? string.Empty));
        html.AppendLine(NotesBlock("Short Description", listing.ShortDescription));
        html.AppendLine(NotesBlock("Long Description", listing.LongDescription));
        html.AppendLine(NotesBlock("Instagram Caption", listing.InstagramCaption));
        html.AppendLine(NotesBlock("Hashtags", listing.Hashtags));
        html.AppendLine(NotesBlock("Notes", listing.Notes));
        html.AppendLine("</section>");
        html.Append(HtmlFooter());
        File.WriteAllText(path, html.ToString());
        return path;
    }

    public static string CreateOnlineListingReport()
    {
        Directory.CreateDirectory(PrintoutFolder);
        using var db = new AppDbContext();
        var listings = db.OnlineListings.AsEnumerable().OrderBy(x => x.Status).ThenBy(x => x.Platform).ToList();
        var itemsNeedingListing = db.JewelleryItems.AsEnumerable()
            .Where(i => i.Status != StockStatus.Sold && !db.OnlineListings.Any(l => l.JewelleryItemId == i.Id))
            .OrderBy(i => i.Status).ThenBy(i => i.StockCode).ToList();
        var path = Path.Combine(PrintoutFolder, SafeFileName($"OnlineListingReport_{DateTime.Now:yyyyMMdd-HHmmss}.html"));

        var html = new StringBuilder();
        html.Append(HtmlHeader("Online Listing Report"));
        html.AppendLine("<section class='card'>");
        html.AppendLine("<h1>Online Listing Report</h1>");
        html.AppendLine(Row("Listings", listings.Count.ToString()));
        html.AppendLine(Row("Needs Photos", listings.Count(l => !l.PhotosDone || l.PhotoStatus == ListingPhotoStatus.NeedsPhotos).ToString()));
        html.AppendLine(Row("Needs Description", listings.Count(l => !l.DescriptionDone).ToString()));
        html.AppendLine(Row("Ready To List", listings.Count(l => l.Status == OnlineListingStatus.ReadyToList).ToString()));
        html.AppendLine(Row("Listed", listings.Count(l => l.Status == OnlineListingStatus.Listed).ToString()));
        html.AppendLine("<h2>Listing Pipeline</h2>");
        html.AppendLine("<table><tr><th>Platform</th><th>Status</th><th>Photos</th><th>Item</th><th>SEO Title</th><th>URL</th></tr>");
        foreach (var listing in listings)
        {
            var item = listing.JewelleryItemId.HasValue ? db.JewelleryItems.Find(listing.JewelleryItemId.Value) : null;
            html.AppendLine($"<tr><td>{Html(listing.Platform)}</td><td>{Html(listing.Status.ToString())}</td><td>{Html(listing.PhotoStatus.ToString())}</td><td>{Html(item?.ToString() ?? "")}</td><td>{Html(listing.SeoTitle ?? "")}</td><td>{Html(listing.ListingUrl ?? "")}</td></tr>");
        }
        html.AppendLine("</table>");
        html.AppendLine("<h2>Stock Without Listing Records</h2>");
        html.AppendLine("<table><tr><th>Stock</th><th>Name</th><th>Status</th><th>Retail</th></tr>");
        foreach (var item in itemsNeedingListing)
            html.AppendLine($"<tr><td>{Html(item.StockCode)}</td><td>{Html(item.Name)}</td><td>{Html(item.Status.ToString())}</td><td>{Money(item.RetailPrice)}</td></tr>");
        html.AppendLine("</table>");
        html.AppendLine("</section>");
        html.Append(HtmlFooter());
        File.WriteAllText(path, html.ToString());
        return path;
    }

    public static void OpenInDefaultApp(string path)
    {
        Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
    }

    private static string BuildStoneDetails(Stone stone)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(stone.BodyTone)) parts.Add($"body tone {stone.BodyTone}");
        if (!string.IsNullOrWhiteSpace(stone.Brightness)) parts.Add($"brightness {stone.Brightness}");
        if (!string.IsNullOrWhiteSpace(stone.Pattern)) parts.Add($"pattern {stone.Pattern}");
        if (!string.IsNullOrWhiteSpace(stone.MainColours)) parts.Add($"colours {stone.MainColours}");
        return parts.Count == 0 ? string.Empty : string.Join(", ", parts) + ".";
    }

    private static string BuildHashtags(JewelleryItem? item, Stone? stone)
    {
        var tags = new List<string> { "#handmadejewellery", "#australianjewellery", "#jewellerystudio" };
        if (item?.Type == JewelleryType.Ring) tags.Add("#handmadering");
        if (item?.Type == JewelleryType.Pendant) tags.Add("#opalpendant");
        if (!string.IsNullOrWhiteSpace(item?.Metal) && item.Metal.Contains("silver", StringComparison.OrdinalIgnoreCase)) tags.Add("#sterlingsilver");
        if (!string.IsNullOrWhiteSpace(item?.Metal) && item.Metal.Contains("gold", StringComparison.OrdinalIgnoreCase)) tags.Add("#goldjewellery");
        if (stone != null && stone.StoneType.Contains("opal", StringComparison.OrdinalIgnoreCase)) tags.Add("#opaljewellery");
        return string.Join(" ", tags.Distinct());
    }

    private static string HtmlHeader(string title)
    {
        var settings = BusinessSettingsService.Load();
        var html = new StringBuilder();
        html.AppendLine("<!doctype html><html><head><meta charset='utf-8'>");
        html.AppendLine($"<title>{Html(title)}</title>");
        html.AppendLine("<style>");
        html.AppendLine("body { font-family: Segoe UI, Arial, sans-serif; margin: 24px; color: #222; }");
        html.AppendLine("button { margin-bottom: 16px; padding: 8px 12px; }");
        html.AppendLine(".brand { display: flex; gap: 16px; border-bottom: 2px solid #222; padding-bottom: 12px; margin-bottom: 18px; }");
        html.AppendLine(".brand-title { margin: 0; }");
        html.AppendLine(".brand-details { font-size: 12px; color: #555; }");
        html.AppendLine(".logo { max-height: 70px; max-width: 160px; object-fit: contain; }");
        html.AppendLine(".card { border: 1px solid #ddd; padding: 18px; }");
        html.AppendLine(".row { display: flex; border-bottom: 1px solid #eee; padding: 6px 0; }");
        html.AppendLine(".key { width: 190px; font-weight: bold; }");
        html.AppendLine(".value { flex: 1; }");
        html.AppendLine(".notes { white-space: pre-wrap; border: 1px solid #ddd; min-height: 54px; padding: 8px; margin-bottom: 10px; }");
        html.AppendLine("table { border-collapse: collapse; width: 100%; margin-bottom: 18px; font-size: 12px; }");
        html.AppendLine("th, td { border: 1px solid #ddd; padding: 7px; text-align: left; }");
        html.AppendLine("th { background: #f2f2f2; }");
        html.AppendLine("@media print { button { display: none; } body { margin: 8mm; } .card { border: none; } }");
        html.AppendLine("</style></head><body><button onclick=\"window.print()\">Print</button>");
        html.AppendLine("<div class='brand'>");
        if (!string.IsNullOrWhiteSpace(settings.LogoPath) && File.Exists(settings.LogoPath))
            html.AppendLine($"<img class='logo' src='{Html(new Uri(settings.LogoPath).AbsoluteUri)}' alt='Business logo'>");
        html.AppendLine("<div>");
        html.AppendLine($"<h1 class='brand-title'>{Html(settings.BusinessName)}</h1>");
        html.AppendLine("<div class='brand-details'>");
        AppendIfPresent(html, "Owner", settings.OwnerName);
        AppendIfPresent(html, "ABN", settings.Abn);
        AppendIfPresent(html, "Phone", settings.Phone);
        AppendIfPresent(html, "Email", settings.Email);
        AppendIfPresent(html, "Website", settings.Website);
        html.AppendLine("</div></div></div>");
        return html.ToString();
    }

    private static string HtmlFooter()
    {
        var settings = BusinessSettingsService.Load();
        var html = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(settings.DocumentFooterText))
            html.AppendLine($"<footer>{Html(settings.DocumentFooterText)}</footer>");
        html.AppendLine("</body></html>");
        return html.ToString();
    }

    private static void AppendIfPresent(StringBuilder html, string label, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            html.AppendLine($"<div>{Html(label)}: {Html(value)}</div>");
    }

    private static string Row(string key, string value) => $"<div class='row'><div class='key'>{Html(key)}</div><div class='value'>{Html(value)}</div></div>";
    private static string Check(string label, bool done) => $"<p>{(done ? "☑" : "☐")} {Html(label)}</p>";
    private static string NotesBlock(string title, string? notes) => $"<h2>{Html(title)}</h2><div class='notes'>{Html(notes ?? string.Empty)}</div>";
    private static string Html(string value) => WebUtility.HtmlEncode(value);
    private static string Money(decimal amount) => amount.ToString("C", CultureInfo.CurrentCulture);
    private static string SafeFileName(string value)
    {
        foreach (var invalid in Path.GetInvalidFileNameChars())
            value = value.Replace(invalid, '_');
        return value;
    }
}
