# Exploration: W2-H8 — Real Teams + Outlook Ingestion

**Change:** `w2-h8-real-teams-outlook-ingestion`  
**Date:** 2026-06-24  
**Artifact store:** openspec  
**Status:** Ready for Proposal

---

## Current State

### Architecture Baseline

Aura is a Clean Architecture .NET 9 solution with five source projects and four test projects.
Ingestion runs through a well-defined pipeline:

```
ConnectorExecutionWorker (Aura.Workers)
  → ExecuteConnectorUseCase (Aura.Application)
    → IConnectorAdapter  (port in Application)
      → TeamsConnectorAdapter / OutlookConnectorAdapter (Infrastructure)
        → WorkItemBuffer → WorkItemStore
```

### What Already Exists (fixture-only implementations)

Both `TeamsConnectorAdapter` and `OutlookConnectorAdapter` are **fully implemented** except for the actual Graph SDK call. They currently load static in-memory fixtures via an injected `Func<IReadOnlyList<T>>` delegate. All downstream infrastructure is complete:

- `TeamsWorkItemMapper` and `OutlookWorkItemMapper` — full ACL mappers with metadata traceability, partial payload tolerance, and priority scoring. Both are spec-covered.
- `IConnectorAdapter` port — provider-neutral; dispatches by `ConnectorName` string.
- `ExecuteConnectorUseCase` — checkpoint-aware, telemetry-emitting, buffer+store pipeline. Fully tested.
- `IWorkItemBuffer` / `IWorkItemStore` — in-memory implementations registered in DI.
- `IIngestionCheckpointStore` — in-memory implementation; cursor + maxProcessedAt + executionFinishedAt shape complete.
- `InboxPreviewPanel.razor` — renders source-grouped items with loading/empty/error/populated states.
- `DashboardPreviewDto` — existing slim DTO with source, title, relativeTimestamp, score, suggestedAction.
- `ConnectorExecutionWorker` — one-shot background worker. Hardcoded to `("teams", "messages", "acme")`.

### What the GraphConnector Adapter Does Today

`Adapters/GraphConnector/` only binds `GraphConnectorOptions` (TenantId, ClientId, ClientSecret, Enabled) and exposes a `IGraphConnectorSettingsProvider` used by the config-readiness status panel. **No GraphServiceClient, no Graph SDK, no token acquisition exists anywhere in the codebase.**

### OpenSpec Spec Coverage Gaps for W2-H8

Existing specs cover:
- `work-item-contract` — canonical field contract ✅
- `connector-execution` — use case + port ✅
- `teams-connector-mapping` — ACL mapping ✅
- `outlook-connector-mapping` — ACL mapping ✅
- `ingestion-checkpoint-store` — checkpoint shape ✅
- `dashboard-inbox-preview` — UI DTO boundary ✅
- `graph-connector-status` — config-readiness panel ✅

**Missing for W2-H8:**
- Graph auth setup (delegated flow, Entra app, incremental consent)
- Real Graph ingestion contract (IGraphMessageProvider, IGraphMailProvider)
- `SyncNow` trigger endpoint + UI feedback
- Per-source sync state field on dashboard items
- `SyncStateDto` / `SyncResultDto` for UI progress reporting
- Playwright E2E contract protection for parallel `feat/playwright-e2e-bootstrap`

### Dashboard DTO Gap

`InboxItemPreviewDto` today has: `Title`, `Source`, `RelativeTimestamp`, `Score`, `SuggestedAction`.

**W2-H8 requires additionally:** `Sender`, `Snippet`, `DeepLink`, `IngestionState`, `PriorityHint`.  
These fields must be added to the DTO and response chain without breaking existing consumers.

---

## Affected Areas

### New files to create

- `src/Aura.Infrastructure/Adapters/Connectors/Graph/` — new folder with `GraphTokenService`, `GraphTeamsMessageProvider`, `GraphOutlookMailProvider`, `GraphClientFactory`, `DependencyInjection.cs`
- `src/Aura.Application/Ports/IGraphMessageProvider.cs` — port for Teams real messages
- `src/Aura.Application/Ports/IGraphMailProvider.cs` — port for Outlook real mail
- `src/Aura.Application/Ports/ISyncTrigger.cs` — port for manual sync trigger
- `src/Aura.Application/UseCases/IngestionSync/TriggerSyncUseCase.cs`
- `src/Aura.Api/Endpoints/SyncEndpoints.cs` — `POST /api/sync/now`, `GET /api/sync/status`
- `src/Aura.UI/Components/Dashboard/SyncStatusPanel.razor` — per-source progress + last sync timestamp
- `openspec/changes/w2-h8-real-teams-outlook-ingestion/specs/` — new delta specs

