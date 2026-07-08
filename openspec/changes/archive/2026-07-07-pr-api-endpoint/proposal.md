# Propuesta: Endpoint específico de Pull Requests (PR API)

## Intención

El mock `AzureDevOpsPrClient` en UI no escala, no refleja datos reales, y el dashboard necesita PRs reales desde BD. La infraestructura ya persiste PRs como `WorkItem` (SourceType=PrReview) y el API expone `/api/workitems?sourceType=PrReview`, pero el UI nunca lo consume — mostrando datos hardcodeados. El coste operativo de mantener datos ficticios crece con cada nueva feature del dashboard.

## Alcance

### Incluido
- Nuevo endpoint `GET /api/pull-requests` con DTO rico (BuildStatus, BranchName, ReviewApprovals, SourceBranchName, ReviewRequired, ReviewChangesRequested)
- `PullRequestDto` en Application + mapper WorkItem → PullRequestDto (deserializa `MetadataJson` con claves `pr.*`)
- `IPullRequestsApiClient` en UI + implementación HTTP
- Migrar `PullRequests.razor` y `PrioritySummaryService` del mock al nuevo cliente
- Tests unitarios, integración y arquitectura (EfWorkItemStore → endpoint → UI)

### Excluido
- Pipeline de ingesta (AzureDevOpsPrProvider, PrReviewConnectorAdapter, PrReviewWorkItemMapper)
- Eliminar el mock `AzureDevOpsPrClient` — se mantiene hasta migración completa de tests E2E
- Modificar endpoint genérico `/api/workitems` ni su DTO `WorkItemDetailDto`
- Modificar entidad `WorkItem` del dominio

## Capacidades

> Contrato con sdd-spec. Investigados `openspec/specs/` existentes.

### Nuevas
- `pull-request-api`: Endpoint `GET /api/pull-requests` que reusa `IWorkItemReader.ReadBySourceAsync`, mapea WorkItem → PullRequestDto (campos específicos PR desde MetadataJson), respeta filtro OwnerUserId (null = visible para todos) y devuelve lista ordenada por PriorityScore DESC

### Modificadas
- `pr-connector-ui`: Migrar consumo de datos PR de mock hardcodeado (`AzureDevOpsPrClient`) a API real (`PullRequestsApiClient`). Modifica PullRequests.razor, PrioritySummaryService, tests unitarios. Preserva testids existentes (`pr-loading`, `pr-empty`, `pr-error`, `pr-row`)

## Enfoque

Opción C de exploración: endpoint específico que reusa `IWorkItemReader`.
1. **Application**: `PullRequestDto` con 22 campos PR + mapper que parsea `MetadataJson["pr.buildStatus"]`, `pr.reviewApprovals`, etc.
2. **Api**: Endpoint `GET /api/pull-requests` → llama a `IWorkItemReader.ReadBySourceAsync(PrReview)` → mapea → devuelve `List<PullRequestDto>`
3. **UI**: `PullRequestsApiClient` (hereda patrón de `WorkItemsApiClient`). `PullRequests.razor` inyecta nuevo cliente. `PrioritySummaryService` migra a nuevo cliente.
4. **Tests**: Stub de `IPullRequestsApiClient` para unit tests. Integration test endpoint → EfWorkItemStore real.

## Áreas afectadas

| Área | Impacto | Descripción |
|------|---------|-------------|
| `src/Aura.Application/Models/PullRequestDto.cs` | New | DTO específico PR con todos los campos |
| `src/Aura.Application/Mapping/` | New | Mapper WorkItem → PullRequestDto |
| `src/Aura.Api/Endpoints/PullRequestsEndpoints.cs` | New | Endpoint GET /api/pull-requests |
| `src/Aura.UI/Services/PullRequestsApiClient.cs` | New | Cliente HTTP del endpoint |
| `src/Aura.UI/Services/PrioritySummaryService.cs` | Modified | Migrar a PullRequestsApiClient |
| `src/Aura.UI/Pages/PullRequests.razor` | Modified | Inyectar nuevo cliente, mantener testids |
| `src/Aura.UI/Program.cs` | Modified | Registrar PullRequestsApiClient + HttpMessageHandler |
| `tests/Aura.UnitTests/` | Modified | Tests migrados al nuevo stub |
| `tests/Aura.IntegrationTests/` | New | Tests endpoint → store |
| `openspec/specs/pull-request-api/` | New | Spec nueva del endpoint |
| `openspec/specs/pr-connector-ui/` | Modified | Delta: mock → API |

## Riesgos

| Riesgo | Prob. | Mitigación |
|--------|-------|------------|
| Metadata drift — claves `pr.*` en connector cambian | Media | Integration tests alertan mismatch; alinear mapper con PrReviewWorkItemMapper |
| Backward compat tests E2E (StubPrClient, ThrowingPrClient) | Media | Mantener mock `IAzureDevOpsPrClient` existente hasta migrar E2E |
| Duplicación semántica PullRequestDto vs WorkItemDetailDto | Baja | Documentar como específico vs genérico; no acoplar uno al otro |

## Plan de Rollback

1. Revertir commits del endpoint (`PullRequestsEndpoints.cs`, `PullRequestDto.cs`, mapper)
2. Restaurar `IAzureDevOpsPrClient` como dependencia en `Aura.UI/Program.cs`
3. Restaurar `PullRequests.razor` y `PrioritySummaryService` a versión anterior
4. Ejecutar `dotnet test Aura.sln` para validar

## Dependencias

Ninguna externa. `IWorkItemReader` ya registrado como `EfWorkItemStore`. BD con seed data de PRs (6 WorkItems con SourceType=PrReview).

## Criterios de Éxito

- [ ] `GET /api/pull-requests` devuelve PRs con campos específicos (BuildStatus, BranchName, ReviewApprovals, etc.)
- [ ] `PullRequests.razor` renderiza con datos reales desde BD (no mock)
- [ ] `PrioritySummaryService` muestra PRs reales en dashboard cards
- [ ] Tests existentes (unitarios) pasan con nuevo stub
- [ ] Tests de integración verifican endpoint → EfWorkItemStore → BD
- [ ] Cobertura ≥ 80% en nuevo código
