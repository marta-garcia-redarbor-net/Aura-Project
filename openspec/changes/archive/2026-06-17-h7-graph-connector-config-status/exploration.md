## Exploration: Graph connector configuration status

### Current State
Aura already has a working pattern for a read-only UI slice: `Aura.UI` is a separate Blazor Server host, it consumes `Aura.Api` through a typed HTTP client, and `MainLayout` streams an initial view state into the page. The current dashboard flow is API-first, with `Aura.Api` exposing `/api/dashboard/initial` and the UI rendering loading/empty/error/populated states from DTOs. There is no existing Graph connector status surface yet, and no Graph-specific configuration model or endpoint in the codebase.

### Affected Areas
- `src/Aura.Infrastructure` — likely place for Graph connector configuration binding/bootstrap, keeping provider-specific details out of Application.
- `src/Aura.Application` — needs the read-model/port for connector status and the logic that maps config presence into the four required states.
- `src/Aura.Api` — needs an endpoint for exposing the connector status to the UI.
- `src/Aura.UI` — needs a read-only screen/panel that renders Disabled, MissingConfig, PartialConfig, and ValidConfig.
- `tests/Aura.IntegrationTests` — should verify the API status contract and status mapping.
- `tests/Aura.E2E` — should smoke-test the UI rendering of the four states without browser automation.

### Approaches
1. **API-backed status read model** — add an Application port/service that evaluates connector configuration and expose it through a small API DTO consumed by the UI.
   - Pros: matches existing API-only UI pattern; keeps Graph details out of the UI; easy to test with stubs.
   - Cons: introduces a new endpoint and DTO layer for a simple read-only slice.
   - Effort: Medium

2. **UI-local config inspection** — have `Aura.UI` read configuration directly and render the status without an API round-trip.
   - Pros: fewer moving parts.
   - Cons: breaks the established UI boundary; duplicates config logic; weakens the “UI consumes Aura.Api only” rule.
   - Effort: Low

### Recommendation
Use the API-backed status read model. The current architecture already proves that `Aura.UI` should stay API-only, so the smallest clean slice is: Infrastructure binds Graph connector settings, Application derives the status, Api exposes it, and UI renders it read-only. The status source should stay behind a domain-capability port, not a Graph SDK type.

### Risks
- The exact definition of `Disabled` vs `MissingConfig` is not specified; the proposal must define what config shape or feature flag separates them.
- The Graph config source is unknown (appsettings, environment variables, secret store, or persisted settings), so bootstrap assumptions must be explicit.
- If the UI tries to model status as editable configuration, scope will drift beyond the confirmed Week 1 slice.

### Ready for Proposal
Yes — proceed to proposal with a read-only, API-backed connector-status slice, but call out the open questions for config source and the precise rules for Disabled/MissingConfig/PartialConfig/ValidConfig.
