using Aura.Application;
using Aura.Infrastructure;
using Aura.Workers;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();

// Application + Infrastructure unified DI
builder.Services.AddAuraApplication();
builder.Services.AddAuraInfrastructure(builder.Configuration, builder.Environment);
builder.Services.AddHostedService<SemanticIndexSyncWorker>();

var host = builder.Build();
host.Run();

// Expose the Program class for integration testing (WebApplicationFactory / ServiceCollection)
public partial class Program { }
