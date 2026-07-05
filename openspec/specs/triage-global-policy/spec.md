# Triage Global Policy Specification

## Purpose

Establishes the documented architectural boundary between connector-level normalization
and the global triage engine. Defines what the architecture docs MUST state about the
two-stage pipeline, rule governance, refinement anchoring, and deferred capabilities.

## Requirements

| Requirement | Constraint | Strength |
|---|---|---|---|
| Two-Stage Pipeline Boundary | Connectors normalize and pre-score; triage decides `INTERRUPT`, `QUEUE`, or `DEFER` | MUST |
| Global Triage Decision Authority | Final `INTERRUPT`, `QUEUE`, or `DEFER` decision owned by the triage engine | MUST |
| Rule Governance | Rules are explainable, auditable, user-adjustable, with explicit per-user overrides and review-first generalization | MUST |
| Refinement Anchoring | Refinement via explicit preferences, feedback, and history only | MUST |
| Focus Mode Deferral | Focus Mode is explicitly deferred and out of scope | MUST |
| Teams Preliminary Scoring Backlog | Future backlog task for Teams content-based preliminary scoring | SHOULD |

---

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

---

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

---

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

---

### Requirement: Refinement Anchoring

The architecture documentation MUST state that rule refinement is driven by explicit user
preferences, explicit feedback, and decision history. The docs MUST NOT describe automatic
or opaque self-learning as a refinement mechanism.

#### Scenario: Docs exclude opaque self-learning

- GIVEN the triage learning documentation
- WHEN a reader looks for how the triage engine refines itself
- THEN the docs describe only explicit preferences, feedback, and history as inputs
- AND no opaque or automatic self-learning mechanism is described

---

### Requirement: Focus Mode Deferral

The architecture documentation MUST explicitly mark Focus Mode as deferred and out of
scope for the current triage foundation.

#### Scenario: Docs mark Focus Mode as deferred

- GIVEN the Focus State Machine documentation
- WHEN a reader checks its current scope status
- THEN the document explicitly states Focus Mode is deferred and out of scope for now

---

### Requirement: Teams Preliminary Scoring Backlog

`StoryBacklog.md` SHOULD contain a future task for Teams connector content-based
preliminary scoring, distinguishing it from the existing Outlook source-specific
scoring approach.

#### Scenario: Backlog contains Teams scoring task

- GIVEN the StoryBacklog.md file
- WHEN a reader looks for Teams connector future work
- THEN a task for Teams content-based preliminary scoring exists and is marked as future work
