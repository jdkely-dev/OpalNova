namespace JewelleryBusinessManager.Models;

public class Sale : BaseEntity
{
    public int? JewelleryItemId { get; set; }
    public int? JobId { get; set; }
    public int? CustomerId { get; set; }
    public DateTime SaleDate { get; set; } = DateTime.Today;
    public decimal SaleAmount { get; set; }
    public decimal CostOfGoods { get; set; }
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Card;
    public SaleLocation SaleLocation { get; set; } = SaleLocation.InPerson;
    public string? Notes { get; set; }
    public decimal Profit => SaleAmount - CostOfGoods;
    public override string ToString() => $"Sale #{Id} - {SaleDate:d} - {SaleAmount:C}";
}
