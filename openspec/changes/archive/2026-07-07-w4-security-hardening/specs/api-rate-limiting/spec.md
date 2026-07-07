# API Rate Limiting Specification

## Purpose

Per-endpoint-group rate limiting to protect the Aura API from abuse and denial-of-service.

## Requirements

### Requirement: Default Rate Limit Policy

The system MUST enforce a default rate limit of 100 requests per minute per client IP for all API endpoints.

#### Scenario: Request within limit succeeds

- GIVEN a client IP has made fewer than 100 requests in the current minute window
- WHEN the client sends a request to any API endpoint
- THEN the request is processed normally
- AND the response includes a `RateLimit-Remaining` header

#### Scenario: Rate limit exceeded returns 429

- GIVEN a client IP has exhausted its 100-request quota for the current minute
- WHEN the client sends another request
- THEN the system responds with HTTP 429 Too Many Requests
- AND the response includes a `Retry-After` header with seconds until the window resets

### Requirement: Strict Rate Limit for Critical Endpoints

The system MUST enforce a stricter rate limit of 10 requests per minute per client IP for authentication-related endpoints (e.g., token exchange, demo login).

#### Scenario: Auth endpoint within strict limit

- GIVEN a client IP has made fewer than 10 requests to an auth endpoint in the current minute
- WHEN the client sends a request to `/auth/token`
- THEN the request is processed normally

#### Scenario: Auth endpoint exceeds strict limit

- GIVEN a client IP has exhausted its 10-request quota on auth endpoints
- WHEN the client sends another request to `/auth/token`
- THEN the system responds with HTTP 429 Too Many Requests

### Requirement: Configurable Rate Limit Policies

The system SHALL support named rate-limit policies that can be applied to endpoint groups. Policy limits (request count and window duration) MUST be configurable via `appsettings.json`.

#### Scenario: Custom policy applied to endpoint group

- GIVEN a policy named "demo" is configured with 20 requests per 5 minutes
- WHEN a client sends requests to endpoints tagged with the "demo" policy
- THEN the 20-requests-per-5-minutes limit is enforced instead of the default
