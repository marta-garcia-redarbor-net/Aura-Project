using Aura.Infrastructure.Embedding;
using Aura.Infrastructure.VectorStore;
using Aura.Workers;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();

// Semantic index sync pipeline
builder.Services.AddQdrantSemanticIndex(builder.Configuration);
builder.Services.AddMeaiEmbeddingProvider(builder.Configuration);
builder.Services.AddHostedService<SemanticIndexSyncWorker>();

var host = builder.Build();
host.Run();

// Expose the Program class for integration testing (WebApplicationFactory / ServiceCollection)
public partial class Program { }
