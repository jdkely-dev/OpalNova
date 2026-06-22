using Microsoft.EntityFrameworkCore;
using JewelleryBusinessManager.Data;
using JewelleryBusinessManager.Models;

namespace JewelleryBusinessManager.Services;

public static class ExternalDiamondInventoryService
{
    private const string ConvertedStatus = "Converted To Owned Inventory";

    public static ExternalDiamondInventoryConversionResult ConvertReceivedDiamondToOwnedStone(int externalDiamondId)
    {
        using var db = new AppDbContext();
        using var transaction = db.Database.BeginTransaction();

        var diamond = db.ExternalDiamonds.FirstOrDefault(x => x.Id == externalDiamondId)
            ?? throw new InvalidOperationException("The selected external diamond could not be found.");

        if (!IsReceivedOrConverted(diamond))
            throw new InvalidOperationException("Mark the supplier diamond as received before converting it into owned inventory.");

        var marker = BuildMarker(diamond.Id);
        var existing = FindExistingOwnedStone(db, diamond, marker);
        if (existing != null)
        {
            diamond.Status = ConvertedStatus;
            diamond.ReceivedAt ??= DateTime.Now;
            AppendDiamondNote(diamond, $"Existing owned stone linked: {existing.StoneCode}");
            UpdateLinkedQuoteDiamondStatuses(db, diamond.Id, ConvertedStatus);
            db.SaveChanges();
            transaction.Commit();
            return new ExternalDiamondInventoryConversionResult
            {
                ExternalDiamondId = diamond.Id,
                StoneId = existing.Id,
                StoneCode = existing.StoneCode,
                CreatedStone = false
            };
        }

        var stone = new Stone
        {
            StoneCode = string.Empty,
            StoneType = diamond.IsLabGrown ? "Lab-grown diamond" : "Natural diamond",
            WeightCarats = diamond.Carat,
            Shape = diamond.Shape,
            BodyTone = diamond.Color,
            Brightness = diamond.Clarity,
            Pattern = diamond.Cut,
            BaseColour = diamond.Color,
            MainColours = diamond.Color,
            EstimatedValue = diamond.EstimatedRetailPrice > 0m ? diamond.EstimatedRetailPrice : diamond.SupplierPrice,
            Status = StoneStatus.Loose,
            Notes = BuildStoneNotes(diamond, marker)
        };

        db.Stones.Add(stone);
        db.SaveChanges();
        stone.StoneCode = $"STN-{stone.Id:0000}";

        diamond.Status = ConvertedStatus;
        diamond.ReceivedAt ??= DateTime.Now;
        AppendDiamondNote(diamond, $"Converted to owned stone {stone.StoneCode}");
        UpdateLinkedQuoteDiamondStatuses(db, diamond.Id, ConvertedStatus);
        db.SaveChanges();
        transaction.Commit();

        return new ExternalDiamondInventoryConversionResult
        {
            ExternalDiamondId = diamond.Id,
            StoneId = stone.Id,
            StoneCode = stone.StoneCode,
            CreatedStone = true
        };
    }

    public static string FindOwnedStoneCode(AppDbContext db, ExternalDiamond diamond)
    {
        var marker = BuildMarker(diamond.Id);
        return FindExistingOwnedStone(db, diamond, marker)?.StoneCode ?? string.Empty;
    }

    private static bool IsReceivedOrConverted(ExternalDiamond diamond)
    {
        return diamond.ReceivedAt.HasValue ||
               diamond.Status.Equals("Received", StringComparison.OrdinalIgnoreCase) ||
               diamond.Status.Equals(ConvertedStatus, StringComparison.OrdinalIgnoreCase);
    }

    private static Stone? FindExistingOwnedStone(AppDbContext db, ExternalDiamond diamond, string marker)
    {
        var certificate = diamond.CertificateNumber?.Trim() ?? string.Empty;
        return db.Stones.AsNoTracking()
            .AsEnumerable()
            .FirstOrDefault(stone =>
            {
                var notes = stone.Notes ?? string.Empty;
                if (notes.Contains(marker, StringComparison.OrdinalIgnoreCase))
                    return true;

                return !string.IsNullOrWhiteSpace(certificate) &&
                       notes.Contains("External diamond certificate:", StringComparison.OrdinalIgnoreCase) &&
                       notes.Contains(certificate, StringComparison.OrdinalIgnoreCase);
            });
    }

    private static void UpdateLinkedQuoteDiamondStatuses(AppDbContext db, int externalDiamondId, string status)
    {
        foreach (var link in db.QuoteOptionExternalDiamondLinks.Where(x => x.ExternalDiamondId == externalDiamondId))
        {
            link.LinkStatus = status;
            link.UpdatedAt = DateTime.Now;
        }
    }

    private static string BuildMarker(int externalDiamondId) => $"ExternalDiamondId:{externalDiamondId}";

    private static string BuildStoneNotes(ExternalDiamond diamond, string marker)
    {
        var lines = new[]
        {
            "Converted from received external supplier diamond.",
            marker,
            $"Source: {diamond.SourceSystem}",
            $"Supplier diamond ID: {diamond.SupplierDiamondId}",
            $"External diamond certificate: {diamond.CertificateNumber}",
            $"Lab: {diamond.Lab}",
            $"Colour: {diamond.Color}",
            $"Clarity: {diamond.Clarity}",
            $"Cut: {diamond.Cut}",
            $"Supplier price: {diamond.SupplierPrice:0.##} {diamond.Currency}",
            $"Estimated retail: {diamond.EstimatedRetailPrice:0.##}",
            $"Video: {diamond.VideoUrl}",
            $"Certificate URL: {diamond.CertificateUrl}",
            $"Converted: {DateTime.Now:g}"
        };

        return string.Join(Environment.NewLine, lines.Where(x => !string.IsNullOrWhiteSpace(x) && !x.EndsWith(": ", StringComparison.Ordinal)));
    }

    private static void AppendDiamondNote(ExternalDiamond diamond, string action)
    {
        var stamped = $"[{DateTime.Now:g}] {action}";
        diamond.Notes = string.IsNullOrWhiteSpace(diamond.Notes)
            ? stamped
            : diamond.Notes + Environment.NewLine + stamped;
    }
}
