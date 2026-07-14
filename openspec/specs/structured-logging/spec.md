# Structured Logging Specification

## Purpose

Provide correlation ID propagation across API requests and worker execution cycles so every structured log entry carries a `CorrelationId` for debugging and troubleshooting. All log calls use source-generated `[LoggerMessage]` for performance and consistency.

## Requirements

### Requirement: API Correlation Middleware

The API MUST read the `X-Correlation-Id` request header or generate a new GUID if absent. It MUST set `HttpContext.TraceIdentifier` to that value, open an `ILogger.BeginScope("{CorrelationId}", id)` for the request lifetime, and include `X-Correlation-Id` in every response. Entry and exit MUST be logged with method, path, status code, and duration.

#### Scenario: Existing header is forwarded

- GIVEN a request with `X-Correlation-Id: abc-123`
- WHEN the middleware processes the request
- THEN `HttpContext.TraceIdentifier` equals `abc-123` and the response includes `X-Correlation-Id: abc-123`

#### Scenario: Missing header generates new ID

- GIVEN a request without `X-Correlation-Id`
- WHEN the middleware processes the request
- THEN a new GUID is generated and the response includes it as `X-Correlation-Id`

#### Scenario: BeginScope enriches all downstream logs

- GIVEN a request is being processed
- WHEN any endpoint logs within that request
- THEN every log entry contains a `CorrelationId` field from the ambient scope

#### Scenario: Entry and exit logged with duration

- GIVEN a request completes with status 200
- WHEN the middleware logs completion
- THEN the log contains method, path, status code, and elapsed time

### Requirement: Worker Correlation Scope

Worker execution cycles MUST generate a correlation ID per cycle and open an `ILogger.BeginScope("{CorrelationId}", id)` before executing the use case. `ConnectorExecutionWorker` and `SemanticIndexSyncWorker` MUST both follow this pattern.

#### Scenario: Worker cycle creates scope

- GIVEN a worker starts a new execution cycle
- WHEN the cycle begins
- THEN a GUID correlation ID is generated and a BeginScope is opened

#### Scenario: All worker logs carry correlation ID

- GIVEN a worker cycle is executing
- WHEN any log call is emitted
- THEN the log entry includes the cycle's `CorrelationId`

### Requirement: LoggerMessage Migration

All `_logger.Log*()` calls MUST be replaced with `[LoggerMessage]` source-generated partial methods. No behavioral change — output MUST be identical in log level, message template, and parameters. The migration covers `SemanticIndexSyncWorker.cs`, `HelloKernelWorker.cs`, `Worker.cs`, and `PluginRegistry.cs`.

#### Scenario: Migrated calls emit identical output

- GIVEN `_logger.LogInformation("original template {param}", value)` exists
- WHEN it is replaced with `[LoggerMessage]` source-generated method
- THEN the emitted log level, message template, and parameters match exactly

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
