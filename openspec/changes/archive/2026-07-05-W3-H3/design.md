# Design: W3-H3 — Focus State UI & Prioritized Queue

## Technical Approach

Four additive changes on existing patterns: (1) nullable `int? PriorityScore` on `WorkItem` + COALESCE sort, (2) SQLite `FocusStateOverrides` table respected by `FocusStateResolver`, (3) persist ALL `InterruptionVerdict` records to new `InterruptionDecisions` table, (4) Dashboard extended with priority counts/ranking. All UI via Blazor Server components calling new API endpoints.

## Architecture Decisions

| Decision | Options | Choice | Rationale |
|----------|---------|--------|-----------|
| Sort strategy | SQL-level COALESCE vs in-memory | SQL `COALESCE(PriorityScore, default)` | Avoids loading all items; matches existing `SqliteWorkItemStore` pattern |
| Override store | New table vs extend user settings | `FocusStateOverrides` table | Simple key-value; no user settings table exists yet |
| Decision persistence | Separate store vs extend outbox | `InterruptionDecisions` table | Outbox is transient (dispatched rows deleted); decisions need permanent audit trail |
| UI state management | Component-local vs service | Component-local `_state` with fetched flags | Existing Blazor pattern (`InboxPreviewPanel.razor`) uses component-local state |
| PriorityScore on entity | Store as column vs computed | Column on `WorkItems` table | Spec requires stable persisted value; computed would drift on each sort |

## Data Flow

```
User (Header dropdown)
  │ PUT /api/focus-state { state: "DeepWork" }
  ▼
FocusStateEndpoints ──→ SqliteFocusStateOverrideStore ──→ FocusStateOverrides table
                                                              │
User (Dashboard loads)                                        │
  │ GET /api/dashboard/preview                                │
  ▼                                                           │
DashboardPreviewReader ──→ FocusStateResolver ────────────────┘
  │                        (checks override before auto-compute)
  ├── WorkItemReader ──→ SqliteWorkItemStore (COALESCE sort)
  └── Priority counts + top-3 computed in-memory

Ingestion pipeline
  ExecuteConnectorUseCase
    └── InterruptionPolicyEngine.EvaluateAsync()
          ├── PriorityScoringService.Score() → WorkItem.PriorityScore
          ├── Persist verdict → SqliteInterruptionDecisionStore
          └── If InterruptNow → NotificationOutboxStore (unchanged)

User opens Decision Log
  │ GET /api/triage/decisions?page=1&pageSize=20
  ▼
TriageEndpoints ──→ SqliteInterruptionDecisionStore (paginated, timestamp DESC)
```

## File Changes

