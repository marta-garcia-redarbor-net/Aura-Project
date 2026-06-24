using Aura.Application.Ports;
using Aura.Infrastructure.Adapters.Connectors.Outlook;
using Aura.Infrastructure.Adapters.Connectors.Teams;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Aura.Infrastructure.Adapters.WorkItems;

internal static class DependencyInjection
{
    internal static IServiceCollection AddWorkItems(
        this IServiceCollection services,
        IConfiguration? configuration = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<SqliteWorkItemStore>(sp =>
        {
            var connectionString = configuration?.GetConnectionString("WorkItems")
                                   ?? "Data Source=workitems.db";
            var connection = new SqliteConnection(connectionString);
            connection.Open();
            SqliteWorkItemStore.InitializeSchema(connection);
            return new SqliteWorkItemStore(connection);
        });
        services.AddSingleton<IWorkItemStore>(sp => sp.GetRequiredService<SqliteWorkItemStore>());
        services.AddSingleton<IWorkItemReader>(sp => sp.GetRequiredService<SqliteWorkItemStore>());
        services.AddScoped<IWorkItemBuffer, InMemoryWorkItemBuffer>();
        services.AddSingleton<TeamsWorkItemMapper>();
        services.AddSingleton<OutlookWorkItemMapper>();

        return services;
    }
}
