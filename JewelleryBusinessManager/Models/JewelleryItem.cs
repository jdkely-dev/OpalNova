namespace JewelleryBusinessManager.Models;

public class JewelleryItem : BaseEntity
{
    public string StockCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public JewelleryType Type { get; set; } = JewelleryType.Other;
    public string? Metal { get; set; }
    public int? MainStoneId { get; set; }
    public string? RingSize { get; set; }
    public string? ChainLength { get; set; }
    public string? Dimensions { get; set; }
    public decimal MaterialCost { get; set; }
    public decimal LabourHours { get; set; }
    public decimal LabourRate { get; set; }
    public decimal OtherCost { get; set; }
    public decimal RetailPrice { get; set; }
    public decimal WholesalePrice { get; set; }
    public StockStatus Status { get; set; } = StockStatus.InStock;
    public DateTime? DateMade { get; set; }
    public string? Notes { get; set; }
    public decimal TotalCost => MaterialCost + OtherCost + (LabourHours * LabourRate);
    public decimal EstimatedProfit => RetailPrice - TotalCost;
    public override string ToString() => string.IsNullOrWhiteSpace(Name) ? $"Jewellery Item #{Id}" : $"{StockCode} {Name}".Trim();
}
