# Authentication — Overview

Aura uses delegated authentication with Microsoft Entra ID via an OIDC popup flow.

## Quick path

1. The user opens `Aura.UI`.
2. The UI triggers an OIDC challenge (`/login/challenge`) that opens a popup to Entra ID.
3. Entra ID returns an authorization code; the OIDC middleware exchanges it for tokens.
4. `SaveTokens = true` stores the `access_token` in cookie authentication properties.
5. `ForwardedAccessTokenHandler` reads the token from the cookie and attaches it as a Bearer header on API requests.
6. Aura uses the `oid` claim from the validated JWT as the stable user identity.
7. When the OIDC session has no `access_token`, `ForwardedAccessTokenHandler` falls back to client credentials via `IConfidentialClientApplication`.

## Core decisions

| Topic | Decision |
|-------|----------|
| Authentication model | Delegated auth with Microsoft Entra ID (OIDC popup flow) |
| Flow type | Authorization Code (`ResponseType = "code"`) with PKCE via OIDC middleware |
| User identity | The Entra ID token `oid` claim is the authoritative Aura user identifier |
| Token storage | `SaveTokens = true` stores access_token in cookie authentication properties |
| Token forwarding | `ForwardedAccessTokenHandler` attaches Bearer tokens to outbound API requests |
| Client-credentials fallback | `IConfidentialClientApplication.AcquireTokenForClient` when OIDC session has no access_token |
| Scope | `openid profile email api://{clientId}/MeetingAlerts` |
| Client secret | Required — used for OIDC token exchange and client-credentials fallback |
| Current delivery scope | Local deployment first, using a production-aligned auth architecture |

## Identity boundaries

| Identity type | What it represents | What it is not |
|---------------|--------------------|----------------|
| Entra ID App Registration | The application identity and configuration container for Aura | Not the user identity |
| User token claims | The signed-in person, including `oid` | Not app configuration |
| Delegated permissions | The permissions granted to Aura on behalf of the signed-in user | Not app-only background permissions |

## Happy-path login flow

```text
User opens Aura.UI
  → UI opens popup to /login/challenge
  → OIDC middleware triggers Entra ID challenge (ResponseType = "code")
  → Entra ID returns authorization code to /signin-oidc callback
  → OIDC middleware exchanges code for tokens (access_token, id_token, refresh_token)
  → SaveTokens=true stores tokens in cookie authentication properties
  → On API call, ForwardedAccessTokenHandler reads access_token from cookie
  → Bearer token forwarded to Aura.Api
  → API validates JWT (dual issuers: v1.0 + v2.0)
  → Aura uses oid as current user identity
```

## ForwardedAccessTokenHandler resolution order

The handler resolves tokens in this order:

1. **Existing Authorization header** — dev mode / mock JWT forwarding
2. **OIDC session token** — `GetTokenAsync("access_token")` from cookie properties (`SaveTokens = true`)
3. **Client-credentials fallback** — `IConfidentialClientApplication.AcquireTokenForClient` with scope `api://{clientId}/.default`

Log levels:
- `Debug` for forwarding (tiers 1 and 2)
- `Information` for successful client-credentials fallback
- `Warning` for errors or missing `IConfidentialClientApplication`

## API authentication pipeline

The API validates JWTs with:

- **Metadata address**: v2.0 OpenID configuration
- **Valid issuers**: `https://login.microsoftonline.com/{tenant}/v2.0` and `https://sts.windows.net/{tenant}/`
- **Valid audiences**: `api://{clientId}` and `{clientId}`
- **Events**: `OnAuthenticationFailed` at LogWarning, `OnChallenge` at LogDebug, `OnTokenValidated` is no-op

## Host interaction rules

- `Aura.UI`, `Aura.Api`, and `Aura.Workers` remain separate hosts/processes.
- UI-to-API communication stays over HTTP with Bearer tokens forwarded by `ForwardedAccessTokenHandler`.
- Real-time delivery stays over SignalR.
- Production behavior requires real JWT validation in the API.
- The `resource` parameter is added to OIDC redirects so Entra ID returns an access_token for the API (not Microsoft Graph).

## Checklist

- [x] OIDC popup flow is documented as the authentication model.
- [x] `oid` is documented as the Aura user identity.
- [x] `ForwardedAccessTokenHandler` and its 3-tier resolution are documented.
- [x] Client-credentials fallback is documented.
- [x] `SaveTokens = true` cookie-based storage is documented.
- [x] `resource` parameter is documented.

## Next step

- Entra ID setup details: [`01-entra-id-configuration.md`](./01-entra-id-configuration.md)
- Token lifecycle details: [`02-token-lifecycle.md`](./02-token-lifecycle.md)
