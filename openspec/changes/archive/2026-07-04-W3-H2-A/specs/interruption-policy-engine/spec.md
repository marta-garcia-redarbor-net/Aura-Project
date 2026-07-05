# Interruption Policy Engine Specification

## Purpose

Define the explainable final decision contract that turns scoring and user context into `INTERRUPT`, `QUEUE`, or `DEFER`.

## Requirements

### Requirement: Explainable Final Decision Authority

The system MUST evaluate a normalized context that includes priority-scoring output and focus-state input, then return exactly one final verdict: `INTERRUPT`, `QUEUE`, or `DEFER`. Connector preliminary scores and explicit content cues MUST remain input signals only. The interruption policy engine SHALL be the sole authority for the final decision.

#### Scenario: Receptive context can interrupt

- GIVEN a user is in `WindowOfOpportunity`
- AND the normalized context shows urgent, action-needed work with high-interruption relevance
- WHEN the engine evaluates the item
- THEN the final verdict is `INTERRUPT` with an explanation that names the decisive signals

#### Scenario: Unavailable context defers interruption

- GIVEN a user is in `Away`
- AND the item does not meet the user's critical-interruption rules
- WHEN the engine evaluates the item
- THEN the final verdict is `DEFER`
- AND the explanation cites focus-state constraints and the evaluated signals

---

### Requirement: Explicit Per-User Adjustment Handling

The system MUST support explicit per-user adjustment behavior without opaque learning. Narrow overrides from explicit user feedback MUST auto-apply to the next similar case for that same user. Broader or riskier generalizations MUST be represented as review-first suggestions and MUST NOT silently change final decision behavior.

#### Scenario: Narrow override applies to the next similar case

- GIVEN a user explicitly overrides a prior decision for a specific similar pattern
- WHEN a new case with the same user and similar pattern is evaluated
- THEN the engine applies that override automatically
- AND the explanation identifies the explicit per-user override

#### Scenario: Broad generalization waits for review

- GIVEN feedback implies a broader rule that could affect many future cases
- WHEN the system derives an adjustment candidate
- THEN the candidate is marked as review-first guidance
- AND the engine continues using currently approved rules until the user approves the broader change
