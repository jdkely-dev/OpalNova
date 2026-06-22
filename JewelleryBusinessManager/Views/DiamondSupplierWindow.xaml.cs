using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using JewelleryBusinessManager.Data;
using JewelleryBusinessManager.Models;
using JewelleryBusinessManager.Services;
using MessageBox = System.Windows.MessageBox;
using MessageBoxButton = System.Windows.MessageBoxButton;
using MessageBoxImage = System.Windows.MessageBoxImage;

namespace JewelleryBusinessManager.Views;

public partial class DiamondSupplierWindow : Window
{
    public bool IsHostedInTab { get; set; }
    public event EventHandler? OpenSavedRecordsRequested;
    public event EventHandler? CloseRequested;

    private BusinessSettings _settings;
    private List<ExternalDiamond> _results = new();

    public DiamondSupplierWindow()
    {
        InitializeComponent();
        _settings = BusinessSettingsService.Load();
        ShapeBox.ItemsSource = new[] { "ROUND", "OVAL", "CUSHION", "PEAR", "PRINCESS", "EMERALD", "RADIANT", "MARQUISE", "ASSCHER", "HEART" };
        LoadSettings();
    }

    private void LoadSettings()
    {
        EndpointBox.Text = string.IsNullOrWhiteSpace(_settings.NivodaEndpoint) ? NivodaDiamondApiService.DefaultEndpoint : _settings.NivodaEndpoint;
        GraphiQlBox.Text = string.IsNullOrWhiteSpace(_settings.NivodaGraphiQlUrl) ? NivodaDiamondApiService.DefaultGraphiQlUrl : _settings.NivodaGraphiQlUrl;
        UsernameBox.Text = _settings.NivodaUsername;
        PasswordBox.Password = _settings.NivodaPassword;
        CurrencyBox.Text = string.IsNullOrWhiteSpace(_settings.ExternalDiamondDefaultCurrency) ? "AUD" : _settings.ExternalDiamondDefaultCurrency;
        MarkupBox.Text = _settings.ExternalDiamondDefaultMarkupPercent.ToString("0.##", CultureInfo.CurrentCulture);
        ShapeBox.SelectedItem = "ROUND";
        MinCaratBox.Text = "1.0";
        MaxCaratBox.Text = "1.5";
        LabsBox.Text = "IGI,GIA";
        LabGrownCheck.IsChecked = true;
        StatusText.Text = _settings.NivodaLastConnectionTestAt.HasValue
            ? $"Last connection test: {_settings.NivodaLastConnectionTestAt.Value:g}. {_settings.NivodaLastConnectionNote}"
            : _settings.NivodaLastConnectionNote;
    }

    private bool ApplySettings()
    {
        if (!TryReadDecimal(MarkupBox.Text, "Retail markup", out var markup)) return false;
        _settings.NivodaEndpoint = EndpointBox.Text.Trim();
        _settings.NivodaGraphiQlUrl = GraphiQlBox.Text.Trim();
        _settings.NivodaUsername = UsernameBox.Text.Trim();
        _settings.NivodaPassword = PasswordBox.Password;
        _settings.ExternalDiamondDefaultCurrency = string.IsNullOrWhiteSpace(CurrencyBox.Text) ? "AUD" : CurrencyBox.Text.Trim().ToUpperInvariant();
        _settings.ExternalDiamondDefaultMarkupPercent = markup;
        return true;
    }

