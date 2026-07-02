# Tasks: Outlook Unread-Only

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | 510–680 |
| 400-line budget risk | High |
| Chained PRs recommended | Yes |
| Suggested split | PR 1: DTO+Query → PR 2: Store → PR 3: UseCase → PR 4: Integration |
| Delivery strategy | ask-on-risk |
| Chain strategy | pending |

Decision needed before apply: Yes
Chained PRs recommended: Yes
Chain strategy: pending
400-line budget risk: High

### Suggested Work Units

| Unit | Goal | Likely PR | Notes |
|------|------|-----------|-------|
| 1 | DTO + Graph query with $filter/select | PR 1 | Infrastructure; tests included. ~90–125 lines |
| 2 | Store port + SQLite/InMemory impl | PR 2 | Application port + Infra + schema migration. ~210–275 lines |
| 3 | Diff lifecycle in ExecuteConnectorUseCase | PR 3 | Application use case; depends on PR 2. ~120–160 lines |
| 4 | Integration + architecture tests | PR 4 | SQLite round-trip, isolation, layer enforcement. ~90–120 lines |

## Phase 1: DTO + Graph Query (TDD)

- [x] 1.1 RED: Write test verifying `$filter=isRead eq false` in query URL and `isRead` in `$select`
- [x] 1.2 RED: Write test verifying `OutlookEmailDto.IsRead` maps correctly from Graph payload
- [x] 1.3 GREEN: Add `bool IsRead { get; init; }` to `OutlookEmailDto`
- [x] 1.4 GREEN: Change `GraphOutlookSourceProvider` to `/me/mailFolders/inbox/messages`, add `$filter=isRead eq false` and `isRead` to `$select`, map `msg.IsRead` to DTO
- [x] 1.5 REFACTOR: Verify tests pass

## Phase 2: Store Operations (TDD)

- [x] 2.1 RED: Write unit tests for `GetPendingExternalIdsAsync` (returns only `"Pending"` TEXT status, filters by source, empty set on none)
- [x] 2.2 RED: Write unit tests for `MarkCompletedAsync` (batch status change, ignores non-existent IDs, includes Source filter)
- [x] 2.3 GREEN: Add `GetPendingExternalIdsAsync` and `MarkCompletedAsync` to `IWorkItemStore`
- [x] 2.4 GREEN: Implement `GetPendingExternalIdsAsync` in `SqliteWorkItemStore` — `SELECT ExternalId FROM WorkItems WHERE Status = 'Pending' AND Source = @source`
- [x] 2.5 GREEN: Implement `MarkCompletedAsync` in `SqliteWorkItemStore` — `UPDATE WorkItems SET Status = 'Completed', UpdatedAt = @now WHERE ExternalId IN (...) AND Source = @source`; add `ALTER TABLE WorkItems ADD COLUMN UpdatedAt TEXT` to `InitializeSchema`
- [x] 2.6 GREEN: Implement both methods in `InMemoryWorkItemStore`
- [x] 2.7 REFACTOR: Verify all store unit tests pass

## Phase 3: Diff Lifecycle (TDD)

- [x] 3.1 RED: Write test — pending IDs minus batch IDs calls `MarkCompletedAsync` with absent IDs
- [x] 3.2 RED: Write test — Graph adapter error skips diff (no `MarkCompletedAsync` call)
- [x] 3.3 RED: Write test — non-Outlook connector (e.g. Teams) skips diff
- [x] 3.4 RED: Write test — inbox-zero (empty batch, no error) marks all pending as completed
- [x] 3.5 GREEN: Add `RunDiffLifecycleAsync` to `ExecuteConnectorUseCase` — capture batch ExternalIds before `Drain()`, compute `pending.Except(batch)`, call `MarkCompletedAsync` for absents; guard on source type and Graph error
- [x] 3.6 REFACTOR: Verify all diff lifecycle tests pass

## Phase 4: Integration + Architecture Tests

- [x] 4.1 Write SQLite round-trip integration test: insert items, `GetPendingExternalIdsAsync`, `MarkCompletedAsync`, verify final state
- [x] 4.2 Write SQLite isolation test: Teams items remain `Pending` after Outlook diff executes
- [x] 4.3 Write architecture layer tests: new dependencies stay within Clean Architecture bounds (Domain not leaking into Infra, etc.)
