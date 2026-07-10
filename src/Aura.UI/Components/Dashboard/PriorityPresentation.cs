using Aura.UI.Models;

namespace Aura.UI.Components.Dashboard;

public static class PriorityPresentation
{
    public static bool IsHighPriority(int? priorityScore)
        => priorityScore is >= 75;

    public static IReadOnlyList<InboxItemPreviewResponse> SelectTopPriority(
        IReadOnlyList<InboxItemPreviewResponse> items,
        int minimumTop = 3)
    {
        if (items.Count == 0)
        {
            return [];
        }

        if (items.Count <= minimumTop)
        {
            return items;
        }

        var cutoff = items
            .Select(i => i.PriorityScore ?? int.MinValue)
            .OrderByDescending(x => x)
            .ElementAt(minimumTop - 1);

        return items
            .Where(i => (i.PriorityScore ?? int.MinValue) >= cutoff)
            .ToList();
    }

    public static bool HasTopPriorityItems(IReadOnlyList<InboxItemPreviewResponse>? items, int minimumTop = 3)
        => items is { Count: > 0 } && SelectTopPriority(items, minimumTop).Count > 0;

    public static bool HasTopPriorityPrItems(IReadOnlyList<PrPreviewItemResponse>? items, int minimumTop = 3)
    {
        if (items is not { Count: > 0 })
        {
            return false;
        }

        var projected = items
            .Select(item => new InboxItemPreviewResponse(item.Title, "pull-requests", item.RelativeTimestamp, 0, "Review")
            {
                PriorityScore = MapPrPriorityToScore(item.Priority)
            })
            .ToList();

        return SelectTopPriority(projected, minimumTop).Count > 0;
    }

    /// <summary>
    /// Resolves the effective priority score for an inbox item, falling back from
    /// <see cref="InboxItemPreviewResponse.PriorityScore"/> to
    /// <see cref="InboxItemPreviewResponse.PriorityHint"/> when the numeric score is not set.
    /// Mirrors <c>DashboardEndpoints.ResolveEffectivePriorityScore</c> on the API side.
    /// </summary>
    public static int ResolveEffectivePriorityScore(InboxItemPreviewResponse item)
    {
        if (item.PriorityScore.HasValue)
            return item.PriorityScore.Value;

        return item.PriorityHint switch
        {
            "Critical" => 100,
            "High" => 75,
            "Medium" => 50,
            "Low" => 25,
            _ => 0
        };
    }

    public static int GetHighPriorityCount(IEnumerable<InboxItemPreviewResponse>? items)
        => items?.Count(item => ResolveEffectivePriorityScore(item) >= 75) ?? 0;

    public static int GetHighPriorityPrCount(IEnumerable<PrPreviewItemResponse>? items)
        => items?.Count(item => IsHighPriority(MapPrPriorityToScore(item.Priority))) ?? 0;

    public static IReadOnlyList<WorkItemDetailResponse> SortWorkItemsForTopPriority(
        IReadOnlyList<WorkItemDetailResponse> items)
        => items
            .OrderByDescending(item => item.PriorityScore ?? int.MinValue)
            .ThenByDescending(item => item.CapturedAtUtc)
            .ToList();

    private static int MapPrPriorityToScore(string? priority)
        => (priority?.ToLowerInvariant()) switch
        {
            "critical" => 100,
            "high" => 75,
            "medium" => 50,
            _ => 25
        };
}
