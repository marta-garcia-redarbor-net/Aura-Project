# Triage Global Policy Specification

## Purpose

Establishes the documented architectural boundary between connector-level normalization
and the global triage engine. Defines what the architecture docs MUST state about the
two-stage pipeline, rule governance, refinement anchoring, and deferred capabilities.

## Requirements

| Requirement | Constraint | Strength |
|---|---|---|
| Two-Stage Pipeline Boundary | Connectors normalize and pre-score; triage decides | MUST |
| Global Triage Decision Authority | Final interrupt-vs-queue decision owned by the triage engine | MUST |
| Rule Governance | Rules are explainable, auditable, and user-adjustable | MUST |
| Refinement Anchoring | Refinement via explicit preferences, feedback, and history only | MUST |
| Focus Mode Deferral | Focus Mode is explicitly deferred and out of scope | MUST |
| Teams Preliminary Scoring Backlog | Future backlog task for Teams content-based preliminary scoring | SHOULD |

---

### Requirement: Two-Stage Pipeline Boundary

The architecture documentation MUST state that connectors normalize source inputs into
canonical `WorkItem`s and compute source-specific preliminary scores. Connectors MUST NOT
be documented as owning the final interrupt-vs-queue decision.

#### Scenario: Docs state connector responsibility boundary

- GIVEN the architecture documentation for connectors
- WHEN a reader inspects the connector ingestion docs
- THEN the docs state that connectors normalize payloads into canonical WorkItems and compute source-specific preliminary scores only

#### Scenario: Docs distinguish pre-scoring from final decision

- GIVEN the architecture documentation
- WHEN a reader looks for where the final interrupt-vs-queue decision is made
- THEN the docs clearly attribute that decision to the global triage engine, not the connector

---

### Requirement: Global Triage Decision Authority

The architecture documentation MUST name a global triage engine as the single authority
for the final interrupt-vs-queue decision. No connector or source-specific component MAY
be documented as owning this decision.

#### Scenario: Docs name the global engine as decision authority

- GIVEN the triage overview documentation
- WHEN a reader looks for the component that makes the final triage decision
- THEN the docs identify the global triage engine as the sole decision authority

#### Scenario: No connector owns the interrupt decision

- GIVEN the connector-level documentation
- WHEN a reader reviews the described connector responsibilities
- THEN no connector is described as making the final interrupt-vs-queue determination

---

### Requirement: Rule Governance

The architecture documentation MUST state that triage rules are explainable, auditable,
and user-adjustable. The docs MUST NOT describe opaque or automatic rule changes.

#### Scenario: Docs assert explainability

- GIVEN the triage rules documentation
- WHEN a reader looks for governance properties
- THEN the docs state that every triage decision can be explained in human-readable terms

#### Scenario: Docs assert user-adjustability

- GIVEN the triage rules documentation
- WHEN a reader looks for user control
- THEN the docs state that users can inspect and adjust triage rules

#### Scenario: Docs prohibit opaque rule changes

- GIVEN the triage rules documentation
- WHEN a reader looks for how rules evolve
- THEN no automatic or silent rule-change mechanism is described

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
