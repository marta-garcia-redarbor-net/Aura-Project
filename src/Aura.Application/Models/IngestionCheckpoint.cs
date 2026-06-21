namespace Aura.Application.Models;

public sealed record IngestionCheckpoint(
    string? Cursor,
    DateTimeOffset? MaxProcessedAt,
    DateTimeOffset? ExecutionFinishedAt);
