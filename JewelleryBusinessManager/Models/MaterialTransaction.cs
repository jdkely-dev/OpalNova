namespace JewelleryBusinessManager.Models;

public class MaterialTransaction : BaseEntity
{
    public int MaterialId { get; set; }
    public DateTime TransactionDate { get; set; } = DateTime.Today;
    public decimal QuantityChange { get; set; }
    public string Reason { get; set; } = string.Empty;
    public int? JobId { get; set; }
    public int? JewelleryItemId { get; set; }
    public string? Notes { get; set; }
}
