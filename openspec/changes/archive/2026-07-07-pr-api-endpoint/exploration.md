## Exploration: PR API Endpoint

### Current State

Aura tiene **dos caminos completamente separados** para trabajar con Pull Requests, y exactamente **cero conectados a la base de datos como fuente para la UI**:

**Camino 1 — UI (mock directo):**
- `Aura.UI` tiene `IAzureDevOpsPrClient` / `AzureDevOpsPrClient` — un mock hardcodeado con 6 PRs en memoria.
- Se usa en `PullRequests.razor` (página Blazor) y en `PrioritySummaryService` (dashboard cards).
- Registrado en `Aura.UI/Program.cs` línea 210.
- **No llama a ninguna API.** Es un mock 100% en el proceso del UI.

**Camino 2 — Connector Pipeline (infraestructura):**
- `AzureDevOpsPrProvider` implementa `IMessageSourceProvider<PrReviewDto>` en Infrastructure.
- El provider es usado por `PrReviewConnectorAdapter` (que implementa `IConnectorAdapter`).
- Los PRs se mapean a `WorkItem` via `PrReviewWorkItemMapper` y se persisten vía `IWorkItemStore`.
- **Esto funciona por detección — NO alimenta al UI.**

**Datos existentes en BD:**
- `WorkItemEntity` en `AuraDbContext.DbSet<WorkItemEntity>` → tabla `WorkItems`.
- El `SeedDataHostedService` ya siembra 6 PRs como WorkItems con `SourceType = PrReview`.
- `EfWorkItemStore` implementa `IWorkItemReader` con método `ReadBySourceAsync`.
- La API ya tiene `/api/workitems?sourceType=PrReview` que lee de la BD via `IWorkItemReader`.

Por tanto, **la BD ya tiene PRs** y **la API ya puede devolverlos**. El problema es que el UI no usa ese endpoint.

### Affected Areas

| Área | Archivo | Por qué |
|------|---------|---------|
| UI Model | `src/Aura.UI/Models/PullRequestResponse.cs` | Record de 22 campos para PR — tendrá que ser servido por API o reemplazado por `WorkItemDetailResponse` |
| UI Interface | `src/Aura.UI/Services/IAzureDevOpsPrClient.cs` | Interfaz con un solo método `GetPendingPullRequestsAsync()` — debe ser reemplazada o adaptada |
| UI Mock | `src/Aura.UI/Services/AzureDevOpsPrClient.cs` | Mock hardcodeado con 6 PRs — será eliminado o redirigido a API |
| UI DI | `src/Aura.UI/Program.cs` (línea 210) | Registro de `IAzureDevOpsPrClient` como `AzureDevOpsPrClient` |
| UI Page | `src/Aura.UI/Pages/PullRequests.razor` | Página Blazor que inyecta `IAzureDevOpsPrClient` directamente — 212 líneas |
| UI Dashboard | `src/Aura.UI/Services/PrioritySummaryService.cs` | Consume `IAzureDevOpsPrClient` para construir cards del dashboard |
| UI Tests (unit) | `tests/Aura.UnitTests/Pages/PullRequestsPageTests.cs` | 6 tests que usan `IAzureDevOpsPrClient` como dependencia |
| UI Tests (e2e) | `tests/Aura.E2E/PullRequests/PullRequestsPageSmokeTests.cs` | Smoke tests con StubPrClient |
| API Endpoint | `src/Aura.Api/Endpoints/WorkItemsEndpoints.cs` | Endpoint existente `/api/workitems` que ya soporta `sourceType=PrReview` |
| API DTO | `src/Aura.Application/Models/WorkItemDetailDto.cs` | DTO actual que la API devuelve — no tiene campos específicos de PR (BuildStatus, ReviewApprovals, etc.) |
| API Program | `src/Aura.Api/Program.cs` | Registro de endpoints — `MapWorkItemsEndpoints()` ya está llamado en línea 106 |
| Infra Store | `src/Aura.Infrastructure/Adapters/WorkItems/EfWorkItemStore.cs` | `ReadBySourceAsync` ya implementado para filtrar por `sourceType` y `status` |
| Infra Store Reg | `src/Aura.Infrastructure/StoreRegistrationExtensions.cs` | `IWorkItemReader` ya registrado como `EfWorkItemStore` |
| API Client (UI) | `src/Aura.UI/Services/WorkItemsApiClient.cs` | Cliente HTTP existente que llama a `/api/workitems` — patrón a seguir |
| API Client Interface | `src/Aura.UI/Services/IWorkItemsApiClient.cs` | Interfaz `GetBySourceAsync(sourceType, status)` — admite filtrar por PrReview |
| UI Response Model | `src/Aura.UI/Models/WorkItemDetailResponse.cs` | Modelo de respuesta que el UI recibe del API — contiene datos genéricos de WorkItem |
| Connector Adapter | `src/Aura.Infrastructure/Adapters/Connectors/PrReview/PrReviewConnectorAdapter.cs` | Pipeline de ingestión: fuente ADO → WorkItem → BD |
| PrReview Mapper | `src/Aura.Infrastructure/Adapters/Connectors/PrReview/PrReviewWorkItemMapper.cs` | Mapea `PrReviewDto` → `WorkItem` con metadata enriquecida |
| Existing Spec | `openspec/specs/pr-connector-ui/spec.md` | Spec existente que describe el modelo PullRequestResponse y la UI — asume mock |
| Demo Endpoint | `src/Aura.Api/Endpoints/DemoEndpoints.cs` (línea 73) | Endpoint demo `/api/demo/pull-request` que también carga PRs |
| Demo Service | `src/Aura.Application/Demo/DemoService.cs` (línea 125) | Genera PRs de demo como WorkItems via IWorkItemStore |
| Seed Data | `src/Aura.Infrastructure/Adapters/SeedData/SeedDataHostedService.cs` (línea 203) | Siembra 6 PRs como WorkItems con SourceType=PrReview |

