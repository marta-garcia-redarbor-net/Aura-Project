# Semantic Index Specification

## Purpose

Formalize the semantic index capability as a derived data store for contextual retrieval. This capability enables the Reviewer and Triage agents to quickly fetch past decisions, project knowledge, and activity memory, without coupling domain logic to specific vector store implementations (like Qdrant). It enforces a strict separation between canonical transactional data and derived semantic chunks.

## Requirements

### Requirement: Derived Store Segregation

The system MUST isolate the semantic index from the transactional source of truth, enforcing that the semantic index acts strictly as a derived store via an outbox/worker pattern.

#### Scenario: Syncing new evidence

- GIVEN a new piece of validated evidence is saved to the primary transactional store
- WHEN the outbox worker processes the synchronization event
- THEN the system MUST extract semantic chunks from the evidence
- AND write those chunks to the semantic index asynchronously

#### Scenario: Canonical source missing (Orphaned Chunk)

- GIVEN a request to retrieve context from the semantic index
- WHEN the returned semantic chunk references a canonical ID that no longer exists in the primary store
- THEN the system MUST discard or ignore the invalid chunk
- AND gracefully recover without throwing a fatal error

### Requirement: Application Ports for Abstraction

The system MUST define application ports for reading and writing semantic context to ensure the `Aura.Domain` and `Aura.Application` layers remain decoupled from external SDKs.

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
