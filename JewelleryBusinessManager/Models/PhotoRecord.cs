namespace JewelleryBusinessManager.Models;

public class PhotoRecord : BaseEntity
{
    public string FilePath { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public int EntityId { get; set; }
    public string? Caption { get; set; }
}
