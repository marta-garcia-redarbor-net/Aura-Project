using Aura.Application;
using Aura.Infrastructure;
using Aura.Workers;

var kernelOnly = args.Contains("--kernel-only");

var builder = Host.CreateApplicationBuilder(args);

// Application services are always registered (kernel + domain abstractions)
builder.Services.AddAuraApplication();

if (kernelOnly)
{
    // Kernel-only mode: skip infrastructure adapters that require external config
    // (EmbeddingProvider, Qdrant, ConnectionStrings). Only the kernel pipeline runs.
    builder.Services.AddHostedService<HelloKernelWorker>();
}
else
{
    // Full mode: all infrastructure adapters + background workers
    builder.Services.AddHostedService<Worker>();
    builder.Services.AddAuraInfrastructure(builder.Configuration);
    builder.Services.AddHostedService<SemanticIndexSyncWorker>();
    builder.Services.AddHostedService<HelloKernelWorker>();
}

var host = builder.Build();
host.Run();

// Expose the Program class for integration testing (WebApplicationFactory / ServiceCollection)
public partial class Program { }
