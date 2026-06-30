using System.IO;
using System.Text.Json;
using JewelleryBusinessManager.Models;

namespace JewelleryBusinessManager.Services;

public static class BusinessSettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static string AppDataFolder => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "JewelleryBusinessManager");

    public static string SettingsPath => Path.Combine(AppDataFolder, "business-settings.json");

    public static BusinessSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsPath))
                return CreateDefaultSettings();

            var json = File.ReadAllText(SettingsPath);
            var settings = JsonSerializer.Deserialize<BusinessSettings>(json, JsonOptions);
            return settings ?? CreateDefaultSettings();
        }
        catch
        {
            // If the settings file is damaged, keep the app usable with safe defaults.
            return CreateDefaultSettings();
        }
    }

    public static void Save(BusinessSettings settings)
    {
        Directory.CreateDirectory(AppDataFolder);
        File.WriteAllText(SettingsPath, JsonSerializer.Serialize(settings, JsonOptions));
    }

    public static string GetPrintoutFolder()
    {
        var settings = Load();
        var folder = string.IsNullOrWhiteSpace(settings.PrintoutFolder)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "JewelleryBusinessManager", "Printouts")
            : settings.PrintoutFolder;
        Directory.CreateDirectory(folder);
        return folder;
    }

    public static string GetBackupFolder()
    {
        var settings = Load();
        var folder = string.IsNullOrWhiteSpace(settings.BackupFolder)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "JewelleryBusinessManager", "Backups")
            : settings.BackupFolder;
        Directory.CreateDirectory(folder);
        return folder;
    }

    public static BusinessSettings CreateDefaultSettings()
    {
        return new BusinessSettings
        {
            BusinessName = "Your Jewellery Business",
            DefaultLabourRate = 60m,
            DefaultProfitMarginPercent = 65m,
            GstRegistered = false,
            GstRatePercent = 10m,
            TaxLabel = "GST",
            DocumentFooterText = "Thank you for supporting handmade jewellery.",
            TermsAndConditions = "Deposits may be required before work begins. Custom order timelines depend on material availability and customer approval. Final balance is due before pickup or shipping.",
            ProposalEmailSubjectTemplate = "Your jewellery proposal - {QuoteCode}",
            ProposalEmailMessageTemplate = "Hi {CustomerName},\n\nThank you for the opportunity to prepare this jewellery proposal.\n\nYou can review the attached proposal or open it from this link:\n{ProposalLink}\n\nPayment schedule: {PaymentSchedule}\n\nPlease let me know which option you prefer, or if you would like any changes.\n\nKind regards,\n{BusinessName}",
            BackupFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "JewelleryBusinessManager", "Backups"),
            PrintoutFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "JewelleryBusinessManager", "Printouts"),
            MetalPriceProvider = "GoldAPI",
            MetalPriceCurrency = "AUD",
            MetalPriceSourceNote = "Manual prices. Add an API key in Metal Prices to refresh live spot pricing.",
            NivodaEndpoint = "https://intg-customer-staging.nivodaapi.net/api/diamonds",
            NivodaGraphiQlUrl = "https://intg-customer-staging.nivodaapi.net/api/diamonds-graphiql",
            ExternalDiamondDefaultCurrency = "AUD",
            ExternalDiamondDefaultMarkupPercent = 35m,
            NivodaLastConnectionNote = "Not tested yet."
        };
    }
}
