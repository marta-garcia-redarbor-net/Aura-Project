# Exploration: W2-H6 Dashboard inbox and Morning Summary

### Current State
- `Aura.UI` is a separate Blazor Server host that consumes `Aura.Api` over HTTP only.
- The current dashboard slice is generic: `Index.razor` renders shell/state panels and dashboard cards from `InitialDashboardResponse` / `DashboardCardResponse`.
- `Aura.Api` already exposes `/api/dashboard/initial`, `/api/dashboard/system-status`, and `/api/dashboard/module-progress`.
- Morning Summary architecture is split: `docs/architecture/triage/01-morning-summary.md` only defines scheduling, timezone, and daily idempotence.
- The archived W2-H5-T1 contract uses internal `RankedWorkItem` with full `WorkItem` plus `double` score, but there is no UI/API contract for inbox-by-source or a Morning Summary preview.
- `Aura.E2E` is currently xUnit + `WebApplicationFactory` smoke coverage; Playwright is not configured.

### Affected Areas
- `StoryBacklog.md` — W2-H6-T1/T2/T3 scope must match repo reality.
- `src/Aura.Api/Endpoints/DashboardEndpoints.cs` — likely place for a new dashboard preview endpoint.
- `src/Aura.UI/Pages/Index.razor` — dashboard shell needs new visible panels.
- `src/Aura.UI/Models/*` — new UI DTOs for inbox-by-source and summary preview.
- `src/Aura.UI/Services/*` — API client for the new dashboard contract.
- `src/Aura.Application/UseCases/MorningSummary/*` — internal ranking/composition model stays the source of truth.
- `tests/Aura.E2E/Dashboard/InitialDashboardSmokeTests.cs` — best fit for repository-realistic smoke verification.
- `tests/Aura.IntegrationTests/Dashboard/InitialDashboardEndpointTests.cs` — precedent for API-only boundary checks.
- `docs/architecture/triage/01-morning-summary.md` — current triage contract boundary.
- `openspec/changes/archive/2026-06-22-w2-h5-t1-morning-summary-contracts/*` — archived contract decisions and open questions.

### Approaches
1. **Dashboard-specific projection DTOs (recommended)** — add a dashboard preview contract that exposes slim inbox-by-source groups and a Morning Summary preview card.
   - Pros: preserves Clean Architecture; keeps `WorkItem` internal; matches the existing API-only UI pattern.
   - Cons: one extra DTO layer.
   - Effort: Medium.

2. **Expose full `WorkItem` across the UI/API boundary** — reuse domain objects directly in the dashboard response.
   - Pros: fastest to wire.
   - Cons: leaks internal shape, overfetches, and weakens the UI boundary.
   - Effort: Low.

### Recommendation
Choose dashboard-specific DTO projections at the API boundary. Keep `WorkItem` inside Application/Domain ranking logic, and let the UI consume a slim dashboard preview model. Keep `Score` as `double` for this slice; do not refine it yet unless precision requirements emerge.

### Risks
- The preview DTO can become too UI-shaped if we overfit it to the first screen.
- `double` may need refinement later if ranking semantics become more precise.

### Implementation Summary
- Implement one dashboard preview endpoint for W2-H6 that returns inbox-by-source groups plus a Morning Summary preview.
- Render two small dashboard panels in `Aura.UI` with loading, empty, error, and populated states.
- Verify the slice with repository-realistic smoke coverage in `Aura.E2E` using `WebApplicationFactory`; do not depend on Playwright for this story.

### Ready for Proposal
Yes — the scope is now clear enough to move into proposal/spec work for W2-H6.
