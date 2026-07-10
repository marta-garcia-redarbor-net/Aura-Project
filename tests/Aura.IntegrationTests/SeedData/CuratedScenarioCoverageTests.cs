using Aura.Application;
using Aura.Application.Models;
using Aura.Application.Ports;
using Aura.Domain.WorkItems;
using Aura.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace Aura.IntegrationTests.SeedData;

public class CuratedScenarioCoverageTests
{
    private sealed class FakeHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "Aura.IntegrationTests";
        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private sealed class StubDecisionContextRetriever : IDecisionContextRetriever
    {
        public Task<IReadOnlyList<DecisionContextItem>> RetrieveAsync(WorkItem item, CancellationToken ct)
            => Task.FromResult<IReadOnlyList<DecisionContextItem>>(
            [
                new DecisionContextItem(
                    CanonicalSourceId: $"ctx-{item.ExternalId}",
                    ContentSnippet: $"Context for {item.Title}",
                    SourceType: "ActivityMemory",
                    RelevanceScore: 0.87)
            ]);
    }

    private sealed class StubAdvisor : ILlmDecisionAdvisor
    {
        public Task<AdvisoryResponse> EvaluateAsync(AdvisoryRequest request, CancellationToken ct)
            => Task.FromResult(new AdvisoryResponse(
                SuggestedVerdict: request.DeterministicVerdict,
                Rationale: "Curated advisor rationale for seeded item.",
                GuardrailOutcome: "confirmed",
                FailureReason: null,
                Confidence: 0.91));
    }

    [Fact]
    public async Task SeedData_CuratedScenarios_CoverInterruptQueueAndDeferPerSource()
    {
        using var provider = BuildProvider(withTraceStubs: false);
        await StartSeedHostedServiceAsync(provider);

        var decisionStore = provider.GetRequiredService<IInterruptionDecisionStore>();
        var decisions = await decisionStore.QueryAsync(page: 1, pageSize: 300, cancellationToken: CancellationToken.None);

        AssertCoverage(decisions.Items, "TeamsMessage");
        AssertCoverage(decisions.Items, "OutlookEmail");
        AssertCoverage(decisions.Items, "PrReview");
    }

    [Fact]
    public async Task SeedData_CuratedScenarios_PersistTraceWhenAdvisoryAndContextAreEnabled()
    {
        using var provider = BuildProvider(withTraceStubs: true);
        await StartSeedHostedServiceAsync(provider);

        var decisionStore = provider.GetRequiredService<IInterruptionDecisionStore>();
        var decisions = await decisionStore.QueryAsync(page: 1, pageSize: 300, cancellationToken: CancellationToken.None);

        var curated = decisions.Items
            .Where(x => x.Title.StartsWith("[Curated]", StringComparison.Ordinal))
            .ToList();

        Assert.NotEmpty(curated);
        Assert.All(curated, item =>
        {
            Assert.False(string.IsNullOrWhiteSpace(item.LlmRationale));
            Assert.NotNull(item.RetrievedSemanticContext);
            Assert.NotEmpty(item.RetrievedSemanticContext!);
            Assert.All(item.RetrievedSemanticContext!, ctx =>
            {
                Assert.False(string.IsNullOrWhiteSpace(ctx.CanonicalSourceId));
                Assert.False(string.IsNullOrWhiteSpace(ctx.ContentSnippet));
                Assert.True(ctx.RelevanceScore > 0);
            });
        });
    }

    private static void AssertCoverage(IReadOnlyList<InterruptionDecisionRecord> decisions, string sourceType)
    {
        var curatedForSource = decisions
            .Where(x => x.SourceType == sourceType)
            .Where(x => x.Title.StartsWith("[Curated]", StringComparison.Ordinal))
            .Select(x => x.Decision)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Contains("INTERRUPT", curatedForSource);
        Assert.Contains("QUEUE", curatedForSource);
        Assert.Contains("DEFER", curatedForSource);
    }

    private static async Task StartSeedHostedServiceAsync(IServiceProvider provider)
    {
        var hostedServices = provider.GetServices<IHostedService>();
        var seedHostedService = hostedServices.Single(x => x.GetType().Name == "SeedDataHostedService");
        await seedHostedService.StartAsync(CancellationToken.None);
    }

    private static ServiceProvider BuildProvider(bool withTraceStubs)
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aura-seed-curated-{Guid.NewGuid():N}.db");
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Aura"] = $"Data Source={dbPath}",
                ["ConnectionStrings:SemanticOutbox"] = "Data Source=:memory:",
                ["EmbeddingProvider:Provider"] = "OpenAI",
                ["EmbeddingProvider:Endpoint"] = "https://test.openai.azure.com",
                ["EmbeddingProvider:DeploymentName"] = "test-model",
                ["EmbeddingProvider:ApiKey"] = "fake-key",
                ["Qdrant:Host"] = "localhost",
                ["Qdrant:GrpcPort"] = "6334",
                ["UseEntraId"] = "false",
                ["SeedData:Enabled"] = "true",
                ["DemoMode:Enabled"] = "false",
                ["Persistence:Provider"] = "Sqlite",
                ["InterruptionOptions:UrgentThreshold"] = "6",
                ["InterruptionOptions:DeadlineWindowHours"] = "2"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuraApplication();
        services.AddAuraInfrastructure(config, new FakeHostEnvironment());

        if (withTraceStubs)
        {
            services.AddScoped<IDecisionContextRetriever, StubDecisionContextRetriever>();
            services.AddScoped<ILlmDecisionAdvisor, StubAdvisor>();
        }

        return services.BuildServiceProvider();
    }
}
