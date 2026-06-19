# Tasks: Ingestion Checkpoint Store Contract

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | 170-260 |
| 400-line budget risk | Low |
| Chained PRs recommended | No |
| Suggested split | Single PR |
| Delivery strategy | ask-always |
| Chain strategy | pending |

Decision needed before apply: Yes
Chained PRs recommended: No
Chain strategy: pending
400-line budget risk: Low

### Suggested Work Units

| Unit | Goal | Likely PR | Notes |
|------|------|-----------|-------|
| 1 | Close remediation gaps: audit parity + runtime scenario proof | PR 1 | Base: main |

## Phase 1: Foundation / RED Tests

- [x] 1.1 Create `tests/Aura.UnitTests/Ingestion/CheckpointIdentityTests.cs` with failing theory tests for null/empty `Connector`, `Source`, and `Tenant` guards (Requirement: Checkpoint Identity).
- [x] 1.2 In `tests/Aura.UnitTests/Ingestion/CheckpointIdentityTests.cs`, add RED tests for nullable `IngestionCheckpoint` fields and structural equality.
- [x] 1.3 Create `tests/Aura.ArchitectureTests/IngestionArchitectureTests.cs` with RED NetArchTest rules for provider isolation and `IIngestionCheckpointStore` placement.

## Phase 2: Core Contract / GREEN

- [x] 2.1 Create `src/Aura.Application/Models/CheckpointIdentity.cs` as guarded `sealed record`.
- [x] 2.2 Create `src/Aura.Application/Models/IngestionCheckpoint.cs` with only `string? Cursor` and `DateTimeOffset? ProcessedAt`.
- [x] 2.3 Create `src/Aura.Application/Ports/IIngestionCheckpointStore.cs` with `GetAsync` and `SaveAsync`.
- [x] 2.4 Add XML docs in `IIngestionCheckpointStore` for null `GetAsync` => caller UTC-today window (not stored).

## Phase 3: Integration / Documentation

- [x] 3.1 Update `docs/architecture/ingestion/05-normalization-checkpoints.md` with identity tuple and checkpoint value shape.
- [x] 3.2 In the same file, document first-run `UTC 00:00:00 -> UtcNow` as caller responsibility; adapter deferred.
- [x] 3.3 In the same file, add observability note that runtime lag/throughput metrics are deferred.

## Phase 4: Verification / REFACTOR

- [x] 4.1 Make RED tests pass in `CheckpointIdentityTests.cs` and `IngestionArchitectureTests.cs` without Infrastructure/SDK leaks.
- [x] 4.2 Refactor duplicated arrange/assert patterns in `CheckpointIdentityTests.cs` while preserving spec scenario naming.
- [x] 4.3 Run `dotnet test Aura.sln` and verify coverage for identity guards, nullable value shape, and provider isolation.

## Phase 5: Remediation A / Audit Parity

- [x] 5.1 Create `openspec/changes/W2-H2-T1/apply-progress.md` with RED/GREEN/REFACTOR evidence for apply and remediation tasks.
- [x] 5.2 Add a 9-scenario spec-to-test ledger in `apply-progress.md` with test names and pass status.

## Phase 6: Remediation A / Harness

- [x] 6.1 Create `tests/Aura.UnitTests/Ingestion/Fakes/InMemoryIngestionCheckpointStore.cs` with dictionary replace-on-save semantics.
- [x] 6.2 Create `tests/Aura.UnitTests/Ingestion/Support/IngestionCheckpointCallerHarness.cs` applying UTC-today window on `GetAsync == null`.
- [x] 6.3 Inject deterministic `Func<DateTimeOffset> utcNow` into the harness for stable first-run assertions.

## Phase 7: Remediation A / Runtime Tests

- [x] 7.1 Create `tests/Aura.UnitTests/Ingestion/InMemoryCheckpointStoreContractTests.cs` for identity independence and replacement on same identity.
- [x] 7.2 In `InMemoryCheckpointStoreContractTests.cs`, add `Get` tests for stored checkpoint and null on unknown identity.
- [x] 7.3 In `InMemoryCheckpointStoreContractTests.cs`, add save→get tests for value round-trip and null-field preservation.
- [x] 7.4 Create `tests/Aura.UnitTests/Ingestion/IngestionCheckpointFirstRunWindowTests.cs` for null-checkpoint today-window and existing-checkpoint bypass.

## Phase 8: Remediation A / Re-Verification Readiness

- [x] 8.1 Run new ingestion runtime suites, then `dotnet test Aura.sln --collect:"XPlat Code Coverage"`; append evidence in `apply-progress.md`.
- [x] 8.2 Update `openspec/changes/W2-H2-T1/verify-report.md` so untested scenarios map to passing runtime tests.
- [x] 8.3 Link remediation tasks in `tasks.md` to `apply-progress.md` and final verification evidence (`openspec/changes/W2-H2-T1/apply-progress.md`, `openspec/changes/W2-H2-T1/verify-report.md`).
