using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Text;
using System.Threading;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using JewelleryBusinessManager.Data;
using JewelleryBusinessManager.Models;

namespace JewelleryBusinessManager.Services;

public static class DataSafetyService
{
    public static string CreateFullDataBundle()
    {
        var settings = BusinessSettingsService.Load();
        var exportRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "JewelleryBusinessManager", "DataBundles");
        Directory.CreateDirectory(exportRoot);
        var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        var bundlePath = Path.Combine(exportRoot, $"jbm-full-data-bundle-{timestamp}.zip");
        var databaseSnapshotPath = string.Empty;

        try
        {
            databaseSnapshotPath = CreateDatabaseSnapshotForBundle();

            using var archive = ZipFile.Open(bundlePath, ZipArchiveMode.Create);
            AddFileIfExists(archive, databaseSnapshotPath, "database/jewellery_business_manager.db");
            AddFileIfExists(archive, BusinessSettingsService.SettingsPath, "settings/business-settings.json");
            AddFileIfExists(archive, SavedViewService.FilePath, "settings/saved-search-views.json");

            if (Directory.Exists(DatabaseBootstrapper.PhotoDirectory))
                AddDirectory(archive, DatabaseBootstrapper.PhotoDirectory, "photos");

            var readme = $"OPALNOVA full data bundle\nCreated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\nBusiness: {settings.BusinessName}\n\nThis bundle contains a safe SQLite database snapshot, business settings and stored photos. Keep it private and backed up securely.\n";
            var readmeEntry = archive.CreateEntry("README.txt");
            using (var writer = new StreamWriter(readmeEntry.Open()))
                writer.Write(readme);

            AddCsvExport<Customer>(archive, "exports/customers.csv");
            AddCsvExport<Supplier>(archive, "exports/suppliers.csv");
            AddCsvExport<Material>(archive, "exports/materials.csv");
            AddCsvExport<MaterialTransaction>(archive, "exports/material-transactions.csv");
            AddCsvExport<OpalParcel>(archive, "exports/opal-parcels.csv");
            AddCsvExport<Stone>(archive, "exports/stones.csv");
            AddCsvExport<JewelleryItem>(archive, "exports/jewellery-stock.csv");
            AddCsvExport<Job>(archive, "exports/jobs.csv");
            AddCsvExport<Sale>(archive, "exports/sales.csv");
            AddCsvExport<Payment>(archive, "exports/payments.csv");
            AddCsvExport<MarketEvent>(archive, "exports/market-events.csv");
            AddCsvExport<MarketStock>(archive, "exports/market-stock.csv");
            AddCsvExport<ProductionBatch>(archive, "exports/production-batches.csv");
            AddCsvExport<ProductionBatchItem>(archive, "exports/production-batch-items.csv");
            AddCsvExport<OnlineListing>(archive, "exports/online-listings.csv");
            AddCsvExport<PurchaseOrder>(archive, "exports/purchase-orders.csv");
            AddCsvExport<PurchaseOrderItem>(archive, "exports/purchase-order-items.csv");
            AddCsvExport<BusinessTask>(archive, "exports/business-tasks.csv");
            AddCsvExport<PhotoRecord>(archive, "exports/photos.csv");

            return bundlePath;
        }
        catch
        {
            TryDeleteFile(bundlePath);
            throw;
        }
        finally
        {
            if (!string.IsNullOrWhiteSpace(databaseSnapshotPath))
                TryDeleteFile(databaseSnapshotPath);
        }
    }

    private static string CreateDatabaseSnapshotForBundle()
    {
        DatabaseBootstrapper.Initialize();
        Directory.CreateDirectory(DatabaseBootstrapper.AppDataDirectory);

        var snapshotFolder = Path.Combine(Path.GetTempPath(), "JewelleryBusinessManagerSnapshots");
        Directory.CreateDirectory(snapshotFolder);
        var tempPath = Path.Combine(snapshotFolder, $"jbm-bundle-snapshot-{DateTime.Now:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}.db");

        try
        {
            BackupService.CreateSQLiteSnapshot(DatabaseBootstrapper.DatabasePath, tempPath);
        }
        catch (Exception ex)
        {
            TryDeleteFile(tempPath);
            throw new InvalidOperationException(
                "Export Bundle could not create a safe snapshot of the active database. " +
                "Close any other running copies of OPALNOVA, then try again.", ex);
        }

        ValidateSQLiteDatabaseFile(tempPath);
        return tempPath;
    }

    public static string RestoreDatabaseFromBackup(string backupPath)
    {
        if (string.IsNullOrWhiteSpace(backupPath))
            throw new ArgumentException("No backup file was selected.", nameof(backupPath));

        var sourcePath = Path.GetFullPath(backupPath);
        if (!File.Exists(sourcePath))
            throw new FileNotFoundException($"The selected backup file could not be found:\n{sourcePath}", sourcePath);

        Directory.CreateDirectory(DatabaseBootstrapper.AppDataDirectory);

        var restoreSource = sourcePath;
        var tempExtractedDatabase = string.Empty;

        if (Path.GetExtension(sourcePath).Equals(".zip", StringComparison.OrdinalIgnoreCase))
        {
            tempExtractedDatabase = ExtractDatabaseFromBundle(sourcePath);
            restoreSource = tempExtractedDatabase;
        }

        if (Path.GetFullPath(restoreSource).Equals(Path.GetFullPath(DatabaseBootstrapper.DatabasePath), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("That file is already the active database. Choose a separate backup file instead.");

        ValidateSQLiteDatabaseFile(restoreSource);

        try
        {
            // Replacing a live SQLite database while WPF/EF is running is unreliable on Windows.
            // Instead, stage the validated restore file and apply it before SQLite opens on next startup.
            SqliteConnection.ClearAllPools();
            TryDeleteFile(DatabaseBootstrapper.PendingRestorePath);
            CopyFileWithRetries(restoreSource, DatabaseBootstrapper.PendingRestorePath);
            File.WriteAllText(DatabaseBootstrapper.PendingRestoreNotePath,
                "A database restore has been staged and will be applied the next time the app starts.\n" +
                $"Staged: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n" +
                $"Restore source: {sourcePath}\n" +
                $"Pending restore file: {DatabaseBootstrapper.PendingRestorePath}\n" +
                "Close the app completely, then run it again to apply the restore.\n");

            return "Restore has been staged. Close the app completely and run it again to apply the restored database. A safety copy of the current database will be created during startup before replacement.";
        }
        finally
        {
            if (!string.IsNullOrWhiteSpace(tempExtractedDatabase))
                TryDeleteFile(tempExtractedDatabase);
        }
    }

    public static string PreviewRestoreSource(string backupPath)
    {
        if (string.IsNullOrWhiteSpace(backupPath))
            throw new ArgumentException("No backup file was selected.", nameof(backupPath));

        var sourcePath = Path.GetFullPath(backupPath);
        if (!File.Exists(sourcePath))
            throw new FileNotFoundException($"The selected backup file could not be found:\n{sourcePath}", sourcePath);

        var restoreSource = sourcePath;
        var tempExtractedDatabase = string.Empty;
        var selectedFile = new FileInfo(sourcePath);

        try
        {
            if (Path.GetExtension(sourcePath).Equals(".zip", StringComparison.OrdinalIgnoreCase))
            {
                tempExtractedDatabase = ExtractDatabaseFromBundle(sourcePath);
                restoreSource = tempExtractedDatabase;
            }

            if (Path.GetFullPath(restoreSource).Equals(Path.GetFullPath(DatabaseBootstrapper.DatabasePath), StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("That file is already the active database. Choose a separate backup file instead.");

            ValidateSQLiteDatabaseFile(restoreSource);
            var databaseFile = new FileInfo(restoreSource);

            var sb = new StringBuilder();
            sb.AppendLine("OPALNOVA Restore Preview");
            sb.AppendLine($"Previewed: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine();
            sb.AppendLine($"Selected file: {sourcePath}");
            sb.AppendLine($"Selected type: {(Path.GetExtension(sourcePath).Equals(".zip", StringComparison.OrdinalIgnoreCase) ? "Export Bundle ZIP" : "SQLite database backup")}");
            sb.AppendLine($"Selected size: {selectedFile.Length:N0} bytes");
            sb.AppendLine($"Selected modified: {selectedFile.LastWriteTime:g}");
            if (!string.IsNullOrWhiteSpace(tempExtractedDatabase))
                sb.AppendLine("Bundle database: extracted and validated from ZIP.");
            sb.AppendLine();
            sb.AppendLine($"Active database: {DatabaseBootstrapper.DatabasePath}");
            sb.AppendLine($"Restore staging path: {DatabaseBootstrapper.PendingRestorePath}");
            sb.AppendLine("SQLite integrity check: OK");
            sb.AppendLine($"Database size: {databaseFile.Length:N0} bytes");
            sb.AppendLine();
            AppendRestorePreviewCounts(sb, restoreSource);
            sb.AppendLine();
            sb.AppendLine("Continuing will only stage the selected restore file. The active database is replaced on the next OPALNOVA startup, after a safety backup is created.");
            return sb.ToString();
        }
        finally
        {
            if (!string.IsNullOrWhiteSpace(tempExtractedDatabase))
                TryDeleteFile(tempExtractedDatabase);
        }
    }

    private static void ValidateSQLiteDatabaseFile(string databasePath)
    {
        if (!File.Exists(databasePath))
            throw new FileNotFoundException("The database file selected for restore could not be found.", databasePath);

        var header = new byte[16];
        using (var stream = new FileStream(databasePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
        {
            if (stream.Length < header.Length)
                throw new InvalidOperationException("The selected file is too small to be a valid SQLite database backup.");

            _ = stream.Read(header, 0, header.Length);
        }

        var headerText = Encoding.ASCII.GetString(header);
        if (!headerText.Equals("SQLite format 3\0", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "The selected restore file is not a valid SQLite database. " +
                "Choose a .db backup created by Create Backup, or choose an Export Bundle .zip file. " +
                "Do not choose a CSV, HTML, text report, or renamed ZIP file.");
        }

        try
        {
            using var connection = new SqliteConnection($"Data Source={databasePath};Mode=ReadOnly");
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "PRAGMA integrity_check;";
            var result = Convert.ToString(command.ExecuteScalar());
            if (!string.Equals(result, "ok", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"SQLite integrity check failed: {result}");
        }
        catch (SqliteException ex)
        {
            throw new InvalidOperationException("The selected file could not be opened as a valid SQLite database backup.", ex);
        }
    }

    private static void AppendRestorePreviewCounts(StringBuilder sb, string databasePath)
    {
        var keyTables = new[]
        {
            "Customers",
            "CustomQuotes",
            "Jobs",
            "Sales",
            "Payments",
            "JewelleryItems",
            "Stones",
            "Materials",
            "BusinessTasks",
            "ExternalDiamonds"
        };

        sb.AppendLine("Key record counts in selected backup:");
        using var connection = new SqliteConnection($"Data Source={databasePath};Mode=ReadOnly");
        connection.Open();
        foreach (var table in keyTables)
        {
            if (!TableExists(connection, table))
            {
                sb.AppendLine($"{table}: not present");
                continue;
            }

            using var command = connection.CreateCommand();
            command.CommandText = $"SELECT COUNT(*) FROM {QuoteIdentifier(table)};";
            var count = Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture);
            sb.AppendLine($"{table}: {count:N0}");
        }
    }

    private static bool TableExists(SqliteConnection connection, string tableName)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = $name;";
        command.Parameters.AddWithValue("$name", tableName);
        return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture) > 0;
    }

    private static string QuoteIdentifier(string identifier)
    {
        return "\"" + identifier.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
    }

    private static string ExtractDatabaseFromBundle(string bundlePath)
    {
        using var archive = ZipFile.OpenRead(bundlePath);
        var entry = archive.GetEntry("database/jewellery_business_manager.db")
            ?? archive.Entries.FirstOrDefault(e => e.FullName.EndsWith(".db", StringComparison.OrdinalIgnoreCase));

        if (entry is null)
            throw new InvalidOperationException("The selected ZIP did not contain a database backup file.");

        var tempPath = Path.Combine(Path.GetTempPath(), $"jbm-restore-{DateTime.Now:yyyyMMdd-HHmmss}.db");
        using (var input = entry.Open())
        using (var output = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            input.CopyTo(output);
        }
        return tempPath;
    }


    private static void CopyFileWithRetries(string sourcePath, string destinationPath)
    {
        Exception? lastError = null;
        for (var attempt = 1; attempt <= 5; attempt++)
        {
            try
            {
                BackupService.CopyFileSharedRead(sourcePath, destinationPath);
                return;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                lastError = ex;
                Thread.Sleep(200 * attempt);
            }
        }

        throw new IOException($"The restore file could not be staged because Windows still has a file locked. Source: {sourcePath}. Destination: {destinationPath}", lastError);
    }

    private static string CreateSafetyBackupIfPossible()
    {
        var activeDatabase = DatabaseBootstrapper.DatabasePath;
        var backupDirectory = BusinessSettingsService.GetBackupFolder();
        Directory.CreateDirectory(backupDirectory);

        if (!File.Exists(activeDatabase))
            return "No previous active database file existed, so no safety backup was needed.";

        var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        var destination = Path.Combine(backupDirectory, $"jbm-before-restore-{timestamp}.db");
        BackupService.CreateSQLiteSnapshot(activeDatabase, destination);
        return destination;
    }

    private static void ClearSQLiteSidecarFiles(string databasePath)
    {
        foreach (var path in new[] { databasePath + "-wal", databasePath + "-shm" })
            TryDeleteFile(path);
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    public static string RunDatabaseHealthCheck()
    {
        var sb = new StringBuilder();
        sb.AppendLine("OPALNOVA - Database Health Check");
        sb.AppendLine($"Checked: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"Database path: {DatabaseBootstrapper.DatabasePath}");
        sb.AppendLine();

        if (!File.Exists(DatabaseBootstrapper.DatabasePath))
        {
            sb.AppendLine("WARNING: Database file was missing. The app will create a new database on startup.");
            DatabaseBootstrapper.Initialize();
        }

        using var db = new AppDbContext();
        var canConnect = db.Database.CanConnect();
        sb.AppendLine(canConnect ? "Connection: OK" : "Connection: FAILED");
        sb.AppendLine();
        sb.AppendLine("Record counts:");
        sb.AppendLine($"Customers: {db.Customers.Count()}");
        sb.AppendLine($"Suppliers: {db.Suppliers.Count()}");
        sb.AppendLine($"Materials: {db.Materials.Count()}");
        sb.AppendLine($"Stones: {db.Stones.Count()}");
        sb.AppendLine($"Jewellery stock: {db.JewelleryItems.Count()}");
        sb.AppendLine($"Jobs: {db.Jobs.Count()}");
        sb.AppendLine($"Sales: {db.Sales.Count()}");
        sb.AppendLine($"Payments: {db.Payments.Count()}");
        sb.AppendLine($"Markets: {db.MarketEvents.Count()}");
        sb.AppendLine($"Production batches: {db.ProductionBatches.Count()}");
        sb.AppendLine($"Batch items: {db.ProductionBatchItems.Count()}");
        sb.AppendLine($"Online listings: {db.OnlineListings.Count()}");
        sb.AppendLine($"Tasks: {db.BusinessTasks.Count()}");
        sb.AppendLine($"Photos: {db.PhotoRecords.Count()}");
        sb.AppendLine();

        var missingPhotos = db.PhotoRecords.AsEnumerable().Where(p => !File.Exists(p.FilePath)).ToList();
        sb.AppendLine(missingPhotos.Count == 0
            ? "Photo file links: OK"
            : $"Photo file links: {missingPhotos.Count} missing file(s). Check the Photos section.");

        var lowMaterials = db.Materials.Count(m => m.CurrentQuantity <= m.ReorderLevel);
        var overdueJobs = db.Jobs.AsEnumerable().Count(j => j.DueDate.HasValue && j.DueDate.Value.Date < DateTime.Today && j.Status != JobStatus.Completed && j.Status != JobStatus.Cancelled);
        sb.AppendLine($"Low stock materials: {lowMaterials}");
        sb.AppendLine($"Overdue active jobs: {overdueJobs}");
        sb.AppendLine();
        sb.AppendLine("Health check complete. If connection is OK and counts look sensible, the database is usable.");
        return sb.ToString();
    }

    public static int ImportCsvIntoSection(string csvPath, Type entityType)
    {
        if (!File.Exists(csvPath))
            throw new FileNotFoundException("CSV file not found.", csvPath);

        var lines = File.ReadAllLines(csvPath);
        if (lines.Length < 2)
            return 0;

        var headers = ParseCsvLine(lines[0]);
        var props = entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite && p.Name != "Id")
            .ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);

        using var db = new AppDbContext();
        var count = 0;
        for (var i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;
            var values = ParseCsvLine(lines[i]);
            var entity = Activator.CreateInstance(entityType)!;
            for (var c = 0; c < headers.Count && c < values.Count; c++)
            {
                if (!props.TryGetValue(headers[c], out var prop)) continue;
                var converted = ConvertTextValue(values[c], prop.PropertyType);
                prop.SetValue(entity, converted);
            }
            db.Add(entity);
            count++;
        }

        db.SaveChanges();
        return count;
    }

    public static string CreateUserGuide()
    {
        var folder = BusinessSettingsService.GetPrintoutFolder();
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, "JewelleryBusinessManager-UserGuide.html");
        var html = """
<!doctype html>
<html><head><meta charset="utf-8"><title>OPALNOVA User Guide</title>
<style>body{font-family:Segoe UI,Arial,sans-serif;margin:32px;line-height:1.55;color:#1f2937;background:#f8fafc}h1,h2,h3{color:#111827}h1{margin-bottom:4px}.meta{color:#6b7280}.grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(270px,1fr));gap:14px}.box{background:#fff;border:1px solid #d1d5db;padding:16px;margin:14px 0;border-radius:10px}.warn{border-left:5px solid #b45309;background:#fff7ed}.ok{border-left:5px solid #047857;background:#ecfdf5}code{background:#e5e7eb;padding:2px 4px;border-radius:3px}li{margin:4px 0}.toc a{display:block;margin:3px 0;color:#1f4f5f;text-decoration:none}.small{font-size:12px;color:#6b7280}@media print{body{background:#fff;margin:12mm}.box{break-inside:avoid}}</style></head>
<body>
<h1>OPALNOVA User Guide</h1>
<p class="meta">Generated locally by OPALNOVA. Manual version V1.72.0.</p>
<div class="box toc"><h2>Contents</h2><a href="#setup">1. Setup and daily rhythm</a><a href="#quotes">2. Quotes and proposals</a><a href="#production">3. Production workflow</a><a href="#payments">4. Payments, invoices and handover</a><a href="#inventory">5. Inventory and supplier diamonds</a><a href="#reports">6. Reports and bookkeeping review</a><a href="#safety">7. Backups, restore and data safety</a><a href="#release">8. Release testing routine</a></div>
<div class="box ok"><h2>Best starting point</h2><p>Use the main workflow homes first: Quotes & Proposals, Production, Payments & Sales, Inventory, Reports, and Settings & Backup. Specialist studios are for deeper work once the daily workflow is clear.</p></div>
<div class="box" id="setup"><h2>1. Setup and daily rhythm</h2><ol><li>Open <b>Settings</b> and confirm business name, contact details, logo, document footer, tax/GST settings, printout folder and backup folder.</li><li>Add suppliers, customers, materials, stones and jewellery stock before relying on reports.</li><li>Use the dashboard setup-readiness card and Alert Centre to find missing setup, overdue work, low stock and follow-ups.</li><li>Run <b>Create Backup</b> before importing CSV files, restoring data, bulk cleanup, or major stock status changes.</li></ol><p class="small">Daily habit: open Alert Centre, review Project Workbench, process payments and follow-ups, then back up after important changes.</p></div>
<div class="box" id="quotes"><h2>2. Quotes and proposals</h2><ol><li>Use <b>Custom Quote Builder</b> for multi-option custom work. Keep customer details, expiry dates, occasion, required-by date, ring size, budget, preferred metal and preferred stone current.</li><li>After selecting a customer, use <b>Use Customer Preferences</b> to fill blank ring size, preferred metal and preferred stone fields from that customer profile.</li><li>Add quote options with material, labour, stone, setting, finding and other costs. Mark the best option as recommended when appropriate.</li><li>Attach design images to quote options when they help the customer compare designs.</li><li>Save before leaving the quote workspace. If you close a quote tab with unsaved changes, OPALNOVA prompts you to save, discard or cancel.</li><li>Preview the proposal, then use the proposal send workflow to copy/open an email draft and record when it was sent.</li><li>Use <b>Proposal Pipeline</b> to review prepared proposals, sent proposals, due follow-ups, accepted quotes and converted jobs from one workspace queue.</li><li>After customer approval, mark the accepted option and convert it into a production job.</li></ol><div class="box warn"><h3>Quote caution</h3><p>Do not mark an option accepted until the customer has clearly approved it. Accepted quotes can drive job creation, reserved stock and follow-up actions. Internal quote notes stay private and are not printed in proposal output.</p></div></div>
<div class="box" id="production"><h2>3. Production workflow</h2><ol><li>Use <b>Production Board</b> to move jobs through approval, materials, bench work, setting, polishing, quality check, pickup/shipping and completion.</li><li>Keep due dates realistic. Alert Centre and reports depend on dates and statuses.</li><li>Use job cards, workshop notes and batch reports when preparing bench work or market collections.</li><li>Use the job completion checklist when a job is physically ready. It can consume reserved materials, set reserved stones and release unused reservations.</li></ol><div class="box warn"><h3>Completion caution</h3><p>Material consumption should be reviewable. Check linked quote reservations before completing a job so stock movement remains traceable.</p></div></div>
<div class="box" id="payments"><h2>4. Payments, invoices and handover</h2><ol><li>Use <b>Payment & Collection</b> near the end of production to record deposits, balance payments, pickup/shipping status and sale creation.</li><li>Open a saved job record to review its payment history panel with total, paid, balance and linked ledger rows.</li><li>Generate invoices, receipts, deposit receipts and payment receipts from Documents Studio or Payment & Collection.</li><li>Generate a handover confirmation when an item is collected or shipped so the customer, job, payment state, checklist and sign-off are recorded together.</li><li>Create a thank-you follow-up after handover when after-care, cleaning or adjustment check-in is useful.</li><li>Use Copy Balance Reminder when a customer-ready balance message is needed, and Create Balance Follow-Up when the reminder should also appear in the task queue.</li><li>Create pickup/handover reminders only when an open reminder does not already exist for the job.</li><li>Check customer details, payment method, reference, total paid and balance before printing or sending paperwork.</li><li>Run <b>Outstanding Balances</b> before pickup days so unpaid balances are not missed.</li></ol><p class="small">Avoid manually creating duplicate sales for the same completed job. Use the guided handover workflow where possible.</p></div>
<div class="box" id="inventory"><h2>5. Inventory and supplier diamonds</h2><ol><li>Use Jewellery Stock, Stones and Materials for owned physical stock.</li><li>Use Saved External Diamonds for supplier diamonds that are not yet owned inventory.</li><li>Use Supplier Holds & Orders to track hold expiry, ordered status, received status and conversion to owned stones.</li><li>Use Stock Movement for material receive/use/adjust/return actions.</li><li>Run Stock Ageing, Inventory Value, Reserved Inventory and Opal / Stone Stock reports before buying more stock or preparing markets.</li></ol><div class="box warn"><h3>Supplier stock caution</h3><p>Do not treat supplier diamonds as owned inventory until they are received and explicitly converted into owned loose stone records.</p></div></div>
<div class="box" id="reports"><h2>6. Reports and bookkeeping review</h2><div class="grid"><div><h3>Weekly review</h3><ul><li>BI Command Report</li><li>Visual Charts</li><li>Weekly Sales</li><li>Customer Follow-Ups</li><li>Outstanding Balances</li></ul></div><div><h3>Stock review</h3><ul><li>Inventory Value</li><li>Stock Ageing</li><li>Reserved Inventory</li><li>Opal / Stone Stock</li><li>Low Stock / Reorder reports</li></ul></div><div><h3>Bookkeeping review</h3><ul><li>Monthly Sales</li><li>Profitability</li><li>Tax / GST Summary</li><li>Export BI Excel</li><li>Export BI CSV</li></ul></div></div><p class="small">Reports are snapshots. Re-run them after major data changes and compare totals against known jobs, sales and payment records.</p></div>
<div class="box" id="safety"><h2>7. Backups, restore and data safety</h2><ol><li><b>Create Backup</b> saves a copy of the active SQLite database.</li><li><b>Health Check</b> checks database access, record counts, missing photo links, low stock and overdue jobs.</li><li><b>Export Bundle</b> creates a private ZIP containing a database snapshot, settings, photos and CSV exports.</li><li><b>Restore Backup</b> validates the selected database or export bundle and shows a restore preview before staging the restore.</li><li><b>Import CSV</b> creates new records from matching column headers. It does not edit existing records by ID.</li></ol><div class="box warn"><h3>High-risk actions</h3><p>Restore, import, bulk status updates and cleanup tools can change many records. Create a backup first and close any other running copy of OPALNOVA before restore work.</p></div></div>
<div class="box" id="release"><h2>8. Release testing routine</h2><ol><li>Open the latest version-specific testing checklist from the project folder.</li><li>Launch the published app and confirm the header and About version.</li><li>Run the new feature from both quick workspace and specialist studio entry points when both exist.</li><li>Confirm reports/documents open in preview and with Open HTML / Print where applicable.</li><li>Run a quick safety check: existing records still load, no unexpected records were added, and the app closes cleanly.</li></ol><p class="small">For development validation, each major build should pass debug build, release publish and published executable launch smoke before being committed and pushed.</p></div>
</body></html>
""";
        File.WriteAllText(path, html);
        return path;
    }

    public static string CreateReleaseNotes()
    {
        var folder = BusinessSettingsService.GetPrintoutFolder();
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, $"OPALNOVA-Release-Notes-{DateTime.Now:yyyyMMdd-HHmmss}.html");
        var html = """
<!doctype html>
<html><head><meta charset="utf-8"><title>OPALNOVA Release Notes</title>
<style>body{font-family:Segoe UI,Arial,sans-serif;margin:32px;line-height:1.5;color:#1f2937;background:#f8fafc}h1,h2{color:#111827}.card{background:#fff;border:1px solid #d1d5db;border-radius:10px;padding:16px;margin:14px 0}.meta{color:#6b7280}.tag{display:inline-block;background:#111827;color:#f9fafb;border-radius:999px;padding:3px 9px;font-size:12px;margin-left:6px}</style></head>
<body>
<h1>OPALNOVA Release Notes</h1>
<p class="meta">Generated from the installed app. These notes summarize the current major workflow builds.</p>
<div class="card"><h2>V1.72.0 <span class="tag">Job Payment History</span></h2><ul><li>Added a read-only payment history panel inside saved job editor tabs.</li><li>The panel shows total, paid, balance, payment count, linked customer, sale-created state and payment ledger rows using the existing Payment & Collection balance calculation pattern.</li></ul></div>
<div class="card"><h2>V1.71.0 <span class="tag">Final Customer Follow-Up</span></h2><ul><li>Added duplicate-safe thank-you follow-up creation in Payment & Collection.</li><li>The task includes a customer-ready after-care check-in message linked to the selected job and customer.</li></ul></div>
<div class="card"><h2>V1.70.0 <span class="tag">Handover Confirmation Document</span></h2><ul><li>Added a Payment & Collection handover confirmation document for collection and shipping workflows.</li><li>The document includes customer/job details, payment summary, linked payment ledger, checklist, handover notes and sign-off lines.</li></ul></div>
<div class="card"><h2>V1.69.0 <span class="tag">Reminder Task Consistency</span></h2><ul><li>Added shared duplicate-safe open-task detection for follow-up and reminder workflows.</li><li>Made pickup, project, proposal, quote, supplier diamond and balance reminders use consistent task-code and duplicate prevention rules.</li></ul></div>
<div class="card"><h2>V1.68.0 <span class="tag">Balance Reminder Workflow</span></h2><ul><li>Added Copy Balance Reminder to Payment & Collection for customer-ready outstanding-balance messages.</li><li>Added duplicate-safe balance follow-up task creation for jobs with money still owing.</li></ul></div>
<div class="card"><h2>V1.67.0 <span class="tag">Quote Customer Preference Fill</span></h2><ul><li>Added a Use Customer Preferences action to Custom Quote Builder.</li><li>The action fills blank ring size, preferred metal and preferred stone fields from the selected customer profile without overwriting quote-specific entries.</li></ul></div>
<div class="card"><h2>V1.66.0 <span class="tag">Quote Unsaved Change Guard</span></h2><ul><li>Added a reusable workspace close guard and enabled it for the Custom Quote Builder.</li><li>Quote tabs now prompt to save, discard or cancel when unsaved quote or option changes are detected before tab close or New Quote.</li></ul></div>
<div class="card"><h2>V1.65.0 <span class="tag">Quote Context Fields</span></h2><ul><li>Added additive quote context fields for occasion, required-by date, ring size, budget, preferred metal and preferred stone.</li><li>Proposal output and Proposal Pipeline now surface customer-facing quote context, while internal notes remain private.</li></ul></div>
<div class="card"><h2>V1.64.0 <span class="tag">Proposal Pipeline</span></h2><ul><li>Added a hosted Proposal Pipeline workspace for prepared, sent, follow-up due, accepted and converted proposals.</li><li>Pipeline rows can reopen the exact quote, open the proposal file, copy recorded email drafts and create duplicate-safe follow-up tasks.</li></ul></div>
<div class="card"><h2>V1.63.0 <span class="tag">Text Encoding and Copy Cleanup</span></h2><ul><li>Standardized generated document headings and support copy to avoid fragile typographic separators in exported text and HTML.</li><li>Cleaned up visible release/about/manual metadata so the installed app reports the V1.63 baseline consistently.</li></ul></div>
<div class="card"><h2>V1.62.0 <span class="tag">Help Manual Refresh</span></h2><ul><li>Expanded the built-in User Guide into a practical OPALNOVA manual covering setup, quotes, production, payments, inventory, reports, backups and release testing.</li><li>Added clearer cautions for high-risk workflows such as restore, import, bulk status changes and supplier stock conversion.</li></ul></div>
<div class="card"><h2>V1.61.0 <span class="tag">Visual Report Charts</span></h2><ul><li>Added printable visual chart reporting for sales, profit, quote conversion, inventory value, payments and outstanding balances.</li><li>Charts use local HTML/CSS and existing report data, with no internet or external chart library dependency.</li></ul></div>
<div class="card"><h2>V1.60.0 <span class="tag">Tax and GST Summary</span></h2><ul><li>Added a read-only Tax / GST Summary report for current month, financial quarter, financial year and last-12-month review.</li><li>Added sales, estimated tax, net sales, payment method, outstanding balance and payment data-quality summaries.</li></ul></div>
<div class="card"><h2>V1.59.0 <span class="tag">Profitability Reporting</span></h2><ul><li>Added a read-only profitability report for product/service categories and job types.</li><li>Added profit-reporting data checks for unlinked sales, missing links, zero-cost sales and jobs with incomplete price/cost inputs.</li></ul></div>
<div class="card"><h2>V1.58.0 <span class="tag">Stock Ageing</span></h2><ul><li>Added stock ageing and slow-moving inventory reporting for unsold jewellery and loose stones.</li><li>Added age bands, slow-moving value and report actions in Reports and Reports Studio.</li></ul></div>
<div class="card"><h2>V1.57.0 <span class="tag">Invoice and Receipt Polish</span></h2><ul><li>Refreshed customer invoice, receipt, deposit receipt and payment receipt output.</li><li>Added clearer financial summaries, payment ledgers and handover/payment notes.</li></ul></div>
<div class="card"><h2>V1.56.0 <span class="tag">Customer Timeline</span></h2><ul><li>Added a customer timeline report for quotes, proposals, jobs, sales, payments and follow-ups.</li><li>Improved customer summary cards with quote context and recent timeline events.</li></ul></div>
<div class="card"><h2>V1.55.0 <span class="tag">Release Readiness</span></h2><ul><li>Added dashboard data-safety status for backup freshness, database path and pending restore state.</li><li>Added restore preview before staging a selected backup.</li><li>Added in-app release notes access from admin workflows.</li></ul></div>
<div class="card"><h2>V1.54.0 <span class="tag">BI Excel Export</span></h2><ul><li>Added an Excel-compatible business intelligence workbook export.</li><li>Workbook sheets cover summary, sales, balances, quotes, inventory, reservations, tasks and external diamonds.</li></ul></div>
<div class="card"><h2>V1.53.0 <span class="tag">External Diamond Conversion</span></h2><ul><li>Converted received supplier diamonds into owned loose-stone inventory through an explicit workflow.</li><li>Added duplicate-safe conversion markers and owned stone codes in supplier diamond workflow.</li></ul></div>
<div class="card"><h2>V1.52.0 <span class="tag">Job Completion</span></h2><ul><li>Added explicit job completion checklist.</li><li>Consumed reserved materials and updated reserved stones through a reviewable completion step.</li></ul></div>
<div class="card"><h2>V1.51.0 <span class="tag">Alert Centre</span></h2><ul><li>Added shared next-action engine and Alert Centre.</li><li>Added dashboard setup-readiness guidance.</li></ul></div>
<div class="card"><h2>V1.50.0 <span class="tag">Proposal Send Workflow</span></h2><ul><li>Added proposal prepared/sent tracking, email draft workflow and sent-proposal follow-ups.</li><li>Improved customer-facing proposal output.</li></ul></div>
<p class="meta">Database path is unchanged. OPALNOVA stores business data locally in SQLite unless you intentionally back up, export, or restore data.</p>
</body></html>
""";
        File.WriteAllText(path, html);
        return path;
    }

    public static string CreateAboutText()
    {
        var settings = BusinessSettingsService.Load();
        return $"OPALNOVA\nVersion 1.72.0 - Job Payment History\n\nBusiness: {settings.BusinessName}\nDatabase: {DatabaseBootstrapper.DatabasePath}\nBackups: {BusinessSettingsService.GetBackupFolder()}\nPrintouts: {BusinessSettingsService.GetPrintoutFolder()}\nPhotos: {DatabaseBootstrapper.PhotoDirectory}\nError log: {ErrorLogService.LogPath}\n\nThis app stores your jewellery business data locally on this Windows computer using SQLite.";
    }

    public static void OpenTextReport(string title, string text)
    {
        var folder = BusinessSettingsService.GetPrintoutFolder();
        Directory.CreateDirectory(folder);
        var safeTitle = string.Join("-", title.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
        var path = Path.Combine(folder, safeTitle + "-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".txt");
        File.WriteAllText(path, text);
        OpenInDefaultApp(path);
    }

    public static void OpenInDefaultApp(string path)
    {
        Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
    }

    private static void AddFileIfExists(ZipArchive archive, string filePath, string entryName)
    {
        if (!File.Exists(filePath))
            return;

        var entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);
        using var input = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        using var output = entry.Open();
        input.CopyTo(output);
    }

    private static void AddDirectory(ZipArchive archive, string directory, string entryRoot)
    {
        foreach (var file in Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(directory, file).Replace('\\', '/');
            AddFileIfExists(archive, file, $"{entryRoot}/{relative}");
        }
    }

    private static void AddCsvExport<T>(ZipArchive archive, string entryName) where T : class
    {
        using var db = new AppDbContext();
        var rows = db.Set<T>().AsNoTracking().Cast<object>().ToList();
        var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var entry = archive.CreateEntry(entryName);
        using var writer = new StreamWriter(entry.Open(), Encoding.UTF8);
        writer.WriteLine(string.Join(",", props.Select(p => EscapeCsv(p.Name))));
        foreach (var row in rows)
        {
            writer.WriteLine(string.Join(",", props.Select(p => EscapeCsv(p.GetValue(row)?.ToString() ?? string.Empty))));
        }
    }

    private static string EscapeCsv(string value)
    {
        if (value.Contains('"') || value.Contains(',') || value.Contains('\n') || value.Contains('\r'))
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        return value;
    }

    private static List<string> ParseCsvLine(string line)
    {
        var values = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var ch = line[i];
            if (ch == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (ch == ',' && !inQuotes)
            {
                values.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(ch);
            }
        }
        values.Add(current.ToString());
        return values;
    }

    private static object? ConvertTextValue(string text, Type propertyType)
    {
        var nullableType = Nullable.GetUnderlyingType(propertyType);
        var targetType = nullableType ?? propertyType;
        var isNullable = nullableType != null || !targetType.IsValueType;
        if (string.IsNullOrWhiteSpace(text))
            return isNullable ? null : Activator.CreateInstance(targetType);
        if (targetType == typeof(string)) return text;
        if (targetType == typeof(int)) return int.Parse(text, CultureInfo.InvariantCulture);
        if (targetType == typeof(decimal)) return decimal.Parse(text, CultureInfo.InvariantCulture);
        if (targetType == typeof(bool)) return bool.Parse(text);
        if (targetType == typeof(DateTime)) return DateTime.Parse(text, CultureInfo.InvariantCulture);
        if (targetType.IsEnum) return Enum.Parse(targetType, text);
        return Convert.ChangeType(text, targetType, CultureInfo.InvariantCulture);
    }
}
