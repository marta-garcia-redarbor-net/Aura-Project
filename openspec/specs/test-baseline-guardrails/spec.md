# Spec: Test Baseline Guardrails

**Capability**: `test-baseline-guardrails`  
**Status**: Standing — applies to every change that touches auth middleware, JWT wiring, UI markup, or Playwright host configuration.  
**Decision rule**: **regression → fix code; intentional refactor fallout → adapt tests.**

---

## Requirements

### REQ-01 — Mock-Auth Integration Contract Preservation

Any change that modifies the auth middleware pipeline, JWT bearer configuration, or
Infrastructure identity adapters MUST either preserve the existing mock-token
integration contract or migrate the `Auth Middleware Integration Tests` within the
same change.

**Scenarios**

#### Scenario: Auth refactor preserves mock-token success path

- GIVEN a change modifies `Program.cs` middleware order or `Identity/DependencyInjection.cs`
- WHEN `dotnet test tests/Aura.IntegrationTests` is run after the change
- THEN all `Auth Middleware Integration Tests` pass without modification
- AND the mock-login endpoint returns `200 OK` with a valid bearer token

#### Scenario: Auth contract change migrates tests in the same PR

- GIVEN a change intentionally alters the mock-login contract (endpoint path or token shape)
- WHEN the PR is submitted
- THEN updated integration tests covering the new contract are included in the same PR
- AND no integration test in `tests/Aura.IntegrationTests` is skipped or deleted without a replacement

**Pinning rule**: Integration test factories MUST set `builder.UseSetting("UseEntraId", "false")`
explicitly. Relying on `appsettings.Development.json` alone is insufficient because user secrets
and environment variables take higher precedence and can silently activate the Entra ID JWT
pipeline, causing `IDX10517` signature validation failures against RSA keys.

---

### REQ-02 — E2E Selector Stability Contract

Stable `data-testid` attributes declared in `openspec/specs/test-baseline-guardrails/stable-selectors.md`
MUST NOT be silently removed or renamed. Any change that removes or renames a stable selector MUST
update all dependent E2E test assertions within the same change.

**Scenarios**

#### Scenario: Layout refactor preserves stable selectors

- GIVEN a change modifies Blazor component markup or layout structure
- WHEN the E2E smoke tests execute against the updated UI
- THEN all assertions on selectors in `stable-selectors.md` remain valid
- AND no smoke test fails with a missing-element error on a previously documented selector

#### Scenario: Selector removal is explicit and atomic

- GIVEN a change intentionally removes or renames a `data-testid` attribute
- WHEN the PR is submitted
- THEN the corresponding E2E test assertions are updated or removed in the same PR
- AND an inline comment documents whether the change is intentional refactor or regression fix

---

### REQ-03 — Playwright Host Reachability Gate

E2E tests MUST assert that the UI host is reachable before executing any browser-level
assertions. A host unreachability failure MUST surface as a distinct, named failure — not
as an element-not-found timeout.

**Scenarios**

#### Scenario: Unreachable host surfaces a named failure

- GIVEN the E2E test run starts
- WHEN the configured host URL is not responding
- THEN the test fixture fails with an explicit `HostNotReachable` message
- AND the message identifies the configured URL and port

#### Scenario: Reachable host — browser assertions proceed

- GIVEN the UI host is running and responds on the configured URL
- WHEN the E2E test run starts
- THEN the host reachability check passes
- AND browser-level test assertions execute normally

**Implementation**: `PlaywrightWebApplicationFactory.StartAsync()` performs an HTTP probe
(`GET {BaseUrl}/health`) after `_app.StartAsync()`. Failure throws
`InvalidOperationException("HostNotReachable: {BaseUrl} — {probe diagnostics}")`.
The probe runs before browser assertions, so host startup failures are surfaced as a
named infrastructure failure rather than selector timeouts.

---

### REQ-04 — Refactor-Test Failure Classification

Every test failure introduced by a refactor MUST be classified before the change is
merged: **regression** (unintended; fix code) or **intentional refactor fallout**
(intended; adapt tests). No test MAY be suppressed, skipped, or deleted without a
documented classification.

**Scenarios**

#### Scenario: Regression classification requires code correction

- GIVEN a refactor causes a test failure that was not the intended outcome
- WHEN the failure is classified as a regression
- THEN the code change is corrected within the same PR to restore passing behavior
- AND the test is not modified to mask the regression

#### Scenario: Intentional fallout classification requires test adaptation

- GIVEN a refactor intentionally changes behavior that a test was asserting
- WHEN the failure is classified as intentional refactor fallout
- THEN the test assertion is updated in the same PR to match the new intended behavior
- AND the test passes without suppression or removal

---

## Related Files

- `openspec/specs/test-baseline-guardrails/stable-selectors.md` — canonical selector inventory
- `openspec/specs/playwright-e2e-bootstrap-spec.md` — original E2E selector documentation
- `tests/Aura.IntegrationTests/Auth/AuthorizationFlowTests.cs` — mock-auth contract tests
- `tests/Aura.E2E/Shared/TestAuthenticationStateProvider.cs` — test auth helper
- `tests/Aura.E2E/Browser/PlaywrightWebApplicationFactory.cs` — host reachability gate