### Existing files to modify

| File | Change |
|------|--------|
| `src/Aura.Infrastructure/Adapters/Connectors/Teams/TeamsConnectorAdapter.cs` | Inject `IGraphMessageProvider`; replace fixture delegate when provider is non-null |
| `src/Aura.Infrastructure/Adapters/Connectors/Outlook/OutlookConnectorAdapter.cs` | Inject `IGraphMailProvider`; replace fixture delegate when provider is non-null |
| `src/Aura.Infrastructure/Adapters/Connectors/Teams/TeamsMessageDto.cs` | Add `Sender`, `WebUrl`, `BodyPreview` fields from Graph response |
| `src/Aura.Infrastructure/Adapters/Connectors/Outlook/OutlookEmailDto.cs` | Add `WebLink`, `BodyContent` fields from Graph response |
| `src/Aura.Infrastructure/Adapters/GraphConnector/GraphConnectorOptions.cs` | Add `UserObjectId` (delegated flow target user) |
| `src/Aura.Infrastructure/Adapters/GraphConnector/DependencyInjection.cs` | Register `GraphServiceClient` (singleton), token credential |
| `src/Aura.Infrastructure/DependencyInjection.cs` | Wire new Graph ingestion sub-registration |
| `src/Aura.Application/Models/DashboardPreviewDto.cs` | Extend `InboxItemPreviewDto` with `Sender`, `Snippet`, `DeepLink`, `IngestionState`, `PriorityHint` |
| `src/Aura.UI/Models/DashboardPreviewResponse.cs` | Mirror DTO extension |
| `src/Aura.UI/Components/Dashboard/InboxPreviewPanel.razor` | Render new fields; add per-source sync state |
| `src/Aura.Workers/ConnectorExecutionWorker.cs` | Make connector identity configurable; iterate both `teams` and `outlook` |
| `src/Aura.Api/Program.cs` | Map new sync endpoints |
| `openspec/specs/dashboard-inbox-preview/spec.md` | Delta: add new DTO fields |
| `openspec/specs/connector-execution/spec.md` | Delta: multi-connector iteration |

---

## Approaches

### 1. Thin Graph Provider Layer — Inject into Existing Adapters (RECOMMENDED)

Add `IGraphMessageProvider` and `IGraphMailProvider` ports in Application. In Infrastructure,
implement `GraphTeamsMessageProvider` and `GraphOutlookMailProvider` using `GraphServiceClient`.
Inject these providers into the *existing* `TeamsConnectorAdapter` and `OutlookConnectorAdapter`
via optional constructor injection or a conditional strategy. When the provider is available and
Graph credentials are valid, the adapter fetches from Graph; when absent or config is invalid,
it falls back to fixtures (controlled-demo mode).

**Pros:**
- Zero disruption to existing ACL mappers and use cases — all tested code stays intact
- Clean Architecture boundaries preserved: Graph SDK stays in Infrastructure only
- Partial degradation per-source is natural: each adapter independently falls back
- The fixture provider pattern already in the adapters (`Func<IReadOnlyList<T>>`) is the exact seam to exploit
- Worker and use case need minimal changes

**Cons:**
- Adapters get slightly more complex (conditional strategy inside Infrastructure)
- Requires a new Graph SDK NuGet package (`Microsoft.Graph` or `Microsoft.Graph.Beta`)

**Effort:** Medium

---

### 2. Replace Adapters Entirely with Graph-Native Adapters

Create new `GraphTeamsConnectorAdapter` and `GraphOutlookConnectorAdapter` that directly call Graph.
Register them as new `IConnectorAdapter` implementations alongside or replacing the existing ones.

**Pros:**
- Clean separation — no conditional logic inside adapters

**Cons:**
- Duplicates the full mapping and partial-tolerance logic already tested in existing adapters
- Risk of spec regression: existing tests would need to be replicated or the old adapters left as dead code
- Higher effort, more PR surface area

