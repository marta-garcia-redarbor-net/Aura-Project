# Tasks: ACA Deployment

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | ~1,200–1,800 |
| 400-line budget risk | High |
| Chained PRs recommended | Yes |
| Suggested split | PR 1 → PR 2 → PR 3 → PR 4 → PR 5 |
| Delivery strategy | ask-always |
| Chain strategy | pending |

Decision needed before apply: Yes
Chained PRs recommended: Yes
Chain strategy: pending
400-line budget risk: High

### Suggested Work Units

| Unit | Goal | PR | Base |
|------|------|----|------|
| 1 | DbContext + entity configs + Bicep | PR 1 | feature/aca-deploy |
| 2 | 9 EF stores + conditional DI | PR 2 | PR 1 branch |
| 3 | Dockerfiles + health + GA workflow | PR 3 | PR 2 branch |
| 4 | Demo + Qdrant fallback | PR 4 | PR 3 branch |
| 5 | Tests | PR 5 | PR 4 branch |

## Phase 1: Foundation

- [x] 1.1 Add `Microsoft.EntityFrameworkCore.SqlServer` + `Sqlite` + `Design` deps
- [x] 1.2 Create `AuraDbContext` with `DbSet<>` for all 9 tables
- [x] 1.3 Create 9 `IEntityTypeConfiguration<T>` entity config files
- [x] 1.4 Create `IConnectionStringProvider` interface
- [x] 1.5 Create `infra/aca/main.bicep` — RG + ACA env + 3 containers + SQL
- [x] 1.6 Create `infra/aca/api.bicep` — API container module: ingress, CORS
- [x] 1.7 Create `infra/aca/ui.bicep` — UI container module: ingress
- [x] 1.8 Create `infra/aca/workers.bicep` — Workers module (no ingress)
- [x] 1.9 Create `infra/aca/sql-database.bicep` — SQL server + free DB + firewall

## Phase 2: Store Migration

- [x] 2.1–2.9 Create EF Core stores: FocusState, InterruptionDecision, AlertRule, Notification, MeetingAlert, MorningSummary, WorkItem, SemanticOutbox, MsalTokenCache
- [x] 2.10 Update `DependencyInjection.cs` — conditional DI per store via config toggle

## Phase 3: Container + CI/CD

- [x] 3.1 Update `src/Aura.Api/Dockerfile` — multi-stage, linux/amd64, ASPNETCORE_URLS
- [x] 3.2 Update `src/Aura.UI/Dockerfile` — same multi-arch pattern
- [x] 3.3 Update `src/Aura.Workers/Dockerfile` — same multistage build (no HTTP)
- [x] 3.4 Add `GET /health` endpoint returning 200 / 503
- [x] 3.5 Create `.github/workflows/deploy.yml` — build → test → push → deploy
- [x] 3.6 Add ACA + Azure SQL env vars to `appsettings.{env}.json`

## Phase 4: Demo Mode

- [x] 4.1 Create `DemoService` — seed data loading for Morning Summary, email, Teams, calendar, PRs
- [x] 4.2 Create `DemoEndpoints` — POST endpoints per demo data type
- [x] 4.3 Create `QdrantFallbackHandler` — graceful empty results on Qdrant failure
- [x] 4.4 Wire `DemoMode__Enabled` toggle to UI button visibility

## Phase 5: Testing

- [x] 5.1 Unit test each EF store with in-memory SQLite — verify port contract
- [x] 5.2 Integration test — side-by-side SQLite vs EF Core, assert equal results
- [x] 5.3 Arch test — no store type leaks outside Infrastructure
- [x] 5.4 Test health probe — 200 healthy, 503 with db failure
- [x] 5.5 Test demo endpoints — data loads, Qdrant fallback returns empty
