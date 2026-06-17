namespace Aura.Infrastructure.Adapters.GraphConnector;

internal sealed class GraphConnectorOptions
{
    internal const string SectionName = "GraphConnector";

    public bool Enabled { get; set; }

    public string? TenantId { get; set; }

    public string? ClientId { get; set; }

    public string? ClientSecret { get; set; }
}
