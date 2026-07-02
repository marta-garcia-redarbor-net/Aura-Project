# Propuesta: outlook-unread-only

## Intención

Hacer que la ingesta de Outlook solo procese correos **no leídos** de la bandeja de entrada, y retire automáticamente como `Completed` los ítems que se marquen como leídos en Outlook, detectados por su ausencia en polls posteriores de Graph.

## Motivación

Reducir carga cognitiva del equipo — la UI solo debe mostrar pendientes que realmente están sin leer. Los ítems se retiran automáticamente al leerse en Outlook, sin intervención manual. Cuando la bandeja esté al día, mostrar zero-state positivo: "Todo al día, no tienes emails pendientes."

## Reglas de negocio

- El usuario **nunca** completa items manualmente en Aura — el estado solo cambia en Outlook (fuente de verdad)
- Respuesta vacía de Graph sin errores → todos los Outlook pendientes se marcan `Completed` (inbox-zero)
- Siempre filtrar solo no-leídos desde el primer fetch
- Diff lifecycle tras persistir: (1) fetch no-leídos, (2) persistir, (3) diff pending vs nuevo batch, (4) marcar ausentes como `Completed`
- `Application` filtra por estado, no la UI

## Alcance

### In Scope
- DTO: agregar `IsRead` a `OutlookEmailDto`
- Graph query: `GraphOutlookSourceProvider` → `/me/mailFolders/inbox/messages` con `$filter=isRead eq false`, incluir `isRead` en `$select`
- Store port: agregar `GetPendingExternalIdsAsync()` + `MarkCompletedAsync(IEnumerable<string> externalIds)` a `IWorkItemStore`
- Store implementations: adaptar `SqliteWorkItemStore` e `InMemoryWorkItemStore`
- Use-case: lógica de diff en `ExecuteConnectorUseCase` tras persistir cada batch
- Tests: unitarios por capa + integración con SQLite

### Out of Scope
- Cambios de UI (zero-state vendrá de Stitch separadamente)
- Slack connector
- Fuentes que no sean Outlook
- Non-Outlook sources

## Capacidades

> Contrato entre proposal y specs. Basado en `openspec/specs/` existente.

### Nuevas Capacidades
Ninguna.

### Capacidades Modificadas
- `outlook-connector-mapping`: agregar `IsRead` al DTO de entrada, añadir `$filter=isRead eq false` + campo `isRead` en `$select` de la query Graph
- `connector-execution`: agregar diff lifecycle tras persistencia — recolectar `ExternalIds` pendientes, comparar contra el batch recibido, marcar ausentes como `Completed`
- `work-item-persistence`: agregar `GetPendingExternalIdsAsync()` y `MarkCompletedAsync(IEnumerable<string>)` al port `IWorkItemStore` e implementar en SQLite e InMemory

## Enfoque

Opción C — endpoint solo de inbox de Graph + `$filter=isRead eq false` en la query + diff lifecycle ejecutado tras persistir cada batch exitoso. El diff usa `IWorkItemStore` existente para obtener los `ExternalIds` pendientes y llama `MarkAutoCompleted()` en los que ya no aparecen en el nuevo batch.

## Áreas Afectadas

| Área | Impacto | Descripción |
|------|---------|-------------|
| `src/Infrastructure/Adapters/Connectors/Graph/` | Modificado | `GraphOutlookSourceProvider`: filtrar no-leídos, `OutlookEmailDto`: agregar `IsRead` |
| `src/Application/Ports/IWorkItemStore.cs` | Modificado | Nuevos métodos: `GetPendingExternalIdsAsync`, `MarkCompletedAsync` |
| `src/Application/UseCases/ExecuteConnectorUseCase.cs` | Modificado | Agregar diff lifecycle tras persistencia exitosa |
| `src/Infrastructure/Persistence/Sqlite/SqliteWorkItemStore.cs` | Modificado | Implementar nuevos métodos del store |
| `tests/Aura.UnitTests/` | Nuevo | Tests de diff lifecycle, DTO mapping y store |
| `tests/Aura.IntegrationTests/` | Nuevo | Test de integración SQLite + diff |
| `tests/Aura.ArchitectureTests/` | Modificado | Verificar que nuevos métodos respetan Clean Architecture |

## Riesgos

| Riesgo | Probabilidad | Mitigación |
|--------|-------------|------------|
| Respuesta vacía de Graph por error marca todo como Completed | Baja | Verificar que la respuesta no tenga error HTTP antes de ejecutar diff |
| SQLite upsert existente ignora items que ya no existen | Media | `MarkCompletedAsync` opera por `ExternalId` sobre `IWorkItemReader` con filtro `Pending`, no depende de upsert |
| Race condition entre polls simultáneos | Baja | Cada ciclo de ejecución usa su propio scope DI; diff opera sobre el snapshot de `Pending` al inicio del ciclo |

## Plan de Rollback

1. Revertir cambios en `GraphOutlookSourceProvider` para volver a query sin filtro `isRead`
2. Revertir adiciones al port `IWorkItemStore`
3. Revertir lógica de diff en `ExecuteConnectorUseCase`
4. Revertir implementaciones SQLite e InMemory
5. Ejecutar `dotnet test Aura.sln` para verificar

## Dependencias

- Ninguna externa nueva. Depende de `IWorkItemStore.FindByExternalIdAsync` e `IWorkItemReader` (status filter) existentes.
- `WorkItem.MarkAutoCompleted()` ya está definido en dominio.

## Criterios de Éxito

- [ ] Solo correos no leídos del inbox son ingeridos
- [ ] Items leídos en Outlook se marcan `Completed` en el siguiente poll exitoso
- [ ] Graph vacío sin errores marca todos los pendientes como `Completed`
- [ ] Graph vacío con error NO marca nada como `Completed`
- [ ] Tests unitarios, de integración y arquitectura pasan
