# Story Backlog — Aura

Este backlog convierte el `StoryPlan.md` en trabajo ejecutable, guiable y verificable. Está organizado por semanas, épicas, historias y subtareas atómicas para que podamos avanzar con control fino y validación visible.

## Cómo usar este backlog

1. Elegir una historia de la semana activa.
2. Ejecutar sus subtareas de arriba hacia abajo.
3. No abrir una subtarea nueva sin haber validado la anterior.
4. Cada subtarea debe dejar evidencia: código, test, pantalla o logs.

## Reglas de ejecución

- Una subtarea = un objetivo técnico claro.
- Si una subtarea toca demasiados frentes, se vuelve a partir.
- Ninguna historia se considera terminada sin UI visible o evidencia verificable.
- Toda historia nueva debe respetar Clean Architecture, observabilidad y testabilidad.

---

## Semana 1 — Cimientos

### Épica W1-E1 — Base de solución y arquitectura

#### Historia W1-H1 — Crear la solución base en .NET 9

**Resultado esperado:** solución compilable con capas limpias y tests separados.

- [x] **W1-H1-T1** Crear la solution `Aura.sln`.  
  **DoD:** archivo de solución creado y visible en repo.  
  **Riesgo:** arrancar sin una convención estructural común.
- [x] **W1-H1-T2** Crear proyectos `Aura.Api`, `Aura.Application`, `Aura.Domain`, `Aura.Infrastructure`, `Aura.Workers`.  
  **DoD:** proyectos creados con target .NET 9.  
  **Riesgo:** diferencias de versión o plantillas inconsistentes.
- [x] **W1-H1-T3** Crear proyectos de test `Aura.UnitTests`, `Aura.IntegrationTests`, `Aura.E2E`, `Aura.ArchitectureTests`.  
  **DoD:** proyectos de test creados y agregados a solución.  
  **Riesgo:** dejar la validación para más tarde.
- [x] **W1-H1-T4** Configurar referencias entre capas respetando Clean Architecture.  
  **DoD:** `Domain` no depende de `Infrastructure`; compilación verde.  
  **Riesgo:** contaminar el modelo desde el primer día.
- [x] **W1-H1-T5** Añadir README corto de arranque técnico.  
  **DoD:** instrucciones mínimas para compilar y arrancar la solución.  
  **Riesgo:** fricción de onboarding desde el inicio.

#### Historia W1-H2 — Asegurar reglas de calidad iniciales

**Resultado esperado:** solución validable con comando único y reglas básicas de calidad.

- [x] **W1-H2-T1** Añadir `.editorconfig` con reglas base.  
  **DoD:** formato y estilo homogéneo en la solución.  
  **Riesgo:** deriva de estilo temprana.
- [x] **W1-H2-T2** Activar analyzers/StyleCop básicos.  
  **DoD:** warnings visibles en build local.  
  **Riesgo:** deuda técnica silenciosa.
- [x] **W1-H2-T3** Definir comando de validación local (`restore/build/test`).  
  **DoD:** existe un flujo reproducible documentado.  
  **Riesgo:** cada desarrollador valida distinto.

### Épica W1-E2 — Entorno local y dependencias técnicas

#### Historia W1-H3 — Levantar Qdrant con Docker

**Resultado esperado:** entorno local reproducible con almacenamiento vectorial listo para integrarse.

- [x] **W1-H3-T1** Crear `docker-compose.yml` con servicio Qdrant.  
  **DoD:** servicio definido con puerto y volumen persistente.  
  **Riesgo:** configuración frágil o no portable.
- [x] **W1-H3-T2** Añadir healthcheck y variables locales.  
  **DoD:** el contenedor expone estado saludable.  
  **Riesgo:** falso positivo de disponibilidad.
- [x] **W1-H3-T3** Documentar arranque y parada del entorno.  
  **DoD:** pasos locales claros en markdown.  
  **Riesgo:** dependencia de memoria operativa.
- [x] **W1-H3-T4** Crear prueba de conectividad desde `Infrastructure`.  
  **DoD:** un test o endpoint simple verifica conexión a Qdrant.  
  **Riesgo:** descubrir tarde que la integración base no funciona.

