# Tasks: Real-User Outlook Sync with Demo Isolation

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | 420-620 |
| 400-line budget risk | Medium |
| Chained PRs recommended | No |
| Suggested split | single PR |
| Delivery strategy | single-pr-default |
| Chain strategy | pending |
| Single-PR feasibility (800-line budget) | Feasible |

Decision needed before apply: No
Chained PRs recommended: No
Chain strategy: pending
400-line budget risk: Medium

### Suggested Work Units

| Unit | Goal | Likely PR | Notes |
|------|------|-----------|-------|
| 1 | End-to-end oid propagation + worker/account isolation + config guard + tests/docs | PR 1 | Base: main; include telemetry, tests, and config docs in same unit |

## Phase 1: Foundation / Identity Boundary

- [x] 1.1 Modify `src/Aura.Api/Endpoints/SyncEndpoints.cs` to inject `ICurrentUserService` and remove MSAL cache lookup from endpoint flow.
- [x] 1.2 Derive `userOid` from `GetCurrentUser()?.Oid`; if missing, return 401/typed failure before `TriggerSyncUseCase` invocation.
- [x] 1.3 Update endpoint telemetry/logs to record identity-source outcome (claims-based oid present/absent) without leaking tokens.

## Phase 2: Worker Account Isolation (TDD)

- [x] 2.1 RED: Extend `tests/Aura.UnitTests/Workers/ConnectorExecutionWorkerTests.cs` for zero-account skip, two-account iteration, and per-account `CheckpointIdentity.UserOid` propagation.
- [x] 2.2 GREEN: Refactor `src/Aura.Workers/ConnectorExecutionWorker.cs` from `FirstOrDefault()` to `foreach account -> foreach adapter` execution.
- [x] 2.3 REFACTOR: Keep warning only for true empty-cache path, preserve cancellation behavior, and keep worker free of Graph SDK/domain leakage.

## Phase 3: Graph Config Safety + Cache Alignment (TDD)

- [x] 3.1 RED: Add/adjust tests in `tests/Aura.UnitTests/GraphConnector/GraphConnectorOptionsExtensionTests.cs` and `GraphConnectorStatusReaderTests.cs` for missing TenantId/ClientId => `Disabled` behavior.
- [x] 3.2 GREEN: Add `IsProductionReady` to `src/Aura.Infrastructure/Adapters/GraphConnector/GraphConnectorOptions.cs` and use it in `src/Aura.Infrastructure/Adapters/Connectors/Graph/DependencyInjection.cs`.
- [x] 3.3 GREEN: Emit structured warning logs in `src/Aura.Infrastructure/Adapters/GraphConnector/DependencyInjection.cs` (or Graph DI boundary) naming missing field(s); never throw/retry-loop on gaps.
- [x] 3.4 REFACTOR: Confirm `src/Aura.Infrastructure/Adapters/Connectors/Graph/GraphClientFactory.cs` keeps oid-keyed selection and no fallback to non-matching accounts.

## Phase 4: Sync Contract Verification

- [x] 4.1 RED: Extend `tests/Aura.UnitTests/Sync/TriggerSyncUseCaseTests.cs` to assert `request.Identity.UserOid` equals the oid passed into `ExecuteAsync(userOid, ct)`.
- [x] 4.2 RED/GREEN: Update `tests/Aura.IntegrationTests/Sync/SyncEndpointTests.cs` with scenario: demo/mock token cannot pass `RequireEntraId` for `POST /api/sync/now`.
- [x] 4.3 GREEN: Add/adjust `tests/Aura.UnitTests/GraphConnector/GraphClientFactoryTests.cs` assertions for unknown oid cache miss (typed failure, no account fallback).

## Phase 5: Documentation / Operational Readiness

- [x] 5.1 Update `docs/architecture/ingestion/02-microsoft-graph-outlook.md` with required production settings and delegated scopes (`Mail.Read`, `User.Read`).
- [x] 5.2 Add coexistence note (demo auth path vs real Graph path) and expected `Disabled` status behavior when config is incomplete.
