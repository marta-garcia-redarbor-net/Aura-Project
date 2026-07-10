# Delta for Semantic Index

## ADDED Requirements

### Requirement: Decision-Time Semantic Retrieval Port

The system MUST expose a retrieval port (e.g., `IDecisionContextRetriever`) that the
interruption engine calls at decision time to fetch top-K semantic context items relevant to the
work item being evaluated. The port MUST be defined in `Aura.Application`, MUST NOT expose vector
store terminology to the domain or application layers, and MUST respect a configurable per-call
timeout. If retrieval fails or times out, the port MUST return an empty result without throwing.

#### Scenario: Retrieval returns relevant context for decisioning

- GIVEN a work item is being evaluated by the interruption engine
- WHEN the engine calls `IDecisionContextRetriever.RetrieveAsync(workItem)`
- THEN the port returns a ranked list of semantic context items from `ActivityMemory`
- AND the engine passes the result to the LLM advisor as part of the decision trace

#### Scenario: Retrieval timeout returns empty without throwing

- GIVEN Qdrant does not respond within the configured timeout
- WHEN `IDecisionContextRetriever.RetrieveAsync` is invoked
- THEN the method returns an empty collection
- AND the calling engine is not blocked and receives no exception

#### Scenario: Port abstraction enforced by architecture tests

- GIVEN the retrieval port is defined in `Aura.Application`
- WHEN architecture tests validate layer dependencies
- THEN no `Aura.Domain` or `Aura.Application` type references `Qdrant` SDK types or any vector store SDK