### Épica W1-E3 — Kernel y autenticación

#### Historia W1-H4 — Montar el skeleton del kernel

**Resultado esperado:** pipeline mínimo capaz de registrar y ejecutar un plugin dummy.

- [x] **W1-H4-T1** Definir contrato base de plugin (`IPlugin` o equivalente).  
  **DoD:** interfaz acordada y ubicada en capa correcta.  
  **Riesgo:** contrato ambiguo que cambie cada semana.
- [x] **W1-H4-T2** Definir modelo inicial de `WorkItem`.  
  **DoD:** entidad/DTO canónico mínimo creado con tests básicos.  
  **Riesgo:** inconsistencia posterior entre conectores.
- [x] **W1-H4-T3** Implementar registry/catálogo de plugins.  
  **DoD:** kernel descubre o registra plugins explícitamente.  
  **Riesgo:** acoplar orquestación a implementaciones concretas.
- [x] **W1-H4-T4** Crear plugin dummy de prueba.  
  **DoD:** devuelve un `WorkItem` controlado.  
  **Riesgo:** no tener forma segura de validar el pipeline.
- [x] **W1-H4-T5** Exponer ejecución “hello kernel” desde API o worker.  
  **DoD:** existe una ruta/flujo ejecutable de punta a punta.  
  **Riesgo:** kernel definido pero no ejecutable.

#### Historia W1-H5 — Preparar autenticación desacoplada de Graph

**Resultado esperado:** login local mockeado y arquitectura lista para proveedor real.

- [x] **W1-H5-T1** Definir puerto de autenticación/autorización.  
  **DoD:** contrato base ubicado fuera de infraestructura.  
  **Riesgo:** atar seguridad a SDK externo.
- [x] **W1-H5-T2** Implementar proveedor mock de identidad.  
  **DoD:** devuelve usuario/sesión simulada para desarrollo.  
  **Riesgo:** frenar el avance por falta de credenciales reales.
- [x] **W1-H5-T3** Integrar login mock en API.  
  **DoD:** endpoint o flujo web funcional en local.  
  **Riesgo:** auth no demostrable desde UI.
- [x] **W1-H5-T4** Crear pruebas de autorización mínima.  
  **DoD:** acceso permitido/denegado cubierto por tests.  
  **Riesgo:** seguridad simulada sin reglas verificadas.

### Épica W1-E4 — UI visible desde el día 1

#### Historia W1-H6 — Construir dashboard inicial

**Resultado esperado:** una pantalla que muestre que Aura está vivo y qué módulos existen.

- [x] **W1-H6-T1** Crear layout base del dashboard.  
  **DoD:** existe una vista inicial navegable.  
  **Riesgo:** no tener punto central de demostración.
- [x] **W1-H6-T2** Mostrar estado de API, Qdrant y auth mock.  
  **DoD:** indicadores visibles y entendibles.  
  **Riesgo:** backend operativo pero invisible.
- [x] **W1-H6-T3** Añadir panel de progreso por módulo.  
  **DoD:** se ven módulos pendientes/en curso/hechos.  
  **Riesgo:** perder trazabilidad del avance.

**Nota:** el smoke browser real con Playwright se mueve a **W4-H2** para tratarlo como un slice específico de tooling y validación E2E, separado del cierre visual/funcional inicial del dashboard.

---

## Semana 2 — Ingestión

### Épica W2-E1 — Modelo canónico e ingestión idempotente

#### Historia W2-H1 — Consolidar el modelo canónico de WorkItem

- [x] **W2-H1-T1** Refinar campos obligatorios del `WorkItem`.  
  **DoD:** origen, prioridad, timestamp y metadatos mínimos definidos.  
  **Riesgo:** modelo insuficiente para summary y triaje.
- [x] **W2-H1-T2** Añadir tests de normalización e idempotencia.  
  **DoD:** duplicados y campos nulos cubiertos.  
  **Riesgo:** inconsistencias silenciosas.

