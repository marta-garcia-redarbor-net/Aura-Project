# Tasks: W4 Security Hardening

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | ~560 |
| 400-line budget risk | High |
| Chained PRs recommended | Yes |
| Suggested split | PR 1 (Middleware) → PR 2 (Validation) → PR 3 (CI/Infra) |
| Delivery strategy | ask-on-risk |
| Chain strategy | pending |

Decision needed before apply: Yes
Chained PRs recommended: Yes
Chain strategy: pending
400-line budget risk: High

### Suggested Work Units

| Unit | Goal | Likely PR | Notes |
|------|------|-----------|-------|
| 1 | Security headers + rate limiting middleware with tests | PR 1 | ~300 lines; standalone middleware layer |
| 2 | FluentValidation pipeline with auto-422 filter | PR 2 | ~164 lines; depends on PR 1 for Program.cs patterns |
| 3 | CI audit + HTTPS redirect + container config + README | PR 3 | ~100 lines; independent infra slice |

## Phase 1: Security Headers Middleware

- [x] 1.1 Create `src/Aura.Api/Middleware/SecurityHeadersMiddleware.cs` — adds X-Content-Type-Options, X-Frame-Options, CSP, and env-conditional HSTS. Mirror `CorrelationMiddleware` pattern. (~50 lines)
- [x] 1.2 Register middleware in `src/Aura.Api/Program.cs` after `CorrelationMiddleware`, before `UseCors`. (~3 lines)
- [x] 1.3 Create `tests/Aura.UnitTests/Middleware/SecurityHeadersMiddlewareTests.cs` — verify all 4 headers present; HSTS absent when `IHostEnvironment.IsDevelopment()` is true. Use `DefaultHttpContext`. (~70 lines)
- [x] 1.4 Create `tests/Aura.IntegrationTests/Middleware/SecurityHeadersIntegrationTests.cs` — verify headers on 200, 404, 500 via `WebApplicationFactory`. (~55 lines)

## Phase 2: Rate Limiting Middleware

- [x] 2.1 Add `RateLimiting` section to `src/Aura.Api/appsettings.json` — Default (100/60s) and Auth (10/60s) policies. (~10 lines)
- [x] 2.2 Register sliding-window rate limiter in `src/Aura.Api/Program.cs` — `AddRateLimiter()` with per-IP partitioning, `UseRateLimiter()` before HTTPS redirect. (~25 lines)
- [x] 2.3 Create `tests/Aura.IntegrationTests/Middleware/RateLimitingIntegrationTests.cs` — exhaust quota, assert 429 + `Retry-After` header. (~75 lines)

## Phase 3: Input Validation (FluentValidation)

- [x] 3.1 Add `FluentValidation` + `FluentValidation.DependencyInjectionExtensions` to `src/Aura.Api/Aura.Api.csproj`. (~4 lines)
- [x] 3.2 Create `src/Aura.Api/Validation/ValidationEndpointFilter.cs` — resolves `IValidator<T>` from DI, returns 422 with RFC 4918 error shape on failure. (~40 lines)
- [x] 3.3 Create validators in `src/Aura.Api/Validation/` for POST/PUT DTOs (e.g., `CreateWorkItemRequest`, auth DTOs). (~50 lines)
- [x] 3.4 Register assembly-scan `AddValidatorsFromAssembly` in `Program.cs`; add `.AddEndpointFilter<ValidationEndpointFilter>()` to POST/PUT groups in `src/Aura.Api/Endpoints/*.cs`. (~15 lines)
- [x] 3.5 Create `tests/Aura.UnitTests/Validation/ValidationEndpointFilterTests.cs` — mock `IValidator<T>`, assert 422 with field errors. (~55 lines)

## Phase 4: CI/CD, HTTPS, Container & Docs

- [x] 4.1 Create `.github/dependabot.yml` — NuGet ecosystem, weekly schedule, solution root. (~12 lines)
- [x] 4.2 Create `.github/workflows/ci.yml` — build + test + `dotnet list package --vulnerable` step; fail on critical/high only. (~55 lines)
- [x] 4.3 Add `app.UseHttpsRedirection()` in `Program.cs` guarded by `!app.Environment.IsDevelopment()`. (~3 lines)
- [x] 4.4 Add HTTPS port mapping `${API_HTTPS_PORT:-7190}:8081` to `aura-api` in `docker-compose.yml`; add `EXPOSE 8081` to `src/Aura.Api/Dockerfile`; add `API_HTTPS_PORT=7190` to `.env.example`. (~8 lines)
- [x] 4.5 Append security hardening section to `README.md` — document headers, rate limiting, validation, HTTPS, vuln scanning. (~25 lines)

## Line Estimates by File

| File | Lines |
|------|-------|
| `SecurityHeadersMiddleware.cs` | 50 |
| `ValidationEndpointFilter.cs` | 40 |
| Validators (new files) | 50 |
| `Program.cs` (modifications) | 35 |
| `appsettings.json` | 10 |
| `Aura.Api.csproj` | 4 |
| `Endpoints/*.cs` (modifications) | 15 |
| `.github/dependabot.yml` | 12 |
| `.github/workflows/ci.yml` | 55 |
| `docker-compose.yml` | 3 |
| `Dockerfile` | 2 |
| `.env.example` | 3 |
| `README.md` | 25 |
| `SecurityHeadersMiddlewareTests.cs` | 70 |
| `ValidationEndpointFilterTests.cs` | 55 |
| `RateLimitingIntegrationTests.cs` | 75 |
| `SecurityHeadersIntegrationTests.cs` | 55 |
| **Total** | **~559** |
