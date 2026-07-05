# Design: Ollama DI Switch for Embeddings

## Technical Approach

Make the embedding provider switchable at DI composition time via config. `MeaiEmbeddingProvider` stays untouched — it receives `IEmbeddingGenerator<string, Embedding<float>>` regardless of the underlying provider. The switch is purely in `DependencyInjection.cs`: a `switch` expression on `EmbeddingProviderOptions.Provider` that selects either the existing Azure OpenAI pipeline or a new Ollama pipeline. Both branches share the same OTel middleware (`EmbeddingGeneratorBuilder.UseOpenTelemetry()`) and the same Polly resilience pipeline registered before the branch. Backward compatible: `Provider` defaults to `"OpenAI"`.

## Architecture Decisions

| Option | Tradeoffs | Decision |
|--------|-----------|----------|
| Strategy pattern (factory interface + impls) | More files, testable in isolation, overkill for 2 variants | Rejected — switch in DI is simpler |
| Separate DI extension per provider | Appears cleaner, but callers must know which to call and compose OTel themselves | Rejected — single entry point guarantees consistent OTel/resilience |
| `switch` expression in the singleton factory | One file changes, branch is explicit, future providers are one `case` away | **Chosen** |

### Decision: Provider Property Not `required`

**Choice**: `Provider` will be a regular property with default `"OpenAI"`, NOT `required`.
**Alternatives considered**: Making it `required` forces all existing configs to add a key.
**Rationale**: Existing deployments without `Provider` in config must keep working. A non-required property with a default achieves this via standard options binding.

### Decision: Ollama Model Uses Existing `DeploymentName`

**Choice**: Reuse the existing `DeploymentName` property for the Ollama model name.
**Alternatives considered**: Add a separate `ModelName` property.
**Rationale**: `DeploymentName` is already required and semantically represents "which model to call" for both providers. Avoids adding another required field.

### Decision: `ApiKey` Not Validated for Ollama

**Choice**: Validator will NOT check `ApiKey` — it's only read when `Provider == "OpenAI"`.
**Alternatives considered**: Validate `ApiKey` is present for OpenAI but not for Ollama.
**Rationale**: Simpler validator. If someone configures Ollama without an `ApiKey` but has a leftover `ApiKey` in their config file, it's harmlessly ignored. The `ApiKey` config read in the OpenAI branch uses `?? ""` which handles absence gracefully.

## Data Flow

```
Config: Provider = "OpenAI"                            Config: Provider = "Ollama"
         │                                                     │
         ▼                                                     ▼
  OpenAIClient(endpoint, apiKey)                        OllamaApiClient(endpoint)
         │                                                     │
         ▼                                                     ▼
  GetEmbeddingClient(deployment).AsIEmbeddingGenerator()  .AsIEmbeddingGenerator()
         │                                                     │
         └─────────── Both share ──────────────────────────────┘
                             │
                             ▼
              EmbeddingGeneratorBuilder
                  .UseOpenTelemetry()
                  .Build()
                             │
                             ▼
                  MeaiEmbeddingProvider
                  (IEmbeddingProvider)
```

## File Changes

| File | Action | Description |
|------|--------|-------------|
| `src/Aura.Infrastructure/Aura.Infrastructure.csproj` | Modify | Add `<PackageReference Include="Microsoft.Extensions.AI.Ollama" Version="10.6.0" />` |
| `src/Aura.Infrastructure/Adapters/Embedding/EmbeddingProviderOptions.cs` | Modify | Add `Provider` property (`string` with `"OpenAI"` default) |
| `src/Aura.Infrastructure/Adapters/Embedding/EmbeddingProviderOptionsValidator.cs` | Modify | Validate `Provider` is `"OpenAI"` or `"Ollama"` |
| `src/Aura.Infrastructure/Adapters/Embedding/DependencyInjection.cs` | Modify | Branch on `options.Provider` — add Ollama pipeline composition |
| `src/Aura.Workers/appsettings.Development.json` | Modify | Add `EmbeddingProvider` section for Ollama (local dev) |
| `tests/Aura.UnitTests/Infrastructure/EmbeddingDependencyInjectionTests.cs` | Modify | Add `Provider: "OpenAI"` to existing test configs |
| `tests/Aura.UnitTests/Infrastructure/EmbeddingProviderOptionsValidatorTests.cs` | Modify | Add Provider validation tests |
| `tests/Aura.IntegrationTests/Embedding/EmbeddingResilienceTests.cs` | Modify | Add `Provider: "OpenAI"` to test config |
| `tests/Aura.IntegrationTests/Workers/WorkersHostCompositionTests.cs` | Modify | Add `Provider: "OpenAI"` to test config |

