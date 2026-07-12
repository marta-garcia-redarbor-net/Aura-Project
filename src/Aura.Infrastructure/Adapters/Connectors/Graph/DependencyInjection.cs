using Aura.Infrastructure.Adapters.GraphConnector;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;

namespace Aura.Infrastructure.Adapters.Connectors.Graph;

/// <summary>
/// DI registration for Graph source providers, MSAL client, and SQLite token cache.
/// All registrations are gated behind <c>GraphConnector:Enabled = true</c>.
/// </summary>
internal static class DependencyInjection
{
    internal static IServiceCollection AddGraphSourceProviders(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var options = new GraphConnectorOptions();
        configuration.GetSection(GraphConnectorOptions.SectionName).Bind(options);

        if (!options.Enabled)
        {
            return services;
        }

        // MSAL SQLite token cache
        services.AddSingleton(sp =>
        {
            var connectionString = configuration.GetConnectionString("TokenCache")
                                   ?? "Data Source=token_cache.db";
            var connection = new SqliteConnection(connectionString);
            connection.Open();
            MsalSqliteTokenCache.InitializeSchema(connection);
            return new MsalSqliteTokenCache(connection);
        });

        // MSAL public client application (delegated flow — no client secret)
        services.AddSingleton<IPublicClientApplication>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<GraphConnectorOptions>>().Value;

            var app = PublicClientApplicationBuilder
                .Create(opts.ClientId)
                .WithTenantId(opts.TenantId)
                .WithRedirectUri(opts.RedirectUri ?? "https://localhost:5001/signin-oidc")
                .Build();

            // Hook SQLite cache into MSAL user token cache (delegated flow).
            // Oid-partitioned: MSAL provides SuggestedCacheKey per-user (includes account identity),
            // so each real user's tokens are stored under a unique key.
            // When SuggestedCacheKey is null (edge case), fall back to a client-scoped key.
            var tokenCache = sp.GetRequiredService<MsalSqliteTokenCache>();

            app.UserTokenCache.SetBeforeAccessAsync(args =>
            {
                var cacheKey = args.SuggestedCacheKey ?? $"msal-user-{opts.ClientId}-unknown";
                var cached = tokenCache.Retrieve(cacheKey);
                if (cached is not null)
                {
                    args.TokenCache.DeserializeMsalV3(cached);
                }
                return Task.CompletedTask;
            });

            app.UserTokenCache.SetAfterAccessAsync(args =>
            {
                if (args.HasStateChanged)
                {
                    var cacheKey = args.SuggestedCacheKey ?? $"msal-user-{opts.ClientId}-unknown";
                    tokenCache.Persist(cacheKey, args.TokenCache.SerializeMsalV3());
                }
                return Task.CompletedTask;
            });

            return app;
        });

        // User token store — OBO-acquired Graph tokens shared with the worker
        services.AddSingleton(sp =>
        {
            var connectionString = configuration.GetConnectionString("TokenCache")
                                   ?? "Data Source=token_cache.db";
            var connection = new SqliteConnection(connectionString);
            connection.Open();
            UserTokenStore.InitializeSchema(connection);
            return new UserTokenStore(connection);
        });

        // Confidential client for On-Behalf-Of token acquisition (API only).
        // Uses the same client ID & secret as the app registration so OBO can
        // exchange the user's JWT for a Graph token with Mail.Read scope.
        // Only registered when a ClientSecret is configured.
        services.AddSingleton<IConfidentialClientApplication>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<GraphConnectorOptions>>().Value;
            if (string.IsNullOrWhiteSpace(opts.ClientSecret))
            {
                // Not configured — return a throw-away placeholder that fails fast.
                return ConfidentialClientApplicationBuilder
                    .Create("placeholder")
                    .WithClientSecret("placeholder")
                    .Build();
            }

            return ConfidentialClientApplicationBuilder
                .Create(opts.ClientId)
                .WithTenantId(opts.TenantId)
                .WithClientSecret(opts.ClientSecret)
                .Build();
        });

        services.AddSingleton<OboTokenService>();

        // Graph client factory
        services.AddSingleton<GraphClientFactory>();
        services.AddSingleton<IGraphClientFactory>(sp => sp.GetRequiredService<GraphClientFactory>());

        return services;
    }
}
