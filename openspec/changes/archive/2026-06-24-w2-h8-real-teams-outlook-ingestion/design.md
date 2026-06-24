# Design: Real Teams and Outlook Ingestion

## Technical Approach

Inject thin Graph provider ports into existing connector adapters. The `Func<IReadOnlyList<T>>` fixture seam in `TeamsConnectorAdapter` / `OutlookConnectorAdapter` is replaced by a strategy that delegates to Graph providers when available, falling back to fixtures for controlled-demo mode. Auth uses MSAL's `IConfidentialClientApplication` with `AuthorizationCodeCredential` for interactive delegated consent in the UI, persisting tokens to a SQLite cache via `MsalSqliteTokenCache`. The worker reads cached tokens non-interactively; if expired, it records `AuthRequired` status and the UI surfaces re-auth need. A new `POST /api/sync/now` endpoint triggers `ExecuteConnectorUseCase` for all registered connectors with per-source result aggregation.

## Architecture Decisions

| Decision | Choice | Alternatives | Rationale |
|----------|--------|-------------|-----------|
| Graph data source injection | Inject `IMessageSourceProvider<T>` into existing adapters | Replace adapters entirely; Graph-only without fixture fallback | Reuses tested ACL mappers. Avoids code duplication. Preserves controlled-demo for Playwright. |
| Port naming | `IMessageSourceProvider<TeamsMessageDto>` / `IMessageSourceProvider<OutlookEmailDto>` (capability-named generic port) | `IGraphMessageProvider` / `IGraphMailProvider` (provider-branded) | Plugin design skill mandates capability naming. Generic port allows future non-Graph providers. Port lives in Application with Infrastructure-internal DTO type params. |
| Auth credential type | `AuthorizationCodeCredential` (MSAL delegated) with SQLite token cache | `ClientSecretCredential` (app-only); `UsernamePasswordCredential` | Delegated-first is a locked decision. App-only can't read `/me/` endpoints. Username/password is deprecated by Microsoft. |
| Token cache backend | SQLite via `Microsoft.Identity.Client.Extensions.Msal` | In-memory; Redis | SQLite is locked decision. Redis is future TODO. MSAL extensions provide cross-platform file-based cache that maps cleanly to SQLite. |
| Worker token strategy | Worker uses `IConfidentialClientApplication.AcquireTokenSilent` from SQLite cache | Worker triggers interactive flow; Worker uses separate app-only credential | Worker cannot prompt user. Separate credential would bypass delegated consent model. Silent acquisition + cache is the correct MSAL pattern. |
| Sync trigger | New `TriggerSyncUseCase` calling `ExecuteConnectorUseCase` per registered connector | Reuse worker directly from API; Queue-based trigger | Direct use-case call is simpler for first slice. Queue adds infrastructure complexity without benefit at current scale. |
| DTO extension strategy | Init-only optional properties on existing `InboxItemPreviewDto` | Positional constructor params; Separate DTO class | Positional constructors break source compat. Optional init-only is additive and JSON-safe. |

## Data Flow

```
UI Login (Blazor)
  │ AuthorizationCodeCredential → MSAL
  │ → SQLite token cache (write)
  │
  ├─► "Sync now" click
  │     POST /api/sync/now
  │       → TriggerSyncUseCase
  │         → ExecuteConnectorUseCase(teams) + ExecuteConnectorUseCase(outlook)
  │           → IMessageSourceProvider<T>.FetchAsync()
  │             → GraphTeamsSourceProvider / GraphOutlookSourceProvider
  │               → AcquireTokenSilent (SQLite cache read)
  │               → GraphServiceClient.Me.Messages / .Chats
  │             → TeamsConnectorAdapter.ExecuteAsync / OutlookConnectorAdapter.ExecuteAsync
  │               → mapper.TryMap → buffer.Enqueue → store.Save
  │         ← AggregatedSyncResult (per-source status, counts, timestamps)
  │       ← SyncResultDto
  │
  ├─► Worker (background, periodic/manual)
  │     Same flow: AcquireTokenSilent → Graph → adapter → mapper → store
  │     If token expired → result.Status = AuthRequired
  │
  └─► GET /api/dashboard/preview
        → IDashboardPreviewReader
          → IWorkItemReader → InboxItemPreviewDto (with new fields)
        ← DashboardPreviewDto (with Sender, Snippet, DeepLink, PriorityHint, SyncState)
```

### Auth Re-authentication Flow

```
Worker/SyncUseCase
  → AcquireTokenSilent fails (MsalUiRequiredException)
    → ConnectorExecutionResult.Status = Failure, Reason = "auth_required"
      → TriggerSyncUseCase marks source SyncState = "auth_required"
        → GET /api/sync/status returns { teams: "auth_required", outlook: "ok" }
          → UI renders "Re-authenticate" prompt for the failed source
```

## File Changes

