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
    public string? Occasion { get; set; }
    public DateTime? RequiredBy { get; set; }
    public string? RingSize { get; set; }
    public string? BudgetRange { get; set; }
    public string? PreferredMetal { get; set; }
    public string? PreferredStone { get; set; }
    public int? AcceptedOptionId { get; set; }
    public decimal DepositPercent { get; set; } = 30m;
    public string ProposalStatus { get; set; } = "Not Sent";
    public DateTime? ProposalLastGeneratedAt { get; set; }
    public DateTime? ProposalSentAt { get; set; }
    public DateTime? ProposalFollowUpDueAt { get; set; }
    public string? ProposalLastPath { get; set; }
    public string? ProposalEmailTo { get; set; }
    public string? ProposalEmailSubject { get; set; }
    public string? ProposalEmailMessage { get; set; }
    public string? Introduction { get; set; }
    public string? CustomerNotes { get; set; }
    public string? InternalNotes { get; set; }
    public string? Terms { get; set; }
    public override string ToString() => string.IsNullOrWhiteSpace(Title) ? QuoteCode : $"{QuoteCode} {Title}".Trim();
}
