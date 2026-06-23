# Design: W2-H5-T3 Morning Summary Timezone Scheduling

## Technical Approach

Provider-based settings + SQLite emission guard, mirroring the existing `GraphConnector` adapter
pattern exactly. Three new Application ports (`IMorningSummarySettingsProvider`,
`IMorningSummaryEmissionStore`, `IMorningSummaryScheduler`) keep all infrastructure dependencies
behind the ports boundary. The scheduler resolves the timezone chain and due-state internally;
the emission store owns all guard state transitions including the override seam.
No Domain changes; `TimeZoneInfo` is BCL, so timezone resolution lives in Application.

---

## Architecture Decisions

| Option | Tradeoff | Decision |
|--------|----------|----------|
| Scheduler resolves settings via `IMorningSummarySettingsProvider` port | + Application stays clean from `IOptions<T>`; + fully substitutable in tests | ✅ Chosen — mirrors `IGraphConnectorSettingsProvider` exactly |
| Caller resolves settings and injects them into the scheduler | + fewer ports; − spreads config knowledge into the worker/orchestration layer | ❌ Rejected — conflicts with proposal constraint |
| `TimeZoneInfo.FindSystemTimeZoneById` for IANA resolution with fallback chain | + BCL, DST-correct, no extra packages | ✅ Chosen — resolved inside `MorningSummaryScheduler` (Application) |
| Fixed UTC offset arithmetic | + simple; − DST-incorrect | ❌ Rejected — violates spec |
| SQLite-backed emission guard (`SqliteMorningSummaryEmissionStore`) | + restart-safe; + mirrors `SqliteSemanticOutboxRepository` pattern | ✅ Chosen |
| Shared SQLite file target for feature tables | + keeps one persistence topology; + allows multiple feature tables to coexist | ✅ Chosen — shared file named `aura.db` |
| In-memory emission guard | + trivially simple; − not restart-safe | ❌ Rejected — spec requires persistence |
| Mark + Reset on `IMorningSummaryScheduler` (single worker dependency) | + simpler worker; − mixes resolution with state mutation | ❌ Rejected — scheduler = pure due-state reader |
| Mark + Reset on `IMorningSummaryEmissionStore` (split ownership) | + clean separation of concerns; worker depends on two ports | ✅ Chosen — store owns all guard transitions |
| Worker guard `userId` source in single-operator mode | + deterministic keying; + no HTTP/user-context dependency in worker | ✅ Chosen — fixed system-level identity |

---

## Data Flow

```
MorningSummarySchedulingWorker
    │
    ├─→ IMorningSummaryScheduler.ResolveAsync(userId, ct)
    │       ├─→ IMorningSummarySettingsProvider.GetSettings()
    │       │       → { TimezoneId?, TargetLocalTime }
    │       ├─ TimeZoneInfo: configured → TimeZoneInfo.Local → TimeZoneInfo.Utc
    │       ├─ DateTimeOffset.UtcNow → local wall-clock in resolved TZ
    │       ├─→ IMorningSummaryEmissionStore.HasBeenEmittedAsync(userId, localDate)
    │       └─→ MorningSummaryDueState { IsDue, ResolvedTimezoneId, LocalDate, TargetLocalTime }
    │
    ├─ if IsDue = true → [emit morning summary — out of scope this slice]
    │       └─→ IMorningSummaryEmissionStore.MarkEmittedAsync(userId, dueState.LocalDate, ct)
    │
    └─ [override seam — prepared, not wired to UI]
            └─→ IMorningSummaryEmissionStore.ResetAsync(userId, localDate, ct)
```

---

## File Changes

