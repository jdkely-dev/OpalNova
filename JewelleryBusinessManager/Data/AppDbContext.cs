using Microsoft.EntityFrameworkCore;
using JewelleryBusinessManager.Models;

namespace JewelleryBusinessManager.Data;

public class AppDbContext : DbContext
{
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Material> Materials => Set<Material>();
    public DbSet<MaterialTransaction> MaterialTransactions => Set<MaterialTransaction>();
    public DbSet<OpalParcel> OpalParcels => Set<OpalParcel>();
    public DbSet<Stone> Stones => Set<Stone>();
    public DbSet<JewelleryItem> JewelleryItems => Set<JewelleryItem>();
    public DbSet<Job> Jobs => Set<Job>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<MarketEvent> MarketEvents => Set<MarketEvent>();
    public DbSet<MarketStock> MarketStocks => Set<MarketStock>();
    public DbSet<PhotoRecord> PhotoRecords => Set<PhotoRecord>();
    public DbSet<ProductionBatch> ProductionBatches => Set<ProductionBatch>();
    public DbSet<ProductionBatchItem> ProductionBatchItems => Set<ProductionBatchItem>();
    public DbSet<OnlineListing> OnlineListings => Set<OnlineListing>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<PurchaseOrderItem> PurchaseOrderItems => Set<PurchaseOrderItem>();
    public DbSet<BusinessTask> BusinessTasks => Set<BusinessTask>();
    public DbSet<CustomQuote> CustomQuotes => Set<CustomQuote>();
    public DbSet<QuoteOption> QuoteOptions => Set<QuoteOption>();
    public DbSet<QuoteOptionStoneLink> QuoteOptionStoneLinks => Set<QuoteOptionStoneLink>();
    public DbSet<QuoteOptionMaterialLink> QuoteOptionMaterialLinks => Set<QuoteOptionMaterialLink>();
    public DbSet<ExternalDiamond> ExternalDiamonds => Set<ExternalDiamond>();
    public DbSet<QuoteOptionExternalDiamondLink> QuoteOptionExternalDiamondLinks => Set<QuoteOptionExternalDiamondLink>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var dbPath = DatabaseBootstrapper.DatabasePath;
        optionsBuilder.UseSqlite($"Data Source={dbPath}");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Prototype indexes: helpful for searching, but not unique yet so quick data entry
        // does not fail when the user leaves a code blank during testing.
        modelBuilder.Entity<Customer>().HasIndex(x => x.FullName);
        modelBuilder.Entity<JewelleryItem>().HasIndex(x => x.StockCode);
        modelBuilder.Entity<Job>().HasIndex(x => x.JobCode);
        modelBuilder.Entity<Stone>().HasIndex(x => x.StoneCode);
        modelBuilder.Entity<Material>().HasIndex(x => x.MaterialCode);
        modelBuilder.Entity<ProductionBatch>().HasIndex(x => x.BatchCode);
        modelBuilder.Entity<ProductionBatchItem>().HasIndex(x => x.ProductionBatchId);
        modelBuilder.Entity<OnlineListing>().HasIndex(x => x.JewelleryItemId);
        modelBuilder.Entity<OnlineListing>().HasIndex(x => x.Status);
        modelBuilder.Entity<PurchaseOrder>().HasIndex(x => x.PurchaseOrderCode);
        modelBuilder.Entity<PurchaseOrderItem>().HasIndex(x => x.PurchaseOrderId);
        modelBuilder.Entity<PurchaseOrderItem>().HasIndex(x => x.MaterialId);
        modelBuilder.Entity<BusinessTask>().HasIndex(x => x.TaskCode);
        modelBuilder.Entity<BusinessTask>().HasIndex(x => x.Status);
        modelBuilder.Entity<BusinessTask>().HasIndex(x => x.DueDate);
        modelBuilder.Entity<CustomQuote>().HasIndex(x => x.QuoteCode);
        modelBuilder.Entity<CustomQuote>().HasIndex(x => x.CustomerId);
        modelBuilder.Entity<QuoteOption>().HasIndex(x => x.CustomQuoteId);
        modelBuilder.Entity<QuoteOptionStoneLink>().HasIndex(x => x.QuoteOptionId);
        modelBuilder.Entity<QuoteOptionStoneLink>().HasIndex(x => x.StoneId);
        modelBuilder.Entity<QuoteOptionMaterialLink>().HasIndex(x => x.QuoteOptionId);
        modelBuilder.Entity<QuoteOptionMaterialLink>().HasIndex(x => x.MaterialId);
        modelBuilder.Entity<ExternalDiamond>().HasIndex(x => x.SupplierDiamondId);
        modelBuilder.Entity<ExternalDiamond>().HasIndex(x => x.CertificateNumber);
        modelBuilder.Entity<ExternalDiamond>().HasIndex(x => x.Status);
        modelBuilder.Entity<ExternalDiamond>().HasIndex(x => x.HoldExpiresAt);
        modelBuilder.Entity<ExternalDiamond>().HasIndex(x => x.ExpectedArrivalDate);
        modelBuilder.Entity<QuoteOptionExternalDiamondLink>().HasIndex(x => x.QuoteOptionId);
        modelBuilder.Entity<QuoteOptionExternalDiamondLink>().HasIndex(x => x.ExternalDiamondId);
        modelBuilder.Entity<QuoteOptionExternalDiamondLink>().HasIndex(x => x.LinkStatus);
    }

    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries<JewelleryBusinessManager.Models.BaseEntity>();
        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
                entry.Entity.CreatedAt = DateTime.Now;
            if (entry.State == EntityState.Added || entry.State == EntityState.Modified)
                entry.Entity.UpdatedAt = DateTime.Now;
        }
    }
}
