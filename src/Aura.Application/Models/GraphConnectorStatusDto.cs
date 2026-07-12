namespace Aura.Application.Models;

public enum GraphConnectorState
{
    Disabled,
    ValidConfig
}

public sealed record GraphConnectorStatusDto(GraphConnectorState State);
