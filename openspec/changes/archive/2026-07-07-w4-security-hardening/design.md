# Design: W4 Security Hardening

## Technical Approach

Six independent security measures wired into the existing ASP.NET Core pipeline. Rate limiting and security headers are middleware registered in `Program.cs`. Input validation uses FluentValidation with assembly-scan registration and an endpoint filter for auto-422. Vulnerability scanning adds Dependabot config and a CI workflow. HTTPS redirect is environment-conditional. Docker Compose gets an HTTPS port mapping. All changes follow existing patterns: middleware mirrors `CorrelationMiddleware`, endpoints use minimal API extension methods, tests use xUnit + NSubstitute + `WebApplicationFactory`.

## Architecture Decisions

| # | Decision | Options | Tradeoff | Choice | Rationale |
|---|----------|---------|----------|--------|-----------|
| D1 | Rate limiter strategy | FixedWindow vs SlidingWindow vs Concurrency | SlidingWindow avoids boundary bursts; Concurrency too restrictive for API | **SlidingWindow (100/min default, 10/min auth)** | Spec requires per-minute windows; SlidingWindow smooths edge-boundary spikes |
| D2 | Rate limiter placement | Before auth vs after auth | Before auth = protects login; after auth = per-user limits | **Before auth (per client IP)** | Spec says per-IP; auth endpoints need the most protection |
| D3 | Security headers approach | Inline lambda vs dedicated middleware class | Inline is shorter; class is testable and follows existing pattern | **Dedicated `SecurityHeadersMiddleware` class** | Matches `CorrelationMiddleware` pattern; unit-testable with `DefaultHttpContext` |
| D4 | Security headers placement | Before vs after correlation middleware | Before = headers on error responses from correlation; after = correlation ID set first | **After `CorrelationMiddleware`, before CORS** | Headers must be on every response; correlation ID already set for logging |
| D5 | Validation integration | Manual model validation vs FluentValidation endpoint filter | Manual couples to controllers; filter is declarative per-endpoint | **FluentValidation + endpoint filter** | Minimal API pattern; auto-422 without touching each handler |
| D6 | CI audit gating | Fail on any vuln vs fail on critical/high only | Any = too noisy; critical/high = actionable | **Fail on critical/high; log low/moderate** | Spec requires non-zero exit only for critical/high |
| D7 | HTTPS redirect scope | Always-on vs environment-conditional | Always-on breaks local dev; conditional preserves DX | **Conditional: `!IsDevelopment()`** | Spec explicitly requires dev HTTP access |

## Data Flow

```
Request
  │
  ▼
┌─────────────────────┐
│  UseRateLimiter()    │──→ 429 if IP quota exhausted (SlidingWindow per policy)
└─────────┬───────────┘
          │
┌─────────▼───────────┐
│  UseHttpsRedirection │──→ 307 if !Development && scheme == http
└─────────┬───────────┘
          │
┌─────────▼───────────┐
│  CorrelationMiddleware│──→ X-Correlation-Id header
└─────────┬───────────┘
          │
┌─────────▼───────────┐
│  SecurityHeaders     │──→ CSP, X-Frame-Options, X-Content-Type-Options, HSTS
└─────────┬───────────┘
          │
┌─────────▼───────────┐
│  CORS → Auth → Authz │
└─────────┬───────────┘
          │
┌─────────▼───────────┐
│  ValidationFilter    │──→ 422 if FluentValidation fails
└─────────┬───────────┘
          │
┌─────────▼───────────┐
│  Endpoint handler    │
└─────────────────────┘
```

## File Changes

