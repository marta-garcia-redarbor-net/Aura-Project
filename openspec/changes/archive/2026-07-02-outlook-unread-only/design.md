# Diseño: outlook-unread-only

## Enfoque Técnico

Modificar el pipeline de ingesta Outlook para que **solo procese correos no leídos** del inbox de Outlook y **complete automáticamente** los ítems que desaparecen del resultado de Graph (porque fueron leídos). El cambio abarca cuatro capas: DTO, query Graph, puerto de persistencia y caso de uso.

## Decisiones Arquitectónicas

| Opción | Alternativa | Decisión |
|--------|-------------|----------|
| `ExecuteConnectorUseCase` conoce "outlook" por string | Pasar discriminador por interfaz | Aceptado — `identity.Connector` ya existe y es el mismo pattern usado en adapter dispatch |
| `MarkCompletedAsync` recibe `WorkItemSourceType` | No recibir source y confiar en unicidad de ExternalId | Aceptado — la SQL filtra por source como safety net |
| Batch IDs capturados antes del drain del buffer | Extraer IDs del `ConnectorExecutionResult` | Buffer drain — `ConnectorExecutionResult` no expone ExternalIds |

## Flujo de Datos (Diff Lifecycle)

```
ExecuteConnectorUseCase.ExecuteAsync()
  │
  ├── adapter.ExecuteAsync(request)      ← Graph fetch con $filter=isRead eq false
  │     └── GraphOutlookSourceProvider.FetchAsync()
  │           └── GET /me/mailFolders/inbox/messages?$filter=isRead eq false&$select=...
  │
  ├── [capturar batchExternalIds del buffer]
  │
  ├── PersistWorkItemsAsync()            ← upsert batch en store
  │
  ├── ¿connector == "outlook" y no hubo error Graph?
  │   └── SÍ → RunDiffLifecycleAsync()
  │         ├── pendingIds = store.GetPendingExternalIdsAsync(OutlookEmail)
  │         ├── absentIds = pendingIds.Except(batchExternalIds)
  │         └── si absentIds.Count > 0 → store.MarkCompletedAsync(absentIds, OutlookEmail)
  │
  └── PersistCheckpointAsync()
```

## Cambios por Archivo

| Archivo | Acción | Descripción |
|---------|--------|-------------|
| `src/Infrastructure/Adapters/Connectors/Outlook/OutlookEmailDto.cs` | Modificar | Agregar `bool IsRead { get; init; }` |
| `src/Infrastructure/Adapters/Connectors/Graph/GraphOutlookSourceProvider.cs` | Modificar | Endpoint → `/me/mailFolders/inbox/messages`; agregar `$filter=isRead eq false` e `isRead` en `$select`; mapear `msg.IsRead` al DTO |
| `src/Application/Ports/IWorkItemStore.cs` | Modificar | Agregar `GetPendingExternalIdsAsync` y `MarkCompletedAsync` |
| `src/Infrastructure/Adapters/WorkItems/SqliteWorkItemStore.cs` | Modificar | Implementar ambos métodos con SQL; agregar columna `UpdatedAt` vía migración de schema |
| `src/Infrastructure/Adapters/WorkItems/InMemoryWorkItemStore.cs` | Modificar | Implementar ambos métodos in-memory |
| `src/Application/UseCases/ConnectorExecution/ExecuteConnectorUseCase.cs` | Modificar | Agregar `RunDiffLifecycleAsync` tras persistencia exitosa; capturar batch IDs del buffer |

## Contratos / Interfaces

```csharp
// IWorkItemStore — métodos nuevos
public interface IWorkItemStore
{
    // ... existentes ...

    /// <summary>Retorna los ExternalIds de ítems Pending para un source type.</summary>
    Task<IReadOnlySet<string>> GetPendingExternalIdsAsync(
        WorkItemSourceType source, CancellationToken ct);

    /// <summary>Marca como Completed los ítems Pending con los ExternalIds dados.
    /// ExternalIds inexistentes se ignoran.</summary>
    Task MarkCompletedAsync(
        IReadOnlySet<string> externalIds, WorkItemSourceType source, CancellationToken ct);
}
```

```csharp
// Migración de schema SQLite
public static void MigrateAddUpdatedAt(SqliteConnection connection)
{
    using var cmd = connection.CreateCommand();
    cmd.CommandText = "ALTER TABLE WorkItems ADD COLUMN UpdatedAt TEXT;";
    cmd.ExecuteNonQuery();
}
```

**Nota**: Status se almacena como TEXT ("Pending", "Completed") — las queries deben usar string, no int.

## Estrategia de Testing

| Capa | Qué probar | Enfoque |
|------|-----------|---------|
| Unit (Source) | `GraphOutlookSourceProvider` construye URL con `$filter=isRead eq false` y `isRead` en select | Mock `IGraphClientFactory`, verificar configuración de request |
| Unit (DTO) | `OutlookEmailDto.IsRead` se mapea correctamente desde payload | Test de mapeo directo |
| Unit (Store) | `GetPendingExternalIdsAsync` retorna solo Pending del source correcto | Test con datos en memoria/SQLite |
| Unit (Store) | `MarkCompletedAsync` cambia status solo para los IDs dados | Test con mezcla de IDs existentes/inexistentes |
| Unit (UseCase) | Diff lifecycle: pendingIds - batchIds = absent → MarkCompletedAsync | Mock de `IWorkItemStore`, verificar llamada |
| Unit (UseCase) | Graph con error NO ejecuta diff | Adapter retorna Failure, verificar que NO se llama MarkCompletedAsync |
| Unit (UseCase) | Diff solo para Outlook, no para Teams | Identity.Connector = "teams", verificar que NO se ejecuta diff |
| Integración | SQLite round-trip completo| Base SQLite real, insertar items, ejecutar diff, verificar estado final |

## Manejo de Errores

| Escenario | Comportamiento |
|-----------|---------------|
| Graph error HTTP (4xx/5xx) | `adapterResult.Status = Failure` → diff se salta, pending items intactos |
| SQLite transiente en `MarkCompletedAsync` | Excepción propaga al catch de `ExecuteAsync` → checkpoint NO se persiste, reintento en próximo ciclo |
| InMemory | No aplica errores transientes (testing) |

## Migración / Rollout

- Schema: `ALTER TABLE WorkItems ADD COLUMN UpdatedAt TEXT` ejecutado en `InitializeSchema`
- Rollback: revertir commits por capa, no requiere rollback de datos (solo se agrega columna)

## Preguntas Abiertas

- Ninguna.
