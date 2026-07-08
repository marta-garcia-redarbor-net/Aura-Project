# Container Configuration Specification

## Purpose

Environment variable injection, volume configuration, and build context management for Aura's Docker-based local deployment. Ensures secrets stay out of images and SQLite state persists across restarts.

## Requirements

### Requirement: Environment Variable Externalization

Each host MUST read its configuration from environment variables injected by Compose. No host SHALL hardcode Entra ID, Graph, Qdrant, or path values.

ASP.NET Core automatically maps environment variables to configuration sections using `__` as the section separator. For example, `AzureAd__ClientId` maps to `config["AzureAd:ClientId"]`. This is the framework's native convention — no explicit mapping code is required.

#### Scenario: UI reads Entra config from env vars

- GIVEN `AzureAd__TenantId`, `AzureAd__ClientId`, and `AzureAd__Scopes` are set in `.env`
- WHEN `aura-ui` container starts
- THEN the application reads these values from environment via IConfiguration, not from `appsettings.json`

#### Scenario: API reads persistence paths from env vars

- GIVEN `ConnectionStrings__Aura` and `ConnectionStrings__SemanticOutbox` are set in `.env`
- WHEN `aura-api` container starts
- THEN the SQLite database and semantic outbox are created/mapped at the specified paths

#### Scenario: Workers read Qdrant config from env vars

- GIVEN `Qdrant__Host=aura-qdrant` and ports are set in `.env`
- WHEN `aura-workers` container starts
- THEN the worker connects to Qdrant using the Docker DNS name, not `localhost`

### Requirement: Environment Variable Contract

The system SHALL use ASP.NET Core's native `__` separator convention for environment variable names. This maps directly to configuration sections without explicit binding code. `Qdrant__Host` MUST default to `aura-qdrant` (Docker service name) inside containers.

| Variable | Config Section | Default | Used By |
|----------|---------------|---------|---------|
| `AzureAd__ClientId` | `AzureAd:ClientId` | (required) | UI, API |
| `AzureAd__TenantId` | `AzureAd:TenantId` | (required) | UI, API |
| `AzureAd__Scopes` | `AzureAd:Scopes` | `openid profile email` | UI |
| `UseEntraId` | `UseEntraId` | `false` | UI |
| `AuraApi__BaseUrl` | `AuraApi:BaseUrl` | `http://aura-api:8080` | UI |
| `Qdrant__Host` | `Qdrant:Host` | `aura-qdrant` | API, Workers |
| `Qdrant__HttpPort` | `Qdrant:HttpPort` | `6333` | API, Workers |
| `Qdrant__GrpcPort` | `Qdrant:GrpcPort` | `6334` | API, Workers |
| `ConnectionStrings__Aura` | `ConnectionStrings:Aura` | `Data Source=/data/aura.db` | API, Workers |
| `ConnectionStrings__SemanticOutbox` | `ConnectionStrings:SemanticOutbox` | `Data Source=/data/semantic_outbox.db` | API, Workers |
| `EmbeddingProvider__Endpoint` | `EmbeddingProvider:Endpoint` | (required) | API, Workers |
| `EmbeddingProvider__DeploymentName` | `EmbeddingProvider:DeploymentName` | (required) | API, Workers |
| `EmbeddingProvider__ApiKey` | `EmbeddingProvider:ApiKey` | (required) | API, Workers |
| `GraphConnector__Enabled` | `GraphConnector:Enabled` | `false` | API, Workers |
| `GraphConnector__TenantId` | `GraphConnector:TenantId` | (empty) | API, Workers |
| `GraphConnector__ClientId` | `GraphConnector:ClientId` | (empty) | API, Workers |
| `UI_PORT` | (Compose host port) | `5180` | Compose |
| `API_PORT` | (Compose host port) | `5190` | Compose |

#### Scenario: Missing required env var causes startup failure

- GIVEN `AzureAd__ClientId` is NOT set in `.env`
- WHEN `docker compose up` is executed with `UseEntraId=true`
- THEN `aura-ui` container exits with a clear error indicating the missing variable

#### Scenario: Defaults apply for optional vars

- GIVEN `.env` only contains `AzureAd__ClientId` and `AzureAd__TenantId`
- WHEN `docker compose up` is executed
- THEN `Qdrant__Host` defaults to `aura-qdrant` inside API and Workers containers
- AND `ConnectionStrings__Aura` defaults to `Data Source=/data/aura.db`

