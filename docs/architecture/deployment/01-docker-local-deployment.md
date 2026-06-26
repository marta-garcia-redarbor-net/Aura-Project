# Deployment — Local Docker Deployment

This document defines the local-first Docker deployment shape for Aura.

## Quick path

1. Run `Aura.UI`, `Aura.Api`, and `Aura.Workers` as separate local containers or processes.
2. Use Docker Compose to orchestrate local dependencies and startup.
3. Mount SQLite-backed persistence files into durable local volumes.
4. Configure Entra ID values through environment variables.

## Deployment shape

| Service | Responsibility | Must stay separate |
|---------|----------------|--------------------|
| `aura-ui` | Browser-facing UI host | Yes |
| `aura-api` | HTTP API and SignalR hub | Yes |
| `aura-workers` | Background polling and scheduled processing | Yes |
| `qdrant` | Vector storage dependency | External dependency |

## Docker Compose responsibilities

Compose should own:

- Service startup order and local network wiring.
- Environment-variable injection per host.
- Volume mounts for SQLite persistence.
- Port exposure for UI, API, and Qdrant.

Compose should not redefine Aura as a single merged process.

## Environment variables

| Variable | Used by | Purpose |
|----------|---------|---------|
| `AURA_UI_BASE_URL` | UI | Public local UI base URL |
| `AURA_API_BASE_URL` | UI / API | API base URL used by the UI |
| `ENTRA_TENANT_ID` | UI / API auth config | Entra tenant identifier from the App Registration context |
| `ENTRA_CLIENT_ID` | UI auth config | Aura App Registration client ID |
| `GRAPH_SCOPES` | UI / Graph integration | Delegated Graph scopes requested for the signed-in user |
| `SQLITE_APP_DB_PATH` | API / workers | Path to the main local SQLite application database |
| `SQLITE_TOKEN_CACHE_PATH` | Auth / Graph integration | Path to the persistent SQLite token cache |
| `QDRANT_HOST` | API / workers | Qdrant host name |
| `QDRANT_HTTP_PORT` | API / workers | Qdrant HTTP port |
| `QDRANT_GRPC_PORT` | API / workers | Qdrant gRPC port |

Do not introduce a `ClientSecret` environment requirement for the delegated Graph flow documented for Aura.

## Persistence requirements

| Data | Persistence mechanism | Why it must persist locally |
|------|------------------------|-----------------------------|
| Application state | SQLite file | Keep local state across restarts |
| MSAL token cache | SQLite file | Preserve silent renewal capability across restarts |
| Qdrant storage | Docker volume/bind mount | Preserve vector state locally |

## Authentication and communication rules

- `Aura.UI` authenticates the user with Entra ID.
- `Aura.UI` forwards the bearer token to `Aura.Api` over HTTP.
- SignalR connections also carry the bearer token to the API host.
- `Aura.Api` performs real JWT validation in production-aligned mode.
- Microsoft Graph access uses delegated user tokens.
- `Aura.Workers` reuses delegated token state from the persistent SQLite cache rather than a separate app credential.

## Happy-path local runtime

```text
User starts local stack
  → opens Aura.UI
  → signs in with Entra ID
  → UI receives delegated token with oid
  → UI calls Aura.Api with bearer token
  → UI opens SignalR connection with bearer token
  → API validates JWT and resolves oid
  → Graph features call Microsoft Graph on behalf of the signed-in user
  → SQLite token cache enables silent renewal on next use
```

## Current-repo note

The repository already contains a local `docker-compose.yml` for Qdrant.
The local-first deployment direction for this phase is to keep extending Docker support without collapsing `Aura.UI`, `Aura.Api`, and `Aura.Workers` into one host.

## Checklist

- [x] Docker local deployment is documented as the current scope.
- [x] Separate hosts/processes are explicitly preserved.
- [x] SQLite persistence is part of the deployment contract.
- [x] Entra ID delegated auth configuration is represented through environment variables.

## Next step

- Deployment overview: [`00-overview.md`](./00-overview.md)
- Auth flow and token cache details: [`../auth/02-token-lifecycle.md`](../auth/02-token-lifecycle.md)
