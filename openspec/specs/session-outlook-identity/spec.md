# Session Outlook Identity Specification

## Purpose

End-to-end identity propagation contract: a real authenticated Entra ID user syncs only
their own Outlook unread mail, and demo-mode logins are excluded from the real Graph
pipeline at every boundary (API endpoint, worker, token acquisition).

## Requirements

### Requirement: Real-User Sync Identity Contract

The system MUST propagate the authenticated user's `oid` claim from the API request
identity through the worker boundary into Graph token acquisition. Outlook sync MUST
access only the mailbox bound to that `oid`. The system MUST NOT infer or select an
account that does not match the authenticated `oid`; when no matching account exists,
the result MUST be a typed failure, not a fallback to another user's mailbox.

#### Scenario: Oid flows end-to-end for real user

- GIVEN a real Entra ID user is authenticated with oid "oid-real-1"
- WHEN the sync pipeline executes
- THEN Graph token acquisition uses "oid-real-1" for account selection
- AND only mail items belonging to "oid-real-1" are returned

#### Scenario: No matching cached account returns typed failure

- GIVEN the authenticated oid "oid-real-1" has no matching cached MSAL account
- WHEN the sync pipeline executes
- THEN the result is a typed failure with a non-empty reason
- AND no mail items are returned from any other account

#### Scenario: Two real users are isolated

- GIVEN users "oid-A" and "oid-B" both have cached MSAL accounts
- WHEN each triggers sync independently
- THEN "oid-A"'s pipeline returns only "oid-A"'s mail
- AND "oid-B"'s pipeline returns only "oid-B"'s mail

---

### Requirement: Demo Session Exclusion from Real Graph Pipeline

The system MUST identify demo-mode sessions at the worker and API boundary before any
Graph interaction. A demo-mode session MUST NOT trigger real Graph token acquisition or
any real Graph HTTP call. The pipeline MUST produce a result indicating demo mode is
active, not a sync error.

#### Scenario: Demo login triggers no Graph call

- GIVEN the current session is identified as demo-mode
- WHEN the sync worker evaluates the connector pipeline
- THEN no Graph token acquisition is attempted
- AND no Graph HTTP call is made
- AND the result indicates demo mode is active (not a sync failure)

#### Scenario: Demo and real sessions coexist without contamination

- GIVEN a demo session and a real session "oid-real-1" are active concurrently
- WHEN each session's sync pipeline executes
- THEN the demo session produces no Graph interaction
- AND the real session produces Graph output scoped to "oid-real-1" only

#### Scenario: Demo session is unambiguously identified

- GIVEN a demo-mode authentication has completed
- WHEN the worker checks session type
- THEN the session is identified as demo
- AND the real Graph pipeline gate is not entered

---

### Requirement: Cross-Session Account Isolation

Concurrent real and demo sessions MUST NOT share or cross-resolve cached MSAL accounts
or tokens. Account selection MUST be exclusive per authenticated `oid`. The system MUST
NOT use `FirstOrDefault()` or unpartitioned enumeration at any layer in the pipeline.

#### Scenario: Concurrent real users do not cross-resolve accounts

- GIVEN "oid-A" and "oid-B" both have MSAL token cache entries
- WHEN both sync pipelines execute concurrently
- THEN "oid-A"'s pipeline selects only the account matching "oid-A"
- AND "oid-B"'s pipeline selects only the account matching "oid-B"

#### Scenario: Demo account is invisible to real-user account lookup

- GIVEN a demo session has a mock account in the auth layer
- WHEN a real-user pipeline looks up the cached account by oid "oid-real-1"
- THEN the demo account is not returned as a candidate
- AND only the real account matching "oid-real-1" is considered
