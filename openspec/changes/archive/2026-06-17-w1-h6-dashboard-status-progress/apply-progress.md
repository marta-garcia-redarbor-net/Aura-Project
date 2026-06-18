# Apply Progress: w1-h6-dashboard-status-progress

## Delivery
- Mode: Strict TDD (project-level)
- Delivery strategy: `size:exception` maintainer-approved
- Work unit: single larger review unit for this apply run
- Remediation batch: verify-failure fixes for degraded readiness, UI runtime evidence, and artifact accuracy

## Completed Tasks
- [x] 1.1 RED tests for `SystemStatusReader`
- [x] 1.2 System status Application contracts/models/service
- [x] 1.3 Dashboard infrastructure readiness adapters + DI wiring
- [x] 1.4 System status endpoint + integration contract tests
- [x] 1.5 Application DI registration and microcopy cleanup
- [x] 2.1 RED tests for `ModuleProgressReader`
- [x] 2.2 Module progress Application contracts/models/service
- [x] 2.3 Seeded module progress adapter + adapter DI registration
- [x] 2.4 Module progress endpoint + integration contract tests
- [x] 2.5 Application DI registration and seeded DTO normalization
- [x] 3.1 RED tests for UI HTTP clients
- [x] 3.2 UI models/clients + DI wiring
- [x] 3.3 Dashboard status/progress panels + page wiring
- [x] 3.4 Architecture tests for dashboard adapter isolation
- [x] 3.5 Non-goal documented: Playwright out of scope

## TDD Cycle Evidence
| Task | Test File | Layer | Safety Net | RED | GREEN | TRIANGULATE | REFACTOR |
|------|-----------|-------|------------|-----|-------|-------------|----------|
| 1.1 | `tests/Aura.UnitTests/Dashboard/SystemStatusReaderTests.cs` | Unit | ✅ prior suite rerun before remediation | ❌ Original sequence not strictly RED-first (historical) | ✅ 6/6 passing after remediation | ✅ healthy + degraded + unavailable + mock-auth provider-scope scenarios | ✅ test/wording evidence corrected |
| 1.2 | `tests/Aura.UnitTests/Dashboard/SystemStatusReaderTests.cs` | Unit | ✅ prior suite rerun before remediation | ❌ Original sequence not strictly RED-first (historical) | ✅ passing after remediation | ✅ multiple indicator combinations and microcopy | ✅ contracts upgraded to readiness signal model |
| 1.3 | `tests/Aura.UnitTests/Dashboard/AlwaysHealthyApiReadinessAdapterTests.cs`, `tests/Aura.UnitTests/Dashboard/QdrantReadinessAdapterTests.cs`, `tests/Aura.UnitTests/Dashboard/MockJwtOptionsReadinessAdapterTests.cs` | Unit | ✅ prior suite rerun before remediation | ❌ Original sequence not strictly RED-first (historical) | ✅ 6/6 passing | ✅ healthy/degraded/unavailable + configured/unconfigured adapter paths | ✅ adapter behavior now runtime-covered |
| 1.4 | `tests/Aura.IntegrationTests/Dashboard/SystemStatusEndpointTests.cs` | Integration | N/A (new) | ❌ Not strictly first in this apply batch | ✅ 6/6 passing | ✅ 401 + 200 + write-verb 405 matrix | ✅ endpoint telemetry/logging kept local |
| 1.5 | same as above | Unit/Integration | ✅ targeted tests rerun | ❌ Not strictly first in this apply batch | ✅ passing | ✅ scenarios preserved after cleanup | ✅ completed |
| 2.1 | `tests/Aura.UnitTests/Dashboard/ModuleProgressReaderTests.cs` | Unit | N/A (new) | ❌ Not strictly first in this apply batch | ✅ 2/2 passing | ✅ populated + empty + seeded propagation | ✅ reader remains provider-pass-through |
| 2.2 | same as above | Unit | N/A (new) | ❌ Not strictly first in this apply batch | ✅ 2/2 passing | ✅ two behavior paths | ✅ DTO/port naming aligned |
| 2.3 | same as above | Unit | N/A (new) | ❌ Not strictly first in this apply batch | ✅ 2/2 passing | ✅ seeded true + entries path validated via endpoint tests | ✅ seeded provider isolated in Infrastructure |
| 2.4 | `tests/Aura.IntegrationTests/Dashboard/ModuleProgressEndpointTests.cs` | Integration | ✅ prior suite rerun before remediation | ❌ Original sequence not strictly RED-first (historical) | ✅ 6/6 passing | ✅ 401 + 200 + write-verb 405 + entry-level assertions | ✅ endpoint payload evidence strengthened |
| 2.5 | same as above | Unit/Integration | ✅ targeted tests rerun | ❌ Not strictly first in this apply batch | ✅ passing | ✅ scenarios preserved after cleanup | ✅ completed |
| 3.1 | `tests/Aura.UnitTests/Dashboard/SystemStatusApiClientTests.cs`, `tests/Aura.UnitTests/Dashboard/ModuleProgressApiClientTests.cs` | Unit | N/A (new) | ❌ Not strictly first in this apply batch | ✅ 6/6 passing | ✅ path + non-200 + null payload for both clients | ✅ shared client pattern aligned |
| 3.2 | same as above | Unit | N/A (new) | ❌ Not strictly first in this apply batch | ✅ 6/6 passing | ✅ both clients validated against independent paths | ✅ DI wiring mirrors existing dashboard clients |
| 3.3 | `tests/Aura.E2E/Dashboard/InitialDashboardSmokeTests.cs` | Host-level UI smoke (xUnit WebApplicationFactory) | ✅ 8/8 prior panel-adjacent smoke tests rerun | ⚠️ New assertions added post-implementation (historical remediation) | ✅ 13/13 passing | ✅ DTO rendering + system-status error + module distinct-state + module empty + module error | ✅ no UI business logic added |
| 3.4 | `tests/Aura.ArchitectureTests/DashboardArchitectureTests.cs` | Architecture | N/A (new) | ❌ Not strictly first in this apply batch | ✅ 2/2 passing | ✅ Application and UI isolation assertions | ✅ completed |
| 3.5 | `openspec/changes/w1-h6-dashboard-status-progress/tasks.md` | Artifact | N/A | ➖ Structural task | ✅ completion note added | ➖ Triangulation skipped (single-output artifact update) | ✅ completed |

