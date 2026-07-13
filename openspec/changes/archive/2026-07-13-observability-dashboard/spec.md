# Delta for Observability Dashboard

## ADDED Requirements

### Requirement: Log Viewer Panel

The `/observability` page MUST render a real-time log table with columns: Level, Timestamp, CorrelationId, Message, Source. Rows MUST be color-coded by log level (e.g. Error=red, Warning=yellow, Information=default, Debug=grey). The table MUST use virtual scrolling to handle high-throughput streams without DOM bloat.

#### Scenario: Page load shows current buffer snapshot

- GIVEN the API has buffered 200 log records in the ring buffer
- WHEN an authenticated user navigates to `/observability`
- THEN the log panel renders the 200 buffered records in reverse-chronological order with correct level colors

#### Scenario: New logs stream in real time

- GIVEN the log panel is open and connected via SignalR
- WHEN the API emits a new `LogRecord` with level `Error`
- THEN the row appears in the table within 1 second, colored red, with Level, Timestamp, CorrelationId, Message, and Source populated

### Requirement: Metrics Display Panel

The page MUST render live gauges for counters: `aura.connector.execution.items`, `aura.graph.token.acquired`, `aura.graph.token.expired`, `aura.graph.http.error`. Gauges MUST update on each SignalR push with the latest snapshot value.

#### Scenario: Page load shows latest metric snapshot

- GIVEN the metrics ring buffer holds 50 snapshots
- WHEN the page loads
- THEN each gauge displays the most recent counter value from the buffer

#### Scenario: Counter increment pushes update

- GIVEN the metrics panel is connected
- WHEN `aura.connector.execution.items` increments by 3
- THEN the gauge updates to the new value within 1 second

### Requirement: Trace Timeline Panel

The page MUST display recent spans with: operation name, duration (ms), start time, and status (Healthy/Unhealthy). Expanding a span MUST reveal its tags. Spans MUST be ordered by start time descending.

#### Scenario: Completed span appears in timeline

- GIVEN an `Activity` with `OperationName = "Graph.GetMessages"` starts and stops with duration 120 ms and status `Ok`
- WHEN the span is captured by the `ActivityListener`
- THEN the trace panel shows a row with operation name, 120 ms duration, start time, and status Healthy

#### Scenario: Span expand reveals tags

- GIVEN a span row is visible in the trace panel
- WHEN the user expands the row
- THEN the span's tags (key-value pairs) are displayed

### Requirement: Ring Buffer Contracts

`TelemetryBuffer<T>` MUST be bounded with fixed capacity: 1000 log records, 500 spans, 100 metric snapshots. The buffer MUST be thread-safe for concurrent producers and consumers. Producers MUST NOT block — when full, the oldest entry is evicted (ring semantics).

#### Scenario: Buffer evicts oldest on overflow

- GIVEN a log buffer at capacity (1000 entries)
- WHEN a new log record is written
- THEN the buffer contains 1000 entries and the oldest entry is replaced

#### Scenario: Concurrent producers do not block

- GIVEN 10 threads writing to the same buffer simultaneously
- WHEN each thread writes 100 entries
- THEN all 1000 writes complete without blocking and the buffer contains the last 1000 entries

### Requirement: SignalR TelemetryHub Contract

`TelemetryHub` MUST expose streaming methods: `StreamLogs`, `StreamMetrics`, `StreamTraces`. The hub MUST push buffer snapshots on a timer (≤1 s interval) and on new data arrival. The UI `TelemetryClient` MUST auto-reconnect with exponential backoff and re-subscribe all streams on reconnect.

#### Scenario: Client receives streamed data

- GIVEN a client connected to `TelemetryHub`
- WHEN the hub timer fires
- THEN the client receives a batch of log, metric, and trace data within 1 second

#### Scenario: Disconnect and auto-reconnect

- GIVEN the SignalR connection drops
- WHEN the `TelemetryClient` detects the disconnect
- THEN it reconnects with exponential backoff and re-subscribes to all three streams, resuming data flow

### Requirement: Auth Reuse

The `/observability` route MUST be protected by the existing Blazor `AuthorizeView` pattern. No new auth logic, roles, or policies are introduced.

#### Scenario: Unauthenticated user is redirected

- GIVEN an unauthenticated user navigates to `/observability`
- WHEN the page loads
- THEN the user is redirected to the login flow by the existing auth middleware

#### Scenario: Authenticated user accesses page

- GIVEN an authenticated user navigates to `/observability`
- WHEN the page loads
- THEN all three panels render and SignalR connections are established

## MODIFIED Requirements

### Requirement: Dashboard Error Correlation

The API MUST expose `GET /api/dashboard/recent-errors` returning the last N errors with correlation ID, timestamp, and message. The dashboard panel MUST display these errors with their correlation ID for troubleshooting. Errors logged MUST ALSO be captured by the in-process log ring buffer for real-time streaming to the observability page.

(Previously: errors were only available via the REST endpoint; now they also feed the real-time log buffer.)

#### Scenario: Recent errors returned with correlation

- GIVEN errors have been logged with correlation IDs
- WHEN the dashboard error endpoint is called
- THEN a list of recent errors is returned, each with correlation ID, timestamp, and message

#### Scenario: No errors returns empty list

- GIVEN no errors have occurred recently
- WHEN the dashboard error endpoint is called
- THEN HTTP 200 is returned with an empty list

#### Scenario: Error appears in both endpoint and log stream

- GIVEN the observability page is open and streaming logs
- WHEN an error is logged with a correlation ID
- THEN the error appears in the real-time log panel AND is available via `/api/dashboard/recent-errors`
