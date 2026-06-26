# Design: W2-H10 — Align Microsoft Graph Flow to Delegated User Context

## Technical Approach

Replace `IConfidentialClientApplication` with `IPublicClientApplication` in the Graph connector DI, remove `ClientSecret` from options, add `oid` parameter to `IGraphClientFactory.CreateClientAsync` for multi-user account selection, propagate `oid` through `CheckpointIdentity.UserOid` from API/Workers to providers, and add structured telemetry for `MsalUiRequiredException` and Graph HTTP failures. The change is confined to Infrastructure (DI, adapters, options) with a small Application-layer delta (`CheckpointIdentity` gains an optional field).

## Architecture Decisions

### Decision: IPublicClientApplication over IConfidentialClientApplication

| Option | Tradeoff | Decision |
|--------|----------|----------|
| `IPublicClientApplication` | Correct for delegated flow; no client secret needed; `AcquireTokenSilent` works identically; `UserTokenCache` is the default cache target | ✅ Chosen |
| `IConfidentialClientApplication` without secret | Technically works for delegated tokens but semantically wrong; Entra ID may enforce different policies for confidential clients; misleads future developers | ❌ Rejected |
| Custom token provider (no MSAL) | Full control but loses token cache, refresh, and silent acquisition for free | ❌ Rejected |

**Rationale**: `IPublicClientApplication` is the MSAL type designed for apps acting on behalf of a user. The current code already uses `UserTokenCache` and `AcquireTokenSilent` — the only difference is the builder type and the absence of `.WithClientSecret()`. This is a minimal, correct change.

### Decision: oid as first parameter to CreateClientAsync

| Option | Tradeoff | Decision |
|--------|----------|----------|
| `CreateClientAsync(string oid, CancellationToken ct)` | Breaking interface change; all 3 callers update; explicit and clear | ✅ Chosen |
| `CreateClientAsync(CancellationToken ct)` with internal oid resolution | Would require factory to know about `ICurrentUserService` — couples Infrastructure to Application port | ❌ Rejected |
| Oid carried in `GraphConnectorOptions` | Options are config-time, not per-request; wrong scope | ❌ Rejected |

**Rationale**: The factory is already internal. The oid is per-request context that comes from the caller (API via `ICurrentUserService`, Worker via token cache). Making it an explicit parameter keeps the factory stateless and testable.

### Decision: Optional UserOid on CheckpointIdentity

| Option | Tradeoff | Decision |
|--------|----------|----------|
| Optional `string? UserOid` property | Backward-compatible; existing constructors still work; null means "no user context" | ✅ Chosen |
| New record type `UserCheckpointIdentity` | Type-safe but forces all consumers to handle two types | ❌ Rejected |
| Mandatory `UserOid` with sentinel `"default"` | Backward-compatible at type level but semantically lies — "default" is not an oid | ❌ Rejected |

**Rationale**: Optional property preserves backward compatibility. Workers that don't have a user context set it to null; the use case checks for null and skips. API path always populates it.

### Decision: Status derivation without ClientSecret

| Option | Tradeoff | Decision |
|--------|----------|----------|
| `HasValidCredentials` = `ClientId + TenantId` present | Delegated flow needs no secret; `ValidConfig` achievable without credentials block | ✅ Chosen |
| Keep `HasValidCredentials` = `ClientSecret` present | Would require users to configure a secret they don't need; blocks status from reaching `ValidConfig` | ❌ Rejected |
| Remove `HasValidCredentialsBlock` from model entirely | Would change the Application-layer DTO contract; more invasive | ❌ Rejected |

**Rationale**: The spec explicitly requires `ValidConfig` when TenantId + ClientId are present, regardless of ClientSecret. The `HasValidCredentialsBlock` field stays in the DTO but its derivation logic changes — this is the minimal Application-layer impact.

## Data Flow

### API/SignalR Path

```
Browser → API Controller
  → ICurrentUserService.GetCurrentUser() → AuraUser.Oid
  → IGraphClientFactory.CreateClientAsync(oid, ct)
    → _msalApp.GetAccountsAsync()
    → accounts.First(a => a.HomeAccountId.ObjectId == oid)
    → _msalApp.AcquireTokenSilent(scopes, account)
    → GraphServiceClient(delegated token)
  → provider.FetchAsync(request, ct)  [request.Identity.UserOid = oid]
```

