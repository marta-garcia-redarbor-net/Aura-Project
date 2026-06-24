# Tasks: Real Teams and Outlook Ingestion

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | 1,200–1,400 |
| 400-line budget risk | High |
| Chained PRs recommended | Yes |
| Suggested split | PR 1 → PR 2 → PR 3 |
| Delivery strategy | ask-always |
| Chain strategy | feature-branch-chain |

Decision needed before apply: Yes
Chained PRs recommended: Yes
Chain strategy: feature-branch-chain
400-line budget risk: High

### Suggested Work Units

| Unit | Goal | Likely PR | Notes |
|------|------|-----------|-------|
| 1 | Ports + Models + Auth infra + SQLite WorkItemStore | PR 1 | Base: tracker or main; standalone, all tests included |
| 2 | Graph providers + Adapter wiring + Sync use case + Endpoints + Worker | PR 2 | Base: PR 1 branch; connectors functional E2E |
| 3 | UI panels + Architecture tests + E2E scaffolding | PR 3 | Base: PR 2 branch; dashboard shows real data |

## Phase 1: Foundation (Ports, Models, Config)

- [x] 1.1 Create `src/Aura.Application/Ports/IMessageSourceProvider.cs` — generic `FetchAsync` port
- [x] 1.2 Create `src/Aura.Application/Ports/ISyncStateStore.cs` and `ITokenCacheStatus.cs`
- [x] 1.3 Create `src/Aura.Application/Models/SyncResultDto.cs` (`SyncResultDto`, `SourceSyncResult`, `SourceSyncState`) and `TokenStatus.cs`
- [x] 1.4 Extend `GraphConnectorOptions.cs` — add `RedirectUri`, `Scopes[]`
- [x] 1.5 Extend `InboxItemPreviewDto` — add optional init-only `Sender`, `Snippet`, `DeepLink`, `PriorityHint`, `SyncState`

## Phase 2: Auth + Persistence Infrastructure

- [x] 2.1 RED: Write tests for `SqliteWorkItemStore` — save, idempotent upsert, read-back
- [x] 2.2 GREEN: Create `src/Aura.Infrastructure/Adapters/WorkItems/SqliteWorkItemStore.cs`; update `WorkItems/DependencyInjection.cs` to register it
- [x] 2.3 RED: Write tests for `MsalSqliteTokenCache` — persist/retrieve token, expired-token path
- [x] 2.4 GREEN: Create `Graph/MsalSqliteTokenCache.cs` and `Graph/GraphClientFactory.cs`
- [x] 2.5 Create `Graph/DependencyInjection.cs` — register MSAL client, token cache, GraphClientFactory behind `GraphConnector:Enabled`

## Phase 3: Graph Providers + Adapter Wiring

- [ ] 3.1 RED: Write tests for `GraphTeamsSourceProvider` — mock `HttpMessageHandler`, verify DTO mapping from Graph shape
- [ ] 3.2 GREEN: Create `Graph/GraphTeamsSourceProvider.cs` implementing `IMessageSourceProvider<TeamsMessageDto>`
- [ ] 3.3 RED: Write tests for `GraphOutlookSourceProvider` — same mock pattern
- [ ] 3.4 GREEN: Create `Graph/GraphOutlookSourceProvider.cs` implementing `IMessageSourceProvider<OutlookEmailDto>`
- [ ] 3.5 Extend `TeamsMessageDto` (add `Sender`, `BodyPreview`, `WebUrl`) and `OutlookEmailDto` (add `WebLink`)
- [ ] 3.6 RED: Write mapper tests verifying deepLink, snippet, sender land in WorkItem metadata
- [ ] 3.7 GREEN: Update `TeamsWorkItemMapper.cs` and `OutlookWorkItemMapper.cs` to map new fields
- [ ] 3.8 Modify `TeamsConnectorAdapter.cs` — inject optional `IMessageSourceProvider<TeamsMessageDto>`; use provider when non-null, else fixtures
- [ ] 3.9 Modify `OutlookConnectorAdapter.cs` — same provider injection pattern
- [ ] 3.10 Update `Connectors/DependencyInjection.cs` — conditionally register Graph source providers

## Phase 4: Sync Use Case + API + Worker

- [ ] 4.1 RED: Write `TriggerSyncUseCase` tests — per-source aggregation, partial degradation, `auth_required` status
- [ ] 4.2 GREEN: Create `src/Aura.Application/UseCases/IngestionSync/TriggerSyncUseCase.cs`
- [ ] 4.3 Create `Graph/InMemorySyncStateStore.cs` implementing `ISyncStateStore`
- [ ] 4.4 Create `src/Aura.Api/Endpoints/SyncEndpoints.cs` — `POST /api/sync/now` + `GET /api/sync/status`
- [ ] 4.5 Modify `ConnectorExecutionWorker.cs` — iterate all registered `IConnectorAdapter` instead of hardcoded teams identity
- [ ] 4.6 Update `src/Aura.Infrastructure/DependencyInjection.cs` — wire Graph sub-registration
- [ ] 4.7 RED/GREEN: Integration test — POST sync/now → GET dashboard/preview returns items with new fields

## Phase 5: UI + Presentation

- [ ] 5.1 Update `src/Aura.UI/Models/DashboardPreviewResponse.cs` — mirror optional fields
- [ ] 5.2 Modify `InboxPreviewPanel.razor` — render sender, snippet, deepLink, syncState with `data-testid` attributes
- [ ] 5.3 Create `SyncStatusPanel.razor` — sync-now button, per-source progress/result, last-sync timestamp, re-auth prompt
- [ ] 5.4 Verify explicit empty-state UX: sync succeeds with zero items → UI says so, no demo fallback

## Phase 6: Architecture Tests + E2E

- [ ] 6.1 Add NetArchTest rule: `Microsoft.Graph` types MUST NOT be referenced from Application or Domain
- [ ] 6.2 Scaffold Playwright real-data smoke test in `tests/Aura.E2E/` — login → sync → verify items appear
- [ ] 6.3 Add `data-testid` selectors to controlled-demo Playwright suite for new dashboard fields