### Diagrama de Relaciones Actual

```
AzureDevOpsPrClient (mock, UI) ──→ PullRequests.razor
    │                              └→ PrioritySummaryService
    │
    └── NO llama a ninguna API

AzureDevOpsPrProvider (Infra) ──→ PrReviewConnectorAdapter ──→ PrReviewWorkItemMapper ──→ IWorkItemStore ──→ BD (WorkItems)
    │                              (solo pipeline de ingestión)
    │
    └── NO alimenta al UI

API /api/workitems?sourceType=PrReview ──→ IWorkItemReader ──→ EfWorkItemStore ──→ BD
    │
    └── WorkItemsApiClient (UI) ──→ IWorkItemsApiClient
         (ya existe, pero PullRequests.razor NO lo usa)
```

### Análisis de Opciones

#### Opción A: Reemplazar IAzureDevOpsPrClient por IWorkItemsApiClient en la UI

| Aspecto | Detalle |
|---------|---------|
| **Qué** | PullRequests.razor y PrioritySummaryService dejan de usar `IAzureDevOpsPrClient` y usan `IWorkItemsApiClient.GetBySourceAsync("PrReview", "Pending")` |
| **DTO mapping** | `WorkItemDetailResponse` no tiene campos específicos PR (BuildStatus, BranchName, etc.). Se necesita o bien extender el DTO o parsear metadata. |
| **Pros** | Sin nuevo endpoint; reusa infraestructura existente; el dato ya está en BD |
| **Contras** | `WorkItemDetailDto` es genérico — metadata PR va en `MetadataJson` (serializado). El UI necesitaría deserializar. Es un paso atrás en UX. Elimina `IAzureDevOpsPrClient`. |
| **Esfuerzo** | Medio |

#### Opción B: Nuevo endpoint específico `/api/pull-requests` con DTO rico

| Aspecto | Detalle |
|---------|---------|
| **Qué** | Nuevo endpoint `GET /api/pull-requests` en `Aura.Api` que lee WorkItems con `SourceType=PrReview` y construye un DTO específico de PR con todos los campos (BuildStatus, ReviewApprovals, BranchName, etc.) |
| **DTO mapping** | Nuevo `PullRequestDto` en Application layer con los 22 campos. Se mapea desde `WorkItem` deserializando metadata (`pr.buildStatus`, `pr.reviewApprovals`, etc.) |
| **API Client** | Nuevo `IPullRequestsApiClient` en UI (o se extiende `WorkItemsApiClient`) |
| **Pros** | DTO limpio y específico; el UI recibe exactamente lo que necesita; no toca el pipeline de conectores; preserva separación de concerns |
| **Contras** | Nuevo endpoint, nuevo DTO, nuevo cliente HTTP; más código |
| **Esfuerzo** | Medio-Alto |

