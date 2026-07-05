# Semantic Index Specification

## Purpose

Formalize the semantic index capability as a derived data store for contextual retrieval. This capability enables the Reviewer and Triage agents to quickly fetch past decisions, project knowledge, and activity memory, without coupling domain logic to specific vector store implementations (like Qdrant). It enforces a strict separation between canonical transactional data and derived semantic chunks.

## Requirements

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

### Requirement: Derived Store Segregation

The system MUST isolate the semantic index from the transactional source of truth, enforcing that the semantic index acts strictly as a derived store via an outbox/worker pattern that processes semantic chunks in optimized batches.

#### Scenario: Syncing new evidence in batches

- GIVEN multiple new pieces of validated evidence are saved to the primary transactional store
- WHEN the outbox worker processes the synchronization events
- THEN the system MUST extract semantic chunks from the evidence
- AND accumulate the chunks to send as a single batch to the embedding provider
- AND write the resulting embeddings to the semantic index asynchronously

#### Scenario: Protecting context window limits

- GIVEN the outbox worker prepares a batch of semantic chunks
- WHEN the batch size exceeds the provider's maximum allowed tokens or item count limits
- THEN the system MUST split the chunks into smaller, safe batches
- AND process each sub-batch without losing any chunks

#### Scenario: Canonical source missing (Orphaned Chunk)

- GIVEN a request to retrieve context from the semantic index
- WHEN the returned semantic chunk references a canonical ID that no longer exists in the primary store
- THEN the system MUST discard or ignore the invalid chunk
- AND gracefully recover without throwing a fatal error

### Requirement: Application Ports for Abstraction

The system MUST define application ports for generating embeddings and for reading/writing semantic context, ensuring the `Aura.Domain` and `Aura.Application` layers remain entirely decoupled from external SDKs (like Microsoft.Extensions.AI or Qdrant).

#### Scenario: Generating embeddings without SDK leakage

- GIVEN the outbox worker needs to convert semantic chunks to vectors
- WHEN it requests embeddings
- THEN it MUST use an abstract port (e.g., `IEmbeddingProvider`) passing standard .NET collections
- AND the infrastructure implementation MUST encapsulate the Microsoft.Extensions.AI SDK usage completely

#### Scenario: Writing context via Application port

- GIVEN an active outbox worker process
- WHEN it attempts to update the semantic index
- THEN it MUST use an abstract port (e.g., `ISemanticIndexWriter`)
- AND the implementation MUST map the abstract request to specific SDK types within `Aura.Infrastructure`

#### Scenario: Retrieving context for Reviewer agent

- GIVEN the Reviewer agent requires past decision context
- WHEN it queries the semantic index
- THEN it MUST use an abstract port (e.g., `ISemanticContextRetriever`)
- AND the domain logic MUST NOT reference any vector store terminology

### Requirement: Collection Segregation

The system MUST segregate semantic data into distinct collections based on data volatility and purpose: `ProjectKnowledge` for stable evidence and `ActivityMemory` for fast-moving triage context.

#### Scenario: Storing stable project knowledge

- GIVEN an architectural decision is approved and logged
- WHEN the outbox worker synchronizes this data
- THEN the system MUST route the semantic chunks to the `ProjectKnowledge` collection

#### Scenario: Storing dynamic activity memory

- GIVEN a user updates an active Pull Request
- WHEN the triage worker processes the event for semantic context
- THEN the system MUST route the semantic chunks to the `ActivityMemory` collection

### Requirement: Semantic Unit Structure

The system MUST define semantic units as explicit, filterable chunks of derived evidence rather than raw source events. Each unit MUST include minimal metadata (e.g., canonical ID, timestamps, domain tags) to ensure accurate contextual retrieval.

#### Scenario: Chunking a large source event

- GIVEN a large source document or event
- WHEN the system prepares it for semantic indexing
- THEN the system MUST split the data into concise semantic chunks
- AND each chunk MUST contain a canonical source ID and filterable domain tags

#### Scenario: Handling sensitive content

- GIVEN a source event containing sensitive data or PII
- WHEN the system creates a semantic chunk for indexing
- THEN the system MUST strip or mask the sensitive content before it is passed to the writer port
- AND the resulting chunk MUST NOT contain the sensitive data