## Verification Report

**Change**: `recover-broken-test-baseline`
**Version**: `proposal.md` + `design.md` + `tasks.md` + `apply-progress.md` + `specs/test-baseline-guardrails/spec.md`
**Mode**: Strict TDD

### Completeness
| Metric | Value |
|--------|-------|
| Tasks total | 18 |
| Tasks checked complete in `tasks.md` | 18 |
| Tasks verified complete by evidence | 18 |
| Tasks incomplete | 0 |

### Build & Tests Execution
**Build**: ✅ Passed
```text
dotnet build Aura.sln -v minimal
Result: 0 warnings, 0 errors
```

**Tests**: ✅ Targeted reruns green and ✅ full-suite green
```text
Targeted commands:
- dotnet test tests/Aura.IntegrationTests/Aura.IntegrationTests.csproj --filter "FullyQualifiedName~AuthorizationFlowTests" -v minimal
  Result: 4/4 passed

- dotnet test tests/Aura.E2E/Aura.E2E.csproj --filter "FullyQualifiedName~PlaywrightHostReachabilityGateTests" -v minimal
  Result: 3/3 passed

- dotnet test tests/Aura.E2E/Aura.E2E.csproj --filter "FullyQualifiedName~PlaywrightBootstrapTests|FullyQualifiedName~DashboardRootBrowserTests|FullyQualifiedName~HealthRouteBrowserTests|FullyQualifiedName~PlaywrightHostReachabilityGateTests" -v minimal
  Result: 8/8 passed

Full-suite command:
- dotnet test Aura.sln -v minimal
  Result: 815/815 passed
    - Aura.UnitTests: 644/644
    - Aura.IntegrationTests: 84/84
    - Aura.E2E: 40/40
    - Aura.ArchitectureTests: 47/47
```

**Coverage**: 76.96% / threshold: 80% → ⚠️ Below
```text
dotnet test Aura.sln -m:1 --collect:"XPlat Code Coverage" --results-directory "C:\Users\marta.garcia\AppData\Local\Temp\opencode\verify-recover-broken-test-baseline-rerun" -v minimal
Result: passed and emitted 4 Cobertura reports
```

### TDD Compliance
| Check | Result | Details |
|-------|--------|---------|
| TDD Evidence reported | ✅ | `apply-progress.md` contains a `TDD Cycle Evidence` table with 8 evidence rows, including the targeted verify-gap closure for `HostNotReachable`. |
| All tasks have tests | ✅ | 6/6 code-backed TDD rows map to concrete runtime tests; the documentation/config rows are non-executable and present. |
| RED confirmed (tests exist) | ✅ | Referenced test files exist for auth integration, dashboard smoke, Playwright bootstrap, host reachability gate, unit coverage, and architecture coverage. |
| GREEN confirmed (tests pass) | ✅ | Current verification reruns passed: targeted auth + host-gate suites and 815/815 in the full suite. |
| Triangulation adequate | ✅ | `PlaywrightHostReachabilityGateTests` now covers non-success health probe, transport exception, and success path; auth/dashboard flows remain multi-case. |
| Safety Net for modified files | ✅ | Targeted auth/E2E reruns and the full-suite safety net all passed. |

**TDD Compliance**: 6/6 checks passed

---

### Test Layer Distribution
| Layer | Tests | Files | Tools |
|-------|-------|-------|-------|
| Unit | 7 | 2 | `dotnet test` / xUnit / bUnit / NSubstitute |
| Integration | 47 | 8 | `dotnet test` / xUnit / `WebApplicationFactory` |
| E2E | 37 | 8 | `dotnet test` / xUnit / `WebApplicationFactory` / Playwright |
| Architecture | 6 | 1 | `dotnet test` / xUnit / NetArchTest |
| **Total** | **97** | **19** | |

**Tooling note**: `openspec/config.yaml` is now aligned with repo reality and correctly declares Playwright-backed E2E coverage as available.

---

### Changed File Coverage
| File | Line % | Branch % | Uncovered Lines | Rating |
|------|--------|----------|-----------------|--------|
| `src/Aura.Api/Endpoints/DashboardEndpoints.cs` | 51.06% | 27.59% | `L57-L61, L63-L67, L91-L95, L97-L101, L159-L163, L177-L178, L180-L181, L184-L186, L188, L190, L192, L194-L198, L200-L204, L206, L212-L213, L215-L216, L219-L223, L225, L227, L229, L231-L235, L237-L241, L243` | ⚠️ Low |
| `src/Aura.Application/UseCases/Calendar/GetUpcomingMeetingsUseCase.cs` | 100.00% | N/A | — | ✅ Excellent |
| `src/Aura.Application/Models/UpcomingMeetingDto.cs` | 88.89% | N/A | `L10` | ⚠️ Acceptable |
| `src/Aura.UI/Components/Dashboard/SyncStatusPanel.razor` | 58.18% | 76.92% | `L13, L15, L17, L61-L64, L67-L68, L70-L76, L78-L82, L99, L101` | ⚠️ Low |
| `src/Aura.UI/Components/Dashboard/UpcomingMeetingsPanel.razor` | 86.67% | 78.57% | `L53-L55, L75-L77` | ⚠️ Acceptable |

