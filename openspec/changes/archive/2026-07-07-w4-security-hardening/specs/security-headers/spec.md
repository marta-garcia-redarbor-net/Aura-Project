# Security Headers Specification

## Purpose

HTTP response headers that mitigate XSS, clickjacking, MIME-sniffing, and protocol-downgrade attacks.

## Requirements

### Requirement: X-Content-Type-Options Header

Every HTTP response from the API MUST include `X-Content-Type-Options: nosniff`.

#### Scenario: Header present on all responses

- GIVEN the API is running
- WHEN any HTTP response is returned (200, 400, 404, 500)
- THEN the response includes the header `X-Content-Type-Options: nosniff`

### Requirement: X-Frame-Options Header

Every HTTP response from the API MUST include `X-Frame-Options: DENY` to prevent clickjacking.

#### Scenario: Header present on all responses

- GIVEN the API is running
- WHEN any HTTP response is returned
- THEN the response includes the header `X-Frame-Options: DENY`

### Requirement: Content-Security-Policy Header

Every HTTP response from the API MUST include a `Content-Security-Policy` header with a restrictive policy.

#### Scenario: CSP header present with safe defaults

- GIVEN the API is running
- WHEN any HTTP response is returned
- THEN the response includes a `Content-Security-Policy` header
- AND the policy includes at minimum `default-src 'self'`

### Requirement: Strict-Transport-Security in Production

In non-Development environments, every HTTP response MUST include `Strict-Transport-Security` with `max-age` of at least 31536000 and `includeSubDomains`.

#### Scenario: HSTS present in production

- GIVEN `ASPNETCORE_ENVIRONMENT` is `Production`
- WHEN any HTTP response is returned
- THEN the response includes `Strict-Transport-Security: max-age=31536000; includeSubDomains`

#### Scenario: HSTS absent in development

- GIVEN `ASPNETCORE_ENVIRONMENT` is `Development`
- WHEN any HTTP response is returned
- THEN the response does NOT include a `Strict-Transport-Security` header
