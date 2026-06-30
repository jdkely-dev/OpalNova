using JewelleryBusinessManager.Models;

namespace JewelleryBusinessManager.Services;

public static class StockLifecycleService
{
    public static string DescribeStockStatus(StockStatus status) => status switch
    {
        StockStatus.InStock => "Owned stock - available to sell, reserve or use.",
        StockStatus.Reserved => "Reserved stock - committed to a customer, quote or job until released or completed.",
        StockStatus.Sold => "Sold stock - no longer available for new quotes or markets.",
        StockStatus.AtMarket => "Owned stock - packed for a market or selling event.",
        StockStatus.ListedOnline => "Owned stock - currently listed online.",
        StockStatus.NeedsPhotos => "Owned stock - not ready for listing until photos are complete.",
        StockStatus.InProgress => "Work in progress - not yet finished stock.",
        _ => "Stock status needs review."
    };

    public static string DescribeStoneStatus(StoneStatus status) => status switch
    {
        StoneStatus.Rough => "Owned stone - rough or uncut, not ready to set.",
        StoneStatus.Loose => "Owned stone - loose and available.",
        StoneStatus.Polished => "Owned stone - polished and available.",
        StoneStatus.Cutting => "Work in progress - currently being cut or finished.",
        StoneStatus.SelectedForDesign => "Reserved/design stock - selected for a proposed design.",
        StoneStatus.Reserved => "Reserved stone - committed to a customer, quote or job.",
        StoneStatus.AssignedToJewellery => "Work in progress - assigned to a jewellery piece.",
        StoneStatus.SetInJewellery => "Consumed/set stone - already used in finished jewellery.",
        StoneStatus.Sold => "Sold stone - no longer available.",
        _ => "Stone status needs review."
    };

    public static string DescribeReservationStatus(string? status)
    {
        var normalized = (status ?? string.Empty).Trim();
        return normalized.ToLowerInvariant() switch
        {
            "reserved" => "Reserved inventory - committed to an accepted quote option.",
            "proposed" => "Proposed allocation - not yet committed.",
            "consumed" => "Consumed inventory - used when the job was completed.",
            "released" => "Released reservation - no longer committed to this quote option.",
            "converted to owned inventory" => "Converted supplier stock - now linked to owned inventory.",
            "" => "No reservation status recorded.",
            _ => $"{normalized} - review the linked quote or supplier workflow for context."
        };
    }

    public static string DescribeExternalDiamondStatus(string? status)
    {
        var normalized = (status ?? string.Empty).Trim();
        return normalized.ToLowerInvariant() switch
        {
            "saved" or "search result" => "Supplier stock - saved from search, not held or owned.",
            "hold requested" or "hold confirmed" or "hold expiring" => "Supplier stock - held or waiting on hold confirmation.",
            "order requested" or "ordered" => "Supplier stock - ordered but not yet received.",
            "received" => "Received supplier stock - physically received, not yet converted to owned stone inventory.",
            "converted to owned inventory" => "Owned stock - converted into a local stone record.",
            "released" or "unavailable" => "Supplier stock - not available for this quote path.",
            "" => "Supplier stock - status not recorded.",
            _ => $"Supplier stock - {normalized}."
        };
    }

    public static string DescribeRecord(object record) => record switch
    {
        JewelleryItem item => DescribeStockStatus(item.Status),
        Stone stone => DescribeStoneStatus(stone.Status),
        ExternalDiamond diamond => DescribeExternalDiamondStatus(diamond.Status),
        QuoteOptionStoneLink link => DescribeReservationStatus(link.ReservationStatus),
        QuoteOptionMaterialLink link => DescribeReservationStatus(link.ReservationStatus),
        _ => "Lifecycle guidance is not available for this record type."
    };

    public static IReadOnlyList<(string Label, string Guidance)> SummaryRows => new[]
    {
        ("Owned stock", "Physical stock recorded in OPALNOVA and available unless marked reserved, sold, set or consumed."),
        ("Reserved stock", "Stock committed to an accepted quote or job. Release it when the quote is cancelled or redesigned."),
        ("Supplier stock", "External diamonds or supplier items not owned until received and converted or entered as owned stock."),
        ("Sold stock", "Finished stock or stones sold to a customer and unavailable for new work."),
        ("Consumed stock", "Materials or stones used in a completed job, usually through the job completion checklist."),
        ("Archived / inactive", "Records kept for history but not intended for daily selling, quoting or production.")
    };
}
