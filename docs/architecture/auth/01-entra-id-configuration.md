# Authentication — Entra ID Configuration

This document defines how Aura uses the Microsoft Entra ID App Registration for delegated authentication and Microsoft Graph access.

## Quick path

1. Create or reuse one Entra ID App Registration for Aura.
2. Configure `ClientId` and `TenantId` from that App Registration.
3. Grant Microsoft Graph **delegated** permissions.
4. Configure redirect URIs for the local UI host.
5. Do not require a `ClientSecret` for the delegated Graph flow.

## Core decisions

| Topic | Decision |
|-------|----------|
| App Registration ownership | `ClientId` and `TenantId` belong to the Aura Entra ID App Registration |
| User ownership | The user identity comes from token claims, especially `oid` |
| Graph permission model | Delegated permissions only |
| Client secret usage | Not required for the delegated Graph flow |
| Initial deployment scope | Local development and local Docker deployment |

## Configuration model

| Setting | Source | Meaning |
|---------|--------|---------|
| `TenantId` | Entra ID App Registration / tenant | Which Entra tenant issues Aura tokens |
| `ClientId` | Entra ID App Registration | Which application the user is signing into |
| `ClientSecret` | Not needed for this flow | Do not require it for delegated Graph access |
| Redirect URI | Aura UI local host | Where Entra ID returns the browser after sign-in |

## Important distinction

| Question | Answer |
|----------|--------|
| Is `ClientId` the user ID? | No. It identifies the Aura application registration. |
| Is `TenantId` the user tenant membership claim? | No. It identifies the Entra tenant Aura is configured against. |
| How is the user identified inside Aura? | By the `oid` claim from the validated token. |

## Delegated Graph permissions

Aura calls Microsoft Graph on behalf of the signed-in user.

That means:

- Graph permissions must be configured as **Delegated permissions**.
- Aura must request only the scopes required for its features.
- Graph access depends on the signed-in user's consent model and tenant policy.
- Aura should not document or configure an app-only `ClientSecret` fallback for the same feature path.

## How to grant Graph delegated permissions

1. Open the Aura App Registration in Microsoft Entra ID.
2. Go to **API Permissions**.
3. Select **Add a permission**.
4. Choose **Microsoft Graph**.
5. Choose **Delegated permissions**.
6. Add only the scopes Aura needs, such as Teams, Outlook, and Calendar scopes.
7. Apply **admin consent** when the tenant requires it.
8. If tenant policy allows user consent for a scope, the user may grant consent during sign-in.

## Consent notes

| Consent type | When it applies |
|--------------|-----------------|
| Admin consent | Required for scopes or tenant policies that do not allow self-service user consent |
| User consent | Possible only when tenant policy and scope classification allow it |

## Local-host expectations

For the current scope:

- `Aura.UI` is its own local host/process/port.
- `Aura.Api` is its own local host/process/port.
- `Aura.Workers` runs separately and does not replace user sign-in.
- The local redirect URI must point to the UI host, not the API host.

## Checklist

- [x] `ClientId` is documented as an App Registration value.
- [x] `TenantId` is documented as an App Registration value.
- [x] `ClientSecret` is explicitly marked unnecessary for delegated Graph flow.
- [x] Graph permissions are explicitly documented as delegated permissions.

## Next step

- Login and token behavior: [`00-overview.md`](./00-overview.md)
- Token caching and renewal: [`02-token-lifecycle.md`](./02-token-lifecycle.md)
