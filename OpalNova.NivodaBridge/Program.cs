using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
var config = BridgeConfig.FromEnvironment(builder.Environment.IsProduction());

builder.Services.AddSingleton(config);
builder.Services.AddSingleton<NivodaBridgeClient>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("opalnova", policy =>
    {
        if (config.AllowedOrigins.Length == 0)
        {
            if (config.IsProduction)
            {
                policy.WithOrigins(Array.Empty<string>());
            }
            else
            {
                policy.AllowAnyOrigin();
            }
        }
        else
        {
            policy.WithOrigins(config.AllowedOrigins);
        }

        policy.AllowAnyHeader().AllowAnyMethod();
    });
});

var app = builder.Build();
app.UseCors("opalnova");

app.MapGet("/", () => Results.Ok(new
{
    service = "OPALNOVA Nivoda API Bridge",
    status = "online",
    productionUrlMeaning = "Give Nivoda the deployed domain for this bridge: https://api.jackthejeweller.com.au.",
    endpoints = new[] { "GET /health", "POST /nivoda/search", "POST /nivoda/schema" }
}));

app.MapGet("/health", (BridgeConfig bridgeConfig) => Results.Ok(new
{
    status = "ok",
    nivodaEndpointConfigured = !string.IsNullOrWhiteSpace(bridgeConfig.NivodaEndpoint),
    nivodaCredentialsConfigured = bridgeConfig.HasNivodaCredentials,
    apiKeyRequired = bridgeConfig.ApiKeyRequired,
    allowedOrigins = bridgeConfig.AllowedOrigins,
    productionSafe = bridgeConfig.IsProductionSafe,
    warnings = bridgeConfig.GetConfigurationWarnings()
}));

app.MapPost("/nivoda/search", async (DiamondSearchRequest request, HttpRequest httpRequest, BridgeConfig bridgeConfig, NivodaBridgeClient client, CancellationToken cancellationToken) =>
{
    if (!bridgeConfig.IsProductionSafe)
        return Results.Problem("Bridge production configuration is incomplete. Configure Nivoda credentials, OPALNOVA_BRIDGE_API_KEY and OPALNOVA_ALLOWED_ORIGINS on the host.", statusCode: StatusCodes.Status503ServiceUnavailable);

    if (!IsAuthorized(httpRequest, bridgeConfig))
        return Results.Unauthorized();

    var response = await client.SearchDiamondsAsync(request, cancellationToken);
    return Results.Ok(response);
});

app.MapPost("/nivoda/schema", async (HttpRequest httpRequest, BridgeConfig bridgeConfig, NivodaBridgeClient client, CancellationToken cancellationToken) =>
{
    if (!bridgeConfig.IsProductionSafe)
        return Results.Problem("Bridge production configuration is incomplete. Configure Nivoda credentials, OPALNOVA_BRIDGE_API_KEY and OPALNOVA_ALLOWED_ORIGINS on the host.", statusCode: StatusCodes.Status503ServiceUnavailable);

    if (!IsAuthorized(httpRequest, bridgeConfig))
        return Results.Unauthorized();

    var response = await client.GetSchemaDiagnosticsAsync(cancellationToken);
    return Results.Ok(response);
});

app.Run();

static bool IsAuthorized(HttpRequest request, BridgeConfig config)
{
    if (!config.ApiKeyRequired)
        return true;

    return request.Headers.TryGetValue("X-OPALNOVA-BRIDGE-KEY", out var key)
        && string.Equals(key.ToString(), config.BridgeApiKey, StringComparison.Ordinal);
}

public sealed record BridgeConfig(
    string NivodaEndpoint,
    string NivodaUsername,
    string NivodaPassword,
    string[] AllowedOrigins,
    string BridgeApiKey,
    bool IsProduction)
{
    public bool HasNivodaCredentials => !string.IsNullOrWhiteSpace(NivodaUsername) && !string.IsNullOrWhiteSpace(NivodaPassword);
    public bool ApiKeyRequired => !string.IsNullOrWhiteSpace(BridgeApiKey);
    public bool IsProductionSafe => !IsProduction || (HasNivodaCredentials && ApiKeyRequired && AllowedOrigins.Length > 0);

    public static BridgeConfig FromEnvironment(bool isProduction)
    {
        var endpoint = Env("NIVODA_ENDPOINT", "https://intg-customer-staging.nivodaapi.net/api/diamonds");
        var origins = Env("OPALNOVA_ALLOWED_ORIGINS", string.Empty)
            .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return new BridgeConfig(
            endpoint,
            Env("NIVODA_USERNAME", string.Empty),
            Env("NIVODA_PASSWORD", string.Empty),
            origins,
            Env("OPALNOVA_BRIDGE_API_KEY", string.Empty),
            isProduction);
    }

    public string[] GetConfigurationWarnings()
    {
        var warnings = new List<string>();
        if (!HasNivodaCredentials)
            warnings.Add("Nivoda credentials are not configured.");
        if (IsProduction && !ApiKeyRequired)
            warnings.Add("OPALNOVA_BRIDGE_API_KEY is required in production.");
        if (IsProduction && AllowedOrigins.Length == 0)
            warnings.Add("OPALNOVA_ALLOWED_ORIGINS is required in production.");

        return warnings.ToArray();
    }

    private static string Env(string name, string fallback)
        => Environment.GetEnvironmentVariable(name)?.Trim() ?? fallback;
}