### Worker Path

```
ConnectorExecutionWorker.ExecuteAsync()
  → _msalApp.GetAccountsAsync()  [read from SQLite cache]
  → if accounts.Empty → log warning, skip connector
  → accounts.First().HomeAccountId.ObjectId → oid
  → CheckpointIdentity.UserOid = oid
  → ExecuteConnectorUseCase.ExecuteAsync(identity, ct)
    → adapter.ExecuteAsync(request, ct)
      → IGraphClientFactory.CreateClientAsync(oid, ct)
```

### Error Flow: MsalUiRequiredException

```
GraphClientFactory.CreateClientAsync(oid, ct)
  → AcquireTokenSilent throws MsalUiRequiredException
  → propagates to provider
  → provider catches, logs with oid correlation
  → returns ConnectorExecutionResult(Failure, "re-authentication required")
  → ExecuteConnectorUseCase emits telemetry
```

### Error Flow: Graph HTTP Failure

```
GraphServiceClient.Me.Chats.GetAsync(...)
  → Graph API returns 403/503
  → ODataErrorException thrown by SDK
  → provider catches, logs status_code + endpoint + connector
  → returns ConnectorExecutionResult(Failure, reason)
```

## File Changes

| File | Action | Description |
|------|--------|-------------|
| `src/Aura.Infrastructure/Adapters/Connectors/Graph/IGraphClientFactory.cs` | Modify | Add `string oid` as first parameter to `CreateClientAsync` |
| `src/Aura.Infrastructure/Adapters/Connectors/Graph/GraphClientFactory.cs` | Modify | Replace `IConfidentialClientApplication` with `IPublicClientApplication`; filter accounts by `oid` instead of `FirstOrDefault()` |
| `src/Aura.Infrastructure/Adapters/Connectors/Graph/DependencyInjection.cs` | Modify | Replace `ConfidentialClientApplicationBuilder` with `PublicClientApplicationBuilder`; remove `.WithClientSecret()`; keep SQLite cache hooks on `UserTokenCache` |
| `src/Aura.Infrastructure/Adapters/GraphConnector/GraphConnectorOptions.cs` | Modify | Remove `ClientSecret` property |
| `src/Aura.Infrastructure/Adapters/GraphConnector/AppSettingsGraphConnectorSettingsProvider.cs` | Modify | `HasValidCredentials` returns `true` when `ClientId` + `TenantId` are present (no secret check) |
| `src/Aura.Application/Models/CheckpointIdentity.cs` | Modify | Add `string? UserOid` optional property |
| `src/Aura.Application/Models/ConnectorExecutionRequest.cs` | Modify | Add `string? UserOid` property (or derive from `Identity.UserOid`) |
| `src/Aura.Infrastructure/Adapters/Connectors/Graph/GraphTeamsSourceProvider.cs` | Modify | Pass `request.Identity.UserOid` to `CreateClientAsync`; add `MsalUiRequiredException` catch + structured log |
| `src/Aura.Infrastructure/Adapters/Connectors/Graph/GraphOutlookSourceProvider.cs` | Modify | Same pattern as Teams provider |
| `src/Aura.Infrastructure/Adapters/Connectors/Calendar/GraphCalendarEventProvider.cs` | Modify | Same pattern as Teams/Outlook providers |
| `src/Aura.Workers/ConnectorExecutionWorker.cs` | Modify | Resolve oid from token cache accounts before creating `CheckpointIdentity`; skip connector if no accounts |
| `src/Aura.Application/UseCases/IngestionSync/TriggerSyncUseCase.cs` | Modify | Accept optional oid parameter or resolve from `ICurrentUserService` |
| `tests/Aura.UnitTests/GraphConnector/GraphClientFactoryTests.cs` | Modify | Replace `IConfidentialClientApplication` mocks with `IPublicClientApplication`; add oid-based selection tests |
| `src/Aura.Application/Services/GraphConnectorStatusReader.cs` | Modify | Remove `HasValidCredentialsBlock` from `PartialConfig` condition (line 52) |

## Interfaces / Contracts

### IGraphClientFactory (updated)

```csharp
internal interface IGraphClientFactory
{
    /// <exception cref="MsalUiRequiredException">When no valid cached token for the given oid.</exception>
    Task<GraphServiceClient> CreateClientAsync(string oid, CancellationToken ct);
}
```

