# Proposal: Real-User Outlook Sync with Demo Isolation

## Intent

Real Entra ID users log in but their Outlook unread mail is not fetched for the actual mailbox. `SyncEndpoints` and `ConnectorExecutionWorker` resolve the *first* cached MSAL account, so sync can target the wrong account or none. The shared, unpartitioned token cache also lets real and demo sessions contaminate each other. Fix: propagate the authenticated identity end-to-end and isolate demo logins from the real Graph pipeline.

## Scope

### In Scope
- Propagate the authenticated real-user `oid` from request/session and worker into Graph token acquisition.
- Select the Graph account by authenticated `oid`, never `FirstOrDefault()`.
- Block demo-mode logins from reaching real Graph sync.
- Partition token-cache access by `oid` to stop cross-login token bleed.
- Document required production Azure/Entra configuration.

### Out of Scope
- New connectors (Teams, Calendar, GitHub) behavior changes.
- Multi-user concurrent sync scaling / background scheduling redesign.
- Token cache storage backend migration.

## Capabilities

### New Capabilities
- `session-outlook-identity`: End-to-end propagation of the authenticated real-user oid (API sync trigger + worker) into Graph token acquisition, so a real user syncs only their own Outlook unread mail and demo sessions are excluded from the real pipeline.

### Modified Capabilities
- `connector-execution`: Sync trigger and worker MUST resolve identity from the authenticated login/session, not the first cached account.
- `token-cache-alignment`: Token cache lookups MUST be keyed by authenticated oid to prevent real/demo cross-login contamination.
- `graph-config`: Add production Azure/Entra configuration requirements (env vars / user secrets) that enable real-user sync in a deployment where demo mode also runs.

## Approach

Adopt explicit per-login identity propagation (exploration Approach 1). Carry the authenticated `oid` through the API/worker boundary into `IGraphClientFactory`, which selects the matching cached account. Demo auth stays on its own mock login/session path and never reaches Graph acquisition. Connector enablement stays independent from demo mode so both coexist in one build. SDK types stay in Infrastructure; ports stay provider-neutral.

## Affected Areas

| Area | Impact | Description |
|------|--------|-------------|
| `src/Aura.Api/Endpoints/SyncEndpoints.cs` | Modified | Use authenticated request identity, not first cached account |
| `src/Aura.Workers/ConnectorExecutionWorker.cs` | Modified | Resolve oid per authenticated user; skip demo sessions |
| `src/Aura.Infrastructure/Adapters/Connectors/Graph/GraphClientFactory.cs` | Modified | Oid-based account selection + cache partitioning |
| `src/Aura.Infrastructure/Adapters/Identity/DependencyInjection.cs` | Modified | Keep demo/real auth boundaries isolated |
| `src/Aura.Infrastructure/Adapters/GraphConnector/GraphConnectorOptions.cs` | Modified | Production config surface for Graph readiness |

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| Worker still infers first cached account | Med | Enforce oid-keyed selection + arch/unit tests |
| Demo login bleeds into real sync | Med | Gate pipeline on real auth; isolate demo session path |
| Missing prod Graph/Entra env vars → reauth loop | Med | Document required config; derive `Disabled` status when absent |

## Rollback Plan

Changes are additive identity plumbing gated by existing config. Revert the affected files to restore prior first-account behavior; demo mode and connector toggles are unchanged, so reverting does not break existing logins.

## Dependencies

- Production Azure app registration with delegated Graph scopes (`Mail.Read`, `User.Read`) and `GraphConnector__Enabled=true`.

## Success Criteria

- [ ] A real user sees only their own Outlook unread emails in the priority dashboard.
- [ ] A demo login triggers no real Graph token acquisition or Graph HTTP call.
- [ ] Concurrent real and demo sessions never resolve each other's cached account.
- [ ] Production config gaps yield a `Disabled` status instead of a reauth loop.
