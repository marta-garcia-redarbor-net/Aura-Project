# Proposal: W1-H6 Dashboard Inicial

## Intent

Implement the first visible dashboard slice from the Stitch design so Aura has a demonstrable UI, while preserving the rule that UI consumes `Aura.Api` via HTTP and does not bypass Clean Architecture.

## Scope

### In Scope
- Create a separate `src/Aura.UI/` Blazor Server project and add it to `Aura.sln`.
- Import/adapt Stitch layout assets into Blazor components for sidebar, header, and dashboard shell.
- Wire the dashboard to `Aura.Api` contracts only, with initial loading/empty/error states.

### Out of Scope
- Embedding UI inside `Aura.Api` or injecting `Application`/`Infrastructure` services into components.
- Full Playwright coverage, live updates, or extra dashboard widgets beyond the initial shell.

## Capabilities

### New Capabilities
- `initial-dashboard`: Blazor dashboard shell, Stitch-derived layout, and HTTP-based consumption of `Aura.Api` for the first visible UI slice.

### Modified Capabilities
- None.

## Approach

Use a dedicated `Aura.UI` Blazor Server app in the same solution. Copy only required Stitch assets into `wwwroot`, decompose HTML into Blazor layout/components, replace static mock content with DTO-driven view models from `Aura.Api`, and keep browser verification limited to a scaffold/smoke path until Playwright is actually configured.

## Affected Areas

| Area | Impact | Description |
|------|--------|-------------|
| `src/Aura.UI/` | New | New Blazor Server UI project and layout components |
| `Aura.sln` | Modified | Include `Aura.UI` in solution |
| `src/Aura.Api/` | Modified | Expose/confirm dashboard DTO endpoints consumed by UI |
| `tests/Aura.E2E/` or new UI smoke path | Modified | Minimal verification scaffold; no full Playwright suite yet |

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| UI bypasses `Aura.Api` boundary | Med | Enforce HTTP-only integration and spec it explicitly |
| Raw Stitch JS/CSS conflicts with Blazor | Med | Port interactions to Blazor events; import only needed assets |
| Proposal assumes Playwright exists | High | Limit H6 to smoke/scaffold verification and document the gap |

## Rollback Plan

Remove `Aura.UI` from `Aura.sln`, revert imported assets/components, and disable any new dashboard endpoint/UI wiring so Aura returns to API-only operation.

## Dependencies

- Access to the Stitch-exported dashboard assets/source.
- Existing mock-auth and API contracts from H4/H5.

## Success Criteria

- [ ] `Aura.UI` runs as a separate project and renders the initial dashboard shell.
- [ ] The UI gets visible data/state through `Aura.Api` HTTP endpoints only.
- [ ] Tests/scaffolding added in the same change reflect current repo reality without pretending Playwright is complete.
