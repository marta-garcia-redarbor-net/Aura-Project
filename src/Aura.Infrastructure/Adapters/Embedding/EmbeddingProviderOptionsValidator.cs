using Microsoft.Extensions.Options;

namespace Aura.Infrastructure.Adapters.Ingestion.Embedding;

/// <summary>
/// Fail-fast validation for <see cref="EmbeddingProviderOptions"/>.
/// Registered via <see cref="OptionsServiceCollectionExtensions"/> to run at first resolve.
/// </summary>
public sealed class EmbeddingProviderOptionsValidator : IValidateOptions<EmbeddingProviderOptions>
{
    public ValidateOptionsResult Validate(string? name, EmbeddingProviderOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.Endpoint))
            failures.Add("Endpoint is required.");

        if (string.IsNullOrWhiteSpace(options.DeploymentName))
            failures.Add("DeploymentName is required.");

        if (options.MaxBatchSize < 1)
            failures.Add("MaxBatchSize must be >= 1.");

        if (options.MaxTokensPerBatch < 1)
            failures.Add("MaxTokensPerBatch must be >= 1.");

        if (options.TimeoutSeconds < 1)
            failures.Add("TimeoutSeconds must be >= 1.");

        if (options.MaxRetries < 0)
            failures.Add("MaxRetries must be >= 0.");

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}
