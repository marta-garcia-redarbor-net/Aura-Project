## Exploration: mock-auth-bootstrap

### Current State
Currently, `Aura.Api` has no authentication middleware registered. `Aura.Application` has no ports defining identity, user context, or authorization rules. The application lacks a mechanism to authenticate users or establish their identity. Since the future goal is to integrate with Microsoft Entra ID / Graph (H4) but currently avoid coupling, a simulated mock provider is needed to allow development of W1-H5 tasks without external dependencies. 

### Affected Areas
- `src/Aura.Application/Ports/IAuthService.cs` or `ICurrentUserService.cs` — Needs to be created to abstract the identity provider.
- `src/Aura.Application/Models/AuraUser.cs` — Needs to represent the domain identity (decoupled from Graph/Entra ID).
- `src/Aura.Infrastructure/Adapters/Auth/MockIdentityProvider.cs` — Needs to be created to simulate login and return a mock user.
- `src/Aura.Infrastructure/DependencyInjection.cs` — Needs to register the mock provider.
- `src/Aura.Api/Program.cs` — Needs to register Authentication/Authorization middleware (`AddAuthentication`, `AddAuthorization`).
- `src/Aura.Api/Endpoints/AuthEndpoints.cs` — Needs a mock login endpoint to return a simulated token.
- `tests/Aura.IntegrationTests/` — Needs basic tests for allow/deny logic using the mock setup.

### Approaches
1. **JWT Bearer Token Mocking**
   - **Pros**: Highly realistic. ASP.NET Core natively supports it via `JwtBearerDefaults`. It tests the exact same `[Authorize]` and `ClaimsPrincipal` infrastructure that a real Entra ID integration will use. Allows API to remain stateless.
   - **Cons**: Requires setting up local token generation and symmetric key signing for the mock.
   - **Effort**: Medium

2. **Cookie Authentication Mocking**
   - **Pros**: Very simple to implement. Standard ASP.NET Core auth.
   - **Cons**: Less fidelity if the final consumer is a pure API expecting Bearer tokens (though works fine for Blazor Server). 
   - **Effort**: Low

3. **Custom Headers / Middleware (Bypass)**
   - **Pros**: Extremely fast to build.
   - **Cons**: Low fidelity. Doesn't properly test ASP.NET Core auth mechanisms.
   - **Effort**: Low

### Recommendation
**Approach 1 (JWT Bearer Token Mocking)** is recommended. It provides the highest fidelity for an API-first approach and tests the same infrastructure that Entra ID will use later. We will implement a generic `IAuthenticationService` port in Application that abstracts token generation.

**Minimum Cut**:
1. **Port**: `ICurrentUserService` (returns `AuraUser` / `ClaimsPrincipal`).
2. **Adapter**: `MockIdentityProvider` (implements the port, returns static test users).
3. **API**: `POST /api/auth/mock-login` that issues a JWT for a test user. `builder.Services.AddAuthentication().AddJwtBearer(...)` configured with a local symmetric key for development.
4. **Tests**: Integration test hitting an `[Authorize]` endpoint with and without the mock JWT.

### Risks
- **Coupling to Mock**: The `MockIdentityProvider` must be strictly confined to `Aura.Infrastructure`. We must ensure it doesn't leak into `Aura.Application` or `Aura.Domain`.
- **H4 Coexistence**: `kernel/plugins/WorkItem` is off-limits. The auth system must be completely orthogonal to existing kernel plugins to avoid conflicts.
- **Future Graph Integration**: `AuraUser` must not contain Entra-specific fields (like `Oid`, `Tid`) to prevent premature coupling. It should only contain standard generic claims (Id, Name, Email, Roles).

### Ready for Proposal
Yes. The orchestrator can proceed with creating the SDD proposal targeting a JWT-based mock authentication slice conforming to Clean Architecture.