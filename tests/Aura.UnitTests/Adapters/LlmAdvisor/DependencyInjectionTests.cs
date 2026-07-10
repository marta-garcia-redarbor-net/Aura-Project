using Aura.Application.Ports;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Aura.UnitTests.Adapters.LlmAdvisor;

public class DependencyInjectionTests
{
    [Fact]
    public void ResolveAdvisorChatSettings_DoesNotUseEmbeddingDeploymentNameAsChatModel()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["EmbeddingProvider:Provider"] = "Ollama",
                ["EmbeddingProvider:Endpoint"] = "http://localhost:11434",
                ["EmbeddingProvider:DeploymentName"] = "nomic-embed-text"
            })
            .Build();

        var settings = Aura.Infrastructure.Adapters.LlmAdvisor.DependencyInjection.ResolveAdvisorChatSettings(config);

        Assert.Equal("Ollama", settings.Provider);
        Assert.Equal("http://localhost:11434", settings.Endpoint);
        Assert.Null(settings.ModelId);
    }

    [Fact]
    public void ResolveAdvisorChatSettings_PrefersLlmAdvisorModelIdAndEndpoint()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["EmbeddingProvider:Provider"] = "Ollama",
                ["EmbeddingProvider:Endpoint"] = "http://localhost:11434",
                ["EmbeddingProvider:DeploymentName"] = "nomic-embed-text",
                ["LlmAdvisor:Endpoint"] = "http://llm-service:11434",
                ["LlmAdvisor:ModelId"] = "llama3.1:8b-instruct"
            })
            .Build();

        var settings = Aura.Infrastructure.Adapters.LlmAdvisor.DependencyInjection.ResolveAdvisorChatSettings(config);

        Assert.Equal("Ollama", settings.Provider);
        Assert.Equal("http://llm-service:11434", settings.Endpoint);
        Assert.Equal("llama3.1:8b-instruct", settings.ModelId);
    }

    [Fact]
    public void AddLlmDecisionAdvisor_WhenDisabled_RegistersNullAdvisor()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["LlmAdvisor:Enabled"] = "false"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();

        Aura.Infrastructure.Adapters.LlmAdvisor.DependencyInjection.AddLlmDecisionAdvisor(services, config);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var advisor = scope.ServiceProvider.GetRequiredService<ILlmDecisionAdvisor>();

        Assert.IsType<Aura.Infrastructure.Adapters.LlmAdvisor.NullLlmDecisionAdvisor>(advisor);
    }

    [Fact]
    public void AddLlmDecisionAdvisor_WhenEnabledWithOllama_RegistersChatClientAndMeaiAdvisor()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["LlmAdvisor:Enabled"] = "true",
                ["EmbeddingProvider:Provider"] = "Ollama",
                ["EmbeddingProvider:Endpoint"] = "http://localhost:11434",
                ["LlmAdvisor:ModelId"] = "llama3.1:8b-instruct"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();

        Aura.Infrastructure.Adapters.LlmAdvisor.DependencyInjection.AddLlmDecisionAdvisor(services, config);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var chatClient = scope.ServiceProvider.GetRequiredService<IChatClient>();
        var advisor = scope.ServiceProvider.GetRequiredService<ILlmDecisionAdvisor>();

        Assert.IsType<OllamaSharp.OllamaApiClient>(chatClient);
        Assert.IsType<Aura.Infrastructure.Adapters.LlmAdvisor.MeaiLlmDecisionAdvisorAdapter>(advisor);
    }

    [Fact]
    public void AddLlmDecisionAdvisor_WhenEnabledWithoutModelId_RegistersUnavailableChatClient()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["LlmAdvisor:Enabled"] = "true",
                ["EmbeddingProvider:Provider"] = "Ollama",
                ["EmbeddingProvider:Endpoint"] = "http://localhost:11434"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();

        Aura.Infrastructure.Adapters.LlmAdvisor.DependencyInjection.AddLlmDecisionAdvisor(services, config);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var chatClient = scope.ServiceProvider.GetRequiredService<IChatClient>();

        Assert.NotEqual(typeof(OllamaSharp.OllamaApiClient), chatClient.GetType());
    }
}
