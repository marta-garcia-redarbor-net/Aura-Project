# Proposal: Mock Authentication Bootstrap

## Intent

Prepare the authentication foundation decoupled from Graph, allowing local development and testing (W1-H5) via a mock identity provider without coupling to external infrastructure.

## Scope

### In Scope
- Define generic identity ports and domain models (`ICurrentUserService`, `AuraUser`) in `Application`.
- Implement `MockIdentityProvider` in `Infrastructure` using local JWT generation.
- Add `POST /api/auth/mock-login` endpoint in `Api` to issue development JWTs.
- Register standard ASP.NET Core Authentication/Authorization middleware.
- Add integration tests validating allow/deny logic for a protected endpoint.

### Out of Scope
- Microsoft Entra ID or Graph integration.
- Touching or modifying `kernel/plugins/WorkItem`.
- Production-grade asymmetric JWT signing.
- Persistent user storage (in-memory mock only).

## Capabilities

### New Capabilities
- `api-authentication`: Defines how users authenticate with the API and how their identity context is established.

### Modified Capabilities
- None

## Approach

Use **JWT Bearer Token Mocking**. The `Application` layer will define an `ICurrentUserService` port and an `AuraUser` model (pure C# objects, no Entra ID fields). The `Infrastructure` layer will implement this with a mock provider and local symmetric JWT generator. The `Api` layer will expose a mock login endpoint and configure `AddJwtBearer` for standard ASP.NET Core authorization, maintaining clean architecture boundaries as required by `aura-clean-arch-guard`.

## Affected Areas

| Area | Impact | Description |
|------|--------|-------------|
| `src/Aura.Application/` | New | `ICurrentUserService`, `AuraUser` models. |
| `src/Aura.Infrastructure/` | New | `MockIdentityProvider`, JWT generation, DI registration. |
| `src/Aura.Api/` | Modified | Middleware setup (`Program.cs`), `AuthEndpoints`. |
| `tests/Aura.IntegrationTests/` | New | Authorization flow testing. |

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| Leakage of mock to production | Low | Confine mock DI registration to development environments using `#if DEBUG` or environment checks. |
| Entra ID coupling | Low | Code review to strictly prohibit Entra/Graph specific claims (e.g. `Oid`, `Tid`) in `AuraUser`. |
| `kernel/plugins/WorkItem` conflict | Low | Keep all auth work strictly orthogonal; no modifications to kernel. |

## Rollback Plan

Revert the commits introducing the Auth endpoints, the `AddAuthentication` and `AddAuthorization` middleware registrations in `Program.cs`, and remove the mock provider from the DI container.

## Dependencies

- None (Mock logic is fully self-contained)

## Success Criteria

- [ ] `ICurrentUserService` exists in Application and is clean of SDKs.
- [ ] `POST /api/auth/mock-login` returns a valid JWT.
- [ ] An `[Authorize]` endpoint successfully accepts the mock JWT and rejects unauthenticated requests.
- [ ] No Microsoft Graph dependencies are introduced.