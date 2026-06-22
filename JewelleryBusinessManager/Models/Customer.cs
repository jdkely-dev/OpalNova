namespace JewelleryBusinessManager.Models;

public class Customer : BaseEntity
{
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? InstagramHandle { get; set; }
    public string? Address { get; set; }
    public string? RingSizes { get; set; }
    public string? PreferredMetals { get; set; }
    public string? PreferredStones { get; set; }
    public string? Notes { get; set; }
    public override string ToString() => string.IsNullOrWhiteSpace(FullName) ? $"Customer #{Id}" : FullName;
}
