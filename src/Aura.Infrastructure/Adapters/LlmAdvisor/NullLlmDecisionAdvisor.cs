using Aura.Application.Models;
using Aura.Application.Ports;

namespace Aura.Infrastructure.Adapters.LlmAdvisor;

/// <summary>
/// Null advisor used when LLM advisory is disabled.
/// </summary>
internal sealed class NullLlmDecisionAdvisor : ILlmDecisionAdvisor
{
    public Task<AdvisoryResponse> EvaluateAsync(AdvisoryRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        ct.ThrowIfCancellationRequested();

        return Task.FromResult(new AdvisoryResponse(
            SuggestedVerdict: request.DeterministicVerdict,
            Rationale: "LLM advisor disabled. Deterministic verdict retained.",
            GuardrailOutcome: "confirmed"));
    }
}