## Test Summary
- Targeted test commands executed:
  - Safety net rerun: `SystemStatusReaderTests` + `SystemStatusEndpointTests` + `ModuleProgressEndpointTests` + `InitialDashboardSmokeTests` → 23 passed
  - `SystemStatusReaderTests` + dashboard adapter unit tests (`AlwaysHealthyApiReadinessAdapterTests`, `QdrantReadinessAdapterTests`, `MockJwtOptionsReadinessAdapterTests`) → 12 passed
  - `SystemStatusEndpointTests` + `ModuleProgressEndpointTests` → 12 passed
  - `InitialDashboardSmokeTests` → 13 passed
  - `DashboardArchitectureTests` → 2 passed
- Total targeted tests passing in remediation executions: 39 (plus 23-test safety-net baseline)

## Remediation Outcomes
- Implemented explicit degraded path support with `ReadinessSignal` in Application ports/services so `Warning` is now a real runtime behavior (not inferred from booleans).
- Added host-level runtime UI evidence in `InitialDashboardSmokeTests` for:
  - system-status DTO rendering (state + microcopy),
  - system-status error fallback,
  - module-progress distinct pending/in-progress/completed rendering,
  - module-progress empty state,
  - module-progress error state.
- Strengthened module-progress endpoint assertions to validate module identifiers and states, not only count.
- Reconciled artifact wording: task 1.1 completion note now reflects actual degraded/unavailable/provider-scope test coverage.

## Strict TDD Limitations (explicit)
- Original implementation batch was not executed as strict RED-first for each behavior-bearing task.
- This remediation improves evidence and runtime coverage but cannot retroactively reconstruct exact historical RED sequencing for already-merged production code.
- Evidence table now marks those historical limits explicitly instead of overstating strict compliance.

## Deviations / Notes
- Playwright remains out of scope and is documented in `tasks.md` completion notes.
- Clean Architecture boundaries remain preserved: readiness derivation in Application, adapters in Infrastructure, and presentation-only behavior in UI.
