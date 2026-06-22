namespace JewelleryBusinessManager.Models;

public class QuoteOptionStoneLink : BaseEntity
{
    public int QuoteOptionId { get; set; }
    public int StoneId { get; set; }
    public string StoneCodeSnapshot { get; set; } = string.Empty;
    public string DescriptionSnapshot { get; set; } = string.Empty;
    public decimal UnitCost { get; set; }
    public string ReservationStatus { get; set; } = "Proposed";
    public override string ToString() => $"{StoneCodeSnapshot} {DescriptionSnapshot} — {UnitCost:C} [{ReservationStatus}]".Trim();
}
