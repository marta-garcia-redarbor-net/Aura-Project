# Exploration: real-user-outlook-sync

### Current State
Real login now resolves identity in the UI, but Outlook sync still relies on shared MSAL cache lookups in the API/worker path. `SyncEndpoints` and `ConnectorExecutionWorker` both read the first cached account and pass only `UserOid` into connector execution. `GraphOutlookSourceProvider` then asks `IGraphClientFactory` for a delegated Graph client using that oid. Demo mode is separate: mock login is always available, `DemoMode` endpoints are only mapped when enabled, and demo auth uses `MockJwt` + session store.

The likely failure mode is identity mismatch: a real user can be logged in, but the sync path may resolve a different cached account or no account at all, so Outlook unread mail is not fetched for the actual mailbox. The current design also risks cross-login contamination because the token cache is process-shared and not explicitly partitioned by login/session intent.

### Affected Areas
- `src/Aura.Api/Endpoints/SyncEndpoints.cs` — resolves user oid from token cache and triggers sync.
- `src/Aura.Workers/ConnectorExecutionWorker.cs` — worker also resolves the first cached oid for Graph execution.
- `src/Aura.Infrastructure/Adapters/Connectors/Graph/GraphClientFactory.cs` — acquires Graph tokens from MSAL cache by oid.
- `src/Aura.Infrastructure/Adapters/Connectors/Graph/GraphOutlookSourceProvider.cs` — uses the client factory and maps Outlook unread mail.
- `src/Aura.Infrastructure/Adapters/Identity/DependencyInjection.cs` — demo/real auth boundaries and policies.
- `src/Aura.UI/Services/ForwardedAccessTokenHandler.cs` — token forwarding order for UI→API calls.
- `src/Aura.Infrastructure/Adapters/GraphConnector/GraphConnectorOptions.cs` — config surface for Graph readiness.
- `openspec/specs/token-cache-alignment/spec.md` — existing cache-path consistency guidance.

### Approaches
1. **Explicit per-login identity propagation** — carry the authenticated user oid from the request/session into sync execution and use that same oid for Graph token acquisition.
   - Pros: fixes cross-login contamination; makes real-user sync deterministic; keeps demo mode isolated.
   - Cons: requires plumbing identity through API/worker boundaries.
   - Effort: Medium

2. **Shared cache + first-account lookup** — keep current shape but rely on the first MSAL account in cache and config toggles to avoid demo conflicts.
   - Pros: minimal code changes.
   - Cons: fragile with multiple logins; can select the wrong mailbox; does not truly isolate demo vs real sessions.
   - Effort: Low, but unsafe

### Recommendation
Use explicit per-login identity propagation and partition Graph token access by authenticated user oid. Keep demo mode on its own mock login/session path and do not let demo logins reach the Graph sync pipeline. For Azure production, expose any required Graph/Entra settings via environment variables or user secrets, but keep connector enablement separate from demo mode so both can coexist in one deployment.

### Risks
- If the worker still infers identity from the first cached account, real and demo sessions can bleed into each other.
- If Azure production does not provide the required Graph/Entra env vars, real-user sync will remain disabled or reauth-loop.

### Ready for Proposal
Yes — the architecture is clear enough for a proposal. Tell the user the safe path is explicit identity propagation with per-user token boundaries, while demo mode stays independently enabled.
