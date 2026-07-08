# Design: Graph Real Data Dashboard

## Technical Approach

Enable live Microsoft Graph data flow and build a Stitch-designed operational dashboard. Five workstreams: (1) Graph config enablement, (2) SyncStatusPanel API wiring, (3) Token cache path alignment for Docker, (4) New Blazor priority dashboard with Stitch design tokens, (5) Route separation for legacy/new dashboards.

The implementation leverages existing infrastructure — `SyncEndpoints.cs` already exposes `POST /api/sync/now` and `GET /api/sync/status`, `GraphConnectorOptions` already binds from config, and `MsalSqliteTokenCache` already persists tokens. The main work is config wiring, Blazor component creation, and CSS token definition.

## Architecture Decisions

### Decision: Default route — new dashboard at `/`, legacy at `/test-dashboard`

| Option | Tradeoff | Decision |
|--------|----------|----------|
| New at `/`, legacy at `/test-dashboard` | Clean production path; legacy preserved for comparison | **Chosen** |
| New at `/dashboard`, legacy at `/` | Familiar but hides the new UI behind extra nav | Rejected |
| Feature flag toggle | Adds runtime complexity for a one-time migration | Rejected |

### Decision: Design system via CSS custom properties (not Tailwind runtime)

| Option | Tradeoff | Decision |
|--------|----------|----------|
| CSS custom properties in `stitch-dashboard.css` | Zero new deps; tokens already partially defined | **Chosen** |
| Tailwind CDN runtime | Adds ~300ms load; inconsistent with existing pattern | Rejected |
| Compiled Tailwind via build | Requires PostCSS toolchain; overkill for token-only use | Rejected |

### Decision: SyncStatusPanel — HttpClient POST from Blazor component

| Option | Tradeoff | Decision |
|--------|----------|----------|
| Direct `HttpClient.POST` via injected client | Simple; matches existing `GraphConnectorStatusPanel` pattern | **Chosen** |
| Dedicated `ISyncApiClient` service | More testable but adds abstraction for one endpoint | Rejected (defer to future) |
| SignalR push for sync status | Real-time but existing polling is sufficient | Rejected |

### Decision: Token cache alignment via `ConnectionStrings__TokenCache` env var

| Option | Tradeoff | Decision |
|--------|----------|----------|
| Override via env var in docker-compose | Already used for other connection strings; zero code change | **Chosen** |
| Hardcode `/data/tokens/cache.db` in `DependencyInjection.cs` | Breaks local dev without Docker | Rejected |
| Separate config file per environment | Over-engineered for two environments | Rejected |

## Data Flow

### Sync Trigger Flow

```
User clicks "Sync Now"
  → Blazor POST /api/sync/now
    → TriggerSyncUseCase.ExecuteAsync()
      → ExecuteConnectorUseCase per connector (Outlook, Teams, Calendar)
        → Graph API call via GraphServiceClient
        → Result persisted to ISyncStateStore
  ← Response: SyncResult with per-source item counts + status
  → UI updates connector status cards
```

### Connector Status Flow

```
Dashboard loads
  → GET /api/sync/status → ISyncStateStore.GetAllAsync()
    → Per-source: last sync time, item count, status
  → GET /api/connectors/graph/status → IGraphConnectorStatusReader
    → Derived state: ValidConfig / PartialConfig / Disabled
  → Render connector status cards with emerald/amber/slate badges
```

## Component Design

### PriorityDashboard.razor (new — default route `/`)

```
PriorityDashboard.razor
├── ConnectorStatusCard × 3 (Outlook, Teams, Calendar)
│   ├── StatusBadge (glow dot: emerald/amber/slate)
│   ├── ItemCount (JetBrains Mono)
│   └── LastSyncTime (relative: "2 min ago")
├── SyncButton (loading spinner, disabled during sync)
└── RankedSummaryList
    └── SummaryItem × N (source icon, title, snippet, score)
```

**State management**: Component-level `_syncStatus` (SyncStatusResponse[]), `_isSyncing` (bool), `_error` (string?). Fetches on `OnInitializedAsync`. Re-fetches after sync completes.

### CSS Token Additions (stitch-dashboard.css)

Existing tokens already cover most of the Stitch palette. Add:
- `--aura-radius-card: 0.25rem` (card border-radius from Stitch spec)
- `--aura-glow-size: 8px` (status badge glow diameter)
- `--aura-font-mono: 'JetBrains Mono'` (alias for metric labels)

## API Changes

**No new endpoints.** Existing endpoints are sufficient:
- `POST /api/sync/now` — triggers all three connectors (already in `SyncEndpoints.cs`)
- `GET /api/sync/status` — returns per-source sync state (already in `SyncEndpoints.cs`)
- `GET /api/connectors/graph/status` — returns connector config readiness (already in `GraphConnectorEndpoints.cs`)

The only backend change is adding `Calendars.Read` to `GraphClientFactory` default scopes.

## Config Changes

### `.env` additions

