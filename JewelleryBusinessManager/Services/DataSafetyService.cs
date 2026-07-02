using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
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

    public static string CreateDataIntegrityReport()
    {
        DatabaseBootstrapper.Initialize();

        var folder = BusinessSettingsService.GetPrintoutFolder();
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, $"OPALNOVA-Data-Integrity-Check-{DateTime.Now:yyyyMMdd-HHmmss}.html");

        using var db = new AppDbContext();
        var issues = new List<DataIntegrityIssue>();

        var customerIds = db.Customers.AsNoTracking().Select(x => x.Id).ToHashSet();
        var supplierIds = db.Suppliers.AsNoTracking().Select(x => x.Id).ToHashSet();
        var materialIds = db.Materials.AsNoTracking().Select(x => x.Id).ToHashSet();
        var opalParcelIds = db.OpalParcels.AsNoTracking().Select(x => x.Id).ToHashSet();
        var stoneIds = db.Stones.AsNoTracking().Select(x => x.Id).ToHashSet();
        var jewelleryIds = db.JewelleryItems.AsNoTracking().Select(x => x.Id).ToHashSet();
        var jobIds = db.Jobs.AsNoTracking().Select(x => x.Id).ToHashSet();
        var saleIds = db.Sales.AsNoTracking().Select(x => x.Id).ToHashSet();
        var marketEventIds = db.MarketEvents.AsNoTracking().Select(x => x.Id).ToHashSet();
        var productionBatchIds = db.ProductionBatches.AsNoTracking().Select(x => x.Id).ToHashSet();
        var onlineListingIds = db.OnlineListings.AsNoTracking().Select(x => x.Id).ToHashSet();
        var purchaseOrderIds = db.PurchaseOrders.AsNoTracking().Select(x => x.Id).ToHashSet();
        var taskIds = db.BusinessTasks.AsNoTracking().Select(x => x.Id).ToHashSet();
        var quoteIds = db.CustomQuotes.AsNoTracking().Select(x => x.Id).ToHashSet();
        var quoteOptionIds = db.QuoteOptions.AsNoTracking().Select(x => x.Id).ToHashSet();
        var externalDiamondIds = db.ExternalDiamonds.AsNoTracking().Select(x => x.Id).ToHashSet();
        var photoIds = db.PhotoRecords.AsNoTracking().Select(x => x.Id).ToHashSet();
        var jewelleryIdsWithSales = db.Sales.AsNoTracking()
            .Where(s => s.JewelleryItemId.HasValue)
            .Select(s => s.JewelleryItemId!.Value)
            .ToHashSet();

        foreach (var job in db.Jobs.AsNoTracking())
        {
            AddMissingReference(issues, "Warning", "Jobs", $"Job #{job.Id} {job.JobCode}", "CustomerId", job.CustomerId, "Customers", customerIds);
            if (job.BalanceOwing < 0)
                AddIssue(issues, "Review", "Jobs", $"Job #{job.Id} {job.JobCode}", "BalanceOwing", "Negative balance. Confirm payment totals and final price.");
        }

        foreach (var quote in db.CustomQuotes.AsNoTracking())
        {
            AddMissingReference(issues, "Warning", "Quotes", $"Quote #{quote.Id} {quote.QuoteCode}", "CustomerId", quote.CustomerId, "Customers", customerIds);
            AddMissingReference(issues, "Warning", "Quotes", $"Quote #{quote.Id} {quote.QuoteCode}", "LinkedJobId", quote.LinkedJobId, "Jobs", jobIds);
            AddMissingReference(issues, "Warning", "Quotes", $"Quote #{quote.Id} {quote.QuoteCode}", "AcceptedOptionId", quote.AcceptedOptionId, "QuoteOptions", quoteOptionIds);
            if (!string.IsNullOrWhiteSpace(quote.ProposalLastPath) && !File.Exists(quote.ProposalLastPath))
                AddIssue(issues, "Warning", "Quotes", $"Quote #{quote.Id} {quote.QuoteCode}", "ProposalLastPath", $"Proposal file is missing: {quote.ProposalLastPath}");
        }

        foreach (var option in db.QuoteOptions.AsNoTracking())
        {
            AddRequiredReference(issues, "Error", "Quote Options", $"Option #{option.Id} {option.OptionName}", "CustomQuoteId", option.CustomQuoteId, "CustomQuotes", quoteIds);
            if (!string.IsNullOrWhiteSpace(option.ImagePath) && !File.Exists(option.ImagePath))
                AddIssue(issues, "Warning", "Quote Options", $"Option #{option.Id} {option.OptionName}", "ImagePath", $"Design image file is missing: {option.ImagePath}");
        }

        foreach (var link in db.QuoteOptionStoneLinks.AsNoTracking())
        {
            AddRequiredReference(issues, "Error", "Quote Stone Links", $"Quote stone link #{link.Id}", "QuoteOptionId", link.QuoteOptionId, "QuoteOptions", quoteOptionIds);
            AddRequiredReference(issues, "Error", "Quote Stone Links", $"Quote stone link #{link.Id}", "StoneId", link.StoneId, "Stones", stoneIds);
        }

        foreach (var link in db.QuoteOptionMaterialLinks.AsNoTracking())
        {
            AddRequiredReference(issues, "Error", "Quote Material Links", $"Quote material link #{link.Id}", "QuoteOptionId", link.QuoteOptionId, "QuoteOptions", quoteOptionIds);
            AddRequiredReference(issues, "Error", "Quote Material Links", $"Quote material link #{link.Id}", "MaterialId", link.MaterialId, "Materials", materialIds);
            if (link.Quantity <= 0)
                AddIssue(issues, "Review", "Quote Material Links", $"Quote material link #{link.Id}", "Quantity", "Quantity is zero or negative. Confirm the reservation line is intentional.");
        }

        foreach (var link in db.QuoteOptionExternalDiamondLinks.AsNoTracking())
        {
            AddRequiredReference(issues, "Error", "Quote Diamond Links", $"Quote diamond link #{link.Id}", "QuoteOptionId", link.QuoteOptionId, "QuoteOptions", quoteOptionIds);
            AddRequiredReference(issues, "Error", "Quote Diamond Links", $"Quote diamond link #{link.Id}", "ExternalDiamondId", link.ExternalDiamondId, "ExternalDiamonds", externalDiamondIds);
        }

        foreach (var material in db.Materials.AsNoTracking())
        {
            AddMissingReference(issues, "Warning", "Materials", $"Material #{material.Id} {material.MaterialCode}", "SupplierId", material.SupplierId, "Suppliers", supplierIds);
            if (material.CurrentQuantity < 0)
                AddIssue(issues, "Warning", "Materials", $"Material #{material.Id} {material.MaterialCode}", "CurrentQuantity", "Current quantity is negative. Review stock movements.");
        }

        foreach (var transaction in db.MaterialTransactions.AsNoTracking())
        {
            AddRequiredReference(issues, "Error", "Material Transactions", $"Material transaction #{transaction.Id}", "MaterialId", transaction.MaterialId, "Materials", materialIds);
            AddMissingReference(issues, "Warning", "Material Transactions", $"Material transaction #{transaction.Id}", "JobId", transaction.JobId, "Jobs", jobIds);
            AddMissingReference(issues, "Warning", "Material Transactions", $"Material transaction #{transaction.Id}", "JewelleryItemId", transaction.JewelleryItemId, "JewelleryItems", jewelleryIds);
        }

        foreach (var stone in db.Stones.AsNoTracking())
        {
            AddMissingReference(issues, "Warning", "Stones", $"Stone #{stone.Id} {stone.StoneCode}", "OpalParcelId", stone.OpalParcelId, "OpalParcels", opalParcelIds);
        }

        foreach (var item in db.JewelleryItems.AsNoTracking())
        {
            AddMissingReference(issues, "Warning", "Jewellery Stock", $"Stock #{item.Id} {item.StockCode}", "MainStoneId", item.MainStoneId, "Stones", stoneIds);
            if (item.Status == StockStatus.Sold && !jewelleryIdsWithSales.Contains(item.Id))
                AddIssue(issues, "Review", "Jewellery Stock", $"Stock #{item.Id} {item.StockCode}", "Status", "Marked Sold but no linked sale record was found.");
        }

        foreach (var sale in db.Sales.AsNoTracking())
        {
            AddMissingReference(issues, "Warning", "Sales", $"Sale #{sale.Id}", "CustomerId", sale.CustomerId, "Customers", customerIds);
            AddMissingReference(issues, "Warning", "Sales", $"Sale #{sale.Id}", "JobId", sale.JobId, "Jobs", jobIds);
            AddMissingReference(issues, "Warning", "Sales", $"Sale #{sale.Id}", "JewelleryItemId", sale.JewelleryItemId, "JewelleryItems", jewelleryIds);
            if (sale.SaleAmount <= 0)
                AddIssue(issues, "Review", "Sales", $"Sale #{sale.Id}", "SaleAmount", "Sale amount is zero or negative. Confirm this is intentional.");
        }

        foreach (var payment in db.Payments.AsNoTracking())
        {
            AddMissingReference(issues, "Warning", "Payments", $"Payment #{payment.Id}", "CustomerId", payment.CustomerId, "Customers", customerIds);
            AddMissingReference(issues, "Warning", "Payments", $"Payment #{payment.Id}", "JobId", payment.JobId, "Jobs", jobIds);
            AddMissingReference(issues, "Warning", "Payments", $"Payment #{payment.Id}", "SaleId", payment.SaleId, "Sales", saleIds);
            if (payment.Amount <= 0)
                AddIssue(issues, "Review", "Payments", $"Payment #{payment.Id}", "Amount", "Payment amount is zero or negative. Confirm refund/credit handling.");
            if (!payment.CustomerId.HasValue && !payment.JobId.HasValue && !payment.SaleId.HasValue)
                AddIssue(issues, "Review", "Payments", $"Payment #{payment.Id}", "Links", "Payment is not linked to a customer, job or sale.");
        }

        foreach (var marketStock in db.MarketStocks.AsNoTracking())
        {
            AddRequiredReference(issues, "Error", "Market Stock", $"Market stock #{marketStock.Id}", "MarketEventId", marketStock.MarketEventId, "MarketEvents", marketEventIds);
            AddRequiredReference(issues, "Error", "Market Stock", $"Market stock #{marketStock.Id}", "JewelleryItemId", marketStock.JewelleryItemId, "JewelleryItems", jewelleryIds);
            AddMissingReference(issues, "Warning", "Market Stock", $"Market stock #{marketStock.Id}", "SaleId", marketStock.SaleId, "Sales", saleIds);
            if (marketStock.SoldAtMarket && marketStock.ReturnedToStock)
                AddIssue(issues, "Warning", "Market Stock", $"Market stock #{marketStock.Id}", "Status", "Marked both sold at market and returned to stock.");
        }

        foreach (var batchItem in db.ProductionBatchItems.AsNoTracking())
        {
            AddRequiredReference(issues, "Error", "Production Batch Items", $"Batch item #{batchItem.Id}", "ProductionBatchId", batchItem.ProductionBatchId, "ProductionBatches", productionBatchIds);
            AddMissingReference(issues, "Warning", "Production Batch Items", $"Batch item #{batchItem.Id}", "JewelleryItemId", batchItem.JewelleryItemId, "JewelleryItems", jewelleryIds);
            AddMissingReference(issues, "Warning", "Production Batch Items", $"Batch item #{batchItem.Id}", "StoneId", batchItem.StoneId, "Stones", stoneIds);
            AddMissingReference(issues, "Warning", "Production Batch Items", $"Batch item #{batchItem.Id}", "JobId", batchItem.JobId, "Jobs", jobIds);
        }

        foreach (var listing in db.OnlineListings.AsNoTracking())
            AddMissingReference(issues, "Warning", "Online Listings", $"Listing #{listing.Id}", "JewelleryItemId", listing.JewelleryItemId, "JewelleryItems", jewelleryIds);

        foreach (var purchaseOrder in db.PurchaseOrders.AsNoTracking())
            AddMissingReference(issues, "Warning", "Purchase Orders", $"Purchase order #{purchaseOrder.Id} {purchaseOrder.PurchaseOrderCode}", "SupplierId", purchaseOrder.SupplierId, "Suppliers", supplierIds);

        foreach (var purchaseOrderItem in db.PurchaseOrderItems.AsNoTracking())
        {
            AddRequiredReference(issues, "Error", "Purchase Order Items", $"Purchase order item #{purchaseOrderItem.Id}", "PurchaseOrderId", purchaseOrderItem.PurchaseOrderId, "PurchaseOrders", purchaseOrderIds);
            AddMissingReference(issues, "Warning", "Purchase Order Items", $"Purchase order item #{purchaseOrderItem.Id}", "MaterialId", purchaseOrderItem.MaterialId, "Materials", materialIds);
            if (purchaseOrderItem.OrderedQuantity <= 0)
                AddIssue(issues, "Review", "Purchase Order Items", $"Purchase order item #{purchaseOrderItem.Id}", "OrderedQuantity", "Ordered quantity is zero or negative.");
        }

        foreach (var task in db.BusinessTasks.AsNoTracking())
        {
            AddMissingReference(issues, "Warning", "Tasks", $"Task #{task.Id} {task.TaskCode}", "CustomerId", task.CustomerId, "Customers", customerIds);
            AddMissingReference(issues, "Warning", "Tasks", $"Task #{task.Id} {task.TaskCode}", "JobId", task.JobId, "Jobs", jobIds);
            AddMissingReference(issues, "Warning", "Tasks", $"Task #{task.Id} {task.TaskCode}", "JewelleryItemId", task.JewelleryItemId, "JewelleryItems", jewelleryIds);
            AddMissingReference(issues, "Warning", "Tasks", $"Task #{task.Id} {task.TaskCode}", "StoneId", task.StoneId, "Stones", stoneIds);
            AddMissingReference(issues, "Warning", "Tasks", $"Task #{task.Id} {task.TaskCode}", "MarketEventId", task.MarketEventId, "MarketEvents", marketEventIds);
            AddMissingReference(issues, "Warning", "Tasks", $"Task #{task.Id} {task.TaskCode}", "ProductionBatchId", task.ProductionBatchId, "ProductionBatches", productionBatchIds);
            AddMissingReference(issues, "Warning", "Tasks", $"Task #{task.Id} {task.TaskCode}", "PurchaseOrderId", task.PurchaseOrderId, "PurchaseOrders", purchaseOrderIds);
        }

        foreach (var photo in db.PhotoRecords.AsNoTracking())
        {
            if (string.IsNullOrWhiteSpace(photo.FilePath) || !File.Exists(photo.FilePath))
                AddIssue(issues, "Warning", "Photos", $"Photo #{photo.Id}", "FilePath", $"Photo file is missing: {photo.FilePath}");

            if (string.IsNullOrWhiteSpace(photo.EntityType))
            {
                AddIssue(issues, "Warning", "Photos", $"Photo #{photo.Id}", "EntityType", "Photo has no linked entity type.");
                continue;
            }

            if (photo.EntityId <= 0)
            {
                AddIssue(issues, "Warning", "Photos", $"Photo #{photo.Id}", "EntityId", "Photo has no valid linked entity id.");
                continue;
            }

            if (!PhotoEntityExists(photo.EntityType, photo.EntityId, customerIds, supplierIds, materialIds, stoneIds, jewelleryIds, jobIds, saleIds, marketEventIds, productionBatchIds, onlineListingIds, purchaseOrderIds, taskIds, quoteIds, quoteOptionIds, externalDiamondIds, photoIds))
                AddIssue(issues, "Warning", "Photos", $"Photo #{photo.Id}", "Entity Link", $"Linked {photo.EntityType} #{photo.EntityId} was not found.");
        }

        var html = BuildDataIntegrityHtml(db, issues);
        File.WriteAllText(path, html);
        return path;
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
<p class="meta">Generated locally by OPALNOVA. Manual version V2.6.0.</p>
<div class="box toc"><h2>Contents</h2><a href="#setup">1. Setup and daily rhythm</a><a href="#quotes">2. Quotes and proposals</a><a href="#production">3. Production workflow</a><a href="#payments">4. Payments, invoices and handover</a><a href="#inventory">5. Inventory and supplier diamonds</a><a href="#reports">6. Reports and bookkeeping review</a><a href="#safety">7. Backups, restore and data safety</a><a href="#release">8. Release testing routine</a></div>
<div class="box ok"><h2>Best starting point</h2><p>Use the main workflow homes first: Quotes & Proposals, Production, Payments & Sales, Inventory, Reports, and Settings & Backup. Use Search All for records, saved views and workflow actions when you know what you need but not where it lives. Specialist studios are for deeper work once the daily workflow is clear.</p></div>
<div class="box" id="setup"><h2>1. Setup and daily rhythm</h2><ol><li>Open <b>Settings</b> and confirm business name, contact details, logo, document footer, tax/GST settings, printout folder and backup folder.</li><li>Add suppliers, customers, materials, stones and jewellery stock before relying on reports.</li><li>Use the dashboard setup-readiness card and Alert Centre to find missing setup, overdue work, low stock and follow-ups.</li><li>Use <b>Search All</b> to find customers, jobs, quotes, quote options, supplier diamonds, saved views and workflow actions from one window.</li><li>Use the dashboard <b>Recent Work</b> panel to reopen workflow tabs, generated reports and saved record editors from the current session.</li><li>Use Customer Relationship Studio for customer summary cards, timelines, communication templates, value guidance and follow-up creation.</li><li>When closing changed record editor tabs, choose Save, Discard or Cancel from the unsaved-change prompt.</li><li>Run <b>Create Backup</b> before importing CSV files, restoring data, bulk cleanup, or major stock status changes.</li></ol><p class="small">Daily habit: open Alert Centre, review Project Workbench, process payments and follow-ups, then back up after important changes.</p></div>
<div class="box" id="quotes"><h2>2. Quotes and proposals</h2><ol><li>Use <b>Custom Quote Builder</b> for multi-option custom work. Keep customer details, expiry dates, occasion, required-by date, ring size, budget, preferred metal and preferred stone current.</li><li>After selecting a customer, use <b>Use Customer Preferences</b> to fill blank ring size, preferred metal and preferred stone fields from that customer profile.</li><li>Add quote options with material, labour, stone, setting, finding and other costs. Mark the best option as recommended when appropriate.</li><li>Attach design images to quote options when they help the customer compare designs.</li><li>Save before leaving the quote workspace. If you close a quote tab with unsaved changes, OPALNOVA prompts you to save, discard or cancel.</li><li>Preview the proposal, then use the proposal send workflow to copy/open an email draft and record when it was sent.</li><li>Proposal files are revisioned automatically and include a Print / Save as PDF button for browser-based PDF output.</li><li>Use <b>Proposal Pipeline</b> to review prepared proposals, sent proposals, due follow-ups, accepted quotes and converted jobs from one workspace queue.</li><li>After customer approval, mark the accepted option and convert it into a production job.</li></ol><div class="box warn"><h3>Quote caution</h3><p>Do not mark an option accepted until the customer has clearly approved it. Accepted quotes can drive job creation, reserved stock and follow-up actions. Internal quote notes stay private and are not printed in proposal output.</p></div></div>
<div class="box" id="production"><h2>3. Production workflow</h2><ol><li>Use <b>Production Board</b> to move jobs through approval, materials, bench work, setting, polishing, quality check, pickup/shipping and completion.</li><li>Select a job on Production Board and use <b>Open Payments</b> when the next step is deposit, balance, receipt, sale or handover work.</li><li>Use <b>Capacity Snapshot</b> from Production Board, Production studio, Production &amp; Opal Studio or Reports Studio to review due-date buckets, recorded labour hours, active batches and scheduling risk before promising new dates.</li><li>Use <b>Stage Checklist</b> from the Production Board, Production studio, Production &amp; Opal Studio or Documents Studio to review customer contact, quote acceptance, reservations, supplier waits, payment position, open tasks and linked job photos before moving a job forward.</li><li>Keep due dates and labour hours realistic. Alert Centre, Production Board and capacity reporting depend on dates, statuses and labour estimates.</li><li>Use job cards, workshop notes and batch reports when preparing bench work or market collections.</li><li>Use the job completion checklist when a job is physically ready. It can consume reserved materials, set reserved stones and release unused reservations.</li></ol><div class="box warn"><h3>Completion caution</h3><p>Material consumption should be reviewable. Check linked quote reservations before completing a job so stock movement remains traceable.</p></div></div>
<div class="box" id="payments"><h2>4. Payments, invoices and handover</h2><ol><li>Use <b>Payment & Collection</b> near the end of production to record deposits, balance payments, pickup/shipping status and sale creation.</li><li>When opened from a selected Production Board card, Payment & Collection focuses that job and switches to All Jobs if the default filter would hide it.</li><li>Review the Payment Schedule panel for deposit target, final balance target, paid amount, remaining amount and timing guidance before handover.</li><li>Use the live handover checklist before collection or shipping so payment, item condition, customer notification, care instructions and document readiness are recorded in the handover notes path.</li><li>Open a saved job record to review its payment history panel with total, paid, balance and linked ledger rows.</li><li>For market sales, use Market Operations or Record Market Sale so the sale record, stock status, market stock row and reconciliation totals stay aligned.</li><li>Generate invoices, receipts, deposit receipts and payment receipts from Documents Studio or Payment & Collection.</li><li>Generate a handover confirmation when an item is collected or shipped so the customer, job, payment state, checklist and sign-off are recorded together.</li><li>Create a thank-you follow-up after handover when after-care, cleaning or adjustment check-in is useful.</li><li>Use Copy Balance Reminder when a customer-ready balance message is needed, and Create Balance Follow-Up when the reminder should also appear in the task queue.</li><li>Create pickup/handover reminders only when an open reminder does not already exist for the job.</li><li>Check customer details, payment method, reference, total paid and balance before printing or sending paperwork.</li><li>Run <b>Outstanding Balances</b> before pickup days so unpaid balances are not missed.</li></ol><p class="small">Avoid manually creating duplicate sales for the same completed job. Use the guided handover workflow where possible.</p></div>
<div class="box" id="inventory"><h2>5. Inventory and supplier diamonds</h2><ol><li>Use Jewellery Stock, Stones and Materials for owned physical stock.</li><li>Use the record detail <b>+ Photos</b> action to attach one or more image files to stock, stones, materials, jobs and other saved records.</li><li>Use <b>Jeweller Tools</b> from Pricing Studio or Hardware & POS Studio for quick ring-size reference, metal-weight estimates and stone-carat estimates before final manual checks.</li><li>Use Saved External Diamonds for supplier diamonds that are not yet owned inventory.</li><li>Use Supplier Holds & Orders to track hold expiry, ordered status, received status, replacement search criteria and conversion to owned stones.</li><li>Use Nivoda Staging Handoff from Diamonds, Diamond Supplier Studio or the Nivoda search window when Nivoda needs a non-secret API setup report with endpoint, GraphiQL and schema diagnostics.</li><li>Use Copy Replacement Search when a supplier diamond is expired, unavailable, unsuitable or needs a backup option. It copies target criteria and close saved alternatives already in OPALNOVA.</li><li>Use Stock Movement for material receive/use/adjust/return actions.</li><li>Use Change Inventory Status when a jewellery item or stone needs a lifecycle update, and read the lifecycle guidance before saving.</li><li>Run Stock Ageing, Inventory Value, Reserved Inventory and Opal / Stone Stock reports before buying more stock or preparing markets. These reports now explain owned, reserved, supplier, sold and consumed states.</li></ol><div class="box warn"><h3>Supplier stock caution</h3><p>Do not treat supplier diamonds as owned inventory until they are received and explicitly converted into owned loose stone records. Confirm live availability and price with the supplier before promising a replacement stone. Keep live API hold/order actions disabled until Nivoda confirms the accessible mutation names and required payloads for your account.</p></div></div>
<div class="box" id="reports"><h2>6. Reports and bookkeeping review</h2><div class="grid"><div><h3>Weekly review</h3><ul><li>BI Command Report</li><li>Operations Performance</li><li>Visual Charts</li><li>Weekly Sales</li><li>Customer Follow-Ups</li><li>Outstanding Balances</li></ul></div><div><h3>Stock review</h3><ul><li>Inventory Intelligence</li><li>Inventory Value</li><li>Stock Ageing</li><li>Reserved Inventory</li><li>Opal / Stone Stock</li><li>Low Stock / Reorder reports</li></ul></div><div><h3>Bookkeeping review</h3><ul><li>Monthly Sales</li><li>Profitability</li><li>Tax / GST Summary</li><li>Export BI Excel</li><li>Export BI CSV</li></ul></div></div><p class="small">Reports are snapshots. Re-run them after major data changes and compare totals against known jobs, sales and payment records. Use Operations Performance as the weekly operations checkpoint and Inventory Intelligence before buying stock, ordering materials, discounting older pieces or reviewing supplier diamonds.</p></div>
<div class="box" id="safety"><h2>7. Backups, restore and data safety</h2><ol><li><b>Create Backup</b> saves a copy of the active SQLite database.</li><li><b>Health Check</b> checks database access, record counts, missing photo links, low stock and overdue jobs.</li><li><b>Data Integrity</b> creates a read-only report for orphaned links, missing proposal/design/photo files, inconsistent market stock states, negative stock quantities and payment records needing review.</li><li><b>Decision Review</b> creates a read-only post-V2 planning report so installer/update, email, supplier API, scheduling, cloud/shared-device and navigation decisions stay explicit before broad systems are added.</li><li><b>Installer/Update Readiness</b> creates a read-only packaging and update-channel checklist before installer or auto-update behavior is added.</li><li><b>Installer Validation Checklist</b> creates a read-only portable-first validation plan with executable fingerprint, data boundaries, update rehearsal gates and hold conditions before installer assets are created.</li><li><b>Portable Build Manifest</b> creates a read-only inventory of the current publish folder, executable version/hash and private-data exclusions before handoff or packaging.</li><li><b>Packaging Decision Record</b> records that portable handoff is the validated route and that MSIX/Inno packaging requires an explicit next ticket.</li><li><b>Roadmap Completion Record</b> records that the current no-schema version stream is complete and lists the remaining large decisions that require explicit approval before implementation.</li><li><b>Support Snapshot</b> creates a read-only HTML page with version, executable, database, backup, printout, photo, settings and log paths for troubleshooting.</li><li><b>Export Bundle</b> creates a private ZIP containing a database snapshot, settings, photos and CSV exports.</li><li><b>Restore Backup</b> validates the selected database or export bundle and shows a restore preview before staging the restore.</li><li><b>Import CSV</b> creates new records from matching column headers. It does not edit existing records by ID.</li></ol><div class="box warn"><h3>High-risk actions</h3><p>Restore, import, bulk status updates and cleanup tools can change many records. Create a backup first and close any other running copy of OPALNOVA before restore work. Data Integrity, Decision Review, Installer/Update Readiness, Installer Validation Checklist, Portable Build Manifest, Packaging Decision Record and Roadmap Completion Record are read-only, but any manual fixes after reviewing them should still start with a backup.</p></div></div>
<div class="box" id="release"><h2>8. Release testing routine</h2><ol><li>Open the latest version-specific testing checklist from the project folder.</li><li>Launch the published app and confirm the header and About version.</li><li>Run the new feature from both quick workspace and specialist studio entry points when both exist.</li><li>Open <b>Release Readiness</b> from Settings &amp; Backup or Safety &amp; Data Studio to review packaging notes, validation gates, staging cautions and generated document checks.</li><li>Open <b>Decision Review</b> before choosing a broad post-V2 product direction such as installer/update, direct email, supplier ordering, scheduling, cloud sync or navigation redesign.</li><li>Open <b>Installer/Update Readiness</b> before packaging, copying or installing a build for another machine.</li><li>Open <b>Installer Validation Checklist</b> before turning the publish folder into MSIX, Inno Setup or another installer format.</li><li>Open <b>Portable Build Manifest</b> from the published app before sharing or packaging the publish folder.</li><li>Open <b>Packaging Decision Record</b> to confirm portable handoff is complete and installer packaging remains an explicit future choice.</li><li>Open <b>Roadmap Completion Record</b> when deciding whether the current version stream is finished or a new major product direction should start.</li><li>Open <b>Support Snapshot</b> when install, publish, backup path, printout path, photo path or log-path details need to be shared for support.</li><li>Confirm reports/documents open in preview and with Open HTML / Print where applicable.</li><li>Run a quick safety check: existing records still load, no unexpected records were added, and the app closes cleanly.</li></ol><p class="small">For development validation, each major build should pass debug build, release publish and published executable launch smoke before being committed and pushed.</p></div>
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
<div class="card"><h2>V2.6.0 <span class="tag">Roadmap Completion Record</span></h2><ul><li>Added a read-only Roadmap Completion Record report for the current no-schema version stream.</li><li>The report records completed tracks, remaining explicit major decisions and the stop condition for further automatic version passes.</li><li>Added Roadmap Completion Record actions in Settings &amp; Backup and Safety &amp; Data Studio, with action-specific mini-guide metadata and Search All workflow discoverability.</li><li>Kept the pass no-schema and did not create installer files, shortcuts, update feeds, background jobs, task records, data moves, supplier mutations, hardware dependencies or schema changes.</li></ul></div>
<div class="card"><h2>V2.5.0 <span class="tag">Packaging Decision Record</span></h2><ul><li>Added a read-only Packaging Decision Record report to close the installer/update readiness track.</li><li>The report records portable handoff as the validated route and keeps MSIX/Inno Setup as explicit future packaging decisions.</li><li>Added Packaging Decision Record actions in Settings &amp; Backup and Safety &amp; Data Studio, with action-specific mini-guide metadata and Search All workflow discoverability.</li><li>Kept the pass no-schema and did not create installer files, shortcuts, update feeds, background jobs, task records, data moves or schema changes.</li></ul></div>
<div class="card"><h2>V2.4.0 <span class="tag">Portable Build Manifest</span></h2><ul><li>Added a read-only Portable Build Manifest report for the current publish/app folder.</li><li>The report records executable version/hash, publish-folder signal, top-level file inventory, total size, private-data exclusions, support-path context and handoff decision notes.</li><li>Added Portable Build Manifest actions in Settings &amp; Backup and Safety &amp; Data Studio, with action-specific mini-guide metadata and Search All workflow discoverability.</li><li>Kept the pass no-schema and did not create installer files, shortcuts, update feeds, background jobs, task records, data moves or schema changes.</li></ul></div>
<div class="card"><h2>V2.3.0 <span class="tag">Installer Validation Checklist</span></h2><ul><li>Chose the portable publish folder as the first installer/update validation route before creating installer assets.</li><li>Added a read-only Installer Validation Checklist report with executable fingerprint, publish-folder signal, local data boundaries, update rehearsal gates, rollback checks and hold conditions.</li><li>Added Installer Validation Checklist actions in Settings &amp; Backup and Safety &amp; Data Studio, with action-specific mini-guide metadata and Search All workflow discoverability.</li><li>Kept the pass no-schema and did not create installer files, shortcuts, update feeds, background jobs, task records, data moves or schema changes.</li></ul></div>
<div class="card"><h2>V2.2.0 <span class="tag">Installer Update Readiness</span></h2><ul><li>Chose installer/update readiness as the first concrete post-V2 product direction.</li><li>Added a read-only Installer/Update Readiness report for runtime paths, portable publish-folder signals, installer decisions, update-channel boundaries and manual distribution checks.</li><li>Added Installer/Update Readiness actions in Settings &amp; Backup and Safety &amp; Data Studio, with action-specific mini-guide metadata and Search All workflow discoverability.</li><li>Kept the pass no-schema and did not add installer creation, auto-update behavior, shortcut creation, background scheduling or database-location changes.</li></ul></div>
<div class="card"><h2>V2.1.0 <span class="tag">Post-V2 Decision Review</span></h2><ul><li>Added a read-only Post-V2 Decision Review report for the product choices that should be settled before broad new systems are added.</li><li>Added Decision Review actions in Settings &amp; Backup and Safety &amp; Data Studio.</li><li>The report summarizes local data footprint, current workflow evidence and decision guidance for multi-user/cloud sync, direct email delivery, supplier API ordering, scheduling/capacity, navigation and installer/update direction.</li><li>Preserved database schema and existing quote, production, payment, inventory, supplier diamond, Nivoda staging, backup, restore, support snapshot and report behavior.</li></ul></div>
<div class="card"><h2>V2.0.0 <span class="tag">Release Candidate Validation</span></h2><ul><li>Completed a release-candidate validation pass across the V1.91-V1.99 working set before the V2.0 checkpoint.</li><li>Confirmed selector prompt coverage, help-guide key uniqueness and per-section tool-action title uniqueness with static checks.</li><li>Refreshed visible/project version metadata, release notes, user guide version text, roadmap, forward plan, future plan, version report and testing checklist for the V2.0 baseline.</li><li>Preserved database schema and existing quote, production, payment, inventory, supplier diamond, Nivoda staging, backup, restore, support snapshot and report behavior.</li></ul></div>
<div class="card"><h2>V1.99.0 <span class="tag">Pre-Milestone Hardening</span></h2><ul><li>Reviewed the V1.94-V1.98 selector, workflow-surface and support-polish changes before the V2.0 milestone.</li><li>Corrected Customer Relationship Studio help routing so Customer Timeline opens its specific mini guide instead of falling back to the broad section guide.</li><li>Removed duplicate Communication Templates help metadata from the section guide map while preserving the action-specific help entry.</li><li>Kept published ProductVersion metadata on the clean OPALNOVA release label without an SDK source-revision suffix.</li><li>Preserved database schema and existing quote, production, payment, inventory, supplier diamond, backup, restore, support snapshot and report behavior.</li></ul></div>
<div class="card"><h2>V1.98.0 <span class="tag">Support Snapshot Polish</span></h2><ul><li>Added a Support Snapshot report with version, executable, app folder, database, backup, printout, photo, settings, saved-view and error-log paths.</li><li>Added Support Snapshot actions in Settings &amp; Backup and Safety &amp; Data Studio so troubleshooting can start from exact local path and file-status context.</li><li>Updated the user guide and workflow help metadata to include the support snapshot without exposing customer records or supplier credentials.</li></ul></div>
<div class="card"><h2>V1.97.0 <span class="tag">Daily Workflow Edge Polish</span></h2><ul><li>Added an Open Payments action to the Production Board so a selected job can move directly into the payment, receipt, sale and handover workflow.</li><li>Payment &amp; Collection can now open focused on a specific job and switches to All Jobs when the default handover filter would hide that selected production job.</li><li>Cleaned visible Payment &amp; Collection list separators to plain ASCII text.</li></ul></div>
<div class="card"><h2>V1.96.0 <span class="tag">Workspace Surface Reduction</span></h2><ul><li>Compressed high-use workflow headers, metric rows and selected-detail panels so more of the workspace is available for records and action content.</li><li>Reduced redundant explanatory copy in Alert Centre, Project Workbench, Proposal Pipeline, Payment &amp; Collection, Production Board, Supplier Diamond Holds &amp; Orders and Stock Movement without changing workflow behavior.</li></ul></div>
<div class="card"><h2>V1.95.0 <span class="tag">Workflow Control Consolidation</span></h2><ul><li>Routed remaining high-priority workflow Button and TextBox styles through the shared OPALNOVA control templates while preserving local spacing and colours.</li><li>Added explicit prompt text to every ComboBox declaration so the shared selector empty-state path shows workflow-specific guidance.</li></ul></div>
<div class="card"><h2>V1.94.0 <span class="tag">Selector Theme Polish</span></h2><ul><li>Added shared empty-state prompts to ComboBox selectors so unselected fields show friendly muted guidance instead of blank faces.</li><li>Darkened DatePicker text boxes and calendar buttons globally, routed Payment &amp; Collection and Production Board selectors through the shared OPALNOVA control styles, and cleaned high-use selector/list display separators to plain ASCII.</li></ul></div>
<div class="card"><h2>V1.93.0 <span class="tag">Inventory Reorder Intelligence</span></h2><ul><li>Added an Inventory Intelligence report combining valuation by category, low-stock reorder coverage, slow-moving stock guidance, supplier diamond state and material adjustment audit signals.</li><li>Added Inventory Intelligence actions in Inventory, Reports, Inventory Studio and Reports Studio without changing database schema or stock quantities.</li></ul></div>
<div class="card"><h2>V1.92.0 <span class="tag">Operations Performance Reporting</span></h2><ul><li>Added an Operations Performance report combining workshop productivity, supplier diamond state, market results and suggested report cadence.</li><li>Added Operations Performance actions in Reports and Reports Studio without changing database schema or creating background scheduled tasks.</li></ul></div>
<div class="card"><h2>V1.91.0 <span class="tag">Nivoda Staging Readiness</span></h2><ul><li>Added a non-secret Nivoda Staging Handoff report with configured endpoint, GraphiQL URL, optional external review URL, authentication status and accessible GraphQL schema fields.</li><li>Added Nivoda environment/review URL settings in Diamond Supplier API and a ready-to-host static handoff page under <code>docs/nivoda-staging/</code>.</li><li>Started customer segment guidance in customer summaries, timelines, communication templates and follow-up notes using existing records without schema changes.</li></ul></div>
<div class="card"><h2>V1.90.0 <span class="tag">Stability Milestone</span></h2><ul><li>Completed a redundancy and consistency checkpoint across V1.81 through V1.89 before the whole-number git milestone.</li><li>Confirmed the recent additions preserve database schema, keep cross-studio action repetition intentional, and pass debug build, release publish and launch smoke validation.</li></ul></div>
<div class="card"><h2>V1.89.0 <span class="tag">Release Readiness Prep</span></h2><ul><li>Added a Release Readiness report covering runtime paths, database/photo/settings paths, validation gates, packaging notes, staging cautions and generated document checks.</li><li>Added Release Readiness entry points in Settings &amp; Backup, Safety &amp; Data Studio and Search All workflow actions while keeping installer, shortcut and update-channel choices deferred.</li></ul></div>
<div class="card"><h2>V1.88.0 <span class="tag">Practical Jeweller Tools</span></h2><ul><li>Added a dark themed Jeweller Tools window with ring-size reference, metal-weight estimator and faceted-stone carat estimator.</li><li>Added Jeweller Tools entry points in Pricing Studio, Hardware &amp; POS Studio and Search All workflow actions without changing the database schema.</li></ul></div>
<div class="card"><h2>V1.87.0 <span class="tag">Workflow Search Finder</span></h2><ul><li>Expanded Search All to include Custom Quotes, Quote Options and External Diamonds so newer workflow records are searchable from the global search window.</li><li>Added searchable Workflow Actions that navigate to daily priorities, Project Workbench, quotes, production, payments, inventory, supplier diamonds, reports, backups, data integrity, customer relationship, market and hardware/tool areas.</li></ul></div>
<div class="card"><h2>V1.86.0 <span class="tag">Data Integrity Check</span></h2><ul><li>Added a read-only Data Integrity report for orphaned links, missing proposal/design/photo files, inconsistent market stock states, negative stock quantities and incomplete payment links.</li><li>Added Data Integrity entry points on the dashboard Data Safety card, Settings &amp; Backup and Safety &amp; Data Studio without changing the database schema.</li></ul></div>
<div class="card"><h2>V1.85.0 <span class="tag">Proposal Revision PDF-Ready Polish</span></h2><ul><li>Proposal HTML output now uses explicit revisioned filenames and displays generated time plus revision label in the customer-facing document.</li><li>Proposal output now includes a Print / Save as PDF button, and Send / Record Proposal can copy browser print-to-PDF steps for customer PDF delivery.</li></ul></div>
<div class="card"><h2>V1.84.0 <span class="tag">Production Capacity Snapshot</span></h2><ul><li>Added a no-schema Production Capacity Snapshot report based on existing job due dates, job labour hours and active production batches.</li><li>Added Capacity Snapshot actions in Production Board, Production workflow, Production & Opal Studio and Reports Studio.</li></ul></div>
<div class="card"><h2>V1.83.0 <span class="tag">Supplier Diamond Replacement Readiness</span></h2><ul><li>Added Copy Replacement Search in Supplier Diamond Holds & Orders to copy replacement criteria for the selected supplier diamond.</li><li>The copied note includes target type, shape, carat range, colour/clarity, lab, original certificate, quote/customer context and close saved alternatives already in OPALNOVA.</li></ul></div>
<div class="card"><h2>V1.82.0 <span class="tag">Inventory Media Batch Workflow</span></h2><ul><li>Updated the record detail photo action to support selecting and importing multiple image files at once.</li><li>Batch imports reuse existing OPALNOVA photo storage and `PhotoRecord` links, preserving the current local database schema.</li></ul></div>
<div class="card"><h2>V1.81.0 <span class="tag">Market POS Speed Polish</span></h2><ul><li>Routed Market Operations sales through the shared market sale workflow so sale records, sold stock state, market stock rows and reconciliation totals stay aligned.</li><li>Improved market stock state display, returned-stock safety checks, packed-state handling, and end-of-day reconciliation guidance.</li></ul></div>
<div class="card"><h2>V1.80.0 <span class="tag">Stability Milestone</span></h2><ul><li>Completed a redundancy and consistency review across the V1.76 production checklist, V1.77 Recent Work, V1.78 payment schedule guidance and V1.79 stock lifecycle guidance work.</li><li>Confirmed the recent workflow additions remain advisory/read-only where intended and do not create payment, stock movement, reservation or supplier-diamond state changes just by opening guidance or reports.</li></ul></div>
<div class="card"><h2>V1.79.0 <span class="tag">Stock Lifecycle Clarity</span></h2><ul><li>Added shared lifecycle guidance for jewellery stock, stones, quote reservations and supplier diamonds.</li><li>Inventory status changes, supplier diamond workflow and inventory reports now distinguish owned, reserved, supplier, sold and consumed states more clearly.</li></ul></div>
<div class="card"><h2>V1.78.0 <span class="tag">Payment Schedule Guidance</span></h2><ul><li>Added shared payment schedule guidance for quotes and jobs using existing quote, job and payment records.</li><li>Payment & Collection, proposal output and job payment summaries now show deposit and final-balance stages with paid and remaining amounts.</li></ul></div>
<div class="card"><h2>V1.77.0 <span class="tag">Recent Work Recall</span></h2><ul><li>Added a dashboard Recent Work panel for reopening workspace tabs, generated reports and saved record editors from the current session.</li><li>Recent entries deduplicate automatically, can be cleared, and preserve existing tab close and unsaved-change behaviour.</li></ul></div>
<div class="card"><h2>V1.76.0 <span class="tag">Production Stage Checklist</span></h2><ul><li>Added a Production Stage Checklist report for selected jobs, covering stage readiness, waiting warnings, quote context, reservations, supplier diamond state, payments, linked tasks and job photos/files.</li><li>Added Stage Checklist actions in Production Board, Production studio, Production & Opal Studio and Documents Studio.</li></ul></div>
<div class="card"><h2>V1.75.0 <span class="tag">Customer Communication Templates</span></h2><ul><li>Added customer-specific communication templates in Customer Relationship Studio.</li><li>Customer summaries, timelines, relationship reports and generated follow-up task notes now include lifetime value guidance and repeat follow-up suggestions.</li></ul></div>
<div class="card"><h2>V1.74.0 <span class="tag">Payment Handover Checklist</span></h2><ul><li>Added a live Handover Checklist panel to Payment & Collection.</li><li>Checklist summaries flow into pickup reminders, handover confirmations, ready/complete notes, sale notes and job completion notes.</li></ul></div>
<div class="card"><h2>V1.73.0 <span class="tag">Hosted Editor Unsaved Change Guard</span></h2><ul><li>Added Save, Discard and Cancel protection when closing changed hosted record editor tabs.</li><li>Close-prompt saving routes through the existing record editor save event, so normal business rules and database persistence still apply.</li></ul></div>
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

    public static string CreateReleaseReadinessReport()
    {
        var folder = BusinessSettingsService.GetPrintoutFolder();
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, $"OPALNOVA-Release-Readiness-{DateTime.Now:yyyyMMdd-HHmmss}.html");
        var settings = BusinessSettingsService.Load();
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? assembly.GetName().Version?.ToString()
            ?? "unknown";
        var processPath = Environment.ProcessPath ?? "unknown";
        var appFolder = AppContext.BaseDirectory;

        var html = $$$"""
<!doctype html>
<html><head><meta charset="utf-8"><title>OPALNOVA Release Readiness</title>
<style>body{font-family:Segoe UI,Arial,sans-serif;margin:32px;line-height:1.5;color:#1f2937;background:#f8fafc}h1,h2,h3{color:#111827}.meta{color:#6b7280}.grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(260px,1fr));gap:14px}.card{background:#fff;border:1px solid #d1d5db;border-radius:10px;padding:16px;margin:14px 0}.warn{border-left:5px solid #b45309;background:#fff7ed}.ok{border-left:5px solid #047857;background:#ecfdf5}code{background:#e5e7eb;padding:2px 5px;border-radius:4px}li{margin:5px 0}@media print{body{background:#fff;margin:12mm}.card{break-inside:avoid}}</style></head>
<body>
<h1>OPALNOVA Release Readiness</h1>
<p class="meta">Generated locally: {{{Html(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))}}}<br>Installed version: {{{Html(version)}}}</p>
<div class="grid">
<div class="card"><h2>Runtime</h2><p><b>Executable:</b><br>{{{Html(processPath)}}}</p><p><b>App folder:</b><br>{{{Html(appFolder)}}}</p></div>
<div class="card"><h2>Business Data</h2><p><b>Database:</b><br>{{{Html(DatabaseBootstrapper.DatabasePath)}}}</p><p><b>Photos:</b><br>{{{Html(DatabaseBootstrapper.PhotoDirectory)}}}</p></div>
<div class="card"><h2>Business Settings</h2><p><b>Business:</b> {{{Html(settings.BusinessName)}}}</p><p><b>Backups:</b><br>{{{Html(BusinessSettingsService.GetBackupFolder())}}}</p><p><b>Printouts:</b><br>{{{Html(BusinessSettingsService.GetPrintoutFolder())}}}</p></div>
</div>
<div class="card ok"><h2>Release Gate Checklist</h2><ol><li>Run Debug build and confirm zero warnings and zero errors.</li><li>Run Release publish for <code>win-x64</code> self-contained output.</li><li>Launch the published <code>OPALNOVA.exe</code>, confirm the header/About version, then close cleanly.</li><li>Create a fresh backup and confirm the backup folder is accessible.</li><li>Run Health Check and Data Integrity from Safety &amp; Data Studio.</li><li>Open Release Notes and User Guide from inside the app.</li><li>Open core workflows: dashboard, Search All, Project Workbench, Alert Centre, quotes, proposal pipeline, production, payments, inventory, supplier diamonds, reports and data safety.</li><li>Generate one customer-facing document and one business report, then confirm OPALNOVA branding, readable text, current version/release context where shown, and print layout.</li></ol></div>
<div class="card"><h2>Packaging Notes</h2><ul><li>Current release output is the published folder containing <code>OPALNOVA.exe</code> and self-contained runtime files.</li><li>Do not distribute only the executable unless publish settings are changed and validated. Use the full publish folder for portable testing.</li><li>Installer technology remains a release decision. Good candidates are MSIX for Windows-managed installs or Inno Setup for a traditional installer.</li><li>A desktop shortcut should point to the installed <code>OPALNOVA.exe</code>. Shortcut creation should be owned by the installer, not by normal app startup.</li></ul></div>
<div class="card warn"><h2>Production / Staging Separation</h2><ul><li>OPALNOVA currently uses the standard local database path: <code>{{{Html(DatabaseBootstrapper.DatabasePath)}}}</code>.</li><li>Do not test destructive restore/import flows against real business data without a fresh backup.</li><li>For staged release testing, use a separate Windows profile, VM, or copied database folder until a formal staging configuration is introduced.</li><li>Nivoda credentials remain user-entered. Do not package real supplier credentials in builds, scripts, reports or screenshots.</li></ul></div>
<div class="card"><h2>Installer Decision Record</h2><ul><li><b>Installer:</b> not created in this pass.</li><li><b>Desktop shortcut:</b> deferred to installer packaging.</li><li><b>Update/version check:</b> design remains manual release-notes verification until a trusted update channel is chosen.</li><li><b>Automatic backups:</b> app-level reminders exist through health/readiness surfaces; OS scheduling remains deferred until lifecycle support is explicit.</li></ul></div>
<div class="card"><h2>Generated Document Review</h2><ul><li>Proposal HTML should show OPALNOVA branding, proposal revision and print/PDF guidance.</li><li>Invoices, receipts and handover confirmations should show customer/job/payment details with readable headings.</li><li>Reports should open locally, avoid broken special characters, and print without clipped headers.</li><li>Release notes and user guide should be available from Settings &amp; Backup and Safety &amp; Data Studio.</li></ul></div>
</body></html>
""";
        File.WriteAllText(path, html);
        return path;
    }

    public static string CreateInstallerUpdateReadinessReport()
    {
        var folder = BusinessSettingsService.GetPrintoutFolder();
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, $"OPALNOVA-Installer-Update-Readiness-{DateTime.Now:yyyyMMdd-HHmmss}.html");
        var settings = BusinessSettingsService.Load();
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? assembly.GetName().Version?.ToString()
            ?? "unknown";
        var processPath = Environment.ProcessPath ?? "unknown";
        var appFolder = AppContext.BaseDirectory;
        var backupFolder = BusinessSettingsService.GetBackupFolder();
        var printoutFolder = BusinessSettingsService.GetPrintoutFolder();
        var isPublishFolder = IsPublishFolder(appFolder);
        var executableSummary = FileSummary(processPath);
        var appFolderSummary = FolderSummary(appFolder);
        var backupSummary = FolderSummary(backupFolder);
        var printoutSummary = FolderSummary(printoutFolder);
        var databaseSummary = FileSummary(DatabaseBootstrapper.DatabasePath);
        var settingsSummary = FileSummary(BusinessSettingsService.SettingsPath);
        var supportSnapshotName = $"OPALNOVA-Support-Snapshot-{DateTime.Now:yyyyMMdd-HHmmss}.html";

        var html = $$$"""
<!doctype html>
<html><head><meta charset="utf-8"><title>OPALNOVA Installer Update Readiness</title>
<style>body{font-family:Segoe UI,Arial,sans-serif;margin:32px;line-height:1.5;color:#1f2937;background:#f8fafc}h1,h2,h3{color:#111827}.meta{color:#6b7280}.grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(260px,1fr));gap:14px}.card{background:#fff;border:1px solid #d1d5db;border-radius:10px;padding:16px;margin:14px 0}.warn{border-left:5px solid #b45309;background:#fff7ed}.ok{border-left:5px solid #047857;background:#ecfdf5}.decision{border-left:5px solid #1f4f5f}code{background:#e5e7eb;padding:2px 5px;border-radius:4px;word-break:break-all}li{margin:5px 0}table{border-collapse:collapse;width:100%;background:#fff}th,td{border:1px solid #d1d5db;padding:8px;text-align:left;vertical-align:top}th{background:#e5e7eb}@media print{body{background:#fff;margin:12mm}.card{break-inside:avoid}}</style></head>
<body>
<h1>OPALNOVA Installer Update Readiness</h1>
<p class="meta">Generated locally: {{{Html(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))}}}<br>Installed version: {{{Html(version)}}}</p>
<div class="card ok"><h2>Scope</h2><p>This read-only report records the installer/update direction chosen after the V2.1 decision review. It does not create an installer, create shortcuts, move data, change update behavior, schedule background jobs or alter the database schema.</p></div>
<div class="grid">
<div class="card"><h2>Runtime</h2><p><b>Executable:</b><br>{{{Html(processPath)}}}</p><p>{{{Html(executableSummary)}}}</p><p><b>App folder:</b><br>{{{Html(appFolder)}}}</p><p>{{{Html(appFolderSummary)}}}</p></div>
<div class="card"><h2>Distribution Signal</h2><p><b>Publish folder detected:</b> {{{(isPublishFolder ? "Yes" : "No")}}}</p><p>{{{Html(isPublishFolder ? "This appears to be running from a publish folder. Validate this folder as the portable distribution candidate." : "This does not appear to be the final publish folder. Validate the published OPALNOVA.exe before distributing or installing.")}}}</p></div>
<div class="card"><h2>Business Data</h2><p><b>Database:</b><br>{{{Html(DatabaseBootstrapper.DatabasePath)}}}</p><p>{{{Html(databaseSummary)}}}</p><p><b>Settings:</b><br>{{{Html(BusinessSettingsService.SettingsPath)}}}</p><p>{{{Html(settingsSummary)}}}</p></div>
<div class="card"><h2>Output Folders</h2><p><b>Backups:</b><br>{{{Html(backupFolder)}}}<br>{{{Html(backupSummary)}}}</p><p><b>Printouts:</b><br>{{{Html(printoutFolder)}}}<br>{{{Html(printoutSummary)}}}</p></div>
</div>
<div class="card decision"><h2>Installer Decision Checklist</h2><table><thead><tr><th>Decision</th><th>Current recommendation</th><th>Do before implementation</th></tr></thead><tbody>
<tr><td>Installer technology</td><td>Compare MSIX for Windows-managed install/update behavior against Inno Setup for a traditional installer and simpler folder ownership.</td><td>Package the existing published folder first, then test install, upgrade and uninstall on a separate Windows profile or VM.</td></tr>
<tr><td>Shortcut ownership</td><td>Installer owns desktop and Start Menu shortcuts. Normal OPALNOVA startup should not create or repair shortcuts.</td><td>Confirm shortcut points to the installed <code>OPALNOVA.exe</code> and does not point to a temporary build folder.</td></tr>
<tr><td>Local data location</td><td>Keep the SQLite database, settings, photos, backups and printouts outside the install folder under the current local app/user folders.</td><td>Back up before installing over an existing copy and confirm no installer action deletes <code>{{{Html(DatabaseBootstrapper.DatabasePath)}}}</code>.</td></tr>
<tr><td>Code signing</td><td>Manual/portable distribution can continue without signing for local testing, but a public installer should be signed before wider distribution.</td><td>Choose certificate ownership, signing toolchain and release artifact naming before customer-facing installation.</td></tr>
<tr><td>Uninstall behavior</td><td>Uninstall should remove application binaries and shortcuts only, not business data.</td><td>Document where data remains and how to remove it manually only when intentionally retiring an installation.</td></tr>
</tbody></table></div>
<div class="card decision"><h2>Update Channel Checklist</h2><table><thead><tr><th>Update question</th><th>Current answer</th><th>Safe next step</th></tr></thead><tbody>
<tr><td>Automatic update</td><td>Deferred. There is no trusted update feed, package signing rule or rollback workflow in this pass.</td><td>Keep updates manual until installer technology and signing are chosen.</td></tr>
<tr><td>Version verification</td><td>Use the app header, About, Release Notes, Support Snapshot and this report to verify installed version context.</td><td>Before handoff, confirm FileVersion and ProductVersion on the published <code>OPALNOVA.exe</code>.</td></tr>
<tr><td>Manual update routine</td><td>Close OPALNOVA, create a backup, install or copy the new published build, launch, check About, then run Health Check and Support Snapshot.</td><td>Write this routine into installer release notes before distributing a packaged build.</td></tr>
<tr><td>Rollback</td><td>Rollback remains manual: keep the previous publish folder or installer, and keep database backups before update testing.</td><td>Test whether older builds can still open the current no-schema database before promising rollback.</td></tr>
</tbody></table></div>
<div class="card warn"><h2>Distribution Boundaries</h2><ul><li>Do not move the live database into the install or publish folder.</li><li>Do not package database backups, export bundles, customer documents, supplier credentials, Nivoda credentials or private API keys.</li><li>Do not add auto-update behavior until update signing, feed ownership, rollback and offline behavior are specified.</li><li>Do not test install/uninstall against the only copy of live business data. Use a separate Windows profile, VM or backup-restored test data.</li></ul></div>
<div class="card"><h2>Manual Portable Build Handoff</h2><ol><li>Publish the Release build for <code>win-x64</code> self-contained output.</li><li>Open the publish folder and verify <code>OPALNOVA.exe</code> reports the expected FileVersion and ProductVersion.</li><li>Launch the published app, confirm the header and About version, then close cleanly.</li><li>Run Release Readiness, Installer/Update Readiness and Support Snapshot from the published app.</li><li>Share the publish folder as a whole. Do not copy only the executable unless publish settings are deliberately changed and retested.</li><li>Recommended support snapshot filename for handoff context: <code>{{{Html(supportSnapshotName)}}}</code>.</li></ol></div>
<div class="card"><h2>Business Context</h2><p><b>Business:</b> {{{Html(settings.BusinessName)}}}</p><p>Installer and update work should preserve existing quote, production, payment, inventory, supplier diamond, backup, restore, support snapshot and report behavior. This readiness pass records the decision boundary; it does not change workflow records.</p></div>
</body></html>
""";
        File.WriteAllText(path, html);
        return path;

        static bool IsPublishFolder(string folderPath)
        {
            var trimmed = folderPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return trimmed.EndsWith($"{Path.DirectorySeparatorChar}publish", StringComparison.OrdinalIgnoreCase)
                || trimmed.EndsWith($"{Path.AltDirectorySeparatorChar}publish", StringComparison.OrdinalIgnoreCase);
        }

        static string FileSummary(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return "Path not configured.";
            if (!File.Exists(filePath))
                return "File not found.";
            var info = new FileInfo(filePath);
            return $"Exists - {info.Length:N0} bytes - modified {info.LastWriteTime:g}.";
        }

        static string FolderSummary(string folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath))
                return "Path not configured.";
            if (!Directory.Exists(folderPath))
                return "Folder not found.";
            var info = new DirectoryInfo(folderPath);
            return $"Exists - modified {info.LastWriteTime:g}.";
        }
    }

    public static string CreateInstallerValidationChecklistReport()
    {
        var folder = BusinessSettingsService.GetPrintoutFolder();
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, $"OPALNOVA-Installer-Validation-Checklist-{DateTime.Now:yyyyMMdd-HHmmss}.html");
        var settings = BusinessSettingsService.Load();
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? assembly.GetName().Version?.ToString()
            ?? "unknown";
        var processPath = Environment.ProcessPath ?? "unknown";
        var appFolder = AppContext.BaseDirectory;
        var backupFolder = BusinessSettingsService.GetBackupFolder();
        var printoutFolder = BusinessSettingsService.GetPrintoutFolder();
        var isPublishFolder = IsPublishFolder(appFolder);
        var executableSummary = ExecutableSummary(processPath);
        var executableHash = Sha256Summary(processPath);
        var appFolderSummary = FolderInventorySummary(appFolder);
        var databaseSummary = FileSummary(DatabaseBootstrapper.DatabasePath);
        var settingsSummary = FileSummary(BusinessSettingsService.SettingsPath);
        var backupSummary = FolderSummary(backupFolder);
        var printoutSummary = FolderSummary(printoutFolder);

        var html = $$$"""
<!doctype html>
<html><head><meta charset="utf-8"><title>OPALNOVA Installer Validation Checklist</title>
<style>body{font-family:Segoe UI,Arial,sans-serif;margin:32px;line-height:1.5;color:#1f2937;background:#f8fafc}h1,h2,h3{color:#111827}.meta{color:#6b7280}.grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(260px,1fr));gap:14px}.card{background:#fff;border:1px solid #d1d5db;border-radius:10px;padding:16px;margin:14px 0}.warn{border-left:5px solid #b45309;background:#fff7ed}.ok{border-left:5px solid #047857;background:#ecfdf5}.decision{border-left:5px solid #1f4f5f}.hold{border-left:5px solid #991b1b;background:#fef2f2}code{background:#e5e7eb;padding:2px 5px;border-radius:4px;word-break:break-all}li{margin:5px 0}table{border-collapse:collapse;width:100%;background:#fff}th,td{border:1px solid #d1d5db;padding:8px;text-align:left;vertical-align:top}th{background:#e5e7eb}@media print{body{background:#fff;margin:12mm}.card{break-inside:avoid}}</style></head>
<body>
<h1>OPALNOVA Installer Validation Checklist</h1>
<p class="meta">Generated locally: {{{Html(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))}}}<br>Installed version: {{{Html(version)}}}</p>
<div class="card ok"><h2>Selected First Route</h2><p>Validate the portable publish folder first. This tests the current self-contained OPALNOVA build, version metadata, local data boundaries and manual update routine before MSIX, Inno Setup or any other installer technology owns shortcuts, install paths or uninstall behavior.</p></div>
<div class="grid">
<div class="card"><h2>Executable Fingerprint</h2><p><b>Executable:</b><br>{{{Html(processPath)}}}</p><p>{{{Html(executableSummary)}}}</p><p><b>SHA-256:</b><br><code>{{{Html(executableHash)}}}</code></p></div>
<div class="card"><h2>Publish Folder</h2><p><b>App folder:</b><br>{{{Html(appFolder)}}}</p><p><b>Publish folder detected:</b> {{{(isPublishFolder ? "Yes" : "No")}}}</p><p>{{{Html(appFolderSummary)}}}</p></div>
<div class="card"><h2>Local Data Boundary</h2><p><b>Database:</b><br>{{{Html(DatabaseBootstrapper.DatabasePath)}}}</p><p>{{{Html(databaseSummary)}}}</p><p><b>Settings:</b><br>{{{Html(BusinessSettingsService.SettingsPath)}}}</p><p>{{{Html(settingsSummary)}}}</p></div>
<div class="card"><h2>Support Paths</h2><p><b>Backups:</b><br>{{{Html(backupFolder)}}}<br>{{{Html(backupSummary)}}}</p><p><b>Printouts:</b><br>{{{Html(printoutFolder)}}}<br>{{{Html(printoutSummary)}}}</p></div>
</div>
<div class="card decision"><h2>Portable Publish Validation</h2><ol><li>Build and publish the Release <code>win-x64</code> self-contained output.</li><li>Open the published <code>OPALNOVA.exe</code> properties and confirm FileVersion <code>2.4.0.0</code> and ProductVersion <code>2.4.0 OPALNOVA Portable Build Manifest</code>.</li><li>Launch the published executable, confirm the header and About version, then close cleanly.</li><li>Run Release Readiness, Installer/Update Readiness, Installer Validation Checklist, Portable Build Manifest and Support Snapshot from the published app.</li><li>Confirm Search All finds installer/update readiness, installer validation and portable build manifest workflow actions.</li><li>Copy the whole publish folder to a separate test folder or Windows profile. Do not copy only <code>OPALNOVA.exe</code>.</li><li>Launch from the copied folder and confirm the database path still uses the local app data location, not the publish folder.</li><li>Create a fresh backup before any upgrade rehearsal and confirm the backup appears in the configured backup folder.</li></ol></div>
<div class="card decision"><h2>Manual Update Rehearsal</h2><table><thead><tr><th>Gate</th><th>Expected result</th><th>Stop if</th></tr></thead><tbody>
<tr><td>Pre-update backup</td><td>A current database backup exists outside the publish folder.</td><td>No backup can be created or located.</td></tr>
<tr><td>Close running app</td><td>No old OPALNOVA process is holding files or the database open.</td><td>The app cannot close cleanly.</td></tr>
<tr><td>Replace application files</td><td>The old test publish folder can be replaced by the new full publish folder.</td><td>Only the executable is copied, files remain locked, or runtime files are missing.</td></tr>
<tr><td>Version check</td><td>Header, About, Release Notes, Portable Build Manifest and Support Snapshot all show the expected V2.4 context.</td><td>Any visible version still reports an older current baseline.</td></tr>
<tr><td>Data check</td><td>Existing customers, quotes, jobs, payments, inventory, supplier diamonds and reports still open.</td><td>Records disappear, a new empty database appears, or data is moved into the install folder.</td></tr>
<tr><td>Rollback check</td><td>Previous no-schema build can be restored for test only, using the same local data and a known backup.</td><td>The rollback story depends on deleting live data or guessing which build is installed.</td></tr>
</tbody></table></div>
<div class="card"><h2>Installer Technology Gate</h2><ul><li><b>MSIX:</b> only start after portable validation passes and package identity, signing, install path, app data boundary, update channel and uninstall behavior are written down.</li><li><b>Inno Setup:</b> only start after portable validation passes and shortcut ownership, install folder, previous-version replacement, uninstall behavior and signing are written down.</li><li><b>Portable handoff:</b> acceptable for controlled local testing if the whole publish folder is shared, the executable fingerprint is recorded, and the user understands there is no installer-owned shortcut or update channel.</li></ul></div>
<div class="card hold"><h2>Hold Conditions</h2><ul><li>Do not create installer files until the published executable launches, reports V2.4 correctly and preserves the existing local data path.</li><li>Do not add auto-update behavior until signing, update feed ownership, offline behavior and rollback are explicit.</li><li>Do not create shortcuts from normal OPALNOVA startup; shortcut creation belongs to installer packaging.</li><li>Do not package customer records, database backups, export bundles, supplier credentials, Nivoda credentials or bridge API keys.</li><li>Do not proceed if Health Check, Data Integrity, Portable Build Manifest, Support Snapshot or the installer validation checklist exposes a path/version mismatch.</li></ul></div>
<div class="card"><h2>Business Context</h2><p><b>Business:</b> {{{Html(settings.BusinessName)}}}</p><p>This checklist is read-only. It does not create an installer, create shortcuts, install updates, move the database, schedule background work, create tasks, change records or alter the database schema.</p></div>
</body></html>
""";
        File.WriteAllText(path, html);
        return path;

        static bool IsPublishFolder(string folderPath)
        {
            var trimmed = folderPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return trimmed.EndsWith($"{Path.DirectorySeparatorChar}publish", StringComparison.OrdinalIgnoreCase)
                || trimmed.EndsWith($"{Path.AltDirectorySeparatorChar}publish", StringComparison.OrdinalIgnoreCase);
        }

        static string ExecutableSummary(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return "Path not configured.";
            if (!File.Exists(filePath))
                return "File not found.";

            var info = new FileInfo(filePath);
            var versionInfo = FileVersionInfo.GetVersionInfo(filePath);
            var fileVersion = string.IsNullOrWhiteSpace(versionInfo.FileVersion) ? "unknown" : versionInfo.FileVersion;
            var productVersion = string.IsNullOrWhiteSpace(versionInfo.ProductVersion) ? "unknown" : versionInfo.ProductVersion;
            return $"Exists - {FormatBytes(info.Length)} - modified {info.LastWriteTime:g} - FileVersion {fileVersion} - ProductVersion {productVersion}.";
        }

        static string Sha256Summary(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return "Path not configured.";
            if (!File.Exists(filePath))
                return "File not found.";

            try
            {
                using var sha = SHA256.Create();
                using var stream = File.OpenRead(filePath);
                return Convert.ToHexString(sha.ComputeHash(stream));
            }
            catch (Exception ex)
            {
                return $"Could not calculate SHA-256: {ex.Message}";
            }
        }

        static string FileSummary(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return "Path not configured.";
            if (!File.Exists(filePath))
                return "File not found.";
            var info = new FileInfo(filePath);
            return $"Exists - {FormatBytes(info.Length)} - modified {info.LastWriteTime:g}.";
        }

        static string FolderSummary(string folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath))
                return "Path not configured.";
            if (!Directory.Exists(folderPath))
                return "Folder not found.";
            var info = new DirectoryInfo(folderPath);
            return $"Exists - modified {info.LastWriteTime:g}.";
        }

        static string FolderInventorySummary(string folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath))
                return "Path not configured.";
            if (!Directory.Exists(folderPath))
                return "Folder not found.";

            try
            {
                var files = Directory.EnumerateFiles(folderPath, "*", SearchOption.TopDirectoryOnly)
                    .Select(file => new FileInfo(file))
                    .ToList();
                var totalBytes = files.Sum(file => file.Length);
                var info = new DirectoryInfo(folderPath);
                return $"Exists - {files.Count:N0} top-level files - {FormatBytes(totalBytes)} top-level size - modified {info.LastWriteTime:g}.";
            }
            catch (Exception ex)
            {
                return $"Folder exists, but inventory could not be read: {ex.Message}";
            }
        }

        static string FormatBytes(long bytes)
        {
            string[] units = ["bytes", "KB", "MB", "GB"];
            var value = (double)bytes;
            var unitIndex = 0;
            while (value >= 1024 && unitIndex < units.Length - 1)
            {
                value /= 1024;
                unitIndex++;
            }

            return unitIndex == 0
                ? $"{bytes:N0} {units[unitIndex]}"
                : $"{value:N1} {units[unitIndex]}";
        }
    }

    public static string CreatePortableBuildManifestReport()
    {
        var folder = BusinessSettingsService.GetPrintoutFolder();
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, $"OPALNOVA-Portable-Build-Manifest-{DateTime.Now:yyyyMMdd-HHmmss}.html");
        var settings = BusinessSettingsService.Load();
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? assembly.GetName().Version?.ToString()
            ?? "unknown";
        var processPath = Environment.ProcessPath ?? "unknown";
        var appFolder = AppContext.BaseDirectory;
        var backupFolder = BusinessSettingsService.GetBackupFolder();
        var printoutFolder = BusinessSettingsService.GetPrintoutFolder();
        var isPublishFolder = IsPublishFolder(appFolder);
        var executableSummary = ExecutableSummary(processPath);
        var executableHash = Sha256Summary(processPath);
        var inventory = BuildFolderInventory(appFolder);
        var excludedPrivateData = BuildPrivateDataCheck(appFolder);
        var topLevelRows = BuildTopLevelFileRows(inventory.Files);

        var html = $$$"""
<!doctype html>
<html><head><meta charset="utf-8"><title>OPALNOVA Portable Build Manifest</title>
<style>body{font-family:Segoe UI,Arial,sans-serif;margin:32px;line-height:1.5;color:#1f2937;background:#f8fafc}h1,h2,h3{color:#111827}.meta{color:#6b7280}.grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(260px,1fr));gap:14px}.card{background:#fff;border:1px solid #d1d5db;border-radius:10px;padding:16px;margin:14px 0}.warn{border-left:5px solid #b45309;background:#fff7ed}.ok{border-left:5px solid #047857;background:#ecfdf5}.decision{border-left:5px solid #1f4f5f}.hold{border-left:5px solid #991b1b;background:#fef2f2}code{background:#e5e7eb;padding:2px 5px;border-radius:4px;word-break:break-all}li{margin:5px 0}table{border-collapse:collapse;width:100%;background:#fff}th,td{border:1px solid #d1d5db;padding:8px;text-align:left;vertical-align:top}th{background:#e5e7eb}.small{font-size:12px;color:#6b7280}@media print{body{background:#fff;margin:12mm}.card{break-inside:avoid}}</style></head>
<body>
<h1>OPALNOVA Portable Build Manifest</h1>
<p class="meta">Generated locally: {{{Html(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))}}}<br>Installed version: {{{Html(version)}}}</p>
<div class="card ok"><h2>Purpose</h2><p>This read-only manifest records what is in the current OPALNOVA app folder before portable handoff or installer packaging. It does not copy files, create an installer, create shortcuts, install updates, move data, create tasks or alter the database schema.</p></div>
<div class="grid">
<div class="card"><h2>Executable</h2><p><b>Path:</b><br>{{{Html(processPath)}}}</p><p>{{{Html(executableSummary)}}}</p><p><b>SHA-256:</b><br><code>{{{Html(executableHash)}}}</code></p></div>
<div class="card"><h2>App Folder</h2><p><b>Folder:</b><br>{{{Html(appFolder)}}}</p><p><b>Publish folder detected:</b> {{{(isPublishFolder ? "Yes" : "No")}}}</p><p><b>Total files:</b> {{{inventory.FileCount:N0}}}<br><b>Total folders:</b> {{{inventory.DirectoryCount:N0}}}<br><b>Total size:</b> {{{Html(FormatBytes(inventory.TotalBytes))}}}</p></div>
<div class="card"><h2>Business Data Boundary</h2><p><b>Database:</b><br>{{{Html(DatabaseBootstrapper.DatabasePath)}}}</p><p><b>Settings:</b><br>{{{Html(BusinessSettingsService.SettingsPath)}}}</p><p><b>Backups:</b><br>{{{Html(backupFolder)}}}</p><p><b>Printouts:</b><br>{{{Html(printoutFolder)}}}</p></div>
<div class="card"><h2>Private Data Exclusion</h2><p>{{{Html(excludedPrivateData.Summary)}}}</p><p class="small">Checked for private data file types inside the current app folder: .db, .sqlite, .zip, .json backup/settings candidates, .csv exports and common secret env files.</p></div>
</div>
<div class="card decision"><h2>Top-Level File Inventory</h2><table><thead><tr><th>File</th><th>Size</th><th>Modified</th></tr></thead><tbody>
{{{topLevelRows}}}
</tbody></table></div>
<div class="card"><h2>Handoff Decision Notes</h2><ul><li><b>Portable handoff:</b> share the entire publish folder only after Debug build, Release publish, launch smoke, Installer Validation Checklist, Portable Build Manifest and Support Snapshot all match the expected version.</li><li><b>MSIX/Inno Setup:</b> do not start packaging until this manifest proves the publish folder is clean and the installer decision record names shortcut ownership, install path, signing, update channel and uninstall behavior.</li><li><b>Support context:</b> pair this manifest with Support Snapshot so both app binaries and local business-data paths are clear without sharing private database or backup files.</li></ul></div>
<div class="card hold"><h2>Do Not Include In Distribution</h2><ul><li>SQLite databases, database backups, export bundles, customer documents, printouts, photos, error logs, supplier credentials, Nivoda credentials, bridge API keys or environment files.</li><li>Any copied test data under the publish folder. Test data belongs in local app/user folders or a separate test profile.</li><li>Only <code>OPALNOVA.exe</code> by itself. The portable route requires the full publish folder unless publish settings are deliberately changed and revalidated.</li></ul></div>
<div class="card warn"><h2>Review Items</h2><ul><li>{{{Html(isPublishFolder ? "This appears to be a publish folder. Continue with portable validation if the version, hash and support paths match." : "This does not appear to be the final publish folder. Generate the manifest from the published app before handoff.")}}}</li><li>{{{Html(excludedPrivateData.HasFindings ? "Private-data-like files were detected in the app folder. Review them before any handoff." : "No private-data-like files were detected in the app folder by extension/name check.")}}}</li><li>Confirm the executable hash above after copying to another folder or machine if portable handoff is used.</li></ul></div>
<div class="card"><h2>Business Context</h2><p><b>Business:</b> {{{Html(settings.BusinessName)}}}</p><p>This manifest is a local report only. It is not an installer, not an updater and not a backup.</p></div>
</body></html>
""";
        File.WriteAllText(path, html);
        return path;

        static bool IsPublishFolder(string folderPath)
        {
            var trimmed = folderPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return trimmed.EndsWith($"{Path.DirectorySeparatorChar}publish", StringComparison.OrdinalIgnoreCase)
                || trimmed.EndsWith($"{Path.AltDirectorySeparatorChar}publish", StringComparison.OrdinalIgnoreCase);
        }

        static string ExecutableSummary(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return "Path not configured.";
            if (!File.Exists(filePath))
                return "File not found.";

            var info = new FileInfo(filePath);
            var versionInfo = FileVersionInfo.GetVersionInfo(filePath);
            var fileVersion = string.IsNullOrWhiteSpace(versionInfo.FileVersion) ? "unknown" : versionInfo.FileVersion;
            var productVersion = string.IsNullOrWhiteSpace(versionInfo.ProductVersion) ? "unknown" : versionInfo.ProductVersion;
            return $"Exists - {FormatBytes(info.Length)} - modified {info.LastWriteTime:g} - FileVersion {fileVersion} - ProductVersion {productVersion}.";
        }

        static string Sha256Summary(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return "Path not configured.";
            if (!File.Exists(filePath))
                return "File not found.";

            try
            {
                using var sha = SHA256.Create();
                using var stream = File.OpenRead(filePath);
                return Convert.ToHexString(sha.ComputeHash(stream));
            }
            catch (Exception ex)
            {
                return $"Could not calculate SHA-256: {ex.Message}";
            }
        }

        static FolderInventory BuildFolderInventory(string folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
                return new FolderInventory([], 0, 0, 0);

            var files = Directory.EnumerateFiles(folderPath, "*", SearchOption.AllDirectories)
                .Select(file => new FileInfo(file))
                .OrderBy(file => file.FullName, StringComparer.OrdinalIgnoreCase)
                .ToList();
            var directoryCount = Directory.EnumerateDirectories(folderPath, "*", SearchOption.AllDirectories).Count();
            var totalBytes = files.Sum(file => file.Length);
            return new FolderInventory(files, files.Count, directoryCount, totalBytes);
        }

        static PrivateDataCheck BuildPrivateDataCheck(string folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
                return new PrivateDataCheck(false, "App folder was not available for private-data exclusion checks.");

            var suspiciousNames = new[] { ".env", "secrets", "credential", "password", "backup", "export" };
            var suspiciousExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ".db", ".sqlite", ".sqlite3", ".zip", ".csv", ".env"
            };

            var findings = Directory.EnumerateFiles(folderPath, "*", SearchOption.AllDirectories)
                .Select(file => new FileInfo(file))
                .Where(file =>
                    suspiciousExtensions.Contains(file.Extension)
                    || suspiciousNames.Any(name => file.Name.Contains(name, StringComparison.OrdinalIgnoreCase)))
                .Take(12)
                .Select(file => Path.GetRelativePath(folderPath, file.FullName))
                .ToList();

            if (findings.Count == 0)
                return new PrivateDataCheck(false, "No private-data-like files were detected in the app folder by extension/name check.");

            return new PrivateDataCheck(true, $"Review before handoff: {string.Join(", ", findings)}.");
        }

        static string BuildTopLevelFileRows(IReadOnlyList<FileInfo> files)
        {
            var topLevelFiles = files
                .Where(file => file.DirectoryName is not null
                    && string.Equals(file.DirectoryName.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), StringComparison.OrdinalIgnoreCase))
                .OrderBy(file => file.Name, StringComparer.OrdinalIgnoreCase)
                .Take(80)
                .ToList();

            if (topLevelFiles.Count == 0)
                return "<tr><td colspan=\"3\">No top-level files found.</td></tr>";

            var rows = new StringBuilder();
            foreach (var file in topLevelFiles)
            {
                rows.Append("<tr><td>")
                    .Append(Html(file.Name))
                    .Append("</td><td>")
                    .Append(Html(FormatBytes(file.Length)))
                    .Append("</td><td>")
                    .Append(Html(file.LastWriteTime.ToString("g")))
                    .Append("</td></tr>");
            }

            return rows.ToString();
        }

        static string FormatBytes(long bytes)
        {
            string[] units = ["bytes", "KB", "MB", "GB"];
            var value = (double)bytes;
            var unitIndex = 0;
            while (value >= 1024 && unitIndex < units.Length - 1)
            {
                value /= 1024;
                unitIndex++;
            }

            return unitIndex == 0
                ? $"{bytes:N0} {units[unitIndex]}"
                : $"{value:N1} {units[unitIndex]}";
        }
    }

    private sealed record FolderInventory(IReadOnlyList<FileInfo> Files, int FileCount, int DirectoryCount, long TotalBytes);

    private sealed record PrivateDataCheck(bool HasFindings, string Summary);

    public static string CreatePackagingDecisionRecordReport()
    {
        var folder = BusinessSettingsService.GetPrintoutFolder();
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, $"OPALNOVA-Packaging-Decision-Record-{DateTime.Now:yyyyMMdd-HHmmss}.html");
        var settings = BusinessSettingsService.Load();
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? assembly.GetName().Version?.ToString()
            ?? "unknown";
        var processPath = Environment.ProcessPath ?? "unknown";
        var appFolder = AppContext.BaseDirectory;
        var isPublishFolder = IsPublishFolder(appFolder);
        var executableSummary = ExecutableSummary(processPath);
        var executableHash = Sha256Summary(processPath);

        var html = $$$"""
<!doctype html>
<html><head><meta charset="utf-8"><title>OPALNOVA Packaging Decision Record</title>
<style>body{font-family:Segoe UI,Arial,sans-serif;margin:32px;line-height:1.5;color:#1f2937;background:#f8fafc}h1,h2,h3{color:#111827}.meta{color:#6b7280}.grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(260px,1fr));gap:14px}.card{background:#fff;border:1px solid #d1d5db;border-radius:10px;padding:16px;margin:14px 0}.warn{border-left:5px solid #b45309;background:#fff7ed}.ok{border-left:5px solid #047857;background:#ecfdf5}.decision{border-left:5px solid #1f4f5f}.hold{border-left:5px solid #991b1b;background:#fef2f2}code{background:#e5e7eb;padding:2px 5px;border-radius:4px;word-break:break-all}li{margin:5px 0}table{border-collapse:collapse;width:100%;background:#fff}th,td{border:1px solid #d1d5db;padding:8px;text-align:left;vertical-align:top}th{background:#e5e7eb}@media print{body{background:#fff;margin:12mm}.card{break-inside:avoid}}</style></head>
<body>
<h1>OPALNOVA Packaging Decision Record</h1>
<p class="meta">Generated locally: {{{Html(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))}}}<br>Installed version: {{{Html(version)}}}</p>
<div class="card ok"><h2>Current Decision</h2><p><b>Decision:</b> portable publish-folder handoff is the validated route for the current OPALNOVA build. MSIX and Inno Setup remain future packaging options and should not be started without an explicit packaging ticket covering signing, install path, shortcuts, update channel, rollback and uninstall behavior.</p></div>
<div class="grid">
<div class="card"><h2>Executable Evidence</h2><p><b>Executable:</b><br>{{{Html(processPath)}}}</p><p>{{{Html(executableSummary)}}}</p><p><b>SHA-256:</b><br><code>{{{Html(executableHash)}}}</code></p></div>
<div class="card"><h2>App Folder</h2><p><b>Folder:</b><br>{{{Html(appFolder)}}}</p><p><b>Publish folder detected:</b> {{{(isPublishFolder ? "Yes" : "No")}}}</p></div>
<div class="card"><h2>Local Data Boundary</h2><p><b>Database:</b><br>{{{Html(DatabaseBootstrapper.DatabasePath)}}}</p><p><b>Settings:</b><br>{{{Html(BusinessSettingsService.SettingsPath)}}}</p><p><b>Backups:</b><br>{{{Html(BusinessSettingsService.GetBackupFolder())}}}</p><p><b>Printouts:</b><br>{{{Html(BusinessSettingsService.GetPrintoutFolder())}}}</p></div>
<div class="card"><h2>Business Context</h2><p><b>Business:</b> {{{Html(settings.BusinessName)}}}</p><p>Keep OPALNOVA local-data storage in existing app/user folders. Do not move the SQLite database into the install or publish folder.</p></div>
</div>
<div class="card decision"><h2>Evidence Chain</h2><table><thead><tr><th>Surface</th><th>Purpose</th><th>Decision status</th></tr></thead><tbody>
<tr><td>Release Readiness</td><td>General release gates, packaging notes, staging cautions and generated document checks.</td><td>Use before every build handoff.</td></tr>
<tr><td>Installer/Update Readiness</td><td>Installer choices, update-channel boundaries, portable build handoff and distribution cautions.</td><td>Completed as the installer/update direction foundation.</td></tr>
<tr><td>Installer Validation Checklist</td><td>Portable-first validation, version checks, update rehearsal, rollback and hold conditions.</td><td>Portable route selected as the first tested path.</td></tr>
<tr><td>Portable Build Manifest</td><td>Executable hash, publish-folder inventory, private-data exclusions and handoff notes.</td><td>Use as the binary handoff record for portable distribution.</td></tr>
<tr><td>Support Snapshot</td><td>Version, executable, database, backup, printout, photo, settings and log paths for support.</td><td>Pair with manifest when troubleshooting or handing off a build.</td></tr>
</tbody></table></div>
<div class="card"><h2>Allowed Next Actions</h2><ul><li><b>Controlled portable handoff:</b> allowed after build, publish, launch smoke, manifest and support snapshot all match the intended version.</li><li><b>MSIX packaging:</b> requires a new ticket with package identity, certificate/signing, install/update channel, app data boundary, uninstall behavior and rollback notes.</li><li><b>Inno Setup packaging:</b> requires a new ticket with install folder, shortcut ownership, previous-version replacement, signing, uninstall behavior and rollback notes.</li><li><b>Auto-update:</b> still deferred until installer technology, signing, feed ownership, offline behavior and rollback are explicit.</li></ul></div>
<div class="card hold"><h2>Non-Negotiable Boundaries</h2><ul><li>No installer creation, shortcut creation or auto-update logic from normal OPALNOVA startup.</li><li>No database, backup, export bundle, customer document, supplier credential, Nivoda credential or bridge API key in distribution artifacts.</li><li>No data-location changes without an explicit migration, backup and rollback plan.</li><li>No packaging work proceeds if version metadata, support paths or manifest inventory do not match the intended release.</li></ul></div>
<div class="card warn"><h2>Track Closure</h2><p>The installer/update readiness track is complete for a portable handoff. Further work should be a named packaging implementation decision, not another readiness report.</p></div>
</body></html>
""";
        File.WriteAllText(path, html);
        return path;

        static bool IsPublishFolder(string folderPath)
        {
            var trimmed = folderPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return trimmed.EndsWith($"{Path.DirectorySeparatorChar}publish", StringComparison.OrdinalIgnoreCase)
                || trimmed.EndsWith($"{Path.AltDirectorySeparatorChar}publish", StringComparison.OrdinalIgnoreCase);
        }

        static string ExecutableSummary(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return "Path not configured.";
            if (!File.Exists(filePath))
                return "File not found.";

            var info = new FileInfo(filePath);
            var versionInfo = FileVersionInfo.GetVersionInfo(filePath);
            var fileVersion = string.IsNullOrWhiteSpace(versionInfo.FileVersion) ? "unknown" : versionInfo.FileVersion;
            var productVersion = string.IsNullOrWhiteSpace(versionInfo.ProductVersion) ? "unknown" : versionInfo.ProductVersion;
            return $"Exists - {FormatBytes(info.Length)} - modified {info.LastWriteTime:g} - FileVersion {fileVersion} - ProductVersion {productVersion}.";
        }

        static string Sha256Summary(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return "Path not configured.";
            if (!File.Exists(filePath))
                return "File not found.";

            try
            {
                using var sha = SHA256.Create();
                using var stream = File.OpenRead(filePath);
                return Convert.ToHexString(sha.ComputeHash(stream));
            }
            catch (Exception ex)
            {
                return $"Could not calculate SHA-256: {ex.Message}";
            }
        }

        static string FormatBytes(long bytes)
        {
            string[] units = ["bytes", "KB", "MB", "GB"];
            var value = (double)bytes;
            var unitIndex = 0;
            while (value >= 1024 && unitIndex < units.Length - 1)
            {
                value /= 1024;
                unitIndex++;
            }

            return unitIndex == 0
                ? $"{bytes:N0} {units[unitIndex]}"
                : $"{value:N1} {units[unitIndex]}";
        }
    }

    public static string CreateRoadmapCompletionRecordReport()
    {
        var folder = BusinessSettingsService.GetPrintoutFolder();
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, $"OPALNOVA-Roadmap-Completion-Record-{DateTime.Now:yyyyMMdd-HHmmss}.html");
        var settings = BusinessSettingsService.Load();
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? assembly.GetName().Version?.ToString()
            ?? "unknown";

        var html = $$$"""
<!doctype html>
<html><head><meta charset="utf-8"><title>OPALNOVA Roadmap Completion Record</title>
<style>body{font-family:Segoe UI,Arial,sans-serif;margin:32px;line-height:1.5;color:#1f2937;background:#f8fafc}h1,h2,h3{color:#111827}.meta{color:#6b7280}.grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(260px,1fr));gap:14px}.card{background:#fff;border:1px solid #d1d5db;border-radius:10px;padding:16px;margin:14px 0}.warn{border-left:5px solid #b45309;background:#fff7ed}.ok{border-left:5px solid #047857;background:#ecfdf5}.decision{border-left:5px solid #1f4f5f}.hold{border-left:5px solid #991b1b;background:#fef2f2}code{background:#e5e7eb;padding:2px 5px;border-radius:4px;word-break:break-all}li{margin:5px 0}table{border-collapse:collapse;width:100%;background:#fff}th,td{border:1px solid #d1d5db;padding:8px;text-align:left;vertical-align:top}th{background:#e5e7eb}@media print{body{background:#fff;margin:12mm}.card{break-inside:avoid}}</style></head>
<body>
<h1>OPALNOVA Roadmap Completion Record</h1>
<p class="meta">Generated locally: {{{Html(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))}}}<br>Installed version: {{{Html(version)}}}</p>
<div class="card ok"><h2>Completion Decision</h2><p>The current no-schema version stream is complete. OPALNOVA has validated the portable handoff path, release/support surfaces and workflow polish baseline. Further work should start only after choosing a named major product direction with acceptance criteria.</p></div>
<div class="grid">
<div class="card"><h2>Business Context</h2><p><b>Business:</b> {{{Html(settings.BusinessName)}}}</p><p><b>Database:</b><br>{{{Html(DatabaseBootstrapper.DatabasePath)}}}</p></div>
<div class="card"><h2>Current Baseline</h2><p><b>Version:</b> {{{Html(version)}}}</p><p><b>App folder:</b><br>{{{Html(AppContext.BaseDirectory)}}}</p></div>
<div class="card"><h2>Data Boundary</h2><p>SQLite data, settings, photos, backups and printouts remain in local app/user folders. No install-folder data move is part of this stream.</p></div>
</div>
<div class="card decision"><h2>Completed Tracks</h2><table><thead><tr><th>Track</th><th>Completion state</th></tr></thead><tbody>
<tr><td>Quote, proposal, production and payment workflow polish</td><td>Implemented across the earlier V1.x stream with no current blocking readiness item.</td></tr>
<tr><td>Inventory, supplier diamond and Nivoda staging readiness</td><td>Implemented as local workflows, reports and non-secret staging/diagnostic surfaces; live supplier mutations remain gated.</td></tr>
<tr><td>Reports, support and data-safety surfaces</td><td>Implemented through release readiness, support snapshot, data integrity, operations, inventory and business reporting passes.</td></tr>
<tr><td>Installer/update readiness</td><td>Completed for portable handoff through readiness, validation checklist, portable manifest and packaging decision record.</td></tr>
<tr><td>Version/document/checklist alignment</td><td>Current app metadata, built-in notes, roadmap, future plan, handoff and checklist are aligned to V2.6.</td></tr>
</tbody></table></div>
<div class="card"><h2>Remaining Major Decisions</h2><ul><li><b>MSIX packaging:</b> requires package identity, certificate/signing, install/update channel, uninstall behavior and rollback rules.</li><li><b>Inno Setup packaging:</b> requires install folder, shortcut ownership, previous-version replacement, signing, uninstall behavior and rollback rules.</li><li><b>True backup scheduling:</b> requires explicit app lifecycle, installer or Windows Task Scheduler ownership.</li><li><b>Advanced hardware:</b> requires confirmed printer/camera/scale devices and driver behavior.</li><li><b>Scheduled reports:</b> requires background scheduling and report delivery rules.</li><li><b>Deeper calendar/capacity planning:</b> requires richer scheduling acceptance criteria.</li><li><b>Command palette expansion:</b> requires a command model decision beyond current Search All workflow actions.</li><li><b>API-level Nivoda hold/order:</b> requires confirmed supplier credentials, accessible mutation schema and payload/error behavior.</li></ul></div>
<div class="card hold"><h2>Stop Condition</h2><ul><li>Do not continue adding readiness-only versions after this point without a new concrete product decision.</li><li>Do not add installer generation, auto-update, shortcut creation, background scheduling, cloud sync, supplier mutations or hardware dependencies as hidden side effects.</li><li>Before the next development stream, choose one major direction and write its acceptance checklist first.</li></ul></div>
<div class="card warn"><h2>Recommended Next Ticket Shape</h2><p>Pick one: MSIX packaging, Inno Setup packaging, backup scheduling design, advanced hardware setup, scheduled reports, calendar/capacity planning, command-palette expansion, or Nivoda hold/order implementation after schema confirmation. Treat that as a new stream, not a continuation of the current readiness loop.</p></div>
</body></html>
""";
        File.WriteAllText(path, html);
        return path;
    }

    public static string CreatePostV2DecisionReviewReport()
    {
        var folder = BusinessSettingsService.GetPrintoutFolder();
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, $"OPALNOVA-Post-V2-Decision-Review-{DateTime.Now:yyyyMMdd-HHmmss}.html");
        var settings = BusinessSettingsService.Load();
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? assembly.GetName().Version?.ToString()
            ?? "unknown";

        using var db = new AppDbContext();
        var customerCount = db.Customers.Count();
        var quoteCount = db.CustomQuotes.Count();
        var generatedProposalCount = db.CustomQuotes.Count(q => q.ProposalLastGeneratedAt.HasValue);
        var sentProposalCount = db.CustomQuotes.Count(q => q.ProposalSentAt.HasValue);
        var activeJobCount = db.Jobs.Count(j => j.Status != JobStatus.Completed && j.Status != JobStatus.Cancelled);
        var openTaskCount = db.BusinessTasks.Count(t => t.Status != BusinessTaskStatus.Completed && t.Status != BusinessTaskStatus.Cancelled);
        var saleCount = db.Sales.Count();
        var stockedItemCount = db.JewelleryItems.Count();
        var stoneCount = db.Stones.Count();
        var materialCount = db.Materials.Count();
        var externalDiamondCount = db.ExternalDiamonds.Count();
        var externalDiamondInProgressCount = db.ExternalDiamonds.Count(d =>
            d.Status == "Hold Requested"
            || d.Status == "Hold Confirmed"
            || d.Status == "Hold Expiring"
            || d.Status == "Order Requested"
            || d.Status == "Ordered");
        var hasNivodaCredentials = !string.IsNullOrWhiteSpace(settings.NivodaUsername)
            && !string.IsNullOrWhiteSpace(settings.NivodaPassword);
        var lastNivodaTest = settings.NivodaLastConnectionTestAt?.ToString("g") ?? "not tested";

        var html = $$$"""
<!doctype html>
<html><head><meta charset="utf-8"><title>OPALNOVA Post-V2 Decision Review</title>
<style>body{font-family:Segoe UI,Arial,sans-serif;margin:32px;line-height:1.5;color:#1f2937;background:#f8fafc}h1,h2,h3{color:#111827}.meta{color:#6b7280}.grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(250px,1fr));gap:14px}.card{background:#fff;border:1px solid #d1d5db;border-radius:10px;padding:16px;margin:14px 0}.warn{border-left:5px solid #b45309;background:#fff7ed}.ok{border-left:5px solid #047857;background:#ecfdf5}.decision{border-left:5px solid #1f4f5f}code{background:#e5e7eb;padding:2px 5px;border-radius:4px;word-break:break-all}li{margin:5px 0}table{border-collapse:collapse;width:100%;background:#fff}th,td{border:1px solid #d1d5db;padding:8px;text-align:left;vertical-align:top}th{background:#e5e7eb}@media print{body{background:#fff;margin:12mm}.card{break-inside:avoid}}</style></head>
<body>
<h1>OPALNOVA Post-V2 Decision Review</h1>
<p class="meta">Generated locally: {{{Html(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))}}}<br>Installed version: {{{Html(version)}}}</p>
<div class="card ok"><h2>Purpose</h2><p>This read-only review keeps post-V2 planning deliberate. It does not create tasks, send email, call supplier order mutations, change stock, schedule background jobs or alter the database schema.</p></div>
<div class="grid">
<div class="card"><h2>Workflow Footprint</h2><p><b>Customers:</b> {{{customerCount:N0}}}<br><b>Quotes:</b> {{{quoteCount:N0}}}<br><b>Generated proposals:</b> {{{generatedProposalCount:N0}}}<br><b>Sent proposals:</b> {{{sentProposalCount:N0}}}</p></div>
<div class="card"><h2>Operations Load</h2><p><b>Active jobs:</b> {{{activeJobCount:N0}}}<br><b>Open tasks:</b> {{{openTaskCount:N0}}}<br><b>Sales:</b> {{{saleCount:N0}}}</p></div>
<div class="card"><h2>Stock And Supplier Context</h2><p><b>Jewellery stock:</b> {{{stockedItemCount:N0}}}<br><b>Loose stones:</b> {{{stoneCount:N0}}}<br><b>Materials:</b> {{{materialCount:N0}}}<br><b>External diamonds:</b> {{{externalDiamondCount:N0}}}<br><b>Supplier diamonds in hold/order flow:</b> {{{externalDiamondInProgressCount:N0}}}</p></div>
<div class="card"><h2>Nivoda Readiness</h2><p><b>Environment:</b> {{{Html(settings.NivodaEnvironmentName)}}}<br><b>Endpoint:</b><br><code>{{{Html(settings.NivodaEndpoint)}}}</code><br><b>Credentials entered:</b> {{{(hasNivodaCredentials ? "Yes" : "No")}}}<br><b>Last test:</b> {{{Html(lastNivodaTest)}}}</p></div>
</div>
<div class="card decision"><h2>Decision Matrix</h2><table><thead><tr><th>Product choice</th><th>Current evidence</th><th>Recommended posture</th></tr></thead><tbody>
<tr><td>Multi-user, cloud sync or shared-device workflow</td><td>OPALNOVA is currently a local Windows desktop app using one SQLite database at <code>{{{Html(DatabaseBootstrapper.DatabasePath)}}}</code>.</td><td>Do not start this as a small patch. Choose the sharing model first: single owner with backups, shared Windows profile, LAN file risk, or a real hosted service with authentication and conflict handling.</td></tr>
<tr><td>Direct email delivery</td><td>Proposal generation, email draft copy and sent/follow-up tracking already exist. {{{sentProposalCount:N0}}} quote(s) currently have recorded sent proposal dates.</td><td>Keep the draft-based workflow until sending volume, deliverability requirements and account setup are clear. If added later, start with one provider and an opt-in settings page.</td></tr>
<tr><td>API-level supplier ordering</td><td>Nivoda staging handoff and GraphQL diagnostics exist, while live hold/order mutations remain gated. {{{externalDiamondInProgressCount:N0}}} saved supplier diamond(s) are currently in hold/order-style states.</td><td>Keep hold/order API actions disabled until Nivoda confirms the accessible mutation names, required payloads and account-specific error cases.</td></tr>
<tr><td>Scheduling, calendar and capacity planning</td><td>Production Board, Stage Checklist and Capacity Snapshot already provide daily planning from existing job due dates and labour hours. {{{activeJobCount:N0}}} active job(s) are available for planning.</td><td>Add deeper scheduling only after due dates, status movement and labour estimates are consistently maintained. Avoid background scheduling before installer/app lifecycle decisions are settled.</td></tr>
<tr><td>Workspace navigation redesign</td><td>Workspace tabs, Search All, workflow homes and Recent Work already reduce hunting across the app.</td><td>Run a manual click-through before redesigning navigation. If friction remains, choose one workflow path to streamline rather than replacing the whole command centre.</td></tr>
<tr><td>Installer, update channel and release packaging</td><td>Release Readiness and Support Snapshot now expose runtime, path and validation context.</td><td>This is the lowest-risk next product decision if external distribution is the priority. Pick installer technology, shortcut ownership and update/version-check expectations before coding auto-update behavior.</td></tr>
</tbody></table></div>
<div class="card warn"><h2>Recommended Next Ticket Shape</h2><ul><li>Choose exactly one post-V2 product direction before implementation.</li><li>Write a narrow acceptance checklist before coding the chosen direction.</li><li>Keep the next change no-schema unless the selected direction has an explicit migration and backup plan.</li><li>Use Debug build, Release publish and launch smoke as the validation floor.</li></ul></div>
<div class="card"><h2>Safe Follow-Up Options</h2><ul><li><b>Installer decision note:</b> compare MSIX and Inno Setup using the current publish folder, without changing app startup.</li><li><b>Email delivery spike:</b> document provider/settings requirements and keep proposal sending draft-based until chosen.</li><li><b>Supplier ordering spike:</b> use Nivoda diagnostics and external review, with no live mutation button until schema confirmation.</li><li><b>Scheduling review:</b> audit active jobs for due dates and labour-hour quality before adding a calendar view.</li><li><b>Navigation review:</b> click through quote to payment to handover and record the first repeated friction point.</li></ul></div>
<p class="meta">This review intentionally reads existing records only. It does not replace Release Readiness, Support Snapshot, Data Integrity or a manual V2.x workflow smoke test.</p>
</body></html>
""";
        File.WriteAllText(path, html);
        return path;
    }

    public static string CreateSupportSnapshotReport()
    {
        var folder = BusinessSettingsService.GetPrintoutFolder();
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, $"OPALNOVA-Support-Snapshot-{DateTime.Now:yyyyMMdd-HHmmss}.html");
        var settings = BusinessSettingsService.Load();
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? assembly.GetName().Version?.ToString()
            ?? "unknown";
        var processPath = Environment.ProcessPath ?? "unknown";
        var appFolder = AppContext.BaseDirectory;
        var backupFolder = BusinessSettingsService.GetBackupFolder();
        var printoutFolder = BusinessSettingsService.GetPrintoutFolder();
        var databaseSummary = FileSummary(DatabaseBootstrapper.DatabasePath);
        var settingsSummary = FileSummary(BusinessSettingsService.SettingsPath);
        var savedViewsSummary = FileSummary(SavedViewService.FilePath);
        var errorLogSummary = FileSummary(ErrorLogService.LogPath);
        var photoSummary = FolderSummary(DatabaseBootstrapper.PhotoDirectory);
        var backupSummary = FolderSummary(backupFolder);
        var printoutSummary = FolderSummary(printoutFolder);
        var latestBackup = LatestFileSummary(backupFolder, "*.db", "*.zip");
        var pendingRestore = File.Exists(DatabaseBootstrapper.PendingRestorePath)
            ? $"Pending restore staged at {DatabaseBootstrapper.PendingRestorePath}"
            : "No pending restore file found.";

        var html = $$$"""
<!doctype html>
<html><head><meta charset="utf-8"><title>OPALNOVA Support Snapshot</title>
<style>body{font-family:Segoe UI,Arial,sans-serif;margin:32px;line-height:1.5;color:#1f2937;background:#f8fafc}h1,h2,h3{color:#111827}.meta{color:#6b7280}.grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(280px,1fr));gap:14px}.card{background:#fff;border:1px solid #d1d5db;border-radius:10px;padding:16px;margin:14px 0}.warn{border-left:5px solid #b45309;background:#fff7ed}.ok{border-left:5px solid #047857;background:#ecfdf5}code{background:#e5e7eb;padding:2px 5px;border-radius:4px;word-break:break-all}li{margin:5px 0}@media print{body{background:#fff;margin:12mm}.card{break-inside:avoid}}</style></head>
<body>
<h1>OPALNOVA Support Snapshot</h1>
<p class="meta">Generated locally: {{{Html(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))}}}<br>Installed version: {{{Html(version)}}}</p>
<div class="grid">
<div class="card"><h2>Runtime</h2><p><b>Executable:</b><br>{{{Html(processPath)}}}</p><p><b>App folder:</b><br>{{{Html(appFolder)}}}</p></div>
<div class="card"><h2>Business</h2><p><b>Name:</b> {{{Html(settings.BusinessName)}}}</p><p><b>Database:</b><br>{{{Html(DatabaseBootstrapper.DatabasePath)}}}</p><p>{{{Html(databaseSummary)}}}</p></div>
<div class="card"><h2>Folders</h2><p><b>Backups:</b><br>{{{Html(backupFolder)}}}<br>{{{Html(backupSummary)}}}</p><p><b>Printouts:</b><br>{{{Html(printoutFolder)}}}<br>{{{Html(printoutSummary)}}}</p><p><b>Photos:</b><br>{{{Html(DatabaseBootstrapper.PhotoDirectory)}}}<br>{{{Html(photoSummary)}}}</p></div>
</div>
<div class="grid">
<div class="card"><h2>Settings Files</h2><p><b>Business settings:</b><br>{{{Html(BusinessSettingsService.SettingsPath)}}}<br>{{{Html(settingsSummary)}}}</p><p><b>Saved views:</b><br>{{{Html(SavedViewService.FilePath)}}}<br>{{{Html(savedViewsSummary)}}}</p></div>
<div class="card"><h2>Logs And Restore</h2><p><b>Error log:</b><br>{{{Html(ErrorLogService.LogPath)}}}<br>{{{Html(errorLogSummary)}}}</p><p><b>Restore state:</b><br>{{{Html(pendingRestore)}}}</p></div>
<div class="card"><h2>Latest Backup</h2><p>{{{Html(latestBackup)}}}</p></div>
</div>
<div class="card ok"><h2>Useful Support Steps</h2><ol><li>Open About and confirm the displayed version matches this snapshot.</li><li>Run Health Check and save the generated text report if data counts or paths look wrong.</li><li>Run Data Integrity when records, photos, payments, stock states or generated files look inconsistent.</li><li>Create a fresh backup before restore, import, cleanup or manual database repair work.</li><li>Use Release Readiness before packaging, copying, installing or handing off a build.</li></ol></div>
<div class="card"><h2>What To Share For Support</h2><ul><li>This support snapshot.</li><li>The exact action you clicked and what happened.</li><li>A screenshot of the error message if one appeared.</li><li>The Health Check text report when the issue is data/path related.</li><li>The Data Integrity report when linked records, files, payments or inventory states look wrong.</li></ul></div>
<div class="card warn"><h2>Privacy And Safety</h2><ul><li>Do not share database backups, export bundles or customer documents publicly.</li><li>Do not share supplier credentials, Nivoda credentials, private API keys or Windows account passwords.</li><li>This snapshot reports paths and file status only. It does not include customer records, payment records, inventory rows or supplier credentials.</li><li>If a backup must be shared privately for repair, create a fresh backup first and confirm the recipient is trusted.</li></ul></div>
</body></html>
""";
        File.WriteAllText(path, html);
        return path;

        static string FileSummary(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return "Path not configured.";
            if (!File.Exists(filePath))
                return "File not found.";
            var info = new FileInfo(filePath);
            return $"Exists - {info.Length:N0} bytes - modified {info.LastWriteTime:g}.";
        }

        static string FolderSummary(string folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath))
                return "Path not configured.";
            if (!Directory.Exists(folderPath))
                return "Folder not found.";
            var info = new DirectoryInfo(folderPath);
            return $"Exists - modified {info.LastWriteTime:g}.";
        }

        static string LatestFileSummary(string folderPath, params string[] patterns)
        {
            if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
                return "No accessible backup folder found.";

            var latest = patterns
                .SelectMany(pattern => Directory.EnumerateFiles(folderPath, pattern, SearchOption.TopDirectoryOnly))
                .Select(file => new FileInfo(file))
                .OrderByDescending(file => file.LastWriteTime)
                .FirstOrDefault();

            return latest == null
                ? "No .db or .zip backup files found in the configured backup folder."
                : $"{latest.FullName} - {latest.Length:N0} bytes - modified {latest.LastWriteTime:g}.";
        }
    }

    public static string CreateAboutText()
    {
        var settings = BusinessSettingsService.Load();
        return $"OPALNOVA\nVersion 2.6.0 - Roadmap Completion Record\n\nBusiness: {settings.BusinessName}\nDatabase: {DatabaseBootstrapper.DatabasePath}\nBackups: {BusinessSettingsService.GetBackupFolder()}\nPrintouts: {BusinessSettingsService.GetPrintoutFolder()}\nPhotos: {DatabaseBootstrapper.PhotoDirectory}\nError log: {ErrorLogService.LogPath}\n\nThis app stores your jewellery business data locally on this Windows computer using SQLite.";
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

    private static string BuildDataIntegrityHtml(AppDbContext db, List<DataIntegrityIssue> issues)
    {
        var errorCount = issues.Count(i => i.Severity == "Error");
        var warningCount = issues.Count(i => i.Severity == "Warning");
        var reviewCount = issues.Count(i => i.Severity == "Review");
        var status = issues.Count == 0 ? "No integrity issues found" : $"{issues.Count:N0} item(s) need review";

        var html = new StringBuilder();
        html.AppendLine("""
<!doctype html>
<html><head><meta charset="utf-8"><title>OPALNOVA Data Integrity Check</title>
<style>body{font-family:Segoe UI,Arial,sans-serif;margin:32px;line-height:1.45;color:#1f2937;background:#f8fafc}h1,h2{color:#111827}.meta{color:#6b7280}.grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(210px,1fr));gap:12px;margin:18px 0}.card{background:#fff;border:1px solid #d1d5db;border-radius:10px;padding:14px}.card strong{display:block;font-size:24px;color:#111827}.ok{border-left:5px solid #047857}.warn{border-left:5px solid #b45309}.bad{border-left:5px solid #b91c1c}table{width:100%;border-collapse:collapse;background:#fff;border:1px solid #d1d5db}th,td{padding:8px 10px;border-bottom:1px solid #e5e7eb;text-align:left;vertical-align:top}th{background:#111827;color:#f9fafb}.sev{font-weight:700}.Error{color:#b91c1c}.Warning{color:#b45309}.Review{color:#1f4f5f}.small{font-size:12px;color:#6b7280}@media print{body{background:#fff;margin:12mm}.card,table{break-inside:avoid}}</style></head>
<body>
""");
        html.AppendLine("<h1>OPALNOVA Data Integrity Check</h1>");
        html.AppendLine($"<p class=\"meta\">Generated locally: {Html(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))}<br>Database: {Html(DatabaseBootstrapper.DatabasePath)}</p>");
        html.AppendLine("<div class=\"grid\">");
        html.AppendLine($"<div class=\"card {(issues.Count == 0 ? "ok" : "warn")}\"><strong>{Html(status)}</strong><span>Overall status</span></div>");
        html.AppendLine($"<div class=\"card bad\"><strong>{errorCount:N0}</strong><span>Broken required links</span></div>");
        html.AppendLine($"<div class=\"card warn\"><strong>{warningCount:N0}</strong><span>Missing optional links or files</span></div>");
        html.AppendLine($"<div class=\"card\"><strong>{reviewCount:N0}</strong><span>Business data review items</span></div>");
        html.AppendLine("</div>");
        html.AppendLine("<div class=\"grid\">");
        html.AppendLine(SummaryMetric("Customers", db.Customers.Count()));
        html.AppendLine(SummaryMetric("Quotes", db.CustomQuotes.Count()));
        html.AppendLine(SummaryMetric("Quote Options", db.QuoteOptions.Count()));
        html.AppendLine(SummaryMetric("Jobs", db.Jobs.Count()));
        html.AppendLine(SummaryMetric("Sales", db.Sales.Count()));
        html.AppendLine(SummaryMetric("Payments", db.Payments.Count()));
        html.AppendLine(SummaryMetric("Stock", db.JewelleryItems.Count()));
        html.AppendLine(SummaryMetric("Stones", db.Stones.Count()));
        html.AppendLine(SummaryMetric("Materials", db.Materials.Count()));
        html.AppendLine(SummaryMetric("Photos", db.PhotoRecords.Count()));
        html.AppendLine("</div>");
        html.AppendLine("<h2>Integrity Findings</h2>");

        if (issues.Count == 0)
        {
            html.AppendLine("<div class=\"card ok\"><p>No orphaned links, missing proposal/design/photo files, negative stock balances, incomplete payment links, or conflicting market stock states were found by this read-only check.</p></div>");
        }
        else
        {
            html.AppendLine("<table><thead><tr><th>Severity</th><th>Area</th><th>Record</th><th>Field</th><th>Issue</th></tr></thead><tbody>");
            foreach (var issue in issues.OrderBy(i => SeverityRank(i.Severity)).ThenBy(i => i.Area).ThenBy(i => i.Record))
            {
                html.AppendLine($"<tr><td class=\"sev {Html(issue.Severity)}\">{Html(issue.Severity)}</td><td>{Html(issue.Area)}</td><td>{Html(issue.Record)}</td><td>{Html(issue.Field)}</td><td>{Html(issue.Message)}</td></tr>");
            }
            html.AppendLine("</tbody></table>");
        }

        html.AppendLine("<p class=\"small\">This check is read-only. It does not repair, delete, relink, import or restore any records. Create a backup before manually fixing records listed here.</p>");
        html.AppendLine("</body></html>");
        return html.ToString();
    }

    private static string SummaryMetric(string label, int count) => $"<div class=\"card\"><strong>{count:N0}</strong><span>{Html(label)}</span></div>";

    private static int SeverityRank(string severity) => severity switch
    {
        "Error" => 0,
        "Warning" => 1,
        "Review" => 2,
        _ => 3
    };

    private static void AddMissingReference(List<DataIntegrityIssue> issues, string severity, string area, string record, string field, int? id, string targetArea, HashSet<int> targetIds)
    {
        if (id.HasValue && !targetIds.Contains(id.Value))
            AddIssue(issues, severity, area, record, field, $"Linked {targetArea} record #{id.Value} was not found.");
    }

    private static void AddRequiredReference(List<DataIntegrityIssue> issues, string severity, string area, string record, string field, int id, string targetArea, HashSet<int> targetIds)
    {
        if (id <= 0 || !targetIds.Contains(id))
            AddIssue(issues, severity, area, record, field, $"Required {targetArea} record #{id} was not found.");
    }

    private static void AddIssue(List<DataIntegrityIssue> issues, string severity, string area, string record, string field, string message)
    {
        issues.Add(new DataIntegrityIssue(severity, area, record, field, message));
    }

    private static bool PhotoEntityExists(
        string entityType,
        int entityId,
        HashSet<int> customerIds,
        HashSet<int> supplierIds,
        HashSet<int> materialIds,
        HashSet<int> stoneIds,
        HashSet<int> jewelleryIds,
        HashSet<int> jobIds,
        HashSet<int> saleIds,
        HashSet<int> marketEventIds,
        HashSet<int> productionBatchIds,
        HashSet<int> onlineListingIds,
        HashSet<int> purchaseOrderIds,
        HashSet<int> taskIds,
        HashSet<int> quoteIds,
        HashSet<int> quoteOptionIds,
        HashSet<int> externalDiamondIds,
        HashSet<int> photoIds) => entityType switch
    {
        nameof(Customer) => customerIds.Contains(entityId),
        nameof(Supplier) => supplierIds.Contains(entityId),
        nameof(Material) => materialIds.Contains(entityId),
        nameof(Stone) => stoneIds.Contains(entityId),
        nameof(JewelleryItem) => jewelleryIds.Contains(entityId),
        nameof(Job) => jobIds.Contains(entityId),
        nameof(Sale) => saleIds.Contains(entityId),
        nameof(MarketEvent) => marketEventIds.Contains(entityId),
        nameof(ProductionBatch) => productionBatchIds.Contains(entityId),
        nameof(OnlineListing) => onlineListingIds.Contains(entityId),
        nameof(PurchaseOrder) => purchaseOrderIds.Contains(entityId),
        nameof(BusinessTask) => taskIds.Contains(entityId),
        nameof(CustomQuote) => quoteIds.Contains(entityId),
        nameof(QuoteOption) => quoteOptionIds.Contains(entityId),
        nameof(ExternalDiamond) => externalDiamondIds.Contains(entityId),
        nameof(PhotoRecord) => photoIds.Contains(entityId),
        _ => false
    };

    private static string Html(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);

    private sealed record DataIntegrityIssue(string Severity, string Area, string Record, string Field, string Message);
}
