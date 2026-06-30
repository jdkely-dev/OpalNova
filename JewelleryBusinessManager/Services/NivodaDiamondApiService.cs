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

public sealed class NivodaSchemaField
{
    public string Name { get; set; } = string.Empty;
    public string Signature { get; set; } = string.Empty;
}

public sealed class NivodaApiDiagnostics
{
    public string EnvironmentName { get; set; } = "Staging";
    public string Endpoint { get; set; } = string.Empty;
    public string GraphiQlUrl { get; set; } = string.Empty;
    public string ReviewUrl { get; set; } = string.Empty;
    public bool CredentialsEntered { get; set; }
    public bool Authenticated { get; set; }
    public string AuthenticationStatus { get; set; } = string.Empty;
    public string SchemaStatus { get; set; } = string.Empty;
    public List<NivodaSchemaField> QueryFields { get; set; } = new();
    public List<NivodaSchemaField> MutationFields { get; set; } = new();
    public List<NivodaSchemaField> DiamondFields { get; set; } = new();
    public List<NivodaSchemaField> HoldOrderFields { get; set; } = new();
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

    private const string IntrospectionQuery = @"
query OpalNovaNivodaSchema {
  __schema {
    queryType {
      fields {
        name
        args {
          name
          type {
            kind
            name
            ofType {
              kind
              name
              ofType {
                kind
                name
                ofType {
                  kind
                  name
                }
              }
            }
          }
        }
      }
    }
    mutationType {
      fields {
        name
        args {
          name
          type {
            kind
            name
            ofType {
              kind
              name
              ofType {
                kind
                name
                ofType {
                  kind
                  name
                }
              }
            }
          }
        }
      }
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

    public static async Task<NivodaApiDiagnostics> CreateDiagnosticsAsync(BusinessSettings settings, CancellationToken cancellationToken = default)
    {
        var diagnostics = new NivodaApiDiagnostics
        {
            EnvironmentName = string.IsNullOrWhiteSpace(settings.NivodaEnvironmentName) ? "Staging" : settings.NivodaEnvironmentName.Trim(),
            Endpoint = string.IsNullOrWhiteSpace(settings.NivodaEndpoint) ? DefaultEndpoint : settings.NivodaEndpoint.Trim(),
            GraphiQlUrl = string.IsNullOrWhiteSpace(settings.NivodaGraphiQlUrl) ? DefaultGraphiQlUrl : settings.NivodaGraphiQlUrl.Trim(),
            ReviewUrl = settings.NivodaStagingReviewUrl.Trim(),
            CredentialsEntered = !string.IsNullOrWhiteSpace(settings.NivodaUsername) && !string.IsNullOrWhiteSpace(settings.NivodaPassword)
        };

        if (!diagnostics.CredentialsEntered)
        {
            diagnostics.AuthenticationStatus = "Credentials are not entered. Handoff report can still show configuration, but live schema checks require Nivoda staging credentials.";
            diagnostics.SchemaStatus = "Schema not checked.";
            return diagnostics;
        }

        try
        {
            var token = await AuthenticateAsync(settings, cancellationToken);
            diagnostics.Authenticated = true;
            diagnostics.AuthenticationStatus = "Authenticated successfully with user-entered credentials.";

            using var document = await ExecuteRawAsync(settings, IntrospectionQuery, new { }, token, cancellationToken);
            if (document.RootElement.TryGetProperty("data", out var data) &&
                data.TryGetProperty("__schema", out var schema))
            {
                diagnostics.QueryFields = ReadSchemaFields(schema, "queryType");
                diagnostics.MutationFields = ReadSchemaFields(schema, "mutationType");
                diagnostics.DiamondFields = diagnostics.QueryFields
                    .Concat(diagnostics.MutationFields)
                    .Where(f => ContainsAny(f.Name, "diamond", "stone", "inventory", "certificate", "availability", "price"))
                    .OrderBy(f => f.Name)
                    .ToList();
                diagnostics.HoldOrderFields = diagnostics.MutationFields
                    .Where(f => ContainsAny(f.Name, "hold", "reserve", "order", "cart", "purchase", "checkout"))
                    .OrderBy(f => f.Name)
                    .ToList();
                diagnostics.SchemaStatus = $"Schema introspection succeeded. Queries: {diagnostics.QueryFields.Count}. Mutations: {diagnostics.MutationFields.Count}.";
            }
            else
            {
                diagnostics.SchemaStatus = "Schema introspection returned no __schema data.";
            }
        }
        catch (Exception ex)
        {
            diagnostics.AuthenticationStatus = diagnostics.Authenticated
                ? diagnostics.AuthenticationStatus
                : "Authentication or endpoint check failed.";
            diagnostics.SchemaStatus = ex.Message;
        }

        return diagnostics;
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

    private static List<NivodaSchemaField> ReadSchemaFields(JsonElement schema, string typeName)
    {
        if (!schema.TryGetProperty(typeName, out var typeElement) ||
            typeElement.ValueKind != JsonValueKind.Object ||
            !typeElement.TryGetProperty("fields", out var fieldsElement) ||
            fieldsElement.ValueKind != JsonValueKind.Array)
        {
            return new List<NivodaSchemaField>();
        }

        var fields = new List<NivodaSchemaField>();
        foreach (var field in fieldsElement.EnumerateArray())
        {
            var name = GetString(field, "name");
            if (string.IsNullOrWhiteSpace(name)) continue;
            fields.Add(new NivodaSchemaField
            {
                Name = name,
                Signature = BuildFieldSignature(field)
            });
        }

        return fields.OrderBy(f => f.Name).ToList();
    }

    private static string BuildFieldSignature(JsonElement field)
    {
        var name = GetString(field, "name");
        if (!field.TryGetProperty("args", out var argsElement) || argsElement.ValueKind != JsonValueKind.Array)
            return name;

        var args = argsElement
            .EnumerateArray()
            .Select(arg => $"{GetString(arg, "name")}: {GraphQlTypeName(arg.TryGetProperty("type", out var type) ? type : default)}")
            .Where(x => !x.StartsWith(": ", StringComparison.Ordinal))
            .ToList();

        return args.Count == 0 ? name : $"{name}({string.Join(", ", args)})";
    }

    private static string GraphQlTypeName(JsonElement type)
    {
        if (type.ValueKind != JsonValueKind.Object)
            return "Unknown";

        var kind = GetString(type, "kind");
        var name = GetString(type, "name");
        if (!string.IsNullOrWhiteSpace(name))
            return kind == "NON_NULL" ? name + "!" : name;

        if (!type.TryGetProperty("ofType", out var ofType) || ofType.ValueKind != JsonValueKind.Object)
            return string.IsNullOrWhiteSpace(kind) ? "Unknown" : kind;

        var inner = GraphQlTypeName(ofType);
        return kind switch
        {
            "NON_NULL" => inner + "!",
            "LIST" => "[" + inner + "]",
            _ => inner
        };
    }

    private static bool ContainsAny(string text, params string[] terms) =>
        terms.Any(term => text.Contains(term, StringComparison.OrdinalIgnoreCase));

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
