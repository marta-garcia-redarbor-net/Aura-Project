## Exploration: week1-qdrant-local-environment

### Current State
- The infrastructure refactor is complete, leaving `Aura.Infrastructure` with working `QdrantClient` integration tests via Testcontainers.
- The project lacks a local reproducible standing environment (no `docker-compose.yml`), making it hard for developers to run Aura components against a persistent local vector store.
- `Aura.Api` is currently an empty minimal API without health checks or connectivity endpoints.

### Affected Areas
- `docker-compose.yml` (New) — Defines the Qdrant container, volumes, ports, and Docker healthcheck.
- `.env.example` (New) — Documents the local environment variables needed.
- `src/Aura.Infrastructure/DependencyInjection.cs` — Register Qdrant health checks.
- `src/Aura.Infrastructure/Health/QdrantHealthCheck.cs` (New) — Proves connectivity to Qdrant from the infrastructure layer.
- `src/Aura.Api/Program.cs` — Wires up `/health` endpoint to expose connectivity evidence.
- `README.md` — Document how to start and stop the local environment.

### Approaches
1. **Docker-Compose + API Health Endpoint (Recommended)**
   - Pros: Solves Docker setup, configuration, and provides a standard `.NET` way to verify connectivity. Fulfills both T2 (local config) and T4 (connectivity endpoint).
   - Cons: Slightly more code than a raw console script.
   - Effort: Low

2. **Docker-Compose + Script/Test only**
   - Pros: Minimal code changes to `src/`.
   - Cons: Doesn't provide a living endpoint in `Aura.Api` for ongoing operational visibility. Doesn't fulfill the "endpoint simple" option in W1-H3-T4 as elegantly.
   - Effort: Low

### Recommendation
**Approach 1 (Docker-Compose + API Health Endpoint)**. It provides a robust local Qdrant instance via Docker Compose, uses `.env` for configuration, and exposes an `IHealthCheck` in the `Aura.Api` (`/health`). This elegantly satisfies W1-H3 criteria for Docker, configuration, and connectivity evidence in a single cohesive slice. 

### Risks
- Port collisions on 6333/6334 (Qdrant REST/gRPC) in local environments if developers are already running instances.
- Ensuring the API uses the correct gRPC port when connecting to the local compose instance.

### Ready for Proposal
Yes. The orchestrator can proceed to `sdd-propose`.