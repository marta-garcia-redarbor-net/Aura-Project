# Delta for Connector Execution

## MODIFIED Requirements

### Requirement: Telemetry Emission

The use case MUST open an `ILogger.BeginScope("{CorrelationId}", id)` before invoking the connector adapter, so all telemetry within the cycle inherits the same correlation ID. Telemetry MUST additionally include:
- Structured log for `MsalUiRequiredException` with oid correlation
- Structured log for Graph HTTP failures including status code, endpoint URL, and connector name
- Metric `graph.token.acquired` emitted on successful token acquisition
- Metric `graph.token.expired` emitted when `MsalUiRequiredException` is caught
- Metric `graph.http.error` emitted on 4xx/5xx Graph responses, tagged by status code

(Previously: correlation ID was shared across telemetry but not set via `BeginScope` before execution)

#### Scenario: Successful run emits correlated telemetry

- GIVEN execution completes
- WHEN telemetry is inspected
- THEN trace span, item-count metric, and log entry share one correlation identifier from the ambient `BeginScope`

#### Scenario: Failed run emits error-level telemetry

- GIVEN execution fails
- WHEN telemetry is inspected
- THEN a trace span, metric (count = 0), and error-level log share the same correlation identifier

#### Scenario: MsalUiRequiredException emits re-auth telemetry

- GIVEN a Graph call fails with `MsalUiRequiredException` for user oid "abc-123"
- WHEN telemetry is inspected
- THEN a structured log entry with level Warning is emitted
- AND the log contains oid = "abc-123" and connector name
- AND metric `graph.token.expired` is incremented

#### Scenario: Graph HTTP 4xx emits error telemetry

- GIVEN a Graph API call returns HTTP 403 Forbidden
- WHEN telemetry is inspected
- THEN a structured log entry with level Warning is emitted
- AND the log contains status code = 403, endpoint URL, and connector name
- AND metric `graph.http.error` is incremented with status_code = 403

#### Scenario: Graph HTTP 5xx emits error telemetry

- GIVEN a Graph API call returns HTTP 503 Service Unavailable
- WHEN telemetry is inspected
- THEN a structured log entry with level Error is emitted
- AND the log contains status code = 503, endpoint URL, and connector name
- AND metric `graph.http.error` is incremented with status_code = 503
