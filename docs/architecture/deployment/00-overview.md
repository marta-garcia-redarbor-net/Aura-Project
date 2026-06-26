# Deployment — Overview

Aura is designed with production-aligned boundaries, but the current delivery scope is local Docker-based deployment first.

## Quick path

1. Run each Aura host as a separate process or container.
2. Keep UI, API, and workers separated.
3. Persist SQLite files with local volumes.
4. Use Docker Compose to orchestrate local dependencies and host startup.

## Core decisions

| Topic | Decision |
|-------|----------|
| Current deployment target | Local Docker-based deployment |
| Host topology | Separate `Aura.UI`, `Aura.Api`, and `Aura.Workers` hosts |
| UI/API communication | HTTP for request/response, SignalR for real-time updates |
| Authentication transport | Bearer token forwarding from UI to API |
| API authentication | Real JWT validation in production-aligned behavior |
| Local persistence | SQLite for application persistence and token cache state |

## Required host separation

| Host | Role | Notes |
|------|------|-------|
| `Aura.UI` | Interactive user host | Own process and own port |
| `Aura.Api` | HTTP API + SignalR host | Own process and own port |
| `Aura.Workers` | Background jobs and polling | Own process; no UI responsibilities |

Aura must not collapse these three hosts into a single process in the documentation or deployment model.

## Happy-path local topology

```text
Browser
  → Aura.UI
      → HTTP + bearer token forwarding → Aura.Api
      → SignalR + bearer token forwarding → Aura.Api hub

Aura.Workers
  → background polling / scheduled work
  → same persistence boundary

Shared local dependencies
  → SQLite files
  → Qdrant container
```

## Expected local ports

| Service | Expected local role | Notes |
|---------|---------------------|-------|
| `Aura.UI` | UI host port | Keep separate from API |
| `Aura.Api` | API/SignalR port | Current docs and code commonly reference `http://localhost:5180` |
| Qdrant HTTP | Vector store HTTP port | `6333` by default |
| Qdrant gRPC | Vector store gRPC port | `6334` by default |

If local defaults change later, preserve the separation rule.

## What exists now vs. deployment target

| Area | Current repo state | Documented target for this phase |
|------|--------------------|----------------------------------|
| Docker Compose | Repository already includes local Qdrant compose wiring | Extend local compose around separate Aura hosts, not a merged host |
| Authentication architecture | Production-aligned delegated design is defined | Deliver locally first |
| Persistence | SQLite is already part of the architecture | Persist SQLite files in local mounted storage |

## Checklist

- [x] Local Docker is documented as the current target.
- [x] UI, API, and workers are documented as separate hosts.
- [x] HTTP/SignalR bearer token forwarding is documented.
- [x] SQLite persistence is documented.

## Next step

- Local deployment details: [`01-docker-local-deployment.md`](./01-docker-local-deployment.md)
- Authentication architecture: [`../auth/00-overview.md`](../auth/00-overview.md)
