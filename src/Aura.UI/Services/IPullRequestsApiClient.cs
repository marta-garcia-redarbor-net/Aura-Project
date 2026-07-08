using Aura.UI.Models;

namespace Aura.UI.Services;

public interface IPullRequestsApiClient
{
    Task<IReadOnlyList<PullRequestResponse>> GetPendingPullRequestsAsync(CancellationToken ct = default);
}