**Average changed executable-file coverage**: **76.96%**

**Coverage scope note**: Coverlet reports executable source files under `src/**`. Changed test infrastructure under `tests/**` and OpenSpec artifacts were verified through runtime/static evidence, not Cobertura line metrics.

---

### Assertion Quality
| File | Line | Assertion | Issue | Severity |
|------|------|-----------|-------|----------|
| `tests/Aura.E2E/Playwright/PlaywrightBootstrapTests.cs` | 64 | `Assert.True(await shell.IsVisibleAsync())` | Smoke-test-only visibility check; proves presence, not behavior beyond render | WARNING |
| `tests/Aura.E2E/Playwright/PlaywrightBootstrapTests.cs` | 88 | `Assert.True(await inboxPanel.IsVisibleAsync())` | Smoke-test-only visibility check; no state or contract behavior asserted | WARNING |
| `tests/Aura.E2E/Playwright/PlaywrightBootstrapTests.cs` | 107 | `Assert.True(await syncPanel.IsVisibleAsync())` | Smoke-test-only visibility check; no reachability/error-path behavior asserted | WARNING |
| `tests/Aura.E2E/Dashboard/SyncStatusPanelSmokeTests.cs` | 49 | `Assert.Contains("data-testid=\"sync-status-panel\"", html)` | Presence-only smoke assertion; selector exists but user-visible behavior is not exercised | WARNING |

**Assertion quality**: 0 CRITICAL, 4 WARNING

---

### Quality Metrics
**Linter**: ✅ `dotnet build Aura.sln -v minimal` completed with 0 warnings / 0 errors
**Type Checker**: ✅ No compile/type errors surfaced during build or test execution

### Spec Compliance Matrix
| Requirement | Scenario | Test | Result |
|-------------|----------|------|--------|
| Mock-Auth Integration Contract Preservation | Auth refactor preserves mock-token success path | `tests/Aura.IntegrationTests/Auth/AuthorizationFlowTests.cs` > `ProtectedEndpoint_WithMockToken_Returns200WithUser`; `MockLogin_InDevelopment_ReturnsValidJwt` | ✅ COMPLIANT |
| Mock-Auth Integration Contract Preservation | Auth contract change migrates tests in the same PR | `AuthorizationFlowTests`, `CorsMockLoginTests`, and affected authenticated integration suites all remain present and passing; no auth contract change was introduced | ✅ COMPLIANT |
| E2E Selector Stability Contract | Layout refactor preserves stable selectors | `tests/Aura.E2E/Dashboard/InitialDashboardSmokeTests.cs`; `tests/Aura.E2E/Dashboard/SyncStatusPanelSmokeTests.cs`; `tests/Aura.E2E/GraphConnector/GraphConnectorStatusSmokeTests.cs`; `tests/Aura.E2E/Playwright/PlaywrightBootstrapTests.cs` | ✅ COMPLIANT |
| E2E Selector Stability Contract | Selector removal is explicit and atomic | `tests/Aura.E2E/Dashboard/SyncStatusPanelSmokeTests.cs` + `openspec/specs/test-baseline-guardrails/stable-selectors.md` | ⚠️ PARTIAL |
| Playwright Host Reachability Gate | Unreachable host surfaces a named failure | `tests/Aura.E2E/Browser/PlaywrightHostReachabilityGateTests.cs` > `EnsureHostReachableAsync_WhenHealthProbeReturnsNonSuccess_ThrowsHostNotReachableWithUrlAndPort`; `EnsureHostReachableAsync_WhenProbeThrowsTransportException_ThrowsHostNotReachableWithInnerError` | ✅ COMPLIANT |
| Playwright Host Reachability Gate | Reachable host — browser assertions proceed | `tests/Aura.E2E/Browser/PlaywrightHostReachabilityGateTests.cs` > `EnsureHostReachableAsync_WhenHealthProbeReturnsSuccess_DoesNotThrow`; `tests/Aura.E2E/Playwright/PlaywrightBootstrapTests.cs`; `tests/Aura.E2E/Browser/DashboardRootBrowserTests.cs`; `tests/Aura.E2E/Browser/HealthRouteBrowserTests.cs` | ✅ COMPLIANT |
| Refactor-Test Failure Classification | Regression classification requires code correction | `tests/Aura.IntegrationTests/Auth/AuthorizationFlowTests.cs`; `tests/Aura.ArchitectureTests/DashboardArchitectureTests.cs`; `dotnet test Aura.sln -v minimal` | ✅ COMPLIANT |
| Refactor-Test Failure Classification | Intentional fallout classification requires test adaptation | `tests/Aura.E2E/Dashboard/InitialDashboardSmokeTests.cs`; `tests/Aura.E2E/Dashboard/SyncStatusPanelSmokeTests.cs`; `openspec/specs/test-baseline-guardrails/stable-selectors.md` | ✅ COMPLIANT |

