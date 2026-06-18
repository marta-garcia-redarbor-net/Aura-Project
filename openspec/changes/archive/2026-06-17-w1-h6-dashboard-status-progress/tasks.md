# Tasks: W1-H6 Dashboard Status & Progress

## Review Workload Forecast

| Field | Value |
|---|---|
| Estimated changed lines | 620-880 |
| 400-line budget risk | High |
| Chained PRs recommended | Yes |
| Suggested split | PR 1 (system-status) → PR 2 (module-progress) → PR 3 (UI wiring + architecture checks) |
| Delivery strategy | ask-on-risk |
| Chain strategy | pending |

Decision needed before apply: Yes
Chained PRs recommended: Yes
Chain strategy: pending
400-line budget risk: High

### Suggested Work Units

| Unit | Goal | Likely PR | Notes |
|---|---|---|---|
| 1 | Deliver `dashboard-system-status` backend + endpoint + tests | PR 1 | Include RED→GREEN→REFACTOR + endpoint telemetry/log tags |
| 2 | Deliver `dashboard-module-progress` backend + endpoint + tests | PR 2 | Keep seeded provider contract isolated and swappable |
| 3 | Deliver dashboard UI panels + API clients + architecture tests | PR 3 | No Playwright; keep read-only states + stable `data-testid` hooks |

## Phase 1: System Status Capability (TDD First)

- [x] 1.1 **RED** Add failing unit tests in `tests/Aura.UnitTests/Dashboard/SystemStatusReaderTests.cs` for healthy/degraded/unavailable and mock-auth scope scenarios.
- [x] 1.2 **GREEN** Create contracts/models: `src/Aura.Application/Ports/ISystemStatusReader.cs`, `IApiReadinessProvider.cs`, `IQdrantReadinessProvider.cs`, `IMockAuthReadinessProvider.cs`, `src/Aura.Application/Models/SystemStatusDto.cs`, and `src/Aura.Application/Services/SystemStatusReader.cs`.
- [x] 1.3 **GREEN** Create adapters and wiring: `src/Aura.Infrastructure/Adapters/Dashboard/AlwaysHealthyApiReadinessAdapter.cs`, `QdrantReadinessAdapter.cs`, `MockJwtOptionsReadinessAdapter.cs`, `DependencyInjection.cs`; update `src/Aura.Infrastructure/DependencyInjection.cs`.
- [x] 1.4 **RED→GREEN** Add endpoint contract tests in `tests/Aura.IntegrationTests/Dashboard/SystemStatusEndpointTests.cs`; update `src/Aura.Api/Endpoints/DashboardEndpoints.cs` for `GET /api/dashboard/system-status` (GET-only + tags/logging).
- [x] 1.5 **REFACTOR** Register reader in `src/Aura.Application/DependencyInjection.cs`; clean naming/microcopy constants without moving logic to Api/UI.

## Phase 2: Module Progress Capability (TDD First)

- [x] 2.1 **RED** Add failing unit tests in `tests/Aura.UnitTests/Dashboard/ModuleProgressReaderTests.cs` for valid states, empty list, and seeded flag propagation.
- [x] 2.2 **GREEN** Create contracts/models: `src/Aura.Application/Ports/IModuleProgressReader.cs`, `IModuleProgressProvider.cs`, `src/Aura.Application/Models/ModuleProgressDto.cs`, `src/Aura.Application/Services/ModuleProgressReader.cs`.
- [x] 2.3 **GREEN** Create seeded adapter `src/Aura.Infrastructure/Adapters/Dashboard/SeededModuleProgressProvider.cs` and update dashboard adapter DI registration.
- [x] 2.4 **RED→GREEN** Add `tests/Aura.IntegrationTests/Dashboard/ModuleProgressEndpointTests.cs`; extend `src/Aura.Api/Endpoints/DashboardEndpoints.cs` with `GET /api/dashboard/module-progress` (GET-only + telemetry/logging).
- [x] 2.5 **REFACTOR** Update `src/Aura.Application/DependencyInjection.cs` registrations and normalize seeded microcopy/labels in Application DTOs.

## Phase 3: UI Wiring, Isolation, and Non-Goals

- [x] 3.1 **RED** Add failing HTTP client tests: `tests/Aura.UnitTests/Dashboard/SystemStatusApiClientTests.cs` and `ModuleProgressApiClientTests.cs` (path, non-200, null payload).
- [x] 3.2 **GREEN** Create UI models/clients: `src/Aura.UI/Models/SystemStatusResponse.cs`, `ModuleProgressResponse.cs`, `src/Aura.UI/Services/ISystemStatusApiClient.cs`, `SystemStatusApiClient.cs`, `IModuleProgressApiClient.cs`, `ModuleProgressApiClient.cs`; wire DI in `src/Aura.UI/Program.cs`.
- [x] 3.3 **GREEN** Create panels `src/Aura.UI/Components/Dashboard/SystemStatusPanel.razor` and `ModuleProgressPanel.razor`; update `src/Aura.UI/Pages/Index.razor` with loading/empty/error/read-only states and stable `data-testid` hooks.
- [x] 3.4 **RED→GREEN** Add `tests/Aura.ArchitectureTests/DashboardArchitectureTests.cs` enforcing no Infrastructure/provider types in Application or UI.
- [x] 3.5 Document non-goal in `openspec/changes/w1-h6-dashboard-status-progress/tasks.md` completion notes: Playwright remains out of scope for this change.

## Completion Notes

- Playwright remains explicitly out of scope for this change. Verification is covered by unit, integration, and architecture tests only.
- Post-verify remediation (2026-06-17): task 1.1 evidence now includes degraded/unavailable system-status derivation and explicit mock-auth provider-scope assertions in `SystemStatusReaderTests`.