### Requirement: SQLite Volume Persistence

The system SHALL mount host directory `./data/` to `/data/` in API and Workers containers. The `aura.db` and MSAL token cache files MUST persist across container restarts and removals.

#### Scenario: Database persists across restart

- GIVEN Aura has been running and created `./data/aura.db`
- WHEN `docker compose down` followed by `docker compose up` is executed
- THEN `./data/aura.db` still contains the previously created data

#### Scenario: Token cache persists across restart

- GIVEN a user has authenticated and the MSAL cache exists at `./data/msal-token-cache.db`
- WHEN containers are restarted
- THEN the token cache file is preserved and silent token renewal succeeds on next API call

#### Scenario: Data directory is created if absent

- GIVEN `./data/` does not exist on the host
- WHEN `docker compose up` is executed
- THEN Docker creates the `./data/` directory and containers start normally

### Requirement: Qdrant Volume Persistence

The system SHALL preserve the existing `qdrant_storage` volume mount for the Qdrant container. This volume MUST NOT be removed or altered by this change.

#### Scenario: Qdrant data survives restart

- GIVEN Qdrant has indexed vector data in `./qdrant_storage/`
- WHEN `docker compose down` and `docker compose up` are executed
- THEN the previously indexed data is still available

### Requirement: No Secrets in Images

The system SHALL NOT bake environment variables, `.env` files, or credential files into Docker images. Secrets MUST be injected at runtime via Compose `env_file` or `environment` blocks.

#### Scenario: Image does not contain .env

- GIVEN a Docker image is built for any Aura host
- WHEN the image layers are inspected
- THEN `.env` is not present in any layer

#### Scenario: Image does not contain hardcoded secrets

- GIVEN a Docker image is built for Aura.Api
- WHEN the published output is inspected
- THEN no `appsettings*.json` file contains literal Entra tenant/client values

### Requirement: .env.example Completeness

The system SHALL maintain a `.env.example` file that lists every supported environment variable with a placeholder value and a comment explaining its purpose. A developer MUST be able to copy `.env.example` to `.env` and fill in only the required values to get a working local stack.

#### Scenario: .env.example covers all variables

- GIVEN `.env.example` is present at the repository root
- WHEN its contents are compared against the environment variable contract
- THEN every variable in the contract table has a corresponding entry

#### Scenario: Developer onboarding from .env.example

- GIVEN a fresh clone with no `.env` file
- WHEN `.env.example` is copied to `.env` and required values are filled
- THEN `docker compose up --build` succeeds and all services start

### Requirement: HTTPS Port Mapping in Docker Compose

The `docker-compose.yml` MUST expose an HTTPS port mapping for the API service (e.g., host port mapped to container HTTPS port). The `.env.example` MUST document the new HTTPS port variable.

#### Scenario: HTTPS port accessible via Docker Compose

- GIVEN `docker-compose.yml` includes an HTTPS port mapping for `aura-api`
- WHEN `docker compose up` is executed
- THEN the API is reachable via HTTPS on the configured host port

#### Scenario: .env.example documents HTTPS port

- GIVEN `.env.example` exists at the repository root
- WHEN its contents are inspected
- THEN an `API_HTTPS_PORT` (or equivalent) variable is listed with a placeholder and comment

---

### Requirement: ACA Health Probes

Expose `GET /health`: 200 healthy, 503 if deps fail.

#### Scenario: Healthy

- GIVEN container running
- WHEN `GET /health`
- THEN 200 `{"status":"Healthy"}`

#### Scenario: Unhealthy

- GIVEN db unreachable
- WHEN `GET /health`
- THEN 503

---

### Requirement: Multi-Arch

Dockerfiles MUST support `buildx` for amd64 and arm64.
(Previously: single-arch)

#### Scenario: Buildx manifest

- GIVEN `buildx build --platform linux/amd64,linux/arm64`
- WHEN manifest inspected
- THEN both platforms present

---

### Requirement: ASPNETCORE_URLS

Add `ASPNETCORE_URLS=http://*:8080` for ACA.
(Previously: no constraint)

#### Scenario: Port

- GIVEN container on ACA
- WHEN port checked
- THEN binds `http://*:8080`
