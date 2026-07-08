# Proposal: W4 Security Hardening

## Intent

Close OWASP Top 10 before TFM delivery. No rate limiting, no security headers, no DTO validation, no vuln scanning in CI, no HTTPS enforcement. Targets abuse/DoS, XSS/clickjacking, input injection, unpatched deps, cleartext traffic.

## Scope

### In Scope
- Rate limiting via `System.Threading.RateLimiting` (.NET 9 built-in)
- Security headers middleware (CSP, X-Frame-Options, HSTS, X-Content-Type-Options)
- FluentValidation on API DTOs with auto-422 filter
- Dependabot + `dotnet list package --vulnerable` in CI
- HTTPS redirect in production only
- Security section in README

### Out of Scope
- Auth/authorization (existing Entra stays)
- SQL injection (EF Core parameterization)
- Secrets management, container image scanning

## Capabilities

### New Capabilities
- `api-rate-limiting`: Per-endpoint-group rate-limit policies (100/min general, 10/min auth)
- `security-headers`: Middleware for CSP, X-Frame-Options, HSTS, X-Content-Type-Options
- `input-validation`: FluentValidation per DTO, auto-422 on failure
- `vulnerability-scanning`: Dependabot config + CI `dotnet list package --vulnerable` step

### Modified Capabilities
- `environment-config`: HTTPS redirect keyed by `ASPNETCORE_ENVIRONMENT`
- `container-configuration`: HTTPS port mapping in Docker Compose

## Approach

1. `app.UseRateLimiter()` in Program.cs with named policies
2. Inline security headers middleware before CORS
3. FluentValidation + DI extensions; assembly-scan registration
4. `.github/dependabot.yml` + CI workflow with audit step
5. Conditional `UseHttpsRedirection()` outside Development
6. Append security section to README

## Affected Areas

| Area | Impact |
|------|--------|
| `src/Aura.Api/Program.cs` | Modified |
| `src/Aura.Api/Endpoints/*.cs` | Modified — validation filter |
| `src/Aura.Api/*.csproj` | Modified — FluentValidation packages |
| `src/Aura.Api/Validation/` | New — validators |
| `.github/dependabot.yml` | New |
| `.github/workflows/ci.yml` | New |
| `README.md`, `docker-compose.yml` | Modified |

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| Rate limiting blocks legit traffic | Low | Generous limits, per-group policies |
| CSP breaks embedded resources | Low | Report-only first, pinned allowlist |
| CI audit fails on known vulns | Med | Fail on critical/high only; allowlist configured |
| HTTPS redirect breaks local dev | Low | Guard with `IsDevelopment()` |

## Rollback Plan

- Comment out `UseRateLimiter()` and middleware registrations
- Remove FluentValidation packages from csproj
- Delete `.github/dependabot.yml`; revert CI workflow
- Revert README and docker-compose changes
- All gated to single files — clean revert

## Dependencies

- NuGet: `FluentValidation` 11.x + DI
- Built-in: `System.Threading.RateLimiting` (.NET 9)

## Success Criteria

- [ ] All endpoints return 429 when rate-limit exceeded (integration test)
- [ ] Security headers present in every HTTP response (curl verify)
- [ ] Invalid DTO returns 422 with validation errors (integration test)
- [ ] CI audit step exits non-zero on known vulnerabilities
- [ ] HTTPS redirect active in production; inactive in dev
- [ ] README includes security hardening section
