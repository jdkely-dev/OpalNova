namespace JewelleryBusinessManager.Models;

public class QuoteOption : BaseEntity
{
    public int CustomQuoteId { get; set; }
    public string OptionName { get; set; } = "Option A";
    public string? Description { get; set; }
    public string? MetalDetails { get; set; }
    public string? StoneDetails { get; set; }
    public string? ImagePath { get; set; }
    public decimal LabourHours { get; set; }
    public decimal LabourRate { get; set; }
    public decimal MetalCost { get; set; }
    public decimal StoneCost { get; set; }
    public decimal SettingCost { get; set; }
    public decimal FindingsCost { get; set; }
    public decimal OtherCost { get; set; }
    public decimal MarkupPercent { get; set; }
    public decimal GstPercent { get; set; }
    public decimal Subtotal { get; set; }
    public decimal TotalPrice { get; set; }
    public bool IsRecommended { get; set; }
    public override string ToString() => OptionName;
}
