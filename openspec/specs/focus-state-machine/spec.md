# Focus State Machine Specification

## Purpose

Define the domain state machine for user focus: the `FocusStateType` enum, the `FocusState` sealed class with guarded transitions, the `IFocusStateResolver` port, and the deterministic resolver that maps signals to focus states.

## Requirements

### Requirement: FocusStateType Enum

`FocusStateType` MUST define exactly four members: `DeepWork`, `WindowOfOpportunity`, `Away`, and `Recovery`. No other values SHALL be valid.

#### Scenario: All four states exist

- GIVEN the `FocusStateType` enum
- WHEN a caller enumerates its values
- THEN exactly `DeepWork`, `WindowOfOpportunity`, `Away`, and `Recovery` are present

---

### Requirement: FocusState Initial State

`FocusState` MUST begin in `WindowOfOpportunity` upon construction. This is the default receptive state.

#### Scenario: New instance starts in WindowOfOpportunity

- GIVEN a newly constructed `FocusState`
- WHEN its current state is inspected
- THEN it equals `WindowOfOpportunity`

---

### Requirement: Guarded State Transitions

`FocusState` MUST expose methods for each allowed transition. An `InvalidOperationException` MUST be thrown for any disallowed transition.

| From | To | Allowed |
|------|----|---------|
| DeepWork | WindowOfOpportunity | ✅ |
| WindowOfOpportunity | Away | ✅ |
| Away | Recovery | ✅ |
| Away | DeepWork | ✅ |
| Recovery | DeepWork | ✅ |
| Recovery | WindowOfOpportunity | ✅ |
| Any other pair | — | ❌ |

#### Scenario: DeepWork transitions to WindowOfOpportunity

- GIVEN a `FocusState` in `DeepWork`
- WHEN the break transition is invoked
- THEN the state equals `WindowOfOpportunity`

#### Scenario: WindowOfOpportunity transitions to Away

- GIVEN a `FocusState` in `WindowOfOpportunity`
- WHEN the Away transition is invoked
- THEN the state equals `Away`

#### Scenario: Away transitions to Recovery

- GIVEN a `FocusState` in `Away`
- WHEN the end-of-absence transition is invoked
- THEN the state equals `Recovery`

#### Scenario: Away transitions to DeepWork

- GIVEN a `FocusState` in `Away`
- WHEN a direct focus-return transition is invoked
- THEN the state equals `DeepWork`

#### Scenario: Recovery transitions to DeepWork

- GIVEN a `FocusState` in `Recovery`
- WHEN a refocus transition is invoked
- THEN the state equals `DeepWork`

#### Scenario: Recovery transitions to WindowOfOpportunity

- GIVEN a `FocusState` in `Recovery`
- WHEN a soft-landing transition is invoked
- THEN the state equals `WindowOfOpportunity`

#### Scenario: Invalid transition throws

- GIVEN a `FocusState` in any state
- WHEN a disallowed transition is invoked (e.g., `DeepWork` → `Away`)
- THEN `InvalidOperationException` is thrown
- AND the state remains unchanged

---

### Requirement: IFocusStateResolver Port

The system MUST define `IFocusStateResolver` in `Aura.Application.Ports` exposing `Task<FocusState> ResolveAsync(UserId userId, CancellationToken ct)`. The contract MUST NOT reference infrastructure, UI, or transport types.

#### Scenario: Port contract is defined

- GIVEN the Application ports assembly
- WHEN a caller inspects `IFocusStateResolver`
- THEN it exposes a single async method accepting `UserId` and `CancellationToken`
- AND returning `Task<FocusState>`

#### Scenario: Port has no infrastructure dependency

- GIVEN the `IFocusStateResolver` interface
- WHEN an architecture test inspects its dependencies
- THEN no dependency on `Aura.Infrastructure` or external SDKs is present

---

### Requirement: Deterministic State Resolution

Given identical signals (calendars, time, preferences), `FocusStateResolver` MUST return the same `FocusState`. Resolution logic SHALL be a pure function of its signal inputs.

#### Scenario: Same signals produce same state

- GIVEN identical calendar, time, and preference inputs for a user
- WHEN `ResolveAsync` is called twice
- THEN both calls return the same `FocusState`

#### Scenario: Signal priority is documented

- GIVEN the resolver implementation
- WHEN a developer inspects the signal-priority order
- THEN calendar events take precedence over blackout periods, which take precedence over time-of-day heuristics, which take precedence over user preferences

---

### Requirement: BlackoutPeriod Value Object

`BlackoutPeriod` in `Aura.Domain.FocusState` MUST expose Label, TargetState (`DeepWork` or `Away`), StartTime, EndTime, DaysOfWeek, and TimeZoneId. `StartTime` MUST precede `EndTime`. `DaysOfWeek` MUST be non-empty.

#### Scenario: Active blackout resolves state

- GIVEN a DeepWork blackout weekdays 10:00–12:00 UTC
- WHEN current time is 11:00 UTC Wednesday
- THEN the resolver returns `DeepWork`

#### Scenario: Invalid range rejected

- GIVEN a blackout with start >= end
- WHEN constructed
- THEN validation throws
