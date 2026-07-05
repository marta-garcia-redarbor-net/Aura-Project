# Proposal: W3-H3 — Focus State Resolution & Blackout Periods

## Intent

Replace the stub `FocusStateResolver` (always returns `WindowOfOpportunity`) with a **signal-based resolver** that combines calendar events, blackout periods, and time-of-day heuristics to return accurate per-user focus states. This makes interruption policy gating meaningful.

## Scope

### In Scope
1. `SignalBasedFocusStateResolver` in Infrastructure — replaces Application stub
2. `BlackoutPeriod` value object in Domain (recurring/one-off time blocks → DeepWork or Away)
3. `FocusStateOptions` config model in Infrastructure
4. Calendar → Away signal via `ICalendarEventStore` (events overlapping `[now - buffer, now + buffer]`)
5. Extend `InterruptionPolicyEngine` — add DeepWork (defer non-critical) and Recovery (treat like WoO)
6. Add `UserId` to `CalendarEvent` domain record
7. `GET /api/focus-state/current` API endpoint
8. `FocusStatePanel.razor` Blazor UI component
9. `FocusState` config section in `appsettings.json`

### Out of Scope
- User-facing blackout period management UI (CRUD) — deferred
- Persistent blackout store — InMemory for this change
- Activity-based detection (keyboard/mouse, presence) — future
- Push-based state change notifications (SignalR) — deferred

## Capabilities

### New Capabilities
- `focus-state-resolution`: Signal-based resolver implementing `IFocusStateResolver` port, consuming calendar and blackout signals
- `focus-state-api`: `GET /api/focus-state/current` endpoint returning `{ userId, state, since, signals }`

### Modified Capabilities
- `focus-state-machine`: Add `BlackoutPeriod` value object; real resolver replaces stub; signal priority contract now implemented
- `calendar-ingestion`: Add `UserId` field to `CalendarEvent` domain record
- `priority-scoring`: `InterruptionPolicyEngine` gating logic extended for DeepWork (defer non-critical) and Recovery (treat as WindowOfOpportunity)

## Approach

Signal priority (first match wins):
1. **Calendar** — if active meeting overlaps window → `Away`
2. **Blackout periods** — recurring DeepWork block → `DeepWork`, lunch → `Away`
3. **Time-of-day** — working hours → `WindowOfOpportunity`, outside → `Away`
4. **Fallback** — `WindowOfOpportunity`

Live in `Aura.Infrastructure.Adapters.Services` alongside `InterruptionPolicyEngine`. Uses same adapter-style DI registration as `MeaiEmbeddingProvider`.

## Affected Areas

| Area | Impact | Description |
|------|--------|-------------|
| `src/Aura.Domain/Calendar/CalendarEvent.cs` | Modified | Add `string UserId` field |
| `src/Aura.Domain/FocusState/` | New | Add `BlackoutPeriod.cs` value object |
| `src/Aura.Infrastructure/Adapters/Services/SignalBasedFocusStateResolver.cs` | New | Real resolver replacing stub |
| `src/Aura.Infrastructure/Adapters/Options/FocusStateOptions.cs` | New | Config model |
| `src/Aura.Infrastructure/Adapters/Services/InterruptionPolicyEngine.cs` | Modified | DeepWork defer + Recovery handling |
| `src/Aura.Application/Services/FocusStateResolver.cs` | Removed | Stub deleted |
| `src/Aura.Api/Endpoints/FocusStateEndpoints.cs` | New | `GET /api/focus-state/current` |
| `src/Aura.UI/Components/Dashboard/FocusStatePanel.razor` | New | UI component |
| `src/Aura.UI/Pages/Index.razor` | Modified | Add `<FocusStatePanel />` |
| `src/Aura.Api/appsettings.json` | Modified | Add `FocusState` section |

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| Calendar data stale (InMemory store) | High | Dogfood with real store before GA; document dev limitation |
| Timezone errors in blackout periods | Medium | Store blackouts in UTC; resolve against user's local time via configured offset |
| DeepWork not gated in policy engine | Low | Extend `EvaluateAsync` to check DeepWork before rule evaluation |
| `UserId` added to domain record may break consumers | Low | `CalendarEvent` is a record — additive field with default null |

## Rollback Plan

1. Revert `SignalBasedFocusStateResolver` registration, restore stub
2. Remove `FocusState` config section from `appsettings.json`
3. Revert `UserId` addition on `CalendarEvent`
4. Remove `FocusStatePanel.razor` and `GET /api/focus-state/current`
5. Revert `InterruptionPolicyEngine` gating changes

## Dependencies

- W3-H1 (Focus State Machine) — complete
- W3-H2 (Interruption Scoring) — complete
- `ICalendarEventStore` — exists, needs `GetByUserAndTimeRange` or scoped overload

## Success Criteria

- [ ] `SignalBasedFocusStateResolver` returns all four states correctly from signals
- [ ] Calendar event overlapping current time → `Away`
- [ ] Active blackout DeepWork block → `DeepWork`
- [ ] Outside working hours → `Away`
- [ ] `InterruptionPolicyEngine` defers non-critical interruptions during `DeepWork`
- [ ] `InterruptionPolicyEngine` treats `Recovery` like `WindowOfOpportunity`
- [ ] `GET /api/focus-state/current` returns correct per-user state
- [ ] `FocusStatePanel.razor` renders state label with colour indicator