| File | Action | Description |
|------|--------|-------------|
| `src/Aura.Application/Ports/IMorningSummarySettingsProvider.cs` | Create | `GetSettings()` → `MorningSummarySettings` |
| `src/Aura.Application/Ports/IMorningSummaryEmissionStore.cs` | Create | `HasBeenEmittedAsync`, `MarkEmittedAsync`, `ResetAsync` |
| `src/Aura.Application/Ports/IMorningSummaryScheduler.cs` | Create | `ResolveAsync(userId, ct)` → `MorningSummaryDueState` |
| `src/Aura.Application/Models/MorningSummarySettings.cs` | Create | `sealed record`: `string? TimezoneId`, `TimeOnly TargetLocalTime` |
| `src/Aura.Application/Models/MorningSummaryDueState.cs` | Create | `sealed record`: `bool IsDue`, `string ResolvedTimezoneId`, `DateOnly LocalDate`, `TimeOnly TargetLocalTime` |
| `src/Aura.Application/UseCases/MorningSummaryScheduling/MorningSummaryScheduler.cs` | Create | Implements `IMorningSummaryScheduler`; owns timezone resolution + guard check |
| `src/Aura.Application/DependencyInjection.cs` | Modify | `AddScoped<IMorningSummaryScheduler, MorningSummaryScheduler>()` |
| `src/Aura.Infrastructure/Adapters/MorningSummaryScheduling/MorningSummaryOptions.cs` | Create | `internal sealed class`; `SectionName = "MorningSummary"`; `TimezoneId?`, `TargetLocalTime = "09:00"` |
| `src/Aura.Infrastructure/Adapters/MorningSummaryScheduling/AppSettingsMorningSummarySettingsProvider.cs` | Create | Implements `IMorningSummarySettingsProvider` via `IOptionsMonitor<MorningSummaryOptions>` |
| `src/Aura.Infrastructure/Adapters/MorningSummaryScheduling/SqliteMorningSummaryEmissionStore.cs` | Create | SQLite-backed in shared `aura.db`; `InitializeSchema`; PK `(UserId TEXT, LocalDate TEXT)` |
| `src/Aura.Infrastructure/Adapters/MorningSummaryScheduling/DependencyInjection.cs` | Create | `internal static`; binds options section + registers both adapters |
| `src/Aura.Infrastructure/DependencyInjection.cs` | Modify | Call `AddMorningSummarySchedulingAdapters(configuration)` |
| `src/Aura.Workers/MorningSummarySchedulingWorker.cs` | Create | `BackgroundService`; calls scheduler, marks emission using fixed system-level `userId`; `[LoggerMessage]` source gen |
| `src/Aura.Workers/Program.cs` | Modify | `AddHostedService<MorningSummarySchedulingWorker>()` in full mode |
| `tests/Aura.UnitTests/MorningSummary/MorningSummarySchedulerTests.cs` | Create | NSubstitute; timezone fallback, due-state, guard-block scenarios |
| `tests/Aura.UnitTests/MorningSummary/SqliteMorningSummaryEmissionStoreTests.cs` | Create | In-memory SQLite; emit, duplicate block, reset, new-day reset, restart survival |

---

## Interfaces / Contracts

```csharp
// Application/Ports
public interface IMorningSummarySettingsProvider
{
    MorningSummarySettings GetSettings();
}

public interface IMorningSummaryEmissionStore
{
    Task<bool> HasBeenEmittedAsync(string userId, DateOnly localDate, CancellationToken ct);
    Task MarkEmittedAsync(string userId, DateOnly localDate, CancellationToken ct);
    Task ResetAsync(string userId, DateOnly localDate, CancellationToken ct); // override seam
}

public interface IMorningSummaryScheduler
{
    Task<MorningSummaryDueState> ResolveAsync(string userId, CancellationToken ct);
}

// Application/Models
public sealed record MorningSummarySettings(string? TimezoneId, TimeOnly TargetLocalTime);

public sealed record MorningSummaryDueState(
    bool IsDue,
    string ResolvedTimezoneId,
    DateOnly LocalDate,
    TimeOnly TargetLocalTime);
```

**SQLite emission guard schema** (non-obvious — surfaces PK contract):

```sql
CREATE TABLE IF NOT EXISTS MorningSummaryEmission (
    UserId   TEXT NOT NULL,
    LocalDate TEXT NOT NULL,  -- ISO 8601 date, e.g. "2026-06-23"
    EmittedAt TEXT NOT NULL,
    PRIMARY KEY (UserId, LocalDate)
);
```

---

## Testing Strategy

| Layer | What to Test | Approach |
|-------|-------------|----------|
| Unit | Timezone resolution: configured → system → UTC fallback | `MorningSummarySchedulerTests`; NSubstitute settings provider; override `TimeZoneInfo` resolution via test doubles |
| Unit | Due-state: before / at / after `targetLocalTime` | Inject controlled UTC instant via `DateTimeOffset`; assert `IsDue` and `ResolvedTimezoneId` |
| Unit | Guard blocks same-day duplicate | `HasBeenEmittedAsync` returns `true`; assert `IsDue = false` without clock check |
| Integration | Store persists across simulated restart | Write guard → dispose connection → new `SqliteMorningSummaryEmissionStore` on same file → read |
| Integration | Override seam resets guard | Mark → Reset → `HasBeenEmittedAsync` = `false` |

---

## Migration / Rollout

No migration required. Change is fully additive (new ports, models, use case, adapters, worker).
Rollback: remove `MorningSummarySchedulingWorker` hosted service registration and the
`AddMorningSummarySchedulingAdapters` call; no existing capability is modified.

---

## Resolved Follow-up Decisions

- SQLite persistence reuses the existing approach with a shared database target named
  `aura.db`, allowing multiple feature tables to coexist in one file.
- For the current single-operator scope, the worker uses a fixed system-level user
  identity for the Morning Summary emission guard key.
