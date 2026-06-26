# Authentication — Token Lifecycle

Aura uses delegated user tokens with a persistent SQLite-backed token cache.

## Quick path

1. The user signs in through Entra ID.
2. MSAL stores token state in the persistent SQLite cache.
3. Aura reuses cached state and attempts silent token renewal.
4. If silent renewal succeeds, the session continues without user interruption.
5. If silent renewal fails, Aura requires the user to authenticate again.

## Lifecycle decisions

| Topic | Decision |
|-------|----------|
| Token type | Delegated user tokens |
| Cache storage | Persistent SQLite-backed token cache |
| Renewal strategy | Silent renewal first via MSAL |
| Failure behavior | Re-authenticate when silent renewal fails |
| Graph usage | Reuse the delegated user token context for Graph calls |

## Happy path

```text
First login
  → user signs in with Entra ID
  → token contains oid
  → MSAL persists token cache state in SQLite

Later request
  → Aura loads cached account/token state
  → MSAL tries AcquireTokenSilent
  → success: Graph/API calls continue
  → failure: Aura prompts the user to sign in again
```

## Runtime responsibilities

| Runtime part | Responsibility |
|--------------|----------------|
| `Aura.UI` | Starts the interactive login flow and forwards bearer tokens to API and SignalR |
| `Aura.Api` | Validates JWTs and resolves the current user from token claims |
| `Aura.Workers` | Runs background work as a separate process; it does not collapse user auth into the worker host |
| Token cache storage | Persists MSAL token cache state in SQLite for reuse across local restarts |

## First login flow

The first login flow is intentionally simple:

1. User opens Aura.
2. User authenticates with Entra ID.
3. Aura receives a token with `oid`.
4. Aura uses `oid` as the internal user identity key.

## Failure path

| Failure | Expected behavior |
|---------|-------------------|
| Token expired but refreshable | MSAL silently renews it |
| Silent renewal fails | Aura requests interactive re-authentication |
| Missing or invalid bearer token at API | API rejects the request |
| Invalid JWT in production | API rejects the request after real JWT validation |

## Persistence notes

- The token cache is persistent, not in-memory only.
- SQLite is the local persistence mechanism for this phase.
- Local deployment should preserve the SQLite file across container restarts with a mounted volume.

## Checklist

- [x] SQLite persistence is documented for token cache state.
- [x] Silent renewal is documented as the default renewal path.
- [x] Re-authentication is documented as the fallback path.
- [x] `oid` is documented as the identity claim Aura relies on.

## Next step

- Entra ID setup: [`01-entra-id-configuration.md`](./01-entra-id-configuration.md)
- Deployment implications: [`../deployment/00-overview.md`](../deployment/00-overview.md)
