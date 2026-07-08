namespace Aura.Infrastructure.Adapters.Options;

/// <summary>
/// Configuration options for Demo Mode.
/// When enabled, demo data-loading endpoints are mapped and the UI shows "Load Sample Data" buttons.
/// </summary>
public sealed class DemoModeOptions
{
    /// <summary>Configuration section name in appsettings.json.</summary>
    public const string SectionName = "DemoMode";

    /// <summary>
    /// Whether demo mode is enabled. Default: false.
    /// </summary>
    public bool Enabled { get; set; }
}
