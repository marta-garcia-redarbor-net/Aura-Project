# Microsoft Graph — Outlook Connector

This document defines Outlook email ingestion and source-signal extraction via Microsoft Graph.

## Quick path

1. Select Outlook messages to ingest into canonical flow.
2. Normalize selected messages into canonical `WorkItem` records.
3. Compute source-specific preliminary scoring signals and emit metadata.

## Connector responsibilities

- Implement `IExternalConnector<OutlookEvent>`.
- Use delta queries, checkpoints, and retries.
- Apply sender/subject/body signal extraction and deadline cue detection.
- Minimize sensitive data in metadata.
- Emit connector metrics for volume, latency, and errors.

## Preliminary scoring note

Outlook currently applies deterministic multi-signal preliminary scoring
(`Importance + subject + sender + body`) and writes score breakdown metadata under
`outlook.scoring.*` plus deadline cues under `outlook.deadline.*`.

This scoring is connector-local and preliminary.

## Boundary with global triage

The Outlook connector does not own final interrupt-vs-queue decisions.
Final decision authority belongs to the global triage engine (`IInterruptionPolicyEngine`).

## Open follow-up

- [ ] Extend Outlook selection and sync strategy documentation with concrete filtering examples.
