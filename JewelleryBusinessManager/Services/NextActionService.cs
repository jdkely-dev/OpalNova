using Microsoft.EntityFrameworkCore;
using JewelleryBusinessManager.Data;
using JewelleryBusinessManager.Models;

namespace JewelleryBusinessManager.Services;

public static class NextActionService
{
    public static IReadOnlyList<NextActionItem> BuildActions(AppDbContext db, DateTime? referenceDate = null)
    {
        var today = (referenceDate ?? DateTime.Today).Date;
        var soon = today.AddDays(7);
        var now = DateTime.Now;

        var customers = db.Customers.AsNoTracking().ToDictionary(x => x.Id, x => x.FullName);
        var quotes = db.CustomQuotes.AsNoTracking().OrderByDescending(x => x.UpdatedAt).ToList();
        var options = db.QuoteOptions.AsNoTracking().ToList();
        var optionsByQuote = options.GroupBy(x => x.CustomQuoteId).ToDictionary(x => x.Key, x => x.ToList());
        var jobs = db.Jobs.AsNoTracking().OrderBy(x => x.DueDate ?? DateTime.MaxValue).ThenByDescending(x => x.UpdatedAt).ToList();
        var jobsById = jobs.ToDictionary(x => x.Id, x => x);
        var paymentsByJob = db.Payments.AsNoTracking()
            .Where(x => x.JobId.HasValue)
            .AsEnumerable()
            .GroupBy(x => x.JobId!.Value)
            .ToDictionary(x => x.Key, x => x.Sum(p => p.Amount));
        var tasks = db.BusinessTasks.AsNoTracking()
            .Where(x => x.Status != BusinessTaskStatus.Completed && x.Status != BusinessTaskStatus.Cancelled)
            .OrderBy(x => x.DueDate ?? DateTime.MaxValue)
            .ToList();
        var externalDiamonds = db.ExternalDiamonds.AsNoTracking().ToDictionary(x => x.Id, x => x);
        var diamondLinks = db.QuoteOptionExternalDiamondLinks.AsNoTracking().ToList();
        var diamondLinksByOption = diamondLinks.GroupBy(x => x.QuoteOptionId).ToDictionary(x => x.Key, x => x.ToList());
        var quoteIdByOption = options.ToDictionary(x => x.Id, x => x.CustomQuoteId);
        var quoteById = quotes.ToDictionary(x => x.Id, x => x);
        var materials = db.Materials.AsNoTracking().OrderBy(x => x.Name).ToList();

        var actions = new List<NextActionItem>();

        foreach (var quote in quotes)
        {
            optionsByQuote.TryGetValue(quote.Id, out var quoteOptions);
            var accepted = QuoteAccepted(quote);
            var linkedJob = quote.LinkedJobId.HasValue && jobsById.ContainsKey(quote.LinkedJobId.Value);
            var customer = GetCustomerName(customers, quote.CustomerId);
            var topOption = GetDisplayOption(quote, quoteOptions);
            var title = $"{customer} - {DisplayOrFallback(quote.Title, quote.QuoteCode, "Custom quote")}";
            var status = quote.Status ?? string.Empty;
            var proposalSent = ProposalSent(quote);
            var generated = quote.ProposalLastGeneratedAt.HasValue || !string.IsNullOrWhiteSpace(quote.ProposalLastPath);
            var expired = quote.ValidUntil.HasValue && quote.ValidUntil.Value.Date < today && !accepted;
            var dueSoon = quote.ValidUntil.HasValue && quote.ValidUntil.Value.Date <= soon && !accepted;
            var linkedDiamonds = GetLinkedDiamonds(quote.Id, quoteOptions, diamondLinksByOption, externalDiamonds);
            var hasDiamondRisk = linkedDiamonds.Any(DiamondNeedsAction);

            if (accepted && !linkedJob)
            {
                actions.Add(Create("Quote", "Urgent", 1, title,
                    "Accepted quote has not been converted into a production job.",
                    "Create the production job from the accepted quote.",
                    quote.ValidUntil, topOption?.TotalPrice, "Custom Quotes", "Open Quote Workflow",
                    "Customer has accepted, but production is not linked yet.", $"quote:{quote.Id}:accepted-no-job"));
            }
            else if (expired)
            {
                actions.Add(Create("Quote", "Urgent", 1, title,
                    "Quote has expired before acceptance.",
                    "Follow up, refresh pricing, or reissue the proposal before progressing.",
                    quote.ValidUntil, topOption?.TotalPrice, "Custom Quotes", "Open Quote Workflow",
                    "Expired quotes may have unsafe supplier, diamond, or metal pricing.", $"quote:{quote.Id}:expired"));
            }
            else if (hasDiamondRisk)
            {
                actions.Add(Create("Diamond", "Urgent", 1, title,
                    "A supplier diamond linked to this quote needs hold or order attention.",
                    "Open diamond holds and confirm availability before relying on this option.",
                    linkedDiamonds.Where(x => x.HoldExpiresAt.HasValue).Min(x => x.HoldExpiresAt), topOption?.TotalPrice,
                    "Diamond Holds", "Open Diamond Holds",
                    "External supplier stones can become unavailable without warning.", $"quote:{quote.Id}:diamond-risk"));
            }
            else if (proposalSent && quote.ProposalFollowUpDueAt.HasValue && quote.ProposalFollowUpDueAt.Value.Date <= today && !accepted)
            {
                var overdue = quote.ProposalFollowUpDueAt.Value.Date < today;
                actions.Add(Create("Quote", overdue ? "Urgent" : "High", overdue ? 1 : 2, title,
                    overdue ? "Sent proposal follow-up is overdue." : "Sent proposal follow-up is due today.",
                    "Contact the customer and record the next response.",
                    quote.ProposalFollowUpDueAt, topOption?.TotalPrice, "Custom Quotes", "Open Quote Workflow",
                    "A sent proposal without a follow-up can stall the sale.", $"quote:{quote.Id}:proposal-followup"));
            }
            else if (dueSoon)
            {
                actions.Add(Create("Quote", "High", 2, title,
                    "Quote validity ends soon.",
                    proposalSent ? "Follow up before the quote expires." : "Send or refresh the proposal before expiry.",
                    quote.ValidUntil, topOption?.TotalPrice, "Custom Quotes", "Open Quote Workflow",
                    string.Empty, $"quote:{quote.Id}:expires-soon"));
            }
            else if (generated && !proposalSent && !accepted)
            {
                actions.Add(Create("Quote", "High", 2, title,
                    "Proposal has been prepared but not recorded as sent.",
                    "Open the send proposal workflow and record the customer email step.",
                    quote.ProposalLastGeneratedAt, topOption?.TotalPrice, "Custom Quotes", "Open Quote Workflow",
                    string.Empty, $"quote:{quote.Id}:prepared-not-sent"));
            }
            else if (!proposalSent && !accepted && (string.IsNullOrWhiteSpace(status) || status.Equals("Draft", StringComparison.OrdinalIgnoreCase)))
            {
                actions.Add(Create("Quote", "Medium", 3, title,
                    "Draft quote is waiting for proposal completion.",
                    "Finish the quote, choose a recommended option, and prepare the proposal.",
                    quote.ValidUntil, topOption?.TotalPrice, "Custom Quotes", "Open Quote Workflow",
                    string.Empty, $"quote:{quote.Id}:draft"));
            }
        }

        foreach (var job in jobs.Where(x => x.Status != JobStatus.Completed && x.Status != JobStatus.Cancelled))
        {
            var customer = GetCustomerName(customers, job.CustomerId);
            var total = GetJobTotal(job);
            paymentsByJob.TryGetValue(job.Id, out var paidFromPayments);
            var paid = Math.Max(job.DepositPaid, paidFromPayments);
            var calculatedBalance = total > 0 ? Math.Max(0, total - paid) : 0;
            var balance = Math.Max(calculatedBalance, Math.Max(0, job.BalanceOwing));
            var title = $"{customer} - {DisplayOrFallback(job.JobTitle, job.JobCode, "Job")}";
            var overdue = job.DueDate.HasValue && job.DueDate.Value.Date < today;
            var dueSoon = job.DueDate.HasValue && job.DueDate.Value.Date <= soon;

            if ((job.Status == JobStatus.ReadyForPickup || job.Status == JobStatus.ReadyToShip) && balance > 0)
            {
                actions.Add(Create("Payment", "Urgent", 1, title,
                    "Job is ready for handover but still has a balance owing.",
                    "Open payment and collection before pickup or shipping.",
                    job.DueDate, balance, "Payments", "Open Payment Workflow",
                    "Do not complete handover before confirming payment.", $"job:{job.Id}:ready-balance"));
            }
            else if (job.Status == JobStatus.ReadyForPickup || job.Status == JobStatus.ReadyToShip)
            {
                actions.Add(Create("Production", "High", 2, title,
                    "Job is ready for customer handover.",
                    "Arrange pickup or shipping and complete the customer handover.",
                    job.DueDate, total, "Payments", "Open Payment Workflow",
                    string.Empty, $"job:{job.Id}:ready-handover"));
            }
            else if (overdue)
            {
                actions.Add(Create("Production", "Urgent", 1, title,
                    "Production job is overdue.",
                    "Update the production stage or contact the customer with a new date.",
                    job.DueDate, total, "Production", "Open Production Board",
                    "Overdue production work can damage customer confidence.", $"job:{job.Id}:overdue"));
            }
            else if (dueSoon)
            {
                actions.Add(Create("Production", "High", 2, title,
                    "Production job is due within seven days.",
                    "Check bench progress, materials, and any customer approvals.",
                    job.DueDate, total, "Production", "Open Production Board",
                    string.Empty, $"job:{job.Id}:due-soon"));
            }
            else if (job.Status == JobStatus.AwaitingMaterials)
            {
                actions.Add(Create("Production", "High", 2, title,
                    "Job is waiting on materials.",
                    "Confirm stock, supplier diamond status, or purchasing needs.",
                    job.DueDate, total, "Production", "Open Production Board",
                    "Waiting material state can block promised delivery dates.", $"job:{job.Id}:awaiting-materials"));
            }
            else if (job.Status == JobStatus.AwaitingCustomerApproval)
            {
                actions.Add(Create("Production", "High", 2, title,
                    "Job is waiting on customer approval.",
                    "Follow up with the customer and record the approval decision.",
                    job.DueDate, total, "Production", "Open Production Board",
                    string.Empty, $"job:{job.Id}:awaiting-approval"));
            }
        }

        foreach (var diamond in externalDiamonds.Values.Where(DiamondNeedsAction))
        {
            var link = diamondLinks.FirstOrDefault(x => x.ExternalDiamondId == diamond.Id);
            CustomQuote? quote = null;
            if (link != null && quoteIdByOption.TryGetValue(link.QuoteOptionId, out var quoteId))
                quoteById.TryGetValue(quoteId, out quote);

            var customer = quote == null ? "No customer linked" : GetCustomerName(customers, quote.CustomerId);
            var title = $"{customer} - {BuildDiamondSummary(diamond)}";
            var expiresSoon = diamond.HoldExpiresAt.HasValue && diamond.HoldExpiresAt.Value <= now.AddHours(24);
            var orderedNotReceived = diamond.IsOrderedNotReceived;
            actions.Add(Create("Diamond", expiresSoon ? "Urgent" : "High", expiresSoon ? 1 : 2, title,
                expiresSoon ? "Supplier diamond hold is expired or expiring." : orderedNotReceived ? "Supplier diamond is ordered but not received." : "Supplier diamond needs hold or order progress.",
                expiresSoon ? "Confirm availability with the supplier now." : "Open diamond holds and update the supplier workflow.",
                diamond.HoldExpiresAt ?? diamond.ExpectedArrivalDate, diamond.SupplierPrice, "Diamond Holds", "Open Diamond Holds",
                "Supplier stones can become unavailable while a quote is still active.", $"diamond:{diamond.Id}:supplier"));
        }

        foreach (var material in materials.Where(x => x.ReorderLevel > 0 && x.CurrentQuantity <= x.ReorderLevel))
        {
            var urgent = material.CurrentQuantity <= 0;
            actions.Add(Create("Inventory", urgent ? "Urgent" : "High", urgent ? 1 : 2,
                DisplayOrFallback(material.Name, material.MaterialCode, "Material"),
                $"Current stock is {material.CurrentQuantity:0.###} {material.UnitType}; reorder level is {material.ReorderLevel:0.###}.",
                "Open materials and reorder or adjust stock.",
                null, null, "Materials", "Open Materials",
                urgent ? "This material is at zero stock." : "Low stock can block quoting or production.", $"material:{material.Id}:low-stock"));
        }

        foreach (var task in tasks.Where(ShouldShowTaskAction).Take(120))
        {
            var overdue = task.DueDate.HasValue && task.DueDate.Value.Date < today;
            var dueToday = task.DueDate.HasValue && task.DueDate.Value.Date == today;
            var priorityRank = overdue || task.Priority == BusinessTaskPriority.Urgent ? 1 : task.Priority == BusinessTaskPriority.High || dueToday ? 2 : 3;
            var priority = priorityRank == 1 ? "Urgent" : priorityRank == 2 ? "High" : "Medium";
            var target = task.JobId.HasValue ? "Jobs" : task.CustomerId.HasValue ? "Customers" : "Tasks";
            actions.Add(Create("Follow-up", priority, priorityRank,
                DisplayOrFallback(task.Title, task.TaskCode, "Task"),
                DisplayOrFallback(task.Description, task.FollowUpNotes, $"{task.Category} task is still open."),
                "Open the linked record or task queue and complete the next contact.",
                task.DueDate, null, target, target == "Tasks" ? "Open Tasks" : $"Open {target}",
                overdue ? "Task is overdue." : string.Empty, $"task:{task.Id}:open"));
        }

        return actions
            .OrderBy(x => x.PriorityRank)
            .ThenBy(x => x.DueDate ?? DateTime.MaxValue)
            .ThenBy(x => x.Area)
            .ThenBy(x => x.Title)
            .Take(350)
            .ToList();

        bool ShouldShowTaskAction(BusinessTask task)
        {
            if (task.Priority == BusinessTaskPriority.High || task.Priority == BusinessTaskPriority.Urgent)
                return true;
            if (task.DueDate.HasValue && task.DueDate.Value.Date <= today.AddDays(3))
                return true;
            return task.ShowOnDashboard;
        }
    }

