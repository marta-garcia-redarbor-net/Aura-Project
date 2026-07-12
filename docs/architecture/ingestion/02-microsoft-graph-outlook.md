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

## Production readiness and demo coexistence

For real-user Graph sync to run, the deployment must provide:

```bash
GraphConnector__Enabled=true
GraphConnector__TenantId=<tenant-guid>
GraphConnector__ClientId=<client-guid>
GraphConnector__RedirectUri=<redirect-uri>
GraphConnector__Scopes__0=Mail.Read
GraphConnector__Scopes__1=User.Read
```

Behavioral contract:

- Demo/mock authentication remains available through the demo auth pipeline.
- Real Graph connector registration is enabled only when Graph config is production-ready.
- If `TenantId` or `ClientId` is missing while `Enabled=true`, Graph connector state is `Disabled`.
- `/api/sync/now` requires real Entra identity (`RequireEntraId`) and an authenticated `oid`; demo/mock tokens are rejected with `401`.

## Current runtime limitation

Aura's current Outlook validation path assumes the signed-in user is a **work or school account in the same tenant** and has a real **Exchange Online mailbox**.

Observed behavior during validation:

- A **personal Microsoft account invited as a guest** can authenticate to Aura and reach the OBO/token-cache path.
- However, `GET /me/mailFolders/inbox/messages` can still fail because the guest object in the resource tenant does **not** own an Exchange Online mailbox there.
- In practice, this means a guest/personal invited account is **not a valid test identity** for the current `/me/...` Outlook flow.

Validation rule for real-user Outlook sync:

- Use a **tenant-local work/school user**.
- Ensure the user has an **Exchange Online mailbox**.
- Then validate `/api/sync/now` and dashboard population.

## Architecture note

Current practical implementation:

- `Aura.Api` acquires delegated Graph tokens and caches them for reuse.
- `Aura.Workers` is expected to reuse cached tokens for background sync.

Cleaner long-term architecture:

- move the connector sync worker into `Aura.Api` so auth, token acquisition, and sync execution share one host boundary.

The worker-separated approach is kept for now because it was the fastest path to unblock runtime investigation.