#### Historia W2-H2 — Crear orquestador de ingestión

- [x] **W2-H2-T1** Definir contrato de checkpoint store.  
  **DoD:** interfaz lista para persistencia.  
  **Riesgo:** duplicidad en sincronizaciones.
- [x] **W2-H2-T2** Implementar flujo de ejecución por conector.  
  **DoD:** ingestión invocable de forma controlada.  
  **Riesgo:** pipeline difícil de depurar.
- [x] **W2-H2-T3** Guardar y recuperar checkpoints.  
  **DoD:** una segunda ejecución no duplica datos.  
  **Riesgo:** pérdida de confianza en la plataforma.

### Épica W2-E2 — Plugins de Teams y Outlook

#### Historia W2-H3 — Implementar plugin de Teams

- [x] **W2-H3-T1** Definir DTOs/mock payloads de Teams.  
  **DoD:** fixtures controlados disponibles.  
  **Riesgo:** depender demasiado pronto del entorno real.
- [x] **W2-H3-T2** Mapear payloads de Teams a `WorkItem`.  
  **DoD:** mensajes convertidos con prioridad y contexto.  
  **Riesgo:** pérdida de semántica útil.
- [x] **W2-H3-T3** Añadir tests de mapeo y errores.  
  **DoD:** casos felices y fallos cubiertos.  
  **Riesgo:** mapeos rotos detectados tarde.

#### Historia W2-H4 — Implementar plugin de Outlook

- [x] **W2-H4-T1** Definir DTOs/mock payloads de Outlook.  
  **DoD:** correos/eventos de ejemplo versionados.  
  **Riesgo:** dataset pobre para validar reglas.
- [x] **W2-H4-T2** Mapear correos a `WorkItem`.  
  **DoD:** soporte a remitente, prioridad y deadline inicial.  
  **Riesgo:** heurística demasiado ingenua.
- [x] **W2-H4-T3** Añadir tests de clasificación inicial.  
  **DoD:** reglas básicas validadas.  
  **Riesgo:** summary inconsistente.

### Épica W2-E3 — Morning Summary visible

#### Historia W2-H5 — Construir el motor de summary

- [x] **W2-H5-T1** Definir contrato `IMorningSummaryComposer`.  
  **DoD:** puerto de composición definido.  
  **Riesgo:** lógica acoplada a UI o transporte.
- [x] **W2-H5-T2** Implementar ranking por impacto, deadline y riesgo.  
  **DoD:** output priorizado y testeado.  
  **Riesgo:** resumen plano sin criterio ejecutivo.
- [x] **W2-H5-T3** Añadir soporte inicial de timezone.  
  **DoD:** scheduling según `Project/System Settings` (incluye `timezoneId` y `targetLocalTime`, configurable; default esperado `09:00`), resolución de timezone con cadena configurada -> sistema -> UTC, idempotencia de un único summary por usuario/día local y test de due-state.  
  **Alcance:** sólo scheduling/timezone; no incluye semántica timezone-aware de ventanas de datos ni cambios de ranking/composición.  
  **Riesgo:** resumen disparado en momentos incorrectos.

#### Historia W2-H6 — Mostrar bandeja y summary en dashboard

- [x] **W2-H6-T1** Crear vista de bandeja por origen.  
  **DoD:** Teams/Outlook visibles en UI.  
  **Riesgo:** ingestión correcta pero no interpretable.
- [x] **W2-H6-T2** Crear tarjeta de Morning Summary.  
  **DoD:** resumen visible con ranking y acciones sugeridas.  
  **Riesgo:** imposibilidad de validar utilidad real.
- [x] **W2-H6-T3** Añadir test Playwright del flujo de ingestión + preview.  
  **DoD:** prueba E2E cubre aparición de items y summary.  
  **Riesgo:** integración rota entre backend y UI.

### Épica W2-E4 — Calendar y notificaciones

#### Historia W2-H7 — Reuniones próximas y alertas de calendario

**Resultado esperado:** las reuniones del día aparecen en el dashboard y el sistema avisa con sonido y notificación de browser a los 60, 10 y 5 minutos antes del inicio, sin duplicar avisos entre tabs.

