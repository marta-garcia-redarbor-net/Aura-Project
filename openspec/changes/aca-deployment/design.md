# Design: ACA Deployment

## Technical Approach

Three ACA container apps (UI, Api, Workers) using ghcr.io images, EF Core + Azure SQL replacing 9 raw ADO.NET SQLite stores, Qdrant on ACA with demo-mode fallback, and GitHub Actions CI/CD on `release/aura/*` branches. Stores migrate one by one under existing port contracts.

## Architecture Decisions

| Decision | Choice | Alternatives | Rationale |
|----------|--------|-------------|-----------|
| DB provider abstraction | Conditional DI (SQLite vs EF Core) | Always EF Core | Keep local dev unchanged; swap per environment via config |
| DbContext strategy | Single `AuraDbContext` with entity configs per store | One DbContext per store | Single Azure SQL DB free tier (4 GB); shared connection pool |
| Connection string switching | `appsettings.{env}.json` + env vars | Runtime detection | Standard ASP.NET pattern; ACA env vars override config |
| Store migration order | Read-only stores first (Rules, Decisions) then write-heavy (WorkItems, Outbox) | All at once | Reduce blast radius; rollback per store via config toggle |
| ACA Bicep | Single module per container app | Monolithic bicep | Independent scaling, ingress, and env var config |
| Demo data loading | API endpoints + seeded `DemoUseCase` | Startup seed (always-on) | Interactive on-demand; no perf impact at boot |
| Multi-arch builds | `docker buildx` with `linux/amd64` only for ACA | `linux/arm64` also | ACA nodes are amd64; arm64 adds build time with no benefit |

## Data Flow

```
SQLite (local dev)                           Azure SQL (ACA)
┌──────────────────────┐                    ┌──────────────────────┐
│ aura.db              │     EF Core         │ AuraDb               │
│   ├─ FocusState     │──── DbContext ──────→├─ FocusStateOverrides │
│   ├─ Decisions      │    (per env)         │─ InterruptionDecisions│
│   ├─ AlertRules     │                    │─ AlertRules           │
│   ├─ Notifications  │                    │─ Notifications        │
│   ├─ MeetingAlerts  │                    │─ MeetingAlerts        │
│   └─ MorningSummary │                    │─ MorningSummaryEmission│
│ workitems.db        │                    │─ WorkItems            │
│ semantic_outbox.db  │                    │─ SemanticOutbox       │
│ token_cache.db      │                    │─ MsalTokenCache       │
└──────────────────────┘                    └──────────────────────┘
```

## File Changes

| File | Action | Description |
|------|--------|-------------|
| `infra/aca/main.bicep` | Create | RG + ACA env + 3 container apps + Azure SQL DB |
| `infra/aca/api.bicep` | Create | API container app module: ingress, env, CORS |
| `infra/aca/ui.bicep` | Create | UI container app module: ingress, env |
| `infra/aca/workers.bicep` | Create | Workers container app module: no ingress |
| `infra/aca/sql-database.bicep` | Create | Azure SQL server + free-tier DB + firewall rule |
| `.github/workflows/deploy.yml` | Create | GA workflow: build → test → push → deploy |
| `src/Aura.Infrastructure/Aura.Infrastructure.csproj` | Modify | Add `Microsoft.EntityFrameworkCore.SqlServer`, `Microsoft.EntityFrameworkCore.Sqlite`, `Microsoft.EntityFrameworkCore.Design` |
| `src/Aura.Infrastructure/Persistence/AuraDbContext.cs` | Create | Single DbContext with entity configs for all 9 stores |
| `src/Aura.Infrastructure/Persistence/EntityConfigurations/` | Create | 9 `IEntityTypeConfiguration<T>` files (one per table) |
| `src/Aura.Infrastructure/Adapters/*/Ef*Store.cs` | Create (×9) | EF Core implementations for each port interface |
| `src/Aura.Infrastructure/DependencyInjection.cs` | Modify | Add conditional DI: SQLite or EF Core per store based on config |
| `src/Aura.Api/Dockerfile` | Modify | Multi-stage with `linux/amd64` target, health probe endpoint |
| `src/Aura.UI/Dockerfile` | Modify | Same multi-arch pattern |
| `src/Aura.Workers/Dockerfile` | Modify | Same pattern; no probe needed (no HTTP) |
| `src/Aura.Application/Demo/DemoService.cs` | Create | Demo use case: seed data loading for Morning Summary, emails, Teams, calendar, PRs |
| `src/Aura.Api/Endpoints/DemoEndpoints.cs` | Create | POST endpoints for each demo data-loading action |
| `src/Aura.Infrastructure/Adapters/Demo/QdrantFallbackHandler.cs` | Create | Graceful Qdrant skip when unavailable in demo mode |

## Interfaces / Contracts

```csharp
// Connection strategy — per store, injected into each EF store constructor
public interface IConnectionStringProvider
{
    string GetConnectionString(string storeName);
}

// Demo use case — called by API endpoints
public class DemoDataLoadRequest
{
    public DemoDataType Type { get; init; }  // MorningSummary | Emails | Teams | Calendar | PriorityAlerts | PullRequests
}

// DbContext — single database, multiple entity sets
public class AuraDbContext : DbContext
{
    public DbSet<FocusStateOverride> FocusStateOverrides => Set<FocusStateOverride>();
    public DbSet<InterruptionDecision> InterruptionDecisions => Set<InterruptionDecision>();
    // ... one DbSet per table
}
```

## Testing Strategy

| Layer | What | Approach |
|-------|------|----------|
| Unit | Each EF store maps ports correctly | Mock `AuraDbContext` with in-memory SQLite provider; verify CRUD matches old store behavior |
| Integration | Full migration: old SqliteStore vs new EfStore produce same results | Side-by-side test: seed SQLite, run same queries on EF, assert equal |
| E2E | ACA deployment + demo mode boots | Deploy to ACA playground, run health probes, invoke demo endpoints |

## Migration / Rollout

Per-store config key: `Persistence:Providers:{StoreName}` = `Sqlite` | `EntityFramework`. Roll out one store at a time. Default ACA config uses `EntityFramework` for all stores. Bicep `what-if` before every deploy.

## Open Questions

- [ ] Azure SQL firewall rules: allow Azure Services only, or add ACA outbound IPs?
- [ ] MSAL token cache in shared DB — token serialization is MSAL-v3 binary blob, needs `byte[]` column
- [ ] Demo Mode Qdrant bypass: return empty results vs throw? Spec says graceful — return empty `IReadOnlyList<>`
