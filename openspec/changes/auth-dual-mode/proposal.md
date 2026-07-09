# Proposal: Auth Dual Mode

## Intent

Aura toggles between Entra ID and mock auth via `UseEntraId` config — only one mode works at a time. Users need both simultaneously: real Microsoft login for authenticated users AND demo-mode entry for visitors, without reconfiguring or deploying separate instances.

## Scope

### In Scope
- Remove `UseEntraId` branching from UI auth config (always register OIDC + Cookie + DevAccessTokenHandler)
- LoginButton always uses Microsoft OIDC popup (login real)
- Always register `/login/demo` and `/login/dev` endpoints (demo + dev, sin guards de config)
- API dual JWT validation: accept Entra ID OR mock symmetric JWTs simultaneously, siempre
- Mock-login endpoint disponible en todos los entornos (no solo Development)
- Logout clears both OIDC and cookie sessions unconditionally
- Update tests for always-on auth paths

### Out of Scope
- Changing landing page or dashboard rendering
- Removing `UseEntraId` config key (keep for backward compat)
- Demo data loading or behavior
- Changes to SignalR auth or Graph token acquisition

## Capabilities

### New Capabilities
None. All changes modify existing capabilities.

### Modified Capabilities
- `api-authentication`: API MUST accept both Entra ID-issued and mock-symmetric JWTs simultaneously. Add a second JWT bearer scheme or use a custom handler that tries both validators.
- `restricted-access-view`: Login card MUST always show "Sign in with Microsoft" AND "Explore Demo Mode" buttons. Remove `UseEntraId` gating on button visibility.
- `demo-auth`: Spec already requires `UseEntraId` independence — no delta needed.

## Approach

**UI**: Always register OIDC + Cookie auth chains. Always register `DevAccessTokenHandler` and `ITokenAcquisitionService` implementations. The `ForwardedAccessTokenHandler` already chains token sources (Auth header → OIDC session → cookie claim → client-credentials) — it works with any session type. `/login/dev` and `/login/demo` always registered. `/logout` signs out from both OIDC and Cookie schemes unconditionally.

**API**: Register a second JWT bearer scheme (e.g., `MockJwt`) alongside the existing Entra ID scheme. Order: Entra ID first (production-safe), MockJwt second (symmetric key, demo-only claims). Mock-login endpoint and mock JWT validation available in all environments — demo JWT carries restricted claims (role=Demo) and API authorization policies enforce read-only demo access.

## Affected Areas

| Area | Impact | Description |
|------|--------|-------------|
| `src/Aura.UI/Program.cs` | Modified | Remove `UseEntraId` if/else from auth registration, cleanup |
| `src/Aura.Infrastructure/Adapters/Identity/DependencyInjection.cs` | Modified | Dual JWT bearer schemes |
| `src/Aura.UI/Components/Auth/LoginButton.razor` | Modified | Always uses Microsoft OIDC popup |
| `src/Aura.UI/Components/Layout/Header.razor` | Modified | Logout sends both OIDC and Cookie sign-out |
| `src/Aura.UI/Services/ForwardedAccessTokenHandler.cs` | None | Already handles both paths |
| `openspec/specs/api-authentication/spec.md` | Modified | Add dual-validation requirements |
| `openspec/specs/restricted-access-view/spec.md` | Modified | Always-show-both-buttons requirement |
| Tests | Modified | Remove `UseEntraId`-guarded test scenarios |

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| Mock JWT grants production data access | Medium | Demo JWT has limited claims (role=Demo); API enforces per-endpoint authorization policies restricting demo users to demo-only data |
| OIDC registration fails without Entra config | Medium | Register OIDC conditionally on config presence, not `UseEntraId` |
| Dual JWT schemes produce ambiguous 401s | Low | Explicit per-endpoint policy; Entra ID as default scheme |
| Mock-login endpoint exposed in production | Low | Endpoint returns demo-only token; rate-limited like other auth endpoints |

## Rollback Plan

Revert `Program.cs` and `DependencyInjection.cs` to restore `UseEntraId` branching. Restore original auth gate logic. Deploy prior commit.

## Dependencies

- None (all changes self-contained)

## Success Criteria

- [ ] "Login / Access Aura" → OIDC popup → dashboard with real Entra ID session
- [ ] "Explore Demo Mode" → demo cookie → dashboard with demo identity
- [ ] Logout clears both OIDC and cookie sessions
- [ ] Both auth paths work in all environments (dev + production)
- [ ] Demo JWT enforces role=Demo on API — no access to production-only data
- [ ] `dotnet test Aura.sln` passes with all existing and new tests