**Effort:** High — AVOID

---

### 3. Graph-Only — Remove Fixture Fallback Entirely

No fixture path. If Graph creds are absent, adapter fails with a clear `Disabled` result.

**Pros:**
- Simplest mental model for the real-data-first path

**Cons:**
- Breaks controlled-demo capability required by Playwright strategy
- Violates "visible partial degradation per source" locked decision
- Makes local dev without Graph credentials impossible

**Effort:** Low but **structurally wrong** — AVOID

---

## Recommendation

**Use Approach 1** (thin Graph provider layer, inject into existing adapters).

Implementation sequence:

1. **Auth foundation first:** Register `GraphServiceClient` in `Adapters/GraphConnector/DependencyInjection.cs` using `ClientSecretCredential` with User Secrets (not appsettings). This is the same pattern W2-H7 was planning. W2-H8 should do this step since it is prioritized first.

2. **Ports in Application:** Define `IGraphMessageProvider` and `IGraphMailProvider` as capability-named ports. No Graph types in signatures — they return `IReadOnlyList<TeamsMessageDto>` equivalents using *internal record types* or renamed Application-level source DTOs... Actually, to preserve the ACL correctly: ports return `IReadOnlyList<RawTeamsPayload>` (internal Application DTOs, no SDK types). Infrastructure adapters map Graph SDK responses to those.

   **Correction on typing:** The safest approach that avoids two mapping layers is to have the Graph providers return `IReadOnlyList<TeamsMessageDto>` (already Infrastructure-internal) and inject them directly into the adapter. The port stays in Application but uses a simple `IReadOnlyList<TeamsMessagePayload>` Application model that the Infrastructure mapper bridges from Graph response → payload → mapper → WorkItem.

3. **Graph provider implementations** in `Adapters/Connectors/Graph/`: call `/me/messages` (Outlook) and `/me/chats/getAllMessages` or `/teams/{id}/channels/{id}/messages` (Teams). See scope analysis below.

4. **Extend DTOs:** Add `Sender`, `Snippet`, `DeepLink`, `IngestionState`, `PriorityHint` to `InboxItemPreviewDto` and mirror to UI model.

5. **SyncNow endpoint + UI panel:** New `POST /api/sync/now` triggers the use case; `GET /api/sync/status` returns per-source result. Blazor `SyncStatusPanel.razor` shows loading → per-source progress → counts + last sync timestamp.

6. **Make ConnectorExecutionWorker iterate both connectors** (currently hardcoded to `teams/messages/acme`).

---

## Graph Scopes Analysis (Delegated-First)

> ⚠️ **Uncertainty notice:** The exact Graph API endpoint availability depends on the tenant configuration, license tier, and the target user's consent. The table below is the best-known mapping for a delegated (on-behalf-of) flow with incremental consent.

| Source | Likely Endpoint | Required Scope | Certainty |
|--------|-----------------|----------------|-----------|
| Outlook inbox | `GET /me/messages?$select=...&$top=50&$filter=receivedDateTime ge ...` | `Mail.Read` | High |
| Teams messages (joined teams) | `GET /teams/{id}/channels/{id}/messages` | `ChannelMessage.Read.All` | Medium — requires specific team/channel enumeration |
| Teams chats (1:1 and group) | `GET /me/chats?$expand=messages` | `Chat.Read` | Medium — not available on all tenants without additional consent |
| Teams messages (all, across teams) | `GET /me/chats/getAllMessages` | `Chat.ReadBasic` + `Chat.Read` or `ChannelMessage.Read.All` | Low — endpoint availability is beta-tier and tenant-gated |

**Minimum viable scope set for first slice:**
- `Mail.Read` — Outlook (high confidence)
- `ChannelMessage.Read.All` — Teams messages from specific channels (medium confidence)
- `Chat.Read` — Teams personal/group chat messages (medium confidence, may need `Chat.ReadBasic` only for list)
- `offline_access` — refresh tokens for delegated flow
- `openid profile` — identity context

**Incremental consent strategy:** Register scopes in the Entra app manifest with `"type": "Scope"`. On first delegated token request, request only `Mail.Read` + `ChannelMessage.Read.All`. Add `Chat.Read` as a separate incremental consent step if channel-only coverage is insufficient.

