using System.ClientModel;
using Aura.Application.Ports;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenAI;
using Polly.Registry;

namespace Aura.Infrastructure.Embedding;

/// <summary>
/// DI registration for the MEAI-based embedding provider adapter.
/// Wires the MEAI pipeline with OpenTelemetry, Polly resilience (via shared builder),
/// options validation, and the <see cref="IEmbeddingProvider"/> adapter.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers MEAI-backed embedding services.
    /// Binds <see cref="EmbeddingProviderOptions"/> from the "EmbeddingProvider" configuration section.
    /// Establishes the Polly resilience pipeline via <see cref="EmbeddingResiliencePolicyBuilder"/>.
    /// </summary>
    public static IServiceCollection AddMeaiEmbeddingProvider(
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

        // MEAI embedding generator pipeline: OpenAI client → OTel → registered as singleton
        services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(sp =>
        {
            var embeddingOptions = sp.GetRequiredService<IOptions<EmbeddingProviderOptions>>().Value;

            // OpenAI SDK v2 supports Azure OpenAI via Endpoint override
            var clientOptions = new OpenAIClientOptions
            {
                Endpoint = new Uri(embeddingOptions.Endpoint)
            };

            var apiKey = configuration[$"{EmbeddingProviderOptions.SectionName}:ApiKey"] ?? "";
            var client = new OpenAIClient(new ApiKeyCredential(apiKey), clientOptions);

            IEmbeddingGenerator<string, Embedding<float>> generator =
                client.GetEmbeddingClient(embeddingOptions.DeploymentName).AsIEmbeddingGenerator();

            // Build pipeline with OTel middleware
            var pipeline = new EmbeddingGeneratorBuilder<string, Embedding<float>>(generator)
                .UseOpenTelemetry()
                .Build();

            return pipeline;
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
}
