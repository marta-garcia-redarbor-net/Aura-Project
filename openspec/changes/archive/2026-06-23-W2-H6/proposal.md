# Proposal: W2-H6 Dashboard Inbox-by-Source and Morning Summary Preview

## Intent

The dashboard shell renders only generic shell/state cards. Users have no inbox view and no Morning Summary surface. When they arrive, they need a morning summary plus an inbox-by-source preview that updates as items arrive — without overwhelming them and without leaking internal `WorkItem` shape across the UI/API boundary.

## Scope

### In Scope
- One dashboard preview endpoint in `Aura.Api` returning inbox-by-source groups + a Morning Summary preview card.
- Slim dashboard-specific API DTO projections (not domain `WorkItem`).
- Two new Blazor panels in `Aura.UI` with loading, empty, error, and populated states.
- Morning Summary preview = summarized ranking only.
- Per inbox item: title/subject, source, relative timestamp, relevance score, brief suggested action.
- Repository-realistic smoke verification (W2-H6-T3) via `WebApplicationFactory`.

### Out of Scope
- Richer Morning Summary expansion / drill-down (deferred).
- Playwright setup or E2E browser automation.
- Refining `Score` precision or changing its type.
- Live push/SignalR refresh mechanics beyond existing patterns.

## Capabilities

### New Capabilities
- `dashboard-inbox-preview`: API contract + UI panels for inbox-by-source groups and a Morning Summary preview card, exposed via slim dashboard DTO projections.

### Modified Capabilities
- None. `morning-summary-ranking` and `morning-summary-contracts` stay the internal source of truth; the preview projects from them without changing their requirements.

## Approach

Add a dashboard-specific projection at the API boundary (recommended approach #1). Keep `WorkItem` inside Application/Domain ranking. `Aura.Api` maps ranked results into slim preview DTOs; `Aura.UI` consumes them through its API client and renders two small presentation-only panels. Keep `Score` as `double` for this slice.

## Affected Areas

| Area | Impact | Description |
|------|--------|-------------|
| `src/Aura.Api/Endpoints/DashboardEndpoints.cs` | Modified | New preview endpoint |
| `src/Aura.Api` (DTOs) | New | Slim inbox + summary preview projections |
| `src/Aura.UI/Pages/Index.razor` | Modified | Mount two panels |
| `src/Aura.UI/Models/*`, `Services/*` | New | UI DTOs + API client |
| `tests/Aura.E2E/Dashboard/InitialDashboardSmokeTests.cs` | Modified | Smoke coverage |

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| Preview DTO overfit to first screen | Med | Keep fields to the confirmed minimal set |
| `double` precision insufficient later | Low | Revisit only if ranking semantics tighten |
| Business logic leaking into Blazor | Low | Panels stay presentation-only; mapping in Api |

## Rollback Plan

Revert the dashboard preview endpoint, DTOs, UI panels, and smoke test in one change. `Index.razor` returns to prior shell cards; no schema or persistence migration involved.

## Dependencies

- Existing `morning-summary-ranking` output as projection source.

## Success Criteria

- [ ] Endpoint returns inbox-by-source groups + Morning Summary preview as slim DTOs.
- [ ] UI shows both panels with loading/empty/error/populated states.
- [ ] `WorkItem` never crosses the UI/API boundary.
- [ ] Smoke test passes via `WebApplicationFactory` (no Playwright).