- [ ] **W2-H7-T1** Configurar Graph shared client y User Secrets.  
  **DoD:** `GraphServiceClient` registrado como singleton en Infrastructure con `ClientSecretCredential`; credenciales en User Secrets (no en appsettings); permiso `Calendars.Read` (Application) documentado.  
  **Riesgo:** credenciales filtradas al repo si se usan appsettings.

- [ ] **W2-H7-T2** Implementar dominio Calendar y adapter de Graph.  
  **DoD:** `CalendarEvent`, `MeetingAlertTrigger`, `MeetingAlert` en Domain; puertos `ICalendarEventProvider`, `IMeetingAlertStore`, `IMeetingAlertDispatcher` en Application; `GraphCalendarEventProvider` llamando `/me/calendarView` en Infrastructure; tests unitarios con mock del provider.  
  **Riesgo:** timezone incorrecta en eventos si no se normaliza a UTC en el adapter.

- [ ] **W2-H7-T3** Implementar alert store, SignalR hub y worker.  
  **DoD:** `SqliteMeetingAlertStore` en `aura.db` con PK `(EventId, Trigger, LocalDate)`; `MeetingAlertHub` con grupos por UserId y método `AcknowledgeAlert`; `MeetingAlertWorker` con polling cada 2 min; use cases `CheckAndDispatchMeetingAlertsUseCase` y `GetUpcomingMeetingsUseCase`; tests del store y del use case.  
  **Riesgo:** doble disparo si el worker hace polling concurrente; mitigado por `MarkSentAsync` previo al dispatch.

- [ ] **W2-H7-T4** Implementar notificaciones browser con sonido.  
  **DoD:** `meetingAlert.js` con Web Notification API + `Audio.play()`; `MeetingAlertToast.razor` recibe push de SignalR y llama JS interop; deduplicación: el primer tab en llamar `AcknowledgeAlert` gana, los demás descartan; funciona con el tab minimizado.  
  **Riesgo:** browser puede bloquear notificaciones si el usuario no concedió permiso; el JS debe pedir permiso al cargar.

- [ ] **W2-H7-T5** Mostrar reuniones del día en el dashboard.  
  **DoD:** `UpcomingMeetingsPanel.razor` agregado al dashboard; muestra título, hora de inicio, duración y link de Teams si existe; se refresca cada 5 min; vacío elegante si no hay reuniones.  
  **Riesgo:** panel vacío si `ICalendarEventProvider` falla; debe degradar con mensaje de error, no pantalla rota.

### Épica W2-E5 — Ingestión real con Graph API

#### Historia W2-H8 — Conectar Teams y Outlook reales con Graph delegated

**Resultado esperado:** el usuario se autentica en Microsoft desde la app, Aura lee sus mensajes de Teams y correos de Outlook reales, los normaliza a WorkItem con metadata enriquecida, y el dashboard muestra el resultado con degradación parcial visible por fuente.

- [x] **W2-H8-T1** Crear puertos `IMessageSourceProvider<T>`, `ISyncStateStore`, `ITokenCacheStatus` en Application.  
  **DoD:** interfaces definidas sin dependencias de SDK externo.  
  **Riesgo:** acoplar Application a Graph/MSAL.

- [x] **W2-H8-T2** Crear modelos `SyncResultDto`, `SourceSyncResult`, `SourceSyncState`, `TokenStatus`.  
  **DoD:** records con tests de construcción y estado.  
  **Riesgo:** contrato insuficiente para estados de sync parcial.

- [x] **W2-H8-T3** Extender `GraphConnectorOptions` con `RedirectUri` y `Scopes[]`.  
  **DoD:** opciones de Graph configurables desde appsettings/User Secrets.  
  **Riesgo:** configuración inconsistente entre entornos.

- [x] **W2-H8-T4** Extender `InboxItemPreviewDto` con campos aditivos init-only: `Sender`, `Snippet`, `DeepLink`, `PriorityHint`, `SyncState`.  
  **DoD:** DTOs aditivos sin romper constructores posicionales existentes.  
  **Riesgo:** romper compatibilidad con consumidores actuales del DTO.

