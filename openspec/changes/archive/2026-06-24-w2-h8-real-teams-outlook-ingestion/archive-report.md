# Archive Report: Real Teams and Outlook Ingestion

**Change**: w2-h8-real-teams-outlook-ingestion
**Archived**: 2026-06-24
**Mode**: openspec
**Verdict**: PASS WITH WARNINGS

## Summary

Implemented real Microsoft Graph API ingestion for Teams and Outlook, replacing fixture-based data with live API calls. The change spans 3 chained PRs across 34 tasks, delivering delegated auth, Graph source providers, sync orchestration, UI panels, architecture tests, and Playwright scaffolding.

## What Was Delivered

### Core Capabilities
- **Delegated-first Graph auth**: MSAL `AuthorizationCodeCredential` with SQLite token cache; worker uses silent acquisition from cache; re-auth surfaced to UI on expiration.
- **Graph source providers**: `GraphTeamsSourceProvider` and `GraphOutlookSourceProvider` implementing `IMessageSourceProvider<T>`, injected into existing adapters with fixture fallback.
- **Sync orchestration**: `TriggerSyncUseCase` iterates all registered connectors, aggregates per-source results, handles partial degradation, persists items via buffer drain.
- **Sync endpoints**: `POST /api/sync/now` + `GET /api/sync/status` with per-source status.
- **Multi-connector worker**: `ConnectorExecutionWorker` iterates all `IConnectorAdapter` implementations.
- **UI panels**: `SyncStatusPanel` with sync-now button, per-source progress, last sync timestamp, re-auth prompt. `InboxPreviewPanel` extended with sender, snippet, deepLink, syncState fields.
- **Architecture tests**: 7 NetArchTest rules isolating Graph SDK types from Application/Domain.
- **Playwright scaffolding**: Bootstrap tests for shell, inbox panel, and sync panel.

### PR Breakdown
| PR | Tasks | Scope |
|----|-------|-------|
| PR1 | 10 | Ports, models, auth infra, SQLite stores |
| PR2 | 17 | Graph providers, adapter wiring, sync use case, endpoints, worker |
| PR3 | 7 | UI panels, architecture tests, Playwright scaffolding |

## Key Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Auth credential type | `AuthorizationCodeCredential` (delegated) | Delegated-first locked decision; app-only can't read `/me/` endpoints |
| Token cache | SQLite via MSAL extensions | Cross-platform file-based cache; Redis is future TODO |
| Port naming | `IMessageSourceProvider<T>` (capability-named) | Plugin design skill mandates capability naming; generic allows future non-Graph providers |
| Graph data source injection | Inject into existing adapters | Reuses tested ACL mappers; preserves controlled-demo for Playwright |
| DTO extension | Init-only optional properties | Additive; backward-compatible; no source-breaking changes |
| Sync trigger | Dedicated `TriggerSyncUseCase` | Simpler than queue-based; direct use-case call is correct for first slice |

## Risks and Follow-Up Items

| Risk | Severity | Recommendation |
|------|----------|----------------|
| `GraphTeamsSourceProvider` uses `/me/chats` + `LastMessagePreview` — slight deviation from message-oriented flow in design.md | Warning | Revalidate before PR3 or real-tenant smoke verification |
| `dashboard-inbox-preview` full end-to-end field assertion is partial (deepLink, snippet, syncState only unit-tested, not full endpoint assertion) | Warning | Extend sync→preview integration path when PR3 lands |
| 7 Docker/Qdrant integration tests fail when Docker unavailable | Warning | Pre-existing; outside this change's scope |
| `SyncEndpoints` coverage is low (50% lines, 25% branches) | Low | Add cancellation/error path tests |
| Graph DI registration coverage is thin (15% lines) | Low | Exercise failure-path branches in DI tests |
| Exact Entra app redirect URI for Blazor Server is unresolved | Open | Likely `https://localhost:{port}/signin-oidc` for dev |
| `ChannelMessage.Read.All` may require admin consent | Open | Impacts first-run Teams results in target tenant |
| `IWorkItemStore` in-memory acceptable for E2E or needs SQLite? | Open | Current in-memory store works for functional tests |

## Specs Synced

| Domain | Action | Details |
|--------|--------|---------|
| connector-execution | Updated | Added `Partial Degradation Handling` requirement; modified `Canonical Execution Result` to include partial degradation details |
| dashboard-inbox-preview | Updated | Added `Manual Sync Trigger and Feedback` requirement; modified `Preview Endpoint Contract` with real data fields; added explicit empty-state no-demo-fallback scenario |
| graph-delegated-auth | Created | New spec (delta was full spec, not delta) |
| outlook-connector-mapping | Updated | Modified `Outlook Field Mapping` to include deep link and snippet metadata fields |
| teams-connector-mapping | Updated | Modified `Teams Field Mapping` to include deep link and snippet metadata fields |

## Verification Summary

- **PR2 verify**: PASS WITH WARNINGS — 54 tests passing (38 unit + 10 integration + 6 architecture)
- **PR1 verify**: PASS — all foundation tests passing
- **PR3 verify**: PASS — 20 new tests (5 unit + 8 E2E + 7 architecture)
- **No CRITICAL issues** in any verification report
- **Average changed-file line coverage**: ~84.1% (PR2 slice)
- **TDD compliance**: 5/6 checks passed, 1/6 partial (triangulation depth)

## Archive Contents

- proposal.md ✅
- design.md ✅
- tasks.md ✅ (34/34 tasks complete)
- verify-report.md ✅
- apply-progress.md ✅
- exploration.md ✅
- specs/ ✅ (5 domain specs)
- archive-report.md ✅ (this file)
