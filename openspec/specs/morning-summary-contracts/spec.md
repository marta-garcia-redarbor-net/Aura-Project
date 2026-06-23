# Morning Summary Contracts Specification

## Purpose

Defines the Application-layer ports and DTO contracts for the daily Morning Summary: composition,
scheduling-window resolution, work-item reading, and the explainable ranking-explanation shape.
This spec describes the contracts only — no runtime behavior, scoring math, scheduler execution,
caching, or UI. Scoring is expressed as a contract shape that is explainable and auditable,
consistent with the global triage governance principles.

## Requirements

| Requirement | Constraint | Strength |
|---|---|---|
| Composer Port | `IMorningSummaryComposer` composes a summary payload for a user/window | MUST |
| Scheduler Port | `IMorningSummaryScheduler` resolves and evaluates the daily window | MUST |
| Work Item Reader Port | `IWorkItemReader` reads work items for a window; port only | MUST |
| Summary Payload Shape | Payload carries user, window, generation instant, ranked entries | MUST |
| Ranking Explanation Shape | Each ranked entry carries an explainable factor breakdown | MUST |
| Contract Purity | Ports/DTOs carry no Infrastructure, SDK, UI, or transport dependency | MUST |
| Empty Window Handling | An empty work-item set yields a valid empty summary | MUST |

---

### Requirement: Composer Port

The system MUST define `IMorningSummaryComposer` in `Aura.Application.Ports` exposing an
asynchronous composition operation that accepts a summary request and a `CancellationToken`
and returns a summary payload. The contract MUST NOT expose implementation, scoring math, or
transport details.

#### Scenario: Composer contract is defined

- GIVEN the Application ports assembly
- WHEN a caller inspects `IMorningSummaryComposer`
- THEN it exposes a single async method taking a summary request and `CancellationToken`
- AND returning the summary payload contract

#### Scenario: Composition is deterministic for caching

- GIVEN the same summary request and the same underlying work-item inputs
- WHEN the contract is documented for consumers
- THEN it states composition SHOULD be deterministic so a later cache adapter is safe

---

### Requirement: Scheduler Port

The system MUST define `IMorningSummaryScheduler` in `Aura.Application.Ports` that resolves a
daily summary window for a user (carrying the user's timezone identifier and target local time)
and evaluates whether a given window is due at an evaluation instant. The contract MUST carry
timezone data but MUST NOT implement timezone resolution (deferred to W2-H5-T3).

#### Scenario: Scheduler resolves a window

- GIVEN a schedule context with a user, timezone identifier, and target local time
- WHEN a caller invokes window resolution
- THEN the contract returns a summary-window value carrying the window date and timezone

#### Scenario: Scheduler evaluates due state

- GIVEN a resolved summary window and an evaluation instant
- WHEN a caller checks whether the window is due
- THEN the contract returns a boolean due indication

---

### Requirement: Work Item Reader Port

The system MUST define `IWorkItemReader` in `Aura.Application.Ports` exposing an asynchronous
read operation that accepts a summary query (user and window bounds) and a `CancellationToken`
and returns a read-only collection of `WorkItem`. No adapter or implementation is provided in
this change.

#### Scenario: Reader contract is defined without implementation

- GIVEN the Application ports assembly
- WHEN a caller inspects `IWorkItemReader`
- THEN it exposes an async read method returning `IReadOnlyList<WorkItem>`
- AND no concrete adapter implementing it exists in the solution

---

### Requirement: Summary Payload Shape

The summary payload contract MUST expose the user identifier, the resolved summary window, the
UTC generation instant, and an ordered read-only collection of ranked entries. Each ranked entry
MUST expose its rank position, the referenced work item, a ranking score value, and a ranking
explanation.

#### Scenario: Payload exposes ordered ranked entries

- GIVEN a constructed summary payload
- WHEN a consumer reads its entries
- THEN entries are an ordered read-only collection
- AND each entry exposes rank, work item, score, and explanation

---

### Requirement: Ranking Explanation Shape

The ranking explanation contract MUST express an explainable, auditable breakdown as an ordered
collection of factor contributions, where each contribution names a ranking factor (at minimum
Impact, Deadline, and Risk), a contribution value, and a human-readable rationale. The contract
MUST NOT compute scores.

#### Scenario: Explanation lists factor contributions

- GIVEN a ranking explanation on a ranked entry
- WHEN a consumer inspects it
- THEN it exposes an ordered collection of factor contributions
- AND each contribution carries a factor, a value, and a human-readable rationale

#### Scenario: Explanation covers the required factors

- GIVEN the ranking factor contract
- WHEN a consumer enumerates the supported factors
- THEN Impact, Deadline, and Risk are representable

---

### Requirement: Contract Purity

All Morning Summary ports and DTOs MUST reside in `Aura.Application` and MUST NOT depend on
`Aura.Infrastructure`, provider SDKs, UI, or transport types.

#### Scenario: Ports carry no infrastructure dependency

- GIVEN the Application assembly containing the Morning Summary ports
- WHEN an architecture test inspects their dependencies
- THEN no dependency on `Aura.Infrastructure`, UI, or external SDK namespaces is present

---

### Requirement: Empty Window Handling

The summary payload contract MUST support an empty result: when no work items exist for a
window, the payload is valid and its ranked-entries collection is empty (never null).

#### Scenario: Empty work-item set yields valid empty summary

- GIVEN a summary composed for a window with no work items
- WHEN a consumer reads the payload
- THEN the payload is valid and its ranked-entries collection is empty and non-null
