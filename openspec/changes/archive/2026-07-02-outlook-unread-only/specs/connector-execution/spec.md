# Delta para connector-execution

## ADDED Requirements

### Requirement: Diff lifecycle tras persistencia

Después de persistir exitosamente el batch de Outlook, la Application MUST obtener los
`ExternalId` pendientes de Outlook, compararlos contra el batch recibido y marcar como
`Completed` los ids ausentes. Este diff solo MUST ejecutarse cuando la ejecución de Graph
no devuelve error.

#### Scenario: Email leído se completa en el siguiente poll exitoso

- DADO Outlook items pendientes con `ExternalId` `A`, `B`, `C`
- CUANDO el batch persistido contiene solo `A` y `B`
- ENTONCES `MarkCompletedAsync` se invoca para `C`
- Y `C` pasa a `Completed`

#### Scenario: Error de Graph salta el diff

- DADO que Graph retorna un error HTTP
- CUANDO `ExecuteConnectorUseCase` procesa el resultado
- ENTONCES el diff no se ejecuta
- Y ningún item cambia de estado a `Completed`

#### Scenario: Inbox-zero completa todos los pendientes de Outlook

- DADO Outlook items en estado `Pending`
- CUANDO Graph retorna 0 correos no leídos sin errores
- ENTONCES todos los pendientes de Outlook se marcan `Completed`

#### Scenario: Primera sincronización vacía no marca nada

- DADO que no existen WorkItems Outlook en `Pending`
- CUANDO Graph retorna 0 correos no leídos sin errores
- ENTONCES `GetPendingExternalIdsAsync` retorna un set vacío
- Y `MarkCompletedAsync` no se invoca

#### Scenario: Items no Outlook no son afectados

- DADO items Teams y Outlook en `Pending`
- CUANDO el batch de Outlook omite un único item Outlook
- ENTONCES `MarkCompletedAsync` recibe solo ese `ExternalId` de Outlook
- Y los items Teams permanecen sin cambios

### Requirement: Filtrado por estado en Application

El filtrado Pending/Completed MUST vivir en Application y persistencia. La UI MUST NOT
aplicar lógica extra de read-state para esconder items completados automáticamente.

#### Scenario: Application pide solo ids pendientes para el diff

- DADO WorkItems Outlook con estados mezclados
- CUANDO el diff prepara su snapshot
- ENTONCES solo usa ids Outlook en `Pending`

#### Scenario: La UI recibe datos ya filtrados

- DADO un item Outlook autocompletado por el diff
- CUANDO la UI consulta datos derivados
- ENTONCES el item completado ya no aparece por el filtro de Application/store
- Y la UI no aplica filtros adicionales
