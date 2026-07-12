namespace Aura.Application.Models;

public enum SystemIndicatorState
{
    Ok,
    Warning,
    Error
}

public sealed record SystemIndicatorDto(SystemIndicatorState State, string Microcopy);

public sealed record SystemStatusDto(
    SystemIndicatorDto Api,
    SystemIndicatorDto Qdrant,
    SystemIndicatorDto MockAuth,
    SystemIndicatorDto Database,
    SystemIndicatorDto Llm);
