using Aura.Application.Ports;
using Aura.Application.UseCases.IngestionSync;
using Aura.Infrastructure.Adapters.Identity;
using Aura.Infrastructure.Adapters.Ingestion;
using Aura.Infrastructure.Adapters.Ingestion.SemanticIndex;
using Aura.Infrastructure.Adapters.HealthChecks;
using Aura.Infrastructure.Adapters.Connectors;
using Aura.Infrastructure.Adapters.Connectors.Graph;
using Aura.Infrastructure.Adapters.GraphConnector;
using Aura.Infrastructure.Adapters.Dashboard;
using Aura.Infrastructure.Adapters.MorningSummaryScheduling;
using Aura.Infrastructure.Adapters.Notifications;
using Aura.Infrastructure.Adapters.Rules;
using Aura.Infrastructure.Adapters.SeedData;
using Aura.Infrastructure.Adapters.Options;
using Aura.Infrastructure.Adapters.Decisions;
using Aura.Infrastructure.Adapters.FocusState;
using Aura.Infrastructure.Adapters.Services;
using Aura.Infrastructure.Adapters.Services.Rules;
using Aura.Infrastructure.Adapters.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

namespace Aura.Infrastructure;

/// <summary>
/// Unified DI registration for all Infrastructure adapters.
/// Provides a single entry point for host applications to register all infrastructure services.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers all infrastructure adapter services with the DI container.
    /// Calls internal adapter DI methods for Ingestion (Embedding, SemanticIndex, SemanticOutbox) and Identity.
    /// Also registers the Qdrant health check.
    /// </summary>
    public static IServiceCollection AddAuraInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        services.AddIngestionAdapters(configuration);
        services.AddConnectorAdapters(configuration);
        services.AddGraphConnectorAdapter(configuration);
        services.AddIdentityAdapter(configuration, environment);
        services.AddDashboardAdapters(configuration, environment);
        services.AddMorningSummarySchedulingAdapters(configuration);

        // In-memory error store (dashboard error correlation)
        services.AddSingleton<IErrorStore, InMemoryErrorStore>();

        // Sync infrastructure
        services.AddSingleton<ISyncStateStore, InMemorySyncStateStore>();
        services.AddScoped<TriggerSyncUseCase>(sp => new TriggerSyncUseCase(
            sp.GetServices<IConnectorAdapter>(),
            sp.GetRequiredService<ISyncStateStore>(),
            sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<TriggerSyncUseCase>>(),
            sp.GetRequiredService<IWorkItemBuffer>(),
            sp.GetRequiredService<IWorkItemStore>()));

        // Interruption policy engine
        services.Configure<InterruptionOptions>(configuration.GetSection(InterruptionOptions.SectionName));
        services.Configure<FocusStateOptions>(configuration.GetSection(FocusStateOptions.SectionName));
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<IFocusStateResolver, SignalBasedFocusStateResolver>();
        services.AddScoped<IInterruptionPolicyEngine, InterruptionPolicyEngine>();
        services.TryAddScoped<IDecisionContextRetriever, Aura.Infrastructure.Adapters.Ingestion.SemanticIndex.NullDecisionContextRetriever>();
        services.TryAddScoped<ILlmDecisionAdvisor, Aura.Infrastructure.Adapters.LlmAdvisor.NullLlmDecisionAdvisor>();
        services.AddScoped<IInterruptionRule, ScoreThresholdRule>();
        services.AddScoped<IInterruptionRule, VipSenderRule>();
        services.AddScoped<IInterruptionRule, KeywordMatchRule>();
        services.AddScoped<IInterruptionRule, DeadlineUrgencyRule>();

        // Focus state override store (SQLite)
        services.AddSingleton<IFocusStateOverrideStore>(sp =>
        {
            var connString = ResolveDbPath(configuration, environment, "Aura");
            var connection = new SqliteConnection(connString);
            connection.Open();
            SqliteFocusStateOverrideStore.InitializeSchema(connection);
            return new SqliteFocusStateOverrideStore(connection);
        });

        // Interruption decision store (SQLite)
        services.AddSingleton<IInterruptionDecisionStore>(sp =>
        {
            var connString = ResolveDbPath(configuration, environment, "Aura");
            var connection = new SqliteConnection(connString);
            connection.Open();
            SqliteInterruptionDecisionStore.InitializeSchema(connection);
            return new SqliteInterruptionDecisionStore(connection);
        });

        // Alert rule store (SQLite)
        services.AddSingleton<IAlertRuleStore>(sp =>
        {
            var connString = ResolveDbPath(configuration, environment, "Aura");
            var connection = new SqliteConnection(connString);
            connection.Open();
            SqliteAlertRuleStore.InitializeSchema(connection);
            return new SqliteAlertRuleStore(connection);
        });

        // Cross-process notification outbox (SQLite)
        services.AddSingleton<INotificationOutboxStore>(sp =>
        {
            var connString = ResolveDbPath(configuration, environment, "Aura");
            var connection = new SqliteConnection(connString);
            connection.Open();
            SqliteNotificationOutboxStore.InitializeSchema(connection);
            return new SqliteNotificationOutboxStore(connection);
        });

        services.AddHealthChecks()
            .AddCheck<QdrantHealthCheck>("qdrant")
            .AddCheck<LlmHealthCheck>("llm")
            .Add(new HealthCheckRegistration(
                "database",
                sp =>
                {
                    var cfg = sp.GetRequiredService<IConfiguration>();
                    var env = sp.GetRequiredService<IHostEnvironment>();
                    var connString = ResolveDbPath(cfg, env, "Aura");
                    return new DbHealthCheck(connString);
                },
                failureStatus: null,
                tags: null));

        // EF Core schema initializer — must run before SeedData to ensure tables exist
        if (string.Equals(configuration["Persistence:Provider"], "EntityFramework", StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<EfSchemaInitializer>();
            // Insert at the beginning so it runs before SeedDataHostedService
            services.AddHostedService(sp => sp.GetRequiredService<EfSchemaInitializer>());
        }

        AddSeedDataIfDevelopment(services, configuration, environment);

        // Demo mode — conditionally registers fallback semantic handlers and DemoService
        services.AddDemoMode(configuration);

        return services;
    }

    /// <summary>
    /// Resolves a SQLite connection string, converting relative Data Source paths
    /// to absolute paths rooted at <see cref="IHostEnvironment.ContentRootPath"/>.
    /// Ensures the parent directory exists.
    /// </summary>
    private static string ResolveDbPath(
        IConfiguration configuration,
        IHostEnvironment environment,
        string connectionStringName)
    {
        var raw = configuration.GetConnectionString(connectionStringName)
                  ?? "Data Source=aura.db";
        var builder = new SqliteConnectionStringBuilder(raw);

        if (!string.IsNullOrEmpty(builder.DataSource) && !Path.IsPathRooted(builder.DataSource))
        {
            var fullPath = Path.GetFullPath(
                Path.Combine(environment.ContentRootPath, builder.DataSource));

            var dir = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            builder.DataSource = fullPath;
        }

        return builder.ConnectionString;
    }

    private static void AddSeedDataIfDevelopment(
        IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        if (!environment.IsDevelopment())
        {
            return;
        }

        // When Demo Mode is enabled, skip SeedData to avoid duplicate data.
        // Demo data is loaded on-demand via demo endpoints and simulation.
        var demoEnabled = configuration.GetValue<bool>("DemoMode:Enabled");
        if (demoEnabled)
        {
            return;
        }

        var seedOptions = new SeedDataOptions();
        configuration.GetSection(SeedDataOptions.SectionName).Bind(seedOptions);

        if (!seedOptions.Enabled)
        {
            return;
        }

        services.Configure<SeedDataOptions>(configuration.GetSection(SeedDataOptions.SectionName));
        services.AddHostedService<SeedDataHostedService>();
    }
}