#### Opción C: Híbrida — endpoint nuevo pero reusa WorkItem store

| Aspecto | Detalle |
|---------|---------|
| **Qué** | Endpoint específico que usa `IWorkItemReader.ReadBySourceAsync(PrReview, Pending)`, mapea a `PullRequestDto` y lo sirve |
| **Pros** | Reusa `EfWorkItemStore` y toda la pipeline de persistencia existente; no duplica lógica de acceso a datos |
| **Contras** | Igual que Opción B pero sin cambio en stores |
| **Esfuerzo** | Medio |

### Recomendación

**Opción C** — Endpoint específico `/api/pull-requests` que reusa `IWorkItemReader`. Razones:

1. **Separación de concerns**: El endpoint de WorkItems es genérico (`WorkItemDetailDto` con metadata genérica). PR tiene campos específicos (BuildStatus, ReviewApprovals, etc.) que no deben serializarse como JSON anidado.
2. **Performance**: La UI recibe exactamente los campos que necesita, sin tener que deserializar `MetadataJson` del lado cliente.
3. **Evolutividad**: Cuando se conecte a Azure DevOps real, el DTO PR podrá crecer independientemente del modelo WorkItem.
4. **Consistencia con el patrón existente**: El API ya tiene endpoints específicos: `/api/dashboard/preview`, `/api/dashboard/system-status`. `/api/pull-requests` sigue el mismo patrón.

### Riesgos

1. **Duplicación semántica**: `WorkItemDetailResponse` y `PullRequestResponse` coexistirán — hay que dejar claro que uno es genérico y otro es específico.
2. **Metadata drift**: Los campos PR se almacenan como metadata key-value (`pr.buildStatus`, `pr.reviewApprovals`). Si el connector cambia las keys, el endpoint nuevo se rompe. Hay que alinear el `PrReviewWorkItemMapper` y el nuevo mapper del endpoint.
3. **Backward compat del mock**: `AzureDevOpsPrClient` se usa en tests E2E (StubPrClient). El cambio debe mantener la interfaz o proveer un reemplazo hasta que los tests migren.
4. **OwnerUserId filter**: Los PRs tienen `OwnerUserId` nullable. El endpoint nuevo debe respetar que los PRs visibles para todos (`OwnerUserId = null`) se muestren, igual que hace `ReadForWindowAsync`.

### Preguntas Abiertas

1. **¿Se elimina `IAzureDevOpsPrClient` completamente o se mantiene como fallback?** — Si se elimina, los tests E2E que usan `StubPrClient` y `ThrowingPrClient` deben migrar a stub del nuevo cliente.
2. **¿Los campos PR se extraen de metadata en Application layer o en Infrastructure?** — La lógica de mapeo `PrReviewDto → WorkItem → PullRequestDto` podría ir en Application (nuevo mapper) o en Infrastructure (extender EfWorkItemStore). La Clean Architecture sugiere Application.
3. **¿Se extiende `WorkItemDetailDto` con campos PR opcionales o se crea DTO separado?** — Crear DTO separado evita acoplar el endpoint genérico a PR, pero añade otra clase.
4. **¿Qué pasa con el PrioritySummaryService que también consume PRs?** — También debe migrar al nuevo cliente o endpoint.

### Ready for Proposal

**Sí** — La exploración revela que la infraestructura para leer PRs de BD ya existe (EfWorkItemStore + IWorkItemReader) y el endpoint genérico `/api/workitems` ya funciona. El cambio es crear un endpoint específico con DTO rico y migrar el UI de `IAzureDevOpsPrClient` al nuevo cliente. La propuesta debe incluir la decisión sobre el DTO (nuevo vs. extendido) y el plan de migración de tests.