### CheckpointIdentity (updated)

```csharp
public sealed record CheckpointIdentity
{
    public string Connector { get; }
    public string Source { get; }
    public string Tenant { get; }
    public string? UserOid { get; }  // NEW — optional, null when no user context

    public CheckpointIdentity(string connector, string source, string tenant, string? userOid = null)
    {
        // existing validations unchanged
        Connector = connector;
        Source = source;
        Tenant = tenant;
        UserOid = userOid;
    }
}
```

### GraphConnectorOptions (updated)

```csharp
internal sealed class GraphConnectorOptions
{
    internal const string SectionName = "GraphConnector";
    public bool Enabled { get; set; }
    public string? TenantId { get; set; }
    public string? ClientId { get; set; }
    // ClientSecret REMOVED — delegated flow has no secret
    public string? RedirectUri { get; set; }
    public string[]? Scopes { get; set; }
}
```

### DependencyInjection.cs (updated registration)

```csharp
// REPLACE: IConfidentialClientApplication → IPublicClientApplication
services.AddSingleton<IPublicClientApplication>(sp =>
{
    var opts = sp.GetRequiredService<IOptions<GraphConnectorOptions>>().Value;

    var app = PublicClientApplicationBuilder
        .Create(opts.ClientId)
        .WithTenantId(opts.TenantId)
        .WithRedirectUri(opts.RedirectUri ?? "https://localhost:5001/signin-oidc")
        .Build();

    // SQLite cache hooks — identical pattern, UserTokenCache is the default target
    var tokenCache = sp.GetRequiredService<MsalSqliteTokenCache>();
    var cacheKey = $"msal-user-{opts.ClientId}";

    app.UserTokenCache.SetBeforeAccessAsync(args => { /* same as current */ });
    app.UserTokenCache.SetAfterAccessAsync(args => { /* same as current */ });

    return app;
});
```

### GraphConnectorStatusReader.DeriveState (updated logic)

```csharp
internal static GraphConnectorState DeriveState(GraphConnectorSettings settings)
{
    if (!settings.Enabled) return GraphConnectorState.Disabled;

    var hasTenant = IsPresent(settings.TenantId);
    var hasClient = IsPresent(settings.ClientId);

    if (!hasTenant && !hasClient) return GraphConnectorState.MissingConfig;

    // CHANGED: removed !settings.HasValidCredentialsBlock check
    // Delegated flow needs no credentials block — ClientId + TenantId is sufficient
    if (!hasTenant || !hasClient) return GraphConnectorState.PartialConfig;

    return GraphConnectorState.ValidConfig;
}
```

## Telemetry Design

### Structured Log Messages

All providers follow the same pattern. Event IDs continue the existing 33xx/34xx ranges:

```csharp
// MsalUiRequiredException — Warning level
[LoggerMessage(EventId = 3305, Level = LogLevel.Warning,
    Message = "GraphTeamsSourceProvider token expired for oid={Oid}. Re-authentication required.")]
public static partial void TokenExpired(ILogger logger, string oid);

// Graph HTTP failure — Warning for 4xx, Error for 5xx
[LoggerMessage(EventId = 3306, Level = LogLevel.Warning,
    Message = "GraphTeamsSourceProvider HTTP {StatusCode} from {Endpoint}")]
public static partial void GraphHttpError(ILogger logger, int statusCode, string endpoint);
```

Event ID mapping per provider:

| Provider | TokenExpired | GraphHttpError |
|----------|-------------|----------------|
| Teams | 3305 | 3306 |
| Outlook | 3307 | 3308 |
| Calendar | 3403 | 3404 |

### Metrics

Three new metrics on the existing `Aura.Application.ConnectorExecution` meter or a new `Aura.Infrastructure.GraphConnector` meter:

| Metric | Type | Tags | When |
|--------|------|------|------|
| `graph.token.acquired` | Counter | `connector` | Successful `AcquireTokenSilent` |
| `graph.token.expired` | Counter | `connector`, `oid` | `MsalUiRequiredException` caught |
| `graph.http.error` | Counter | `connector`, `status_code`, `endpoint` | 4xx/5xx from Graph API |

### Provider Catch Pattern

Each provider wraps the Graph call in a try/catch:

