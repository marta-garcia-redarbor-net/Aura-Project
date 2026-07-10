using Aura.Application.Ports;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OllamaSharp;

namespace Aura.Infrastructure.Adapters.LlmAdvisor;

internal static class DependencyInjection
{
    internal sealed record AdvisorChatSettings(string Provider, string? Endpoint, string? ModelId);

    internal static IServiceCollection AddLlmDecisionAdvisor(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<LlmAdvisorOptions>(configuration.GetSection(LlmAdvisorOptions.SectionName));

        var enabled = configuration.GetValue<bool>($"{LlmAdvisorOptions.SectionName}:Enabled");
        if (enabled)
        {
            var chatSettings = ResolveAdvisorChatSettings(configuration);

            services.AddScoped<IChatClient>(_ =>
            {
                if (string.Equals(chatSettings.Provider, "Ollama", StringComparison.OrdinalIgnoreCase)
                    && !string.IsNullOrWhiteSpace(chatSettings.Endpoint)
                    && !string.IsNullOrWhiteSpace(chatSettings.ModelId))
                {
                    return new OllamaApiClient(chatSettings.Endpoint, chatSettings.ModelId);
                }

                return new UnavailableChatClient();
            });

            services.AddScoped<ILlmDecisionAdvisor, MeaiLlmDecisionAdvisorAdapter>();
        }
        else
        {
            services.AddScoped<ILlmDecisionAdvisor, NullLlmDecisionAdvisor>();
        }

        return services;
    }

    internal static AdvisorChatSettings ResolveAdvisorChatSettings(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var provider = configuration[$"{LlmAdvisorOptions.SectionName}:Provider"]
                       ?? configuration["EmbeddingProvider:Provider"]
                       ?? "Ollama";

        var endpoint = configuration[$"{LlmAdvisorOptions.SectionName}:Endpoint"]
                       ?? configuration["EmbeddingProvider:Endpoint"];

        var modelId = configuration[$"{LlmAdvisorOptions.SectionName}:ModelId"];

        return new AdvisorChatSettings(provider, endpoint, modelId);
    }

    private sealed class UnavailableChatClient : IChatClient
    {
        public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
            => Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, string.Empty)));

        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            yield break;
        }

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public void Dispose()
        {
        }
    }
}
