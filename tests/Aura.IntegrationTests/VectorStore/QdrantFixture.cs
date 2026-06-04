using Qdrant.Client;
using Testcontainers.Qdrant;

namespace Aura.IntegrationTests.VectorStore;

/// <summary>
/// Shared Qdrant container fixture. One container per test collection — avoids
/// spinning up a new Docker container per test method.
/// </summary>
public sealed class QdrantFixture : IAsyncLifetime
{
    private readonly QdrantContainer _container = new QdrantBuilder().Build();

    public QdrantClient Client { get; private set; } = null!;
    public string Hostname => _container.Hostname;
    public int GrpcPort => _container.GetMappedPublicPort(QdrantBuilder.QdrantGrpcPort);

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        Client = new QdrantClient(host: Hostname, port: GrpcPort);
    }

    public async Task DisposeAsync()
    {
        Client?.Dispose();
        await _container.DisposeAsync();
    }
}

[CollectionDefinition("Qdrant")]
public class QdrantTestCollection : ICollectionFixture<QdrantFixture>;
