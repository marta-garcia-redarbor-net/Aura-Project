# Proposal: Graph Real Data Dashboard

## Intent

Enable Microsoft Graph connectors (Outlook, Teams, Calendar) to flow live data into Aura, and build a new Stitch-designed operational dashboard that replaces the current stub panels with a dark-mode, status-driven UI. The existing dashboard remains as a legacy/test view.

**Business problem**: Graph connectors are fully implemented but disabled — no real data reaches the system. The current dashboard is a scaffold with seeded/mock data. Without live data and a proper operational view, the team cannot validate ingestion, triage, or review workflows end-to-end.

## Scope

### In Scope
1. **Enable Graph connectors** — flip config switches, add `Calendars.Read` scope, fix token cache path for Docker
2. **Wire SyncStatusPanel** — connect the sync-now button to `POST /api/sync/now`
3. **New operational dashboard** — dark-mode Blazor UI matching Stitch designs (8 screens)
4. **Routing coexistence** — legacy dashboard accessible at `/legacy`, new dashboard at `/` (default)
5. **Connector status cards** — Outlook, Teams, Calendar showing status, item count, last sync time
6. **Per-connector detail views** — Pending Mail, Teams Messages, Pull Requests, Tasks views
7. **Design system tokens** — dark palette, Inter + JetBrains Mono typography, status badges with glow

### Out of Scope
- Calendar data mapping to work items (separate backlog item)
- GitHub connector enablement (PR view uses existing reviewer data)
- Push notifications / real-time sync (polling only for now)
- SSO/OIDC for the new dashboard (uses existing mock-auth flow)
- Playwright E2E tests for new dashboard (no Playwright tooling yet)

## Capabilities

### New Capabilities
- `operational-dashboard`: Dark-mode Blazor dashboard with Stitch-derived shell, navigation, and per-connector detail screens. Replaces the scaffolded initial dashboard as the default view.
- `graph-live-data-pipeline`: End-to-end data flow from Graph API → Infrastructure adapters → Application ports → API endpoints → UI consumption with real credentials and enabled connectors.

### Modified Capabilities
- `graph-connector-status`: Extend to support live status derived from actual Graph API responses (not just config presence). Add Calendars.Read scope to `GraphClientFactory` defaults.
- `connector-execution`: Ensure sync-now endpoint triggers all three connectors and returns per-source status in the response.
- `environment-config`: Document real credential setup flow (separate from placeholder guidance).

## Approach

### Phase 1: Enable Graph Connectors (Config + Code Fixes)
- Update `.env` with instructions for real credentials (TenantId, ClientId)
- Set `GraphConnector__Enabled=true`
- Add `Calendars.Read` to `GraphClientFactory` default scopes
- Align worker token cache path with API (`/data/tokens/cache.db` in Docker)
- Verify `POST /api/sync/now` triggers all three connectors

### Phase 2: Wire SyncStatusPanel
- Connect sync-now button to `POST /api/sync/now`
- Display per-source sync status (last sync time, item count, error state)
- Add loading/error states for the sync trigger action

### Phase 3: Build Operational Dashboard
- Create new Blazor route structure (`/` for new, `/legacy` for existing)
- Implement Stitch design system tokens (CSS variables for colors, typography)
- Build dashboard shell with sidebar navigation matching Stitch screens
- Implement connector status cards (Outlook, Teams, Calendar)
- Build per-connector detail views (Pending Mail, Teams Messages, PR, Tasks)

### Phase 4: Integration + Polish
- Connect all dashboard panels to live API endpoints
- Implement status badges with glow dots (emerald/amber/slate)
- Add loading, empty, error, and populated states per screen
- Verify end-to-end flow: Graph → API → UI

## Affected Areas

