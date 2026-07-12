namespace Aura.Infrastructure.Adapters.GraphConnector;

public sealed class GraphConnectorOptions
{
    public const string SectionName = "GraphConnector";

    public bool Enabled { get; set; }

    public string? TenantId { get; set; }

    public string? ClientId { get; set; }

    public string? ClientSecret { get; set; }

    public string? RedirectUri { get; set; }

    public string[]? Scopes { get; set; }

    public bool IsProductionReady =>
        Enabled
        && IsValidGuid(TenantId)
        && IsValidGuid(ClientId);

    private static bool IsValidGuid(string? value) =>
        !string.IsNullOrWhiteSpace(value)
        && Guid.TryParse(value, out _);
}
