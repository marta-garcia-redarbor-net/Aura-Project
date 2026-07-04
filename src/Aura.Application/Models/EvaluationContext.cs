using Aura.Domain.FocusState;
using Aura.Domain.WorkItems;

namespace Aura.Application.Models;

/// <summary>
/// Context passed to each <see cref="Ports.IInterruptionRule"/> during evaluation.
/// Carries the WorkItem being evaluated and may be extended with cross-cutting state in the future.
/// </summary>
public sealed class EvaluationContext
{
    /// <summary>
    /// The WorkItem being evaluated by the interruption policy engine.
    /// </summary>
    public WorkItem Item { get; }

    public string? UserId { get; }

    public FocusState? FocusState { get; }

    public IReadOnlyDictionary<string, NormalizedSignal> NormalizedSignals { get; }

    public PriorityScore? PriorityScore { get; }

    public UserTriagePolicy ApprovedPolicy { get; }

    public EvaluationContext(WorkItem item)
        : this(item, null, null, null, null, UserTriagePolicy.Empty)
    {
    }

    public EvaluationContext(
        WorkItem item,
        string? userId = null,
        FocusState? focusState = null,
        IReadOnlyDictionary<string, NormalizedSignal>? normalizedSignals = null,
        PriorityScore? priorityScore = null,
        UserTriagePolicy? approvedPolicy = null)
    {
        ArgumentNullException.ThrowIfNull(item);
        Item = item;
        UserId = userId;
        FocusState = focusState;
        NormalizedSignals = normalizedSignals ?? new Dictionary<string, NormalizedSignal>(StringComparer.OrdinalIgnoreCase);
        PriorityScore = priorityScore;
        ApprovedPolicy = approvedPolicy ?? UserTriagePolicy.Empty;
    }

    public string? GetMetadataValue(string key)
        => Item.Metadata.TryGetValue(key, out var value) ? value : null;

    public bool TryGetBooleanSignal(string key, out bool value)
    {
        value = false;
        if (NormalizedSignals.TryGetValue(key, out var signal) && signal is BooleanSignal booleanSignal)
        {
            value = booleanSignal.Value;
            return true;
        }

        if (Item.Metadata.TryGetValue(key, out var raw) && bool.TryParse(raw, out value))
        {
            return true;
        }

        return false;
    }

    public bool TryLevelSignal(string key, out SignalLevel value)
    {
        value = SignalLevel.None;
        if (NormalizedSignals.TryGetValue(key, out var signal) && signal is LevelSignal levelSignal)
        {
            value = levelSignal.Value;
            return true;
        }

        if (Item.Metadata.TryGetValue(key, out var raw) && Enum.TryParse<SignalLevel>(raw, true, out value))
        {
            return true;
        }

        return false;
    }
}
