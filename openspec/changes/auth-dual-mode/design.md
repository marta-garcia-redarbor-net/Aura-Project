# Design: Auth Dual Mode

## Technical Approach

Remove the `UseEntraId` boolean fork so both auth paths (Entra ID OIDC + demo cookie/mock JWT) are always active. The UI always registers Cookie + OIDC + demo endpoints; the API always validates both JWT issuers simultaneously via two named bearer schemes behind a unified default policy.

## Architecture Decisions

### Decision: API Dual JWT — Two Named Schemes + Unified Default Policy

| Option | Tradeoff | Decision |
|--------|----------|----------|
| **A) Two named `JwtBearer` schemes + default policy listing both** | Idiomatic ASP.NET Core; all existing `.RequireAuthorization()` calls work unchanged; clear scheme identity per token | **CHOSEN** |
| B) Single scheme with custom `IssuerSigningKeyResolver` | Simpler registration, but conflates two fundamentally different validation pipelines (asymmetric metadata vs. symmetric key); fragile | Rejected |
| C) Custom middleware that tries both validators | Reinvents `JwtBearerHandler`; loses built-in events, challenges, diagnostics | Rejected |

**Choice**: Register `EntraId` (default) and `MockJwt` schemes. Set `DefaultPolicy` to accept both schemes so every existing `[Authorize]` / `.RequireAuthorization()` call works without modification.

```
Token arrives → AuthenticationMiddleware
  ├─ DefaultPolicy requires ["EntraId", "MockJwt"]
  ├─ Try EntraId: validate against Entra metadata (asymmetric keys, tenant issuer)
  │   └─ Success → ClaimsPrincipal with Entra claims
  └─ Fallback MockJwt: validate against symmetric key (MockJwtOptions)
      └─ Success → ClaimsPrincipal with mock claims (role=Demo)
```

### Decision: Mock JWT Adds `role=Demo` Claim

| Option | Tradeoff | Decision |
|--------|----------|----------|
| Add `ClaimTypes.Role = "Demo"` in `MockJwtGenerator` | Enables standard `[Authorize(Roles = "Demo")]` and policy-based restrictions | **CHOSEN** |
| Custom claim type (`aura_mode=demo`) | More explicit but requires custom handlers everywhere | Rejected |

### Decision: Mock-Login Endpoint Available in All Environments

| Option | Tradeoff | Decision |
|--------|----------|----------|
| Remove `IsDevelopment()` guard on `/api/auth/mock-login` + `MockJwtGenerator` DI | Demo works in production; symmetric key must be rotated to a non-default value via config | **CHOSEN** |
| Keep Development-only | Demo broken in production — contradicts proposal | Rejected |

**Safety**: Mock JWT carries `role=Demo`. API authorization policies restrict demo users to read-only demo data. Rate limiting already applied via `"auth"` policy.

### Decision: UI Auth Registration — Conditional on Config Presence, Not `UseEntraId`

OIDC is registered when `AzureAd:ClientId` is present (not when `UseEntraId=true`). This prevents startup crashes in environments without Entra config while still allowing both modes.

## Data Flow

### Login Flow (Entra ID)
```
Browser → LoginButton click → /login/challenge?popup=true
  → OIDC Challenge → Entra ID → callback → Cookie session (OIDC tokens saved)
  → ForwardedAccessTokenHandler forwards OIDC access_token to API
  → API validates via "EntraId" scheme
```

### Login Flow (Demo)
```
Browser → "Explore Demo Mode" → /login/demo
  → POST /api/auth/mock-login → mock JWT (role=Demo) stored in cookie "token" claim
  → Cookie session established
  → ForwardedAccessTokenHandler forwards cookie "token" claim to API
  → API validates via "MockJwt" scheme
```

### Logout Flow (Both)
```
Browser → /logout (no query params)
  → SignOutAsync("OpenIdConnect") — try/catch, no-op if OIDC not active
  → SignOutAsync("Cookies") — always clears cookie
  → Redirect "/"
```

## File Changes

