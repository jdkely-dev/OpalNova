namespace JewelleryBusinessManager.Models;

public sealed class ExternalDiamondInventoryConversionResult
{
    public int ExternalDiamondId { get; init; }
    public int StoneId { get; init; }
    public string StoneCode { get; init; } = string.Empty;
    public bool CreatedStone { get; init; }
    public string Message => CreatedStone
        ? $"Created owned stone {StoneCode} from received supplier diamond."
        : $"Supplier diamond is already linked to owned stone {StoneCode}.";
}
