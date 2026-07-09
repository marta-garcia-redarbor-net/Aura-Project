# Delta for API Authentication

## MODIFIED Requirements

### Requirement: Mock Login Generation

The system MUST provide an endpoint to generate a local, symmetric JWT for development and demo purposes. The endpoint MUST be available in all environments. The issued JWT MUST include a `role=Demo` claim to identify demo users.
(Previously: Endpoint was only available in development environments and issued basic mock claims without a role claim.)

#### Scenario: Successful Mock Login

- GIVEN the application is running in any environment
- WHEN a client sends a POST request to `/api/auth/mock-login`
- THEN the API returns a valid JWT token containing mock user claims with `role=Demo`

#### Scenario: Mock login in production environment

- GIVEN the application is running in a production environment
- WHEN a client sends a POST request to `/api/auth/mock-login`
- THEN the API returns a valid JWT token with `role=Demo` claims
- AND the token does NOT grant access to production-only data

### Requirement: API Authorization Enforcement

The system MUST enforce authentication on protected endpoints using dual JWT Bearer token validation. The system MUST accept both Entra ID-issued JWTs (primary scheme) and mock symmetric JWTs (secondary scheme) simultaneously. The Entra ID scheme MUST be the default authentication scheme.
(Previously: Single JWT Bearer validation without dual-scheme support.)

#### Scenario: Access without token

- GIVEN a protected API endpoint
- WHEN a client sends a request without an Authorization header
- THEN the API rejects the request with a 401 Unauthorized status

#### Scenario: Access with valid Entra ID token

- GIVEN a valid JWT token issued by Entra ID
- WHEN a client sends a request to a protected API endpoint with the token as a Bearer token
- THEN the API accepts the request and processes it successfully

#### Scenario: Access with valid mock token

- GIVEN a valid mock JWT token obtained from the mock login endpoint
- WHEN a client sends a request to a protected API endpoint with the token as a Bearer token
- THEN the API accepts the request and processes it successfully

#### Scenario: Access with invalid token returns 401

- GIVEN a malformed or expired JWT token from any issuer
- WHEN a client sends a request to a protected API endpoint with that token
- THEN the API rejects the request with a 401 Unauthorized status

## ADDED Requirements

### Requirement: Dual JWT Bearer Schemes

The system MUST register two JWT Bearer authentication schemes simultaneously: an Entra ID scheme (primary) validating tokens against the Entra ID tenant, and a MockJwt scheme (secondary) validating tokens using a symmetric key. The Entra ID scheme MUST be the default authentication scheme.

#### Scenario: Entra ID token validated by primary scheme

- GIVEN both JWT Bearer schemes are registered
- WHEN a request presents an Entra ID-issued JWT
- THEN the primary scheme validates the token against the Entra ID tenant
- AND the request is authorized

#### Scenario: Mock JWT validated by secondary scheme

- GIVEN both JWT Bearer schemes are registered
- WHEN a request presents a mock symmetric JWT
- THEN the secondary scheme validates the token using the symmetric key
- AND the request is authorized

### Requirement: Demo JWT Authorization Policies

The system MUST enforce authorization policies that restrict demo JWT holders (role=Demo) to demo-only data and read-only operations. Endpoints serving production-only data MUST reject requests carrying a demo JWT with a 403 Forbidden status.

#### Scenario: Demo JWT accesses demo-allowed endpoint

- GIVEN a valid mock JWT with role=Demo
- WHEN a client sends a request to a demo-allowed endpoint
- THEN the API accepts the request and returns demo data

#### Scenario: Demo JWT rejected from production-only endpoint

- GIVEN a valid mock JWT with role=Demo
- WHEN a client sends a request to a production-only endpoint
- THEN the API rejects the request with a 403 Forbidden status
