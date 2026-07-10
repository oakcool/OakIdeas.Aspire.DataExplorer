using Microsoft.EntityFrameworkCore;

namespace OakIdeas.Aspire.DataExplorer.Sample.Api.Data;

public sealed class WarehouseDbContext(DbContextOptions<WarehouseDbContext> options) : DbContext(options)
{
    public DbSet<WarehouseSupplier> Suppliers => Set<WarehouseSupplier>();
    public DbSet<WarehouseLocation> WarehouseLocations => Set<WarehouseLocation>();
    public DbSet<WarehouseInventoryItem> InventoryItems => Set<WarehouseInventoryItem>();
    public DbSet<WarehouseStockBin> StockBins => Set<WarehouseStockBin>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WarehouseSupplier>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(120).IsRequired();
            entity.Property(e => e.ContactEmail).HasMaxLength(200).IsRequired();
            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasIndex(e => e.ContactEmail).IsUnique();
            entity.HasData(
                new WarehouseSupplier { Id = 1, Name = "Northwind Storage", ContactEmail = "ops@northwind.example", IsPreferred = true },
                new WarehouseSupplier { Id = 2, Name = "Blue Yonder Parts", ContactEmail = "inventory@blueyonder.example", IsPreferred = false });
        });

        modelBuilder.Entity<WarehouseLocation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(120).IsRequired();
            entity.Property(e => e.RegionCode).HasMaxLength(20).IsRequired();
            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasData(
                new WarehouseLocation { Id = 1, Name = "Seattle Fulfillment", RegionCode = "US-WEST", IsClimateControlled = true },
                new WarehouseLocation { Id = 2, Name = "Austin Overflow", RegionCode = "US-CENTRAL", IsClimateControlled = false });
        });

        modelBuilder.Entity<WarehouseInventoryItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Sku).HasMaxLength(40).IsRequired();
            entity.Property(e => e.Name).HasMaxLength(160).IsRequired();
            entity.Property(e => e.StandardCost).HasColumnType("decimal(10,2)");
            entity.HasIndex(e => e.Sku).IsUnique();
            entity.HasIndex(e => new { e.SupplierId, e.Name });

            entity.HasOne(e => e.Supplier)
                .WithMany(e => e.InventoryItems)
                .HasForeignKey(e => e.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.ToTable(t =>
            {
                t.HasCheckConstraint("CK_WarehouseInventoryItems_ReorderLevel", "[ReorderLevel] >= 0");
                t.HasCheckConstraint("CK_WarehouseInventoryItems_StandardCost", "[StandardCost] >= 0");
            });

            entity.HasData(
                new WarehouseInventoryItem
                {
                    Id = 1,
                    SupplierId = 1,
                    Sku = "BIN-STACK-001",
                    Name = "Stackable storage bin",
                    ReorderLevel = 40,
                    StandardCost = 14.50m,
                    LastRestockedAt = new DateTimeOffset(2026, 2, 4, 14, 0, 0, TimeSpan.Zero)
                },
                new WarehouseInventoryItem
                {
                    Id = 2,
                    SupplierId = 1,
                    Sku = "SCAN-HAND-002",
                    Name = "Handheld barcode scanner",
                    ReorderLevel = 8,
                    StandardCost = 89.00m,
                    LastRestockedAt = new DateTimeOffset(2026, 2, 7, 9, 30, 0, TimeSpan.Zero)
                },
                new WarehouseInventoryItem
                {
                    Id = 3,
                    SupplierId = 2,
                    Sku = "LABEL-THERM-003",
                    Name = "Thermal label refill pack",
                    ReorderLevel = 25,
                    StandardCost = 32.75m,
                    LastRestockedAt = new DateTimeOffset(2026, 2, 2, 11, 15, 0, TimeSpan.Zero)
                });
        });

        modelBuilder.Entity<WarehouseStockBin>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.BinCode).HasMaxLength(32).IsRequired();
            entity.HasIndex(e => new { e.WarehouseLocationId, e.BinCode }).IsUnique();
            entity.HasIndex(e => new { e.InventoryItemId, e.WarehouseLocationId });

            entity.HasOne(e => e.InventoryItem)
                .WithMany(e => e.StockBins)
                .HasForeignKey(e => e.InventoryItemId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.WarehouseLocation)
                .WithMany(e => e.StockBins)
                .HasForeignKey(e => e.WarehouseLocationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.ToTable(t => t.HasCheckConstraint("CK_WarehouseStockBins_QuantityOnHand", "[QuantityOnHand] >= 0"));

            entity.HasData(
                new WarehouseStockBin
                {
                    Id = 1,
                    InventoryItemId = 1,
                    WarehouseLocationId = 1,
                    BinCode = "SEA-A01",
                    QuantityOnHand = 128,
                    LastCountedAt = new DateTimeOffset(2026, 2, 8, 8, 0, 0, TimeSpan.Zero)
                },
                new WarehouseStockBin
                {
                    Id = 2,
                    InventoryItemId = 2,
                    WarehouseLocationId = 1,
                    BinCode = "SEA-B07",
                    QuantityOnHand = 18,
                    LastCountedAt = new DateTimeOffset(2026, 2, 8, 8, 20, 0, TimeSpan.Zero)
                },
                new WarehouseStockBin
                {
                    Id = 3,
                    InventoryItemId = 3,
                    WarehouseLocationId = 2,
                    BinCode = "AUS-C03",
                    QuantityOnHand = 64,
                    LastCountedAt = new DateTimeOffset(2026, 2, 8, 9, 0, 0, TimeSpan.Zero)
                },
                new WarehouseStockBin
                {
                    Id = 4,
                    InventoryItemId = 1,
                    WarehouseLocationId = 2,
                    BinCode = "AUS-A02",
                    QuantityOnHand = 42,
                    LastCountedAt = new DateTimeOffset(2026, 2, 8, 9, 15, 0, TimeSpan.Zero)
                });
        });
    }
}
