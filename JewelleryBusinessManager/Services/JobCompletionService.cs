using Microsoft.EntityFrameworkCore;
using JewelleryBusinessManager.Data;
using JewelleryBusinessManager.Models;

namespace JewelleryBusinessManager.Services;

public static class JobCompletionService
{
    public static JobCompletionReview BuildReview(int jobId)
    {
        using var db = new AppDbContext();
        return BuildReview(db, jobId);
    }

    public static JobCompletionReview BuildReview(AppDbContext db, int jobId)
    {
        var job = db.Jobs.AsNoTracking().FirstOrDefault(x => x.Id == jobId)
            ?? throw new InvalidOperationException("The selected job could not be found.");
        var customerName = job.CustomerId.HasValue
            ? db.Customers.AsNoTracking().FirstOrDefault(x => x.Id == job.CustomerId.Value)?.FullName ?? "No customer linked"
            : "No customer linked";

        var total = GetJobTotal(job);
        var paymentTotal = db.Payments.AsNoTracking()
            .Where(x => x.JobId == job.Id)
            .Select(x => x.Amount)
            .ToList()
            .Sum();
        var paid = Math.Max(job.DepositPaid, paymentTotal);
        var balance = Math.Max(Math.Max(0, total - paid), Math.Max(0, job.BalanceOwing));
        var quote = db.CustomQuotes.AsNoTracking()
            .Where(x => x.LinkedJobId == job.Id)
            .OrderByDescending(x => x.UpdatedAt)
            .FirstOrDefault();
        var option = FindCompletionOption(db, quote);

        var materialLines = new List<JobCompletionMaterialLine>();
        var stoneLines = new List<JobCompletionStoneLine>();

        if (option != null)
        {
            var materialLinks = db.QuoteOptionMaterialLinks.AsNoTracking()
                .Where(x => x.QuoteOptionId == option.Id)
                .OrderBy(x => x.MaterialNameSnapshot)
                .ToList();
            var materials = db.Materials.AsNoTracking()
                .Where(x => materialLinks.Select(l => l.MaterialId).Contains(x.Id))
                .ToDictionary(x => x.Id);
            materialLines.AddRange(materialLinks.Select(link =>
            {
                materials.TryGetValue(link.MaterialId, out var material);
                return new JobCompletionMaterialLine
                {
                    LinkId = link.Id,
                    MaterialId = link.MaterialId,
                    MaterialCode = string.IsNullOrWhiteSpace(link.MaterialCodeSnapshot) ? material?.MaterialCode ?? string.Empty : link.MaterialCodeSnapshot,
                    MaterialName = string.IsNullOrWhiteSpace(link.MaterialNameSnapshot) ? material?.Name ?? "Missing material" : link.MaterialNameSnapshot,
                    Quantity = link.Quantity,
                    UnitType = string.IsNullOrWhiteSpace(link.UnitTypeSnapshot) ? material?.UnitType.ToString() ?? string.Empty : link.UnitTypeSnapshot,
                    CurrentQuantity = material?.CurrentQuantity ?? 0m,
                    ReservationStatus = link.ReservationStatus,
                    MaterialExists = material != null
                };
            }));

            var stoneLinks = db.QuoteOptionStoneLinks.AsNoTracking()
                .Where(x => x.QuoteOptionId == option.Id)
                .OrderBy(x => x.StoneCodeSnapshot)
                .ToList();
            var stones = db.Stones.AsNoTracking()
                .Where(x => stoneLinks.Select(l => l.StoneId).Contains(x.Id))
                .ToDictionary(x => x.Id);
            stoneLines.AddRange(stoneLinks.Select(link =>
            {
                stones.TryGetValue(link.StoneId, out var stone);
                return new JobCompletionStoneLine
                {
                    LinkId = link.Id,
                    StoneId = link.StoneId,
                    StoneCode = string.IsNullOrWhiteSpace(link.StoneCodeSnapshot) ? stone?.StoneCode ?? string.Empty : link.StoneCodeSnapshot,
                    Description = string.IsNullOrWhiteSpace(link.DescriptionSnapshot) ? stone?.ToString() ?? "Missing stone" : link.DescriptionSnapshot,
                    CurrentStatus = stone?.Status.ToString() ?? "Missing stone",
                    ReservationStatus = link.ReservationStatus,
                    StoneExists = stone != null
                };
            }));
        }

        var notes = option == null
            ? "No accepted quote option with reservations was found for this job. Completion can still be recorded, but no stock will be consumed automatically."
            : string.Empty;

        return new JobCompletionReview
        {
            JobId = job.Id,
            JobTitle = $"{job.JobCode} {job.JobTitle}".Trim(),
            CustomerName = customerName,
            QuoteCode = quote?.QuoteCode ?? string.Empty,
            AcceptedOptionName = option?.OptionName ?? string.Empty,
            Total = total,
            Paid = paid,
            Balance = balance,
            Materials = materialLines,
            Stones = stoneLines,
            Notes = notes
        };
    }

