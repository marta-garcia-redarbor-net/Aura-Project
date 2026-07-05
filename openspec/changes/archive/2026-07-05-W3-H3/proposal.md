# Proposal: W3-H3 — Focus State UI & Prioritized Queue

## Intent

Users have no visibility into focus state and no override. The work queue shows no priority differentiation. This exposes focus state in the header with persisted override, surfaces triage priority scores, and adds an interruption decision audit trail.

## Scope

**In**: Header focus badge + dropdown override (persisted); `GET/PUT /api/dashboard/focus-state`; `int? PriorityScore` on WorkItem + DTO + DESC sort; dashboard pending/high-priority counts + top-3 highlight; priority badge on all items; interruption decision log page + API.

**Out**: Real calendar signals for resolver (stub remains); per-user scoring UI; Playwright tests.

## Capabilities

**New**: `interruption-decision-log` — full verdict history with sidebar menu entry + page.

**Modified**:
- `focus-state-machine`: Persisted user override, header badge + dropdown, API endpoints.
- `work-item-contract`: Add `int? PriorityScore` (nullable). DTO carries it. API sorts DESC.
- `dashboard-inbox-preview`: Extend DTO with PriorityScore. Dashboard: pending + high-priority counts + top-3.
- `interruption-policy-engine`: Persist all verdicts (not just INTERRUPT_NOW). Expose query endpoint.

## Approach

1. `int? PriorityScore` on `WorkItem` — nullable, optional ctor param
2. `FocusStateOverride` store (SQLite, existing pattern)
3. `GET/PUT /api/dashboard/focus-state` for override
4. `FocusStateBadge.razor` in header — 4-state dropdown
5. Extend dashboard DTOs: priority counts, sorted items, top-3
6. `InterruptionDecisionStore` — persist all verdicts
7. `GET /api/triage/decisions` + `InterruptionLog.razor` page
8. Update all `WorkItemDetailDto` with `PriorityScore`

## Affected Areas

| Area | Impact |
|------|--------|
| `src/Aura.Domain/WorkItems/WorkItem.cs` | Add `int? PriorityScore` |
| `src/Aura.Application/Ports/IFocusStateResolver.cs` | Override contracts |
| `src/Aura.Application/Services/FocusStateResolver.cs` | Respect override |
| `src/Aura.Application/Services/` (new) | `InterruptionDecisionStore` |
| `src/Aura.Api/Program.cs` | +3 endpoints |
| `src/Aura.UI/Components/Layout/Header.razor` | Focus badge + dropdown |
| `src/Aura.UI/Components/Dashboard/` | Priority badge + top-3 |
| `src/Aura.UI/Pages/InterruptionLog.razor` | New |
| `src/Aura.UI/Models/WorkItemDetailDto.cs` | Add `PriorityScore` |

## Risks

| Risk | Likelihood |
|------|------------|
| Resolver stub — always WindowOfOpportunity | High |
| Non-InterruptNow verdicts not persisted | Med |

## Rollback

Revert API endpoints in `Program.cs`, remove header component, drop `PriorityScore` (SQLite in-memory — restart clears), remove decision store.

## Dependencies

- W3-H2 triage engine deployed (produces `InterruptionVerdict`)

## Success Criteria

- [ ] Header shows focus state; manual override survives restart
- [ ] Dashboard: pending + high-priority count + top-3 highlighted
- [ ] WorkItem API returns sorted by PriorityScore DESC
- [ ] Interruption log shows all past verdicts
- [ ] `dotnet test Aura.sln` passes
