# Triage — Morning Summary

Morning Summary produces a deterministic, explainable ranked list each morning.

## Decision (final rule)

1. Primary decision order: **Deadline > Impact > Risk**.
2. If explicit signals do not fully decide order, use connector **preliminary score**.
3. If items still tie, use nearest due date, then oldest item, then stable Id.
4. If all explicit signals are missing, preliminary score is the fallback decision input.
5. If neither explicit signals nor preliminary score exists, classify `insufficient-signals` and place last.

## Quick path

1. Normalize inputs into canonical `WorkItem` records with explicit signals and optional connector preliminary score.
2. Apply the final ranking policy in Application using the decision sequence above.
3. Emit ordered output plus structured per-item ranking explanation.

## Architecture boundary

- Connector adapters may compute source-specific **preliminary** scores.
- `Aura.Application` owns final Morning Summary ranking policy.
- Connectors must not own final ranking decisions.

## Output contract

- **Ordered list**: deterministic rank order for the summary window.
- **Per-item explanation**: structured explanation aligned with each item's final rank.

## Scope notes

- AI-assisted prioritization is design-only for now and remains out of scope.
- This document defines ranking policy only; transport, delivery scheduling, and UI rendering are handled elsewhere.
