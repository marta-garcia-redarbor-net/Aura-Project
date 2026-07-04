namespace Aura.Application.Models;

public sealed class UserTriagePolicy
{
    public static UserTriagePolicy Empty { get; } = new();

    public IReadOnlyList<string> VipSenders { get; init; } = [];
    public IReadOnlyList<string> ActionNeededKeywords { get; init; } = [];
    public IReadOnlyList<ExplicitTriageOverride> ExplicitOverrides { get; init; } = [];
    public IReadOnlyList<ReviewFirstSuggestion> ReviewFirstSuggestions { get; init; } = [];
}

public sealed record ExplicitTriageOverride(
    string PatternKey,
    InterruptionDecision Decision,
    string Reason,
    bool AutoApply);

public sealed record ReviewFirstSuggestion(string PatternKey, string Reason);
