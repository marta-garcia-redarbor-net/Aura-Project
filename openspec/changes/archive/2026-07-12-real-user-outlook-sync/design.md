# Design: Real-User Outlook Sync with Demo Isolation

## Technical Approach

The identity propagation bug lives at the **caller** level, not in `GraphClientFactory` — which already performs oid-keyed account selection (`FirstOrDefault(a => a.HomeAccountId.ObjectId == oid)`). Two callers pass the wrong oid:

1. **`SyncEndpoints`** reads oid from `msalApp.GetAccountsAsync().FirstOrDefault()` instead of the validated `ClaimsPrincipal`. Fix: inject `ICurrentUserService`, call `GetCurrentUser()?.Oid`.
2. **`ConnectorExecutionWorker`** also uses `FirstOrDefault()` over the MSAL cache. Fix: iterate **all** MSAL accounts and run all adapters for each account.

Demo isolation is already structurally enforced:
- `RequireEntraId` policy blocks MockJwt tokens at the middleware boundary.
- MockJwt (demo) never writes to MSAL cache, so demo sessions never appear in `GetAccountsAsync()`.

A config-readiness guard on `GraphConnectorOptions` prevents reauth loops when production Entra settings are incomplete.

## Architecture Decisions

| # | Decision | Options | Choice | Rationale |
|---|----------|---------|--------|-----------|
| 1 | OID source — HTTP | MSAL cache `FirstOrDefault()` vs. `ICurrentUserService` | `ICurrentUserService` | `ClaimsPrincipal` is the validated, request-scoped identity; MSAL cache has no concept of "current request" |
| 2 | OID source — Worker | MSAL `FirstOrDefault()` vs. iterate all accounts | Iterate all accounts | `FirstOrDefault()` is wrong when ≥2 real users are cached; all MSAL accounts are real (demo never writes there) |
| 3 | Demo gate | Explicit claim check / DemoMode config vs. rely on existing boundaries | Rely on structural isolation | `RequireEntraId` blocks demo at middleware; MockJwt never writes to MSAL cache, so demo sessions never appear in `GetAccountsAsync()` — no DemoMode gate needed in the worker. Worker iteration runs only for real cached accounts |
| 4 | Token cache partitioning | Per-oid SQLite key vs. MSAL-internal partitioning | Oid-partitioned via `SuggestedCacheKey` | MSAL provides `TokenCacheNotificationArgs.SuggestedCacheKey` per-user (includes account identity). The `SetBeforeAccessAsync`/`SetAfterAccessAsync` callbacks use `args.SuggestedCacheKey ?? $"msal-user-{ClientId}-unknown"` to store each user's tokens under an isolated cache key. Falls back to `-unknown` suffix when MSAL does not provide a suggested key (edge case) |
| 5 | Config gap behavior | Exception / reauth loop vs. `Disabled` status | `Disabled` status | Consistent with existing `auth_required` degradation pattern; surfaces as sync status, not a crash |

## Data Flow

```
──── HTTP path ────────────────────────────────────────────────────────
Entra JWT  →  RequireEntraId policy  →  PostSyncNowAsync
                                          │
                                          ICurrentUserService.GetCurrentUser()?.Oid  ← from ClaimsPrincipal
                                          │
                                          TriggerSyncUseCase.ExecuteAsync(oid)
                                          │
                                          ConnectorAdapter → GraphOutlookSourceProvider
                                          │
                                          GraphClientFactory.CreateClientAsync(oid)  ← oid-keyed (already correct)
                                          │
                                          MSAL AcquireTokenSilent(accounts[oid]) → Graph API

──── Worker path ──────────────────────────────────────────────────────
ConnectorExecutionWorker (no HTTP context)
  │
  msalApp.GetAccountsAsync()  →  all real Entra accounts (demo never present)
  │
  foreach account in accounts:
    foreach adapter in adapters:
      CheckpointIdentity(connector, source, "default", account.HomeAccountId.ObjectId)
      ExecuteConnectorUseCase.ExecuteAsync(identity)
      → same Graph path as above
```

## File Changes