    private static NextActionItem Create(
        string area,
        string priority,
        int priorityRank,
        string title,
        string detail,
        string suggestedAction,
        DateTime? dueDate,
        decimal? value,
        string targetKey,
        string actionLabel,
        string risk,
        string sourceKey)
    {
        return new NextActionItem
        {
            Area = area,
            PriorityLabel = priority,
            PriorityRank = priorityRank,
            Title = title,
            Detail = detail,
            SuggestedAction = suggestedAction,
            DueDate = dueDate,
            Value = value,
            TargetKey = targetKey,
            ActionLabel = actionLabel,
            Risk = risk,
            SourceKey = sourceKey
        };
    }

    private static QuoteOption? GetDisplayOption(CustomQuote quote, IReadOnlyCollection<QuoteOption>? options)
    {
        if (options == null || options.Count == 0)
            return null;

        return quote.AcceptedOptionId.HasValue
            ? options.FirstOrDefault(x => x.Id == quote.AcceptedOptionId.Value) ?? options.FirstOrDefault(x => x.IsRecommended) ?? options.OrderByDescending(x => x.TotalPrice).FirstOrDefault()
            : options.FirstOrDefault(x => x.IsRecommended) ?? options.OrderByDescending(x => x.TotalPrice).FirstOrDefault();
    }

