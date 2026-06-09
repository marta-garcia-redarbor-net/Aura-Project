# Design: Week 1 Qdrant Local Environment

## Technical Approach

Provide a Docker Compose file for local Qdrant, reuse the existing `QdrantClient` singleton and `QdrantOptions` for a new `IHealthCheck` implementation inside Infrastructure, wire health checks through `Aura.Api`, and add `.env.example` for port/host configuration. No domain or application layer changes are needed — this is purely operational infrastructure.

## Architecture Decisions

| Decision | Choice | Alternatives | Rationale |
|----------|--------|-------------|-----------|
| Health check location | `Infrastructure/Health/QdrantHealthCheck.cs` | Dedicated adapter subfolder; inline in SemanticIndex | Follows ASP.NET convention (`Health/` folder). Health checks are cross-cutting infra, not adapter-specific. |
| QdrantClient reuse | Inject existing singleton registered by `AddSemanticIndexAdapter` | Create a separate lightweight HTTP client for health | Client is already a singleton. No reason to duplicate connection logic — both use the same gRPC endpoint. |
| Api → Infrastructure reference | Add `ProjectReference` to `Aura.Api.csproj` | Register via separate NuGet or shared host project | Workers already uses this pattern. Api needs `AddAuraInfrastructure()` to access DI registrations. |
| Docker Compose scope | Single-service Qdrant only | Multi-service with Redis/SQL placeholders | Proposal explicitly scopes to Qdrant only. Other services are out of scope. |
| Port configuration | `.env` file consumed by `docker-compose.yml` + `appsettings` | Hardcoded ports | `.env` avoids collisions; `appsettings.Development.json` maps the same values for the app. |

## Data Flow

```
docker-compose up -d
       │
       ▼
  Qdrant Container (ports 6333 HTTP / 6334 gRPC)
       ▲
       │ gRPC
       │
  QdrantClient (singleton, registered by AddSemanticIndexAdapter)
       ▲
       │
  QdrantHealthCheck : IHealthCheck
       ▲
       │
  ASP.NET HealthCheckService ──→ GET /health ──→ 200 OK / 503
```

## File Changes

| File | Action | Description |
|------|--------|-------------|
| `docker-compose.yml` | Create | Qdrant service with `.env`-driven ports and `./qdrant_storage` volume |
| `.env.example` | Create | Template: `QDRANT_HTTP_PORT`, `QDRANT_GRPC_PORT`, `QDRANT_HOST` |
| `.gitignore` | Modify | Add `.env` and `qdrant_storage/` entries |
| `src/Aura.Infrastructure/Health/QdrantHealthCheck.cs` | Create | `IHealthCheck` impl; calls `QdrantClient.HealthAsync()` |
| `src/Aura.Infrastructure/DependencyInjection.cs` | Modify | Add `services.AddHealthChecks().AddCheck<QdrantHealthCheck>("qdrant")` |
| `src/Aura.Api/Aura.Api.csproj` | Modify | Add `ProjectReference` to `Aura.Infrastructure` |
| `src/Aura.Api/Program.cs` | Modify | Call `AddAuraInfrastructure()`; add `app.MapHealthChecks("/health")` |
| `src/Aura.Api/appsettings.Development.json` | Modify | Add `Qdrant` config section (`Host`, `GrpcPort`) |
| `README.md` | Modify | Add "Local Environment" section with setup instructions |
| `tests/Aura.IntegrationTests/Health/QdrantHealthCheckTests.cs` | Create | Verify healthy/unhealthy responses with real/mock client |
| `tests/Aura.ArchitectureTests/SemanticIndexArchitectureTests.cs` | Modify | Add test: health check types stay in Infrastructure |

## Interfaces / Contracts

No new application ports. `QdrantHealthCheck` implements the framework's `IHealthCheck`:

```csharp
// src/Aura.Infrastructure/Health/QdrantHealthCheck.cs
internal sealed class QdrantHealthCheck : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken ct)
    {
        // calls QdrantClient.HealthAsync(ct)
    }
}
```

Marked `internal` — sealed inside Infrastructure, visible to test projects via `InternalsVisibleTo`.

## Testing Strategy

| Layer | What to Test | Approach |
|-------|-------------|----------|
| Unit | `QdrantHealthCheck` returns Healthy/Unhealthy | Mock `QdrantClient`; verify `HealthCheckResult` |
| Integration | `/health` returns 200 when Qdrant is up | `WebApplicationFactory` with real Qdrant from docker-compose |
| Architecture | Qdrant SDK stays in Infrastructure | Extend existing `SemanticIndexArchitectureTests` with health check assertion |

## Migration / Rollout

No migration required. Rollback: remove `docker-compose.yml`, `.env.example`, revert `Program.cs` and `DependencyInjection.cs` changes.

## Open Questions

- [ ] Should `qdrant_storage/` volume be in a shared `./data/` folder for future services, or stay service-specific?
