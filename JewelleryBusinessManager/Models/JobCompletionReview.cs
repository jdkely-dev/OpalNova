namespace JewelleryBusinessManager.Models;

public sealed class JobCompletionReview
{
    public int JobId { get; init; }
    public string JobTitle { get; init; } = string.Empty;
    public string CustomerName { get; init; } = "No customer linked";
    public string QuoteCode { get; init; } = string.Empty;
    public string AcceptedOptionName { get; init; } = string.Empty;
    public decimal Total { get; init; }
    public decimal Paid { get; init; }
    public decimal Balance { get; init; }
    public List<JobCompletionMaterialLine> Materials { get; init; } = new();
    public List<JobCompletionStoneLine> Stones { get; init; } = new();
    public string Notes { get; init; } = string.Empty;

    public bool HasReservedMaterials => Materials.Any(x => x.IsReserved);
    public bool HasReservedStones => Stones.Any(x => x.IsReserved);
    public int ReservedMaterialCount => Materials.Count(x => x.IsReserved);
    public int ReservedStoneCount => Stones.Count(x => x.IsReserved);
    public string QuoteLine => string.IsNullOrWhiteSpace(QuoteCode)
        ? "No linked accepted quote found"
        : $"{QuoteCode} - {AcceptedOptionName}".Trim(' ', '-');
}

public sealed class JobCompletionMaterialLine
{
    public int LinkId { get; init; }
    public int MaterialId { get; init; }
    public string MaterialCode { get; init; } = string.Empty;
    public string MaterialName { get; init; } = string.Empty;
    public decimal Quantity { get; init; }
    public string UnitType { get; init; } = string.Empty;
    public decimal CurrentQuantity { get; init; }
    public string ReservationStatus { get; init; } = string.Empty;
    public bool MaterialExists { get; init; }

    public bool IsReserved => ReservationStatus.Equals("Reserved", StringComparison.OrdinalIgnoreCase);
    public bool WillGoNegative => IsReserved && MaterialExists && CurrentQuantity - Quantity < 0m;
    public string QuantityLine => $"{Quantity:0.###} {UnitType}".Trim();
    public string CurrentLine => MaterialExists ? $"{CurrentQuantity:0.###} {UnitType}".Trim() : "Missing material";
    public string ResultLine => IsReserved
        ? WillGoNegative ? "Will go negative" : "Ready to consume"
        : ReservationStatus;
}

public sealed class JobCompletionStoneLine
{
    public int LinkId { get; init; }
    public int StoneId { get; init; }
    public string StoneCode { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string CurrentStatus { get; init; } = string.Empty;
    public string ReservationStatus { get; init; } = string.Empty;
    public bool StoneExists { get; init; }

    public bool IsReserved => ReservationStatus.Equals("Reserved", StringComparison.OrdinalIgnoreCase);
    public string ResultLine => IsReserved
        ? StoneExists ? "Ready to mark set" : "Missing stone"
        : ReservationStatus;
}

public sealed class JobCompletionOptions
{
    public bool ConsumeReservedMaterials { get; init; } = true;
    public bool MarkReservedStonesSet { get; init; } = true;
    public bool ReleaseUnconsumedReservations { get; init; } = true;
    public bool AllowNegativeMaterialStock { get; init; }
    public bool AllowOutstandingBalance { get; init; }
    public string CompletionNote { get; init; } = string.Empty;
}

public sealed class JobCompletionResult
{
    public int ConsumedMaterialLines { get; init; }
    public int MarkedStoneLines { get; init; }
    public int ReleasedReservationLines { get; init; }
    public string Summary => $"Completed job. Materials consumed: {ConsumedMaterialLines}. Stones set: {MarkedStoneLines}. Reservations released: {ReleasedReservationLines}.";
}
