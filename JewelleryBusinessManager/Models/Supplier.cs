namespace JewelleryBusinessManager.Models;

public class Supplier : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? ContactName { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }
    public string? Notes { get; set; }
    public override string ToString() => string.IsNullOrWhiteSpace(Name) ? $"Supplier #{Id}" : Name;
}
