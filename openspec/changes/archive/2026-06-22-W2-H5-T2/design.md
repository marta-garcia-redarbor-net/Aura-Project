# Design: W2-H5-T2 Morning Summary Ranking Rule

## Technical Approach

Implemented deterministic ranking in `Aura.Application` and wired it into Morning Summary composition.
This design now reflects the delivered architecture and tests (no longer docs-only planning).

AI-assisted prioritization remains explicitly out of scope for the implemented ranking path.

---

## Architecture Decisions (Preserved)

| Decision | Choice | Rejected Alternative | Rationale |
|---|---|---|---|
| Policy ownership | `Aura.Application` port + use-case | Domain, Infrastructure | Final ranking is application policy and stays independent from connectors and SDKs |
| Preliminary score representation | Single `RankingFactor.PreliminaryScore` used in two contexts | Two independent rules | Keeps one coherent rule: post-explicit tiebreak + all-explicit-absent fallback |
| Signal source ownership | Deadline/Risk/Pre-score from `WorkItem.Metadata`; Impact from `WorkItem.Priority` | Moving impact to metadata | Preserves canonical impact field while consuming connector-provided metadata where needed |
| AI extension point boundary | Deterministic path has no AI dependency | Inline AI in ranking policy | Keeps explainable deterministic behavior and clean architecture boundaries |

---

## Implemented Data Flow

```text
IWorkItemReader.ReadForWindowAsync(query)
  -> WorkItem[]
      -> IMorningSummaryRankingPolicy.Rank(items)
         1) Explicit order: Deadline -> Impact -> Risk
         2) PreliminaryScore as single secondary input (post-explicit and fallback)
         3) Tiebreak chain: nearest due date -> oldest CreatedAt -> lexical ExternalId
         4) No usable signals/score -> insufficient-signals (ranked last)
      -> RankedWorkItem[] + RankingExplanation
  -> IMorningSummaryComposer.ComposeAsync(...)
  -> MorningSummary.Entries
```

### Composer constructor/fallback nuance

`MorningSummaryComposer` supports two construction paths:

- Full path (`IWorkItemReader` + `IMorningSummaryRankingPolicy`): production composition reads items and ranks them.
- Ranking-only path (`IMorningSummaryRankingPolicy`): when no reader is provided, composition uses an empty item set and still returns a valid `MorningSummary` envelope.

This fallback is intentional to keep composition deterministic and test-friendly without shifting ranking ownership outside Application.

---

## File Changes (Implemented)

### Application

| File | Action | Description |
|---|---|---|
| `src/Aura.Application/Models/RankingFactor.cs` | Modified | Added `PreliminaryScore` |
| `src/Aura.Application/Models/WorkItemSignalKeys.cs` | Created | Added canonical keys for deadline, preliminary score sources, risk score, and due timestamp |
| `src/Aura.Application/Ports/IMorningSummaryRankingPolicy.cs` | Created | Ranking policy contract (`Rank(IReadOnlyList<WorkItem>)`) |
| `src/Aura.Application/UseCases/MorningSummary/MorningSummaryRankingPolicy.cs` | Created | Deterministic ranking implementation and explanation contributions |
| `src/Aura.Application/UseCases/MorningSummary/MorningSummaryComposer.cs` | Created | Composes summary using reader + ranking policy |
| `src/Aura.Application/DependencyInjection.cs` | Modified | Registered `IMorningSummaryRankingPolicy` and `IMorningSummaryComposer` |

### Tests

| File | Action | Description |
|---|---|---|
| `tests/Aura.UnitTests/Triage/MorningSummaryRankingPolicyTests.cs` | Created | Spec-scenario coverage for order, preliminary score behavior, tie chain, and insufficient-signals |
| `tests/Aura.UnitTests/Triage/MorningSummaryComposerTests.cs` | Created | Validates ordered entries and explanation propagation |
| `tests/Aura.UnitTests/Application/DependencyInjectionTests.cs` | Modified | Verifies DI registrations and resolution path |
| `tests/Aura.ArchitectureTests/MorningSummaryArchitectureTests.cs` | Modified | Enforces Application ownership and no AI ranking dependency |

---

## Contracts and Signals (Implemented)

### Ranking port

```csharp
public interface IMorningSummaryRankingPolicy
{
    IReadOnlyList<RankedWorkItem> Rank(IReadOnlyList<WorkItem> items);
}
```

### Ranking factors

```csharp
public enum RankingFactor
{
    Impact,
    Deadline,
    Risk,
    PreliminaryScore
}
```

### Signal keys in use

| Signal | Source |
|---|---|
| Deadline signal | `outlook.deadline.cue`, `outlook.deadline.source` |
| Due timestamp tie input | `outlook.deadline.atUtc` |
| Impact | `WorkItem.Priority` |
| Risk | `triage.risk.score` |
| Preliminary score | `outlook.scoring.totalScore`, `teams.priority.raw` |

---

## Testing and Verification Status

| Layer | Status | Evidence |
|---|---|---|
| Unit | Implemented and passing | Ranking, composer, and DI tests added/updated |
| Architecture | Implemented and passing | Ownership + no-AI-boundary assertions in architecture tests |
| Full regression | Green | `dotnet test Aura.sln` recorded green in apply progress |

---

## Scope Boundary (Still Out of Scope)

- AI-assisted prioritization implementation.
- Connector pre-scoring algorithm changes.
- Scheduling, caching, persistence, API, or UI behavior changes.

---

## Migration / Rollout

No migration required.

---

## Resolved Decisions

- Keep deterministic ranking fully in Application (`IMorningSummaryRankingPolicy` + `MorningSummaryRankingPolicy`).
- Keep `PreliminaryScore` as one factor used in two contexts; do not split into separate rules.
- Keep `insufficient-signals` as an explanation/classification outcome (not a new ranking factor).
- Keep AI references out of the current ranking implementation and architecture path.