## Interfaces / Contracts

### EmbeddingProviderOptions (modified)

```csharp
public sealed class EmbeddingProviderOptions
{
    public const string SectionName = "EmbeddingProvider";

    /// <summary>Provider selector: "OpenAI" (default) or "Ollama".</summary>
    public string Provider { get; set; } = "OpenAI";

    // Existing properties remain unchanged
    public required string Endpoint { get; set; }
    public required string DeploymentName { get; set; }
    public int MaxBatchSize { get; set; } = 16;
    public int MaxTokensPerBatch { get; set; } = 8192;
    public int TimeoutSeconds { get; set; } = 30;
    public int MaxRetries { get; set; } = 3;
}
```

### DI Branching Pattern (DependencyInjection.cs)

```csharp
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

// Keep these as private static methods in DependencyInjection
static IEmbeddingGenerator<string, Embedding<float>> CreateOpenAIGenerator(
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

static IEmbeddingGenerator<string, Embedding<float>> CreateOllamaGenerator(
    EmbeddingProviderOptions options)
{
    var uri = new Uri(options.Endpoint);
    var apiClient = new OllamaApiClient(uri);
    return apiClient.AsIEmbeddingGenerator();
}
```

### Ollama config (appsettings.Development.json)

```json
{
  "EmbeddingProvider": {
    "Provider": "Ollama",
    "Endpoint": "http://localhost:11434",
    "DeploymentName": "nomic-embed-text"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  }
}
```

## Configuration Design

| Key | OpenAI value | Ollama value |
|-----|-------------|--------------|
| `Provider` | `"OpenAI"` (default, or explicit) | `"Ollama"` |
| `Endpoint` | `https://<res>.openai.azure.com` | `http://localhost:11434` |
| `DeploymentName` | Deployment name (e.g. `text-embedding-ada-002`) | Model name (e.g. `nomic-embed-text`) |
| `ApiKey` | Required | Not needed (omit or leave empty) |
| `MaxBatchSize` | Same | Same |
| `MaxRetries` | Same | Same (network issues apply) |
| `TimeoutSeconds` | Same | Same |

## Testing Strategy

| Layer | What | Approach |
|-------|------|----------|
| Unit | Provider selector resolves correct pipeline | Inject config with each Provider value, assert generator type via reflection or resolution behavior |
| Unit | Validator rejects invalid Provider | `InlineData("", "invalid", "Anthropic")` → assert `Failed` |
| Unit | Validator accepts "OpenAI" and "Ollama" | Assert `Succeeded` |
| Integration | Ollama pipeline composes without error | Register with Ollama config, resolve `IEmbeddingProvider` — assert no composition exception |
| Integration | Existing OpenAI pipeline unchanged | Existing `EmbeddingResilienceTests` — add `Provider: "OpenAI"` to config, all pass |
| Integration | Workers host composition | Add `Provider: "OpenAI"` — `EmbeddingProvider` resolves |

**New test file**: `src/Aura.UnitTests/Infrastructure/EmbeddingProviderResolutionTests.cs`

- `OpenAI_Provider_ResolvesPipeline()` — DI composition with `Provider=OpenAI`, resolve `IEmbeddingProvider`, assert type
- `Ollama_Provider_ResolvesPipeline()` — DI composition with `Provider=Ollama`, resolve `IEmbeddingProvider`, assert no crash
- `DefaultProvider_IsOpenAI()` — config without `Provider` key defaults to OpenAI pipeline
- `InvalidProvider_ThrowsAtResolution()` — `Provider=Anthropic` throws on resolve

## Validation Rules (Validator)

```csharp
public ValidateOptionsResult Validate(string? name, EmbeddingProviderOptions options)
{
    // Existing validations (Endpoint, DeploymentName, MaxBatchSize, etc.)

    if (!string.IsNullOrWhiteSpace(options.Provider)
        && options.Provider is not ("OpenAI" or "Ollama"))
    {
        failures.Add($"Unsupported Provider: '{options.Provider}'. Supported values: 'OpenAI', 'Ollama'.");
    }

    // Rest of existing validation...
}
```

## Rollback / Migration

**Rollback**: Revert `DependencyInjection.cs` to single-path OpenAI. Remove `Microsoft.Extensions.AI.Ollama` package reference. Old configs without `Provider` field are already compatible (default is `"OpenAI"`).

**No data migration required** — the change is pure DI composition. No database schema, no queues, no persisted state changes.

**Feature flag**: Not needed. The `Provider` config field is the feature gate — flip between `"OpenAI"` and `"Ollama"` at deploy time.
