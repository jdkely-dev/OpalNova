namespace JewelleryBusinessManager.Models;

public class Material : BaseEntity
{
    public string MaterialCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public MaterialCategory Category { get; set; } = MaterialCategory.Other;
    public int? SupplierId { get; set; }
    public decimal PurchaseCost { get; set; }
    public decimal CurrentQuantity { get; set; }
    public UnitType UnitType { get; set; } = UnitType.Pieces;
    public decimal ReorderLevel { get; set; }
    public string? StorageLocation { get; set; }
    public string? Notes { get; set; }
    public override string ToString() => string.IsNullOrWhiteSpace(Name) ? $"Material #{Id}" : $"{MaterialCode} {Name}".Trim();
}
