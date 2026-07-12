# Delta for Connector Execution

## MODIFIED Requirements

### Requirement: Worker Oid Resolution

Workers MUST resolve the user oid from the persisted token cache before invoking the
connector use case. If the current session is demo-mode, the worker MUST skip connector
execution without attempting any Graph token acquisition; this MUST NOT be logged as an
error or warning. If no account is cached (user has never logged in via the API), the
worker MUST log a warning and skip that connector execution.
(Previously: No demo-mode gate; only handled the "no cached user" case via warning log.)

#### Scenario: Worker resolves oid from token cache

- GIVEN a user account with oid "oid-A" exists in the SQLite token cache
- WHEN the worker prepares to execute a connector for that user
- THEN `CheckpointIdentity.UserOid` is set to "oid-A"

#### Scenario: Worker skips connector when no cached user

- GIVEN the token cache is empty (no accounts)
- WHEN the worker prepares to execute a connector
- THEN a warning log is emitted
- AND the connector execution is skipped
- AND no Graph call is attempted

#### Scenario: Worker propagates oid to all three providers

- GIVEN a connector identity with `UserOid` = "oid-A"
- WHEN Teams, Outlook, or Calendar providers invoke `IGraphClientFactory.CreateClientAsync`
- THEN each receives `oid = "oid-A"` as the first parameter

#### Scenario: Worker skips connector for demo session (no error emitted)

- GIVEN the current session is identified as demo-mode
- WHEN the worker evaluates whether to invoke connector execution
- THEN the connector execution is skipped
- AND no Graph token acquisition is attempted
- AND no error or warning log is emitted for the skip

---

## ADDED Requirements

### Requirement: API Sync Trigger Identity Propagation

The sync endpoint MUST extract the authenticated user's `oid` from the HTTP request
identity claims and supply it as `CheckpointIdentity.UserOid` to the connector execution
use case. The endpoint MUST NOT use `FirstOrDefault()` or any unpartitioned account
enumeration to determine the target identity. If the request does not carry an
authenticated `oid` claim, the endpoint MUST return a 401 or a typed failure result
without invoking the connector use case.

#### Scenario: Real user oid extracted from request claims

- GIVEN a real Entra ID user is authenticated with oid "oid-real-1"
- WHEN the sync endpoint receives the request
- THEN `CheckpointIdentity.UserOid` is set to "oid-real-1"
- AND the connector use case is invoked with that identity

#### Scenario: Missing oid claim returns authentication failure

- GIVEN the request identity does not contain an `oid` claim
- WHEN the sync endpoint processes the request
- THEN a 401 Unauthorized or typed failure result is returned
- AND the connector execution use case is NOT invoked

#### Scenario: Demo session is blocked before reaching use case

- GIVEN the current session is demo-mode (no real Entra oid)
- WHEN the sync endpoint receives the request
- THEN the request is blocked before use case invocation
- AND no real Graph pipeline is triggered