public sealed record DiamondSearchRequest(
    string Shape = "ROUND",
    bool IsLabGrown = true,
    decimal MinCarat = 1.0m,
    decimal MaxCarat = 1.5m,
    string LabsCsv = "IGI,GIA",
    int Limit = 20);

public sealed record DiamondSearchResponse(List<BridgeDiamond> Diamonds, string Note, string RawJson);

public sealed record BridgeDiamond(
    string SupplierDiamondId,
    string Shape,
    decimal Carat,
    string Color,
    string Clarity,
    string Cut,
    string Lab,
    string CertificateNumber,
    bool IsLabGrown,
    decimal SupplierPrice,
    string Currency,
    string VideoUrl,
    string CertificateUrl,
    string Availability);

public sealed record SchemaDiagnostics(bool Authenticated, string Status, List<string> QueryFields, List<string> MutationFields);

public sealed class NivodaBridgeClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private readonly BridgeConfig _config;
    private string? _token;
    private DateTimeOffset _tokenCreatedAt;

    private const string AuthenticateQuery = """
query Authenticate($username: String!, $password: String!) {
  authenticate {
    username_and_password(username: $username, password: $password) {
      token
    }
  }
}
""";

    private const string IntrospectionQuery = """
query OpalNovaNivodaSchema {
  __schema {
    queryType {
      fields { name }
    }
    mutationType {
      fields { name }
    }
  }
}
""";

    public NivodaBridgeClient(BridgeConfig config)
    {
        _config = config;
    }

    public async Task<DiamondSearchResponse> SearchDiamondsAsync(DiamondSearchRequest request, CancellationToken cancellationToken)
    {
        var token = await GetTokenAsync(cancellationToken);
        using var document = await ExecuteRawAsync(BuildSearchQuery(request), new { }, token, cancellationToken);
        var rawJson = JsonSerializer.Serialize(document.RootElement, JsonOptions);
        var labs = ParseLabs(request.LabsCsv);
        var diamonds = new List<BridgeDiamond>();
        var totalCount = 0;

        if (document.RootElement.TryGetProperty("data", out var data) &&
            data.TryGetProperty("diamonds_by_query", out var diamondsElement))
        {
            if (diamondsElement.TryGetProperty("total_count", out var countElement) && countElement.TryGetInt32(out var count))
                totalCount = count;

            foreach (var item in EnumerateDiamondOffers(diamondsElement))
            {
                var diamond = MapDiamond(item, request.IsLabGrown);
                if (labs.Count == 0 || labs.Contains(diamond.Lab.Trim().ToUpperInvariant()))
                    diamonds.Add(diamond);
            }
        }

        var note = diamonds.Count == 0
            ? $"The API responded but returned no diamond rows for this filter. Total server matches before local lab filtering: {totalCount}."
            : $"Loaded {diamonds.Count} diamond result(s). Total server matches before local lab filtering: {totalCount}.";

        return new DiamondSearchResponse(diamonds, note, rawJson);
    }

    public async Task<SchemaDiagnostics> GetSchemaDiagnosticsAsync(CancellationToken cancellationToken)
    {
        var token = await GetTokenAsync(cancellationToken);
        using var document = await ExecuteRawAsync(IntrospectionQuery, new { }, token, cancellationToken);
        if (!document.RootElement.TryGetProperty("data", out var data) ||
            !data.TryGetProperty("__schema", out var schema))
        {
            return new SchemaDiagnostics(true, "Schema introspection returned no __schema data.", new(), new());
        }

        var queryFields = ReadFieldNames(schema, "queryType");
        var mutationFields = ReadFieldNames(schema, "mutationType");
        return new SchemaDiagnostics(true, $"Schema introspection succeeded. Queries: {queryFields.Count}. Mutations: {mutationFields.Count}.", queryFields, mutationFields);
    }

    private async Task<string> GetTokenAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_config.NivodaUsername) || string.IsNullOrWhiteSpace(_config.NivodaPassword))
            throw new InvalidOperationException("Nivoda credentials are not configured on the bridge host.");

        if (!string.IsNullOrWhiteSpace(_token) && DateTimeOffset.UtcNow - _tokenCreatedAt < TimeSpan.FromMinutes(45))
            return _token;

        using var document = await ExecuteRawAsync(
            AuthenticateQuery,
            new { username = _config.NivodaUsername, password = _config.NivodaPassword },
            bearerToken: null,
            cancellationToken);

        if (document.RootElement.TryGetProperty("data", out var data) &&
            data.TryGetProperty("authenticate", out var auth) &&
            auth.TryGetProperty("username_and_password", out var usernamePassword) &&
            usernamePassword.TryGetProperty("token", out var tokenElement))
        {
            var token = tokenElement.GetString();
            if (!string.IsNullOrWhiteSpace(token))
            {
                _token = token;
                _tokenCreatedAt = DateTimeOffset.UtcNow;
                return token;
            }
        }

        throw new InvalidOperationException("Nivoda authentication succeeded at HTTP level, but no token was returned.");
    }

    private async Task<JsonDocument> ExecuteRawAsync(string query, object variables, string? bearerToken, CancellationToken cancellationToken)
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        if (!string.IsNullOrWhiteSpace(bearerToken))
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

        var payload = JsonSerializer.Serialize(new { query, variables }, JsonOptions);
        using var content = new StringContent(payload, Encoding.UTF8, "application/json");
        using var response = await client.PostAsync(_config.NivodaEndpoint, content, cancellationToken);
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Nivoda request failed: {(int)response.StatusCode} {response.ReasonPhrase}\n\n{responseText}");

        var document = JsonDocument.Parse(responseText);
        if (document.RootElement.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Array && errors.GetArrayLength() > 0)
        {
            var detail = JsonSerializer.Serialize(errors, JsonOptions);
            document.Dispose();
            throw new InvalidOperationException("Nivoda returned GraphQL errors. Error details:\n" + detail);
        }

        return document;
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
        => labsCsv
            .Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(x => x.ToUpperInvariant())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet();

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

    private static BridgeDiamond MapDiamond(JsonElement offer, bool isLabGrown)
    {
        var diamondElement = offer;
        if (offer.ValueKind == JsonValueKind.Object && offer.TryGetProperty("diamond", out var nestedDiamond))
            diamondElement = nestedDiamond;

        diamondElement.TryGetProperty("certificate", out var certificate);
        var supplierPrice = GetDecimal(offer, "price", "supplierPrice", "totalPrice");
        if (supplierPrice == 0)
            supplierPrice = GetDecimal(diamondElement, "price", "supplierPrice", "totalPrice");

        return new BridgeDiamond(
            FirstNonBlank(GetString(offer, "id", "offerId"), GetString(diamondElement, "id", "diamondId", "supplierDiamondId")),
            FirstNonBlank(GetString(diamondElement, "shape"), GetString(certificate, "shape")),
            GetDecimal(diamondElement, "carat", "carats") != 0 ? GetDecimal(diamondElement, "carat", "carats") : GetDecimal(certificate, "carat", "carats"),
            FirstNonBlank(GetString(diamondElement, "color"), GetString(certificate, "color")),
            FirstNonBlank(GetString(diamondElement, "clarity"), GetString(certificate, "clarity")),
            FirstNonBlank(GetString(diamondElement, "cut"), GetString(certificate, "cut")),
            FirstNonBlank(GetString(diamondElement, "lab"), GetString(certificate, "lab")),
            FirstNonBlank(GetString(diamondElement, "certificateNumber", "certNumber"), GetString(certificate, "certificateNumber", "certNumber", "id")),
            isLabGrown,
            supplierPrice,
            "AUD",
            GetString(diamondElement, "videoUrl", "video", "video_url"),
            FirstNonBlank(GetString(diamondElement, "certificateUrl", "certificate_url"), GetString(certificate, "url", "pdfUrl")),
            GetString(diamondElement, "availability", "status"));
    }

    private static List<string> ReadFieldNames(JsonElement schema, string typeName)
    {
        if (!schema.TryGetProperty(typeName, out var typeElement) ||
            typeElement.ValueKind != JsonValueKind.Object ||
            !typeElement.TryGetProperty("fields", out var fieldsElement) ||
            fieldsElement.ValueKind != JsonValueKind.Array)
        {
            return new List<string>();
        }

        return fieldsElement
            .EnumerateArray()
            .Select(field => GetString(field, "name"))
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .OrderBy(name => name)
            .ToList();
    }

    private static string EscapeGraphQlString(string value)
        => value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal);

    private static string FirstNonBlank(params string[] values)
        => values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v)) ?? string.Empty;

    private static string GetString(JsonElement element, params string[] names)
    {
        if (element.ValueKind != JsonValueKind.Object)
            return string.Empty;

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
                    _ => string.Empty
                };
            }
        }

        return string.Empty;
    }

    private static decimal GetDecimal(JsonElement element, params string[] names)
    {
        if (element.ValueKind != JsonValueKind.Object)
            return 0m;

        foreach (var name in names)
        {
            if (element.TryGetProperty(name, out var value))
            {
                if (value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var number))
                    return number;
                if (value.ValueKind == JsonValueKind.String && decimal.TryParse(value.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
                    return parsed;
            }
        }

        return 0m;
    }
}
