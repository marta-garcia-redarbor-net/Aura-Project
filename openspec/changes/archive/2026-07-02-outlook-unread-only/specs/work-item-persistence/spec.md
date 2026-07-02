# Delta para work-item-persistence

## ADDED Requirements

### Requirement: `GetPendingExternalIdsAsync`

`IWorkItemStore` MUST exponer `GetPendingExternalIdsAsync(WorkItemSourceType source,
CancellationToken ct)` y devolver un `IReadOnlySet<string>` con los `ExternalId` de los
items del source pedido cuyo estado persistido sea `"Pending"`.

#### Scenario: Retorna solo pendientes del source pedido

- DADO items `A` y `B` en `Pending` y `C` en `Completed` para el mismo source
- CUANDO se invoca `GetPendingExternalIdsAsync`
- ENTONCES el resultado contiene `A` y `B`
- Y no contiene `C`

#### Scenario: Sin pendientes retorna set vacío

- DADO que ningún WorkItem del source está en `Pending`
- CUANDO se invoca `GetPendingExternalIdsAsync`
- ENTONCES se retorna un `IReadOnlySet<string>` vacío

#### Scenario: SQLite usa estados TEXT y filtro por source

- DADO la implementación SQLite
- CUANDO ejecuta `GetPendingExternalIdsAsync`
- ENTONCES filtra `Status = 'Pending'`
- Y también filtra por el source solicitado

#### Scenario: InMemory filtra por status y source

- DADO la implementación InMemory con datos mezclados
- CUANDO ejecuta `GetPendingExternalIdsAsync`
- ENTONCES retorna solo items con `Status == Pending` del source pedido

### Requirement: `MarkCompletedAsync(IEnumerable<string>)`

`IWorkItemStore` MUST exponer `MarkCompletedAsync(IReadOnlySet<string> externalIds,
WorkItemSourceType source, CancellationToken ct)` para pasar a `"Completed"` los ids
pendientes provistos del source indicado. Los ids inexistentes MUST ignorarse sin error.

#### Scenario: Cambia estado en lote

- DADO items `A`, `B`, `C` en `Pending`
- CUANDO `MarkCompletedAsync` recibe `A` y `C`
- ENTONCES `A` y `C` pasan a `Completed`
- Y `B` permanece `Pending`

#### Scenario: `ExternalId` inexistente se ignora

- DADO item `A` en `Pending`
- CUANDO `MarkCompletedAsync` recibe `A` y `nonexistent`
- ENTONCES `A` cambia a `Completed`
- Y no se lanza ningún error por `nonexistent`

#### Scenario: Respeta aislamiento por source

- DADO items pendientes de múltiples sources
- CUANDO `MarkCompletedAsync` se ejecuta para un source concreto
- ENTONCES solo se actualizan los ids coincidentes de ese source
