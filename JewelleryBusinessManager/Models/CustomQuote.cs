namespace JewelleryBusinessManager.Models;

public class CustomQuote : BaseEntity
{
    public string QuoteCode { get; set; } = string.Empty;
    public int? CustomerId { get; set; }
    public int? LinkedJobId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = "Draft";
    public DateTime QuoteDate { get; set; } = DateTime.Today;
    public DateTime? ValidUntil { get; set; } = DateTime.Today.AddDays(14);
    public int? AcceptedOptionId { get; set; }
    public decimal DepositPercent { get; set; } = 30m;
    public string? Introduction { get; set; }
    public string? CustomerNotes { get; set; }
    public string? InternalNotes { get; set; }
    public string? Terms { get; set; }
    public override string ToString() => string.IsNullOrWhiteSpace(Title) ? QuoteCode : $"{QuoteCode} {Title}".Trim();
}
