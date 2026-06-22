namespace JewelleryBusinessManager.Models;

public class Payment : BaseEntity
{
    public int? CustomerId { get; set; }
    public int? JobId { get; set; }
    public int? SaleId { get; set; }
    public DateTime PaymentDate { get; set; } = DateTime.Today;
    public decimal Amount { get; set; }
    public PaymentMethod Method { get; set; } = PaymentMethod.Card;
    public string? Reference { get; set; }
    public string? Notes { get; set; }
}
