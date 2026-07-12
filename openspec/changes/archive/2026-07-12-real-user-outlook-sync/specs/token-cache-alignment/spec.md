# Delta for Token Cache Alignment

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
