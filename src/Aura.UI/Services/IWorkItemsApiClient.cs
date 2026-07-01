using Aura.UI.Models;

namespace Aura.UI.Services;

public interface IWorkItemsApiClient
{
    Task<IReadOnlyList<WorkItemDetailResponse>> GetBySourceAsync(
        string sourceType,
        string? status,
        CancellationToken cancellationToken);
}
