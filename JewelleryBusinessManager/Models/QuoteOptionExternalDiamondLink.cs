namespace JewelleryBusinessManager.Models;

public class QuoteOptionExternalDiamondLink : BaseEntity
{
    public int QuoteOptionId { get; set; }
    public int ExternalDiamondId { get; set; }
    public string SourceSystemSnapshot { get; set; } = "Nivoda";
    public string SupplierDiamondIdSnapshot { get; set; } = string.Empty;
    public string DiamondSummarySnapshot { get; set; } = string.Empty;
    public string LabSnapshot { get; set; } = string.Empty;
    public string CertificateNumberSnapshot { get; set; } = string.Empty;
    public decimal SupplierPrice { get; set; }
    public string Currency { get; set; } = "AUD";
    public decimal RetailPriceSnapshot { get; set; }
    public string VideoUrlSnapshot { get; set; } = string.Empty;
    public string CertificateUrlSnapshot { get; set; } = string.Empty;
    public string LinkStatus { get; set; } = "Proposed";

    public override string ToString()
    {
        var price = SupplierPrice > 0 ? $" - supplier {SupplierPrice:C}" : string.Empty;
        var cert = string.IsNullOrWhiteSpace(CertificateNumberSnapshot) ? string.Empty : $" Cert {CertificateNumberSnapshot}";
        return $"{DiamondSummarySnapshot}{cert}{price} [{LinkStatus}]".Trim();
    }
}
