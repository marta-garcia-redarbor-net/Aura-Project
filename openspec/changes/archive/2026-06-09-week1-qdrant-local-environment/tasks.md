# Tasks: Week 1 Qdrant Local Environment

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | 200–250 |
| 400-line budget risk | Low |
| Chained PRs recommended | No |
| Suggested split | Single PR |
| Delivery strategy | ask-always |
| Chain strategy | pending |

Decision needed before apply: Yes
Chained PRs recommended: No
Chain strategy: pending
400-line budget risk: Low

### Suggested Work Units

| Unit | Goal | Likely PR | Notes |
|------|------|-----------|-------|
| 1 | Qdrant local env + health check + tests + docs | PR 1 | Single PR; all phases within 400-line budget |

## Phase 1: Foundation (Docker + Config)

- [x] 1.1 Create `docker-compose.yml` — Qdrant service with `.env`-driven ports (6333/6334), `qdrant_storage` volume
- [x] 1.2 Create `.env.example` — `QDRANT_HTTP_PORT`, `QDRANT_GRPC_PORT`, `QDRANT_HOST` template
- [x] 1.3 Modify `.gitignore` — add `.env` and `qdrant_storage/` entries
- [x] 1.4 Modify `src/Aura.Api/appsettings.Development.json` — add `Qdrant` section (`Host`, `GrpcPort`)
- [x] 1.5 Modify `src/Aura.Api/Aura.Api.csproj` — add `ProjectReference` to `Aura.Infrastructure`

## Phase 2: Core Implementation (TDD)

- [x] 2.1 RED: Create `tests/Aura.UnitTests/Health/QdrantHealthCheckTests.cs` — test Healthy when `HealthAsync` succeeds; test Unhealthy when client throws
- [x] 2.2 GREEN: Create `src/Aura.Infrastructure/Health/QdrantHealthCheck.cs` — `internal sealed class` implementing `IHealthCheck`, calls `QdrantClient.HealthAsync(ct)`
- [x] 2.3 Modify `src/Aura.Infrastructure/DependencyInjection.cs` — add `services.AddHealthChecks().AddCheck<QdrantHealthCheck>("qdrant")`
- [x] 2.4 Modify `src/Aura.Api/Program.cs` — call `AddAuraInfrastructure(config)` and `app.MapHealthChecks("/health")`

## Phase 3: Integration + Architecture Tests

- [x] 3.1 Create `tests/Aura.IntegrationTests/Health/QdrantHealthCheckIntegrationTests.cs` — `WebApplicationFactory`: GET `/health` → 200 with Qdrant up, 503 with Qdrant down
- [x] 3.2 Modify `tests/Aura.ArchitectureTests/SemanticIndexArchitectureTests.cs` — assert health check types in Infrastructure; Domain/Application have no Qdrant references

## Phase 4: Documentation

- [x] 4.1 Modify `README.md` — add "Local Environment" section: prerequisites, `docker-compose up -d`, run API, verify `/health`
