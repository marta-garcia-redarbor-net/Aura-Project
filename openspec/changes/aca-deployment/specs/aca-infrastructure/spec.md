# ACA Infrastructure

3 ACA hosts (UI, API, Workers) with managed env, ingress, and Qdrant profile.

| Req | Str |
|---|---|
| 3 ACA containers from ghcr.io with `/health` probes | MUST |
| Single Managed Environment | MUST |
| Qdrant profile with graceful fallback | SHOULD |

#### Scenario: Deploy

- GIVEN Bicep deployed
- WHEN `az containerapp list`
- THEN 3 containers `Running`

#### Scenario: Qdrant fallback

- GIVEN no Qdrant profile
- WHEN API starts
- THEN semantic search returns empty
