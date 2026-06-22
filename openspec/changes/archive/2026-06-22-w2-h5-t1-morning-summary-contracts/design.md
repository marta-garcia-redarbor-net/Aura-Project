# Design: W2-H5-T1 Morning Summary Contracts

## Technical Approach

Define three Application ports and a small set of payload/explanation DTOs that realize the
`morning-summary-contracts` spec. We follow the existing convention: ports under
`src/Aura.Application/Ports/`, DTOs under `src/Aura.Application/Models/`. No runtime code,
adapters, scheduler host, cache, or UI. Scoring is a contract shape only (Decision B); caching
is left as a future Infrastructure concern behind the reader/composer boundary (Decision C);
`IWorkItemReader` ships as a port with no adapter (Decision A). Strict TDD: contract tests and
the architecture boundary test are written first (RED) before the contracts exist (GREEN).

## Architecture Decisions

| Decision | Choice | Alternative rejected | Rationale |
|---|---|---|---|
| Contract location | Ports in `Application.Ports`, DTOs in `Application.Models` | Types in `Aura.Domain` | Matches existing pattern; keeps Domain free of read/query shapes |
| Reader scope | Port only, no adapter | Implement in-memory reader now | Decision A — contract-first, no premature implementation |
| Scoring | Explainable factor contributions, no math | Numeric engine in T1 | Decision B — keep T1 to contracts; engine is T2 |
| Caching | Deterministic composer contract, cache deferred | Add Redis adapter now | Decision C — cache is Infrastructure; contract must stay safe to cache |
| Timezone | Carried in window DTO; resolution deferred | Resolve timezone in scheduler now | T3 owns timezone logic; T1 only shapes the contract |
| DTO style | Immutable `record`/`sealed record` value contracts | Mutable classes | Deterministic, safe to cache, consistent with existing DTOs |

## Data Flow

Contract-level only (no runtime wiring in T1):

    IMorningSummaryScheduler ──resolves──▶ MorningSummaryWindow
            │                                      │
            ▼                                      ▼
    MorningSummaryRequest ──▶ IMorningSummaryComposer ──reads via──▶ IWorkItemReader ──▶ IReadOnlyList<WorkItem>
                                      │
                                      ▼
                               MorningSummary { Entries: RankedWorkItem[] }
                                                         │
                                                         ▼
                                          RankingExplanation { factor contributions }

## File Changes

| File | Action | Description |
|------|--------|-------------|
| `src/Aura.Application/Ports/IMorningSummaryComposer.cs` | Create | Async compose port |
| `src/Aura.Application/Ports/IMorningSummaryScheduler.cs` | Create | Window resolve + due-check port |
| `src/Aura.Application/Ports/IWorkItemReader.cs` | Create | Read port (no adapter) |
| `src/Aura.Application/Models/MorningSummary.cs` | Create | Summary payload record |
| `src/Aura.Application/Models/RankedWorkItem.cs` | Create | Ranked entry record |
| `src/Aura.Application/Models/RankingExplanation.cs` | Create | Factor-contribution breakdown |
| `src/Aura.Application/Models/RankingFactor.cs` | Create | Enum: Impact, Deadline, Risk |
| `src/Aura.Application/Models/MorningSummaryWindow.cs` | Create | Window: date, timezone id, local time, UTC instant |
| `src/Aura.Application/Models/MorningSummaryRequest.cs` | Create | Compose input: user + window |
| `src/Aura.Application/Models/MorningSummaryQuery.cs` | Create | Reader input: user + window bounds |
| `tests/Aura.UnitTests/Triage/MorningSummaryContractTests.cs` | Create | DTO shape + empty-window + fake composer |
| `tests/Aura.ArchitectureTests/MorningSummaryArchitectureTests.cs` | Create | Ports free of Infrastructure/SDK/UI |

## Interfaces / Contracts

```csharp
namespace Aura.Application.Ports;

public interface IMorningSummaryComposer
{
    Task<MorningSummary> ComposeAsync(MorningSummaryRequest request, CancellationToken ct);
}

public interface IMorningSummaryScheduler
{
    MorningSummaryWindow ResolveWindow(MorningSummaryScheduleContext context);
    bool IsWindowDue(MorningSummaryWindow window, DateTimeOffset evaluationInstant);
}

public interface IWorkItemReader
{
    Task<IReadOnlyList<WorkItem>> ReadForWindowAsync(MorningSummaryQuery query, CancellationToken ct);
}
```

```csharp
namespace Aura.Application.Models;

public sealed record MorningSummary(
    string UserId,
    MorningSummaryWindow Window,
    DateTimeOffset GeneratedAtUtc,
    IReadOnlyList<RankedWorkItem> Entries);

public sealed record RankedWorkItem(
    int Rank, WorkItem Item, double Score, RankingExplanation Explanation);

public sealed record RankingExplanation(IReadOnlyList<RankingFactorContribution> Contributions);
public sealed record RankingFactorContribution(RankingFactor Factor, double Value, string Rationale);
public enum RankingFactor { Impact, Deadline, Risk }

public sealed record MorningSummaryWindow(
    DateOnly WindowDate, string UserTimeZoneId, TimeOnly ScheduledLocalTime, DateTimeOffset ScheduledInstantUtc);

public sealed record MorningSummaryRequest(string UserId, MorningSummaryWindow Window);
public sealed record MorningSummaryScheduleContext(string UserId, string UserTimeZoneId, TimeOnly TargetLocalTime, DateOnly WindowDate);
public sealed record MorningSummaryQuery(string UserId, DateTimeOffset FromUtc, DateTimeOffset ToUtc);
```

## Testing Strategy

| Layer | What to Test | Approach |
|-------|-------------|----------|
| Unit | DTO construction, ordered/non-null entries, empty-window summary, explanation factors | xUnit contract tests against records |
| Unit | A test-only fake `IMorningSummaryComposer` satisfies the port and returns a valid payload | In-test fake (no production adapter) |
| Architecture | Morning Summary ports carry no `Aura.Infrastructure`/UI/SDK dependency | NetArchTest, mirroring `DashboardArchitectureTests` |

No integration/E2E layer applies — contracts only.

## Migration / Rollout

No migration required. All files are new and additive; nothing is wired into the running host,
DI container, or UI. Reader has no adapter by design.

## Open Questions

- [ ] Should `RankedWorkItem` reference the full `WorkItem` or a slimmer summary projection?
      Defaulting to full `WorkItem` for T1; revisit if the UI card (W2-H6) needs a leaner shape.
- [ ] Final `Score` type (double vs. a value object) — kept as `double` for T1; T2 may refine.
