using System.Reflection;

namespace Aura.UI.Services;

public class AppVersionService
{
    public string DisplayVersion { get; }

    public AppVersionService()
    {
        var assembly = Assembly.GetEntryAssembly();
        var informationalVersion = assembly
            ?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion;

        DisplayVersion = !string.IsNullOrEmpty(informationalVersion)
            ? $"v{informationalVersion}"
            : "v0.0.0-local";
    }
}
