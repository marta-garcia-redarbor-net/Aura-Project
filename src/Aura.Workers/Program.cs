using Aura.Application;
using Aura.Infrastructure;
using Aura.Workers;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;

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
    builder.Services.Configure<ConnectorExecutionOptions>(builder.Configuration.GetSection("ConnectorExecution"));
    builder.Services.AddHostedService<Worker>();
    builder.Services.AddAuraInfrastructure(builder.Configuration, builder.Environment);
    builder.Services.AddAuraEntityFrameworkCore(builder.Configuration);

    // OpenTelemetry: sends traces, metrics, and logs to Azure Application Insights
    builder.Services.AddOpenTelemetry().UseAzureMonitor();

    builder.Services.AddHostedService<SemanticIndexSyncWorker>();
    builder.Services.AddHostedService<ConnectorExecutionWorker>();
    builder.Services.AddHostedService<MorningSummarySchedulingWorker>();
    builder.Services.AddHostedService<HelloKernelWorker>();

    // MSAL for delegated token cache access (Workers need to resolve user oid)
    var azureAd = builder.Configuration.GetSection("AzureAd");
    var clientId = azureAd["ClientId"];
    var tenantId = azureAd["TenantId"];
    if (!string.IsNullOrEmpty(clientId) && !string.IsNullOrEmpty(tenantId))
    {
        builder.Services.AddSingleton<IPublicClientApplication>(sp =>
            PublicClientApplicationBuilder
                .Create(clientId)
                .WithAuthority(AzureCloudInstance.AzurePublic, tenantId)
                .WithRedirectUri("http://localhost:5000/authentication/login-callback")
                .Build());
    }
}

var host = builder.Build();
host.Run();

// Expose the Program class for integration testing (WebApplicationFactory / ServiceCollection)
public partial class Program { }
