namespace JewelleryBusinessManager.Models;

public class PurchaseOrderItem : BaseEntity
{
    public int PurchaseOrderId { get; set; }
    public int? MaterialId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public string UnitType { get; set; } = string.Empty;
    public decimal OrderedQuantity { get; set; } = 1;
    public decimal ReceivedQuantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal LineTotal { get; set; }
    public string Notes { get; set; } = string.Empty;

    public decimal OutstandingQuantity => Math.Max(0, OrderedQuantity - ReceivedQuantity);

    public override string ToString() => $"{ItemName} x {OrderedQuantity} @ {UnitCost:C}".Trim();
}