| Area | Impact | Description |
|------|--------|-------------|
| `.env` | Modified | Add real credential values, enable GraphConnector |
| `src/Aura.Infrastructure/Adapters/Connectors/Graph/GraphClientFactory.cs` | Modified | Add `Calendars.Read` scope |
| `src/Aura.Infrastructure/Adapters/GraphConnector/GraphConnectorOptions.cs` | Modified | Token cache path alignment |
| `docker-compose.yml` | Modified | Volume mount for token cache |
| `src/Aura.UI/Components/Dashboard/SyncStatusPanel.razor` | Modified | Wire sync-now button to API |
| `src/Aura.UI/Components/Dashboard/` (new files) | New | Operational dashboard screens |
| `src/Aura.UI/Shared/` (new files) | New | Design system tokens, layout components |
| `src/Aura.UI/Routes.razor` | Modified | Route new vs legacy dashboard |

## Key Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Default route | New dashboard at `/`, legacy at `/legacy` | Stitch design is the intended production UI; legacy preserved for comparison |
| Design system | CSS custom properties + Tailwind | Lightweight, no new dependencies; tokens defined once in `app.css` |
| Token cache path | Shared volume at `/data/tokens/` | API and Worker must access same cache; Docker volume ensures persistence |
| Sync trigger | Direct POST from Blazor component | Simple; no SignalR needed for on-demand sync |
| Connector status | Derived from last execution result + config | Combines config readiness (existing spec) with execution history |

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| Real Graph credentials fail silently | Medium | Validate credentials in dev before enabling; add structured error logging |
| Token cache race between API and Worker | Low | SQLite WAL mode; sequential access pattern already in place |
| Stitch design drift from implementation | Medium | Reference Stitch screens as source of truth; review each screen against design |
| Breaking existing dashboard | Low | Legacy dashboard is isolated; no shared state with new dashboard |
| Graph API rate limits during initial sync | Medium | Respect 429 responses; implement exponential backoff in connector adapters |

## Rollback Plan

1. **Graph connectors**: Set `GraphConnector__Enabled=false` in `.env` — disables all data flow immediately
2. **New dashboard**: Change default route back to legacy dashboard in `Routes.razor`
3. **SyncStatusPanel**: Revert button to stub state (no API call)
4. **All changes**: Git revert to previous commit; no database migrations involved

## Dependencies

- Real Azure AD app registration with `Calendars.Read`, `Mail.Read`, `TeamActivityFeed.Read.All` permissions
- Admin consent for delegated permissions in the target tenant
- Docker volume for token cache persistence (`/data/tokens/`)

## Success Criteria

- [ ] `POST /api/sync/now` returns real item counts for all three connectors
- [ ] Graph connector status shows `ValidConfig` with real TenantId and ClientId
- [ ] New operational dashboard renders at `/` with Stitch dark-mode design
- [ ] Connector status cards show live status (emerald=healthy, amber=warning, slate=offline)
- [ ] SyncStatusPanel triggers sync and displays per-source results
- [ ] Legacy dashboard remains accessible at `/legacy` without regression
- [ ] Token cache persists across container restarts (Docker volume)
- [ ] All existing tests pass (`dotnet test Aura.sln`)

## Proposal Question Round

Before finalizing, here are questions to sharpen the proposal:

1. **Credential management**: Should real credentials live in `.env` for local dev, or should we set up a local secrets manager (e.g., User Secrets) to avoid `.env` containing real GUIDs?

2. **Dashboard routing**: Is `/legacy` the right path for the old dashboard, or would you prefer a different convention (e.g., `/dev`, `/test`)?

3. **Initial sync behavior**: When the new dashboard loads for the first time with live data, should it auto-trigger a sync, or wait for the user to click "Sync Now"?

4. **Connector detail views**: The Stitch "Pull Requests" screen — is this a standalone view of PR reviewer data, or should it integrate with the existing morning-summary ranking?

5. **Scope of Phase 1**: Should we enable all three connectors simultaneously, or start with one (e.g., Outlook) and validate before enabling the rest?
