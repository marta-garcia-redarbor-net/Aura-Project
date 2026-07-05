namespace Aura.Application.Models;

public sealed record PriorityScore(
    string RuleKey,
    int InterruptionRank,
    bool IsInterruptCandidate,
    bool IsCriticalInterrupt,
    string Explanation,
    IReadOnlyList<PriorityFactorContribution> Factors);

public sealed record PriorityFactorContribution(string Key, string Explanation);
