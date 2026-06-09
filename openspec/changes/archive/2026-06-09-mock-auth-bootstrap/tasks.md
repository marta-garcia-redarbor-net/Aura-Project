# Tasks: Mock Authentication Bootstrap

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | ~280 |
| 400-line budget risk | Low |
| Chained PRs recommended | No |
| Suggested split | Single PR |
| Delivery strategy | ask-on-risk |
| Chain strategy | pending |

Decision needed before apply: No
Chained PRs recommended: No
Chain strategy: pending
400-line budget risk: Low

### Suggested Work Units

| Unit | Goal | Likely PR | Notes |
|------|------|-----------|-------|
| 1 | Full mock-auth bootstrap | PR 1 | Single PR; ~280 lines within budget |

## Phase 1: Application Contracts

- [x] 1.1 Create `src/Aura.Application/Models/AuraUser.cs` — sealed record with `UserId`, `DisplayName`, `Email` (follow `SemanticQuery` pattern)
- [x] 1.2 Create `src/Aura.Application/Ports/ICurrentUserService.cs` — port returning `AuraUser?` (follow `ISemanticChunkExtractor` pattern)

## Phase 2: Infrastructure Identity Adapter (TDD)

- [x] 2.1 Add `<FrameworkReference Include="Microsoft.AspNetCore.App" />` to `src/Aura.Infrastructure/Aura.Infrastructure.csproj`
- [x] 2.2 Create `src/Aura.Infrastructure/Adapters/Identity/MockJwtOptions.cs` — options with `Key`, `Issuer`, `Audience`, `ExpirationMinutes`
- [x] 2.3 RED: Write unit test `tests/Aura.UnitTests/Identity/HttpContextCurrentUserServiceTests.cs` — verify `ClaimsPrincipal` maps to `AuraUser` and unauthenticated returns null
- [x] 2.4 GREEN: Create `src/Aura.Infrastructure/Adapters/Identity/HttpContextCurrentUserService.cs` — implement `ICurrentUserService` via `IHttpContextAccessor`
- [x] 2.5 Create `src/Aura.Infrastructure/Adapters/Identity/MockJwtGenerator.cs` — symmetric JWT generation with configurable claims
- [x] 2.6 Create `src/Aura.Infrastructure/Adapters/Identity/DependencyInjection.cs` — `AddIdentityAdapter()`: JWT Bearer config, mock provider registration, env guard
- [x] 2.7 Modify `src/Aura.Infrastructure/DependencyInjection.cs` — call `services.AddIdentityAdapter(configuration, environment)`

## Phase 3: API Endpoint + Integration Tests (TDD)

- [x] 3.1 RED: Create `tests/Aura.IntegrationTests/Auth/AuthorizationFlowTests.cs` — scenarios: 401 without token, 200 with mock token, mock-login returns valid JWT (use `WebApplicationFactory`)
- [x] 3.2 GREEN: Create `src/Aura.Api/Endpoints/AuthEndpoints.cs` — `MapAuthEndpoints()` with `POST /api/auth/mock-login` (dev-only)
- [x] 3.3 GREEN: Modify `src/Aura.Api/Program.cs` — add `UseAuthentication`, `UseAuthorization`, `MapAuthEndpoints`

## Phase 5: Remediation (verify blockers)

- [x] 5.1 Align environment guard with design: move `MockJwtGenerator` registration guard from API endpoint-only to DI (`AddIdentityAdapter`) + pass `IHostEnvironment` through `AddAuraInfrastructure`
- [x] 5.2 Add approval test `MockLogin_InProduction_ReturnsNotFound` to verify non-dev guard behavior
- [x] 5.3 Update all `AddAuraInfrastructure` callers: `Program.cs` (Api), `Program.cs` (Workers), `InfrastructureDependencyInjectionTests`, `WorkersHostCompositionTests`
- [x] 5.4 Produce strict-TDD `TDD Cycle Evidence` ledger for apply-progress artifact
