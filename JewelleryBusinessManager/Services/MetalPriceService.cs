using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using JewelleryBusinessManager.Models;

namespace JewelleryBusinessManager.Services;

public sealed class MetalPriceSnapshot
{
    public decimal GoldPerGram { get; init; }
    public decimal SilverPerGram { get; init; }
    public decimal PlatinumPerGram { get; init; }
    public decimal PalladiumPerGram { get; init; }
    public string Currency { get; init; } = "AUD";
    public DateTime UpdatedAt { get; init; } = DateTime.Now;
    public string SourceNote { get; init; } = string.Empty;
}

public static class MetalPriceService
{
    private const decimal GramsPerTroyOunce = 31.1034768m;
    private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromSeconds(15) };

    public static string[] SupportedCurrencies { get; } = ["AUD", "USD", "NZD", "EUR", "GBP", "CAD"];
    public static string[] SupportedProviders { get; } = ["GoldAPI.net", "GoldAPI.io"];

    public static decimal TroyOunceToGram(decimal pricePerTroyOunce)
        => pricePerTroyOunce <= 0 ? 0 : Math.Round(pricePerTroyOunce / GramsPerTroyOunce, 4);

    public static decimal EstimateMetalCost(string metalName, decimal grams, BusinessSettings settings)
    {
        if (grams <= 0) return 0;
        var price = GetPricePerGram(metalName, settings);
        return Math.Round(price * grams, 2);
    }

    public static decimal GetPricePerGram(string? metalName, BusinessSettings settings)
    {
        var metal = (metalName ?? string.Empty).Trim().ToLowerInvariant();
        if (metal.Contains("platinum")) return settings.PlatinumPricePerGram;
        if (metal.Contains("palladium")) return settings.PalladiumPricePerGram;
        if (metal.Contains("silver") || metal.Contains("sterling")) return settings.SilverPricePerGram;
        if (metal.Contains("gold") || metal.Contains("9ct") || metal.Contains("14ct") || metal.Contains("18ct") || metal.Contains("22ct"))
        {
            var pureGold = settings.GoldPricePerGram;
            if (metal.Contains("9ct") || metal.Contains("9k")) return Math.Round(pureGold * 0.375m, 4);
            if (metal.Contains("10ct") || metal.Contains("10k")) return Math.Round(pureGold * 0.4167m, 4);
            if (metal.Contains("14ct") || metal.Contains("14k")) return Math.Round(pureGold * 0.585m, 4);
            if (metal.Contains("18ct") || metal.Contains("18k")) return Math.Round(pureGold * 0.75m, 4);
            if (metal.Contains("22ct") || metal.Contains("22k")) return Math.Round(pureGold * 0.916m, 4);
            return pureGold;
        }
        return 0;
    }

    public static async Task<MetalPriceSnapshot> RefreshFromGoldApiAsync(BusinessSettings settings)
    {
        return await RefreshLivePricesAsync(settings).ConfigureAwait(false);
    }

    public static async Task<MetalPriceSnapshot> RefreshLivePricesAsync(BusinessSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.MetalPriceApiKey))
            throw new InvalidOperationException("Add your metal price API key before refreshing live metal prices. You can still enter prices manually.");

        var provider = NormaliseProvider(settings.MetalPriceProvider);
        var currency = string.IsNullOrWhiteSpace(settings.MetalPriceCurrency) ? "AUD" : settings.MetalPriceCurrency.Trim().ToUpperInvariant();
        var metals = new[] { "XAU", "XAG", "XPT", "XPD" };
        var values = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        var failures = new List<string>();

        foreach (var metal in metals)
        {
            try
            {
                var pricePerTroyOunce = provider == "GoldAPI.io"
                    ? await FetchGoldApiIoPriceAsync(metal, currency, settings.MetalPriceApiKey).ConfigureAwait(false)
                    : await FetchGoldApiNetPriceAsync(metal, currency, settings.MetalPriceApiKey).ConfigureAwait(false);
                values[metal] = TroyOunceToGram(pricePerTroyOunce);
            }
            catch (Exception ex)
            {
                failures.Add($"{metal}: {ex.Message}");
            }
        }

        if (values.Count == 0)
            throw new InvalidOperationException($"No live metal prices were returned by {provider}. Check that the provider is correct for your API key, then check the key, currency and account limits. Details: {string.Join(" | ", failures)}");

        return new MetalPriceSnapshot
        {
            GoldPerGram = values.GetValueOrDefault("XAU", settings.GoldPricePerGram),
            SilverPerGram = values.GetValueOrDefault("XAG", settings.SilverPricePerGram),
            PlatinumPerGram = values.GetValueOrDefault("XPT", settings.PlatinumPricePerGram),
            PalladiumPerGram = values.GetValueOrDefault("XPD", settings.PalladiumPricePerGram),
            Currency = currency,
            UpdatedAt = DateTime.Now,
            SourceNote = $"{provider} spot prices converted from per troy ounce to per gram at {DateTime.Now.ToString("g", CultureInfo.CurrentCulture)}. " +
                         (failures.Count == 0 ? "All metals refreshed." : $"Some metals kept their previous/manual values: {string.Join(" | ", failures)}")
        };
    }

    private static string NormaliseProvider(string? provider)
    {
        var value = (provider ?? string.Empty).Trim();
        if (value.Equals("GoldAPI.io", StringComparison.OrdinalIgnoreCase) || value.Equals("GoldAPI IO", StringComparison.OrdinalIgnoreCase))
            return "GoldAPI.io";
        return "GoldAPI.net";
    }

    private static async Task<decimal> FetchGoldApiNetPriceAsync(string metal, string currency, string apiKey)
    {
        var escapedKey = Uri.EscapeDataString(apiKey.Trim());
        var url = $"https://app.goldapi.net/price/{metal}/{currency}?x-api-key={escapedKey}";
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("User-Agent", "OPALNOVA/1.9.1");

        using var response = await Client.SendAsync(request).ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"GoldAPI.net returned {(int)response.StatusCode}. {SummariseResponse(body)}");
        return ExtractPrice(body);
    }

    private static async Task<decimal> FetchGoldApiIoPriceAsync(string metal, string currency, string apiKey)
    {
        var url = $"https://www.goldapi.io/api/{metal}/{currency}";
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("x-access-token", apiKey.Trim());
        request.Headers.Add("User-Agent", "OPALNOVA/1.9.1");

        using var response = await Client.SendAsync(request).ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"GoldAPI.io returned {(int)response.StatusCode}. {SummariseResponse(body)}");
        return ExtractPrice(body);
    }

    private static string SummariseResponse(string body)
    {
        if (string.IsNullOrWhiteSpace(body)) return "The API returned an empty error response.";
        var cleaned = body.Replace("\r", " ").Replace("\n", " ").Trim();
        return cleaned.Length <= 400 ? cleaned : cleaned[..400] + "...";
    }

    private static decimal ExtractPrice(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        foreach (var name in new[] { "price", "ask", "mid", "bid" })
        {
            if (root.TryGetProperty(name, out var element) && TryReadDecimal(element, out var value) && value > 0)
                return value;
        }
        throw new InvalidOperationException("The metal price response did not contain a usable price, ask, mid or bid field.");
    }

    private static bool TryReadDecimal(JsonElement element, out decimal value)
    {
        value = 0;
        if (element.ValueKind == JsonValueKind.Number && element.TryGetDecimal(out value)) return true;
        if (element.ValueKind == JsonValueKind.String && decimal.TryParse(element.GetString(), NumberStyles.Number, CultureInfo.InvariantCulture, out value)) return true;
        return false;
    }
}
