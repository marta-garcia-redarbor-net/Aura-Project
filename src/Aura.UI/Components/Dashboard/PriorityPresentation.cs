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
}
