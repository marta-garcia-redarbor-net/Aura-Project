# Proposal: Week 1 Qdrant Local Environment

## Intent
Establish a reproducible local environment with a persistent vector store (Qdrant) via Docker Compose, and provide verifiable connectivity evidence via an API health endpoint. This fulfills W1-H3 criteria.

## Scope

### In Scope
- Add `docker-compose.yml` configured for Qdrant local execution.
- Add `.env.example` for required local configuration.
- Implement `QdrantHealthCheck` in `Aura.Infrastructure`.
- Wire up `/health` endpoint in `Aura.Api` to return the Qdrant connection status.
- Update `README.md` with instructions to run the local environment.

### Out of Scope
- Production deployment scripts for Qdrant.
- Full UI dashboard integration for health checks (only API endpoint is exposed for now).
- Adding other services (e.g., Redis, SQL) to docker-compose.

## Capabilities

### New Capabilities
- `qdrant-local-environment`: Provides a containerized local vector store and API connectivity validation.

### Modified Capabilities
- None

## Approach
Docker-Compose + API Health Endpoint. This provides a robust local Qdrant instance via Docker Compose, uses `.env` for configuration, and exposes an `IHealthCheck` in the `Aura.Api` (`/health`). Qdrant SDK and health check logic remain strictly in `Aura.Infrastructure`, maintaining Clean Architecture boundaries.

## Affected Areas

| Area | Impact | Description |
|------|--------|-------------|
| `docker-compose.yml` | New | Defines the Qdrant container and ports |
| `.env.example` | New | Local environment variables template |
| `src/Aura.Infrastructure/Health/QdrantHealthCheck.cs` | New | Proves connectivity to Qdrant |
| `src/Aura.Infrastructure/DependencyInjection.cs` | Modified | Registers Qdrant health checks |
| `src/Aura.Api/Program.cs` | Modified | Wires up `/health` endpoint |
| `README.md` | Modified | Documents start/stop instructions |

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| Port collisions on 6333/6334 | Medium | Document how to change ports using `.env` overrides |
| API fails to start if Qdrant isn't ready | Low | Use standard health check instead of blocking startup |

## Rollback Plan
Remove `docker-compose.yml` and `.env.example`. Revert `IHealthCheck` registrations in `Aura.Infrastructure` and `MapHealthChecks` in `Aura.Api`.

## Dependencies
- Docker Desktop / engine running locally.

## Success Criteria
- [ ] `docker-compose up -d` successfully starts Qdrant.
- [ ] Running `Aura.Api` locally returns a 200 OK from `/health` demonstrating Qdrant connectivity.
- [ ] Clean Architecture is preserved: Qdrant logic remains entirely within `Infrastructure`.