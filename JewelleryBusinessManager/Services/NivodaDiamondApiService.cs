using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using JewelleryBusinessManager.Models;

namespace JewelleryBusinessManager.Services;

public sealed class DiamondSearchRequest
{
    public string Shape { get; set; } = "ROUND";
    public bool IsLabGrown { get; set; } = true;
    public decimal MinCarat { get; set; } = 1.0m;
    public decimal MaxCarat { get; set; } = 1.5m;
    public string LabsCsv { get; set; } = "IGI,GIA";
    public int Limit { get; set; } = 20;
}

public sealed class DiamondSearchResponse
{
    public List<ExternalDiamond> Diamonds { get; set; } = new();
    public string RawJson { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;
}

public static class NivodaDiamondApiService
{
    public const string DefaultEndpoint = "https://intg-customer-staging.nivodaapi.net/api/diamonds";
    public const string DefaultGraphiQlUrl = "https://intg-customer-staging.nivodaapi.net/api/diamonds-graphiql";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    private const string AuthenticateQuery = @"
query Authenticate($username: String!, $password: String!) {
  authenticate {
    username_and_password(username: $username, password: $password) {
      token
    }
  }
}";

    public static async Task<string> TestConnectionAsync(BusinessSettings settings, CancellationToken cancellationToken = default)
    {
        var token = await AuthenticateAsync(settings, cancellationToken);
        using var document = await ExecuteRawAsync(settings, "query { __typename }", new { }, token, cancellationToken);
        if (document.RootElement.TryGetProperty("data", out var data))
        {
            var typeName = data.TryGetProperty("__typename", out var tn) ? tn.ToString() : "GraphQL endpoint";
            return $"Connection succeeded. Authenticated and endpoint responded as: {typeName}";
        }

        return "Connection succeeded and authentication token was received, but no data field was returned.";
    }

    public static async Task<DiamondSearchResponse> SearchDiamondsAsync(BusinessSettings settings, DiamondSearchRequest request, CancellationToken cancellationToken = default)
    {
        var token = await AuthenticateAsync(settings, cancellationToken);
        var labs = ParseLabs(request.LabsCsv);
        var query = BuildSearchQuery(request);

        using var document = await ExecuteRawAsync(settings, query, new { }, token, cancellationToken);
        var rawJson = JsonSerializer.Serialize(document.RootElement, JsonOptions);

        var diamonds = new List<ExternalDiamond>();
        var totalCount = 0;
        if (document.RootElement.TryGetProperty("data", out var data) &&
            data.TryGetProperty("diamonds_by_query", out var diamondsElement))
        {
            if (diamondsElement.TryGetProperty("total_count", out var countElement) && countElement.TryGetInt32(out var count))
                totalCount = count;

            foreach (var item in EnumerateDiamondOffers(diamondsElement))
            {
                var diamond = MapDiamond(item, settings, rawJson, request.IsLabGrown);
                if (labs.Count == 0 || labs.Contains(diamond.Lab.Trim().ToUpperInvariant()))
                    diamonds.Add(diamond);
            }
        }

        return new DiamondSearchResponse
        {
            Diamonds = diamonds,
            RawJson = rawJson,
            Note = diamonds.Count == 0
                ? $"The API responded but returned no diamond rows for this filter. Total server matches before local lab filtering: {totalCount}."
                : $"Loaded {diamonds.Count} diamond result(s). Total server matches before local lab filtering: {totalCount}."
        };
    }

    private static async Task<string> AuthenticateAsync(BusinessSettings settings, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(settings.NivodaUsername) || string.IsNullOrWhiteSpace(settings.NivodaPassword))
            throw new InvalidOperationException("Enter the Nivoda username and password before testing or searching.");

        using var document = await ExecuteRawAsync(
            settings,
            AuthenticateQuery,
            new { username = settings.NivodaUsername.Trim(), password = settings.NivodaPassword },
            bearerToken: null,
            cancellationToken);

