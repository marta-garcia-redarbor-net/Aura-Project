using Aura.Application.Ports;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Aura.Infrastructure.Adapters.Dashboard;

internal static class DependencyInjection
{
    internal static IServiceCollection AddDashboardAdapters(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        services.AddScoped<IApiReadinessProvider, AlwaysHealthyApiReadinessAdapter>();
        services.AddScoped<IQdrantReadinessProvider, QdrantReadinessAdapter>();
        services.AddScoped<IMockAuthReadinessProvider, MockJwtOptionsReadinessAdapter>();
        services.AddScoped<IModuleProgressProvider, SeededModuleProgressProvider>();
        services.AddScoped<IDbReadinessProvider, DbReadinessAdapter>();
        services.AddScoped<ILlmReadinessProvider, LlmReadinessAdapter>();

        return services;
    }
}
