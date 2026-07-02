# Delta Especificación: outlook-unread-only

## outlook-connector-mapping (Añadidos)

| Requisito | Restricción | Fuerza |
|---|---|---|
| Filtro Graph solo no-leídos | `$filter=isRead eq false` en `/me/mailFolders/inbox/messages`; `isRead` en `$select` | MUST |
| DTO OutlookEmailDto.IsRead | Mapear `isRead` desde payload Graph al DTO | MUST |

#### Escenario: Graph query filtra solo no leídos
- DADO un usuario con correos en su bandeja de entrada
- CUANDO `GraphOutlookSourceProvider` construye la query Graph
- ENTONCES la URL contiene `$filter=isRead eq false` e `isRead` en `$select`

#### Escenario: Email leído en Outlook no aparece en resultado
- DADO un email con `isRead = true` en Outlook
- CUANDO Graph retorna la lista filtrada
- ENTONCES ese email no está incluido en el resultado

#### Escenario: DTO mapea IsRead correctamente
- DADO un payload Graph con `isRead = false`
- CUANDO el adaptador mapea al DTO
- ENTONCES `OutlookEmailDto.IsRead` es `false`

---

## connector-execution (Añadidos)

| Requisito | Restricción | Fuerza |
|---|---|---|
| Diff lifecycle tras persistencia | Obtener pending ExternalIds, comparar vs batch recibido, marcar ausentes como Completed. Solo si el batch se persistió sin errores | MUST |
| Filtrado por estado en Application | `IWorkItemReader.GetByStatusAsync(Pending)` filtra pendientes. UI no aplica filtros adicionales de lectura | MUST |

#### Escenario: Email leído en Outlook se completa en el siguiente poll
- DADO Outlook items Pending con ExternalIds A, B, C
- CUANDO el batch persistido contiene solo A, B (C fue leído en Outlook)
- ENTONCES `MarkCompletedAsync(["C"])` se invoca y C pasa a `Completed`

#### Escenario: Correo no leído nuevo se persiste como Pending
- DADO un nuevo email no leído en Outlook con ExternalId X
- CUANDO el batch se persiste exitosamente
- ENTONCES X se almacena con estado `Pending` y diff no lo completa

#### Escenario: Graph con error no ejecuta diff
- DADO que Graph retorna un error HTTP (4xx/5xx)
- CUANDO `ExecuteConnectorUseCase` detecta el error
- ENTONCES el diff NO se ejecuta y ningún WorkItem cambia de estado

#### Escenario: Inbox-zero completa todos los pendientes
- DADO 3 Outlook items en estado `Pending`
- CUANDO Graph retorna 0 correos no leídos sin errores
- ENTONCES los 3 items se marcan `Completed`

#### Escenario: Primera sincronización con inbox vacío
- DADO que no existen WorkItems Outlook en estado `Pending`
- CUANDO Graph retorna 0 correos sin errores
- ENTONCES `GetPendingExternalIdsAsync` retorna lista vacía
- Y no se invoca `MarkCompletedAsync`

#### Escenario: Items de Teams no afectados por diff de Outlook
- DADO Teams items T1, T2 y Outlook items O1, O2 en `Pending`
- CUANDO el batch de Outlook contiene solo O1
- ENTONCES `MarkCompletedAsync` recibe solo `["O2"]`; T1 y T2 permanecen `Pending`

---

## work-item-persistence (Añadidos)

| Requisito | Restricción | Fuerza |
|---|---|---|
| GetPendingExternalIdsAsync | Retorna `IReadOnlySet<string>` con ExternalIds de items en estado Pending | MUST |
| MarkCompletedAsync(IEnumerable<string>) | Cambia estado a Completed para los ExternalIds dados. Inexistentes se ignoran sin error | MUST |

#### Escenario: GetPendingExternalIdsAsync retorna solo pendientes
- DADO items con ExternalIds A, B en Pending y C en Completed
- CUANDO se invoca `GetPendingExternalIdsAsync`
- ENTONCES el resultado contiene A y B pero no C

#### Escenario: Sin pendientes retorna set vacío
- DADO que ningún WorkItem tiene estado Pending
- CUANDO se invoca `GetPendingExternalIdsAsync`
- ENTONCES se retorna un `IReadOnlySet<string>` vacío

#### Escenario: SQLite implementa con SELECT WHERE Status = 0
- DADO la implementación SQLite
- CUANDO se ejecuta `GetPendingExternalIdsAsync`
- ENTONCES la query es `SELECT ExternalId FROM WorkItems WHERE Status = 0`

#### Escenario: InMemory filtra por Status == Pending
- DADO la implementación InMemory
- CUANDO se ejecuta `GetPendingExternalIdsAsync`
- ENTONCES itera el store interno y retorna solo items con `Status == Pending`

#### Escenario: MarkCompletedAsync cambia estado en lote
- DADO items A, B, C en Pending
- CUANDO `MarkCompletedAsync([A, C])`
- ENTONCES A y C pasan a Completed; B permanece Pending

#### Escenario: ExternalId inexistente es ignorado
- DADO items A en Pending
- CUANDO `MarkCompletedAsync([A, "nonexistent"])`
- ENTONCES A cambia a Completed y ningún error se lanza

#### Escenario: SQLite MarkCompletedAsync usa UPDATE IN
- DADO la implementación SQLite recibe [A, B, C]
- CUANDO ejecuta `MarkCompletedAsync`
- ENTONCES emite `UPDATE WorkItems SET Status = @c WHERE ExternalId IN (@a, @b, @c)`

#### Escenario: InMemory MarkCompletedAsync itera y actualiza
- DADO la implementación InMemory recibe [A, C]
- CUANDO ejecuta `MarkCompletedAsync`
- ENTONCES itera sobre los ExternalIds provistos y actualiza solo los que existen