**Compliance summary**: 7/8 scenarios compliant, 1/8 partial, 0/8 untested

### Correctness (Static Evidence)
| Requirement | Status | Notes |
|------------|--------|-------|
| Full-suite green goal | ✅ Implemented | `dotnet test Aura.sln -v minimal` passed with 815/815 tests green. |
| Mock-token integration requests return `200 OK` | ✅ Implemented | `UseEntraId=false` is pinned in affected integration factories and `ProtectedEndpoint_WithMockToken_Returns200WithUser` passes. |
| Runtime proof exists for `HostNotReachable` | ✅ Implemented | `PlaywrightHostReachabilityGateTests` exercises both non-success probe and transport-exception paths, asserting the named failure with URL/port diagnostics. |
| Future guardrails define stable selectors and host gate expectations | ⚠️ Partially implemented | OpenSpec/spec/config are aligned and runtime host-gate proof exists, but the selector deprecation classification is not recorded as an inline source/test comment exactly as REQ-02 scenario 2 describes. |
| No intentional auth/layout rollback was introduced | ✅ Implemented | Changes remain limited to test wiring, selector contract hardening, Playwright host bootstrap, OpenSpec alignment, and the Application DTO boundary fix required for full-suite green. |

### Coherence (Design)
| Decision | Followed? | Notes |
|----------|-----------|-------|
| Pin `UseEntraId=false` in test factories | ✅ Yes | Present in auth and affected integration suites, matching the chosen design. |
| Shared authenticated test user helper for auth-gated UI tests | ✅ Yes | Implemented in `tests/Aura.E2E/Shared/TestAuthenticationStateProvider.cs` and reused via `AddAuthenticatedUiTestUser()`. |
| Preserve `<AuthorizeView>` auth gate and adapt tests via authenticated host wiring | ✅ Yes | `/test-dashboard` remains auth-gated; tests inject authenticated state rather than removing the gate. |
| Migrate Playwright bootstrap to `PlaywrightWebApplicationFactory` | ✅ Yes | Hardcoded `https://localhost:5001` dependency is removed; Playwright tests use the self-hosted factory. |
| Add named host reachability gate | ✅ Yes | `HostNotReachable: {BaseUrl}` is implemented and now runtime-proven for both failure and success probe paths. |
| Keep Clean Architecture boundaries intact | ✅ Yes | `UpcomingMeetingDto` moved into `Aura.Application.Models`; `DashboardArchitectureTests` pass and `DashboardEndpoints` no longer reference `Aura.Domain`. |
| File inventory stays aligned to the implementation | ⚠️ Partial | `GraphConnectorStatusSmokeTests` and the upcoming-meetings DTO boundary fix extend beyond the original design file table, though both changes still align with the stated intent. |

### Issues Found
**CRITICAL**:
- None.

**WARNING**:
- Changed executable-file coverage is **76.96%**, below the configured **80%** threshold; the weakest files remain `DashboardEndpoints.cs` (51.06%) and `SyncStatusPanel.razor` (58.18%).
- `InboxPreviewPanelFieldsSmokeTests.GetRoot_PopulatedPreview_NullFields_OmitsEmptySpans` still does not assert omission of the null-field selectors that the test name promises.
- `PlaywrightBootstrapTests` and one sync smoke assertion remain presence-only smoke checks, so behavioral depth is still thin even though runtime proof now exists for the host gate.
- `apply-progress.md` is materially correct, but its RED/GREEN wording is freer than the strict `✅ Written` / `✅ Passed` convention expected by `strict-tdd-verify.md`, reducing machine-auditability.
- REQ-02 scenario `Selector removal is explicit and atomic` remains partial because the selector deprecation intent is documented in OpenSpec, but not as an inline code/test comment exactly where the rename/deprecation occurred.

**SUGGESTION**:
- Raise changed-file coverage for `DashboardEndpoints.cs` and `SyncStatusPanel.razor`, especially the error/cancellation branches still uncovered.
- Strengthen Playwright/bootstrap smoke checks to assert state transitions or user-visible outcomes, not only element presence.
- Add explicit negative assertions in `InboxPreviewPanelFieldsSmokeTests` for omitted null-field selectors.
- Sync the design file inventory with the final changed-file set if the change is archived as the canonical implementation record.

### Verdict
PASS WITH WARNINGS

The previous CRITICAL blocker is closed: `HostNotReachable` now has passing runtime proof for both non-success and transport-failure paths, and the full suite is green. Remaining issues are warnings only and do not block PASS readiness.
