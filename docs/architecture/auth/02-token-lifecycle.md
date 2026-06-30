# Authentication — Token Lifecycle

Aura uses delegated user tokens with cookie-based persistence (`SaveTokens = true`) and a 3-tier token forwarding strategy.

## Quick path

1. The user signs in through the OIDC popup flow.
2. The OIDC middleware exchanges the authorization code for tokens.
3. `SaveTokens = true` stores the `access_token` in cookie authentication properties.
4. `ForwardedAccessTokenHandler` reads the token from the cookie and attaches it as Bearer.
5. If no `access_token` is available, client-credentials fallback obtains one.

## Lifecycle decisions

| Topic | Decision |
|-------|----------|
| Token type | Delegated user tokens |
| Cache storage | Cookie authentication properties (`SaveTokens = true`) |
| Renewal strategy | OIDC middleware handles token lifecycle; no manual MSAL renewal |
| Token forwarding | `ForwardedAccessTokenHandler` with 3-tier resolution |
| Client-credentials fallback | `IConfidentialClientApplication.AcquireTokenForClient` when OIDC session has no access_token |
| Graph usage | Reuse the delegated user token context for Graph calls |

## Happy path

```text
First login
  → user opens popup via /login/challenge
  → OIDC middleware triggers Entra ID challenge (ResponseType = "code")
  → Entra ID returns authorization code
  → middleware exchanges code for tokens (access_token, id_token, refresh_token)
  → SaveTokens=true stores tokens in cookie authentication properties

API request
  → ForwardedAccessTokenHandler reads access_token from cookie
  → Bearer token attached to outbound HTTP request
  → API validates JWT (dual issuers, dual audiences)
  → oid used as current user identity
```

## Token resolution order

`ForwardedAccessTokenHandler` resolves tokens in this order:

| Priority | Source | Log level | Condition |
|----------|--------|-----------|-----------|
| 1 | Existing `Authorization` header | `Debug` | Dev mode / mock JWT forwarding |
| 2 | OIDC session `access_token` via `GetTokenAsync("access_token")` | `Debug` | `SaveTokens = true` produced a token |
| 3 | Client-credentials fallback via `AcquireTokenForClient` | `Information` | No token in OIDC session |
| — | No token attached | `Warning` | `IConfidentialClientApplication` not available or acquisition failed |

## Runtime responsibilities

| Runtime part | Responsibility |
|--------------|----------------|
| `Aura.UI` | Hosts OIDC middleware, stores tokens in cookie, exposes `/login/challenge` |
| `ForwardedAccessTokenHandler` | Reads tokens from cookie, forwards as Bearer to API |
| `Aura.Api` | Validates JWTs (dual issuers: v1.0 + v2.0, dual audiences) |
| `IConfidentialClientApplication` | Client-credentials fallback for token acquisition |
| `Aura.Workers` | Runs background work as a separate process |

## Cookie-based token storage

Tokens are persisted via `SaveTokens = true` in the OpenIdConnect options:

- The OIDC middleware stores `access_token`, `id_token`, and `refresh_token` in the cookie authentication properties.
- No SQLite database or external token cache is used.
- The cookie is encrypted and signed by the ASP.NET Core data protection stack.
- Token lifetime is bounded by the cookie expiration and Entra ID token lifetimes.

## Client-credentials fallback

When the OIDC session has no `access_token` (e.g., the `MeetingAlerts` scope is not yet registered in Entra ID):

1. `ForwardedAccessTokenHandler` logs at `Information` level.
2. It resolves `IConfidentialClientApplication` from the request services.
3. It calls `AcquireTokenForClient` with scope `api://{clientId}/.default`.
4. The resulting token is attached as Bearer.
5. If `IConfidentialClientApplication` is not available, it logs at `Warning` and sends the request without a token.
6. If acquisition fails, it logs the exception at `Warning` and sends the request without a token.

## Failure path

| Failure | Expected behavior |
|---------|-------------------|
| No `access_token` in OIDC session | Client-credentials fallback acquires token |
| Client-credentials acquisition fails | Warning logged, request sent without token, API may reject |
| `IConfidentialClientApplication` not registered | Warning logged, request sent without token |
| Missing or invalid bearer at API | API rejects the request |
| Invalid JWT in production | API rejects the request after JWT validation |
| Cookie expired | User must re-authenticate via OIDC popup |

## API JWT validation

The API validates inbound JWTs with:

| Parameter | Value |
|-----------|-------|
| Metadata address | v2.0 OpenID configuration |
| Valid issuers | `https://login.microsoftonline.com/{tenant}/v2.0`, `https://sts.windows.net/{tenant}/` |
| Valid audiences | `api://{clientId}`, `{clientId}` |
| `OnAuthenticationFailed` | LogWarning with token details |
| `OnChallenge` | LogDebug |
| `OnTokenValidated` | No-op |

## Checklist

- [x] Cookie-based token storage is documented (no SQLite).
- [x] OIDC middleware handles token lifecycle (no manual MSAL renewal).
- [x] 3-tier token resolution is documented.
- [x] Client-credentials fallback is documented.
- [x] `oid` is documented as the identity claim Aura relies on.
- [x] API JWT validation with dual issuers is documented.

## Next step

- Entra ID setup: [`01-entra-id-configuration.md`](./01-entra-id-configuration.md)
- Deployment implications: [`../deployment/00-overview.md`](../deployment/00-overview.md)
