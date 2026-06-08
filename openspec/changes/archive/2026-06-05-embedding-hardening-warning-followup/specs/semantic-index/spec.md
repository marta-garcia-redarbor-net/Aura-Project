# Delta for Semantic Index

## MODIFIED Requirements

### Requirement: Observable and Resilient Embedding Generation

The system MUST provide an embedding generation capability that is instrumented for observability (OpenTelemetry) and resilience (retries/timeouts), ensuring failures are handled gracefully and operations are traceable.
(Previously: Included telemetry and rate limit scenarios, but lacked explicit scenarios for timeout resilience and host composition)

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

#### Scenario: Accurate Dependency Injection and Host Composition

- GIVEN the application host starts up
- WHEN registering the embedding provider dependencies
- THEN the system MUST correctly compose the implementation and its resilience pipeline
- AND the semantic index infrastructure MUST be fully resolvable without composition errors