```csharp
try
{
    var client = await _clientFactory.CreateClientAsync(request.Identity.UserOid!, ct);
    // ... Graph call ...
}
catch (MsalUiRequiredException ex)
{
    Log.TokenExpired(_logger, request.Identity.UserOid ?? "unknown");
    // emit metric
    return failureResult with { FailureReason = "re-authentication required" };
}
catch (ODataErrorException ex) when (ex.ResponseStatusCode is >= 400 and < 600)
{
    Log.GraphHttpError(_logger, ex.ResponseStatusCode, /* endpoint */ "");
    // emit metric
    return failureResult with { FailureReason = $"Graph HTTP {ex.ResponseStatusCode}" };
}
```

## Worker-Side Design

### Oid Resolution from Token Cache

The worker cannot use `ICurrentUserService` (no HTTP context). Instead, it reads accounts directly from the MSAL public client application:

```csharp
// In ConnectorExecutionWorker.ExecuteAsync:
var msalApp = scope.ServiceProvider.GetRequiredService<IPublicClientApplication>();
var accounts = await msalApp.GetAccountsAsync();

if (!accounts.Any())
{
    Log.NoCachedUser(_logger, adapter.ConnectorName);
    continue;  // skip this connector
}

// For single-user: take the first (and only) account
var oid = accounts.First().HomeAccountId.ObjectId;
var identity = new CheckpointIdentity(
    adapter.ConnectorName,
    GetSource(adapter.ConnectorName),
    "default",
    userOid: oid);
```

### Empty Cache Behavior

When no accounts are cached (user has never logged in via API), the worker:
1. Logs a warning: `ConnectorExecutionWorker skipping {Connector} — no cached user identity`
2. Skips that connector execution
3. Continues to the next adapter
4. Does NOT attempt any Graph call

### Concurrency: SQLite Cache

The SQLite token cache is shared between the API process and the Worker process. Known limitations:
- **Read-write contention**: If the API is writing a token refresh while the Worker reads, SQLite may throw `SQLITE_BUSY`. Mitigation: WAL mode (already used by `MsalSqliteTokenCache`) handles concurrent reads; writes are serialized.
- **Stale reads**: Worker may read a token that was just refreshed by the API. This is benign — the refreshed token is still valid.
- **Future improvement**: Per-user cache files or distributed cache (out of scope).

## Test Strategy

### Unit Tests for GraphClientFactory

| Test | What | How |
|------|------|-----|
| `CreateClientAsync_OidMatch_SelectsCorrectAccount` | Two accounts with different oids; factory selects the right one | Mock `IPublicClientApplication.GetAccountsAsync` returning two accounts; verify `AcquireTokenSilent` called with the matching account |
| `CreateClientAsync_NoMatchingOid_ThrowsMsalUiRequiredException` | Unknown oid → exception | Mock accounts with oids "A" and "B"; call with oid "C"; assert exception |
| `CreateClientAsync_EmptyCache_ThrowsMsalUiRequiredException` | No accounts at all | Mock empty accounts; assert exception with "no_account" error code |
| `CreateClientAsync_ExpiredToken_PropagatesException` | Silent acquisition fails | Mock account exists; mock `AcquireTokenSilent` to throw; assert propagation |
| `CreateClientAsync_Success_ReturnsGraphClient` | Happy path | Mock successful token; verify returned `GraphServiceClient` is not null |

### Unit Tests for Status Derivation

| Test | What | How |
|------|------|-----|
| `DeriveState_ValidConfigWithoutSecret` | TenantId + ClientId → ValidConfig | Settings with both present, no secret |
| `DeriveState_ValidConfigWithSecret` | TenantId + ClientId + Secret → ValidConfig (secret ignored) | Settings with all three |
| `DeriveState_PartialConfig_TenantOnly` | TenantId only → PartialConfig | Settings with only TenantId |
| `DeriveState_PartialConfig_ClientOnly` | ClientId only → PartialConfig | Settings with only ClientId |
| `DeriveState_Disabled_OverridesAll` | Disabled + full config → Disabled | Settings disabled with all fields |

### Unit Tests for Worker Oid Resolution

| Test | What | How |
|------|------|-----|
| `ExecuteAsync_NoCachedAccounts_SkipsConnector` | Empty cache → skip | Mock empty accounts; verify adapter NOT invoked |
| `ExecuteAsync_CachedAccount_PropagatesOid` | Account exists → oid in identity | Mock one account; verify `CheckpointIdentity.UserOid` matches |

