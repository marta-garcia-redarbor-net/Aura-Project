# Token Cache Alignment — Delta Spec

## Purpose

Ensure the token cache path used by the API and Worker services is consistent in Docker by aligning both to a shared volume at `/data/tokens/`, preventing token cache mismatches that cause re-authentication loops.

## MODIFIED Requirements

### Requirement: Token Cache Path Consistency

The API and Worker services MUST use the same token cache path when running in Docker. The default relative path `"Data Source=token_cache.db"` MUST be overridden to `/data/tokens/cache.db` in containerized environments. The Docker volume mount (`./data:/data`) MUST persist the token cache across container restarts.

#### Scenario: Docker containers share token cache

- GIVEN both API and Worker containers are started with the Docker volume `./data:/data`
- WHEN the API acquires a Graph token and caches it
- THEN the Worker can read the same cached token without re-authentication
- AND the token cache file exists at `/data/tokens/cache.db` inside both containers

#### Scenario: Token cache persists across restarts

- GIVEN a token has been cached in `/data/tokens/cache.db`
- WHEN the API container is restarted
- THEN the cached token is still present and usable
- AND no re-authentication is triggered for the same user

#### Scenario: Relative path override in Docker

- GIVEN the default config specifies `Data Source=token_cache.db` (relative)
- WHEN the application starts in Docker with the `ConnectionStrings__TokenCache` environment variable
- THEN the environment variable value (`/data/tokens/cache.db`) takes precedence
- AND the relative path is not used

---

## ADDED Requirements

### Requirement: Token Cache Directory Initialization

The system MUST create the `/data/tokens/` directory if it does not exist at startup. The directory creation MUST be idempotent — repeated calls MUST NOT fail or duplicate.

#### Scenario: Directory created on first start

- GIVEN the `/data/tokens/` directory does not exist
- WHEN the application starts
- THEN the directory is created
- AND the token cache is initialized at `/data/tokens/cache.db`

#### Scenario: Directory already exists

- GIVEN the `/data/tokens/` directory already exists with a valid cache
- WHEN the application starts
- THEN no error occurs
- AND the existing cache is used without modification

#### Scenario: Directory creation failure logs warning

- GIVEN the application cannot create `/data/tokens/` (e.g., permissions)
- WHEN the application starts
- THEN a warning is logged indicating the token cache path is unavailable
- AND the application continues with a fallback in-memory cache or degraded state
