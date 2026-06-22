namespace JewelleryBusinessManager.Models;

public class Stone : BaseEntity
{
    public string StoneCode { get; set; } = string.Empty;
    public int? OpalParcelId { get; set; }
    public string StoneType { get; set; } = "Opal";
    public decimal WeightCarats { get; set; }
    public string? Shape { get; set; }
    public string? Dimensions { get; set; }
    public string? BodyTone { get; set; }
    public string? Brightness { get; set; }
    public string? Pattern { get; set; }
    public string? BaseColour { get; set; }
    public string? MainColours { get; set; }
    public decimal EstimatedValue { get; set; }
    public StoneStatus Status { get; set; } = StoneStatus.Loose;
    public string? Notes { get; set; }
    public override string ToString() => string.IsNullOrWhiteSpace(StoneCode) ? $"{StoneType} #{Id}" : $"{StoneCode} {StoneType}".Trim();
}
