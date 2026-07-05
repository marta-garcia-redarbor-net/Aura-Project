# Delta for Semantic Index

## MODIFIED Requirements

### Requirement: Observable and Resilient Embedding Generation

The system MUST provide an embedding generation capability that is instrumented for observability (OpenTelemetry) and resilience (retries/timeouts), ensuring failures are handled gracefully and operations are traceable. The provider pipeline MUST be selectable at startup via `EmbeddingProviderOptions.Provider`, supporting `"OpenAI"` (default) and `"Ollama"` provider values.
(Previously: provider was hardcoded to Azure OpenAI with no config-driven selection)

#### Scenario: Telemetry on successful batch generation

- GIVEN the outbox worker requests embeddings for a batch of strings
- WHEN the infrastructure provider successfully generates the embeddings
- THEN the system MUST emit a telemetry span documenting the operation
- AND the span MUST include the token usage and batch size

#### Scenario: Recovering from a transient rate limit

- GIVEN the infrastructure provider receives a transient error (e.g., HTTP 429) from the external AI service
- WHEN the response is returned to the resilience pipeline
- THEN the system MUST retry the request using a backoff strategy
- AND the worker MUST eventually receive the embeddings once the limit passes

#### Scenario: Enforcing timeout policies on prolonged generation

- GIVEN the infrastructure provider experiences a delay longer than the configured timeout policy
- WHEN generating embeddings for a batch of strings
- THEN the system MUST cancel the request to the external AI service
- AND throw a timeout or rejection exception to be handled by the outbox worker

#### Scenario: Config-driven provider composition on startup

- GIVEN the application configuration sets `EmbeddingProviderOptions.Provider` to `"OpenAI"`
- WHEN the `DependencyInjection.cs` extension runs
- THEN the system MUST compose the OpenAI pipeline: `OpenAIClient` → `GetEmbeddingClient` → `AsIEmbeddingGenerator`
- AND the pipeline MUST include OpenTelemetry middleware and resilience policies

#### Scenario: Ollama provider composes correctly

- GIVEN the application configuration sets `EmbeddingProviderOptions.Provider` to `"Ollama"`
- WHEN the `DependencyInjection.cs` extension runs
- THEN the system MUST compose the Ollama pipeline: `OllamaApiClient` → `AsIEmbeddingGenerator`
- AND the pipeline MUST include the same OpenTelemetry middleware and resilience policies
- AND the full `IEmbeddingProvider` MUST be resolvable without composition errors

#### Scenario: Default provider when config is absent

- GIVEN the application configuration does not contain `EmbeddingProviderOptions.Provider`
- WHEN the DI registration reads the options
- THEN the system MUST default to `"OpenAI"`
- AND resolve the OpenAI pipeline identically to the explicit `"OpenAI"` case

#### Scenario: Invalid provider value fails fast

- GIVEN the application configuration sets `EmbeddingProviderOptions.Provider` to an unsupported value (e.g., `"Anthropic"`)
- WHEN the options validator runs during startup
- THEN the system MUST throw a validation error
- AND the error message MUST describe the supported values
