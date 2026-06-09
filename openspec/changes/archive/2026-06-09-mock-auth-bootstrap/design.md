# Design: Mock Authentication Bootstrap

## Technical Approach

JWT Bearer authentication bootstrapped through Clean Architecture: port + model in Application, mock JWT adapter in Infrastructure, thin endpoint + middleware in Api. `MockJwtGenerator` produces symmetric JWTs for development; `HttpContextCurrentUserService` bridges ASP.NET Core's `ClaimsPrincipal` to the domain-neutral `AuraUser`. No Graph/Entra dependency introduced.

## Architecture Decisions

| Decision | Choice | Alternative | Rationale |
|----------|--------|-------------|-----------|
| AuraUser location | `Application/Models` | Domain entity | No business rules — read DTO for use-case context, matching `SemanticQuery` pattern |
| ICurrentUserService | `Application/Ports` | Api layer | Consumed by Application use cases; follows `ISemanticChunkExtractor` port pattern |
| CurrentUserService impl | `Infrastructure/Adapters/Identity` | Api layer | Depends on `IHttpContextAccessor` — adapter concern, not transport orchestration |
| Auth DI in Infrastructure | `AddIdentityAdapter()` | Inline Program.cs | Follows existing `AddEmbeddingAdapter()` registration pattern |
| Mock-only guard | Environment check in DI | `#if DEBUG` | Runtime safety; `#if DEBUG` breaks Release integration test builds |
| Symmetric JWT key | Config-bound `MockJwtOptions` | Hardcoded | Follows `EmbeddingProviderOptions` pattern; testable via `UseSetting` |
| Framework ref in Infra | `<FrameworkReference Include="Microsoft.AspNetCore.App" />` | NuGet package | Standard .NET pattern for class libraries needing ASP.NET Core auth types |

## Data Flow

```
Mock Login:
  POST /api/auth/mock-login → AuthEndpoints → MockJwtGenerator → JWT response

Protected Request:
  Request + Bearer JWT → JwtBearer middleware → ClaimsPrincipal
       → HttpContextCurrentUserService.GetCurrentUser()
       → AuraUser (Application model)
```

## File Changes

| File | Action | Description |
|------|--------|-------------|
| `src/Aura.Application/Ports/ICurrentUserService.cs` | Create | Port returning `AuraUser` from authenticated context |
| `src/Aura.Application/Models/AuraUser.cs` | Create | Sealed record: `UserId`, `DisplayName`, `Email` |
| `src/Aura.Infrastructure/Adapters/Identity/MockJwtGenerator.cs` | Create | Generates symmetric JWTs with configurable claims |
| `src/Aura.Infrastructure/Adapters/Identity/MockJwtOptions.cs` | Create | Options: `Key`, `Issuer`, `Audience`, `ExpirationMinutes` |
| `src/Aura.Infrastructure/Adapters/Identity/HttpContextCurrentUserService.cs` | Create | Maps `ClaimsPrincipal` → `AuraUser` via `IHttpContextAccessor` |
| `src/Aura.Infrastructure/Adapters/Identity/DependencyInjection.cs` | Create | `AddIdentityAdapter()`: JWT Bearer config, mock provider, `ICurrentUserService` |
| `src/Aura.Infrastructure/DependencyInjection.cs` | Modify | Call `AddIdentityAdapter()` |
| `src/Aura.Infrastructure/Aura.Infrastructure.csproj` | Modify | Add `<FrameworkReference Include="Microsoft.AspNetCore.App" />` |
| `src/Aura.Api/Endpoints/AuthEndpoints.cs` | Create | `MapAuthEndpoints()`: POST mock-login (dev-only) |
| `src/Aura.Api/Program.cs` | Modify | Add `UseAuthentication`, `UseAuthorization`, `MapAuthEndpoints` |
| `tests/Aura.IntegrationTests/Auth/AuthorizationFlowTests.cs` | Create | 401/200 authorization flow tests via `WebApplicationFactory` |

## Interfaces / Contracts

```csharp
// Application/Ports/ICurrentUserService.cs
public interface ICurrentUserService
{
    AuraUser? GetCurrentUser();
}

// Application/Models/AuraUser.cs
public sealed record AuraUser
{
    public required string UserId { get; init; }
    public required string DisplayName { get; init; }
    public required string Email { get; init; }
}

// Infrastructure/Adapters/Identity/MockJwtOptions.cs
public sealed class MockJwtOptions
{
    public const string SectionName = "MockJwt";
    public string Key { get; set; } = "";        // ≥32 chars for HMAC-SHA256
    public string Issuer { get; set; } = "aura-dev";
    public string Audience { get; set; } = "aura-api";
    public int ExpirationMinutes { get; set; } = 60;
}
```

## Testing Strategy

| Layer | What | Approach |
|-------|------|----------|
| Integration | 401 without token | `WebApplicationFactory` GET protected endpoint — no auth header |
| Integration | 200 with mock token | POST mock-login → use JWT → GET protected endpoint |
| Integration | Mock-login response | POST mock-login → assert 200 + valid JWT in body |
| Unit | Claims→AuraUser mapping | `HttpContextCurrentUserService` with mock `IHttpContextAccessor` |

## Migration / Rollout

No migration required. Mock-login endpoint registered only when `IHostEnvironment.IsDevelopment()`. Revert = remove auth commits.

## Open Questions

- None — all decisions resolved within existing codebase patterns.
