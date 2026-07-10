namespace OakIdeas.Aspire.DataExplorer.Sample.Api.Data;

public sealed class WarehouseSupplier
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public bool IsPreferred { get; set; }

    public ICollection<WarehouseInventoryItem> InventoryItems { get; set; } = [];
}

public sealed class WarehouseLocation
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string RegionCode { get; set; } = string.Empty;
    public bool IsClimateControlled { get; set; }

    public ICollection<WarehouseStockBin> StockBins { get; set; } = [];
}

public sealed class WarehouseInventoryItem
{
    public int Id { get; set; }
    public int SupplierId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int ReorderLevel { get; set; }
    public decimal StandardCost { get; set; }
    public DateTimeOffset LastRestockedAt { get; set; }

    public WarehouseSupplier Supplier { get; set; } = null!;
    public ICollection<WarehouseStockBin> StockBins { get; set; } = [];
}

public sealed class WarehouseStockBin
{
    public int Id { get; set; }
    public int InventoryItemId { get; set; }
    public int WarehouseLocationId { get; set; }
    public string BinCode { get; set; } = string.Empty;
    public int QuantityOnHand { get; set; }
    public DateTimeOffset LastCountedAt { get; set; }

    public WarehouseInventoryItem InventoryItem { get; set; } = null!;
    public WarehouseLocation WarehouseLocation { get; set; } = null!;
}
