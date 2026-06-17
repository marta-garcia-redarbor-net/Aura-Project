# Proposal: Graph Connector Configuration Status

## Intent

Aura needs a foundation slice that tells operators whether the Microsoft Graph connector is configured and ready, before any real ingestion exists. Today there is no Graph configuration model, endpoint, or status surface. This Week 1 slice binds connector settings, derives a status, and renders it read-only — establishing the clean boundary that future ingestion work plugs into.

## Scope

### In Scope
- Infrastructure binding of Graph connector settings from appsettings + environment variables.
- Application port + status read-model deriving four states from config presence.
- `Aura.Api` endpoint exposing connector status as a DTO.
- `Aura.UI` read-only panel rendering Disabled, MissingConfig, PartialConfig, ValidConfig.
- Integration tests for status mapping/API contract; E2E smoke test for the four UI states (no browser automation).

### Out of Scope
- Any real Graph SDK connection, auth handshake, or token acquisition.
- WorkItem normalization or ingestion.
- Editable configuration UI (status is read-only).
- Secret store / persisted settings source (deferred; v1 is appsettings + env vars).

## Capabilities

### New Capabilities
- `graph-connector-status`: derive and expose the Graph connector's configuration readiness as a read-only four-state status.

### Modified Capabilities
- None.

## Approach

API-backed status read model (exploration recommendation). Infrastructure binds settings behind a domain-capability port; Application maps config presence to a state; Api exposes a DTO; UI consumes Api only. No Graph SDK type crosses into Application/UI.

**State rules (explicit):**
| State | Rule |
|-------|------|
| Disabled | Explicit enable flag = false (takes precedence over all other checks). |
| MissingConfig | Enabled, but TenantId and ClientId both absent. |
| PartialConfig | Enabled, some required fields present but not all (missing TenantId, ClientId, or a valid credentials block). |
| ValidConfig | Enabled + TenantId + ClientId + at least one valid credentials/configuration block. |

## Affected Areas

| Area | Impact | Description |
|------|--------|-------------|
| `src/Aura.Infrastructure` | New | Bind Graph connector options + status provider adapter. |
| `src/Aura.Application` | New | Status port + read-model and state-mapping logic. |
| `src/Aura.Api` | New | Connector-status endpoint + DTO. |
| `src/Aura.UI` | New | Read-only status panel (4 states). |
| `tests/Aura.IntegrationTests` | New | API contract + state-mapping coverage. |
| `tests/Aura.E2E` | New | UI smoke test for 4 states. |

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| Scope drift to editable config | Med | Lock UI as read-only; reject edit affordances in spec. |
| Config-source assumption wrong | Low | Explicitly fix v1 source as appsettings + env vars; isolate behind binder. |
| PartialConfig ambiguity | Med | Define exact field-presence rules above; cover each in tests. |

## Rollback Plan

Single feature slice. Revert the change branch: remove the new endpoint, UI panel, Infrastructure binding, and tests. No schema, migration, or external connection exists, so revert is clean with no data impact.

## Dependencies

- Existing API-first UI pattern (`/api/dashboard/initial`, typed HTTP client) as the integration template.

## Success Criteria

- [ ] API returns correct state for each of the four config scenarios.
- [ ] UI renders the matching read-only state for each scenario.
- [ ] No Graph SDK type appears in Application or UI.
- [ ] Integration + E2E tests pass under `dotnet test Aura.sln`.
