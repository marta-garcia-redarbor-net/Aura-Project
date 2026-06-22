namespace Aura.Application.Models;

/// <summary>
/// Explainable ranking breakdown for a ranked work item.
/// </summary>
/// <param name="Contributions">Ordered factor contributions used to explain ranking.</param>
public sealed record RankingExplanation(IReadOnlyList<RankingFactorContribution> Contributions);

/// <summary>
/// Single factor contribution used in ranking explanation.
/// </summary>
/// <param name="Factor">Ranking factor represented by this contribution.</param>
/// <param name="Value">Contribution numeric value.</param>
/// <param name="Rationale">Human-readable rationale for the contribution value.</param>
public sealed record RankingFactorContribution(RankingFactor Factor, double Value, string Rationale);
