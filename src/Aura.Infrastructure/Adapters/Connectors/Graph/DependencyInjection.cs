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
            // Delegated AcquireTokenSilent uses UserTokenCache, not AppTokenCache.
            var tokenCache = sp.GetRequiredService<MsalSqliteTokenCache>();
            var cacheKey = $"msal-user-{opts.ClientId}";

            app.UserTokenCache.SetBeforeAccessAsync(args =>
            {
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
                    tokenCache.Persist(cacheKey, args.TokenCache.SerializeMsalV3());
                }
                return Task.CompletedTask;
            });

            return app;
        });

        // Graph client factory
        services.AddSingleton<GraphClientFactory>();
        services.AddSingleton<IGraphClientFactory>(sp => sp.GetRequiredService<GraphClientFactory>());

        return services;
    }
}
