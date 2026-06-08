using Aura.Application.Ports;
using Aura.Application.Services;
using Aura.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Qdrant.Client;

namespace Aura.Infrastructure.VectorStore;

/// <summary>
/// DI registration for the Qdrant semantic index infrastructure.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers Qdrant-backed semantic index services and supporting infrastructure.
    /// Binds <see cref="QdrantOptions"/> from the "Qdrant" configuration section.
    /// Also registers <see cref="ISemanticChunkExtractor"/> and <see cref="ISemanticOutboxRepository"/>.
    /// </summary>
    public static IServiceCollection AddQdrantSemanticIndex(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<QdrantOptions>(configuration.GetSection(QdrantOptions.SectionName));

        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<QdrantOptions>>().Value;
            return new QdrantClient(
                host: options.Host,
                port: options.GrpcPort,
                apiKey: options.ApiKey);
        });

        services.AddScoped<ISemanticIndexWriter, QdrantSemanticIndexAdapter>();
        services.AddScoped<ISemanticContextRetriever, QdrantSemanticContextAdapter>();

        // Chunk extractor — Application-layer service (no external SDK dependency)
        services.AddSingleton<ISemanticChunkExtractor, BasicSemanticChunkExtractor>();

        // Outbox repository — SQLite for V1
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
