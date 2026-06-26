# Proposal: W2-H10 — Alinear Flujo Microsoft Graph al Contexto Delegado del Usuario

## Intent

Teams, Outlook y Calendar consumen Microsoft Graph exclusivamente con tokens delegados del usuario autenticado, eliminando app-only patterns, propagando `oid` como clave de correlación, y cubriendo fallos de Graph con tests y telemetría.

## Scope

### In Scope

- **T1**: Centralizar adquisición de tokens delegados — factoría reutilizable para API, SignalR y Workers
- **T2**: Propagar `oid` del usuario autenticado hacia Graph y Calendar como clave de correlación
- **T3**: Eliminar `ClientSecret` y `IConfidentialClientApplication` del flujo delegado, migrando a `IPublicClientApplication`
- **T4**: Tests y telemetría para fallos de Graph (4xx/5xx), expiración de token, multi-usuario, y `MsalUiRequiredException`

### Out of Scope

- Reescritura completa del sistema de autenticación (W2-H9 es prerequisite, no parte de este cambio)
- Soporte multi-tenant real (se mantiene `"default"` por ahora, pero el oid permite evolucionar)
- Cambios en el UI Blazor (el panel de estado del conector no se modifica funcionalmente)
- GitHub connector (no usa Graph)
- Refresh token rotation o manejo de token revocation

## Capabilities

### New Capabilities

None — this change modifies existing capabilities only.

### Modified Capabilities

- `graph-connector-status`: Status derivation logic must change — `HasValidCredentials` currently checks `ClientSecret`, must change to check delegated config (ClientId + TenantId only). The ValidConfig state no longer requires a credentials block.
- `connector-execution`: Workers must propagate user `oid` through `CheckpointIdentity` and resolve it from token cache. Telemetry must include `MsalUiRequiredException` handling.
- `calendar-ingestion`: Calendar adapter must propagate user identity when creating Graph client (same pattern as Teams/Outlook).

## Approach

### Core Decision: `IConfidentialClientApplication` → `IPublicClientApplication`

The current code uses `ConfidentialClientApplicationBuilder` with `.WithClientSecret()` — this is app-only pattern that contradicts the delegated auth model. The fix:

1. **Replace** `IConfidentialClientApplication` with `IPublicClientApplication` in DI registration
2. **Remove** `ClientSecret` property from `GraphConnectorOptions`
3. **Remove** `.WithClientSecret()` from `DependencyInjection.cs:49`
4. **Keep** `AcquireTokenSilent` — it works identically on `IPublicClientApplication` for delegated tokens
5. **Add** `oid` parameter to `IGraphClientFactory.CreateClientAsync(string oid, CancellationToken ct)` to select the correct cached account

### Account Selection Fix

Current: `accounts.FirstOrDefault()` — picks ANY cached account.
Fixed: Filter by `account.HomeAccountId.ObjectId` matching the provided `oid`.

### User Identity Propagation Chain

```
API/SignalR → ICurrentUserService.GetOidAsync()
  → IGraphClientFactory.CreateClientAsync(oid, ct)
    → accounts.FirstOrDefault(a => a.HomeAccountId.ObjectId == oid)
      → AcquireTokenSilent(scopes, account)
        → GraphServiceClient with delegated token
```

For Workers: read `oid` from a persisted user context or signal, not from HTTP context.

### Telemetry Additions

- Log `MsalUiRequiredException` with `oid` correlation in all three providers
- Add structured log for Graph HTTP failures (status code, endpoint, connector name)
- Metric: `graph.token.acquired` (success), `graph.token.expired` (re-auth needed), `graph.http.error` (by status code)

## Incompatibilities and Resolutions

| # | Incompatibility | File | Resolution |
|---|-----------------|------|------------|
| 1 | `IConfidentialClientApplication` in DI | `DependencyInjection.cs:43-79` | Replace with `IPublicClientApplication` via `PublicClientApplicationBuilder` |
| 2 | `.WithClientSecret()` call | `DependencyInjection.cs:49` | Remove entirely — delegated flow has no client secret |
| 3 | `ClientSecret` property in options | `GraphConnectorOptions.cs:13` | Remove property |
| 4 | `HasValidCredentials` checks `ClientSecret` | `AppSettingsGraphConnectorSettingsProvider.cs:30` | Change to check `ClientId` + `TenantId` only (delegated needs no secret) |
| 5 | `CreateClientAsync` has no user context | `IGraphClientFactory.cs:11` | Add `string oid` parameter |
| 6 | `accounts.FirstOrDefault()` — wrong account selection | `GraphClientFactory.cs:36` | Filter by `oid` match on `HomeAccountId.ObjectId` |
| 7 | `CheckpointIdentity` has no user oid | `CheckpointIdentity.cs:3-23` | Add optional `UserOid` property |
| 8 | Workers hardcode `"default"` tenant, no oid | `ConnectorExecutionWorker.cs:44-47` | Resolve oid from token cache or injected user context |
| 9 | `TriggerSyncUseCase` hardcodes `"default"` tenant | `TriggerSyncUseCase.cs:92` | Accept oid from caller or resolve from `ICurrentUserService` |
| 10 | Three providers call `CreateClientAsync(ct)` without oid | `GraphTeamsSourceProvider.cs:30`, `GraphOutlookSourceProvider.cs:30`, `GraphCalendarEventProvider.cs:26` | Pass oid from `ConnectorExecutionRequest` or resolved user context |
| 11 | Tests mock `IConfidentialClientApplication` | `GraphClientFactoryTests.cs:12` | Update mocks to `IPublicClientApplication` |
| 12 | No test for oid-based account selection | — | Add test: oid match returns correct account |
| 13 | No test for Graph HTTP failures | — | Add tests for 4xx/5xx responses |
| 14 | No test for token expiration mid-flight | — | Add test for `MsalUiRequiredException` propagation |
| 15 | No test for multi-user scenario | — | Add test: multiple cached accounts, oid selects correct one |
| 16 | No telemetry for `MsalUiRequiredException` in providers | — | Add structured logging in all three providers |

