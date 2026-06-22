namespace JewelleryBusinessManager.Models;

public class Job : BaseEntity
{
    public string JobCode { get; set; } = string.Empty;
    public int? CustomerId { get; set; }
    public string JobTitle { get; set; } = string.Empty;
    public JobType Type { get; set; } = JobType.CustomOrder;
    public JobStatus Status { get; set; } = JobStatus.Enquiry;
    public DateTime DateReceived { get; set; } = DateTime.Today;
    public DateTime? DueDate { get; set; }
    public decimal QuoteAmount { get; set; }
    public decimal DepositPaid { get; set; }
    public decimal BalanceOwing { get; set; }
    public decimal LabourHours { get; set; }
    public decimal LabourCost { get; set; }
    public decimal MaterialCost { get; set; }
    public decimal FinalPrice { get; set; }
    public string? DesignNotes { get; set; }
    public string? CustomerApprovalNotes { get; set; }
    public string? InternalNotes { get; set; }
    public override string ToString() => string.IsNullOrWhiteSpace(JobTitle) ? $"Job #{Id}" : $"{JobCode} {JobTitle}".Trim();
}
