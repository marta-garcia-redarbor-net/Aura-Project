# Tasks: Recover Broken Test Baseline

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | 430-620 |
| 400-line budget risk | High |
| Chained PRs recommended | Yes |
| Suggested split | PR 1 → PR 2 → PR 3 |
| Delivery strategy | ask-always |
| Chain strategy | pending |

Decision needed before apply: Yes
Chained PRs recommended: Yes
Chain strategy: pending
400-line budget risk: High

### Suggested Work Units

| Unit | Goal | Likely PR | Notes |
|------|------|-----------|-------|
| 1 | Restore mock-auth integration contract and remove 401 drift | PR 1 | Includes RED→GREEN auth integration tests; clean rollback boundary |
| 2 | Re-enable auth-gated dashboard smoke coverage with stable selectors | PR 2 | Includes shared auth test provider + selector assertions; depends on PR 1 |
| 3 | Remove external-host Playwright flake and finalize guardrails docs | PR 3 | Includes reachability gate + OpenSpec guardrails sync; depends on PR 2 |

## Phase 1: Foundation / Auth Contract Lock

- [x] 1.1 **RED**: In `tests/Aura.IntegrationTests/Auth/AuthorizationFlowTests.cs`, add/confirm failing coverage for REQ-01 mock-token success path (`ProtectedEndpoint_WithMockToken_Returns200WithUser`).
- [x] 1.2 **GREEN**: In `tests/Aura.IntegrationTests/Auth/AuthorizationFlowTests.cs`, pin `builder.UseSetting("UseEntraId", "false")` with existing test JWT key setup.
- [x] 1.3 **VERIFY**: Run `dotnet test tests/Aura.IntegrationTests --filter "AuthorizationFlowTests"` and keep assertions unchanged except contract-required updates.

## Phase 2: Core Implementation / Auth-Gated E2E Smoke Alignment

- [x] 2.1 **RED**: In `tests/Aura.E2E/Dashboard/InitialDashboardSmokeTests.cs`, assert authorized shell markers for `/test-dashboard` per REQ-02/stable selectors.
- [x] 2.2 **GREEN**: Create `tests/Aura.E2E/Shared/TestAuthenticationStateProvider.cs` returning authenticated `ClaimsPrincipal` for test UI hosts.
- [x] 2.3 **GREEN**: Inject `TestAuthenticationStateProvider` in `InitialDashboardSmokeTests.cs` `CreateClient` test-service wiring.
- [x] 2.4 **GREEN**: Apply the same provider injection in `tests/Aura.E2E/Dashboard/InboxPreviewPanelFieldsSmokeTests.cs` and `tests/Aura.E2E/Dashboard/SyncStatusPanelSmokeTests.cs`.
- [x] 2.5 **REFACTOR**: Remove duplicated auth-test wiring where safe, keeping each file independently readable and reviewable.

## Phase 3: Integration / Playwright Host Reachability Reliability

- [x] 3.1 **RED**: In `tests/Aura.E2E/Playwright/PlaywrightBootstrapTests.cs`, replace hardcoded URL expectations with factory-based host setup that initially fails without wiring.
- [x] 3.2 **GREEN**: In `tests/Aura.E2E/Browser/PlaywrightWebApplicationFactory.cs`, add auth services and `TestAuthenticationStateProvider` registration for `<AuthorizeView>` rendering.
- [x] 3.3 **GREEN**: In `PlaywrightWebApplicationFactory.cs`, add `GET {BaseUrl}/health` probe after startup and throw `HostNotReachable: {BaseUrl}` on failure (REQ-03).
- [x] 3.4 **GREEN**: Update `PlaywrightBootstrapTests.cs` to use `PlaywrightWebApplicationFactory` lifecycle and navigate to self-hosted dashboard route.

## Phase 4: Testing / Suite Recovery Verification

- [x] 4.1 Run `dotnet test tests/Aura.E2E --filter "FullyQualifiedName~Dashboard|FullyQualifiedName~Playwright"` and verify selector + host-gate scenarios pass.
- [x] 4.2 Run `dotnet test Aura.sln` to validate full-suite green baseline (goal constraint #1).
- [x] 4.3 Classify any remaining refactor failures in touched tests as regression vs intentional fallout; fix code or adapt test in the same slice (REQ-04).

## Phase 5: Guardrails / OpenSpec Contract Hardening

- [x] 5.1 Update `openspec/specs/test-baseline-guardrails/spec.md` with final REQ-01/REQ-03 implementation wording if code-level behavior changed.
- [x] 5.2 Update `openspec/specs/test-baseline-guardrails/stable-selectors.md` for any intentional selector rename/removal performed in Phase 2.
- [x] 5.3 Update `openspec/specs/playwright-e2e-bootstrap-spec.md` to cross-reference stable selector contract and explicit host reachability gate.
