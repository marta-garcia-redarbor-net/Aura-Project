# Tasks: Auth Dual Mode

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | 280–350 |
| 400-line budget risk | Medium |
| Chained PRs recommended | No |
| Suggested split | Single PR |
| Delivery strategy | ask-on-risk |
| Chain strategy | pending |

Decision needed before apply: Yes
Chained PRs recommended: No
Chain strategy: pending
400-line budget risk: Medium

### Suggested Work Units

| Unit | Goal | Likely PR | Notes |
|------|------|-----------|-------|
| 1 | Dual JWT auth (API + UI) with tests | PR 1 | Full vertical slice; single PR if under 400 lines |

## Phase 1: Foundation — RED Tests (Contracts First)

- [x] 1.1 Create `tests/Aura.UnitTests/Identity/MockJwtGeneratorTests.cs` — assert generated JWT contains `ClaimTypes.Role = "Demo"` (RED: test fails, role claim missing)
- [x] 1.2 Create `tests/Aura.UnitTests/Identity/DualJwtSchemeRegistrationTests.cs` — assert `IAuthenticationSchemeProvider` returns both `"EntraId"` and `"MockJwt"` schemes (RED: test fails, only one scheme registered)
- [x] 1.3 Create `tests/Aura.IntegrationTests/Auth/DualJwtValidationTests.cs` — stub scenarios: mock JWT accepted on protected endpoint, Entra ID JWT accepted, mock JWT rejected on `DemoOnly` endpoint when role != Demo (RED: all fail)

## Phase 2: Core API — GREEN (Dual JWT + Role Claim)

- [x] 2.1 Modify `src/Aura.Infrastructure/Adapters/Identity/MockJwtGenerator.cs` — add `ClaimTypes.Role = "Demo"` to token claims (GREEN: 1.1 passes)
- [x] 2.2 Modify `src/Aura.Infrastructure/Adapters/Identity/DependencyInjection.cs` — replace `UseEntraId` fork with dual-scheme registration: `EntraId` (when `AzureAd:ClientId` present) + `MockJwt` (always); set `DefaultPolicy` to accept both schemes; remove `IsDevelopment()` guard on `MockJwtGenerator` DI (GREEN: 1.2 passes)
- [x] 2.3 Modify `src/Aura.Api/Endpoints/AuthEndpoints.cs` — remove `IsDevelopment()` guard on `/mock-login`; add `"DemoOnly"` authorization policy (`MockJwt` scheme + `RequireRole("Demo")`) (GREEN: 1.3 passes)
- [x] 2.4 Modify `src/Aura.Api/Program.cs` — ensure `MockJwtGenerator` and mock-login registration runs for all environments

## Phase 3: UI Wiring — Remove UseEntraId Branching

- [x] 3.1 Modify `src/Aura.UI/Program.cs` — remove `useEntraId` variable and all if/else branching; always register Cookie auth; register OIDC conditionally on `AzureAd:ClientId` presence (not `UseEntraId`); always register `/login/dev` and `/login/demo` endpoints; simplify `/logout` to `SignOutAsync("OpenIdConnect")` + `SignOutAsync("Cookies")` unconditionally
- [x] 3.2 Modify `src/Aura.UI/Components/Auth/LoginButton.razor` — remove `_useEntraId` field and conditional; always render OIDC popup flow for Microsoft login AND always render "Explore Demo Mode" button linking to `/login/demo`
- [x] 3.3 Modify `src/Aura.UI/Components/Layout/Header.razor` — remove `UseEntraId` config read; `/logout` link sends no query params

## Phase 4: Verification & Cleanup

- [x] 4.1 Run `dotnet build Aura.sln` — verify zero build errors
- [x] 4.2 Run `dotnet test Aura.sln` — verify all unit + integration tests pass (new and existing)
- [x] 4.3 Remove any remaining `UseEntraId` references in modified files (keep config key for backward compat per proposal)
- [x] 4.4 Verify `ForwardedAccessTokenHandler.cs` requires no changes (already handles both token paths)
