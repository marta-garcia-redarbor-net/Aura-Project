# Delta para outlook-connector-mapping

## ADDED Requirements

### Requirement: Filtro de inbox no leído

La consulta a Microsoft Graph para Outlook MUST usar `/me/mailFolders/inbox/messages`
con `$filter=isRead eq false` e incluir `isRead` dentro de `$select`.

#### Scenario: Graph query filtra solo no leídos

- DADO un usuario con correos en su bandeja de entrada
- CUANDO `GraphOutlookSourceProvider` construye la query Graph
- ENTONCES la URL contiene `$filter=isRead eq false`
- Y el endpoint apunta a `/me/mailFolders/inbox/messages`
- Y `isRead` aparece en `$select`

#### Scenario: Email leído no aparece en el batch ingerido

- DADO un correo con `isRead = true` en Outlook
- CUANDO Graph retorna la lista filtrada
- ENTONCES ese correo no forma parte del batch entregado al adaptador

### Requirement: Mapeo de `OutlookEmailDto.IsRead`

`OutlookEmailDto` MUST exponer `IsRead` y el adaptador MUST mapear el valor Graph
`isRead` a esa propiedad.

#### Scenario: DTO mapea `IsRead = false`

- DADO un payload Graph con `isRead = false`
- CUANDO el adaptador mapea el DTO
- ENTONCES `OutlookEmailDto.IsRead` vale `false`

#### Scenario: DTO mapea `IsRead = true`

- DADO un payload Graph con `isRead = true`
- CUANDO el adaptador mapea el DTO
- ENTONCES `OutlookEmailDto.IsRead` vale `true`
