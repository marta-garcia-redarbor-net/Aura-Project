namespace Aura.Application.Models;

public sealed record ConnectorExecutionRequest(
    CheckpointIdentity Identity,
    DateTimeOffset WindowStart,
    DateTimeOffset WindowEnd);