- [x] **W2-H8-T5** Implementar `SqliteWorkItemStore` con upsert por `ExternalId`.  
  **DoD:** persistencia real en SQLite, tests de save/upsert/read-back.  
  **Riesgo:** schema insuficiente para consultas por ventana temporal.

- [x] **W2-H8-T6** Implementar `MsalSqliteTokenCache` para cache de tokens MSAL.  
  **DoD:** tokens persistidos en SQLite, tests de persist/recuperación/overwrite.  
  **Riesgo:** cache in-memory pierde tokens entre reinicios.

- [x] **W2-H8-T7** Crear `GraphClientFactory` con `IGraphClientFactory` para `GraphServiceClient` delegado.  
  **DoD:** factory crea cliente desde token cache de usuario, tests con mock.  
  **Riesgo:** `AcquireTokenSilent` falla sin token válido.

- [x] **W2-H8-T8** Registrar MSAL, token cache, y GraphClientFactory en DI bajo `GraphConnector:Enabled`.  
  **DoD:** DI no rota cuando feature flag está deshabilitado.  
  **Riesgo:** resolver de DI falla si opciones no están configuradas.

- [x] **W2-H8-T9** Implementar `GraphTeamsSourceProvider` y `GraphOutlookSourceProvider`.  
  **DoD:** providers leen de Graph API real con mocked HTTP handler, tests unitarios.  
  **Riesgo:** scopes insuficientes o permisos requeridos por admin consent.

- [x] **W2-H8-T10** Extender `TeamsMessageDto` y `OutlookEmailDto` con `Sender`, `BodyPreview`, `WebUrl`/`WebLink`.  
  **DoD:** DTOs ampliados, mapeadores actualizan campos en WorkItem metadata.  
  **Riesgo:** mapeo incorrecto de campos opcionales.

- [x] **W2-H8-T11** Inyectar `IMessageSourceProvider<T>` opcional en `TeamsConnectorAdapter` y `OutlookConnectorAdapter`.  
  **DoD:** adapter usa provider cuando existe, fallback a fixtures, tests de ambas ramas.  
  **Riesgo:** lógica condicional dificulta testabilidad.

- [x] **W2-H8-T12** Actualizar DI de conectores para registro condicional de Graph providers.  
  **DoD:** providers solo se registran cuando Graph está habilitado y configurado.  
  **Riesgo:** registro duplicado o conflicto de implementaciones.

- [x] **W2-H8-T13** Implementar `TriggerSyncUseCase` con agregación multi-source y degradación parcial.  
  **DoD:** un fallo por fuente no bloquea las demás, tests de escenarios parciales.  
  **Riesgo:** propagación incorrecta de errores entre fuentes.

- [x] **W2-H8-T14** Crear `InMemorySyncStateStore` para estado de sync por fuente.  
  **DoD:** store registra timestamp y resultado por fuente.  
  **Riesgo:** estado se pierde entre reinicios (aceptable para primer slice).

- [x] **W2-H8-T15** Crear endpoints `POST /api/sync/now` y `GET /api/sync/status`.  
  **DoD:** trigger manual funcional con feedback operativo, tests de integración.  
  **Riesgo:** endpoint expone información sensible de tokens.

- [x] **W2-H8-T16** Actualizar `ConnectorExecutionWorker` para iterar todos los conectores registrados.  
  **DoD:** worker multi-connector con config-driven identity list.  
  **Riesgo:** hardcoded identity list en lugar de resolución dinámica.

- [x] **W2-H8-T17** Integrar persistencia en `TriggerSyncUseCase`: drain buffer + persistir a `IWorkItemStore`.  
  **DoD:** items ingeridos quedan en SQLite, test de integración sync→preview.  
  **Riesgo:** buffer no se dren correctamente entre ejecuciones.

