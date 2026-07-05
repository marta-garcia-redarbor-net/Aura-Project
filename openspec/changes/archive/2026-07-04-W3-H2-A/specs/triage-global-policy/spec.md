# Delta for Triage Global Policy

## MODIFIED Requirements

### Requirement: Two-Stage Pipeline Boundary

The architecture documentation MUST state that connectors normalize source inputs into canonical `WorkItem`s, write preliminary scores and explicit source cues into canonical metadata, and MAY derive traceable content cues. Connectors MUST NOT be documented as owning the final `INTERRUPT`, `QUEUE`, or `DEFER` decision.
(Previously: Connectors were documented as normalizing inputs and pre-scoring without explicit `DEFER` or canonical cue wording.)

#### Scenario: Docs state connector responsibility boundary

- GIVEN the architecture documentation for connectors
- WHEN a reader inspects the connector ingestion docs
- THEN the docs state that connectors normalize payloads into canonical WorkItems and emit preliminary signals only

#### Scenario: Docs distinguish pre-scoring from final decision

- GIVEN the architecture documentation
- WHEN a reader looks for where the final decision is made
- THEN the docs clearly attribute `INTERRUPT`, `QUEUE`, and `DEFER` to the global triage engine, not the connector

### Requirement: Global Triage Decision Authority

The architecture documentation MUST name a global triage engine as the single authority for the final `INTERRUPT`, `QUEUE`, or `DEFER` decision. Connector scores, semantic indexes, and source-specific components MAY provide inputs, but no such component MAY be documented as the source of truth or owner of the final decision.
(Previously: The global engine was documented only as final interrupt-vs-queue authority.)

#### Scenario: Docs name the global engine as decision authority

- GIVEN the triage overview documentation
- WHEN a reader looks for the component that makes the final triage decision
- THEN the docs identify the global triage engine as the sole decision authority

#### Scenario: No connector owns the interrupt decision

- GIVEN the connector-level documentation
- WHEN a reader reviews the described connector responsibilities
- THEN no connector, semantic index, or source-specific component is described as owning the final decision

### Requirement: Rule Governance

The architecture documentation MUST state that triage rules are explainable, auditable, and user-adjustable per user. The docs MUST describe explicit per-user overrides as allowable policy inputs, MUST state that narrow explicit overrides can apply to the next similar case, and MUST state that broader or riskier generalizations require review before they change policy behavior. The docs MUST NOT describe opaque or automatic rule changes.
(Previously: The docs required explainable, auditable, user-adjustable rules but did not define explicit per-user adjustment behavior.)

#### Scenario: Docs assert explainability

- GIVEN the triage rules documentation
- WHEN a reader looks for governance properties
- THEN the docs state that every triage decision can be explained in human-readable terms

#### Scenario: Docs assert per-user adjustability

- GIVEN the triage rules documentation
- WHEN a reader looks for user control
- THEN the docs state that users can inspect and adjust triage rules for themselves

#### Scenario: Docs prohibit silent generalization

- GIVEN the triage rules documentation
- WHEN a reader looks for how rules evolve
- THEN the docs allow narrow explicit overrides to auto-apply
- AND broader or riskier generalizations are described as review-first, not silent changes
