# Tasks: Graph Real Data Dashboard

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | ~350-450 |
| 400-line budget risk | Medium |
| Chained PRs recommended | Yes |
| Suggested split | PR 1 (config + token cache) → PR 2 (SyncStatusPanel wiring) → PR 3 (dashboard UI) |
| Delivery strategy | ask-on-risk |
| Chain strategy | pending |

Decision needed before apply: Yes
Chained PRs recommended: Yes
Chain strategy: pending
400-line budget risk: Medium

### Suggested Work Units

| Unit | Goal | Likely PR | Notes |
|------|------|-----------|-------|
| 1 | Graph config enablement + token cache alignment | PR 1 | Config-only, low risk; .env, appsettings, GraphClientFactory scope, token cache path |
| 2 | SyncStatusPanel API wiring | PR 2 | Wire HandleSyncNow to POST /api/sync/now, add per-source status display |
| 3 | Priority dashboard UI + routing | PR 3 | New Blazor components, CSS tokens, route separation, bUnit tests |

## Phase 1: Config & Infrastructure

- [x] 1.1 Add `Calendars.Read` to default scopes in `src/Aura.Infrastructure/Adapters/Connectors/Graph/GraphClientFactory.cs` (line 26: change array to include `"Calendars.Read"`)
- [x] 1.2 Add `GraphConnector` section to `src/Aura.Api/appsettings.json` with `Enabled: false`, `TenantId: ""`, `ClientId: ""`
- [x] 1.3 Add `ConnectionStrings:TokenCache` to `src/Aura.Api/appsettings.json` with `Data Source=token_cache.db`
- [x] 1.4 Add `ConnectionStrings:TokenCache` to `src/Aura.Workers/appsettings.json` with `Data Source=token_cache.db`
- [x] 1.5 Update `.env`: set `GraphConnector__Enabled=false`, add credential placeholders, add `ConnectionStrings__TokenCache="Data Source=/data/tokens/cache.db"`
- [x] 1.6 Update `.env.example` to mirror `.env` changes
- [x] 1.7 Add unit test in `tests/Aura.UnitTests/GraphConnector/GraphClientFactoryTests.cs`: assert default scopes include `Calendars.Read`

## Phase 2: SyncStatusPanel Wiring

- [x] 2.1 Modify `src/Aura.UI/Components/Dashboard/SyncStatusPanel.razor`: inject `HttpClient`, wire `HandleSyncNow()` to `POST /api/sync/now`
- [x] 2.2 Add `_syncStatus` state (array of source statuses), `_error` string, and `_isSyncing` bool to SyncStatusPanel code block
- [x] 2.3 Add `OnInitializedAsync` to fetch `GET /api/sync/status` and populate per-source state
- [x] 2.4 Render per-source status: emerald/amber/slate badge, item count, last sync time (relative format)
- [x] 2.5 Add bUnit test in `tests/Aura.UnitTests/UI/SyncStatusPanelTests.cs`: mock HttpClient, verify POST sent on button click

## Phase 3: Priority Dashboard UI

- [x] 3.1 Add missing CSS tokens to `src/Aura.UI/wwwroot/css/stitch-dashboard.css`: `--aura-radius-card: 0.25rem`, `--aura-glow-size: 8px`, `--aura-font-mono: 'JetBrains Mono'`
- [x] 3.2 Create `src/Aura.UI/Components/Dashboard/ConnectorStatusCard.razor`: accepts `Name`, `Status` (Healthy/Warning/Offline), `ItemCount`, `LastSyncTime` params; renders glow dot + card
- [x] 3.3 Create `src/Aura.UI/Components/Dashboard/SyncButton.razor`: accepts `IsSyncing` param, emits `OnSync` EventCallback; renders loading spinner when syncing
- [x] 3.4 Create `src/Aura.UI/Components/Dashboard/RankedSummaryList.razor`: accepts list of summary items, renders ranked list with source badge + priority indicator
- [x] 3.5 Create `src/Aura.UI/Components/Dashboard/PriorityDashboard.razor` at route `/`: fetches `/api/sync/status` + `/api/connectors/graph/status`, renders ConnectorStatusCard x3, SyncButton, RankedSummaryList; handles loading/empty/error/populated states
- [x] 3.6 Add bUnit tests in `tests/Aura.UnitTests/UI/ConnectorStatusCardTests.cs`: render with each status, assert correct CSS class for glow color
- [x] 3.7 Add bUnit test in `tests/Aura.UnitTests/UI/SyncButtonTests.cs`: simulate click, assert disabled during sync, re-enabled after

## Phase 4: Routing & Integration

- [x] 4.1 Modify `src/Aura.UI/Pages/Index.razor`: change `@page "/"` to `@page "/test-dashboard"`
- [x] 4.2 Verify `src/Aura.UI/Components/Routes.razor` routes `/` to PriorityDashboard (already handled by `@page` directive on PriorityDashboard.razor)
- [x] 4.3 Run `dotnet build src/Aura.UI` — verify no build errors
- [x] 4.4 Run `dotnet test tests/Aura.UnitTests` — verify all existing + new tests pass
