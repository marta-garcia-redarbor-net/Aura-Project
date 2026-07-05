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

### Requirement: FocusStateOverride Persistence

The system MUST persist user focus-state overrides so they survive application restarts. The persistence store SHALL follow the existing SQLite pattern. An absent or `null` persisted value SHALL be treated as "no override."

#### Scenario: Override survives restart

- GIVEN a user sets an override to `DeepWork`
- WHEN the application is restarted
- THEN `ResolveAsync` still returns `DeepWork`

#### Scenario: Null override has no effect

- GIVEN no override has ever been set for a user
- WHEN `ResolveAsync` is called
- THEN the result is the auto-computed state

---

### Requirement: Focus State API Endpoints

`GET /api/focus-state` MUST return current resolved focus state, whether an override is active, and the user identity. `PUT /api/focus-state` MUST accept a `FocusStateType` value and persist it as the override. Sending `null` SHALL clear any active override.

#### Scenario: GET returns state with override flag

- GIVEN a user with an active `DeepWork` override
- WHEN `GET /api/focus-state` is called
- THEN the response contains `state: "DeepWork"` and `isOverridden: true`

#### Scenario: PUT persists override

- GIVEN a user with no existing override
- WHEN `PUT /api/focus-state` with body `{ "state": "Away" }` is called
- THEN the response is HTTP 200
- AND subsequent `GET` calls return `state: "Away"` with `isOverridden: true`

#### Scenario: PUT with null clears override

- GIVEN a user with an active override
- WHEN `PUT /api/focus-state` with body `null` is called
- THEN the response is HTTP 200
- AND subsequent `ResolveAsync` falls back to auto-computed state

---

### Requirement: Header Focus State Badge

The application header MUST display the current focus state as a color-coded badge. The badge MUST include a dropdown allowing the user to override to any of the four focus states. When an override is active, the dropdown SHALL include a "Clear override" option.

#### Scenario: Badge renders current state

- GIVEN the user is in `WindowOfOpportunity`
- WHEN the header renders
- THEN a color-coded badge displays "Window of Opportunity"

#### Scenario: Dropdown sets override

- GIVEN the header dropdown is open
- WHEN the user selects "Deep Work"
- THEN `PUT /api/focus-state` is called with `{ "state": "DeepWork" }`
- AND the badge updates to show `DeepWork`

#### Scenario: Override badge shows "Clear override"

- GIVEN an override is active
- WHEN the user opens the header dropdown
- THEN a "Clear override" option is visible
- AND selecting it clears the override via API

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
