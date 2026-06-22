using Aura.Domain.WorkItems;

namespace Aura.Application.Models;

/// <summary>
/// Ranked entry contract for Morning Summary payloads.
/// </summary>
/// <param name="Rank">Position in ranking order.</param>
/// <param name="Item">Referenced work item.</param>
/// <param name="Score">Ranking score value.</param>
/// <param name="Explanation">Explainable factor breakdown for the score.</param>
public sealed record RankedWorkItem(int Rank, WorkItem Item, double Score, RankingExplanation Explanation);
