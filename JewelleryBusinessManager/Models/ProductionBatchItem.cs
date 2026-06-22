namespace JewelleryBusinessManager.Models;

public class ProductionBatchItem : BaseEntity
{
    public int ProductionBatchId { get; set; }
    public int? JewelleryItemId { get; set; }
    public int? StoneId { get; set; }
    public int? JobId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public string ItemType { get; set; } = string.Empty;
    public decimal PlannedQuantity { get; set; } = 1;
    public decimal CompletedQuantity { get; set; }
    public decimal EstimatedCost { get; set; }
    public decimal EstimatedRetailValue { get; set; }
    public string Status { get; set; } = "Planned";
    public string? Notes { get; set; }
    public decimal ProgressPercent => PlannedQuantity <= 0 ? 0 : Math.Min(1m, CompletedQuantity / PlannedQuantity);
    public decimal EstimatedProfit => EstimatedRetailValue - EstimatedCost;
    public override string ToString() => string.IsNullOrWhiteSpace(ItemName) ? $"Batch Item #{Id}" : $"{ItemName} ({Status})";
}
