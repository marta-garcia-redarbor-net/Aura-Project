# Proposal: Real Teams and Outlook Ingestion

## Intent
Integrate real Graph API ingestion for Teams and Outlook. Validates authentication flows, mapping, and worker execution on real M365 infrastructure, ensuring real-world data shapes and latency are handled before layering complex triage logic (W2-H7).

## Scope

### In Scope
- Real Teams messages and Outlook emails ingestion via Graph API (read-only).
- Delegated-first Graph auth with incremental consent via UI login.
- SQLite-based token cache for background worker reuse.
- Manual "Sync now" trigger from UI with per-source progress, result counts, and last sync timestamp.
- Visible partial degradation (e.g., if Teams fails, Outlook still syncs).
- Explicit empty data state in UI (no silent fallback to demo data).
- Playwright strategy: real-data smoke test + controlled demo main suite.
- Dashboard fields: source, subject/title, sender, timestamp, priority hint, deep link, snippet, sync state.

### Out of Scope
- Write operations (reply, mark as read, origin edits/categories).
- SignalR real-time updates.
- Advanced cross-source deduplication or full ranking explanations.
- Redis token cache (using SQLite for now).

## Capabilities

> This section is the CONTRACT between proposal and specs phases.

### New Capabilities
- `graph-delegated-auth`: UI login for incremental consent, SQLite token caching, and surfacing re-auth needs to the user.

### Modified Capabilities
- `dashboard-inbox-preview`: Add real data fields (deep link, priority hint, snippet), manual "Sync now" feedback, explicit empty data UX, and summary preview.
- `outlook-connector-mapping`: Adapt to real Graph API email data for read-only metadata.
- `teams-connector-mapping`: Adapt to real Graph API message data for read-only metadata.
- `connector-execution`: Add partial degradation handling per source, worker execution, and manual sync trigger.

## Approach
Implement a delegated-first authentication flow where the Blazor UI initiates Entra ID login to acquire and cache tokens in SQLite. The background worker uses these tokens to fetch read-only data from Microsoft Graph. The UI dashboard adds a "Sync now" button showing granular loading states. Playwright tests are split into a real-data smoke suite and a stable demo suite to coexist with parallel testing efforts.

## Affected Areas

| Area | Impact | Description |
|------|--------|-------------|
| `Aura.Infrastructure/Graph` | New | Graph API clients and SQLite token cache |
| `Aura.UI/Components` | Modified | Add Sync button, sync state feedback, real data fields |
| `Aura.Workers` | Modified | Execute real ingestion using cached tokens |
| `tests/Aura.E2E` | Modified | Add real-data smoke suite; preserve demo suite |

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| Graph API rate limits/latency | High | Read-only fetching, manual sync trigger, partial degradation |
| Token expiration in worker | Med | SQLite cache with clear UI indication when re-auth is needed |
| E2E test flakiness | Med | Separate real-data smoke suite from stable demo main suite |

## Rollback Plan
Revert the Entra ID app registration usage, toggle connectors back to mocked/demo data sources, and disable the manual sync button in the UI.

## Dependencies
- Microsoft Entra ID App Registration (new).
- Microsoft Graph API connectivity.

## Success Criteria
- [ ] UI authenticates and caches delegated tokens in SQLite.
- [ ] Worker successfully reads tokens and fetches Teams and Outlook data.
- [ ] Dashboard displays new metadata fields and clear "Sync now" progress.
- [ ] Playwright smoke test passes with a real account; demo tests remain green.
- [ ] Partial degradation is visible if one source fails.