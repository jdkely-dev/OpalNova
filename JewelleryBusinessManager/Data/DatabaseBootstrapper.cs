using System.IO;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using JewelleryBusinessManager.Services;
using JewelleryBusinessManager.Models;

namespace JewelleryBusinessManager.Data;

public static class DatabaseBootstrapper
{
    public static string AppDataDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "JewelleryBusinessManager");

    private static string _databasePath = Path.Combine(AppDataDirectory, "jewellery_business_manager.db");

    public static string DatabasePath => _databasePath;
    public static string PhotoDirectory => Path.Combine(AppDataDirectory, "Photos");
    public static string BackupDirectory => Path.Combine(AppDataDirectory, "Backups");

    public static string PendingRestorePath => Path.Combine(AppDataDirectory, "pending-restore.db");
    public static string PendingRestoreNotePath => Path.Combine(AppDataDirectory, "pending-restore.txt");

    public static void ApplyPendingRestoreIfNeeded()
    {
        Directory.CreateDirectory(AppDataDirectory);
        if (!File.Exists(PendingRestorePath))
            return;

        var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        var safetyPath = Path.Combine(AppDataDirectory, $"jewellery_business_manager.before-pending-restore-{timestamp}.db");

        try
        {
            SqliteConnection.ClearAllPools();
            GC.Collect();
            GC.WaitForPendingFinalizers();

            if (File.Exists(_databasePath))
            {
                // At this point startup has not opened the active database yet, so a normal copy is safe.
                CopyFileWithSharedRead(_databasePath, safetyPath);
            }

            ClearSQLiteSidecarFiles(_databasePath);
            CopyFileWithSharedRead(PendingRestorePath, _databasePath);
            SafeDelete(PendingRestorePath);
            File.WriteAllText(PendingRestoreNotePath,
                "Pending restore applied successfully.\n" +
                $"Applied: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n" +
                $"Safety copy of previous database: {(File.Exists(safetyPath) ? safetyPath : "No previous database existed")}\n" +
                $"Active database: {_databasePath}\n");
        }
        catch (Exception ex)
        {
            var failedPath = Path.Combine(AppDataDirectory, $"pending-restore.failed-{timestamp}.db");
            try
            {
                if (File.Exists(PendingRestorePath))
                    File.Move(PendingRestorePath, failedPath, overwrite: true);
            }
            catch
            {
                // Keep startup alive; the note explains the failure.
            }

            File.WriteAllText(PendingRestoreNotePath,
                "Pending restore could not be applied. The app will continue using the existing database.\n" +
                $"Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n" +
                $"Active database: {_databasePath}\n" +
                $"Failed pending restore file: {failedPath}\n\n" +
                ex);
        }
    }

    public static void Initialize()
    {
        Directory.CreateDirectory(AppDataDirectory);
        Directory.CreateDirectory(PhotoDirectory);
        Directory.CreateDirectory(BackupDirectory);

        try
        {
            using var db = new AppDbContext();
            db.Database.EnsureCreated();
            EnsureProductionBatchSchema(db);
            EnsureOnlineListingSchema(db);
            EnsureMarketProSchema(db);
            EnsurePurchaseOrderSchema(db);
            EnsureTaskSchema(db);
            EnsureCustomQuoteSchema(db);
            EnsureExternalDiamondSchema(db);
            EnsureExternalDiamondQuoteLinkSchema(db);
            Seed(db);
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 26 || ex.Message.Contains("file is not a database", StringComparison.OrdinalIgnoreCase))
        {
            RecoverInvalidDatabaseFile(ex);
            using var db = new AppDbContext();
            db.Database.EnsureCreated();
            EnsureProductionBatchSchema(db);
            EnsureOnlineListingSchema(db);
            EnsureMarketProSchema(db);
            EnsurePurchaseOrderSchema(db);
            EnsureTaskSchema(db);
            EnsureCustomQuoteSchema(db);
            EnsureExternalDiamondSchema(db);
            EnsureExternalDiamondQuoteLinkSchema(db);
            Seed(db);
        }
    }

    private static void RecoverInvalidDatabaseFile(Exception ex)
    {
        Directory.CreateDirectory(AppDataDirectory);
        var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        var originalDatabasePath = _databasePath;

        // EF/SQLite may still have the invalid file open after the failed EnsureCreated call.
        // Clear pooled SQLite handles before attempting any file operation.
        SqliteConnection.ClearAllPools();
        GC.Collect();
        GC.WaitForPendingFinalizers();

        var recoveryAction = "No existing database file was present; a fresh database was created.";

        if (File.Exists(originalDatabasePath))
        {
            var quarantinePath = Path.Combine(AppDataDirectory, $"jewellery_business_manager.invalid-{timestamp}.db");
            recoveryAction = TryQuarantineInvalidDatabase(originalDatabasePath, quarantinePath);

            // If Windows still has the invalid file locked, do not crash again.
            // Instead, start this run against a new recovery database file.
            if (File.Exists(originalDatabasePath))
            {
                _databasePath = Path.Combine(AppDataDirectory, $"jewellery_business_manager.recovered-{timestamp}.db");
                recoveryAction += "\nThe original invalid database was still locked by Windows, so this run will use a fresh recovery database path instead.";
            }
        }

        ClearSQLiteSidecarFiles(originalDatabasePath);
        if (!string.Equals(originalDatabasePath, _databasePath, StringComparison.OrdinalIgnoreCase))
            ClearSQLiteSidecarFiles(_databasePath);

        var notePath = Path.Combine(AppDataDirectory, $"database-recovery-{timestamp}.txt");
        File.WriteAllText(notePath,
            "OPALNOVA database recovery\n" +
            $"Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n" +
            $"Original database path: {originalDatabasePath}\n" +
            $"Active database path now: {_databasePath}\n" +
            "Problem: The file at the active database path was not a valid SQLite database.\n" +
            $"Action: {recoveryAction}\n" +
            "Next step: Use Restore Backup to restore a valid .db backup or full data bundle ZIP.\n\n" +
            ex);
    }

    private static string TryQuarantineInvalidDatabase(string sourcePath, string quarantinePath)
    {
        Exception? lastError = null;

        for (var attempt = 1; attempt <= 3; attempt++)
        {
            try
            {
                SqliteConnection.ClearAllPools();
                GC.Collect();
                GC.WaitForPendingFinalizers();

                File.Move(sourcePath, quarantinePath, overwrite: true);
                return $"The invalid database was moved to: {quarantinePath}";
            }
            catch (IOException ioEx)
            {
                lastError = ioEx;
                Thread.Sleep(250 * attempt);
            }
            catch (UnauthorizedAccessException accessEx)
            {
                lastError = accessEx;
                Thread.Sleep(250 * attempt);
            }
        }

        try
        {
            CopyFileWithSharedRead(sourcePath, quarantinePath);
            return $"The invalid database could not be moved, but a copy was quarantined at: {quarantinePath}\nMove/delete problem: {lastError?.Message}";
        }
        catch (Exception copyEx) when (copyEx is IOException || copyEx is UnauthorizedAccessException)
        {
            return $"The invalid database could not be moved or copied because Windows still had it locked. Last error: {copyEx.Message}";
        }
    }



    private static void CopyFileWithSharedRead(string sourcePath, string destinationPath)
    {
        BackupService.CopyFileSharedRead(sourcePath, destinationPath);
    }

    private static void SafeDelete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    private static void ClearSQLiteSidecarFiles(string databasePath)
    {
        foreach (var sidecar in new[] { databasePath + "-wal", databasePath + "-shm" })
        {
            try
            {
                if (File.Exists(sidecar))
                    File.Delete(sidecar);
            }
            catch (IOException)
            {
                // Non-fatal. SQLite/Windows may release it later; startup can continue.
            }
            catch (UnauthorizedAccessException)
            {
                // Non-fatal. SQLite/Windows may release it later; startup can continue.
            }
        }
    }

    private static void EnsureProductionBatchSchema(AppDbContext db)
    {
        // EnsureCreated does not add new tables to an already-existing SQLite database.
        // V1.11 adds production batch tables, so create them manually if the user is upgrading an existing database.
        db.Database.ExecuteSqlRaw(@"
CREATE TABLE IF NOT EXISTS ProductionBatches (
    Id INTEGER NOT NULL CONSTRAINT PK_ProductionBatches PRIMARY KEY AUTOINCREMENT,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL,
    BatchCode TEXT NOT NULL DEFAULT '',
    Name TEXT NOT NULL DEFAULT '',
    CollectionName TEXT NULL,
    Status INTEGER NOT NULL DEFAULT 0,
    StartDate TEXT NOT NULL,
    TargetCompletionDate TEXT NULL,
    MarketEventId INTEGER NULL,
    PlannedPieces INTEGER NOT NULL DEFAULT 0,
    CompletedPieces INTEGER NOT NULL DEFAULT 0,
    EstimatedMaterialCost TEXT NOT NULL DEFAULT '0',
    EstimatedLabourHours TEXT NOT NULL DEFAULT '0',
    EstimatedRetailValue TEXT NOT NULL DEFAULT '0',
    Notes TEXT NULL
);");

        db.Database.ExecuteSqlRaw(@"
CREATE TABLE IF NOT EXISTS ProductionBatchItems (
    Id INTEGER NOT NULL CONSTRAINT PK_ProductionBatchItems PRIMARY KEY AUTOINCREMENT,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL,
    ProductionBatchId INTEGER NOT NULL,
    JewelleryItemId INTEGER NULL,
    StoneId INTEGER NULL,
    JobId INTEGER NULL,
    ItemName TEXT NOT NULL DEFAULT '',
    ItemType TEXT NOT NULL DEFAULT '',
    PlannedQuantity TEXT NOT NULL DEFAULT '1',
    CompletedQuantity TEXT NOT NULL DEFAULT '0',
    EstimatedCost TEXT NOT NULL DEFAULT '0',
    EstimatedRetailValue TEXT NOT NULL DEFAULT '0',
    Status TEXT NOT NULL DEFAULT 'Planned',
    Notes TEXT NULL
);");

        db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_ProductionBatches_BatchCode ON ProductionBatches (BatchCode);");
        db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_ProductionBatchItems_ProductionBatchId ON ProductionBatchItems (ProductionBatchId);");
    }

    private static void EnsureOnlineListingSchema(AppDbContext db)
    {
        // EnsureCreated does not add new tables to an existing SQLite database.
        // V1.13 adds online listing tracking, so create it manually for upgrades.
        db.Database.ExecuteSqlRaw(@"
CREATE TABLE IF NOT EXISTS OnlineListings (
    Id INTEGER NOT NULL CONSTRAINT PK_OnlineListings PRIMARY KEY AUTOINCREMENT,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL,
    JewelleryItemId INTEGER NULL,
    Status INTEGER NOT NULL DEFAULT 0,
    PhotoStatus INTEGER NOT NULL DEFAULT 0,
    Platform TEXT NOT NULL DEFAULT 'Website',
    ListingUrl TEXT NULL,
    ListingDate TEXT NULL,
    SeoTitle TEXT NULL,
    ShortDescription TEXT NULL,
    LongDescription TEXT NULL,
    InstagramCaption TEXT NULL,
    Hashtags TEXT NULL,
    PhotosDone INTEGER NOT NULL DEFAULT 0,
    DescriptionDone INTEGER NOT NULL DEFAULT 0,
    PriceChecked INTEGER NOT NULL DEFAULT 0,
    ListedOnline INTEGER NOT NULL DEFAULT 0,
    SharedToSocial INTEGER NOT NULL DEFAULT 0,
    Notes TEXT NULL
);");

        db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_OnlineListings_JewelleryItemId ON OnlineListings (JewelleryItemId);");
        db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_OnlineListings_Status ON OnlineListings (Status);");
    }

    private static void EnsureMarketProSchema(AppDbContext db)
    {
        // V1.16 adds optional market workflow columns. ALTER TABLE is used so existing databases upgrade safely.
        EnsureColumn(db, "MarketEvents", "OpeningFloat", "TEXT NOT NULL DEFAULT '0'");
        EnsureColumn(db, "MarketEvents", "CashSales", "TEXT NOT NULL DEFAULT '0'");
        EnsureColumn(db, "MarketEvents", "CardSales", "TEXT NOT NULL DEFAULT '0'");
        EnsureColumn(db, "MarketEvents", "OtherSales", "TEXT NOT NULL DEFAULT '0'");
        EnsureColumn(db, "MarketEvents", "TravelCost", "TEXT NOT NULL DEFAULT '0'");
        EnsureColumn(db, "MarketEvents", "DisplayCost", "TEXT NOT NULL DEFAULT '0'");
        EnsureColumn(db, "MarketEvents", "OtherCosts", "TEXT NOT NULL DEFAULT '0'");
        EnsureColumn(db, "MarketEvents", "ItemsPacked", "INTEGER NOT NULL DEFAULT 0");
        EnsureColumn(db, "MarketEvents", "ItemsSold", "INTEGER NOT NULL DEFAULT 0");
        EnsureColumn(db, "MarketEvents", "ItemsReturned", "INTEGER NOT NULL DEFAULT 0");
        EnsureColumn(db, "MarketEvents", "LastReconciledAt", "TEXT NULL");
        EnsureColumn(db, "MarketEvents", "PackingChecklist", "TEXT NULL");
        EnsureColumn(db, "MarketEvents", "ReconciliationNotes", "TEXT NULL");

        EnsureColumn(db, "MarketStocks", "PackedAt", "TEXT NULL");
        EnsureColumn(db, "MarketStocks", "SoldAt", "TEXT NULL");
        EnsureColumn(db, "MarketStocks", "ReturnedToStock", "INTEGER NOT NULL DEFAULT 0");
        EnsureColumn(db, "MarketStocks", "SalePrice", "TEXT NOT NULL DEFAULT '0'");
        EnsureColumn(db, "MarketStocks", "PaymentMethodText", "TEXT NULL");
        EnsureColumn(db, "MarketStocks", "SaleId", "INTEGER NULL");

        db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_MarketStocks_MarketEventId ON MarketStocks (MarketEventId);");
        db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_MarketStocks_JewelleryItemId ON MarketStocks (JewelleryItemId);");
    }


    private static void EnsurePurchaseOrderSchema(AppDbContext db)
    {
        // V1.17 adds purchase order and supplier reorder planning tables.
        db.Database.ExecuteSqlRaw(@"
CREATE TABLE IF NOT EXISTS PurchaseOrders (
    Id INTEGER NOT NULL CONSTRAINT PK_PurchaseOrders PRIMARY KEY AUTOINCREMENT,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL,
    PurchaseOrderCode TEXT NOT NULL DEFAULT '',
    SupplierId INTEGER NULL,
    Status INTEGER NOT NULL DEFAULT 0,
    OrderDate TEXT NOT NULL,
    ExpectedDeliveryDate TEXT NULL,
    ReceivedDate TEXT NULL,
    ShippingCost TEXT NOT NULL DEFAULT '0',
    OtherCost TEXT NOT NULL DEFAULT '0',
    SupplierReference TEXT NOT NULL DEFAULT '',
    Notes TEXT NOT NULL DEFAULT '',
    ItemsTotal TEXT NOT NULL DEFAULT '0'
);");

        db.Database.ExecuteSqlRaw(@"
CREATE TABLE IF NOT EXISTS PurchaseOrderItems (
    Id INTEGER NOT NULL CONSTRAINT PK_PurchaseOrderItems PRIMARY KEY AUTOINCREMENT,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL,
    PurchaseOrderId INTEGER NOT NULL,
    MaterialId INTEGER NULL,
    ItemName TEXT NOT NULL DEFAULT '',
    UnitType TEXT NOT NULL DEFAULT '',
    OrderedQuantity TEXT NOT NULL DEFAULT '1',
    ReceivedQuantity TEXT NOT NULL DEFAULT '0',
    UnitCost TEXT NOT NULL DEFAULT '0',
    LineTotal TEXT NOT NULL DEFAULT '0',
    Notes TEXT NOT NULL DEFAULT ''
);");

        db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_PurchaseOrders_PurchaseOrderCode ON PurchaseOrders (PurchaseOrderCode);");
        db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_PurchaseOrderItems_PurchaseOrderId ON PurchaseOrderItems (PurchaseOrderId);");
        db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_PurchaseOrderItems_MaterialId ON PurchaseOrderItems (MaterialId);");
    }


    private static void EnsureTaskSchema(AppDbContext db)
    {
        // V1.18 adds tasks/reminders/work queue. Create the table manually for existing databases.
        db.Database.ExecuteSqlRaw(@"
CREATE TABLE IF NOT EXISTS BusinessTasks (
    Id INTEGER NOT NULL CONSTRAINT PK_BusinessTasks PRIMARY KEY AUTOINCREMENT,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL,
    TaskCode TEXT NOT NULL DEFAULT '',
    Title TEXT NOT NULL DEFAULT '',
    Category INTEGER NOT NULL DEFAULT 0,
    Status INTEGER NOT NULL DEFAULT 0,
    Priority INTEGER NOT NULL DEFAULT 1,
    DueDate TEXT NULL,
    ReminderDate TEXT NULL,
    CompletedAt TEXT NULL,
    CustomerId INTEGER NULL,
    JobId INTEGER NULL,
    JewelleryItemId INTEGER NULL,
    StoneId INTEGER NULL,
    MarketEventId INTEGER NULL,
    ProductionBatchId INTEGER NULL,
    PurchaseOrderId INTEGER NULL,
    Description TEXT NULL,
    FollowUpNotes TEXT NULL,
    ShowOnDashboard INTEGER NOT NULL DEFAULT 1
);");

        db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_BusinessTasks_TaskCode ON BusinessTasks (TaskCode);");
        db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_BusinessTasks_Status ON BusinessTasks (Status);");
        db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_BusinessTasks_DueDate ON BusinessTasks (DueDate);");
    }

    private static void EnsureColumn(AppDbContext db, string tableName, string columnName, string columnDefinition)
    {
        var connection = db.Database.GetDbConnection();
        var shouldClose = connection.State != System.Data.ConnectionState.Open;
        var quotedTableName = QuoteSqlIdentifier(tableName);
        var quotedColumnName = QuoteSqlIdentifier(columnName);
        if (shouldClose)
            connection.Open();

        try
        {
            using var command = connection.CreateCommand();
            command.CommandText = $"PRAGMA table_info({quotedTableName});";
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var existing = reader["name"]?.ToString() ?? string.Empty;
                if (string.Equals(existing, columnName, StringComparison.OrdinalIgnoreCase))
                    return;
            }
        }
        finally
        {
            if (shouldClose)
                connection.Close();
        }

        // SQLite cannot parameterize identifiers, so table and column names are validated before this raw schema SQL is built.
        var alterSql = "ALTER TABLE " + quotedTableName + " ADD COLUMN " + quotedColumnName + " " + columnDefinition + ";";
        db.Database.ExecuteSqlRaw(alterSql);
    }

    private static string QuoteSqlIdentifier(string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier))
            throw new InvalidOperationException("Database identifier cannot be empty.");

        foreach (var ch in identifier)
        {
            if (!char.IsLetterOrDigit(ch) && ch != '_')
                throw new InvalidOperationException($"Unsafe database identifier: {identifier}");
        }

        return "\"" + identifier.Replace("\"", "\"\"") + "\"";
    }


    private static void EnsureCustomQuoteSchema(AppDbContext db)
    {
        db.Database.ExecuteSqlRaw(@"
CREATE TABLE IF NOT EXISTS CustomQuotes (
    Id INTEGER NOT NULL CONSTRAINT PK_CustomQuotes PRIMARY KEY AUTOINCREMENT,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL,
    QuoteCode TEXT NOT NULL DEFAULT '',
    CustomerId INTEGER NULL,
    LinkedJobId INTEGER NULL,
    Title TEXT NOT NULL DEFAULT '',
    Status TEXT NOT NULL DEFAULT 'Draft',
    QuoteDate TEXT NOT NULL,
    ValidUntil TEXT NULL,
    AcceptedOptionId INTEGER NULL,
    DepositPercent TEXT NOT NULL DEFAULT '30',
    ProposalStatus TEXT NOT NULL DEFAULT 'Not Sent',
    ProposalLastGeneratedAt TEXT NULL,
    ProposalSentAt TEXT NULL,
    ProposalFollowUpDueAt TEXT NULL,
    ProposalLastPath TEXT NULL,
    ProposalEmailTo TEXT NULL,
    ProposalEmailSubject TEXT NULL,
    ProposalEmailMessage TEXT NULL,
    Introduction TEXT NULL,
    CustomerNotes TEXT NULL,
    InternalNotes TEXT NULL,
    Terms TEXT NULL
);
CREATE INDEX IF NOT EXISTS IX_CustomQuotes_QuoteCode ON CustomQuotes (QuoteCode);
CREATE INDEX IF NOT EXISTS IX_CustomQuotes_CustomerId ON CustomQuotes (CustomerId);

CREATE TABLE IF NOT EXISTS QuoteOptions (
    Id INTEGER NOT NULL CONSTRAINT PK_QuoteOptions PRIMARY KEY AUTOINCREMENT,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL,
    CustomQuoteId INTEGER NOT NULL,
    OptionName TEXT NOT NULL DEFAULT 'Option A',
    Description TEXT NULL,
    MetalDetails TEXT NULL,
    StoneDetails TEXT NULL,
    ImagePath TEXT NULL,
    LabourHours TEXT NOT NULL DEFAULT '0',
    LabourRate TEXT NOT NULL DEFAULT '0',
    MetalCost TEXT NOT NULL DEFAULT '0',
    StoneCost TEXT NOT NULL DEFAULT '0',
    SettingCost TEXT NOT NULL DEFAULT '0',
    FindingsCost TEXT NOT NULL DEFAULT '0',
    OtherCost TEXT NOT NULL DEFAULT '0',
    MarkupPercent TEXT NOT NULL DEFAULT '0',
    GstPercent TEXT NOT NULL DEFAULT '0',
    Subtotal TEXT NOT NULL DEFAULT '0',
    TotalPrice TEXT NOT NULL DEFAULT '0',
    IsRecommended INTEGER NOT NULL DEFAULT 0
);
CREATE INDEX IF NOT EXISTS IX_QuoteOptions_CustomQuoteId ON QuoteOptions (CustomQuoteId);

CREATE TABLE IF NOT EXISTS QuoteOptionStoneLinks (
    Id INTEGER NOT NULL CONSTRAINT PK_QuoteOptionStoneLinks PRIMARY KEY AUTOINCREMENT,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL,
    QuoteOptionId INTEGER NOT NULL,
    StoneId INTEGER NOT NULL,
    StoneCodeSnapshot TEXT NOT NULL DEFAULT '',
    DescriptionSnapshot TEXT NOT NULL DEFAULT '',
    UnitCost TEXT NOT NULL DEFAULT '0',
    ReservationStatus TEXT NOT NULL DEFAULT 'Proposed'
);
CREATE INDEX IF NOT EXISTS IX_QuoteOptionStoneLinks_QuoteOptionId ON QuoteOptionStoneLinks (QuoteOptionId);
CREATE INDEX IF NOT EXISTS IX_QuoteOptionStoneLinks_StoneId ON QuoteOptionStoneLinks (StoneId);

CREATE TABLE IF NOT EXISTS QuoteOptionMaterialLinks (
    Id INTEGER NOT NULL CONSTRAINT PK_QuoteOptionMaterialLinks PRIMARY KEY AUTOINCREMENT,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL,
    QuoteOptionId INTEGER NOT NULL,
    MaterialId INTEGER NOT NULL,
    MaterialCodeSnapshot TEXT NOT NULL DEFAULT '',
    MaterialNameSnapshot TEXT NOT NULL DEFAULT '',
    Quantity TEXT NOT NULL DEFAULT '0',
    UnitCost TEXT NOT NULL DEFAULT '0',
    UnitTypeSnapshot TEXT NOT NULL DEFAULT '',
    ReservationStatus TEXT NOT NULL DEFAULT 'Proposed'
);
CREATE INDEX IF NOT EXISTS IX_QuoteOptionMaterialLinks_QuoteOptionId ON QuoteOptionMaterialLinks (QuoteOptionId);
CREATE INDEX IF NOT EXISTS IX_QuoteOptionMaterialLinks_MaterialId ON QuoteOptionMaterialLinks (MaterialId);");

        EnsureColumn(db, "CustomQuotes", "ProposalStatus", "TEXT NOT NULL DEFAULT 'Not Sent'");
        EnsureColumn(db, "CustomQuotes", "ProposalLastGeneratedAt", "TEXT NULL");
        EnsureColumn(db, "CustomQuotes", "ProposalSentAt", "TEXT NULL");
        EnsureColumn(db, "CustomQuotes", "ProposalFollowUpDueAt", "TEXT NULL");
        EnsureColumn(db, "CustomQuotes", "ProposalLastPath", "TEXT NULL");
        EnsureColumn(db, "CustomQuotes", "ProposalEmailTo", "TEXT NULL");
        EnsureColumn(db, "CustomQuotes", "ProposalEmailSubject", "TEXT NULL");
        EnsureColumn(db, "CustomQuotes", "ProposalEmailMessage", "TEXT NULL");
    }


    private static void EnsureExternalDiamondSchema(AppDbContext db)
    {
        db.Database.ExecuteSqlRaw(@"
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
");
        EnsureColumn(db, "ExternalDiamonds", "SupplierReference", "TEXT NOT NULL DEFAULT ''");
        EnsureColumn(db, "ExternalDiamonds", "HoldRequestedAt", "TEXT NULL");
        EnsureColumn(db, "ExternalDiamonds", "HoldConfirmedAt", "TEXT NULL");
        EnsureColumn(db, "ExternalDiamonds", "HoldExpiresAt", "TEXT NULL");
        EnsureColumn(db, "ExternalDiamonds", "OrderRequestedAt", "TEXT NULL");
        EnsureColumn(db, "ExternalDiamonds", "OrderedAt", "TEXT NULL");
        EnsureColumn(db, "ExternalDiamonds", "ExpectedArrivalDate", "TEXT NULL");
        EnsureColumn(db, "ExternalDiamonds", "ReceivedAt", "TEXT NULL");
        EnsureColumn(db, "ExternalDiamonds", "ReleasedAt", "TEXT NULL");

        // Keep upgraded databases safe: these indexes depend on columns added above.
        // They must be created after EnsureColumn runs, otherwise older databases crash at startup.
        db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_ExternalDiamonds_HoldExpiresAt ON ExternalDiamonds (HoldExpiresAt);");
        db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_ExternalDiamonds_ExpectedArrivalDate ON ExternalDiamonds (ExpectedArrivalDate);");
    }


    private static void EnsureExternalDiamondQuoteLinkSchema(AppDbContext db)
    {
        db.Database.ExecuteSqlRaw(@"
CREATE TABLE IF NOT EXISTS QuoteOptionExternalDiamondLinks (
    Id INTEGER NOT NULL CONSTRAINT PK_QuoteOptionExternalDiamondLinks PRIMARY KEY AUTOINCREMENT,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL,
    QuoteOptionId INTEGER NOT NULL,
    ExternalDiamondId INTEGER NOT NULL,
    SourceSystemSnapshot TEXT NOT NULL DEFAULT 'Nivoda',
    SupplierDiamondIdSnapshot TEXT NOT NULL DEFAULT '',
    DiamondSummarySnapshot TEXT NOT NULL DEFAULT '',
    LabSnapshot TEXT NOT NULL DEFAULT '',
    CertificateNumberSnapshot TEXT NOT NULL DEFAULT '',
    SupplierPrice TEXT NOT NULL DEFAULT '0',
    Currency TEXT NOT NULL DEFAULT 'AUD',
    RetailPriceSnapshot TEXT NOT NULL DEFAULT '0',
    VideoUrlSnapshot TEXT NOT NULL DEFAULT '',
    CertificateUrlSnapshot TEXT NOT NULL DEFAULT '',
    LinkStatus TEXT NOT NULL DEFAULT 'Proposed'
);
CREATE INDEX IF NOT EXISTS IX_QuoteOptionExternalDiamondLinks_QuoteOptionId ON QuoteOptionExternalDiamondLinks (QuoteOptionId);
CREATE INDEX IF NOT EXISTS IX_QuoteOptionExternalDiamondLinks_ExternalDiamondId ON QuoteOptionExternalDiamondLinks (ExternalDiamondId);
CREATE INDEX IF NOT EXISTS IX_QuoteOptionExternalDiamondLinks_LinkStatus ON QuoteOptionExternalDiamondLinks (LinkStatus);");
    }

    private static void Seed(AppDbContext db)
    {
        if (!db.Suppliers.Any())
        {
            db.Suppliers.Add(new Supplier { Name = "Sample Supplier", ContactName = "Edit or delete me", Notes = "Starter record" });
        }
        if (!db.Customers.Any())
        {
            db.Customers.Add(new Customer { FullName = "Sample Customer", Notes = "Starter record" });
        }
        if (!db.Materials.Any())
        {
            db.Materials.Add(new Material
            {
                MaterialCode = "MAT-0001",
                Name = "Sterling silver sheet",
                Category = MaterialCategory.Silver,
                CurrentQuantity = 10,
                UnitType = UnitType.Grams,
                ReorderLevel = 2,
                PurchaseCost = 35
            });
        }
        if (!db.Stones.Any())
        {
            db.Stones.Add(new Stone
            {
                StoneCode = "STN-0001",
                StoneType = "Opal",
                WeightCarats = 1.25m,
                Shape = "Oval cabochon",
                BodyTone = "N4",
                Brightness = "B4",
                EstimatedValue = 120,
                Status = StoneStatus.Loose
            });
        }
        db.SaveChanges();
    }
}
