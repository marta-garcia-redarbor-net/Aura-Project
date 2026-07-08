# Proposal: ACA Deployment

## Intent

Deploy Aura to Azure Container Apps for live evaluation. Migrate SQLite ADO.NET to EF Core + Azure SQL free tier. Add Demo Mode and CI/CD via GitHub Actions.

## Scope

### In Scope
- 3 ACA hosts — ghcr.io images
- EF Core + Azure SQL migrate stores under existing port contracts
- Qdrant on ACA with workload profile; graceful fallback in Demo Mode
- Demo Mode: interactive data-loading buttons (no narration)
- GA: build → ghcr.io push → deploy on `release/aura/*`
- Entra ID auth with ACA redirect URIs

### Out of Scope
- Guided tour (user records walkthrough)
- Production SLA (evaluator only)
- Remove Docker Compose local dev

## Capabilities

### New Capabilities
- `aca-infrastructure`: Bicep — 3 ACA hosts, managed env, ingress
- `azure-sql-migration`: EF Core + Azure SQL, store-by-store
- `ci-cd-pipeline`: GA workflow — build, push, deploy
- `demo-mode`: Data-loading buttons, Qdrant-optional fallback

### Modified Capabilities
- `work-item-persistence`: SQLite ADO.NET → EF Core + Azure SQL (port unchanged)
- `environment-config`: New vars for Azure SQL, ACA, ghcr.io
- `container-configuration`: Multi-arch builds, ACA health probes

## Approach
1. Bicep: ACA env + 3 container apps
2. EF Core on Azure SQL; migrate stores one by one
3. Multi-arch Dockerfiles, ACA probes
4. GA workflow with deploy action
5. Demo toggle + seed data; Qdrant bypass
6. Entra ID redirect URIs update

## Affected Areas

| Area | Impact | Description |
|------|--------|-------------|
| `src/Infrastructure/Stores/` | Modified | SQLite → EF Core |
| `src/Infrastructure/DI.cs` | Modified | DbContext + store DI |
| `Dockerfile.*` | Modified | Multi-arch, probes |
| `infra/aca/` | New | Bicep templates |
| `.github/workflows/deploy.yml` | New | CI/CD workflow |
| `src/Application/Demo/` | New | Demo use cases |

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| Free tier quota exceeded | Med | Monitor; consumption plan |
| EF Core breaks contract | Low | Tests before swap |
| Qdrant ACA cost overrun | Low | Demo fallback disables it |

## Rollback Plan
- SQLite stores side-by-side (config toggle)
- Docker Compose unchanged
- ghcr.io prior tags via GA revert
- Bicep `what-if` before deploy

## Dependencies
- Azure free account ($200 credit)
- ghcr.io enabled for org
- Entra ID App Registration update

## Success Criteria
- [ ] EF Core + Azure SQL migration complete (all stores)
- [ ] 3 ACA containers deployed and reachable
- [ ] Entra ID auth works with ACA URIs
- [ ] Demo Mode loads seed data on demand
- [ ] CI/CD deploys on `release/aura/*` push
- [ ] Evaluator logs in and sees app working
