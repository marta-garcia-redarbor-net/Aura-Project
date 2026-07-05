# Delta for focus-state-machine

## MODIFIED Requirements

### Requirement: IFocusStateResolver Port

The system MUST define `IFocusStateResolver` in `Aura.Application.Ports` exposing `Task<FocusState> ResolveAsync(UserId userId, CancellationToken ct)`. The contract MUST NOT reference infrastructure, UI, or transport types. When a valid persisted user override exists, `ResolveAsync` MUST return the overridden `FocusState` instead of the auto-computed state. Clearing the override MUST restore auto-computed behavior.
(Previously: resolver had no override concept; it computed focus deterministically from signals alone.)

#### Scenario: Port contract is defined

- GIVEN the Application ports assembly
- WHEN a caller inspects `IFocusStateResolver`
- THEN it exposes a single async method accepting `UserId` and `CancellationToken`
- AND returning `Task<FocusState>`

#### Scenario: Port has no infrastructure dependency

- GIVEN the `IFocusStateResolver` interface
- WHEN an architecture test inspects its dependencies
- THEN no dependency on `Aura.Infrastructure` or external SDKs is present

#### Scenario: Override returned when active

- GIVEN a user has a persisted focus-state override set to `DeepWork`
- AND the auto-resolver would return `WindowOfOpportunity`
- WHEN `ResolveAsync` is called
- THEN the returned `FocusState` equals `DeepWork`

#### Scenario: Cleared override falls back to auto-computed

- GIVEN a user previously had an override that has been cleared
- WHEN `ResolveAsync` is called
- THEN the returned `FocusState` equals the auto-computed state from signals

## ADDED Requirements

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
