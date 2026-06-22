namespace JewelleryBusinessManager.Models;

public class ProductionBatch : BaseEntity
{
    public string BatchCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? CollectionName { get; set; }
    public ProductionBatchStatus Status { get; set; } = ProductionBatchStatus.Planned;
    public DateTime StartDate { get; set; } = DateTime.Today;
    public DateTime? TargetCompletionDate { get; set; }
    public int? MarketEventId { get; set; }
    public int PlannedPieces { get; set; }
    public int CompletedPieces { get; set; }
    public decimal EstimatedMaterialCost { get; set; }
    public decimal EstimatedLabourHours { get; set; }
    public decimal EstimatedRetailValue { get; set; }
    public string? Notes { get; set; }
    public decimal ProgressPercent => PlannedPieces <= 0 ? 0 : Math.Min(1m, CompletedPieces / (decimal)PlannedPieces);
    public decimal EstimatedTotalCost => EstimatedMaterialCost;
    public decimal EstimatedProfit => EstimatedRetailValue - EstimatedMaterialCost;
    public override string ToString() => string.IsNullOrWhiteSpace(Name) ? $"Production Batch #{Id}" : $"{BatchCode} {Name}".Trim();
}