    private static List<ExternalDiamond> GetLinkedDiamonds(
        int quoteId,
        IReadOnlyCollection<QuoteOption>? options,
        IReadOnlyDictionary<int, List<QuoteOptionExternalDiamondLink>> diamondLinksByOption,
        IReadOnlyDictionary<int, ExternalDiamond> externalDiamonds)
    {
        if (options == null || options.Count == 0)
            return new List<ExternalDiamond>();

        return options
            .Where(x => x.CustomQuoteId == quoteId)
            .SelectMany(o => diamondLinksByOption.TryGetValue(o.Id, out var links) ? links : Enumerable.Empty<QuoteOptionExternalDiamondLink>())
            .Select(l => externalDiamonds.TryGetValue(l.ExternalDiamondId, out var d) ? d : null)
            .Where(d => d != null)
            .Cast<ExternalDiamond>()
            .ToList();
    }

    private static bool QuoteAccepted(CustomQuote quote)
    {
        var status = quote.Status ?? string.Empty;
        return quote.AcceptedOptionId.HasValue || status.Contains("Accepted", StringComparison.OrdinalIgnoreCase);
    }

    private static bool ProposalSent(CustomQuote quote)
    {
        var status = quote.ProposalStatus ?? string.Empty;
        return quote.ProposalSentAt.HasValue || status.Contains("Sent", StringComparison.OrdinalIgnoreCase);
    }

