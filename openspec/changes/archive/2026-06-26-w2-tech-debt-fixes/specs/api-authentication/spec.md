# Delta for API Authentication

## ADDED Requirements

### Requirement: Auth Middleware Integration Tests

The system MUST include integration tests that validate the JWT Bearer authentication middleware enforces access control on protected endpoints. Tests MUST cover the 401 Unauthorized path for unauthenticated requests and the 200 OK path for authenticated requests. Tests SHOULD use the existing mock-login endpoint to obtain a valid token.

#### Scenario: Unauthenticated request returns 401

- GIVEN a protected API endpoint exists
- WHEN a GET request is sent without an Authorization header
- THEN the response status is 401 Unauthorized
- AND the response body does not contain protected resource data

#### Scenario: Authenticated request returns 200

- GIVEN a valid JWT token is obtained from the mock-login endpoint
- WHEN a GET request is sent to a protected endpoint with `Authorization: Bearer {token}`
- THEN the response status is 200 OK
- AND the response body contains the expected resource data

#### Scenario: Invalid token returns 401

- GIVEN a malformed or expired JWT token
- WHEN a GET request is sent to a protected endpoint with that token
- THEN the response status is 401 Unauthorized
