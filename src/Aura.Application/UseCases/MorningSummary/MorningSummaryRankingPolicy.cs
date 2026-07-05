using System.Globalization;
using Aura.Application.Models;
using Aura.Application.Ports;
using Aura.Domain.WorkItems;

namespace Aura.Application.UseCases.MorningSummary;

public sealed class MorningSummaryRankingPolicy : IMorningSummaryRankingPolicy
{
    public IReadOnlyList<RankedWorkItem> Rank(IReadOnlyList<WorkItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        if (items.Count == 0)
        {
            return [];
        }

        var snapshots = items.Select(CreateSnapshot).ToList();

        var ordered = snapshots
            .OrderByDescending(s => s.HasAnyUsableSignal)
            .ThenBy(s => s, SnapshotComparer.Instance)
            .ToList();

        var ranked = new List<RankedWorkItem>(ordered.Count);
        for (var index = 0; index < ordered.Count; index++)
        {
            var snapshot = ordered[index];
            ranked.Add(new RankedWorkItem(
                Rank: index + 1,
                Item: snapshot.Item,
                Score: snapshot.Score,
                Explanation: BuildExplanation(snapshot)));
        }

        return ranked;
    }

    private static RankingExplanation BuildExplanation(WorkItemSnapshot snapshot)
    {
        var contributions = new List<RankingFactorContribution>(4);

        if (snapshot.HasDeadlineSignal)
        {
            contributions.Add(new RankingFactorContribution(
                RankingFactor.Deadline,
                snapshot.DeadlineScore,
                "Deadline signal present."));
        }

        if (snapshot.HasImpactSignal)
        {
            contributions.Add(new RankingFactorContribution(
                RankingFactor.Impact,
                snapshot.ImpactScore,
                "Impact derived from WorkItem priority."));
        }

        if (snapshot.HasRiskSignal)
        {
            contributions.Add(new RankingFactorContribution(
                RankingFactor.Risk,
                snapshot.RiskScore,
                "Risk signal present."));
        }

        if (snapshot.HasPreliminaryScore)
        {
            contributions.Add(new RankingFactorContribution(
                RankingFactor.PreliminaryScore,
                snapshot.PreliminaryScore,
                snapshot.HasAnyExplicitSignal
                    ? "Preliminary score used after explicit signals."
                    : "Preliminary score used as fallback when explicit signals are absent."));
        }

        return new RankingExplanation(contributions);
    }

    private static WorkItemSnapshot CreateSnapshot(WorkItem item)
    {
        var metadata = item.Metadata;

        var hasDeadlineSignal = HasDeadlineSignal(metadata);
        var deadlineAtUtc = ParseDateTimeOffset(metadata, WorkItemSignalKeys.OutlookDeadlineAtUtc);
        var deadlineScore = hasDeadlineSignal ? 1d : 0d;

        var (hasImpactSignal, impactScore) = GetImpactSignal(item, metadata);
        var (hasRiskSignal, riskScore) = GetRiskSignal(metadata);
        var (hasPreliminaryScore, preliminaryScore) = GetPreliminaryScore(metadata);

        var hasAnyExplicitSignal = hasDeadlineSignal || hasImpactSignal || hasRiskSignal;
        var hasAnyUsableSignal = hasAnyExplicitSignal || hasPreliminaryScore;

        var score =
            (hasDeadlineSignal ? 1000d : 0d) +
            (impactScore * 100d) +
            (riskScore * 10d) +
            preliminaryScore;

        return new WorkItemSnapshot(
            item,
            hasDeadlineSignal,
            deadlineAtUtc,
            deadlineScore,
            hasImpactSignal,
            impactScore,
            hasRiskSignal,
            riskScore,
            hasPreliminaryScore,
            preliminaryScore,
            hasAnyExplicitSignal,
            hasAnyUsableSignal,
            score);
    }

    private static bool HasDeadlineSignal(IReadOnlyDictionary<string, string> metadata)
    {
        var hasCue = metadata.TryGetValue(WorkItemSignalKeys.OutlookDeadlineCue, out var cue) && !string.IsNullOrWhiteSpace(cue);
        var hasSource = metadata.TryGetValue(WorkItemSignalKeys.OutlookDeadlineSource, out var source) && !string.IsNullOrWhiteSpace(source);
        return hasCue || hasSource;
    }

    private static (bool HasImpactSignal, double Score) GetImpactSignal(WorkItem item, IReadOnlyDictionary<string, string> metadata)
    {
        if (metadata.TryGetValue(WorkItemSignalKeys.TeamsPriorityRaw, out var raw) &&
            string.Equals(raw, "absent", StringComparison.OrdinalIgnoreCase))
        {
            return (false, 0d);
        }

        return (true, PriorityToScore(item.Priority));
    }

