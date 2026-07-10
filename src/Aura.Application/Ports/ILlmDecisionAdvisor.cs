using Aura.Application.Models;

namespace Aura.Application.Ports;

/// <summary>
/// Port for bounded LLM advisory over deterministic interruption decisions.
/// </summary>
public interface ILlmDecisionAdvisor
{
    Task<AdvisoryResponse> EvaluateAsync(AdvisoryRequest request, CancellationToken ct);
}
