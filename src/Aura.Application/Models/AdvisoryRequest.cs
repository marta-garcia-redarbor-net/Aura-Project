using Aura.Domain.WorkItems;

namespace Aura.Application.Models;

/// <summary>
/// Input contract for LLM decision advisory.
/// </summary>
public sealed record AdvisoryRequest(
    WorkItem Item,
    string DeterministicVerdict,
    IReadOnlyDictionary<string, NormalizedSignal> Signals,
    IReadOnlyList<DecisionContextItem> Context);
