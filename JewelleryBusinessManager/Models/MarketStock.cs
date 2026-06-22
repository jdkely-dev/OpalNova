namespace JewelleryBusinessManager.Models;

public class MarketStock : BaseEntity
{
    public int MarketEventId { get; set; }
    public int JewelleryItemId { get; set; }
    public bool Packed { get; set; }
    public DateTime? PackedAt { get; set; }
    public bool SoldAtMarket { get; set; }
    public DateTime? SoldAt { get; set; }
    public bool ReturnedToStock { get; set; }
    public decimal SalePrice { get; set; }
    public string? PaymentMethodText { get; set; }
    public int? SaleId { get; set; }
    public string? Notes { get; set; }
}
