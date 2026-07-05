using Aura.UI.Models;

namespace Aura.UI.Services;

public interface IDecisionLogApiClient
{
    Task<DecisionLogResponse> GetDecisionsAsync(int page, int pageSize, CancellationToken cancellationToken);
}