| File | Action | Description |
|------|--------|-------------|
| `src/Aura.Api/Middleware/SecurityHeadersMiddleware.cs` | Create | Middleware class adding CSP, X-Frame-Options, X-Content-Type-Options, HSTS (env-conditional) |
| `src/Aura.Api/Validation/` | Create | Directory for FluentValidation validators |
| `src/Aura.Api/Validation/ValidationEndpointFilter.cs` | Create | Endpoint filter that invokes `IValidator<T>` and returns 422 on failure |
| `src/Aura.Api/Endpoints/*.cs` | Modify | Add `.AddEndpointFilter<ValidationEndpointFilter>()` to POST/PUT groups |
| `src/Aura.Api/Program.cs` | Modify | Register rate limiter, HTTPS redirect, security headers middleware, FluentValidation assembly scan |
| `src/Aura.Api/appsettings.json` | Modify | Add `RateLimiting` config section (default + auth policies) |
| `src/Aura.Api/Aura.Api.csproj` | Modify | Add `FluentValidation` and `FluentValidation.DependencyInjectionExtensions` packages |
| `.github/dependabot.yml` | Create | NuGet ecosystem, weekly schedule |
| `.github/workflows/ci.yml` | Create | Build + test + `dotnet list package --vulnerable` step (fail on critical/high) |
| `docker-compose.yml` | Modify | Add HTTPS port mapping (`${API_HTTPS_PORT:-7190}:8081`) to `aura-api` |
| `src/Aura.Api/Dockerfile` | Modify | Add `EXPOSE 8081` for HTTPS |
| `.env.example` | Modify | Add `API_HTTPS_PORT=7190` variable |
| `README.md` | Modify | Append security hardening section |
| `tests/Aura.UnitTests/Middleware/SecurityHeadersMiddlewareTests.cs` | Create | Unit tests for header presence and HSTS env-conditional logic |
| `tests/Aura.UnitTests/Validation/ValidationEndpointFilterTests.cs` | Create | Unit tests for 422 response on invalid DTOs |
| `tests/Aura.IntegrationTests/Middleware/RateLimitingIntegrationTests.cs` | Create | Integration tests verifying 429 + `Retry-After` headers |
| `tests/Aura.IntegrationTests/Middleware/SecurityHeadersIntegrationTests.cs` | Create | Integration tests verifying headers on all response codes |

## Interfaces / Contracts

### Rate limiting configuration (appsettings.json)

```json
{
  "RateLimiting": {
    "Default": { "PermitLimit": 100, "WindowSeconds": 60 },
    "Auth": { "PermitLimit": 10, "WindowSeconds": 60 }
  }
}
```

### Validation error response (422)

```json
{
  "type": "https://tools.ietf.org/html/rfc4918#section-11.6.1",
  "title": "Validation failed",
  "status": 422,
  "errors": {
    "Email": ["'Email' must be a valid email address."],
    "Priority": ["'Priority' must be between 1 and 5."]
  }
}
```

### Security headers (every response)

```
X-Content-Type-Options: nosniff
X-Frame-Options: DENY
Content-Security-Policy: default-src 'self'
Strict-Transport-Security: max-age=31536000; includeSubDomains  ← production only
```

## Testing Strategy

| Layer | What | Approach |
|-------|------|----------|
| Unit | `SecurityHeadersMiddleware` sets all 4 headers; HSTS absent in Development | `DefaultHttpContext` + NSubstitute logger, assert response headers (mirrors `CorrelationMiddlewareTests` pattern) |
| Unit | `ValidationEndpointFilter` returns 422 with field errors | Mock `IValidator<T>`, invoke filter with test `EndpointFilterInvocationContext` |
| Integration | Rate limiter returns 429 after quota exhausted; `Retry-After` header present | `WebApplicationFactory`, loop requests until 429, assert headers |
| Integration | Security headers present on 200, 404, 500 responses | `WebApplicationFactory`, GET various paths, assert headers |
| Integration | HTTPS redirect active in Production, inactive in Development | `WebApplicationFactory` with environment override |
| CI | `dotnet list package --vulnerable` exits non-zero on critical/high | Verify workflow step with known-vulnerable test package |

## Migration / Rollout

No migration required. All changes are additive middleware and configuration. Rollback: remove middleware registrations from `Program.cs`, revert `csproj` package additions, delete `.github/` files.

## Open Questions

- [ ] Should rate limiter key include `X-Forwarded-For` for reverse-proxy deployments (ACA), or is `RemoteIpAddress` sufficient for now?
- [ ] CSP `default-src 'self'` — does the Blazor UI need `wasm-unsafe-eval` or SignalR `connect-src` directives?
