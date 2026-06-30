namespace Aura.UI.Services;

public interface ISyncApiClient
{
    Task<List<SourceSyncStateDto>> GetSyncStatusAsync(CancellationToken cancellationToken);
    Task TriggerSyncAsync(CancellationToken cancellationToken);
}

public class SourceSyncStateDto
{
    public string Source { get; set; } = "";
    public string? Status { get; set; }
    public int ItemCount { get; set; }
    public DateTimeOffset? LastSyncTimestamp { get; set; }
}
