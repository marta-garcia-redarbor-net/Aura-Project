# Triage — Overview

Aura triage follows a two-stage model with a strict ownership boundary:

1. **Connector adapters (Infrastructure)** normalize provider payloads into canonical `WorkItem`s,
   extract source-specific signals, and compute **preliminary** scores.
2. The **global triage engine (Application)** is the single authority that makes the final
   **interrupt-vs-queue-or-defer** decision.

Connectors MUST NOT own final interruption decisions.

Connectors may emit canonical metadata such as sender, snippet, target-user hints, and traceable explicit cues,
but those remain policy inputs only.

## Quick path

1. Normalize external events into canonical `WorkItem` records.
2. Attach source signals and preliminary scores in metadata.
3. Apply global triage policy to emit `INTERRUPT`, `QUEUE`, or `DEFER`.

## Decision authority and governance

- Final decision authority: `IInterruptionPolicyEngine` (global triage engine).
- Rule governance requirements:
  - **Explainable**: every decision is human-readable.
  - **Auditable**: decision inputs and rationale can be inspected later.
  - **User-adjustable**: users can tune policy inputs and overrides.
  - **Per-user bounded**: narrow explicit overrides can auto-apply for the same user, while broader or riskier generalizations remain review-first.

## Target-user resolution

The global engine resolves the target user in this order:

1. `assignedTo`
2. explicit connector owner/responsible metadata
3. unresolved target user means the engine must not interrupt and falls back to `QUEUE` or `DEFER`

## Refinement model

Rule refinement is anchored only to explicit, inspectable inputs:

- Explicit user preferences
- Explicit user feedback
- Historical decision outcomes

Aura does not use opaque or silent self-learning to change triage behavior.

## Scope note

- **In scope now**: global policy boundary, governance, and refinement anchors.
- **Out of scope now**: Focus Mode design/state machine (explicitly deferred).
