namespace Aura.UI.Models;

public enum SystemIndicatorStateResponse
{
    Ok,
    Warning,
    Error
}

public sealed record SystemIndicatorResponse(SystemIndicatorStateResponse State, string Microcopy);

public sealed record SystemStatusResponse(
    SystemIndicatorResponse Api,
    SystemIndicatorResponse Qdrant,
    SystemIndicatorResponse MockAuth);
