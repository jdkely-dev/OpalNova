namespace JewelleryBusinessManager.Models;

public class QuoteOptionMaterialLink : BaseEntity
{
    public int QuoteOptionId { get; set; }
    public int MaterialId { get; set; }
    public string MaterialCodeSnapshot { get; set; } = string.Empty;
    public string MaterialNameSnapshot { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public string UnitTypeSnapshot { get; set; } = string.Empty;
    public string ReservationStatus { get; set; } = "Proposed";
    public decimal LineCost => Quantity * UnitCost;
    public override string ToString() => $"{MaterialCodeSnapshot} {MaterialNameSnapshot} — {Quantity:0.###} {UnitTypeSnapshot} × {UnitCost:C} = {LineCost:C} [{ReservationStatus}]".Trim();
}
