namespace JewelleryBusinessManager.Models;

public class ExternalDiamond : BaseEntity
{
    public string SourceSystem { get; set; } = "Nivoda";
    public string SupplierDiamondId { get; set; } = string.Empty;
    public string Status { get; set; } = "Search Result";
    public string Shape { get; set; } = string.Empty;
    public decimal Carat { get; set; } = 0m;
    public string Color { get; set; } = string.Empty;
    public string Clarity { get; set; } = string.Empty;
    public string Cut { get; set; } = string.Empty;
    public string Lab { get; set; } = string.Empty;
    public string CertificateNumber { get; set; } = string.Empty;
    public bool IsLabGrown { get; set; } = true;
    public decimal SupplierPrice { get; set; } = 0m;
    public string Currency { get; set; } = "AUD";
    public decimal MarkupPercent { get; set; } = 35m;
    public decimal EstimatedRetailPrice { get; set; } = 0m;
    public string VideoUrl { get; set; } = string.Empty;
    public string CertificateUrl { get; set; } = string.Empty;
    public string Availability { get; set; } = string.Empty;
    public string SupplierReference { get; set; } = string.Empty;
    public DateTime? HoldRequestedAt { get; set; }
    public DateTime? HoldConfirmedAt { get; set; }
    public DateTime? HoldExpiresAt { get; set; }
    public DateTime? OrderRequestedAt { get; set; }
    public DateTime? OrderedAt { get; set; }
    public DateTime? ExpectedArrivalDate { get; set; }
    public DateTime? ReceivedAt { get; set; }
    public DateTime? ReleasedAt { get; set; }
    public DateTime LastSyncedAt { get; set; } = DateTime.Now;
    public string RawJson { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;

    public bool HasActiveHold => Status == "Hold Requested" || Status == "Hold Confirmed" || Status == "Hold Expiring";
    public bool IsHoldExpired => HoldExpiresAt.HasValue && HoldExpiresAt.Value < DateTime.Now && HasActiveHold;
    public bool IsOrderedNotReceived => (Status == "Order Requested" || Status == "Ordered") && !ReceivedAt.HasValue;

    public override string ToString()
    {
        if (Id <= 0 && SupplierPrice == 0m && string.IsNullOrWhiteSpace(SupplierDiamondId) && string.IsNullOrWhiteSpace(CertificateNumber))
            return "Select external diamond";

        var type = IsLabGrown ? "LG" : "Natural";
        var cert = string.IsNullOrWhiteSpace(CertificateNumber) ? SupplierDiamondId : CertificateNumber;
        return $"{type} {Shape} {Carat:0.###}ct {Color} {Clarity} {Lab} {cert} - {SupplierPrice:C}".Trim();
    }
}