- [x] **W2-H8-T18** Actualizar `DashboardPreviewReader` para propagar metadata sincronizada a DTOs de preview.  
  **DoD:** campos `Sender`, `Snippet`, `DeepLink`, `PriorityHint`, `SyncState` poblados desde `WorkItem.Metadata`.  
  **Riesgo:** campos no poblados cuando metadata no existe.

- [x] **W2-H8-T19** Actualizar `DashboardPreviewResponse.cs` en UI para reflejar campos opcionales.  
  **DoD:** modelo de UI espejo del DTO de Application.  
  **Riesgo:** desalineación entre DTO de API y modelo de UI.

- [x] **W2-H8-T20** Modificar `InboxPreviewPanel.razor` para renderizar nuevos campos con `data-testid`.  
  **DoD:** sender, snippet, deepLink, syncState visibles con selectores estables.  
  **Riesgo:** selectores inestables rompen tests Playwright paralelos.

- [x] **W2-H8-T21** Crear `SyncStatusPanel.razor` con botón sync-now, progreso por fuente, timestamp.  
  **DoD:** panel operativo con feedback visual de sync.  
  **Riesgo:** HandleSyncNow es placeholder sin wiring funcional.

- [x] **W2-H8-T22** Verificar UX de estado vacío explícito: sync correcto sin datos → UI dice "no data", sin fallback a demo.  
  **DoD:** test E2E valida ausencia de texto "demo" y presencia de mensaje de estado vacío.  
  **Riesgo:** fallback automático a datos demo en ausencia de datos reales.

- [x] **W2-H8-T23** Añadir reglas NetArchTest: `Microsoft.Graph` y `Microsoft.Identity.Client` prohibidos en Application, Domain, Workers, Api, UI.  
  **DoD:** 7 reglas archunit pasando, aislamiento de SDK verificado.  
  **Riesgo:** SDK se filtra a capas superiores por mistake.

- [x] **W2-H8-T24** Scaffold Playwright real-data smoke test: dashboard shell, inbox panel, sync panel.  
  **DoD:** tests compilan, scaffold listo para instalar navegadores y ejecutar.  
  **Riesgo:** tests fallan sin app corriendo (ambiental, no de código).

- [x] **W2-H8-T25** Añadir `data-testid` selectors para suite Playwright controlada/demo.  
  **DoD:** selectores estables: `inbox-preview-item-sender`, `sync-now-button`, `sync-status-panel`, etc.  
  **Riesgo:** selectores renombrados rompen suite paralela.

---

## Semana 3 — Deep Work & PRs

### Épica W3-E1 — Triaje proactivo

#### Historia W3-H1 — Modelar estados de foco

- [ ] **W3-H1-T1** Definir estados `DeepWork`, `WindowOfOpportunity`, `Away`, `Recovery`.  
  **DoD:** estados y transiciones documentados y testeados.  
  **Riesgo:** reglas ambiguas de foco.
- [ ] **W3-H1-T2** Implementar resolver de estado actual.  
  **DoD:** el sistema puede determinar el estado activo.  
  **Riesgo:** decisiones inconsistentes entre sesiones.

#### Historia W3-H2 — Construir motor de interrupciones

- [ ] **W3-H2-T1** Definir scoring de prioridad y atención.  
  **DoD:** fórmula/reglas explicitadas en tests.  
  **Riesgo:** ruido excesivo.
- [ ] **W3-H2-T2** Implementar reglas de interrupción vs cola diferida.  
  **DoD:** el motor produce decisión explicable.  
  **Riesgo:** falta de confianza del usuario.
- [ ] **W3-H2-T3** Registrar razón de decisión para auditoría.  
  **DoD:** cada decisión incluye explicación trazable.  
  **Riesgo:** caja negra difícil de defender.

- [ ] **W3-H2-T4** Add Teams connector content-based preliminary scoring (future, non-authoritative).  
  **DoD:** Teams connector extracts content signals and writes preliminary score metadata only; final `INTERRUPT|QUEUE|DEFER` remains owned by `IInterruptionPolicyEngine`; evidence includes docs + tests for metadata mapping.  
  **Riesgo:** accidentally pushing final triage authority into connector logic.

