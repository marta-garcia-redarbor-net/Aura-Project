# Proposal: W1-H6 Dashboard Status & Progress

## Intent

The initial dashboard slice (`initial-dashboard`, archived) renders the shell but gives operators no at-a-glance read on whether Aura's core dependencies are healthy or how Week-1 module work is progressing. This change completes the remaining Week-1 H6 scope by adding visible system-status indicators (API, Qdrant, mock auth) and a module-progress panel, so the dashboard becomes a usable operational surface instead of an empty shell.

## Scope

### In Scope
- System-status indicators for **API**, **Qdrant**, and **mock auth** using tri-state `OK / Warning / Error` with brief explanatory microcopy per state.
- A **module-progress panel** rendering `pending / in-progress / completed` states from manual seeded data.
- Presentation-only UI consuming `Aura.Api` HTTP/DTO contracts; loading/empty/error view states reused from the existing shell.

### Out of Scope
- **Playwright / browser E2E** — intentionally deferred to a separate follow-up change; this slice verifies via .NET smoke/unit tests only.
- Live/real health probes beyond what's needed for the tri-state read; real module-progress data sources (seeded data only for now).
- Auth indicator reflecting live user session state — it reflects **only** whether the mock auth provider is configured/active.
- Any business logic inside Blazor components.

## Capabilities

### New Capabilities
- `dashboard-system-status`: tri-state (OK/Warning/Error) indicators for API, Qdrant, and mock-auth readiness, derived server-side and exposed via a GET-only API endpoint, rendered read-only on the dashboard.
- `dashboard-module-progress`: dashboard panel rendering module progress (pending/in-progress/completed) from manual seeded data via `Aura.Api`.

### Modified Capabilities
- None. `initial-dashboard` requirements are unchanged; the new panels render inside its existing shell and view states.

## Approach

Mirror the proven `graph-connector-status` pattern: derive each status state server-side (Application/Infrastructure), expose GET-only DTO endpoints from `Aura.Api`, and render read-only Blazor panels in `Aura.UI`. Status derivation stays out of the UI; components hold only view state. Module progress is served from a seeded source behind an `Aura.Api` contract so the panel is DTO-driven and swappable later.

## Affected Areas

| Area | Impact | Description |
|------|--------|-------------|
| `src/Aura.Api/` | Modified | GET-only status + module-progress DTO endpoints |
| `src/Aura.Application/` | Modified | Status derivation logic and progress contracts/ports |
| `src/Aura.Infrastructure/` | Modified | Readiness adapters (API/Qdrant/mock-auth), seeded progress source |
| `src/Aura.UI/` | Modified | Status indicator + module-progress Blazor panels |
| `tests/Aura.UnitTests`, `tests/Aura.ArchitectureTests` | Modified | Derivation tests + boundary/isolation checks |

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| Business logic leaks into Blazor components | Med | Derive state server-side; UI consumes DTOs only |
| Tri-state hides real failure nuance | Low | Pair each state with explanatory microcopy |
| Seeded progress mistaken for live data | Med | Label as seeded; isolate behind a swappable API contract |
| Auth indicator misread as session state | Med | Spec it as provider-configured/active only |

## Rollback Plan

Remove the new status and module-progress endpoints, their derivation services/adapters, and the Blazor panels; the dashboard reverts to the existing `initial-dashboard` shell with no behavioral regression.

## Dependencies

- Existing `initial-dashboard` shell and `Aura.Api` HTTP contracts.
- Existing mock-auth bootstrap and Qdrant local-environment capabilities for readiness signals.

## Success Criteria

- [ ] Dashboard shows API, Qdrant, and mock-auth indicators in OK/Warning/Error with microcopy.
- [ ] Module-progress panel renders pending/in-progress/completed from seeded data via `Aura.Api`.
- [ ] No business logic in UI; architecture tests confirm layer isolation.
- [ ] Verified by .NET smoke/unit tests; no Playwright dependency introduced.
