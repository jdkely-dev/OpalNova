namespace JewelleryBusinessManager.Models;

public class BusinessSettings
{
    public string BusinessName { get; set; } = "Your Jewellery Business";
    public string OwnerName { get; set; } = string.Empty;
    public string Abn { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Website { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string LogoPath { get; set; } = string.Empty;
    public decimal DefaultLabourRate { get; set; } = 60m;
    public decimal DefaultProfitMarginPercent { get; set; } = 65m;
    public bool GstRegistered { get; set; } = false;
    public decimal GstRatePercent { get; set; } = 10m;
    public string TaxLabel { get; set; } = "GST";
    public string DocumentFooterText { get; set; } = "Thank you for supporting handmade jewellery.";
    public string TermsAndConditions { get; set; } = "Deposits may be required before work begins. Custom order timelines depend on material availability and customer approval. Final balance is due before pickup or shipping.";
    public string ProposalEmailSubjectTemplate { get; set; } = "Your jewellery proposal - {QuoteCode}";
    public string ProposalEmailMessageTemplate { get; set; } = "Hi {CustomerName},\n\nThank you for the opportunity to prepare this jewellery proposal.\n\nYou can review the attached proposal or open it from this link:\n{ProposalLink}\n\nPayment schedule: {PaymentSchedule}\n\nPlease let me know which option you prefer, or if you would like any changes.\n\nKind regards,\n{BusinessName}";
    public string BackupFolder { get; set; } = string.Empty;
    public string PrintoutFolder { get; set; } = string.Empty;

    public string MetalPriceProvider { get; set; } = "GoldAPI";
    public string MetalPriceApiKey { get; set; } = string.Empty;
    public string MetalPriceCurrency { get; set; } = "AUD";
    public decimal GoldPricePerGram { get; set; } = 0m;
    public decimal SilverPricePerGram { get; set; } = 0m;
    public decimal PlatinumPricePerGram { get; set; } = 0m;
    public decimal PalladiumPricePerGram { get; set; } = 0m;
    public DateTime? MetalPricesLastUpdated { get; set; }
    public string MetalPriceSourceNote { get; set; } = "Manual prices. Add an API key in Metal Prices to refresh live spot pricing.";

    public string NivodaEndpoint { get; set; } = "https://intg-customer-staging.nivodaapi.net/api/diamonds";
    public string NivodaGraphiQlUrl { get; set; } = "https://intg-customer-staging.nivodaapi.net/api/diamonds-graphiql";
    public string NivodaEnvironmentName { get; set; } = "Staging";
    public string NivodaStagingReviewUrl { get; set; } = string.Empty;
    public string NivodaUsername { get; set; } = string.Empty;
    public string NivodaPassword { get; set; } = string.Empty;
    public decimal ExternalDiamondDefaultMarkupPercent { get; set; } = 35m;
    public string ExternalDiamondDefaultCurrency { get; set; } = "AUD";
    public DateTime? NivodaLastConnectionTestAt { get; set; }
    public string NivodaLastConnectionNote { get; set; } = "Not tested yet.";
}
