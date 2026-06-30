using System.Globalization;
using System.IO;
using System.Net;
using System.Text;

namespace JewelleryBusinessManager.Services;

public static class NivodaStagingHandoffService
{
    public static async Task<string> CreateHandoffReportAsync(CancellationToken cancellationToken = default)
    {
        var folder = BusinessSettingsService.GetPrintoutFolder();
        Directory.CreateDirectory(folder);

        var settings = BusinessSettingsService.Load();
        var diagnostics = await NivodaDiamondApiService.CreateDiagnosticsAsync(settings, cancellationToken);
        var path = Path.Combine(folder, $"OPALNOVA-Nivoda-Staging-Handoff-{DateTime.Now:yyyyMMdd-HHmmss}.html");

        var html = new StringBuilder();
        html.AppendLine("<!doctype html>");
        html.AppendLine("<html><head><meta charset=\"utf-8\"><title>OPALNOVA Nivoda Staging Handoff</title>");
        html.AppendLine("<style>body{font-family:Segoe UI,Arial,sans-serif;margin:32px;line-height:1.5;color:#1f2937;background:#f8fafc}h1,h2,h3{color:#111827}.meta{color:#6b7280}.card{background:#fff;border:1px solid #d1d5db;border-radius:10px;padding:16px;margin:14px 0}.warn{border-left:5px solid #b45309;background:#fff7ed}.ok{border-left:5px solid #047857;background:#ecfdf5}code{background:#e5e7eb;padding:2px 5px;border-radius:4px;word-break:break-all}.grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(260px,1fr));gap:14px}table{border-collapse:collapse;width:100%;font-size:12px}th,td{border:1px solid #d1d5db;padding:7px;text-align:left;vertical-align:top}th{background:#e5e7eb}li{margin:5px 0}@media print{body{background:#fff;margin:12mm}.card{break-inside:avoid}}</style>");
        html.AppendLine("</head><body>");
        html.AppendLine("<h1>OPALNOVA Nivoda Staging Handoff</h1>");
        html.AppendLine($"<p class='meta'>Generated locally: {Html(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.CurrentCulture))}</p>");
        html.AppendLine("<div class='grid'>");
        html.AppendLine(Card("Environment", $"<p><b>Name:</b> {Html(diagnostics.EnvironmentName)}</p><p><b>Endpoint:</b><br><code>{Html(diagnostics.Endpoint)}</code></p><p><b>GraphiQL:</b><br><code>{Html(diagnostics.GraphiQlUrl)}</code></p>"));
        html.AppendLine(Card("External Review Link", string.IsNullOrWhiteSpace(diagnostics.ReviewUrl)
            ? "<p>No external staging review URL is saved yet.</p><p class='meta'>For a desktop app, this usually needs to be a private download/share link to the published staging build, or a hosted GitHub/Pages/Netlify handoff page.</p>"
            : $"<p><code>{Html(diagnostics.ReviewUrl)}</code></p>"));
        html.AppendLine(Card("Credential Safety", "<p>Username and password are intentionally not included in this handoff report.</p><p>Nivoda credentials must be entered in OPALNOVA by the user and should not be committed, emailed in screenshots, or packaged with builds.</p>"));
        html.AppendLine("</div>");

        html.AppendLine("<div class='card ok'><h2>Current API Check</h2>");
        html.AppendLine(Row("Credentials entered", diagnostics.CredentialsEntered ? "Yes" : "No"));
        html.AppendLine(Row("Authentication", diagnostics.AuthenticationStatus));
        html.AppendLine(Row("Schema status", diagnostics.SchemaStatus));
        html.AppendLine("</div>");

        html.AppendLine("<div class='card'><h2>What OPALNOVA Needs Confirmed By Nivoda</h2><ol>");
        html.AppendLine("<li>Confirmed staging and production GraphQL endpoints for diamond search.</li>");
        html.AppendLine("<li>Current search query/filter names for lab-grown/natural, shape, carat range, lab, price, availability and certificate/video fields.</li>");
        html.AppendLine("<li>Current authentication flow and token lifetime for username/password accounts.</li>");
        html.AppendLine("<li>Whether hold, reserve, cart, order, purchase or checkout mutations are available to this account.</li>");
        html.AppendLine("<li>Required mutation payloads for requesting holds, confirming orders, checking hold expiry, refreshing price/availability and retrieving supplier order documents.</li>");
        html.AppendLine("<li>Any required webhook/callback URL. OPALNOVA is currently a local Windows desktop app, so it does not expose a public callback unless a separate hosted component is approved.</li>");
        html.AppendLine("</ol></div>");

        AppendFields(html, "Diamond / Availability Related Fields", diagnostics.DiamondFields);
        AppendFields(html, "Hold / Order Candidate Mutations", diagnostics.HoldOrderFields);
        AppendFields(html, "All Query Fields", diagnostics.QueryFields);
        AppendFields(html, "All Mutation Fields", diagnostics.MutationFields);

        html.AppendLine("<div class='card warn'><h2>Implementation Boundary</h2><p>OPALNOVA can search and save external diamond records with user-entered credentials. Live hold/order actions should only be enabled after this report confirms the account-specific mutation names and required payloads. Until then, OPALNOVA should keep hold/order tracking as local workflow state and require supplier confirmation outside the app.</p></div>");
        html.AppendLine("</body></html>");

        File.WriteAllText(path, html.ToString());
        return path;
    }

    private static void AppendFields(StringBuilder html, string title, List<NivodaSchemaField> fields)
    {
        html.AppendLine("<div class='card'>");
        html.AppendLine($"<h2>{Html(title)}</h2>");
        if (fields.Count == 0)
        {
            html.AppendLine("<p class='meta'>No fields discovered or credentials/schema access not available.</p>");
            html.AppendLine("</div>");
            return;
        }

        html.AppendLine("<table><tr><th>Name</th><th>Signature</th></tr>");
        foreach (var field in fields)
            html.AppendLine($"<tr><td>{Html(field.Name)}</td><td><code>{Html(field.Signature)}</code></td></tr>");
        html.AppendLine("</table>");
        html.AppendLine("</div>");
    }

    private static string Card(string title, string body) => $"<div class='card'><h2>{Html(title)}</h2>{body}</div>";
    private static string Row(string key, string value) => $"<p><b>{Html(key)}:</b><br>{Html(value)}</p>";
    private static string Html(string value) => WebUtility.HtmlEncode(value);
}
