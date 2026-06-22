namespace JewelleryBusinessManager.Models;

public class PurchaseOrder : BaseEntity
{
    public string PurchaseOrderCode { get; set; } = string.Empty;
    public int? SupplierId { get; set; }
    public PurchaseOrderStatus Status { get; set; } = PurchaseOrderStatus.Draft;
    public DateTime OrderDate { get; set; } = DateTime.Today;
    public DateTime? ExpectedDeliveryDate { get; set; }
    public DateTime? ReceivedDate { get; set; }
    public decimal ShippingCost { get; set; }
    public decimal OtherCost { get; set; }
    public string SupplierReference { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;

    public decimal ItemsTotal { get; set; }
    public decimal TotalCost => ItemsTotal + ShippingCost + OtherCost;

    public override string ToString() => $"{PurchaseOrderCode} {Status} {TotalCost:C}".Trim();
}
