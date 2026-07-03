using Aura.UI.Models;

namespace Aura.UI.Services;

public interface IAzureDevOpsPrClient
{
    Task<List<PullRequestResponse>> GetPendingPullRequestsAsync(CancellationToken ct = default);
}
