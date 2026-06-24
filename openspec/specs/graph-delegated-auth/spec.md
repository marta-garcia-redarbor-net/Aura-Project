# Graph Delegated Auth Specification

## Purpose

Defines the delegated-first Microsoft Graph authentication flow, ensuring that users can grant incremental consent via UI login, and background workers can reliably reuse cached tokens to fetch real-world data without user intervention.

## Requirements

### Requirement: UI Authentication and Consent

The system MUST provide a UI mechanism to initiate Entra ID login. This flow MUST obtain delegated tokens with the required scopes for reading Teams and Outlook data, supporting incremental consent.

#### Scenario: User logs in successfully

- GIVEN a user accesses the authentication UI
- WHEN they initiate the login flow and grant consent
- THEN a valid delegated token is issued for the requested scopes

### Requirement: SQLite Token Caching

Obtained delegated tokens MUST be securely cached in a SQLite store. The background worker MUST retrieve and use these cached tokens to authenticate Microsoft Graph API requests without requiring an interactive session.

#### Scenario: Worker reuses cached token

- GIVEN a valid token is stored in the SQLite cache
- WHEN the background worker initiates an ingestion sync
- THEN the worker successfully authenticates with Graph API using the cached token

### Requirement: Re-authentication Visibility

If the cached token expires, becomes invalid, or lacks required scopes, the UI MUST surface an explicit indication that re-authentication is required. The system MUST NOT fail silently or fall back to mocked data.

#### Scenario: Token is expired or invalid

- GIVEN the SQLite token cache contains an expired or invalid token
- WHEN the system attempts to sync data
- THEN the UI displays an explicit message indicating that the user needs to re-authenticate
