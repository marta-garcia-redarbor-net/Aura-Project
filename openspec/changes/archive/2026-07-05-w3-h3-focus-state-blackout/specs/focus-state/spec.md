# Delta for Focus State Resolution & Blackout Periods

Applies to `focus-state-machine`, `interruption-policy-engine`, and `calendar-ingestion` main specs.

## ADDED Requirements

### Requirement: BlackoutPeriod Value Object

`BlackoutPeriod` in `Aura.Domain.FocusState` MUST expose Label, TargetState (DeepWork|Away), StartTime, EndTime, DaysOfWeek, TimeZoneId. Start MUST precede end. DaysOfWeek MUST be non-empty.

#### Scenario: Active blackout resolves state

- GIVEN a DeepWork blackout weekdays 10:00–12:00 UTC
- WHEN current time is 11:00 UTC Wednesday
- THEN the resolver returns `DeepWork`

#### Scenario: Invalid range rejected

- GIVEN a blackout with start >= end
- WHEN constructed
- THEN validation throws

---

### Requirement: FocusStateOptions Config Model

`FocusStateOptions` MUST bind from config section `FocusState` with properties: `BlackoutPeriodOptions[]`, `WorkingHoursStart`, `WorkingHoursEnd`, `MeetingBufferMinutes`.

#### Scenario: Binds from configuration

- GIVEN a populated `FocusState` section in `appsettings.json`
- WHEN `IOptions<FocusStateOptions>` is resolved
- THEN all options are populated

---

### Requirement: SignalBasedFocusStateResolver

`SignalBasedFocusStateResolver` in Infrastructure MUST implement `IFocusStateResolver` using priority: (1) calendar meeting → Away, (2) blackout → target state, (3) outside hours → Away, (4) fallback → WindowOfOpportunity. MUST be stateless — fresh `FocusState` per call.

#### Scenario: Calendar overrides blackout

- GIVEN an active meeting and blackout both present
- WHEN resolved
- THEN state is `Away`

#### Scenario: Fallback within hours

- GIVEN no meetings, no blackouts, within working hours
- WHEN resolved
- THEN state is `WindowOfOpportunity`

#### Scenario: Outside hours fallback

- GIVEN 22:00, working hours end 18:00, no meetings or blackouts
- WHEN resolved
- THEN state is `Away`

---

### Requirement: CalendarEvent UserId

`CalendarEvent` domain record MUST include `string? UserId`. Existing events MAY have null `UserId` for backwards compatibility.

#### Scenario: Event carries user identity

- GIVEN a `CalendarEvent` with `UserId` set
- WHEN the property is read
- THEN it returns the owning user's ID

---

### Requirement: GET /api/focus-state/current

The system MUST expose `GET /api/focus-state/current` returning `{ currentState, label, since, signals }`. Endpoint MUST use `IFocusStateResolver` with the current user's OID claim.

#### Scenario: Authenticated returns state

- GIVEN an authenticated user in `DeepWork`
- WHEN `GET /api/focus-state/current` is called
- THEN status is 200 with body `currentState: "DeepWork"`

#### Scenario: Unauthenticated returns 401

- GIVEN no authenticated user
- WHEN the endpoint is called
- THEN status is 401

---

### Requirement: FocusStatePanel.razor

The Blazor component MUST display current state with a color-coded indicator, MUST poll every 5 minutes, and MUST be placed in the dashboard layout.

#### Scenario: Displays current state

- GIVEN user state is `DeepWork`
- WHEN the component renders
- THEN "DeepWork" with a red indicator is shown

#### Scenario: Polls on 5-minute interval

- GIVEN the component has been mounted for 6 minutes
- WHEN the poll timer fires
- THEN the component refetches state from the API

## MODIFIED Requirements

### Requirement: Deterministic State Resolution

(Previously: signal priority was calendar > time-of-day > preferences, no blackout or engine gating defined)

The signal priority chain MUST now be: (1) calendar meeting → `Away`, (2) blackout period → target state, (3) outside working hours → `Away`, (4) fallback → `WindowOfOpportunity`.

#### Scenario: InterruptionPolicyEngine DeepWork gating

- GIVEN user state is `DeepWork`
- WHEN a non-critical interruption arrives
- THEN the engine returns `DEFER`

#### Scenario: InterruptionPolicyEngine Recovery gating

- GIVEN user state is `Recovery`
- WHEN any interruption arrives
- THEN it evaluates as if state were `WindowOfOpportunity`

## REMOVED Requirements

None. The stub `FocusStateResolver` was an implementation detail, not a spec requirement.
