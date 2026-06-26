# Docker Local Deployment Specification

## Purpose

Docker Compose orchestration for local development: separate containers for Aura.UI, Aura.Api, and Aura.Workers, preserving architectural host separation.

## Requirements

### Requirement: Compose Service Definition

The system SHALL define three Compose services — `aura-ui`, `aura-api`, `aura-workers` — each built from a dedicated multi-stage Dockerfile targeting `net9.0`. Services MUST NOT be collapsed into a single container.

#### Scenario: All three services start with `docker compose up`

- GIVEN `docker-compose.yml` and all Dockerfiles are present
- WHEN `docker compose up --build` is executed
- THEN all three containers reach a running state without error
- AND `aura-qdrant` remains a running dependency

#### Scenario: Service separation is preserved

- GIVEN `docker compose up` completes
- WHEN `docker compose ps` is inspected
- THEN `aura-ui`, `aura-api`, `aura-workers`, and `aura-qdrant` are listed as separate services
- AND no service is in a restart loop

### Requirement: Port Mapping

The system SHALL expose fixed host ports: UI on `5180`, API on `5190`. Ports MUST be configurable via environment variables but MUST have hardcoded defaults.

#### Scenario: UI is accessible on port 5180

- GIVEN all containers are running
- WHEN a browser navigates to `http://localhost:5180`
- THEN the Aura.UI Blazor Server page loads without connection error

#### Scenario: API is accessible on port 5190

- GIVEN all containers are running
- WHEN `curl http://localhost:5190/health` is executed from the host
- THEN an HTTP 200 response is returned

#### Scenario: Ports are configurable via env vars

- GIVEN `UI_PORT=8080` and `API_PORT=8081` are set in `.env`
- WHEN `docker compose up --build` is executed
- THEN UI is reachable on `http://localhost:8080` and API on `http://localhost:8081`

### Requirement: Service Startup Ordering

The system SHALL use Compose `depends_on` with health-check conditions so that `aura-api` starts before `aura-ui`, and `aura-workers` starts after `aura-api`.

#### Scenario: API starts before UI

- GIVEN `docker compose up` is executed
- WHEN the startup logs are inspected
- THEN `aura-api` logs a listening message before `aura-ui` attempts connection

#### Scenario: Workers start after API

- GIVEN `docker compose up` is executed
- WHEN `aura-workers` container starts
- THEN `aura-api` is already in a healthy/running state

### Requirement: Network Isolation

The system SHALL place all Aura services on a dedicated Compose network. Inter-service communication MUST use Docker DNS names (`aura-api`, `aura-ui`, `aura-workers`), not `localhost`.

#### Scenario: UI reaches API via Docker DNS

- GIVEN all containers are running on the Compose network
- WHEN `aura-ui` makes an HTTP call to `http://aura-api:5190`
- THEN the request succeeds and reaches `aura-api`

#### Scenario: Workers reach API via Docker DNS

- GIVEN all containers are running on the Compose network
- WHEN `aura-workers` makes an HTTP call to `http://aura-api:5190`
- THEN the request succeeds and reaches `aura-api`

### Requirement: Multi-Stage Dockerfiles

Each host Dockerfile MUST use multi-stage builds: a `build` stage with the .NET SDK and a `runtime` stage with the ASP.NET Core or .NET runtime image. This reduces final image size.

#### Scenario: Runtime stage uses slim base image

- GIVEN a Dockerfile for any Aura host
- WHEN the final stage is inspected
- THEN it uses `mcr.microsoft.com/dotnet/aspnet:9.0` or `mcr.microsoft.com/dotnet/runtime:9.0` (NOT the `-sdk` image)

#### Scenario: Build stage compiles the application

- GIVEN a Dockerfile for Aura.Api
- WHEN `docker build` is executed
- THEN the build stage restores, builds, and publishes the application
- AND the runtime stage copies only the published output

### Requirement: .dockerignore

The system SHALL include a `.dockerignore` at the repository root that excludes `tests/`, `docs/`, `openspec/`, `.git/`, `**/bin/`, `**/obj/`, `*.md`, and `.env` from the build context.

#### Scenario: Build context excludes tests

- GIVEN `.dockerignore` exists at the repository root
- WHEN `docker build` is executed for any host
- THEN the `tests/` directory is NOT included in the build context

#### Scenario: Build context excludes secrets

- GIVEN `.dockerignore` exists at the repository root
- WHEN `docker build` is executed
- THEN `.env` is NOT included in the build context

### Requirement: Smoke Test

The system SHALL include a smoke test script that verifies: (1) all containers are running, (2) UI responds on its port, (3) API responds on its port, (4) Workers container logs show no crash-loop errors.

#### Scenario: Smoke test passes on clean start

- GIVEN `docker compose up --build -d` has completed
- WHEN the smoke test script is executed
- THEN it exits with code 0 and reports all services healthy

#### Scenario: Smoke test detects API failure

- GIVEN `aura-api` container is stopped manually
- WHEN the smoke test script is executed
- THEN it exits with non-zero code and reports `aura-api` as unhealthy