| File | Action | Description |
|------|--------|-------------|
| `src/Aura.Application/Ports/IMessageSourceProvider.cs` | Create | Generic port: `Task<IReadOnlyList<T>> FetchAsync(ConnectorExecutionRequest, CancellationToken)` |
| `src/Aura.Application/Ports/ITokenCacheStatus.cs` | Create | Port to query token validity: `Task<TokenStatus> GetStatusAsync(CancellationToken)` |
| `src/Aura.Application/Ports/ISyncStateStore.cs` | Create | Port: per-source sync state (last timestamp, count, status) |
| `src/Aura.Application/Models/SyncResultDto.cs` | Create | `SyncResultDto`, `SourceSyncResult`, `SyncState` enum |
| `src/Aura.Application/Models/TokenStatus.cs` | Create | `TokenStatus` record (IsValid, RequiresReauth, Scopes) |
| `src/Aura.Application/UseCases/IngestionSync/TriggerSyncUseCase.cs` | Create | Iterates all `IConnectorAdapter`, aggregates per-source results, updates `ISyncStateStore` |
| `src/Aura.Infrastructure/Adapters/Connectors/Graph/GraphTeamsSourceProvider.cs` | Create | `IMessageSourceProvider<TeamsMessageDto>` impl using `GraphServiceClient` |
| `src/Aura.Infrastructure/Adapters/Connectors/Graph/GraphOutlookSourceProvider.cs` | Create | `IMessageSourceProvider<OutlookEmailDto>` impl using `GraphServiceClient` |
| `src/Aura.Infrastructure/Adapters/Connectors/Graph/GraphClientFactory.cs` | Create | Creates `GraphServiceClient` from MSAL token acquisition |
| `src/Aura.Infrastructure/Adapters/Connectors/Graph/MsalSqliteTokenCache.cs` | Create | SQLite-backed MSAL token cache using `Microsoft.Identity.Client.Extensions.Msal` |
| `src/Aura.Infrastructure/Adapters/Connectors/Graph/DependencyInjection.cs` | Create | Registers Graph providers, MSAL client, token cache |
| `src/Aura.Infrastructure/Adapters/Connectors/Graph/InMemorySyncStateStore.cs` | Create | In-memory `ISyncStateStore` implementation |
| `src/Aura.Infrastructure/Adapters/Connectors/Teams/TeamsConnectorAdapter.cs` | Modify | Replace `Func<IReadOnlyList<TeamsMessageDto>>` with `IMessageSourceProvider<TeamsMessageDto>?` injection; use provider when non-null, else fixtures |
| `src/Aura.Infrastructure/Adapters/Connectors/Outlook/OutlookConnectorAdapter.cs` | Modify | Same pattern as Teams adapter |
| `src/Aura.Infrastructure/Adapters/Connectors/Teams/TeamsMessageDto.cs` | Modify | Add `Sender`, `BodyPreview`, `WebUrl` fields |
| `src/Aura.Infrastructure/Adapters/Connectors/Outlook/OutlookEmailDto.cs` | Modify | Add `WebLink` field (BodyPreview already exists) |
| `src/Aura.Infrastructure/Adapters/Connectors/Teams/TeamsWorkItemMapper.cs` | Modify | Map new fields into WorkItem metadata (deepLink, snippet, sender) |
| `src/Aura.Infrastructure/Adapters/Connectors/Outlook/OutlookWorkItemMapper.cs` | Modify | Map WebLink into metadata (deepLink) |
| `src/Aura.Infrastructure/Adapters/GraphConnector/GraphConnectorOptions.cs` | Modify | Add `RedirectUri`, `Scopes` array |
| `src/Aura.Infrastructure/Adapters/Connectors/DependencyInjection.cs` | Modify | Register Graph source providers conditionally |
| `src/Aura.Infrastructure/DependencyInjection.cs` | Modify | Wire new Graph ingestion sub-registration |
| `src/Aura.Application/Models/DashboardPreviewDto.cs` | Modify | Add `Sender?`, `Snippet?`, `DeepLink?`, `PriorityHint?`, `SyncState?` as optional init-only props to `InboxItemPreviewDto` |
| `src/Aura.UI/Models/DashboardPreviewResponse.cs` | Modify | Mirror new optional fields |
| `src/Aura.UI/Components/Dashboard/InboxPreviewPanel.razor` | Modify | Render new fields with `data-testid` attributes |
| `src/Aura.UI/Components/Dashboard/SyncStatusPanel.razor` | Create | Sync now button, per-source progress, last sync timestamp |
| `src/Aura.Api/Endpoints/SyncEndpoints.cs` | Create | `POST /api/sync/now`, `GET /api/sync/status` |
| `src/Aura.Api/Endpoints/DashboardEndpoints.cs` | Modify | No structural change; data flows through existing `IDashboardPreviewReader` |
| `src/Aura.Workers/ConnectorExecutionWorker.cs` | Modify | Iterate all registered `IConnectorAdapter` connectors instead of hardcoded teams identity |
| `tests/Aura.UnitTests/` | Create | Graph provider unit tests (mocked `GraphServiceClient`), TriggerSyncUseCase tests, mapper tests for new fields |
| `tests/Aura.IntegrationTests/` | Create | SyncNow endpoint → items visible in preview |
| `tests/Aura.ArchitectureTests/` | Modify | Verify Graph SDK types don't leak into Application/Domain |

