using Aura.Infrastructure.Embedding;
using Aura.Infrastructure.VectorStore;
using Aura.Application;
using Aura.Infrastructure;
using Aura.Workers;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();

// Semantic index sync pipeline
builder.Services.AddQdrantSemanticIndex(builder.Configuration);
<<<<<<< Updated upstream
builder.Services.AddMeaiEmbeddingProvider(builder.Configuration);
=======
// Application + Infrastructure unified DI
builder.Services.AddAuraApplication();
builder.Services.AddAuraInfrastructure(builder.Configuration);
>>>>>>> Stashed changes
builder.Services.AddHostedService<SemanticIndexSyncWorker>();

var host = builder.Build();
host.Run();

// Expose the Program class for integration testing (WebApplicationFactory / ServiceCollection)
public partial class Program { }
