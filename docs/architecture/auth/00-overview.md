# Authentication — Overview

Aura uses delegated authentication with Microsoft Entra ID.

## Quick path

1. The user opens `Aura.UI`.
2. The user signs in with Microsoft Entra ID.
3. Aura receives a user access token that contains the `oid` claim.
4. Aura uses `oid` as the stable user identity across UI, API, workers, and SignalR authorization decisions.
5. Microsoft Graph calls run with delegated user tokens, not app-only client credentials.

## Core decisions

| Topic | Decision |
|-------|----------|
| Authentication model | Delegated auth with Microsoft Entra ID |
| User identity | The Entra ID token `oid` claim is the authoritative Aura user identifier |
| Graph access model | Microsoft Graph uses delegated user tokens |
| Token renewal | MSAL attempts silent renewal first; Aura requires re-authentication if silent renewal fails |
| Token persistence | User token cache is persisted in SQLite |
| Current delivery scope | Local deployment first, using a production-aligned auth architecture |

## Identity boundaries

| Identity type | What it represents | What it is not |
|---------------|--------------------|----------------|
| Entra ID App Registration | The application identity and configuration container for Aura | Not the user identity |
| User token claims | The signed-in person, including `oid` | Not app configuration |
| Delegated Graph permissions | The Graph permissions granted to Aura on behalf of the signed-in user | Not app-only background permissions |

Aura docs should treat this delegated model as the only target architecture for Graph-backed features.

## Happy-path login flow

```text
User opens Aura.UI
  → Aura.UI redirects to Entra ID sign-in
  → Entra ID returns delegated user token
  → token contains oid
  → Aura.UI forwards bearer token to Aura.Api and SignalR hub connections
  → Aura.Api validates the JWT
  → Aura uses oid as current user identity
  → Graph adapters call Microsoft Graph with delegated user access
```

## Host interaction rules

- `Aura.UI`, `Aura.Api`, and `Aura.Workers` remain separate hosts/processes.
- UI-to-API communication stays over HTTP.
- Real-time delivery stays over SignalR.
- Bearer tokens are forwarded from the UI to the API.
- Production behavior requires real JWT validation in the API.
- Worker/background paths reuse the delegated token cache and must not introduce a separate app credential path for Graph.

## What exists now vs. target direction

| Area | Current documentation decision |
|------|-------------------------------|
| Architecture direction | Production-aligned delegated auth model |
| Delivery scope | Local Docker-based deployment first |
| Host topology | Separate UI, API, and worker hosts from day one |

## Checklist

- [x] Delegated Entra ID auth is the only documented login model.
- [x] `oid` is documented as the Aura user identity.
- [x] Graph access is documented as delegated, not app-only.
- [x] Token cache persistence and silent renewal behavior are documented.

## Next step

- Entra ID setup details: [`01-entra-id-configuration.md`](./01-entra-id-configuration.md)
- Token lifecycle details: [`02-token-lifecycle.md`](./02-token-lifecycle.md)
