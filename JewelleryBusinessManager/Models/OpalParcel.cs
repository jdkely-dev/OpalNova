namespace JewelleryBusinessManager.Models;

public class OpalParcel : BaseEntity
{
    public string ParcelCode { get; set; } = string.Empty;
    public int? SupplierId { get; set; }
    public DateTime PurchaseDate { get; set; } = DateTime.Today;
    public decimal TotalCost { get; set; }
    public decimal StartingWeightCarats { get; set; }
    public string? Origin { get; set; }
    public decimal ExpectedYieldCarats { get; set; }
    public decimal ActualYieldCarats { get; set; }
    public string Status { get; set; } = "Uncut";
    public string? Notes { get; set; }
    public override string ToString() => string.IsNullOrWhiteSpace(ParcelCode) ? $"Opal Parcel #{Id}" : ParcelCode;
}
