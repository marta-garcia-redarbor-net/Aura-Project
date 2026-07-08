# Delta for Dashboard System Status

## ADDED Requirements

### Requirement: Recent Errors Endpoint

The API MUST expose `GET /api/dashboard/recent-errors` returning the last N errors. Each error MUST include a correlation ID, timestamp, and message. The endpoint MUST be read-only and scoped to the same authorization group as the system-status endpoint.

#### Scenario: Recent errors returned with correlation ID

- GIVEN errors have been logged with correlation IDs
- WHEN a client sends GET to the recent-errors endpoint
- THEN the response is HTTP 200 with a list of errors, each containing correlation ID, timestamp, and message

#### Scenario: No errors returns empty list

- GIVEN no errors have been logged recently
- WHEN a client sends GET to the recent-errors endpoint
- THEN the response is HTTP 200 with an empty array

#### Scenario: Write verbs rejected

- GIVEN the recent-errors endpoint is available
- WHEN a client sends POST, PUT, PATCH, or DELETE
- THEN the response is HTTP 405 Method Not Allowed

### Requirement: Error Correlation in UI

The dashboard system-status panel MUST display the most recent errors alongside the existing readiness indicators. Each displayed error MUST show its correlation ID as a clickable or copyable reference for log lookup.

#### Scenario: Panel renders errors with correlation IDs

- GIVEN the recent-errors endpoint returns 3 errors
- WHEN the status panel loads
- THEN each error is rendered with its correlation ID, timestamp, and message
- AND the existing readiness indicators remain unchanged
