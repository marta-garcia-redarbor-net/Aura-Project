# CI/CD Pipeline

GitHub Actions: build → ghcr.io → ACA on `release/aura/*`.

| Req | Str |
|---|---|
| Trigger on `release/aura/*` | MUST |
| Multi-arch push for 3 hosts | MUST |
| Tag with SHA and `latest` | MUST |
| Deploy via `az containerapp update` | MUST |
| Prior tags kept for rollback | MUST |

#### Scenario: Release push

- GIVEN push to `release/aura/v1.0`
- WHEN workflow runs
- THEN all 3 ACA hosts updated

#### Scenario: Rollback

- GIVEN `sha:abc123` on ghcr.io
- WHEN `az containerapp update --image aura-api:abc123`
- THEN container reverts
