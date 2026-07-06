# Proposal: W4-H1 Structured Logging with Correlation

## Intent

API requests and worker execution cycles lack a traceable correlation ID across logs, spans, and responses. Debugging production issues requires manual log correlation across hosts. This change introduces a lightweight correlation middleware and logging scope pattern — no new external dependencies.

## Scope

### In Scope
- Correlation middleware for `Aura.Api` — generates/forward `X-Correlation-Id`, enriches `HttpContext`, sets `ILogger.BeginScope`
- Correlation helper for `Aura.Workers` — per-cycle ID generation + `BeginScope` in base worker pattern
- `ILogger.BeginScope` enrichment across both hosts so every log entry carries `CorrelationId`
- 4 remaining `_logger.Log*()` calls migrated to source-generated `[LoggerMessage]`
- Dashboard error-panel enhancement (last-N errors with correlation link)

### Out of Scope
- OpenTelemetry SDK or OTel exporter installation (keep MEAI-only)
- Centralized log aggregation / external log shipping
- Structured log parsing or query tooling
- Cross-host correlation via HTTP headers (workers are push-only)

## Capabilities

### New Capabilities
- `structured-logging`: Correlation middleware, `BeginScope` enrichment, `[LoggerMessage]` audit, and dashboard error-panel contract

### Modified Capabilities
- `connector-execution`: Telemetry section updated — correlation ID MUST be set via `BeginScope` before use case execution, not just "shared"
- `dashboard-system-status`: Dashboard panel SHALL expose last-N errors with correlation ID for troubleshooting context

## Approach

**Aura.Api**: Add inline middleware (before `UseAuthentication`) that reads/generates `X-Correlation-Id`, sets `HttpContext.TraceIdentifier`, and opens `ILogger.BeginScope("{CorrelationId}", id)`. Remove manual `CorrelationId` from individual endpoint logs — it's inherited from scope.

**Aura.Workers**: Extract a `CorrelatedWorkerBase` that wraps `ExecuteAsync` with a new `Activity` + `BeginScope` per cycle. Workers opt in by inheriting instead of `BackgroundService`.

**Migration**: Replace the 4 remaining `_logger.Log*()` call sites with `[LoggerMessage]` partial methods. No behavioral change — only compilation-time source gen.

**Dashboard**: Add GET `/api/dashboard/recent-errors` returning last N errors with correlation ID, timestamp, and message. UI renders as a read-only panel.

## Affected Areas

| Area | Impact | Description |
|------|--------|-------------|
| `src/Aura.Api/Program.cs` | Modified | Add correlation middleware before auth |
| `src/Aura.Api/Endpoints/` | Modified | Remove manual `CorrelationId` from logs (scope inherits it) |
| `src/Aura.Workers/*Worker.cs` | Modified | Inherit `CorrelatedWorkerBase` |
| `src/Aura.Workers/CorrelatedWorkerBase.cs` | New | Base class for scoped correlation |
| `src/Aura.Infrastructure/` | Modified | Migrate 4 `_logger.Log*()` to `[LoggerMessage]` |
| `src/Aura.Api/Endpoints/ErrorEndpoints.cs` | New | `GET /api/dashboard/recent-errors` |

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| `BeginScope` leaks PII via correlation ID in all logs | Low | Use `Guid` IDs, never user data |
| Worker refactor breaks existing workers | Low | `CorrelatedWorkerBase` is opt-in, preserves `BackgroundService` contract |
| Dashboard error panel increases API surface | Low | Read-only GET, scoped to `Dashboard` group auth |

## Rollback Plan

Revert correlation middleware in `Program.cs` and `CorrelatedWorkerBase.cs`. Dashboard error endpoint is additive — no rollback needed for data safety. `[LoggerMessage]` migrations are idempotent.

## Dependencies

- None — this change is standalone, no new NuGet packages

## Success Criteria

- [ ] Every API log entry contains `CorrelationId` in structured output
- [ ] Every worker cycle log entry contains `CorrelationId` in structured output
- [ ] `X-Correlation-Id` response header present on all API responses
- [ ] Zero `_logger.Log*()` calls remain (all migrated to `[LoggerMessage]`)
- [ ] Dashboard panel renders recent errors with correlation ID