        if (document.RootElement.TryGetProperty("data", out var data) &&
            data.TryGetProperty("authenticate", out var auth) &&
            auth.TryGetProperty("username_and_password", out var usernamePassword) &&
            usernamePassword.TryGetProperty("token", out var tokenElement))
        {
            var token = tokenElement.GetString();
            if (!string.IsNullOrWhiteSpace(token))
                return token;
        }

        throw new InvalidOperationException("Nivoda authentication succeeded at HTTP level, but no token was returned. Check the Nivoda username/password.");
    }

    private static string BuildSearchQuery(DiamondSearchRequest request)
    {
        var shape = EscapeGraphQlString(string.IsNullOrWhiteSpace(request.Shape) ? "ROUND" : request.Shape.Trim().ToUpperInvariant());
        var minCarat = Math.Max(0m, request.MinCarat).ToString("0.###", CultureInfo.InvariantCulture);
        var maxCarat = Math.Max(request.MinCarat, request.MaxCarat).ToString("0.###", CultureInfo.InvariantCulture);
        var labgrown = request.IsLabGrown ? "true" : "false";
        var limit = Math.Clamp(request.Limit, 1, 50);

        return $$"""
query {
  diamonds_by_query(
    query: {
      labgrown: {{labgrown}},
      shapes: ["{{shape}}"],
      sizes: [{ from: {{minCarat}}, to: {{maxCarat}} }]
    },
    offset: 0,
    limit: {{limit}},
    order: { type: price, direction: ASC }
  ) {
    items {
      id
      diamond {
        id
        video
        image
        availability
        supplierStockId
        brown
        green
        milky
        eyeClean
        mine_of_origin
        certificate {
          id
          lab
          shape
          certNumber
          cut
          carats
          clarity
          polish
          symmetry
          color
          width
          length
          depth
          girdle
          floInt
          floCol
          depthPercentage
          table
        }
      }
      price
      discount
    }
    total_count
  }
}
""";
    }

    private static HashSet<string> ParseLabs(string labsCsv)
    {
        return labsCsv
            .Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(x => x.ToUpperInvariant())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet();
    }