| File | Action | Description |
|------|--------|-------------|
| `src/Aura.Infrastructure/Adapters/Identity/DependencyInjection.cs` | Modify | Replace `UseEntraId` fork with dual-scheme registration: `EntraId` (always, when AzureAd config present) + `MockJwt` (always). Set default policy to accept both. Remove `IsDevelopment()` guard on `MockJwtGenerator`. |
| `src/Aura.Infrastructure/Adapters/Identity/MockJwtGenerator.cs` | Modify | Add `ClaimTypes.Role = "Demo"` to generated token claims. |
| `src/Aura.Api/Endpoints/AuthEndpoints.cs` | Modify | Remove `IsDevelopment()` guard on `/mock-login`. Add `DemoOnly` authorization policy for demo-restricted endpoints. |
| `src/Aura.UI/Program.cs` | Modify | Remove `useEntraId` variable and all branching. Always register Cookie + OIDC (conditional on `AzureAd:ClientId` presence). Always register `/login/dev`, `/login/demo`. Simplify `/logout` to sign out both schemes unconditionally. Remove `DevAccessTokenHandler` conditional block — `ForwardedAccessTokenHandler` already handles token forwarding. |
| `src/Aura.UI/Components/Auth/LoginButton.razor` | Modify | Remove `_useEntraId` field. Always use OIDC popup flow. |
| `src/Aura.UI/Components/Layout/Header.razor` | Modify | Remove `UseEntraId` read. Navigate to `/logout` without query params. |
| `src/Aura.UI/Services/ForwardedAccessTokenHandler.cs` | None | Already handles both token paths (OIDC session → cookie claim → client-credentials). |
| Tests | Modify | Remove `UseEntraId`-guarded test scenarios. Add dual-scheme validation tests. |

## Interfaces / Contracts

### Authorization Policies (API)

```csharp
// Default policy — accepts either scheme
options.DefaultPolicy = new AuthorizationPolicyBuilder("EntraId", "MockJwt")
    .RequireAuthenticatedUser()
    .Build();

// Demo-restricted policy — only mock JWT users
options.AddPolicy("DemoOnly", policy =>
{
    policy.AddAuthenticationSchemes("MockJwt");
    policy.RequireAuthenticatedUser();
    policy.RequireRole("Demo");
});
```

### Mock JWT Claims (Updated)

```
sub: mock-user-001
name: Demo User
email: demo@aura.local
oid: demo-user-001
role: Demo          ← NEW
iss: aura-dev
aud: aura-api
```

## Testing Strategy

| Layer | What to Test | Approach |
|-------|-------------|----------|
| Unit | `MockJwtGenerator` produces `role=Demo` claim | xUnit assertion on generated JWT claims |
| Unit | Dual-scheme registration creates both `EntraId` and `MockJwt` | Verify `IAuthenticationSchemeProvider` has both schemes |
| Integration | API accepts Entra ID JWT on protected endpoints | `WebApplicationFactory` with test Entra token |
| Integration | API accepts mock JWT on protected endpoints | `WebApplicationFactory` with mock symmetric token |
| Integration | API rejects mock JWT on `DemoOnly`-restricted endpoints when role != Demo | Token without `role=Demo` → 403 |
| Integration | `/api/auth/mock-login` returns valid token in all environments | Test without `IsDevelopment()` |
| E2E | Login via OIDC → dashboard loads | Playwright (when available) |
| E2E | Login via Demo → dashboard loads with demo identity | Playwright (when available) |
| E2E | Logout clears both session types | Playwright (when available) |

## Migration / Rollout

**No-downtime deployment.** The `UseEntraId` config key is retained but ignored. On deploy:
1. API starts with dual schemes — existing Entra ID tokens continue to validate via `EntraId` scheme.
2. UI starts with both auth paths — existing OIDC login unaffected.
3. Demo mode becomes available in all environments (previously dev-only).
4. Remove `UseEntraId` from config in a follow-up change (out of scope).

**Rollback**: Revert `DependencyInjection.cs` and `Program.cs` to restore `UseEntraId` branching.

## Open Questions

- [ ] Should `MockJwtOptions.Key` have a non-default value enforced in production via startup validation (throw if key == default)?
- [ ] Should SignalR hubs (`AlertHub`, `MeetingAlertHub`) explicitly list both schemes in `[Authorize]`, or does the default policy cover them?
