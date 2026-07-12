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

---

## ADDED Requirements

### Requirement: Oid-Partitioned Cache Access

The system MUST key all token cache read and write operations by the authenticated user's
`oid`. Cache lookups MUST NOT use `FirstOrDefault()` or any unpartitioned enumeration
over cached accounts. Each `oid` MUST access an isolated subset of the token cache that
cannot be read or overwritten via a different `oid`.

#### Scenario: Oid-keyed lookup returns the correct account

- GIVEN two accounts with oids "oid-A" and "oid-B" exist in the token cache
- WHEN a cache lookup is performed for "oid-B"
- THEN only the account matching "oid-B" is returned
- AND the account for "oid-A" is not a candidate

#### Scenario: Cache miss for unknown oid returns no account (no fallback)

- GIVEN the token cache contains accounts for "oid-A" and "oid-B"
- WHEN a cache lookup is performed for "oid-unknown"
- THEN no account is returned
- AND no fallback to "oid-A" or "oid-B" occurs

#### Scenario: Cache write is scoped to the authenticated oid

- GIVEN a real user with oid "oid-A" acquires a Graph token
- WHEN the token is persisted to the cache
- THEN the entry is keyed to "oid-A"
- AND a subsequent lookup for "oid-B" does not return this entry

---

### Requirement: Demo-Real Cache Isolation

Demo-mode sessions MUST NOT share, read, or overwrite token cache entries belonging to
real authenticated users. Real-user cache lookups MUST NOT return demo accounts. The
system MUST maintain distinct cache partitions for demo and real identity flows.

#### Scenario: Real-user lookup does not return demo account

- GIVEN a demo session has produced a mock or stub account in the auth layer
- WHEN a real-user pipeline performs a cache lookup for oid "oid-real-1"
- THEN the demo account is not returned
- AND only real accounts matching "oid-real-1" are candidates

#### Scenario: Concurrent demo and real sessions use isolated cache partitions

- GIVEN a demo session and a real session for "oid-real-1" are both active
- WHEN both sessions access the token cache concurrently
- THEN each session reads only its own partition
- AND neither session's cache write affects the other's partition

#### Scenario: Demo session writes no entry into the real-user partition

- GIVEN a demo-mode user completes mock authentication
- WHEN the demo session performs any auth-related cache interaction
- THEN no token cache entry is written into the real-user partition