## Affected Areas

| Area | Impact | Description |
|------|--------|-------------|
| `src/Aura.Infrastructure/Adapters/Connectors/Graph/DependencyInjection.cs` | Modified | Replace `IConfidentialClientApplication` → `IPublicClientApplication` |
| `src/Aura.Infrastructure/Adapters/Connectors/Graph/GraphClientFactory.cs` | Modified | Add `oid` parameter, filter accounts by oid |
| `src/Aura.Infrastructure/Adapters/Connectors/Graph/IGraphClientFactory.cs` | Modified | Add `oid` parameter to interface |
| `src/Aura.Infrastructure/Adapters/Connectors/Graph/GraphTeamsSourceProvider.cs` | Modified | Pass oid to `CreateClientAsync`, add telemetry |
| `src/Aura.Infrastructure/Adapters/Connectors/Graph/GraphOutlookSourceProvider.cs` | Modified | Pass oid to `CreateClientAsync`, add telemetry |
| `src/Aura.Infrastructure/Adapters/Connectors/Calendar/GraphCalendarEventProvider.cs` | Modified | Pass oid to `CreateClientAsync`, add telemetry |
| `src/Aura.Infrastructure/Adapters/GraphConnector/GraphConnectorOptions.cs` | Modified | Remove `ClientSecret` property |
| `src/Aura.Infrastructure/Adapters/GraphConnector/AppSettingsGraphConnectorSettingsProvider.cs` | Modified | Change `HasValidCredentials` logic |
| `src/Aura.Application/Models/CheckpointIdentity.cs` | Modified | Add optional `UserOid` field |
| `src/Aura.Workers/ConnectorExecutionWorker.cs` | Modified | Resolve user oid from token cache |
| `src/Aura.Application/UseCases/IngestionSync/TriggerSyncUseCase.cs` | Modified | Accept/resolved oid for identity |
| `tests/Aura.UnitTests/GraphConnector/GraphClientFactoryTests.cs` | Modified | Update to `IPublicClientApplication`, add oid tests |
| `openspec/specs/graph-connector-status/spec.md` | Delta | ValidConfig no longer requires credentials block |
| `openspec/specs/connector-execution/spec.md` | Delta | CheckpointIdentity includes UserOid |

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| W2-H9 not yet delivered — delegated auth prereq missing | High | T2 depends on W2-H9-T2 (oid resolution) and W2-H9-T3 (SQLite cache). Implement T1+T3 first; T2 blocks on W2-H9. |
| Worker-side: no HTTP context for user oid | Med | Workers must resolve oid from persisted token cache (user account in SQLite). If no account cached, worker logs warning and skips connector. |
| SQLite token cache concurrency between Api and Workers | Med | Document as known limitation. Workers should be triggered after API login (not concurrent). Future: per-user cache files or distributed cache. |
| `IPublicClientApplication` redirect URI in server-side context | Low | `AcquireTokenSilent` never triggers redirect. `AcquireTokenInteractive` (used in API login flow, not here) handles redirect via browser. |
| Breaking change to `IGraphClientFactory` interface | Low | All consumers are internal; no external API surface. Update all three providers in same PR. |

## Rollback Plan

1. **Revert DI registration**: restore `IConfidentialClientApplication` and `ClientSecret` in `DependencyInjection.cs`
2. **Revert interface**: restore `CreateClientAsync(CancellationToken)` without `oid`
3. **Revert options**: restore `ClientSecret` property in `GraphConnectorOptions`
4. **Revert status provider**: restore `HasValidCredentials` checking `ClientSecret`
5. **Run full test suite**: `dotnet test Aura.sln` — all existing tests must pass
6. All changes are in Infrastructure layer — no Domain or Application contracts change signature (only `CheckpointIdentity` gains an optional field, which is backward-compatible)

## Dependencies

- **W2-H9** (prerequisite): Delegated auth end-to-end must be delivered first. T1 and T3 can proceed in parallel; T2 blocks on W2-H9-T2 (oid resolution) and W2-H9-T3 (SQLite cache).

## Success Criteria

- [ ] `IConfidentialClientApplication` is no longer referenced in the Graph connector DI registration
- [ ] `ClientSecret` is removed from `GraphConnectorOptions` and config validation
- [ ] `IGraphClientFactory.CreateClientAsync` accepts `oid` and filters accounts by oid match
- [ ] `CheckpointIdentity` carries optional `UserOid` for correlation
- [ ] All three providers (Teams, Outlook, Calendar) pass oid to factory and log `MsalUiRequiredException`
- [ ] Workers resolve oid from token cache before connector execution
- [ ] Tests cover: oid-based selection, multi-user, Graph HTTP failures, token expiration
- [ ] `dotnet test Aura.sln` passes with zero regressions
- [ ] No `ClientSecret` or app-only pattern remains in the delegated flow codepath

## PR Strategy

Single PR — the total change (~500 lines) fits within the 600-line review budget.

- **One PR**: All four tasks (T1–T4) in a single review. Easier to trace intent and test the full flow atomically.
- **Review focus**: The PR will touch Infrastructure layer primarily (DI, adapters, options), with a small delta in Application (CheckpointIdentity) and tests. No Domain or Api contract changes.
