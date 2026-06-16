using Aura.Application.Ports;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Aura.Infrastructure.Adapters.Ingestion.SemanticOutbox;

/// <summary>
/// DI registration for the SQLite-backed semantic outbox adapter.
/// </summary>
internal static class DependencyInjection
{
    /// <summary>
    /// Registers the SQLite semantic outbox repository.
    /// </summary>
    internal static IServiceCollection AddSemanticOutboxAdapter(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddSingleton<ISemanticOutboxRepository>(sp =>
        {
            var connectionString = configuration.GetConnectionString("SemanticOutbox")
                                   ?? "Data Source=semantic_outbox.db";
            var connection = new SqliteConnection(connectionString);
            connection.Open();
            SqliteSemanticOutboxRepository.InitializeSchema(connection);
            return new SqliteSemanticOutboxRepository(connection);
        });

        return services;
    }
}
