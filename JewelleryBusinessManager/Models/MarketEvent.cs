namespace JewelleryBusinessManager.Models;

public class MarketEvent : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public DateTime EventDate { get; set; } = DateTime.Today;
    public string? Location { get; set; }
    public decimal StallFee { get; set; }
    public decimal OpeningFloat { get; set; }
    public decimal CashSales { get; set; }
    public decimal CardSales { get; set; }
    public decimal OtherSales { get; set; }
    public decimal TotalSales { get; set; }
    public decimal TravelCost { get; set; }
    public decimal DisplayCost { get; set; }
    public decimal OtherCosts { get; set; }
    public int ItemsPacked { get; set; }
    public int ItemsSold { get; set; }
    public int ItemsReturned { get; set; }
    public DateTime? LastReconciledAt { get; set; }
    public string? PackingChecklist { get; set; }
    public string? ReconciliationNotes { get; set; }
    public string? Notes { get; set; }
    public decimal TotalCosts => StallFee + TravelCost + DisplayCost + OtherCosts;
    public decimal TotalTakings => CashSales + CardSales + OtherSales;
    public decimal NetMarketProfit => (TotalTakings > 0 ? TotalTakings : TotalSales) - TotalCosts;
    public override string ToString() => string.IsNullOrWhiteSpace(Name) ? $"Market Event #{Id}" : $"{EventDate:d} {Name}";
}
