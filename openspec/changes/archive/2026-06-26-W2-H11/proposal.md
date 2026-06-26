# Proposal: Docker-first Local Deployment with Separate Hosts

## Intent

Consolidate local development into Docker Compose with separate containers for Aura.UI, Aura.Api, and Aura.Workers, replacing the current `dotnet run` approach while preserving architectural boundaries.

## Scope

### In Scope
- Create Dockerfiles for all three hosts (ASP.NET Core web, Blazor Server, .NET Worker)
- Extend docker-compose.yml with aura-ui, aura-api, aura-workers services
- Configure SQLite volumes for aura.db and MSAL token cache
- Externalize environment variables for Entra ID and Graph configuration
- Add .dockerignore for build optimization
- Expand .env.example with all required variables
- Add Docker local startup smoke test

### Out of Scope
- Production deployment or cloud orchestration
- CI/CD pipeline integration
- Container health checks beyond basic startup
- Logging/monitoring infrastructure in containers
- Container orchestration beyond local development

## Capabilities

### New Capabilities
- `docker-local-deployment`: Docker Compose configuration for local development with separate containers
- `container-configuration`: Environment variable injection and volume configuration for containers

### Modified Capabilities
None — this change creates deployment infrastructure, not spec-level behavior changes.

## Approach

1. **Create Dockerfiles**: Multi-stage builds targeting net9.0 with appropriate SDKs (ASP.NET Core for UI/API, Worker SDK for Workers)
2. **Extend docker-compose.yml**: Add three services with proper networking, port mapping, and volume mounts
3. **Configure volumes**: Mount SQLite databases to host paths for persistence across container restarts
4. **Externalize configuration**: Move all hardcoded values to environment variables with sensible defaults
5. **Add .dockerignore**: Exclude build artifacts, test projects, and documentation from container context
6. **Create smoke test**: Script to verify all containers start and communicate correctly

## Affected Areas

| Area | Impact | Description |
|------|--------|-------------|
| `docker-compose.yml` | Modified | Extend with aura-ui, aura-api, aura-workers services |
| `src/Aura.Api/Dockerfile` | New | ASP.NET Core web SDK container |
| `src/Aura.UI/Dockerfile` | New | Blazor Server with WebSocket support |
| `src/Aura.Workers/Dockerfile` | New | .NET Worker SDK, non-web |
| `.dockerignore` | New | Build optimization |
| `.env.example` | Modified | Expand with all environment variables |
| `src/*/Program.cs` | Modified | Minor adjustments for env var reading in containers |

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| SQLite file locking in Docker volumes | Medium | Use named volumes with proper permissions; test concurrent access |
| WebSockets for Blazor Server in containers | Low | Configure proper WebSocket headers and proxy settings |
| Port conflicts with `dotnet run` | High | Document different port ranges; stop dotnet run before Docker |
| Dockerfiles targeting wrong OS | Low | Use Linux base images; test on Windows containers |

## Rollback Plan

1. Remove new Dockerfiles and .dockerignore
2. Revert docker-compose.yml to original Qdrant-only configuration
3. Remove expanded .env.example entries
4. Restore any modified Program.cs files to original state

## Dependencies

- W2-H9 (delegated auth) and W2-H10 (Graph delegated flow) must be IMPLEMENTED before Docker deployment is meaningful
- Docker Desktop installed and running on development machines
- Linux containers enabled in Docker Desktop

## Success Criteria

- [ ] All three containers start successfully with `docker compose up`
- [ ] SQLite databases persist across container restarts
- [ ] Environment variables are properly injected and read by each host
- [ ] UI is accessible via browser and can authenticate with Entra ID
- [ ] API responds to requests from UI container
- [ ] Workers container runs background tasks without errors
- [ ] Qdrant container remains accessible to API and Workers

## Proposal question round

**Questions for user review:**

1. **Port mapping**: Should we use fixed ports (5180, 5190) or dynamic ports to avoid conflicts with `dotnet run`?

2. **Volume paths**: Should SQLite files be mounted to host paths (e.g., `./data/aura.db`) or Docker named volumes?

3. **Build optimization**: Should we use multi-stage builds to reduce image size, or keep simple Dockerfiles for faster iteration?

4. **Health checks**: Should we add basic health checks to each container, or rely on Docker Compose's `depends_on`?

5. **Environment file**: Should we use a single `.env` file or separate files per service?

**Assumptions needing user review:**
- W2-H9 and W2-H10 are IMPLEMENTED (not just in progress)
- Docker Desktop with Linux containers is available
- Development machines have sufficient resources for 4+ containers
- Current port assignments (5180, 5190) are acceptable