**Known uncertainty:** Teams message ingestion via delegated flow has historically required either (a) the user being a member of specific teams/channels, or (b) Application-level permissions (`ChannelMessage.Read.All` with admin consent). The delegated-first approach may produce zero results if the user's joined teams are not enumerated first via `GET /me/joinedTeams`. The adapter must handle empty results gracefully and report them as `SyncSucceeded_NoData`.

---

## API/UI Contract Implications for Parallel Playwright Work

The parallel branch `feat/playwright-e2e-bootstrap` must be protected from breaking API contract changes. The following contracts MUST be stable before that branch merges:

### Contracts that MUST NOT regress

| Contract | Current State | W2-H8 Change |
|----------|---------------|--------------|
| `GET /api/dashboard/preview` response shape | `InboxItemPreviewDto` with 5 fields | Additive: 5 new fields. Playwright selectors using existing `data-testid` attributes remain valid. |
| `data-testid="inbox-preview-*"` selectors | Stable | No removal. New `data-testid="inbox-preview-item-sender"`, `"...-snippet"`, `"...-deeplink"`, `"...-state"` added. |
| `data-testid="inbox-preview-panel"` | Stable | Preserved. New `data-testid="sync-status-panel"` added alongside it. |
| Auth flow (`GET /auth/token`) | Stable | No change — W2-H8 uses service-to-Graph auth, not user-facing token change. |
| `GET /api/connectors/graph/status` | Stable | No change to response shape. |

### New contracts W2-H8 introduces

| Endpoint | Method | Response | Notes |
|----------|--------|----------|-------|
| `/api/sync/now` | POST | `SyncTriggerResultDto` (per-source: status, count, timestamp) | Playwright can verify sync feedback |
| `/api/sync/status` | GET | `SyncStatusDto` (per-source: last sync timestamp, last count, state) | Polling endpoint for UI |

**Playwright contract protection rule:** All new `data-testid` attributes introduced by W2-H8 MUST be documented in the exploration/proposal before implementation. No `data-testid` removal is allowed during W2-H8 without a migration note.

### Sync-Succeeded-No-Data state

When sync completes but no items exist: the API MUST return HTTP 200 with empty `InboxGroups` and a `SyncState = "succeeded_no_data"` marker. The UI MUST render an explicit message ("Sync succeeded — no items available"). No automatic fallback to demo fixtures.

---

## OpenSpec Capability Analysis

### Extend existing capabilities vs. add new ones

| Decision | Rationale |
|----------|-----------|
| **MODIFY** `connector-execution/spec.md` | Add multi-connector iteration requirement (currently spec says "Teams is the first connector") |
| **MODIFY** `teams-connector-mapping/spec.md` | Add real Graph payload mapping requirement alongside fixture path |
| **MODIFY** `outlook-connector-mapping/spec.md` | Same — add real Graph payload alongside fixture path |
| **MODIFY** `dashboard-inbox-preview/spec.md` | Add new DTO fields and sync-state requirement |
| **ADD** `graph-ingestion-auth/spec.md` | New capability: delegated Graph auth, Entra app registration, incremental consent, User Secrets |
| **ADD** `ingestion-sync-trigger/spec.md` | New capability: `POST /api/sync/now`, per-source progress, SyncNow feedback states |
| **ADD** `ingestion-sync-state/spec.md` | New capability: per-source state on dashboard items (IngestionState field) |

The existing `graph-connector-status` spec covers config-readiness only. It is NOT the right spec to extend for live ingestion auth — that is a different concern and needs its own capability.

---

## Risks

1. **Teams scope availability in delegated flow** — `ChannelMessage.Read.All` may require admin consent even in delegated mode depending on tenant policy. Risk: first real sync returns zero Teams items. Mitigation: enumerate joined teams first; document as known limitation; show `SyncSucceeded_NoData` state clearly.

2. **Graph API rate limiting** — `GET /me/messages` default paging returns 10 items; `$top` max is 1000 for mail but only 50 for some Teams endpoints. The first slice must use `$select` to fetch only needed fields and implement `$skip`/`@odata.nextLink` pagination. Missing this silently truncates ingested data.

