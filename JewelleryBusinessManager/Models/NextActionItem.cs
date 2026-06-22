namespace JewelleryBusinessManager.Models;

public sealed class NextActionItem
{
    public string Area { get; init; } = string.Empty;
    public string PriorityLabel { get; init; } = "Medium";
    public int PriorityRank { get; init; } = 3;
    public string Title { get; init; } = string.Empty;
    public string Detail { get; init; } = string.Empty;
    public DateTime? DueDate { get; init; }
    public string DueText { get; init; } = string.Empty;
    public decimal? Value { get; init; }
    public string ValueText { get; init; } = string.Empty;
    public string Risk { get; init; } = string.Empty;
    public string SuggestedAction { get; init; } = string.Empty;
    public string TargetKey { get; init; } = "Project Workbench";
    public string ActionLabel { get; init; } = "Open Next Step";
    public string SourceKey { get; init; } = string.Empty;

    public string DueLine => !string.IsNullOrWhiteSpace(DueText)
        ? DueText
        : DueDate.HasValue
            ? DueDate.Value.ToString("d")
            : "No due date";

    public string ValueLine => !string.IsNullOrWhiteSpace(ValueText)
        ? ValueText
        : Value.HasValue
            ? Value.Value.ToString("C")
            : string.Empty;

    public bool IsActionNeeded => PriorityRank <= 2;
    public string StableKey => string.IsNullOrWhiteSpace(SourceKey) ? $"{Area}:{Title}:{DueLine}" : SourceKey;
}
