# Design: Recover Broken Test Baseline

## Technical Approach

Two vertical slices targeting each failure class, followed by a standing guardrails capability.
**Slice 1** fixes genuine regressions in auth wiring and host startup. **Slice 2** adapts
smoke-test infrastructure to the intentional `<AuthorizeView>` layout refactor. **Slice 3**
promotes the guardrails spec and adds the selector inventory. Clean Architecture boundaries
stay intact throughout — all changes are confined to test infrastructure and openspec.

---

## Architecture Decisions

| Option | Tradeoff | Decision |
|--------|----------|----------|
| Pin `UseEntraId=false` in test factory via `UseSetting` | Slightly more explicit than relying on env file; survives user-secrets overrides | **Chosen**: explicit pin wins over config-precedence ambiguity |
| Rely on `appsettings.Development.json` for `UseEntraId=false` | Breaks when user secrets or env vars set `UseEntraId=true` (observed root cause: Entra ID RSA keys downloaded during validation) | Rejected |
| Shared `TestAuthenticationStateProvider` in `tests/Aura.E2E/Shared/` | One class, used by 3 smoke-test files + `PlaywrightWebApplicationFactory` | **Chosen**: avoids duplication; follows existing stub pattern |
| Per-test inline auth provider | No duplication risk from shared code | Rejected: 4 copies, divergence risk |
| Remove `<AuthorizeView>` from `/test-dashboard` | Simpler tests; removes production auth gate | Rejected: auth gate is intentional production behavior |
| Migrate `PlaywrightBootstrapTests` to `PlaywrightWebApplicationFactory` | Eliminates external-host dependency; reachability gate enforced in `StartAsync()` | **Chosen**: aligns with working browser-test pattern |
| Keep `PlaywrightBootstrapTests` pointing to `https://localhost:5001` | Zero code change; add retry logic | Rejected: unreliable in CI, no explicit failure message |

---

## Data Flow

### Slice 1 — Auth Regression Fix

```
Test factory configuration
  └─ UseSetting("UseEntraId", "false")   ← explicit pin, overrides all config sources
       └─ DependencyInjection.AddIdentityAdapter
            └─ mock JWT pipeline (symmetric key)
                 ├─ MockJwtGenerator signs token with test key
                 └─ JwtBearer validates token with same test key
                      └─ /api/auth/me → 200 OK
```

### Slice 2 — Auth Gate Adaptation

```
WebApplicationFactory<UiMarker>
  └─ ConfigureTestServices
       └─ RemoveAll<AuthenticationStateProvider>
            └─ TestAuthenticationStateProvider(authenticated=true)
                 └─ CascadingAuthenticationState cascades to <AuthorizeView>
                      └─ <Authorized> branch renders
                           └─ data-testid markers present in HTML response
```

### Slice 2 — Browser Test Host Fix

```
PlaywrightWebApplicationFactory.StartAsync()
  └─ AddCascadingAuthenticationState()
  └─ AddAuthorization()
  └─ TestAuthenticationStateProvider → authenticated ClaimsPrincipal
  └─ await _app.StartAsync()
  └─ HTTP probe: GET BaseUrl/health
       ├─ FAIL → throw "HostNotReachable: {BaseUrl}" (named failure per spec)
       └─ OK  → tests proceed; browser navigates to /test-dashboard
```

---

## File Changes

| File | Action | Description |
|------|--------|-------------|
| `tests/Aura.IntegrationTests/Auth/AuthorizationFlowTests.cs` | Modify | Add `UseSetting("UseEntraId", "false")` to factory builder |
| `tests/Aura.E2E/Shared/TestAuthenticationStateProvider.cs` | Create | Shared test helper: always returns authenticated `ClaimsPrincipal` |
| `tests/Aura.E2E/Dashboard/InitialDashboardSmokeTests.cs` | Modify | Inject `TestAuthenticationStateProvider` in `CreateClient` setup |
| `tests/Aura.E2E/Dashboard/InboxPreviewPanelFieldsSmokeTests.cs` | Modify | Same injection as above |
| `tests/Aura.E2E/Dashboard/SyncStatusPanelSmokeTests.cs` | Modify | Same injection as above |
| `tests/Aura.E2E/Browser/PlaywrightWebApplicationFactory.cs` | Modify | Add auth services + HTTP probe reachability gate |
| `tests/Aura.E2E/Playwright/PlaywrightBootstrapTests.cs` | Modify | Replace hardcoded `https://localhost:5001` with `PlaywrightWebApplicationFactory` |
| `openspec/specs/test-baseline-guardrails/spec.md` | Create | Standing spec (promoted from change artifact) |
| `openspec/specs/test-baseline-guardrails/stable-selectors.md` | Create | Canonical inventory of stable `data-testid` markers |

---

## Interfaces / Contracts

```csharp
// tests/Aura.E2E/Shared/TestAuthenticationStateProvider.cs
internal sealed class TestAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly string _userId;
    public TestAuthenticationStateProvider(string userId = "test-user-001") 
        => _userId = userId;

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
        => Task.FromResult(new AuthenticationState(
            new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, _userId),
                 new Claim(ClaimTypes.Name, "Test User")],
                authenticationType: "Test"))));
}
```

**PlaywrightWebApplicationFactory additions:**
```csharp
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthorization();
builder.Services.AddSingleton<AuthenticationStateProvider>(_ =>
    new TestAuthenticationStateProvider());
```

---

## Testing Strategy

| Layer | What to Test | Approach |
|-------|-------------|----------|
| Integration | Mock-auth pipeline end-to-end | `ProtectedEndpoint_WithMockToken_Returns200WithUser` passes green |
| E2E host-level | Dashboard HTML markers present | All 17 `InitialDashboardSmokeTests` pass green |
| E2E browser | Shell + state transition in real browser | `DashboardRootBrowserTests` + `HealthRouteBrowserTests` pass |
| E2E Playwright | Self-hosted bootstrap, reachability gate | Migrated `PlaywrightBootstrapTests` pass; host-not-reachable case throws named exception |
| Architecture | No new layer violations | Existing ArchitectureTests stay green |

---

## Migration / Rollout

No data or schema migrations. Changes are test-infrastructure only.
Deploy per slice (Slice 1 → Slice 2 → Slice 3). Each slice ships as its own PR.
Rollback: revert the PR; prior (red) baseline restored.

---

## Open Questions

- [ ] `openspec/config.yaml` records `e2e: available: false` — stale after Playwright landed. Update in Slice 3 or as a follow-on?
