using Aura.Application;
using Aura.Infrastructure;
using Aura.Workers;
using Microsoft.Extensions.Configuration;

var kernelOnly = args.Contains("--kernel-only");

var builder = Host.CreateApplicationBuilder(args);

// Load user secrets (GraphConnector:ClientId, GraphConnector:TenantId, etc.)
// HostApplicationBuilder does NOT load secrets automatically — only WebApplicationBuilder does.
builder.Configuration.AddUserSecrets<Program>();

// Required for AuthorizationPolicyCache (registered transitively by AddAuthentication
// in AddIdentityAdapter) which depends on EndpointDataSource from ASP.NET Core routing.
// The worker does not serve HTTP, but the service registration is needed for DI validation.
builder.Services.AddRouting();

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
    builder.Services.Configure<ConnectorExecutionOptions>(builder.Configuration.GetSection("ConnectorExecution"));
    builder.Services.AddHostedService<Worker>();
    builder.Services.AddAuraInfrastructure(builder.Configuration, builder.Environment);
    builder.Services.AddAuraEntityFrameworkCore(builder.Configuration);
    builder.Services.AddHostedService<SemanticIndexSyncWorker>();
    builder.Services.AddHostedService<ConnectorExecutionWorker>();
    builder.Services.AddHostedService<MorningSummarySchedulingWorker>();
    builder.Services.AddHostedService<HelloKernelWorker>();
}

var host = builder.Build();
host.Run();

// Expose the Program class for integration testing (WebApplicationFactory / ServiceCollection)
public partial class Program { }