    private static bool DiamondNeedsAction(ExternalDiamond diamond)
    {
        var status = diamond.Status ?? string.Empty;
        if (status.Contains("Received", StringComparison.OrdinalIgnoreCase) ||
            status.Contains("Declined", StringComparison.OrdinalIgnoreCase) ||
            status.Contains("Released", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (diamond.HoldExpiresAt.HasValue && diamond.HoldExpiresAt.Value <= DateTime.Now.AddHours(24))
            return true;

        return status is "Customer Interested" or "Hold Requested" or "Hold Confirmed" or "Hold Expiring" or "Order Requested" or "Ordered";
    }

    private static decimal GetJobTotal(Job job) => job.FinalPrice > 0 ? job.FinalPrice : job.QuoteAmount;

    private static string GetCustomerName(IReadOnlyDictionary<int, string> customers, int? customerId)
    {
        return customerId.HasValue && customers.TryGetValue(customerId.Value, out var name) && !string.IsNullOrWhiteSpace(name)
            ? name
            : "No customer linked";
    }

    private static string BuildDiamondSummary(ExternalDiamond diamond)
    {
        var type = diamond.IsLabGrown ? "Lab-grown" : "Natural";
        return $"{type} {diamond.Shape} {diamond.Carat:0.###}ct {diamond.Color} {diamond.Clarity} {diamond.Lab} {DisplayOrFallback(diamond.CertificateNumber, diamond.SupplierDiamondId)}".Replace("  ", " ").Trim();
    }

    private static string DisplayOrFallback(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
                return value.Trim();
        }

        return "Untitled";
    }
}
