using System.ClientModel;
using Aura.Application.Ports;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OllamaSharp;
using OpenAI;
using Polly.Registry;

namespace Aura.Infrastructure.Adapters.Ingestion.Embedding;

/// <summary>
/// DI registration for the MEAI-based embedding provider adapter.
/// Wires the MEAI pipeline with OpenTelemetry, Polly resilience (via shared builder),
/// options validation, and the <see cref="IEmbeddingProvider"/> adapter.
/// </summary>
internal static class DependencyInjection
{
    /// <summary>
    /// Registers MEAI-backed embedding services.
    /// Binds <see cref="EmbeddingProviderOptions"/> from the "EmbeddingProvider" configuration section.
    /// Establishes the Polly resilience pipeline via <see cref="EmbeddingResiliencePolicyBuilder"/>.
    /// </summary>
    internal static IServiceCollection AddEmbeddingAdapter(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // Options + fail-fast validation
        services.Configure<EmbeddingProviderOptions>(
            configuration.GetSection(EmbeddingProviderOptions.SectionName));
        services.AddSingleton<IValidateOptions<EmbeddingProviderOptions>,
            EmbeddingProviderOptionsValidator>();

        // Resolve options for resilience setup
        var options = configuration
            .GetSection(EmbeddingProviderOptions.SectionName)
            .Get<EmbeddingProviderOptions>()
            ?? throw new InvalidOperationException(
                $"Configuration section '{EmbeddingProviderOptions.SectionName}' is missing or empty.");

        // Polly resilience pipeline: retry on transient HTTP errors + timeout
        services.AddEmbeddingResiliencePolicy(options);

        // MEAI embedding generator pipeline: provider-specific inner generator → OTel
        services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(sp =>
        {
            var embeddingOptions = sp.GetRequiredService<IOptions<EmbeddingProviderOptions>>().Value;

            IEmbeddingGenerator<string, Embedding<float>> innerGenerator = embeddingOptions.Provider switch
            {
                "OpenAI" => CreateOpenAIGenerator(configuration, embeddingOptions),
                "Ollama" => CreateOllamaGenerator(embeddingOptions),
                _ => throw new InvalidOperationException(
                    $"Unsupported embedding provider: '{embeddingOptions.Provider}'. " +
                    $"Supported values: 'OpenAI', 'Ollama'.")
            };

            // Same OTel middleware for both providers
            return new EmbeddingGeneratorBuilder<string, Embedding<float>>(innerGenerator)
                .UseOpenTelemetry()
                .Build();
        });

        // Resolve the named resilience pipeline for injection into the adapter
        services.AddSingleton(sp =>
        {
            var pipelineProvider = sp.GetRequiredService<ResiliencePipelineProvider<string>>();
            return pipelineProvider.GetPipeline(EmbeddingResiliencePolicyBuilder.PipelineKey);
        });

        // Our port adapter
        services.AddSingleton<IEmbeddingProvider, MeaiEmbeddingProvider>();

        return services;
    }

    private static IEmbeddingGenerator<string, Embedding<float>> CreateOpenAIGenerator(
        IConfiguration configuration, EmbeddingProviderOptions options)
    {
        var clientOptions = new OpenAIClientOptions
        {
            Endpoint = new Uri(options.Endpoint)
        };
        var apiKey = configuration[$"{EmbeddingProviderOptions.SectionName}:ApiKey"] ?? "";
        var client = new OpenAIClient(new ApiKeyCredential(apiKey), clientOptions);
        return client.GetEmbeddingClient(options.DeploymentName).AsIEmbeddingGenerator();
    }

    private static IEmbeddingGenerator<string, Embedding<float>> CreateOllamaGenerator(
        EmbeddingProviderOptions options)
    {
        // OllamaApiClient directly implements IEmbeddingGenerator<string, Embedding<float>>
        // No .AsIEmbeddingGenerator() call needed (unlike OpenAI)
        return new OllamaApiClient(options.Endpoint, options.DeploymentName);
    }
}
