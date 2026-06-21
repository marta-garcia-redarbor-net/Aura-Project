# Microsoft Graph — Teams Connector

This document defines the Teams connector scope for message and mention ingestion via Microsoft Graph.

## Quick path

1. Ingest Teams events required by Aura triage flow.
2. Normalize Teams payloads into canonical `WorkItem` records.
3. Attach source-specific signal metadata for downstream triage.

## Connector responsibilities

- Implement `IExternalConnector<TeamsEvent>`.
- Enforce least-privilege Graph permissions and auth flow.
- Handle throttling with retries/jitter and safe backoff.
- Normalize, deduplicate, and correlate events.
- Emit source-specific metadata for preliminary scoring inputs.

## Boundary with global triage

The Teams connector may extract source signals and prepare preliminary scoring inputs,
but it does not own final interrupt-vs-queue decisions.

Final decision authority belongs to the global triage engine (`IInterruptionPolicyEngine`).

## Future work

- [ ] Teams content-based preliminary scoring remains future work and is tracked in `StoryBacklog.md`.
