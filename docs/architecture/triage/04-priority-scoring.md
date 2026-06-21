# Triage — Priority Scoring and Cognitive Load

Aura uses global priority scoring as part of triage policy, not as connector-owned logic.

## Quick path

1. Receive canonical `WorkItem` plus connector preliminary score/signals.
2. Apply global scoring through `IPriorityScoringService`.
3. Use score output as one input for interruption policy decisions.

## Two-stage scoring boundary

- **Connector layer** computes source-specific **preliminary** score and writes signal metadata.
- **Global triage layer** computes policy-ready priority score across sources and contexts.

Connector scoring helps preserve source semantics, but it is not the final triage decision.

## Governance requirements

Global scoring must be:

- **Explainable**: factor contributions are visible and reviewable.
- **Auditable**: historical score inputs/outputs can be inspected.
- **User-adjustable**: tuning and overrides are user-controlled.

## Refinement anchors (explicit only)

Score refinement is allowed only from explicit, traceable inputs:

- Explicit user preferences
- Explicit user feedback
- Historical decisions and outcomes

Opaque self-learning or silent score recalibration is out of scope.

## Relationship to interruption policy

`IPriorityScoringService` outputs feed `IInterruptionPolicyEngine`.
The interruption policy remains the final authority for `INTERRUPT`, `QUEUE`, or `DEFER`.
