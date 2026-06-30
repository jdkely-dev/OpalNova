using System.Globalization;
using System.Text;
using JewelleryBusinessManager.Models;

namespace JewelleryBusinessManager.Services;

public sealed record PaymentScheduleLine(
    string Stage,
    decimal TargetAmount,
    decimal PaidAmount,
    decimal RemainingAmount,
    string DueText,
    string Status,
    string Note);

public sealed record PaymentScheduleSummary(
    decimal TotalAmount,
    decimal DepositPercent,
    decimal DepositTarget,
    decimal PaidAmount,
    decimal BalanceRemaining,
    string Guidance,
    IReadOnlyList<PaymentScheduleLine> Lines);

public static class PaymentScheduleService
{
    private const decimal DefaultDepositPercent = 30m;

    public static PaymentScheduleSummary BuildForQuote(CustomQuote quote, QuoteOption option)
    {
        var total = Math.Max(0m, option.TotalPrice);
        var depositPercent = NormalizeDepositPercent(quote.DepositPercent);
        var requiredBy = quote.RequiredBy ?? quote.ValidUntil;
        return Build(total, 0m, depositPercent, "On acceptance / before production", BuildFinalDueText(requiredBy), null);
    }

    public static PaymentScheduleSummary BuildForJob(Job job, IEnumerable<Payment> payments, CustomQuote? linkedQuote = null)
    {
        var total = Math.Max(0m, job.FinalPrice > 0m ? job.FinalPrice : job.QuoteAmount);
        var depositPercent = NormalizeDepositPercent(linkedQuote?.DepositPercent ?? DefaultDepositPercent);
        var paymentTotal = payments.Where(x => x.JobId == job.Id).Sum(x => x.Amount);
        var paid = Math.Max(job.DepositPaid, paymentTotal);
        return Build(total, paid, depositPercent, "Before production starts", BuildFinalDueText(job.DueDate), job.Status);
    }

    public static string BuildCompactText(PaymentScheduleSummary schedule)
    {
        if (schedule.TotalAmount <= 0m)
            return "Payment schedule: add a quote or final price to calculate staged payments.";

        var finalTarget = Math.Max(0m, schedule.TotalAmount - schedule.DepositTarget);
        return $"Deposit {schedule.DepositPercent:0.##}%: {Money(schedule.DepositTarget)} | Final balance: {Money(finalTarget)} | Remaining now: {Money(schedule.BalanceRemaining)}";
    }

    public static string BuildPlainText(PaymentScheduleSummary schedule)
    {
        var sb = new StringBuilder();
        sb.AppendLine(BuildCompactText(schedule));
        foreach (var line in schedule.Lines)
            sb.AppendLine($"{line.Stage}: target {Money(line.TargetAmount)}, paid {Money(line.PaidAmount)}, remaining {Money(line.RemainingAmount)}. {line.DueText}. {line.Status}.");
        sb.Append(schedule.Guidance);
        return sb.ToString();
    }

    private static PaymentScheduleSummary Build(decimal total, decimal paid, decimal depositPercent, string depositDueText, string finalDueText, JobStatus? jobStatus)
    {
        if (total <= 0m)
        {
            var line = new PaymentScheduleLine("Price required", 0m, 0m, 0m, "Set price first", "Waiting", "Add a quote or final price before collecting scheduled payments.");
            return new PaymentScheduleSummary(0m, depositPercent, 0m, paid, 0m, line.Note, new[] { line });
        }

        var depositTarget = decimal.Round(total * depositPercent / 100m, 2);
        var balanceTarget = Math.Max(0m, total - depositTarget);
        var paidClamped = Math.Max(0m, paid);
        var depositPaid = Math.Min(paidClamped, depositTarget);
        var finalPaid = Math.Min(Math.Max(0m, paidClamped - depositTarget), balanceTarget);
        var depositRemaining = Math.Max(0m, depositTarget - depositPaid);
        var finalRemaining = Math.Max(0m, balanceTarget - finalPaid);
        var balanceRemaining = Math.Max(0m, total - paidClamped);

        var depositStatus = depositRemaining <= 0m ? "Complete" : depositPaid > 0m ? "Part paid" : "Due";
        var finalStatus = finalRemaining <= 0m ? "Complete" : finalPaid > 0m ? "Part paid" : "Pending";

        var lines = new[]
        {
            new PaymentScheduleLine("Deposit", depositTarget, depositPaid, depositRemaining, depositDueText, depositStatus, "Confirms the job can move into production."),
            new PaymentScheduleLine("Final balance", balanceTarget, finalPaid, finalRemaining, finalDueText, finalStatus, "Clear before pickup, shipping or final handover.")
        };

        var guidance = BuildGuidance(depositRemaining, balanceRemaining, jobStatus);
        return new PaymentScheduleSummary(total, depositPercent, depositTarget, paidClamped, balanceRemaining, guidance, lines);
    }

    private static string BuildGuidance(decimal depositRemaining, decimal balanceRemaining, JobStatus? jobStatus)
    {
        if (balanceRemaining <= 0m)
            return "Payment schedule complete. No balance is currently owing.";

        if (depositRemaining > 0m)
            return "Deposit is still required before this job should move deeper into production.";

        return jobStatus switch
        {
            JobStatus.ReadyForPickup or JobStatus.ReadyToShip or JobStatus.QualityCheck => "Deposit is covered. Final balance should be cleared before handover.",
            JobStatus.Completed => "Job is complete but still shows a balance. Review payments before closing the customer file.",
            _ => "Deposit is covered. Track the remaining balance against the due or handover date."
        };
    }

    private static string BuildFinalDueText(DateTime? dueDate) => dueDate.HasValue
        ? $"Before handover / due {dueDate.Value:dd MMM yyyy}"
        : "Before pickup, shipping or handover";

    private static decimal NormalizeDepositPercent(decimal depositPercent) => depositPercent <= 0m ? DefaultDepositPercent : depositPercent;

    private static string Money(decimal amount) => amount.ToString("C", CultureInfo.CurrentCulture);
}