    private static (bool HasRiskSignal, double Score) GetRiskSignal(IReadOnlyDictionary<string, string> metadata)
    {
        if (!metadata.TryGetValue(WorkItemSignalKeys.RiskScore, out var rawRisk))
        {
            return (false, 0d);
        }

        if (!double.TryParse(rawRisk, NumberStyles.Float, CultureInfo.InvariantCulture, out var riskScore))
        {
            return (false, 0d);
        }

        return (true, riskScore);
    }

    private static (bool HasPreliminaryScore, double Score) GetPreliminaryScore(IReadOnlyDictionary<string, string> metadata)
    {
        if (TryParseDouble(metadata, WorkItemSignalKeys.OutlookScoringTotalScore, out var score))
        {
            return (true, score);
        }

        if (TryParseTeamsPriorityRaw(metadata, out score))
        {
            return (true, score);
        }

        return (false, 0d);
    }

    private static bool TryParseTeamsPriorityRaw(IReadOnlyDictionary<string, string> metadata, out double score)
    {
        score = 0d;
        if (!metadata.TryGetValue(WorkItemSignalKeys.TeamsPriorityRaw, out var rawPriority))
        {
            return false;
        }

        if (string.Equals(rawPriority, "absent", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (double.TryParse(rawPriority, NumberStyles.Float, CultureInfo.InvariantCulture, out score))
        {
            return true;
        }

        score = rawPriority.ToLowerInvariant() switch
        {
            "critical" => 1.0,
            "high" => 0.75,
            "medium" => 0.5,
            "low" => 0.25,
            _ => 0d
        };

        return score > 0;
    }

    private static bool TryParseDouble(IReadOnlyDictionary<string, string> metadata, string key, out double value)
    {
        value = 0d;
        if (!metadata.TryGetValue(key, out var raw) || string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        return double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    private static DateTimeOffset? ParseDateTimeOffset(IReadOnlyDictionary<string, string> metadata, string key)
    {
        if (!metadata.TryGetValue(key, out var raw) || string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        return DateTimeOffset.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed)
            ? parsed.ToUniversalTime()
            : null;
    }

    private static double PriorityToScore(WorkItemPriority priority)
    {
        return priority switch
        {
            WorkItemPriority.Critical => 1.00,
            WorkItemPriority.High => 0.75,
            WorkItemPriority.Medium => 0.50,
            WorkItemPriority.Low => 0.25,
            _ => 0d
        };
    }

    private readonly record struct WorkItemSnapshot(
        WorkItem Item,
        bool HasDeadlineSignal,
        DateTimeOffset? DeadlineAtUtc,
        double DeadlineScore,
        bool HasImpactSignal,
        double ImpactScore,
        bool HasRiskSignal,
        double RiskScore,
        bool HasPreliminaryScore,
        double PreliminaryScore,
        bool HasAnyExplicitSignal,
        bool HasAnyUsableSignal,
        double Score);

    private sealed class SnapshotComparer : IComparer<WorkItemSnapshot>
    {
        public static SnapshotComparer Instance { get; } = new();

        public int Compare(WorkItemSnapshot left, WorkItemSnapshot right)
        {
            var deadlineComparison = CompareDeadline(left, right);
            if (deadlineComparison != 0)
            {
                return deadlineComparison;
            }

            var impactComparison = right.ImpactScore.CompareTo(left.ImpactScore);
            if (impactComparison != 0)
            {
                return impactComparison;
            }

            var riskComparison = right.RiskScore.CompareTo(left.RiskScore);
            if (riskComparison != 0)
            {
                return riskComparison;
            }

            var preliminaryComparison = right.PreliminaryScore.CompareTo(left.PreliminaryScore);
            if (preliminaryComparison != 0)
            {
                return preliminaryComparison;
            }

            var dueDateComparison = CompareNullableDate(left.DeadlineAtUtc, right.DeadlineAtUtc);
            if (dueDateComparison != 0)
            {
                return dueDateComparison;
            }

            var createdComparison = left.Item.CreatedAt.CompareTo(right.Item.CreatedAt);
            if (createdComparison != 0)
            {
                return createdComparison;
            }

            return string.Compare(left.Item.ExternalId, right.Item.ExternalId, StringComparison.Ordinal);
        }

        private static int CompareDeadline(WorkItemSnapshot left, WorkItemSnapshot right)
        {
            if (left.HasDeadlineSignal && !right.HasDeadlineSignal)
            {
                return -1;
            }

            if (!left.HasDeadlineSignal && right.HasDeadlineSignal)
            {
                return 1;
            }

            if (left.HasDeadlineSignal && right.HasDeadlineSignal)
            {
                return CompareNullableDate(left.DeadlineAtUtc, right.DeadlineAtUtc);
            }

            return 0;
        }

        private static int CompareNullableDate(DateTimeOffset? left, DateTimeOffset? right)
        {
            if (left.HasValue && right.HasValue)
            {
                return left.Value.CompareTo(right.Value);
            }

            if (left.HasValue)
            {
                return -1;
            }

            if (right.HasValue)
            {
                return 1;
            }

            return 0;
        }
    }
}
