# Tasks: W2-H4 — Outlook Plugin Mapping and Initial Classification

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | 560-760 |
| 400-line budget risk | High |
| Chained PRs recommended | No |
| Suggested split | Single PR (size exception approved) with work-unit commits |
| Delivery strategy | exception-ok |
| Chain strategy | size-exception |

Decision needed before apply: No
Chained PRs recommended: No
Chain strategy: size-exception
400-line budget risk: High

### Suggested Work Units

| Unit | Goal | Likely PR | Notes |
|------|------|-----------|-------|
| 1 | Mapper scoring contract and threshold tests | Single PR | Commit as RED tests first, then GREEN mapper implementation |
| 2 | Adapter batch behavior, logging, and DI wiring | Single PR | Keep observability + behavior tests in same work unit |
| 3 | Boundary enforcement, regression run, and docs sync | Single PR | Final commit keeps architecture guard and docs aligned |

## Phase 1: Foundation and Architecture Guard

- [x] 1.1 Create `src/Aura.Infrastructure/Adapters/Connectors/Outlook/OutlookEmailDto.cs` (`internal sealed record`) with `ExternalId`, `Subject`, `Importance`, `SenderAddress`, `BodyPreview`, `ReceivedDateTime`, `CorrelationId`, `ConversationId`.
- [x] 1.2 Create `tests/Aura.ArchitectureTests/OutlookConnectorBoundaryTests.cs` (RED) asserting `Aura.Application` and `Aura.Domain` MUST NOT depend on `Aura.Infrastructure.Adapters.Connectors.Outlook`.

## Phase 2: Mapper Multi-Signal Scoring (TDD)

- [x] 2.1 Create `tests/Aura.UnitTests/Ingestion/Outlook/OutlookWorkItemMapperTests.cs` (RED) for valid mapping, missing `ExternalId` skip, and missing `Subject` default title with metadata trace.
- [x] 2.2 Extend mapper RED tests for scoring scenarios: `Importance` high/normal/low/absent, absent-importance + strong sender => elevated priority, absent-importance + body cue => elevated priority, all signals absent => Medium, max signals => Critical.
- [x] 2.3 Implement `src/Aura.Infrastructure/Adapters/Connectors/Outlook/OutlookWorkItemMapper.cs` (GREEN) with `TryMap`, `ResolvePriority(importance, subject, sender, body)`, thresholds (`>=6 Critical`, `>=2 High`, `>=0 Medium`, `<0 Low`), and `Source="inbox"` + `SourceType=OutlookEmail`.
- [x] 2.4 Implement subject/body deadline-cue scanning (subject first, then body fallback) and always write scoring metadata keys: `outlook.importance.raw`, `outlook.scoring.subjectCues`, `outlook.scoring.senderWeight`, `outlook.scoring.bodyCues`, `outlook.scoring.totalScore`.
- [x] 2.5 REFACTOR mapper internals into private scoring/cue helpers while preserving deterministic token order and existing RED/GREEN assertions.

## Phase 3: Adapter, Telemetry, and Wiring (TDD)

- [x] 3.1 Create `tests/Aura.UnitTests/Ingestion/Outlook/OutlookConnectorAdapterTests.cs` (RED) for all-valid batch success, mixed batch partial failure, and default fixture-provider path.
- [x] 3.2 Implement `src/Aura.Infrastructure/Adapters/Connectors/Outlook/OutlookConnectorAdapter.cs` (GREEN) with continue-on-skip behavior, mapped/skipped counters, and `ConnectorExecutionResult` status/failureReason mapping.
- [x] 3.3 Add source-generated logging in `OutlookConnectorAdapter.cs` for EventIds 3203 (execution summary) and 3204 (skipped item) including source, tenant, window, mapped, and skipped fields.
- [x] 3.4 Update `src/Aura.Infrastructure/Adapters/Connectors/DependencyInjection.cs` to register `OutlookConnectorAdapter` as scoped `IConnectorAdapter` without changing existing connector contracts.

## Phase 4: Verification and Documentation

- [x] 4.1 Run focused tests: `dotnet test tests/Aura.UnitTests/Aura.UnitTests.csproj --filter OutlookWorkItemMapperTests|OutlookConnectorAdapterTests` and `dotnet test tests/Aura.ArchitectureTests/Aura.ArchitectureTests.csproj --filter OutlookConnectorBoundaryTests`.
- [x] 4.2 Run full gate `dotnet test Aura.sln` and verify no architecture boundary regressions and no scoring-scenario failures.
- [x] 4.3 Update `docs/architecture/ingestion/00-overview.md` to reflect Outlook connector mapping and multi-signal classification behavior (importance + subject + sender + body).
