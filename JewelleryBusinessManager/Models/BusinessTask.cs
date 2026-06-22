using System.ComponentModel.DataAnnotations.Schema;

namespace JewelleryBusinessManager.Models;

public class BusinessTask : BaseEntity
{
    public string TaskCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public BusinessTaskCategory Category { get; set; } = BusinessTaskCategory.General;
    public BusinessTaskStatus Status { get; set; } = BusinessTaskStatus.ToDo;
    public BusinessTaskPriority Priority { get; set; } = BusinessTaskPriority.Normal;
    public DateTime? DueDate { get; set; }
    public DateTime? ReminderDate { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int? CustomerId { get; set; }
    public int? JobId { get; set; }
    public int? JewelleryItemId { get; set; }
    public int? StoneId { get; set; }
    public int? MarketEventId { get; set; }
    public int? ProductionBatchId { get; set; }
    public int? PurchaseOrderId { get; set; }
    public string? Description { get; set; }
    public string? FollowUpNotes { get; set; }
    public bool ShowOnDashboard { get; set; } = true;

    [NotMapped]
    public bool IsOpen => Status != BusinessTaskStatus.Completed && Status != BusinessTaskStatus.Cancelled;
    [NotMapped]
    public bool IsOverdue => IsOpen && DueDate.HasValue && DueDate.Value.Date < DateTime.Today;

    public override string ToString()
    {
        var code = string.IsNullOrWhiteSpace(TaskCode) ? $"Task #{Id}" : TaskCode;
        return string.IsNullOrWhiteSpace(Title) ? code : $"{code} {Title}".Trim();
    }
}