#### Historia W3-H3 — Exponer foco y cola priorizada en UI

- [ ] **W3-H3-T1** Mostrar estado de foco actual en dashboard.  
  **DoD:** UI indica modo actual y su significado.  
  **Riesgo:** motor invisible.
- [ ] **W3-H3-T2** Mostrar cola diferida y motivo de interrupción.  
  **DoD:** cada item visible con explicación.  
  **Riesgo:** decisiones no auditables.
- [ ] **W3-H3-T3** Añadir Playwright para cambio de estado y decisiones.  
  **DoD:** flujo E2E cubre al menos un caso Deep Work.  
  **Riesgo:** UX rota pese a lógica correcta.

### Épica W3-E2 — Reviewer técnico de PRs

#### Historia W3-H4 — Preparar pipeline de PRs

- [ ] **W3-H4-T1** Definir modelo de evidencia de PR.  
  **DoD:** diff, findings, criterios y decisión comparten contrato.  
  **Riesgo:** reviewer sin estructura consistente.
- [ ] **W3-H4-T2** Crear payloads mock de PR.  
  **DoD:** fixtures listos para pruebas de reviewer.  
  **Riesgo:** depender del ecosistema real demasiado pronto.
- [ ] **W3-H4-T3** Implementar ingesta inicial de PR en el pipeline.  
  **DoD:** un PR de demo entra al sistema.  
  **Riesgo:** reviewer aislado del flujo principal.

#### Historia W3-H5 — Integrar SonarQube y reglas OWASP

- [ ] **W3-H5-T1** Definir puerto `IStaticAnalysisProvider`.  
  **DoD:** contrato desacoplado de SonarQube.  
  **Riesgo:** acoplamiento a proveedor.
- [ ] **W3-H5-T2** Implementar adaptador inicial SonarQube.  
  **DoD:** findings traducidos al modelo interno.  
  **Riesgo:** pérdida de severidad/contexto.
- [ ] **W3-H5-T3** Definir reglas iniciales OWASP.  
  **DoD:** conjunto inicial acotado y explicable.  
  **Riesgo:** falsos positivos masivos.
- [ ] **W3-H5-T4** Combinar findings en decisión final del reviewer.  
  **DoD:** estado final calculado y testeado.  
  **Riesgo:** decisión inconsistente.

#### Historia W3-H6 — Crear panel de reviewer en dashboard

- [ ] **W3-H6-T1** Diseñar tarjeta/resumen del PR analizado.  
  **DoD:** PR visible con score y estado final.  
  **Riesgo:** demo poco clara.
- [ ] **W3-H6-T2** Mostrar evidencias SonarQube/OWASP.  
  **DoD:** findings legibles y agrupados.  
  **Riesgo:** reviewer incomprensible para evaluación.
- [ ] **W3-H6-T3** Añadir Playwright del flujo reviewer.  
  **DoD:** prueba E2E valida carga y lectura de findings.  
  **Riesgo:** fallo tardío en integración UI-reviewer.

---

## Semana 4 — Cierre

### Épica W4-E1 — Observabilidad y diagnósticos

#### Historia W4-H1 — Añadir logs estructurados con correlación

- [ ] **W4-H1-T1** Definir formato mínimo de logs.  
   **DoD:** eventos clave comparten estructura.  
   **Riesgo:** diagnósticos inconsistentes.
- [ ] **W4-H1-T2** Introducir correlation id en API y workers.  
   **DoD:** se puede seguir un flujo extremo a extremo.  
   **Riesgo:** imposible reconstruir incidencias.
- [ ] **W4-H1-T3** Mostrar errores/estado relevante en dashboard o panel técnico.  
   **DoD:** fallos importantes visibles para demo y depuración.  
   **Riesgo:** soporte totalmente dependiente de consola.

### Épica W4-E2 — Infraestructura de ingestión para producción

#### Historia W4-H1bis — Persistencia de checkpoints en base de datos

**Resultado esperado:** checkpoints de ejecución de conectores persistidos durablemente en BD.

