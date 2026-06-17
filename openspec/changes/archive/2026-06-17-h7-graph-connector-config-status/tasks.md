# Tasks: Graph Connector Configuration Status (Week 1)

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | 620-780 |
| 400-line budget risk | High |
| Chained PRs recommended | Yes |
| Suggested split | PR 1 -> PR 2 |
| Delivery strategy | single-pr |
| Chain strategy | size-exception |

Decision needed before apply: Yes
Chained PRs recommended: Yes
Chain strategy: size-exception
400-line budget risk: High

### Suggested Work Units

| Unit | Goal | Likely PR | Notes |
|------|------|-----------|-------|
| 1 | Backend status derivation + API contract + tests | PR 1 | Base: main; includes unit/integration tests and telemetry logs |
| 2 | UI read-only panel + architecture/E2E coverage + docs | PR 2 | Base: PR 1 branch if chained; otherwise merged into single PR |

## Phase 1: Foundation / Test-First Contracts (RED)

- [x] 1.1 Create `tests/Aura.UnitTests/GraphConnector/GraphConnectorStatusReaderTests.cs` for Disabled, MissingConfig, PartialConfig, ValidConfig, and precedence ordering; verify tests fail before implementation.
- [x] 1.2 Create `tests/Aura.IntegrationTests/GraphConnector/GraphConnectorStatusEndpointTests.cs` for GET 200 DTO, write verbs 405, and 401 unauthenticated; verify failures against missing endpoint.
- [x] 1.3 Create `tests/Aura.E2E/GraphConnector/GraphConnectorStatusSmokeTests.cs` using stubbed API client for four UI states and no edit controls; verify failing UI assertions first.
- [x] 1.4 Create `tests/Aura.ArchitectureTests/GraphConnectorArchitectureTests.cs` asserting no Graph/provider types in Application or UI namespaces; verify test fails with temporary forbidden reference.

## Phase 2: Core Implementation (GREEN)

- [x] 2.1 Create `src/Aura.Application/Models/GraphConnectorSettings.cs` and `GraphConnectorStatusDto.cs` (`GraphConnectorState` enum); verify unit tests compile against contracts.
- [x] 2.2 Create `src/Aura.Application/Ports/IGraphConnectorSettingsProvider.cs` and `IGraphConnectorStatusReader.cs`; update `src/Aura.Application/DependencyInjection.cs`; verify DI resolves `IGraphConnectorStatusReader`.
- [x] 2.3 Create `src/Aura.Application/Services/GraphConnectorStatusReader.cs` with ordered derivation rules from spec; verify all unit RED tests turn GREEN.
- [x] 2.4 Create `src/Aura.Infrastructure/Adapters/GraphConnector/GraphConnectorOptions.cs`, `AppSettingsGraphConnectorSettingsProvider.cs`, and adapter DI; update `src/Aura.Infrastructure/DependencyInjection.cs`; verify appsettings/env binding scenarios pass in integration tests.

## Phase 3: Integration / API + UI Wiring (GREEN)

- [x] 3.1 Create `src/Aura.Api/Endpoints/GraphConnectorEndpoints.cs` mapping `GET /api/connectors/graph/status` (auth required, GET-only); update `src/Aura.Api/Program.cs`; verify endpoint integration tests pass.
- [x] 3.2 Update `src/Aura.Api/appsettings.Development.json` with `GraphConnector` defaults for local bootstrap; verify status can be derived without external services.
- [x] 3.3 Create `src/Aura.UI/Services/IGraphConnectorApiClient.cs`, `GraphConnectorApiClient.cs`, and `Models/GraphConnectorStatusResponse.cs`; update `src/Aura.UI/Program.cs`; verify typed client retrieves API DTO.
- [x] 3.4 Create `src/Aura.UI/Components/GraphConnector/GraphConnectorStatusPanel.razor` and update `src/Aura.UI/Pages/Index.razor`; verify each state renders distinct `data-testid` and panel remains read-only.
- [x] 3.5 Add structured logs in `GraphConnectorStatusReader` and `GraphConnectorEndpoints` for status evaluation/request path; verify logs are emitted during integration tests.

## Phase 4: Refactor / Verification / Documentation (REFACTOR)

- [x] 4.1 Refactor naming/null-handling in Application and Infrastructure graph connector files without behavior change; verify full test suite still GREEN.
- [x] 4.2 Run `dotnet test Aura.sln` and `dotnet build Aura.sln`; verify unit, integration, E2E smoke, and architecture tests pass together.
- [x] 4.3 Update `openspec/changes/h7-graph-connector-config-status/tasks.md` checkboxes during apply and ensure no task drifts beyond Week 1 scope; verify completed checklist matches implemented files.
