using Aura.Infrastructure.Adapters.Embedding;
using Microsoft.Extensions.Options;

namespace Aura.UnitTests.Infrastructure;

public class EmbeddingProviderOptionsValidatorTests
{
    private readonly EmbeddingProviderOptionsValidator _validator = new();

    private static EmbeddingProviderOptions ValidOptions() => new()
    {
        Endpoint = "https://test.openai.azure.com",
        DeploymentName = "text-embedding-ada-002",
        MaxBatchSize = 16,
        MaxTokensPerBatch = 8192,
        TimeoutSeconds = 30,
        MaxRetries = 3
    };

    [Fact]
    public void Validate_ValidOptions_Succeeds()
    {
        var result = _validator.Validate(null, ValidOptions());
        Assert.True(result.Succeeded, result.FailureMessage);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_MissingEndpoint_Fails(string? endpoint)
    {
        var options = ValidOptions();
        options.Endpoint = endpoint!;
        var result = _validator.Validate(null, options);
        Assert.True(result.Failed);
        Assert.Contains("Endpoint", result.FailureMessage);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_MissingDeploymentName_Fails(string? name)
    {
        var options = ValidOptions();
        options.DeploymentName = name!;
        var result = _validator.Validate(null, options);
        Assert.True(result.Failed);
        Assert.Contains("DeploymentName", result.FailureMessage);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_InvalidMaxBatchSize_Fails(int batchSize)
    {
        var options = ValidOptions();
        options.MaxBatchSize = batchSize;
        var result = _validator.Validate(null, options);
        Assert.True(result.Failed);
        Assert.Contains("MaxBatchSize", result.FailureMessage);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Validate_InvalidMaxTokensPerBatch_Fails(int tokens)
    {
        var options = ValidOptions();
        options.MaxTokensPerBatch = tokens;
        var result = _validator.Validate(null, options);
        Assert.True(result.Failed);
        Assert.Contains("MaxTokensPerBatch", result.FailureMessage);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_InvalidTimeoutSeconds_Fails(int timeout)
    {
        var options = ValidOptions();
        options.TimeoutSeconds = timeout;
        var result = _validator.Validate(null, options);
        Assert.True(result.Failed);
        Assert.Contains("TimeoutSeconds", result.FailureMessage);
    }

    [Fact]
    public void Validate_NegativeMaxRetries_Fails()
    {
        var options = ValidOptions();
        options.MaxRetries = -1;
        var result = _validator.Validate(null, options);
        Assert.True(result.Failed);
        Assert.Contains("MaxRetries", result.FailureMessage);
    }

    [Fact]
    public void Validate_ZeroMaxRetries_Succeeds()
    {
        var options = ValidOptions();
        options.MaxRetries = 0;
        var result = _validator.Validate(null, options);
        Assert.True(result.Succeeded, result.FailureMessage);
    }

    [Fact]
    public void Options_DefaultValues_AreReasonable()
    {
        var options = new EmbeddingProviderOptions
        {
            Endpoint = "https://test.openai.azure.com",
            DeploymentName = "model"
        };

        Assert.Equal(16, options.MaxBatchSize);
        Assert.Equal(8192, options.MaxTokensPerBatch);
        Assert.Equal(30, options.TimeoutSeconds);
        Assert.Equal(3, options.MaxRetries);
    }
}
