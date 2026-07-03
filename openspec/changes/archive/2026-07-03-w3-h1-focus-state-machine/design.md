# Design: W3-H1 — Focus State Machine

## Technical Approach

Rich domain state machine with guarded transitions, following the established `WorkItem`/`WorkItemStatus` pattern (sealed class + enum + per-method guard checks). The `FocusState` entity lives in `Aura.Domain`, the `IFocusStateResolver` port in `Aura.Application.Ports`, and the resolver implementation in `Aura.Application.Services` — clean layer separation per `docs/ai/02-architecture-map.md`. Spec `focus-state-machine` defines the exact transition matrix.

## Architecture Decisions

### Decision: Domain state machine style

| Option | Tradeoff | Decision |
|--------|----------|----------|
| Simple enum + resolver switch | Fewer files, but transition rules hidden in resolver logic | ❌ Rejected |
| **Rich domain with guarded transitions** | More code, but transitions visible in domain, matches WorkItem pattern, guards prevent illegal moves | ✅ **Chosen** |
| Configured transition matrix | Maximum flexibility, but premature — no user config requirement yet | ❌ Rejected |

### Decision: Resolver implementation location

`Aura.Application.Services` over `Aura.Infrastructure` — ports belong in Application layer until they need SDK access. The stub resolver returns `WindowOfOpportunity` by default. Signal sources (calendar, activity, preferences) will be injected when the interruption engine (W3-H2) is built. This keeps the resolver testable without infrastructure dependencies.

### Decision: Transition Matrix

Exactly 6 valid transitions per spec:

| From → To | Allowed | Trigger (future) |
|-----------|---------|-----------------|
| DeepWork → WindowOfOpportunity | ✅ | break |
| WindowOfOpportunity → Away | ✅ | meeting / DND |
| Away → Recovery | ✅ | absence end |
| Away → DeepWork | ✅ | direct refocus |
| Recovery → DeepWork | ✅ | refocus |
| Recovery → WindowOfOpportunity | ✅ | soft-landing |
| Any other | ❌ | `InvalidOperationException` |

## Data Flow

```
Calendar/Activity signals ──→ FocusStateResolver ──→ FocusState (guarded transitions)
                                     │
                                     └── Returns FocusState to consumer (W3-H2 interruption engine)
```

The resolver is a deterministic function of its inputs: calendar events, time-of-day heuristics, and user preferences. Same inputs → same state. No external side effects.

## File Changes

| File | Action | Purpose |
|------|--------|---------|
| `src/Aura.Domain/FocusState/FocusStateType.cs` | Create | Enum: DeepWork, WindowOfOpportunity, Away, Recovery |
| `src/Aura.Domain/FocusState/FocusState.cs` | Create | Sealed class with 6 guarded transitions |
| `src/Aura.Application/Ports/IFocusStateResolver.cs` | Create | Port: `Task<FocusState> ResolveAsync(...)` |
| `src/Aura.Application/Services/FocusStateResolver.cs` | Create | Stub impl — returns WindowOfOpportunity by default |
| `src/Aura.Application/DependencyInjection.cs` | Modify | Register `IFocusStateResolver` → `FocusStateResolver` |
| `tests/Aura.UnitTests/Triage/FocusStateMachineTests.cs` | Create | Unit tests for transitions + resolver |
| `docs/architecture/triage/03-focus-state-machine.md` | Modify | Update from "deferred" to implemented |

## Interfaces / Contracts

```csharp
// IFocusStateResolver — Port in Aura.Application.Ports
public interface IFocusStateResolver
{
    Task<FocusState> ResolveAsync(
        string userId,
        CancellationToken cancellationToken = default);
}
```

No other new interfaces. `FocusState` is the domain entity consumed by callers — no separate DTO needed at this layer.

## Testing Strategy

| Layer | What to Test | Approach |
|-------|-------------|----------|
| Unit | FocusState transitions (6 valid + all invalid per state) | xUnit `[Fact]`, arrange entity in starting state, call transition, assert state or exception |
| Unit | FocusState initial state | Construct new FocusState, assert WindowOfOpportunity |
| Unit | Resolver determinism | Same inputs → same state on 2 calls |
| Unit | Resolver default | Default resolver returns WindowOfOpportunity for any userId |

Test naming: `Method_Scenario_Expected` (e.g. `TryEnterDeepWork_FromRecovery_ChangesState`). No mocking framework — manual stubs. Follow existing pattern in `MorningSummaryRankingPolicyTests.cs`.

## Migration / Rollout

No migration required. This is a net-new domain capability with no existing data or consumers. The resolver stub is safe to deploy — it returns a constant until W3-H2 wires real signals.

## Open Questions

- [ ] Signal priority: when calendar (meeting → Away) conflicts with user preference (DeepWork), which wins? Spec says calendar > time-of-day > preferences, but needs team validation.
- [ ] Cooldown enforcement: should transitions have minimum time between state changes out of scope for now? (Proposal says yes — document and defer.)