| File | Action | Description |
|------|--------|-------------|
| `src/Aura.Domain/WorkItems/WorkItem.cs` | Modify | Add `int? PriorityScore` property, optional ctor param |
| `src/Aura.Domain/WorkItems/WorkItemPriority.cs` | Modify | Add `GetDefaultScore()` helper |
| `src/Aura.Application/Ports/IFocusStateOverrideStore.cs` | Create | Port for override CRUD |
| `src/Aura.Application/Ports/IInterruptionDecisionStore.cs` | Create | Port for persisting + querying decisions |
| `src/Aura.Application/Models/InterruptionDecisionRecord.cs` | Create | DTO for decision log entries |
| `src/Aura.Application/Models/FocusStateResponse.cs` | Create | API response DTO |
| `src/Aura.Application/Models/PagedResult.cs` | Create | Generic paginated result |
| `src/Aura.Application/Models/DashboardPriorityDto.cs` | Create | Priority counts + top-3 DTO |
| `src/Aura.Application/Models/WorkItemDetailDto.cs` | Modify | Add `int? PriorityScore` |
| `src/Aura.Application/Models/DashboardPreviewDto.cs` | Modify | Add `PriorityScore` to `InboxItemPreviewDto` |
| `src/Aura.Application/Services/FocusStateResolver.cs` | Modify | Inject override store, check before auto-compute |
| `src/Aura.Infrastructure/Adapters/FocusState/SqliteFocusStateOverrideStore.cs` | Create | SQLite impl with `FocusStateOverrides` table |
| `src/Aura.Infrastructure/Adapters/Decisions/SqliteInterruptionDecisionStore.cs` | Create | SQLite impl with `InterruptionDecisions` table |
| `src/Aura.Infrastructure/Adapters/Services/InterruptionPolicyEngine.cs` | Modify | Inject decision store, persist ALL verdicts, capture `PriorityScore` |
| `src/Aura.Infrastructure/Adapters/WorkItems/SqliteWorkItemStore.cs` | Modify | Add `PriorityScore` column, COALESCE sort |
| `src/Aura.Infrastructure/DependencyInjection.cs` | Modify | Register new stores |
| `src/Aura.Api/Endpoints/FocusStateEndpoints.cs` | Create | GET+PUT `/api/focus-state` |
| `src/Aura.Api/Endpoints/TriageEndpoints.cs` | Create | GET `/api/triage/decisions` |
| `src/Aura.Api/Endpoints/DashboardEndpoints.cs` | Modify | Extend preview with priority counts |
| `src/Aura.Api/Endpoints/WorkItemsEndpoints.cs` | Modify | Include PriorityScore in DTO |
| `src/Aura.Api/Program.cs` | Modify | Register new endpoint groups |
| `src/Aura.UI/Models/FocusStateResponse.cs` | Create | UI model |
| `src/Aura.UI/Models/DecisionLogResponse.cs` | Create | UI model |
| `src/Aura.UI/Models/WorkItemDetailResponse.cs` | Modify | Add `int? PriorityScore` |
| `src/Aura.UI/Models/DashboardPreviewResponse.cs` | Modify | Add `PriorityScore` to `InboxItemPreviewResponse` |
| `src/Aura.UI/Services/IFocusStateApiClient.cs` | Create | API client interface |
| `src/Aura.UI/Services/FocusStateApiClient.cs` | Create | API client impl |
| `src/Aura.UI/Services/IDecisionLogApiClient.cs` | Create | API client interface |
| `src/Aura.UI/Services/DecisionLogApiClient.cs` | Create | API client impl |
| `src/Aura.UI/Components/Layout/Header.razor` | Modify | Add `FocusStateBadge` component |
| `src/Aura.UI/Components/Layout/FocusStateBadge.razor` | Create | Color-coded badge + dropdown |
| `src/Aura.UI/Components/Layout/Sidebar.razor` | Modify | Add "Interruption Log" entry |
| `src/Aura.UI/Pages/DecisionLog.razor` | Create | Decision history page at `/triage/decisions` |
| `src/Aura.UI/Program.cs` | Modify | Register new HTTP clients |

## Interfaces / Contracts

```csharp
// Application/Ports/IFocusStateOverrideStore.cs
public interface IFocusStateOverrideStore
{
    Task<FocusStateType?> GetOverrideAsync(string userId, CancellationToken ct);
    Task SetOverrideAsync(string userId, FocusStateType state, CancellationToken ct);
    Task ClearOverrideAsync(string userId, CancellationToken ct);
}

// Application/Ports/IInterruptionDecisionStore.cs
public interface IInterruptionDecisionStore
{
    Task RecordAsync(InterruptionDecisionRecord record, CancellationToken ct);
    Task<PagedResult<InterruptionDecisionRecord>> QueryAsync(
        int page, int pageSize, CancellationToken ct);
}

// Application/Models/InterruptionDecisionRecord.cs
public sealed record InterruptionDecisionRecord(
    Guid WorkItemId, string Title, string SourceType,
    string Decision, int? PriorityScore, string Explanation,
    DateTimeOffset Timestamp, string FocusState);

// Domain helper on WorkItemPriority
public static int GetDefaultScore(this WorkItemPriority p) => p switch
{
    Critical => 100, High => 75, Medium => 50, Low => 25
};
```

## SQLite Schema Changes