3. **Token refresh in worker context** — Delegated flow requires refresh tokens. A background worker cannot interactively prompt for consent. The Entra app registration must pre-consent the required scopes for the specific user via admin consent or an interactive consent on first run. Mitigation: implement an interactive consent step on startup when no valid token exists.

4. **Dashboard DTO breaking change** — Adding fields to `InboxItemPreviewDto` is additive in JSON deserialization (new fields ignored by old clients). However, if `InboxItemPreviewResponse` in `Aura.UI` is a record with positional constructor, adding parameters is a source-breaking change. Mitigation: use named/optional parameters or init-only properties in the record, not positional constructors.

5. **ConnectorExecutionWorker identity hardcoding** — Currently hardcoded to `("teams", "messages", "acme")`. Multi-connector iteration requires either: (a) looping over configured identities, or (b) running the worker once per identity. Option (b) is safer given the one-shot worker pattern.

6. **Playwright branch contract stability** — Adding new DTO fields before the E2E bootstrap branch lands is safe (additive). The risk is removing or renaming existing `data-testid` attributes. Mitigation: the exploration establishes a no-removal contract for the duration of W2-H8.

7. **InMemoryWorkItemStore persistence** — Items survive only in the current process. For functional E2E validation, `GET /api/dashboard/preview` must read from the same store that the worker wrote to. This is fine if API and worker share the same process/DI scope but breaks in separate-process deployment. For first-slice validation this is acceptable.

8. **Microsoft.Graph NuGet package size** — Adding `Microsoft.Graph` (or `Microsoft.Graph.Beta`) brings a large transitive closure. `Microsoft.Graph.Beta` should be avoided for the first slice. Use `Microsoft.Graph` stable (v5.x) with the `GraphServiceClient` + `ClientSecretCredential` (or `UsernamePasswordCredential` for delegated dev).

---

## Sequencing and Readiness for Proposal

### Pre-conditions met ✅

- ACL mappers for Teams and Outlook are complete and tested
- Checkpoint store, use case, worker, and buffer pipeline are complete
- Dashboard preview endpoint and UI panel are complete
- `GraphConnectorOptions` binding (TenantId, ClientId, ClientSecret) already exists
- Clean Architecture boundaries are established and guarded by arch tests

### Recommended implementation order

```
1. Entra app registration + User Secrets setup (infrastructure prerequisite)
2. GraphServiceClient registration in DependencyInjection.cs
3. IGraphMessageProvider + IGraphMailProvider ports (Application)
4. GraphTeamsMessageProvider + GraphOutlookMailProvider (Infrastructure)
5. Extend TeamsMessageDto + OutlookEmailDto with real Graph fields
6. Update TeamsConnectorAdapter + OutlookConnectorAdapter to use providers conditionally
7. Extend InboxItemPreviewDto + mirror to UI model
8. Update InboxPreviewPanel.razor to render new fields + sync state
9. ConnectorExecutionWorker: iterate both connectors
10. TriggerSyncUseCase + SyncEndpoints + SyncStatusPanel.razor
11. Delta specs (MODIFY existing + ADD new specs)
12. Unit tests for Graph providers (mocked GraphServiceClient)
13. Integration test: SyncNow endpoint → items visible in preview
14. Playwright: real-data smoke + controlled-demo suite
```

### W2-H8 before W2-H7

W2-H8 (Teams + Outlook real ingestion) should complete before W2-H7 (Calendar). Both need the Graph auth foundation (step 1–2 above). Doing W2-H8 first means W2-H7 inherits the registered `GraphServiceClient` and only adds the Calendar scope + new adapter. This avoids doing the auth plumbing twice.

---

## Ready for Proposal

**Yes.** All architectural questions are answered. Locked decisions are consistent with current codebase structure.

Key clarifications to carry into proposal:
- Confirm whether delegated flow uses `ClientSecretCredential` (service-to-service, no interactive step) or `AuthorizationCodeCredential` (interactive, returns delegated token). Delegated-first with incremental consent typically requires at least one interactive step per user — this is incompatible with a pure background worker. The proposal should define the consent acquisition flow explicitly.
- Confirm `Microsoft.Graph` v5.x (stable, not Beta) is the target package.
- Confirm whether `IWorkItemStore` should be upgraded from in-memory to SQLite for functional E2E validation in this slice, or if in-memory is acceptable for first slice.
