using Aura.Application;
using Aura.Application.Ports;
using Aura.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace Aura.IntegrationTests.Triage;

public sealed class InterruptionPolicyCompositionTests
{
    [Fact]
    public void AddAuraApplicationAndInfrastructure_ResolvesPriorityScoringAndPolicyServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuraApplication();
        services.AddAuraInfrastructure(CreateConfig(), new FakeHostEnvironment());

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var scorer = scope.ServiceProvider.GetRequiredService<IPriorityScoringService>();
        var policyProvider = scope.ServiceProvider.GetRequiredService<IUserTriagePolicyProvider>();
        var engine = scope.ServiceProvider.GetRequiredService<IInterruptionPolicyEngine>();
        var resolver = scope.ServiceProvider.GetRequiredService<IFocusStateResolver>();

        Assert.NotNull(scorer);
        Assert.NotNull(policyProvider);
        Assert.NotNull(engine);
        Assert.NotNull(resolver);
    }

    private static IConfiguration CreateConfig()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aura-triage-{Guid.NewGuid():N}.db");

        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Qdrant:Host"] = "localhost",
                ["Qdrant:GrpcPort"] = "6334",
                ["Qdrant:VectorSize"] = "768",
                ["ConnectionStrings:SemanticOutbox"] = "Data Source=:memory:",
                ["ConnectionStrings:Aura"] = $"Data Source={dbPath}",
                ["EmbeddingProvider:Endpoint"] = "https://test.openai.azure.com",
                ["EmbeddingProvider:DeploymentName"] = "text-embedding-ada-002",
                ["EmbeddingProvider:ApiKey"] = "test-key",
                ["MorningSummary:TimezoneId"] = "UTC",
                ["MorningSummary:TargetLocalTime"] = "09:00"
            })
            .Build();
    }

    private sealed class FakeHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "Aura.Triage.Tests";
        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
