namespace Aura.Application.Models;

public sealed record CheckpointIdentity
{
    public string Connector { get; }
    public string Source { get; }
    public string Tenant { get; }

    public CheckpointIdentity(string connector, string source, string tenant)
    {
        if (string.IsNullOrEmpty(connector))
            throw new ArgumentException("Connector must not be null or empty.", nameof(connector));

        if (string.IsNullOrEmpty(source))
            throw new ArgumentException("Source must not be null or empty.", nameof(source));

        if (string.IsNullOrEmpty(tenant))
            throw new ArgumentException("Tenant must not be null or empty.", nameof(tenant));

        Connector = connector;
        Source = source;
        Tenant = tenant;
    }
}