| File | Action | Description |
|------|--------|-------------|
| `src/Aura.Api/Endpoints/SyncEndpoints.cs` | Modify | Replace `IPublicClientApplication?` parameter with `ICurrentUserService`; derive oid from `GetCurrentUser()?.Oid` |
| `src/Aura.Workers/ConnectorExecutionWorker.cs` | Modify | Replace `FirstOrDefault()` with `GetAccountsAsync()` iteration; restructure to `foreach account → foreach adapter` |
| `src/Aura.Infrastructure/Adapters/GraphConnector/GraphConnectorOptions.cs` | Modify | Add `IsProductionReady` computed property: `Enabled && !string.IsNullOrWhiteSpace(TenantId) && !string.IsNullOrWhiteSpace(ClientId)` |
| `src/Aura.Infrastructure/Adapters/Connectors/Graph/DependencyInjection.cs` | Modify | Log warning and return early when `Enabled=true` but config is incomplete; prevents silent misconfiguration |
| `tests/Aura.UnitTests/Workers/ConnectorExecutionWorkerTests.cs` | Create | Unit tests: zero-account skip, single-account sync, two-account iteration; each verifies correct oid is passed |
| `tests/Aura.UnitTests/Sync/TriggerSyncUseCaseTests.cs` | Modify | Add assertion: `request.Identity.UserOid` matches the oid passed to `ExecuteAsync` |
| `tests/Aura.IntegrationTests/Sync/SyncEndpointTests.cs` | Modify | Add test: `POST /api/sync/now` with MockJwt returns 401 (demo gate via `RequireEntraId`) |

## Interfaces / Contracts

No new ports or interfaces. One endpoint signature change:

```csharp
// Before — reads MSAL cache
private static async Task<IResult> PostSyncNowAsync(
    TriggerSyncUseCase useCase,
    IPublicClientApplication? msalApp,   // removed
    ILoggerFactory loggerFactory,
    CancellationToken cancellationToken)

// After — reads validated claims
private static async Task<IResult> PostSyncNowAsync(
    TriggerSyncUseCase useCase,
    ICurrentUserService currentUserService,   // injected by minimal API DI
    ILoggerFactory loggerFactory,
    CancellationToken cancellationToken)
```

Worker iteration contract (non-obvious pattern — included here):

```csharp
var accounts = await msalApp.GetAccountsAsync();
foreach (var account in accounts)
{
    var oid = account.HomeAccountId.ObjectId;
    foreach (var adapter in adapters)
    {
        var identity = new CheckpointIdentity(
            adapter.ConnectorName, GetSource(adapter.ConnectorName), "default", oid);
        await useCase.ExecuteAsync(identity, stoppingToken);
    }
}
```

## Testing Strategy

| Layer | What to Test | Approach |
|-------|-------------|----------|
| Unit | Worker skips all connectors when MSAL has zero accounts | Mock `IPublicClientApplication` returning empty list |
| Unit | Worker runs all adapters per account when MSAL has two accounts | Mock returning two accounts; verify `ExecuteAsync` called twice per adapter |
| Unit | `GraphConnectorOptions.IsProductionReady` false when TenantId empty | Pure property assertion, no DI |
| Unit | `TriggerSyncUseCase` propagates oid into `CheckpointIdentity` | Capture `ExecuteAsync` call args in existing test class |
| Integration | `POST /api/sync/now` with MockJwt token returns 401 | Extend `SyncEndpointTests` with mock token and EntraId-only config |
| Architecture | `ICurrentUserService` not referenced from Domain | Existing ArchitectureTests project pattern |

## Migration / Rollout

No data migration. Config required for production Graph sync:

```
GraphConnector__Enabled=true
GraphConnector__TenantId=<tenant-guid>
GraphConnector__ClientId=<client-id>
GraphConnector__RedirectUri=<redirect-uri>
GraphConnector__Scopes__0=Mail.Read
GraphConnector__Scopes__1=User.Read
```

When `TenantId` or `ClientId` is absent: `IsProductionReady=false` → connectors report `Disabled`, no reauth loop. Existing `Enabled=false` deployments are unaffected.

## Open Questions

- None blocking.
