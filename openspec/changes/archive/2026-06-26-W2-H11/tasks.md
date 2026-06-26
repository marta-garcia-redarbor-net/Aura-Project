# Tasks: Docker-first Local Deployment

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | 250–350 |
| 400-line budget risk | Medium |
| Chained PRs recommended | No |
| Suggested split | Single PR with 4 work-unit commits |
| Delivery strategy | ask-on-risk |
| Chain strategy | size-exception |

### Suggested Work Units

| Unit | Goal | Likely PR | Notes |
|------|------|-----------|-------|
| 1 | Dockerfiles + .dockerignore | PR 1 (commit 1) | Multi-stage builds for all 3 hosts + ignore rules |
| 2 | Compose services + .env.example | PR 1 (commit 2) | docker-compose.yml extension + env var contract |
| 3 | Program.cs env var reading | PR 1 (commit 3) | No changes needed — ASP.NET Core reads env vars via `__` automatically |
| 4 | Smoke test script | PR 1 (commit 4) | PowerShell verification script for all containers |

## Phase 1: Infrastructure — Dockerfiles and Build Context

- [x] 1.1 Create `.dockerignore` at repo root: exclude `tests/`, `docs/`, `openspec/`, `.git/`, `**/bin/`, `**/obj/`, `*.md`, `.env`, `.env.*`, `qdrant_storage/`, `data/`, `.vs/`, `.vscode/`, `*.user`, `*.suo`
- [x] 1.2 Create `src/Aura.Api/Dockerfile`: multi-stage build — SDK `mcr.microsoft.com/dotnet/sdk:9.0` build stage, runtime `mcr.microsoft.com/dotnet/aspnet:9.0` runtime stage; restore, build, publish `Aura.Api.csproj`. **Includes `curl` install for health checks.**
- [x] 1.3 Create `src/Aura.UI/Dockerfile`: multi-stage build — SDK `mcr.microsoft.com/dotnet/sdk:9.0` build stage, runtime `mcr.microsoft.com/dotnet/aspnet:9.0` runtime stage; restore, build, publish `Aura.UI.csproj`. **Includes `curl` install for health checks.**
- [x] 1.4 Create `src/Aura.Workers/Dockerfile`: multi-stage build — SDK `mcr.microsoft.com/dotnet/sdk:9.0` build stage, runtime `mcr.microsoft.com/dotnet/aspnet:9.0` runtime stage (NOT `runtime:9.0` — Workers uses `Host.CreateApplicationBuilder` which requires ASP.NET Core runtime); restore, build, publish `Aura.Workers.csproj`. **Includes `curl` install for health checks.**

## Phase 2: Compose Services and Environment

- [x] 2.1 Extend `docker-compose.yml`: add `aura-api` service (build context `.`, dockerfile `src/Aura.Api/Dockerfile`, port `${API_PORT:-5190}:8080`, volume `./data:/data`, env_file `.env`, depends_on `qdrant` with health check)
- [x] 2.2 Extend `docker-compose.yml`: add `aura-ui` service (build context `.`, dockerfile `src/Aura.UI/Dockerfile`, port `${UI_PORT:-5180}:8080`, env_file `.env`, depends_on `aura-api` with health check)
- [x] 2.3 Extend `docker-compose.yml`: add `aura-workers` service (build context `.`, dockerfile `src/Aura.Workers/Dockerfile`, volume `./data:/data`, env_file `.env`, depends_on `aura-api` with health check)
- [x] 2.4 Extend `docker-compose.yml`: add dedicated `aura-network` bridge network; attach all 4 services to it
- [x] 2.5 Expand `.env.example` with all env vars using ASP.NET Core `__` convention: `AzureAd__ClientId`, `AzureAd__TenantId`, `AzureAd__Scopes`, `UseEntraId`, `AuraApi__BaseUrl`, `Qdrant__Host`, `Qdrant__HttpPort`, `Qdrant__GrpcPort`, `ConnectionStrings__Aura`, `ConnectionStrings__SemanticOutbox`, `UI_PORT`, `API_PORT`

## Phase 3: Application — Environment Variable Reading

- [x] 3.1 `src/Aura.Api/Program.cs`: No changes needed — `builder.Configuration` already reads env vars via `__` separator automatically. `Qdrant__Host`, `ConnectionStrings__Aura` are resolved from env without code changes.
- [x] 3.2 `src/Aura.UI/Program.cs`: No changes needed — `AuraApi:BaseUrl` resolves from `AuraApi__BaseUrl` env var automatically. `AzureAd` section resolves from `AzureAd__*` env vars.
- [x] 3.3 `src/Aura.Workers/Program.cs`: No changes needed — `Qdrant__Host` defaults to `aura-qdrant` via env var override.

## Phase 4: Verification — Smoke Test

- [x] 4.1 Create `scripts/docker-smoke-test.ps1`: verify all 4 containers running, curl UI on `${UI_PORT:-5180}`, curl API `/health` on `${API_PORT:-5190}`, check Qdrant healthz, check Workers logs for crash-loop; exit 0 on success, non-zero on failure
- [x] 4.2 Verify: `docker compose up --build -d` starts all containers without error — **PASSED after fixing health checks and runtime image**
- [x] 4.3 Verify: smoke test — **PASSED** (API healthy, Qdrant healthy, Workers running)
- [x] 4.4 Verify: stop `aura-api` container → smoke test exits non-zero — **PASSED**

### Testing Findings (2026-06-26)

**Issues found and resolved during docker compose testing:**

1. **Qdrant health check failed**: Qdrant image doesn't have `curl` or `wget`. Fixed by using `bash -c 'echo > /dev/tcp/localhost/6333'` (bash built-in TCP check).

2. **API/UI health checks needed `curl`**: ASP.NET Core images don't include `curl`. Fixed by adding `RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*` to Dockerfiles.

3. **Workers crashed with `runtime:9.0`**: `Host.CreateApplicationBuilder` requires `Microsoft.AspNetCore.App` framework, not just `Microsoft.NETCore.App`. Fixed by changing Workers Dockerfile from `mcr.microsoft.com/dotnet/runtime:9.0` to `mcr.microsoft.com/dotnet/aspnet:9.0`.

4. **API crashed: `EmbeddingProvider` section missing**: In Production mode, `appsettings.Development.json` isn't loaded. Fixed by adding `EmbeddingProvider__*` env vars to `.env`.

5. **Workers crashed: empty API key for EmbeddingProvider**: `ApiKeyCredential` requires non-empty key. Fixed by adding `EmbeddingProvider__ApiKey=placeholder-replace-with-real-key` to `.env`.

6. **UI returns 500**: Expected — `AzureAd__TenantId` and `AzureAd__ClientId` are empty. User must configure real Entra ID credentials.

**Pre-existing issues (not caused by Docker):**
- Workers warnings: `CheckAndDispatchMeetingAlertsUseCase` not registered — missing DI registration in Workers project.
- Integration test failure: `SyncEndpointTests.PostSyncNow_ThenGetDashboardPreview_ReturnsItemsWithSyncedFields` — unrelated to Docker changes.

### Note on Phase 3

Program.cs files were NOT modified. ASP.NET Core's configuration system automatically reads environment variables using `__` as the section separator. For example:
- `Qdrant__Host=aura-qdrant` → `config["Qdrant:Host"]`
- `AzureAd__ClientId=xxx` → `config["AzureAd:ClientId"]`
- `ConnectionStrings__Aura=Data Source=/data/aura.db` → `config["ConnectionStrings:Aura"]`

This is the framework's native convention — no explicit mapping code is required.
