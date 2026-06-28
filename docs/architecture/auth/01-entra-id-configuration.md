# Authentication — Entra ID Configuration

This document defines how Aura uses the Microsoft Entra ID App Registration for delegated authentication and API access.

## Quick path

1. Create or reuse one Entra ID App Registration for Aura.
2. Configure `ClientId`, `TenantId`, and `ClientSecret` from that App Registration.
3. Register the `MeetingAlerts` API scope under **Expose an API**.
4. Grant delegated permissions.
5. Configure redirect URIs for the local UI host.

## Core decisions

| Topic | Decision |
|-------|----------|
| App Registration ownership | `ClientId` and `TenantId` belong to the Aura Entra ID App Registration |
| User ownership | The user identity comes from token claims, especially `oid` |
| Permission model | Delegated permissions only |
| Client secret usage | **Required** — used for OIDC token exchange and client-credentials fallback |
| API scope | `api://{clientId}/MeetingAlerts` registered in Expose an API |
| Initial deployment scope | Local development and local Docker deployment |

## Configuration model

| Setting | Source | Meaning |
|---------|--------|---------|
| `TenantId` | Entra ID App Registration / tenant | Which Entra tenant issues Aura tokens |
| `ClientId` | Entra ID App Registration | Which application the user is signing into |
| `ClientSecret` | App Registration → Certificates & secrets | Required for OIDC token exchange and client-credentials fallback |
| Redirect URI | Aura UI local host | Where Entra ID returns the browser after sign-in |

## Client secret

`ClientSecret` **IS required**. It serves two purposes:

1. **OIDC token exchange** — the Authorization Code flow requires a client secret to exchange the authorization code for tokens.
2. **Client-credentials fallback** — `IConfidentialClientApplication.AcquireTokenForClient` uses the client secret to obtain tokens when the OIDC session has no `access_token`.

Store the secret in `appsettings.json` under `AzureAd:ClientSecret` (development) or a secure secret store (production).

## API scope (Expose an API)

The `MeetingAlerts` scope must be registered in the App Registration under **Expose an API**:

1. Open the App Registration in Entra ID.
2. Go to **Expose an API**.
3. Set the Application ID URI to `api://{clientId}` (default) or a custom URI.
4. Add a scope named `MeetingAlerts`.
5. Set who can consent ( admins or users).
6. Save the scope.

The OIDC configuration requests this scope:

```
api://{clientId}/MeetingAlerts
```

This ensures the `access_token` saved by `SaveTokens = true` has audience `api://{clientId}`, which the API JWT Bearer validator accepts.

## Resource parameter

The OIDC configuration adds a `resource` parameter via `OnRedirectToIdentityProvider`:

```
resource = api://{clientId}
```

This tells Entra ID to issue an access_token for the Aura API instead of the default Microsoft Graph token. Without this parameter, the saved access_token would have a Graph audience and fail API validation.

## Redirect URIs

Configure these redirect URIs in the App Registration under **Authentication**:

| URI | Purpose |
|-----|---------|
| `https://localhost:{port}/signin-oidc` | OIDC callback (default middleware path) |
| `https://localhost:{port}/authentication/callback` | Post-login redirect target |

The exact port depends on the local development configuration. The `/login/challenge` endpoint in the UI triggers the OIDC challenge and specifies `/authentication/callback` as the redirect URI.

## Delegated permissions

Aura calls downstream services on behalf of the signed-in user:

- Graph permissions must be configured as **Delegated permissions**.
- Aura must request only the scopes required for its features.
- Access depends on the signed-in user's consent model and tenant policy.

## How to grant delegated permissions

1. Open the Aura App Registration in Microsoft Entra ID.
2. Go to **API Permissions**.
3. Select **Add a permission**.
4. Choose the target API (Microsoft Graph or the Aura API).
5. Choose **Delegated permissions**.
6. Add only the scopes Aura needs.
7. Apply **admin consent** when the tenant requires it.

## Important distinction

| Question | Answer |
|----------|--------|
| Is `ClientId` the user ID? | No. It identifies the Aura application registration. |
| Is `TenantId` the user tenant membership claim? | No. It identifies the Entra tenant Aura is configured against. |
| How is the user identified inside Aura? | By the `oid` claim from the validated token. |
| Is `ClientSecret` optional? | **No.** It is required for OIDC token exchange and client-credentials fallback. |

## Checklist

- [x] `ClientId` is documented as an App Registration value.
- [x] `TenantId` is documented as an App Registration value.
- [x] `ClientSecret` is documented as **required**.
- [x] `MeetingAlerts` API scope is documented.
- [x] `resource` parameter is documented.
- [x] Redirect URI configuration is documented.
- [x] Delegated permissions are documented.

## Next step

- Login and token behavior: [`00-overview.md`](./00-overview.md)
- Token caching and renewal: [`02-token-lifecycle.md`](./02-token-lifecycle.md)