    public static JobCompletionResult CompleteJob(int jobId, JobCompletionOptions options)
    {
        using var db = new AppDbContext();
        using var transaction = db.Database.BeginTransaction();

        var job = db.Jobs.FirstOrDefault(x => x.Id == jobId)
            ?? throw new InvalidOperationException("The selected job could not be found.");
        var total = GetJobTotal(job);
        var paymentTotal = db.Payments.AsNoTracking()
            .Where(x => x.JobId == job.Id)
            .Select(x => x.Amount)
            .ToList()
            .Sum();
        var paid = Math.Max(job.DepositPaid, paymentTotal);
        var balance = Math.Max(Math.Max(0, total - paid), Math.Max(0, job.BalanceOwing));
        if (balance > 0m && !options.AllowOutstandingBalance)
            throw new InvalidOperationException($"This job still has a balance of {balance:C}. Tick the outstanding-balance confirmation before completing it.");

        var quote = db.CustomQuotes
            .Where(x => x.LinkedJobId == job.Id)
            .OrderByDescending(x => x.UpdatedAt)
            .FirstOrDefault();
        var option = FindCompletionOption(db, quote);

        var consumedMaterials = 0;
        var markedStones = 0;
        var releasedReservations = 0;

        if (option != null)
        {
            var reservedMaterialLinks = db.QuoteOptionMaterialLinks
                .Where(x => x.QuoteOptionId == option.Id && x.ReservationStatus == "Reserved")
                .OrderBy(x => x.Id)
                .ToList();
            foreach (var link in reservedMaterialLinks)
            {
                if (options.ConsumeReservedMaterials)
                {
                    var material = db.Materials.FirstOrDefault(x => x.Id == link.MaterialId)
                        ?? throw new InvalidOperationException($"Material {link.MaterialNameSnapshot} no longer exists.");
                    var newQuantity = material.CurrentQuantity - link.Quantity;
                    if (newQuantity < 0m && !options.AllowNegativeMaterialStock)
                        throw new InvalidOperationException($"{material.Name} would go below zero ({newQuantity:0.###} {material.UnitType}). Tick the negative-stock confirmation before completing.");

                    material.CurrentQuantity = newQuantity;
                    db.MaterialTransactions.Add(new MaterialTransaction
                    {
                        MaterialId = material.Id,
                        TransactionDate = DateTime.Today,
                        QuantityChange = -link.Quantity,
                        Reason = "Consumed for completed job",
                        JobId = job.Id,
                        Notes = $"V1.52 job completion consumption from quote option {option.OptionName}; reservation link #{link.Id}."
                    });
                    link.ReservationStatus = "Consumed";
                    consumedMaterials++;
                }
                else if (options.ReleaseUnconsumedReservations)
                {
                    link.ReservationStatus = "Released";
                    releasedReservations++;
                }
            }

            var reservedStoneLinks = db.QuoteOptionStoneLinks
                .Where(x => x.QuoteOptionId == option.Id && x.ReservationStatus == "Reserved")
                .OrderBy(x => x.Id)
                .ToList();
            foreach (var link in reservedStoneLinks)
            {
                if (options.MarkReservedStonesSet)
                {
                    var stone = db.Stones.FirstOrDefault(x => x.Id == link.StoneId)
                        ?? throw new InvalidOperationException($"Stone {link.StoneCodeSnapshot} no longer exists.");
                    stone.Status = StoneStatus.SetInJewellery;
                    link.ReservationStatus = "Consumed";
                    markedStones++;
                }
                else if (options.ReleaseUnconsumedReservations)
                {
                    link.ReservationStatus = "Released";
                    releasedReservations++;
                }
            }
        }

        job.Status = JobStatus.Completed;
        job.BalanceOwing = balance;
        AppendCompletionNote(job, options.CompletionNote, consumedMaterials, markedStones, releasedReservations);
        db.SaveChanges();
        transaction.Commit();

        return new JobCompletionResult
        {
            ConsumedMaterialLines = consumedMaterials,
            MarkedStoneLines = markedStones,
            ReleasedReservationLines = releasedReservations
        };
    }

    private static QuoteOption? FindCompletionOption(AppDbContext db, CustomQuote? quote)
    {
        if (quote == null)
            return null;

        if (quote.AcceptedOptionId.HasValue)
            return db.QuoteOptions.FirstOrDefault(x => x.Id == quote.AcceptedOptionId.Value);

        var optionIds = db.QuoteOptions.AsNoTracking()
            .Where(x => x.CustomQuoteId == quote.Id)
            .Select(x => x.Id)
            .ToList();
        var reservedOptionId = db.QuoteOptionMaterialLinks.AsNoTracking()
            .Where(x => optionIds.Contains(x.QuoteOptionId) && x.ReservationStatus == "Reserved")
            .Select(x => (int?)x.QuoteOptionId)
            .FirstOrDefault()
            ?? db.QuoteOptionStoneLinks.AsNoTracking()
                .Where(x => optionIds.Contains(x.QuoteOptionId) && x.ReservationStatus == "Reserved")
                .Select(x => (int?)x.QuoteOptionId)
                .FirstOrDefault();

        return reservedOptionId.HasValue ? db.QuoteOptions.FirstOrDefault(x => x.Id == reservedOptionId.Value) : null;
    }

    private static decimal GetJobTotal(Job job) => job.FinalPrice > 0 ? job.FinalPrice : job.QuoteAmount;

    private static void AppendCompletionNote(Job job, string note, int consumedMaterials, int markedStones, int releasedReservations)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(note))
            parts.Add(note.Trim());
        parts.Add($"V1.52 completion checklist: consumed {consumedMaterials} material line(s), marked {markedStones} stone line(s) set, released {releasedReservations} reservation line(s).");
        var stamped = $"[{DateTime.Now:g}] {string.Join(" ", parts)}";
        job.InternalNotes = string.IsNullOrWhiteSpace(job.InternalNotes) ? stamped : job.InternalNotes + Environment.NewLine + stamped;
    }
}