```sql
-- FocusStateOverrides (new table)
CREATE TABLE IF NOT EXISTS FocusStateOverrides (
    UserId TEXT PRIMARY KEY,
    State TEXT NOT NULL,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NULL
);

-- InterruptionDecisions (new table)
CREATE TABLE IF NOT EXISTS InterruptionDecisions (
    Id TEXT PRIMARY KEY,
    WorkItemId TEXT NOT NULL,
    Title TEXT NOT NULL,
    SourceType TEXT NOT NULL,
    Decision TEXT NOT NULL,
    PriorityScore INTEGER NULL,
    Explanation TEXT NULL,
    Timestamp TEXT NOT NULL,
    FocusState TEXT NOT NULL
);
CREATE INDEX IF NOT EXISTS IX_InterruptionDecisions_Timestamp
    ON InterruptionDecisions (Timestamp DESC);

-- WorkItems (migration)
ALTER TABLE WorkItems ADD COLUMN PriorityScore INTEGER NULL;
```

## Component Tree

```
MainLayout.razor
├── Header.razor
│   └── FocusStateBadge.razor        ← NEW: badge + dropdown
├── Sidebar.razor                    ← MODIFIED: add "Interruption Log"
└── Body
    ├── PriorityDashboard.razor
    │   └── PrioritySummaryCards.razor ← MODIFIED: PriorityScore badge
    ├── InboxPreviewPanel.razor       ← MODIFIED: top-3 highlight badge
    │   └── PriorityBadge.razor (existing)
    └── DecisionLog.razor             ← NEW: paginated table at /triage/decisions
```

## Sequence Diagrams

### Focus State Override

```
User               Header               FocusStateApi         Api.Endpoints        Store
  │                  │                      │                     │                   │
  │───open dropdown──→                      │                     │                   │
  │──select "DeepWork"──→                   │                     │                   │
  │                  │──PUT {state}─────────→│───persist override──→──────────────────→│
  │                  │                      │                     │                   │
  │                  │←────200 OK────────────│←────────────────────│                   │
  │──badge updates──→│                      │                     │                   │
```

### Interruption Decision Persistence

```
Worker/UseCase         InterruptionPolicyEngine    PriorityScoring    DecisionStore    OutboxStore
    │                          │                        │                  │              │
    │──EvaluateAsync(item)────→│                        │                  │              │
    │                          │──Score(context)────────→│                  │              │
    │                          │←──PriorityScore─────────│                  │              │
    │                          │                         │                  │              │
    │                          │──RecordAsync(decision)───────────────────→│              │
    │                          │                         │                  │              │
    │                          │──EnqueueAsync(if InterruptNow)─────────────│─────────────→│
    │←──Verdict────────────────│                         │                  │              │
```

## Testing Strategy

| Layer | What | Approach |
|-------|------|----------|
| Unit | `WorkItem` constructor accepts `PriorityScore` | Fact: null default, explicit value, round-trip |
| Unit | `FocusStateResolver` checks override before auto-compute | Stub `IFocusStateOverrideStore`, verify resolution order |
| Unit | `InterruptionPolicyEngine` persists ALL verdicts | Mock decision store, verify `RecordAsync` called for InterruptNow/Queue/Defer |
| Unit | COALESCE sort derivation | Fact: Critical→100, Low→25, explicit score wins |
| Unit | Decision log pagination | Test boundary: page size, total count, empty |
| Integration | Override survives app restart | SQLite in-memory, write then read |
| Integration | GET/PUT focus-state API | `WebApplicationFactory`, verify response shape |
| Integration | `/api/triage/decisions` returns paginated results | Seed records, assert page metadata |
| UI | Header badge renders current state | bUnit: render with mock API, assert badge text |
| UI | Decision log loading/empty/error states | bUnit: test all 4 UI states (loading, populated, empty, error+retry) |

## Migration / Rollout

No data migration required. `FocusStateOverrides` and `InterruptionDecisions` are new empty tables. `WorkItems` column addition via safe `ALTER TABLE` with try/catch (existing pattern). Rollback: revert `Program.cs` endpoint registrations, remove new UI files, drop columns (SQLite ignores `ALTER TABLE DROP COLUMN` — full table recreate if needed).

## Open Questions

None. All design decisions resolved by existing patterns and spec constraints.
