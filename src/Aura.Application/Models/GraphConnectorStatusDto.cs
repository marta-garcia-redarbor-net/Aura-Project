namespace Aura.Application.Models;

public enum GraphConnectorState
{
    Disabled,
    MissingConfig,
    PartialConfig,
    ValidConfig
}

public sealed record GraphConnectorStatusDto(GraphConnectorState State);
