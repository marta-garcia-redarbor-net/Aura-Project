using Aura.Application.Ports;
using Aura.Infrastructure.Adapters.Calendar;
using Aura.Infrastructure.Adapters.Connectors.Graph;
using Aura.Infrastructure.Adapters.Decisions;
using Aura.Infrastructure.Adapters.FocusState;
using Aura.Infrastructure.Adapters.MorningSummaryScheduling;
using Aura.Infrastructure.Adapters.Notifications;
using Aura.Infrastructure.Adapters.Rules;
using Aura.Infrastructure.Adapters.SemanticOutbox;
using Aura.Infrastructure.Adapters.WorkItems;
using Aura.Infrastructure.Adapters.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Aura.Infrastructure;

/// <summary>
/// Extension methods for conditional store registration based on configuration.
/// Each store can be independently switched between SQLite (local dev) and EF Core (ACA)
/// via the config key <c>Persistence:Providers:{StoreName}</c>.
/// </summary>
public static class StoreRegistrationExtensions
{
    /// <summary>
    /// Registers <see cref="AuraDbContext"/> with the specified connection string name.
    /// Uses the SQLite provider (suitable for both local dev and Azure SQL via EF Core).
    /// </summary>
    public static IServiceCollection AddAuraDbContext(this IServiceCollection services, string connectionStringName)
    {
        services.AddDbContext<AuraDbContext>((sp, options) =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var connectionString = config.GetConnectionString(connectionStringName)
                                   ?? $"Data Source={connectionStringName}.db";
            var provider = config["Persistence:Provider"];

            // Use SQL Server provider for Enterprise/Cloud connections, SQLite for local
            if (string.Equals(provider, "EntityFramework", StringComparison.OrdinalIgnoreCase)
                && connectionString.Contains("Server=", StringComparison.OrdinalIgnoreCase))
            {
                options.UseSqlServer(connectionString);
            }
            else
            {
                options.UseSqlite(connectionString);
            }
        });

        return services;
    }

    /// <summary>
    /// Reads config and registers EF Core implementations for stores where the
    /// provider is <c>EntityFramework</c>. Resolution order:
    /// 1. Per-store toggle <c>Persistence:Providers:{StoreName}</c>
    /// 2. Global default <c>Persistence:Provider</c>
    /// 3. Falls back to <c>Sqlite</c> (existing SQLite store in <see cref="DependencyInjection"/>)
    /// </summary>
    public static IServiceCollection RegisterConditionalStores(this IServiceCollection services, IConfiguration configuration)
    {
        if (IsEntityFramework(configuration, "FocusStateOverride"))
        {
            services.AddScoped<IFocusStateOverrideStore, EfFocusStateOverrideStore>();
        }

        if (IsEntityFramework(configuration, "InterruptionDecision"))
        {
            services.AddScoped<IInterruptionDecisionStore, EfInterruptionDecisionStore>();
        }

        if (IsEntityFramework(configuration, "AlertRule"))
        {
            services.AddScoped<IAlertRuleStore, EfAlertRuleStore>();
        }

        if (IsEntityFramework(configuration, "NotificationOutbox"))
        {
            services.AddScoped<INotificationOutboxStore, EfNotificationOutboxStore>();
        }

        if (IsEntityFramework(configuration, "MeetingAlert"))
        {
            services.AddScoped<IMeetingAlertStore, EfMeetingAlertStore>();
        }

        if (IsEntityFramework(configuration, "MorningSummaryEmission"))
        {
            services.AddScoped<IMorningSummaryEmissionStore, EfMorningSummaryEmissionStore>();
        }

        if (IsEntityFramework(configuration, "WorkItem"))
        {
            services.AddScoped<IWorkItemStore, EfWorkItemStore>();
            services.AddScoped<IWorkItemReader, EfWorkItemStore>(sp =>
                (EfWorkItemStore)sp.GetRequiredService<IWorkItemStore>());
        }

        if (IsEntityFramework(configuration, "SemanticOutbox"))
        {
            services.AddScoped<ISemanticOutboxRepository, EfSemanticOutboxRepository>();
        }

        if (IsEntityFramework(configuration, "MsalTokenCache"))
        {
            services.AddScoped<IMsalTokenCacheStore, EfMsalTokenCacheStore>();
        }

        return services;
    }

    /// <summary>
    /// Opt-in method for EF Core + Azure SQL. Only call this from host Program.cs
    /// when the ACA deployment is active. Registers the DbContext and shadows
    /// SQLite stores with EF Core implementations when the config toggle is set.
    /// Safe to call unconditionally — checks <c>Persistence:Provider</c> internally.
    /// </summary>
    public static IServiceCollection AddAuraEntityFrameworkCore(this IServiceCollection services, IConfiguration configuration)
    {
        // Guard: only register EF Core when Persistence:Provider = EntityFramework
        if (string.Equals(configuration["Persistence:Provider"], "EntityFramework", StringComparison.OrdinalIgnoreCase))
        {
            services.AddAuraDbContext("AuraDb");
            services.RegisterConditionalStores(configuration);
        }

        return services;
    }

    internal sealed class EfSchemaInitializer : IHostedService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        public EfSchemaInitializer(IServiceScopeFactory scopeFactory) => _scopeFactory = scopeFactory;
        public async Task StartAsync(CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AuraDbContext>();
            await db.Database.EnsureCreatedAsync(ct);
        }
        public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
    }

    private static bool IsEntityFramework(IConfiguration config, string storeName)
    {
        // Per-store override takes precedence (e.g., Persistence:Providers:WorkItem)
        var perStore = config[$"Persistence:Providers:{storeName}"];
        if (!string.IsNullOrEmpty(perStore))
            return string.Equals(perStore, "EntityFramework", StringComparison.OrdinalIgnoreCase);

        // Fall back to global default: Persistence:Provider = EntityFramework
        var global = config["Persistence:Provider"];
        return string.Equals(global, "EntityFramework", StringComparison.OrdinalIgnoreCase);
    }
}

internal sealed class EfSchemaInitializer : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    public EfSchemaInitializer(IServiceScopeFactory scopeFactory) => _scopeFactory = scopeFactory;
    public async Task StartAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuraDbContext>();
        await db.Database.EnsureCreatedAsync(ct);
    }
    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