    private static bool TryReadDecimal(string text, string label, out decimal value)
    {
        if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out value) && value >= 0)
            return true;
        MessageBox.Show($"{label} must be zero or a positive number.", "Diamond Supplier", MessageBoxButton.OK, MessageBoxImage.Warning);
        return false;
    }

    private void SaveSettings_Click(object sender, RoutedEventArgs e)
    {
        if (!ApplySettings()) return;
        BusinessSettingsService.Save(_settings);
        StatusText.Text = "Nivoda settings saved locally.";
    }

    private void UseDefaultNivodaEndpoints_Click(object sender, RoutedEventArgs e)
    {
        EndpointBox.Text = NivodaDiamondApiService.DefaultEndpoint;
        GraphiQlBox.Text = NivodaDiamondApiService.DefaultGraphiQlUrl;
        StatusText.Text = "Default Nivoda endpoints filled. Enter your own Nivoda credentials before testing or searching.";
    }

    private async void TestConnection_Click(object sender, RoutedEventArgs e)
    {
        if (!ApplySettings()) return;
        try
        {
            Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;
            var note = await NivodaDiamondApiService.TestConnectionAsync(_settings);
            _settings.NivodaLastConnectionTestAt = DateTime.Now;
            _settings.NivodaLastConnectionNote = note;
            BusinessSettingsService.Save(_settings);
            StatusText.Text = note;
            MessageBox.Show(note, "Nivoda Connection", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            ErrorLogService.Log(ex, "Nivoda connection test");
            StatusText.Text = "Connection test failed.";
            MessageBox.Show($"Could not connect to Nivoda.\n\n{ex.Message}", "Nivoda Connection", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            Mouse.OverrideCursor = null;
        }
    }

    private async void Search_Click(object sender, RoutedEventArgs e)
    {
        if (!ApplySettings()) return;
        if (!TryReadDecimal(MinCaratBox.Text, "Minimum carat", out var minCarat)) return;
        if (!TryReadDecimal(MaxCaratBox.Text, "Maximum carat", out var maxCarat)) return;
        if (maxCarat < minCarat)
        {
            MessageBox.Show("Maximum carat must be greater than or equal to minimum carat.", "Diamond Supplier", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;
            BusinessSettingsService.Save(_settings);
            var request = new DiamondSearchRequest
            {
                Shape = ShapeBox.SelectedItem?.ToString() ?? "ROUND",
                MinCarat = minCarat,
                MaxCarat = maxCarat,
                LabsCsv = LabsBox.Text.Trim(),
                IsLabGrown = LabGrownCheck.IsChecked == true,
                Limit = 25
            };
            var response = await NivodaDiamondApiService.SearchDiamondsAsync(_settings, request);
            _results = response.Diamonds;
            ResultsGrid.ItemsSource = null;
            ResultsGrid.ItemsSource = _results;
            if (_results.Count > 0)
            {
                ResultsGrid.SelectedIndex = 0;
                ResultsGrid.ScrollIntoView(_results[0]);
            }
            StatusText.Text = response.Note + " Select a row and use Save Selected Result above the table.";
        }
        catch (Exception ex)
        {
            ErrorLogService.Log(ex, "Nivoda diamond search");
            StatusText.Text = "Search failed. The GraphQL schema may need one field/filter name adjusted.";
            MessageBox.Show($"Diamond search failed.\n\n{ex.Message}", "Diamond Supplier", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            Mouse.OverrideCursor = null;
        }
    }

    private void SaveSelected_Click(object sender, RoutedEventArgs e)
    {
        var selected = GetSelectedDiamond();
        if (selected == null)
        {
            MessageBox.Show("Select a diamond result first. If the grid has results, click once on the row you want to save.", "Diamond Supplier", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            SaveExternalDiamondDirect(selected);
            StatusText.Text = $"External diamond saved: {BuildDiamondLabel(selected)}";
            MessageBox.Show("Selected external diamond saved successfully.", "Diamond Supplier", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            ErrorLogService.Log(ex, "Save Nivoda external diamond");
            MessageBox.Show($"Could not save the selected diamond.\n\n{ex.Message}\n\nA detailed error was written to the OPALNOVA error log.", "Diamond Supplier", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private ExternalDiamond? GetSelectedDiamond()
    {
        return ResultsGrid.SelectedItem as ExternalDiamond
            ?? ResultsGrid.CurrentItem as ExternalDiamond
            ?? _results.FirstOrDefault();
    }

    private static string BuildDiamondLabel(ExternalDiamond diamond)
    {
        var parts = new[]
        {
            diamond.Shape,
            diamond.Carat > 0 ? diamond.Carat.ToString("0.###", CultureInfo.CurrentCulture) + "ct" : string.Empty,
            diamond.Color,
            diamond.Clarity,
            !string.IsNullOrWhiteSpace(diamond.CertificateNumber) ? "Cert " + diamond.CertificateNumber : string.Empty
        }.Where(x => !string.IsNullOrWhiteSpace(x));
        return string.Join(" ", parts);
    }

    private static void SaveExternalDiamondDirect(ExternalDiamond selected)
    {
        // Use a direct SQLite upsert for supplier results. This avoids EF/model drift issues while the
        // external supplier integration is still being adjusted against Nivoda's live staging schema.
        DatabaseBootstrapper.Initialize();

        using var connection = new SqliteConnection($"Data Source={DatabaseBootstrapper.DatabasePath}");
        connection.Open();
        EnsureExternalDiamondTable(connection);

        var existingId = FindExistingExternalDiamondId(connection, selected);
        if (existingId.HasValue)
            UpdateExternalDiamond(connection, existingId.Value, selected);
        else
            InsertExternalDiamond(connection, selected);
    }

    private static void EnsureExternalDiamondTable(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = @"
CREATE TABLE IF NOT EXISTS ExternalDiamonds (
    Id INTEGER NOT NULL CONSTRAINT PK_ExternalDiamonds PRIMARY KEY AUTOINCREMENT,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL,
    SourceSystem TEXT NOT NULL DEFAULT 'Nivoda',
    SupplierDiamondId TEXT NOT NULL DEFAULT '',
    Status TEXT NOT NULL DEFAULT 'Search Result',
    Shape TEXT NOT NULL DEFAULT '',
    Carat TEXT NOT NULL DEFAULT '0',
    Color TEXT NOT NULL DEFAULT '',
    Clarity TEXT NOT NULL DEFAULT '',
    Cut TEXT NOT NULL DEFAULT '',
    Lab TEXT NOT NULL DEFAULT '',
    CertificateNumber TEXT NOT NULL DEFAULT '',
    IsLabGrown INTEGER NOT NULL DEFAULT 1,
    SupplierPrice TEXT NOT NULL DEFAULT '0',
    Currency TEXT NOT NULL DEFAULT 'AUD',
    MarkupPercent TEXT NOT NULL DEFAULT '35',
    EstimatedRetailPrice TEXT NOT NULL DEFAULT '0',
    VideoUrl TEXT NOT NULL DEFAULT '',
    CertificateUrl TEXT NOT NULL DEFAULT '',
    Availability TEXT NOT NULL DEFAULT '',
    SupplierReference TEXT NOT NULL DEFAULT '',
    HoldRequestedAt TEXT NULL,
    HoldConfirmedAt TEXT NULL,
    HoldExpiresAt TEXT NULL,
    OrderRequestedAt TEXT NULL,
    OrderedAt TEXT NULL,
    ExpectedArrivalDate TEXT NULL,
    ReceivedAt TEXT NULL,
    ReleasedAt TEXT NULL,
    LastSyncedAt TEXT NOT NULL,
    RawJson TEXT NOT NULL DEFAULT '',
    Notes TEXT NOT NULL DEFAULT ''
);
CREATE INDEX IF NOT EXISTS IX_ExternalDiamonds_SupplierDiamondId ON ExternalDiamonds (SupplierDiamondId);
CREATE INDEX IF NOT EXISTS IX_ExternalDiamonds_CertificateNumber ON ExternalDiamonds (CertificateNumber);
CREATE INDEX IF NOT EXISTS IX_ExternalDiamonds_Status ON ExternalDiamonds (Status);
CREATE INDEX IF NOT EXISTS IX_ExternalDiamonds_HoldExpiresAt ON ExternalDiamonds (HoldExpiresAt);
CREATE INDEX IF NOT EXISTS IX_ExternalDiamonds_ExpectedArrivalDate ON ExternalDiamonds (ExpectedArrivalDate);";
        command.ExecuteNonQuery();
        EnsureExternalDiamondColumn(connection, "SupplierReference", "TEXT NOT NULL DEFAULT ''");
        EnsureExternalDiamondColumn(connection, "HoldRequestedAt", "TEXT NULL");
        EnsureExternalDiamondColumn(connection, "HoldConfirmedAt", "TEXT NULL");
        EnsureExternalDiamondColumn(connection, "HoldExpiresAt", "TEXT NULL");
        EnsureExternalDiamondColumn(connection, "OrderRequestedAt", "TEXT NULL");
        EnsureExternalDiamondColumn(connection, "OrderedAt", "TEXT NULL");
        EnsureExternalDiamondColumn(connection, "ExpectedArrivalDate", "TEXT NULL");
        EnsureExternalDiamondColumn(connection, "ReceivedAt", "TEXT NULL");
        EnsureExternalDiamondColumn(connection, "ReleasedAt", "TEXT NULL");
    }

    private static void EnsureExternalDiamondColumn(SqliteConnection connection, string columnName, string columnDefinition)
    {
        using (var check = connection.CreateCommand())
        {
            check.CommandText = "PRAGMA table_info(ExternalDiamonds);";
            using var reader = check.ExecuteReader();
            while (reader.Read())
            {
                var existing = reader["name"]?.ToString() ?? string.Empty;
                if (string.Equals(existing, columnName, StringComparison.OrdinalIgnoreCase))
                    return;
            }
        }

        using var alter = connection.CreateCommand();
        alter.CommandText = $"ALTER TABLE ExternalDiamonds ADD COLUMN {columnName} {columnDefinition};";
        alter.ExecuteNonQuery();
    }

    private static int? FindExistingExternalDiamondId(SqliteConnection connection, ExternalDiamond selected)
    {
        var supplierId = Clean(selected.SupplierDiamondId);
        var certNumber = Clean(selected.CertificateNumber);
        if (string.IsNullOrWhiteSpace(supplierId) && string.IsNullOrWhiteSpace(certNumber))
            return null;

        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT Id
FROM ExternalDiamonds
WHERE SourceSystem = 'Nivoda'
  AND ((@supplierId <> '' AND SupplierDiamondId = @supplierId)
       OR (@certNumber <> '' AND CertificateNumber = @certNumber))
LIMIT 1;";
        command.Parameters.AddWithValue("@supplierId", supplierId);
        command.Parameters.AddWithValue("@certNumber", certNumber);
        var value = command.ExecuteScalar();
        return value == null || value == DBNull.Value ? null : Convert.ToInt32(value, CultureInfo.InvariantCulture);
    }

    private static void InsertExternalDiamond(SqliteConnection connection, ExternalDiamond selected)
    {
        using var command = connection.CreateCommand();
        command.CommandText = @"
INSERT INTO ExternalDiamonds
(CreatedAt, UpdatedAt, SourceSystem, SupplierDiamondId, Status, Shape, Carat, Color, Clarity, Cut, Lab, CertificateNumber,
 IsLabGrown, SupplierPrice, Currency, MarkupPercent, EstimatedRetailPrice, VideoUrl, CertificateUrl, Availability, LastSyncedAt, RawJson, Notes)
VALUES
(@now, @now, 'Nivoda', @supplierId, 'Saved', @shape, @carat, @color, @clarity, @cut, @lab, @certificateNumber,
 @isLabGrown, @supplierPrice, @currency, @markupPercent, @retailPrice, @videoUrl, @certificateUrl, @availability, @now, @rawJson, @notes);";
        AddExternalDiamondParameters(command, selected);
        command.ExecuteNonQuery();
    }

    private static void UpdateExternalDiamond(SqliteConnection connection, int id, ExternalDiamond selected)
    {
        using var command = connection.CreateCommand();
        command.CommandText = @"
UPDATE ExternalDiamonds
SET UpdatedAt = @now,
    SupplierDiamondId = @supplierId,
    Status = CASE WHEN Status IS NULL OR Status = '' OR Status = 'Search Result' THEN 'Saved' ELSE Status END,
    Shape = @shape,
    Carat = @carat,
    Color = @color,
    Clarity = @clarity,
    Cut = @cut,
    Lab = @lab,
    CertificateNumber = @certificateNumber,
    IsLabGrown = @isLabGrown,
    SupplierPrice = @supplierPrice,
    Currency = @currency,
    MarkupPercent = @markupPercent,
    EstimatedRetailPrice = @retailPrice,
    VideoUrl = @videoUrl,
    CertificateUrl = @certificateUrl,
    Availability = @availability,
    LastSyncedAt = @now,
    RawJson = @rawJson
WHERE Id = @id;";
        AddExternalDiamondParameters(command, selected);
        command.Parameters.AddWithValue("@id", id);
        command.ExecuteNonQuery();
    }

    private static void AddExternalDiamondParameters(SqliteCommand command, ExternalDiamond selected)
    {
        var now = DateTime.Now.ToString("O", CultureInfo.InvariantCulture);
        command.Parameters.AddWithValue("@now", now);
        command.Parameters.AddWithValue("@supplierId", Clean(selected.SupplierDiamondId));
        command.Parameters.AddWithValue("@shape", Clean(selected.Shape));
        command.Parameters.AddWithValue("@carat", selected.Carat.ToString(CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("@color", Clean(selected.Color));
        command.Parameters.AddWithValue("@clarity", Clean(selected.Clarity));
        command.Parameters.AddWithValue("@cut", Clean(selected.Cut));
        command.Parameters.AddWithValue("@lab", Clean(selected.Lab));
        command.Parameters.AddWithValue("@certificateNumber", Clean(selected.CertificateNumber));
        command.Parameters.AddWithValue("@isLabGrown", selected.IsLabGrown ? 1 : 0);
        command.Parameters.AddWithValue("@supplierPrice", selected.SupplierPrice.ToString(CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("@currency", Clean(selected.Currency, "AUD"));
        command.Parameters.AddWithValue("@markupPercent", selected.MarkupPercent.ToString(CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("@retailPrice", selected.EstimatedRetailPrice.ToString(CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("@videoUrl", Clean(selected.VideoUrl));
        command.Parameters.AddWithValue("@certificateUrl", Clean(selected.CertificateUrl));
        command.Parameters.AddWithValue("@availability", Clean(selected.Availability));
        command.Parameters.AddWithValue("@rawJson", Clean(selected.RawJson));
        command.Parameters.AddWithValue("@notes", Clean(selected.Notes));
    }

    private static string Clean(string? value, string fallback = "") => string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();

    private void OpenVideo_Click(object sender, RoutedEventArgs e)
    {
        var selected = GetSelectedDiamond();
        if (selected == null || string.IsNullOrWhiteSpace(selected.VideoUrl))
        {
            MessageBox.Show("The selected diamond does not include a video URL.", "Diamond Supplier", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        Process.Start(new ProcessStartInfo(selected.VideoUrl) { UseShellExecute = true });
    }

    private void CopyCert_Click(object sender, RoutedEventArgs e)
    {
        var selected = GetSelectedDiamond();
        if (selected == null || string.IsNullOrWhiteSpace(selected.CertificateNumber))
        {
            MessageBox.Show("The selected diamond does not include a certificate number.", "Diamond Supplier", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        Clipboard.SetText(selected.CertificateNumber);
        StatusText.Text = "Certificate number copied.";
    }

    private void OpenSavedRecords_Click(object sender, RoutedEventArgs e)
    {
        if (IsHostedInTab)
        {
            OpenSavedRecordsRequested?.Invoke(this, EventArgs.Empty);
            return;
        }

        DialogResult = true;
    }

    private void OpenGraphiQl_Click(object sender, RoutedEventArgs e)
    {
        var url = string.IsNullOrWhiteSpace(GraphiQlBox.Text) ? NivodaDiamondApiService.DefaultGraphiQlUrl : GraphiQlBox.Text.Trim();
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        if (IsHostedInTab)
        {
            CloseRequested?.Invoke(this, EventArgs.Empty);
            return;
        }

        DialogResult = false;
    }
}