```bash
# Graph Connector — set to true with real credentials to enable live data
GraphConnector__Enabled=true
GraphConnector__TenantId=<real-tenant-guid>
GraphConnector__ClientId=<real-client-guid>

# Token cache — shared between API and Worker in Docker
ConnectionStrings__TokenCache="Data Source=/data/tokens/cache.db"
```

### `src/Aura.Api/appsettings.json` additions

```json
{
  "GraphConnector": {
    "Enabled": false,
    "TenantId": "",
    "ClientId": ""
  },
  "ConnectionStrings": {
    "TokenCache": "Data Source=token_cache.db"
  }
}
```

### `src/Aura.Workers/appsettings.json` additions

```json
{
  "ConnectionStrings": {
    "TokenCache": "Data Source=token_cache.db"
  }
}
```

### `docker-compose.yml` — Token cache volume

Already has `./data:/data` volume on both `aura-api` and `aura-workers`. The `ConnectionStrings__TokenCache` env var overrides the relative path. No docker-compose change needed.

### `GraphClientFactory.cs` — Add `Calendars.Read`

```csharp
_scopes = opts.Scopes ?? ["Mail.Read", "Chat.Read", "User.Read", "Calendars.Read"];
```

## File Changes

| File | Action | Description |
|------|--------|-------------|
| `src/Aura.Infrastructure/Adapters/Connectors/Graph/GraphClientFactory.cs` | Modify | Add `Calendars.Read` to default scopes array |
| `.env` | Modify | Add `GraphConnector__Enabled=true`, credential placeholders, `ConnectionStrings__TokenCache` |
| `.env.example` | Modify | Mirror `.env` changes |
| `src/Aura.Api/appsettings.json` | Modify | Add `GraphConnector` section and `ConnectionStrings:TokenCache` |
| `src/Aura.Workers/appsettings.json` | Modify | Add `ConnectionStrings:TokenCache` |
| `src/Aura.UI/Components/Dashboard/PriorityDashboard.razor` | Create | New Blazor component — Stitch-designed priority dashboard |
| `src/Aura.UI/Components/Dashboard/ConnectorStatusCard.razor` | Create | Reusable connector status card with glow badge |
| `src/Aura.UI/Components/Dashboard/SyncButton.razor` | Create | Sync trigger button with loading state |
| `src/Aura.UI/Components/Dashboard/RankedSummaryList.razor` | Create | Ranked cross-connector summary items |
| `src/Aura.UI/Components/Dashboard/SyncStatusPanel.razor` | Modify | Wire sync-now button to `POST /api/sync/now` via HttpClient |
| `src/Aura.UI/Components/Routes.razor` | Modify | Route `/` to PriorityDashboard, keep `/test-dashboard` for old Index |
| `src/Aura.UI/Components/Layout/MainLayoutAuthenticated.razor` | Modify | Decouple from hardcoded dashboard state for new dashboard |
| `src/Aura.UI/Pages/Index.razor` | Modify | Change `@page` to `/test-dashboard` |
| `src/Aura.UI/wwwroot/css/stitch-dashboard.css` | Modify | Add `--aura-radius-card`, `--aura-glow-size`, connector card styles |

## Testing Strategy

| Layer | What to Test | Approach |
|-------|-------------|----------|
| Unit | `GraphClientFactory` default scopes include `Calendars.Read` | xUnit assertion on scopes array |
| Unit | `ConnectorStatusCard` renders correct glow color per state | bUnit: render with each status, assert CSS class |
| Unit | `SyncButton` disables during sync, re-enables after | bUnit: simulate click, assert disabled state |
| Unit | `SyncStatusPanel` calls POST on button click | bUnit: mock HttpClient, verify POST sent |
| Integration | `GET /api/sync/status` returns per-source state | Mvc.Testing + WireMock for Graph stub |
| Integration | `POST /api/sync/now` triggers all three connectors | Mvc.Testing + mock connector adapters |
| E2E | Priority dashboard renders at `/` with connector cards | Manual verification (no Playwright yet) |
| Regression | Legacy dashboard at `/test-dashboard` unchanged | Manual: navigate, verify panels render |

## Migration / Rollout

1. **Config-first**: Merge `.env` and `appsettings.json` changes with `GraphConnector__Enabled=false` — no behavior change
2. **Code change**: Add `Calendars.Read` scope — backward compatible, just adds a scope
3. **UI change**: Route separation — old dashboard moves to `/test-dashboard`, new at `/`
4. **Enable**: Set `GraphConnector__Enabled=true` with real credentials — activates live data

Rollback: Set `GraphConnector__Enabled=false` reverts to mock data. Change default route back in `Routes.razor` restores old dashboard.

## Risks and Mitigations

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| Real Graph credentials fail silently | Medium | Validate in dev; structured error logging in connector adapters |
| Token cache race (API vs Worker) | Low | SQLite WAL mode; sequential access pattern already in place |
| Stitch design drift | Medium | Reference Stitch screens as source of truth per screen |
| Breaking existing dashboard | Low | Legacy dashboard isolated; no shared state |
| Graph API 429 rate limits on first sync | Medium | Exponential backoff in connector adapters (existing pattern) |
| Blazor SSR + InteractiveServer render mismatch | Low | Use `@rendermode InteractiveServer` consistently on new components |