    private static string EscapeGraphQlString(string value)
    {
        return value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal);
    }

    private static async Task<JsonDocument> ExecuteRawAsync(BusinessSettings settings, string query, object variables, string? bearerToken, CancellationToken cancellationToken)
    {
        var endpoint = string.IsNullOrWhiteSpace(settings.NivodaEndpoint) ? DefaultEndpoint : settings.NivodaEndpoint.Trim();
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        if (!string.IsNullOrWhiteSpace(bearerToken))
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

        var payload = JsonSerializer.Serialize(new { query, variables }, JsonOptions);
        using var content = new StringContent(payload, Encoding.UTF8, "application/json");
        using var response = await client.PostAsync(endpoint, content, cancellationToken);
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Nivoda request failed: {(int)response.StatusCode} {response.ReasonPhrase}\n\n{responseText}");

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(responseText);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("Nivoda returned a non-JSON response. Check the endpoint URL and credentials.\n\n" + responseText, ex);
        }

        if (document.RootElement.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Array && errors.GetArrayLength() > 0)
        {
            var detail = JsonSerializer.Serialize(errors, JsonOptions);
            document.Dispose();
            throw new InvalidOperationException("Nivoda returned GraphQL errors. Error details:\n" + detail);
        }

        return document;
    }

    private static IEnumerable<JsonElement> EnumerateDiamondOffers(JsonElement diamondsElement)
    {
        if (diamondsElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in diamondsElement.EnumerateArray())
                yield return item;
        }
        else if (diamondsElement.ValueKind == JsonValueKind.Object)
        {
            foreach (var arrayName in new[] { "items", "nodes", "results", "diamonds" })
            {
                if (diamondsElement.TryGetProperty(arrayName, out var array) && array.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in array.EnumerateArray())
                        yield return item;
                }
            }
        }
    }

    private static ExternalDiamond MapDiamond(JsonElement offer, BusinessSettings settings, string rawJson, bool isLabGrown)
    {
        var diamondElement = offer;
        if (offer.ValueKind == JsonValueKind.Object && offer.TryGetProperty("diamond", out var nestedDiamond))
            diamondElement = nestedDiamond;

        diamondElement.TryGetProperty("certificate", out var certificate);
        var carat = GetDecimal(diamondElement, "carat", "carats") != 0 ? GetDecimal(diamondElement, "carat", "carats") : GetDecimal(certificate, "carat", "carats");
        var supplierPrice = GetDecimal(offer, "price", "supplierPrice", "totalPrice");
        if (supplierPrice == 0)
            supplierPrice = GetDecimal(diamondElement, "price", "supplierPrice", "totalPrice");

        var markup = settings.ExternalDiamondDefaultMarkupPercent <= 0 ? 35m : settings.ExternalDiamondDefaultMarkupPercent;
        var retail = supplierPrice > 0 ? decimal.Round(supplierPrice * (1m + markup / 100m), 2) : 0m;

        return new ExternalDiamond
        {
            SourceSystem = "Nivoda",
            SupplierDiamondId = FirstNonBlank(GetString(offer, "id", "offerId"), GetString(diamondElement, "id", "diamondId", "supplierDiamondId")),
            Status = "Search Result",
            Shape = FirstNonBlank(GetString(diamondElement, "shape"), GetString(certificate, "shape")),
            Carat = carat,
            Color = FirstNonBlank(GetString(diamondElement, "color"), GetString(certificate, "color")),
            Clarity = FirstNonBlank(GetString(diamondElement, "clarity"), GetString(certificate, "clarity")),
            Cut = FirstNonBlank(GetString(diamondElement, "cut"), GetString(certificate, "cut")),
            Lab = FirstNonBlank(GetString(diamondElement, "lab"), GetString(certificate, "lab")),
            CertificateNumber = FirstNonBlank(GetString(diamondElement, "certificateNumber", "certNumber"), GetString(certificate, "certificateNumber", "certNumber", "id")),
            IsLabGrown = isLabGrown,
            SupplierPrice = supplierPrice,
            Currency = string.IsNullOrWhiteSpace(settings.ExternalDiamondDefaultCurrency) ? "AUD" : settings.ExternalDiamondDefaultCurrency.Trim().ToUpperInvariant(),
            MarkupPercent = markup,
            EstimatedRetailPrice = retail,
            VideoUrl = GetString(diamondElement, "videoUrl", "video", "video_url"),
            CertificateUrl = FirstNonBlank(GetString(diamondElement, "certificateUrl", "certificate_url"), GetString(certificate, "url", "pdfUrl")),
            Availability = GetString(diamondElement, "availability", "status"),
            LastSyncedAt = DateTime.Now,
            RawJson = rawJson
        };
    }

    private static string FirstNonBlank(params string[] values) => values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v)) ?? string.Empty;

    private static string GetString(JsonElement element, params string[] names)
    {
        if (element.ValueKind != JsonValueKind.Object) return string.Empty;
        foreach (var name in names)
        {
            if (element.TryGetProperty(name, out var value))
            {
                return value.ValueKind switch
                {
                    JsonValueKind.String => value.GetString() ?? string.Empty,
                    JsonValueKind.Number => value.ToString(),
                    JsonValueKind.True => "True",
                    JsonValueKind.False => "False",
                    _ => value.ToString()
                };
            }
        }
        return string.Empty;
    }

    private static decimal GetDecimal(JsonElement element, params string[] names)
    {
        var value = GetString(element, names);
        return decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var invariantResult)
            ? invariantResult
            : decimal.TryParse(value, NumberStyles.Number, CultureInfo.CurrentCulture, out var currentResult) ? currentResult : 0m;
    }
}