### Unit Tests for Providers (Teams/Outlook/Calendar)

| Test | What | How |
|------|------|-----|
| `FetchAsync_MsalUiRequiredException_ReturnsFailure` | Token expired → failure result | Mock factory to throw; assert failure result with "re-authentication" reason |
| `FetchAsync_GraphHttpError_ReturnsFailureWithStatusCode` | 403 response → failure | Mock factory to return client that throws `ODataErrorException(403)`; assert failure |
| `FetchAsync_PassesOidToFactory` | Verify oid propagation | Mock factory; capture `oid` argument; assert matches request identity |

### Integration Test Considerations

- **SQLite cache integration**: Test that `IPublicClientApplication` + SQLite cache round-trips work (acquire → serialize → deserialize → acquire silent). This is the same pattern as current tests but with `PublicClientApplicationBuilder`.
- **DI registration**: Verify `IPublicClientApplication` resolves correctly from the container; verify `IConfidentialClientApplication` is NOT registered.

## Migration Path

### Step-by-Step Order

1. **GraphConnectorOptions**: Remove `ClientSecret` property. Run tests — existing tests that set `ClientSecret` will fail, confirming the property is gone.
2. **AppSettingsGraphConnectorSettingsProvider**: Change `HasValidCredentials` to check `ClientId + TenantId`. Run status derivation tests.
3. **GraphConnectorStatusReader**: Remove `HasValidCredentialsBlock` from `PartialConfig` condition. Run status tests.
4. **IGraphClientFactory**: Add `string oid` parameter. All callers will fail to compile — this is expected.
5. **GraphClientFactory**: Replace `IConfidentialClientApplication` with `IPublicClientApplication`; filter by `oid`. Run factory tests.
6. **DependencyInjection.cs**: Replace builder. Run DI tests.
7. **CheckpointIdentity**: Add optional `UserOid`. Backward-compatible — no tests break.
8. **ConnectorExecutionWorker**: Resolve oid from token cache. Run worker tests.
9. **TriggerSyncUseCase**: Accept oid from caller. Run sync tests.
10. **Teams/Outlook/Calendar providers**: Pass oid to factory; add error catch + telemetry. Run provider tests.
11. **New tests**: Add oid-based selection, multi-user, Graph HTTP failure, MsalUiRequiredException tests.

### Verification Per Step

Each step is verified by `dotnet test Aura.sln`. Steps 1-3 can be verified independently. Steps 4-6 are a compiler-verified atomic unit (interface change forces all callers to update). Steps 7-10 are incremental and testable individually.

### Rollback Points

- **Before step 4**: All existing behavior intact; only status derivation and options changed (backward-compatible).
- **After step 6**: Factory uses `IPublicClientApplication`; all callers updated. Full rollback to `IConfidentialClientApplication` by reverting DI + factory + interface.
- **After step 10**: Full feature complete. Rollback by reverting all files to pre-W2-H10 state.

## Risks and Open Questions

| Risk | Severity | Mitigation |
|------|----------|------------|
| `IPublicClientApplication` in server-side context (no browser) | Low | `AcquireTokenSilent` never triggers redirect; `AcquireTokenInteractive` (not used here) handles browser. Server only uses cached tokens. |
| SQLite cache concurrency between API and Workers | Medium | WAL mode handles concurrent reads. Document as known limitation. Workers should run after API login, not concurrently. |
| Redirect URI for public client in server context | Low | `WithRedirectUri` is required by builder but never used in `AcquireTokenSilent` flow. Set to `https://localhost:5001/signin-oidc` (same as current). |
| Breaking change to `IGraphClientFactory` | Low | All consumers are internal; no external API surface. Updated in same PR. |
| W2-H9 dependency (SQLite cache + oid resolution) | High | T1+T3 (factory + options) can proceed without W2-H9. T2 (oid propagation) blocks on W2-H9 delivering `ICurrentUserService` with `Oid`. |
| `ClientSecret` in appsettings still present but ignored | Low | Status derivation ignores it. Config validation may still bind it. No harm — it's a no-op field. |
| Multi-user scenario (future) | Low | Current design supports single-user only (first account). `oid` parameter makes multi-user trivial to add later by changing the worker's account selection logic. |
