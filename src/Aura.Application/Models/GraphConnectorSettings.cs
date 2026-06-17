namespace Aura.Application.Models;

public sealed record GraphConnectorSettings(
    bool Enabled,
    string? TenantId,
    string? ClientId,
    bool HasValidCredentialsBlock);
