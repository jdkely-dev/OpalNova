namespace JewelleryBusinessManager.Models;

public class OnlineListing : BaseEntity
{
    public int? JewelleryItemId { get; set; }
    public OnlineListingStatus Status { get; set; } = OnlineListingStatus.NotStarted;
    public ListingPhotoStatus PhotoStatus { get; set; } = ListingPhotoStatus.NotStarted;
    public string Platform { get; set; } = "Website";
    public string? ListingUrl { get; set; }
    public DateTime? ListingDate { get; set; }
    public string? SeoTitle { get; set; }
    public string? ShortDescription { get; set; }
    public string? LongDescription { get; set; }
    public string? InstagramCaption { get; set; }
    public string? Hashtags { get; set; }
    public bool PhotosDone { get; set; }
    public bool DescriptionDone { get; set; }
    public bool PriceChecked { get; set; }
    public bool ListedOnline { get; set; }
    public bool SharedToSocial { get; set; }
    public string? Notes { get; set; }

    public override string ToString()
    {
        var title = string.IsNullOrWhiteSpace(SeoTitle) ? $"Listing #{Id}" : SeoTitle;
        return $"{Platform} — {title}".Trim();
    }
}
