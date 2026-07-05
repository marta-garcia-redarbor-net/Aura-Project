# Tasks: W3-H3 Focus State Resolution & Blackout Periods

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | ~900–1000 |
| 400-line budget risk | High |
| Chained PRs recommended | Yes |
| Suggested split | PR 1: Domain + Infra; PR 2: API + UI + Config |
| Delivery strategy | single-pr |
| Chain strategy | pending |

Decision needed before apply: Yes
Chained PRs recommended: Yes
Chain strategy: pending
400-line budget risk: High

### Suggested Work Units

| Unit | Goal | Likely PR | Notes |
|------|------|-----------|-------|
| 1 | Domain + Infra resolver/engine + DI | PR 1 | master base; all tasks include tests + telemetry |
| 2 | API endpoint + UI panel + config | PR 2 | depends on PR 1 |

### Dependency Graph

```
T1/T2 → T4 ← T3 → T5 → T6 → T7
T8 + T9 + T10 ← all above
```

## Phase 1: Domain Foundation

- [x] 1.1 `BlackoutPeriod` value object with `IsActive()` — validation (Start<End), DaysOfWeek, TimeZoneId. **Tests**: valid/invalid range, UTC crossing, weekday edge cases. **Risk**: TZ conversion errors (mitigated by `TimeZoneId`+`TimeProvider`). [M]
- [x] 1.2 `string? UserId` on `CalendarEvent` record. **Tests**: record compiles, existing consumers unaffected. [S]

## Phase 2: Infrastructure Implementation

- [x] 2.1 `FocusStateOptions` config model bound to `FocusState` section. **Tests**: JSON round-trip. [S]
- [x] 2.2 `SignalBasedFocusStateResolver` — signal pipeline (calendar→blackout→time-of-day→fallback). `ActivitySource("Aura.Infrastructure.FocusState")`, mock `ICalendarEventStore`+`TimeProvider`. Update `CalendarEventMapper`+`InMemoryCalendarEventStore` for UserId. **Tests**: all 4 states, calendar-over-blackout, hours boundary. **Risk**: stale calendar store. [L]
- [x] 2.3 `InterruptionPolicyEngine` — DeepWork gates non-critical, Recovery passthrough. Extract `ApplyFocusStateGate()`. **Tests**: DeepWork+non-critical→Defer, Recovery→evaluates rules. [M]

## Phase 3: Application Cleanup

- [x] 3.1 Delete stub `FocusStateResolver`, remove `AddScoped<IFocusStateResolver, FocusStateResolver>()` from Application DI. **Tests**: compilation check. [S]

## Phase 4: API & UI

- [x] 4.1 `GET /api/focus-state/current` returning `FocusStateResponse`. Auth via OID claim. Map in `Program.cs`. **Tests**: `WebApplicationFactory` — 200 with claim, 401 without, response shape. [M]
- [x] 4.2 `IFocusStateApiClient`+`FocusStateApiClient` (HTTP). `FocusStatePanel.razor` with 5-min polling, color-coded badge. Add to `Index.razor`. **Tests**: component renders per state, poll fires. **Risk**: 5-min lag acceptable per spec. [M]

## Phase 5: Wiring & Config

- [x] 5.1 Register `SignalBasedFocusStateResolver` + `Configure<FocusStateOptions>` in Infrastructure DI. **Tests**: container resolves to correct implementation. [S]
- [x] 5.2 Add `FocusState` section to `appsettings.json` (defaults: 08:00–18:00, 5min buffer, empty blackouts). [S]

## Phase 6: Integration Verification

- [x] 6.1 Composition tests — DI resolution, config binding, engine gating. [M]
- [x] 6.2 `dotnet test Aura.sln` — all tests pass. [S]

### Testing Approach

| Layer | Tool | Key Coverage |
|-------|------|-------------|
| Domain | xUnit | BlackoutPeriod IsActive, validation |
| Infra | xUnit+Moq | Signal priority, engine gating, config binding |
| API | WebApplicationFactory | Auth, response shape, status codes |
| UI | bUnit | State rendering, polling |
| Arch | NetArchTest | DI registration correctness |