## Interfaces / Contracts

```csharp
// Application/Ports/IMessageSourceProvider.cs
public interface IMessageSourceProvider<T>
{
    Task<IReadOnlyList<T>> FetchAsync(ConnectorExecutionRequest request, CancellationToken ct);
}

// Application/Ports/ISyncStateStore.cs
public interface ISyncStateStore
{
    Task<IReadOnlyList<SourceSyncState>> GetAllAsync(CancellationToken ct);
    Task UpdateAsync(string source, SourceSyncState state, CancellationToken ct);
}

// Application/Models/SyncResultDto.cs
public sealed record SyncResultDto(IReadOnlyList<SourceSyncResult> Results);
public sealed record SourceSyncResult(string Source, string Status, int ItemCount,
    DateTimeOffset? LastSyncTimestamp, string? FailureReason);
public sealed record SourceSyncState(string Source, string Status, int LastItemCount,
    DateTimeOffset? LastSyncTimestamp);

// New fields on InboxItemPreviewDto (init-only, nullable for backward compat)
public sealed record InboxItemPreviewDto(
    string Title, string Source, string RelativeTimestamp, double Score, string SuggestedAction)
{
    public string? Sender { get; init; }
    public string? Snippet { get; init; }
    public string? DeepLink { get; init; }
    public string? PriorityHint { get; init; }
    public string? SyncState { get; init; }
}
```

## Testing Strategy

| Layer | What to Test | Approach |
|-------|-------------|----------|
| Unit | GraphTeamsSourceProvider, GraphOutlookSourceProvider | Mock `GraphServiceClient` via `HttpMessageHandler` stub; verify DTO mapping from Graph response shape |
| Unit | TriggerSyncUseCase | Mock `IConnectorAdapter` list; verify per-source aggregation, partial degradation, auth_required handling |
| Unit | TeamsWorkItemMapper / OutlookWorkItemMapper new fields | Verify deepLink, snippet, sender land in WorkItem metadata |
| Unit | InboxItemPreviewDto new fields | Verify JSON serialization backward compat (null fields omitted) |
| Integration | POST /api/sync/now → GET /api/dashboard/preview | WebApplicationFactory; verify items appear with new fields after sync |
| Architecture | Graph SDK isolation | NetArchTest rule: `Microsoft.Graph` types not referenced from Application or Domain |
| E2E | Real-data smoke | Playwright: login → sync now → verify items appear (real account, expected to be flaky) |
| E2E | Controlled-demo suite | Playwright: fixture path → verify all fields render with `data-testid` selectors |

## Playwright Contract Stability

New `data-testid` attributes: `inbox-preview-item-sender`, `inbox-preview-item-snippet`, `inbox-preview-item-deeplink`, `inbox-preview-item-state`, `sync-status-panel`, `sync-now-button`, `sync-source-progress`, `sync-last-timestamp`. No existing `data-testid` is removed or renamed.

## Migration / Rollout

No data migration. Rollout sequence:

1. **Auth + token cache** — register MSAL + SQLite cache; no functional impact until providers are wired
2. **Graph source providers** — implement but register behind `GraphConnector:Enabled` flag
3. **Adapter injection** — wire providers into adapters; fixture fallback when provider absent
4. **DTO extension** — additive; existing JSON consumers unaffected
5. **Sync endpoints + UI** — new routes; no existing endpoint changes
6. **Worker multi-connector** — iterate all adapters; currently teams + outlook

Rollback: set `GraphConnector:Enabled = false` → adapters revert to fixture path. Remove Entra app scopes if needed.

## Implementation Sequencing

| Phase | What | Risk |
|-------|------|------|
| 1 | MSAL registration + SQLite token cache + GraphConnectorOptions extension | Low — no runtime change until wired |
| 2 | IMessageSourceProvider port + Graph provider implementations | Medium — Graph SDK integration; mitigate with HttpMessageHandler mocks |
| 3 | Adapter modification (inject providers) + DTO/Mapper extensions | Medium — touches tested code; mitigate with TDD on new fields |
| 4 | TriggerSyncUseCase + SyncEndpoints + ISyncStateStore | Low — new code path, no existing code modified |
| 5 | UI (SyncStatusPanel + InboxPreviewPanel new fields) | Low — additive Blazor components |
| 6 | Worker multi-connector iteration | Low — small change, well-isolated |
| 7 | Architecture tests + integration tests | Low |
| 8 | Playwright smoke suite (real account) + demo suite | Medium — real-data flakiness expected |

## Open Questions

- [ ] Exact Entra app redirect URI for Blazor Server (likely `https://localhost:{port}/signin-oidc` for dev)
- [ ] Whether `ChannelMessage.Read.All` requires admin consent in the target tenant — impacts first-run Teams results
- [ ] Whether `IWorkItemStore` in-memory is acceptable for functional E2E or needs SQLite upgrade in this slice
