# Triage — Focus State Machine

## Status

**Deferred / Out of Scope** for `triage-global-policy-foundation`.

## Why deferred

This change establishes the policy foundation and ownership boundary first:

- Connectors normalize and pre-score.
- The global triage engine decides interrupt-vs-queue.

Defining Focus Mode state transitions before this boundary is fully stabilized would create
false confidence and likely require immediate rework.

## Contract note

`IFocusStateResolver` remains a future integration point that can consume triage outcomes,
but no Focus Mode behavior is defined in this documentation change.