- [ ] **W4-H1bis-T1** Diseñar entidad EF Core `ConnectorCheckpointEntity`.  
   **DoD:** entidad con índice compuesto (Connector, Source, Tenant) definida.  
   **Riesgo:** esquema insuficiente para recuperación por tenant.
- [ ] **W4-H1bis-T2** Implementar `DatabaseIngestionCheckpointStore` reemplazando in-memory.  
   **DoD:** implementa `IIngestionCheckpointStore` con persistencia real.  
   **Riesgo:** transacciones race o deadlock en concurrencia.
- [ ] **W4-H1bis-T3** Crear migration de EF Core para tabla de checkpoints.  
   **DoD:** migración ejecutable y versionada.  
   **Riesgo:** inconsistencia entre entornos.
- [ ] **W4-H1bis-T4** Añadir tests de integración para recuperación desde cursor anterior.  
   **DoD:** prueba verifica idempotencia en reinicio de worker.  
   **Riesgo:** pérdida de checkpoint o duplicidad silenciosa.
- [ ] **W4-H1bis-T5** Registrar implementación en DI para producción.  
   **DoD:** `IIngestionCheckpointStore` resuelve a `DatabaseIngestionCheckpointStore` en Prod.  
   **Riesgo:** seguir usando in-memory en producción por olvido.
- [ ] **W4-H1bis-T6** Documentar estrategia de recuperación ante corrupción de checkpoint.  
   **DoD:** manual de troubleshooting para operación.  
   **Riesgo:** incidente sin playbook.

### Épica W4-E3 — Validación E2E y demo

#### Historia W4-H2 — Consolidar suite Playwright

- [ ] **W4-H2-T1** Configurar Playwright para Aura y crear el proyecto base de smoke del dashboard.  
  **DoD:** existe proyecto/configuración Playwright ejecutable en local contra la UI con un caso smoke mínimo.  
  **Riesgo:** seguir posponiendo E2E browser real y descubrir tarde problemas de integración visual.
- [ ] **W4-H2-T2** Crear fixtures de demo para journeys completos.  
  **DoD:** dataset reproducible para E2E.  
  **Riesgo:** tests flaky.
- [ ] **W4-H2-T3** Cubrir flujo dashboard → ingestión → summary → focus → reviewer.  
  **DoD:** journey principal automatizado.  
  **Riesgo:** integración final no validada.
- [ ] **W4-H2-T4** Guardar screenshots, traces y artifacts.  
  **DoD:** evidencia diagnóstica accesible.  
  **Riesgo:** errores difíciles de reproducir.

#### Historia W4-H3 — Preparar Demo Mode

- [ ] **W4-H3-T1** Diseñar escenario demo reproducible.  
  **DoD:** narrativa funcional definida.  
  **Riesgo:** demo improvisada.
- [ ] **W4-H3-T2** Implementar carga de datos semilla end-to-end.  
  **DoD:** un comando deja el entorno listo para demo.  
  **Riesgo:** dependencia de pasos manuales frágiles.
- [ ] **W4-H3-T3** Añadir selector o bandera de Demo Mode.  
  **DoD:** activación simple y visible.  
  **Riesgo:** activar demo con configuración insegura.

### Épica W4-E4 — Cierre documental

#### Historia W4-H4 — Documentar TFM y operación técnica

- [ ] **W4-H4-T1** Documentar arquitectura final y decisiones clave.  
  **DoD:** documento base del TFM actualizado.  
  **Riesgo:** perder racional técnico.
- [ ] **W4-H4-T2** Documentar estrategia TDD y cobertura E2E.  
  **DoD:** Playwright y tests quedan explicados.  
  **Riesgo:** evaluador sin trazabilidad metodológica.
- [ ] **W4-H4-T3** Crear checklist de arranque y demo.  
  **DoD:** cualquier persona puede reproducir la entrega.  
  **Riesgo:** conocimiento encerrado en una sola cabeza.

---

## Siguiente paso recomendado

Empezar por **W1-H4-T1** y no abrir más de una historia a la vez hasta cerrar el skeleton del kernel. Esa disciplina es la que mantiene el control del proyecto.
