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

- [ ] **W1-H4-T1** Definir contrato base de plugin (`IPlugin` o equivalente).  
  **DoD:** interfaz acordada y ubicada en capa correcta.  
  **Riesgo:** contrato ambiguo que cambie cada semana.
- [ ] **W1-H4-T2** Definir modelo inicial de `WorkItem`.  
  **DoD:** entidad/DTO canónico mínimo creado con tests básicos.  
  **Riesgo:** inconsistencia posterior entre conectores.
- [ ] **W1-H4-T3** Implementar registry/catálogo de plugins.  
  **DoD:** kernel descubre o registra plugins explícitamente.  
  **Riesgo:** acoplar orquestación a implementaciones concretas.
- [ ] **W1-H4-T4** Crear plugin dummy de prueba.  
  **DoD:** devuelve un `WorkItem` controlado.  
  **Riesgo:** no tener forma segura de validar el pipeline.
- [ ] **W1-H4-T5** Exponer ejecución “hello kernel” desde API o worker.  
  **DoD:** existe una ruta/flujo ejecutable de punta a punta.  
  **Riesgo:** kernel definido pero no ejecutable.

#### Historia W1-H5 — Preparar autenticación desacoplada de Graph

**Resultado esperado:** login local mockeado y arquitectura lista para proveedor real.

- [ ] **W1-H5-T1** Definir puerto de autenticación/autorización.  
  **DoD:** contrato base ubicado fuera de infraestructura.  
  **Riesgo:** atar seguridad a SDK externo.
- [ ] **W1-H5-T2** Implementar proveedor mock de identidad.  
  **DoD:** devuelve usuario/sesión simulada para desarrollo.  
  **Riesgo:** frenar el avance por falta de credenciales reales.
- [ ] **W1-H5-T3** Integrar login mock en API.  
  **DoD:** endpoint o flujo web funcional en local.  
  **Riesgo:** auth no demostrable desde UI.
- [ ] **W1-H5-T4** Crear pruebas de autorización mínima.  
  **DoD:** acceso permitido/denegado cubierto por tests.  
  **Riesgo:** seguridad simulada sin reglas verificadas.

### Épica W1-E4 — UI visible desde el día 1

#### Historia W1-H6 — Construir dashboard inicial

**Resultado esperado:** una pantalla que muestre que Aura está vivo y qué módulos existen.

- [ ] **W1-H6-T1** Crear layout base del dashboard.  
  **DoD:** existe una vista inicial navegable.  
  **Riesgo:** no tener punto central de demostración.
- [ ] **W1-H6-T2** Mostrar estado de API, Qdrant y auth mock.  
  **DoD:** indicadores visibles y entendibles.  
  **Riesgo:** backend operativo pero invisible.
- [ ] **W1-H6-T3** Añadir panel de progreso por módulo.  
  **DoD:** se ven módulos pendientes/en curso/hechos.  
  **Riesgo:** perder trazabilidad del avance.
- [ ] **W1-H6-T4** Crear primer test Playwright de smoke del dashboard.  
  **DoD:** Playwright abre la app y valida elementos críticos de arranque.  
  **Riesgo:** no detectar roturas tempranas de experiencia básica.

---

## Semana 2 — Ingestión

### Épica W2-E1 — Modelo canónico e ingestión idempotente

#### Historia W2-H1 — Consolidar el modelo canónico de WorkItem

- [ ] **W2-H1-T1** Refinar campos obligatorios del `WorkItem`.  
  **DoD:** origen, prioridad, timestamp y metadatos mínimos definidos.  
  **Riesgo:** modelo insuficiente para summary y triaje.
- [ ] **W2-H1-T2** Añadir tests de normalización e idempotencia.  
  **DoD:** duplicados y campos nulos cubiertos.  
  **Riesgo:** inconsistencias silenciosas.

#### Historia W2-H2 — Crear orquestador de ingestión

- [ ] **W2-H2-T1** Definir contrato de checkpoint store.  
  **DoD:** interfaz lista para persistencia.  
  **Riesgo:** duplicidad en sincronizaciones.
- [ ] **W2-H2-T2** Implementar flujo de ejecución por conector.  
  **DoD:** ingestión invocable de forma controlada.  
  **Riesgo:** pipeline difícil de depurar.
- [ ] **W2-H2-T3** Guardar y recuperar checkpoints.  
  **DoD:** una segunda ejecución no duplica datos.  
  **Riesgo:** pérdida de confianza en la plataforma.

### Épica W2-E2 — Plugins de Teams y Outlook

#### Historia W2-H3 — Implementar plugin de Teams

- [ ] **W2-H3-T1** Definir DTOs/mock payloads de Teams.  
  **DoD:** fixtures controlados disponibles.  
  **Riesgo:** depender demasiado pronto del entorno real.
- [ ] **W2-H3-T2** Mapear payloads de Teams a `WorkItem`.  
  **DoD:** mensajes convertidos con prioridad y contexto.  
  **Riesgo:** pérdida de semántica útil.
- [ ] **W2-H3-T3** Añadir tests de mapeo y errores.  
  **DoD:** casos felices y fallos cubiertos.  
  **Riesgo:** mapeos rotos detectados tarde.

#### Historia W2-H4 — Implementar plugin de Outlook

- [ ] **W2-H4-T1** Definir DTOs/mock payloads de Outlook.  
  **DoD:** correos/eventos de ejemplo versionados.  
  **Riesgo:** dataset pobre para validar reglas.
- [ ] **W2-H4-T2** Mapear correos a `WorkItem`.  
  **DoD:** soporte a remitente, prioridad y deadline inicial.  
  **Riesgo:** heurística demasiado ingenua.
- [ ] **W2-H4-T3** Añadir tests de clasificación inicial.  
  **DoD:** reglas básicas validadas.  
  **Riesgo:** summary inconsistente.

### Épica W2-E3 — Morning Summary visible

#### Historia W2-H5 — Construir el motor de summary

- [ ] **W2-H5-T1** Definir contrato `IMorningSummaryComposer`.  
  **DoD:** puerto de composición definido.  
  **Riesgo:** lógica acoplada a UI o transporte.
- [ ] **W2-H5-T2** Implementar ranking por impacto, deadline y riesgo.  
  **DoD:** output priorizado y testeado.  
  **Riesgo:** resumen plano sin criterio ejecutivo.
- [ ] **W2-H5-T3** Añadir soporte inicial de timezone.  
  **DoD:** test de 09:00 según zona configurada.  
  **Riesgo:** resumen disparado en momentos incorrectos.

#### Historia W2-H6 — Mostrar bandeja y summary en dashboard

- [ ] **W2-H6-T1** Crear vista de bandeja por origen.  
  **DoD:** Teams/Outlook visibles en UI.  
  **Riesgo:** ingestión correcta pero no interpretable.
- [ ] **W2-H6-T2** Crear tarjeta de Morning Summary.  
  **DoD:** resumen visible con ranking y acciones sugeridas.  
  **Riesgo:** imposibilidad de validar utilidad real.
- [ ] **W2-H6-T3** Añadir test Playwright del flujo de ingestión + preview.  
  **DoD:** prueba E2E cubre aparición de items y summary.  
  **Riesgo:** integración rota entre backend y UI.

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

### Épica W4-E2 — Validación E2E y demo

#### Historia W4-H2 — Consolidar suite Playwright

- [ ] **W4-H2-T1** Crear fixtures de demo para journeys completos.  
  **DoD:** dataset reproducible para E2E.  
  **Riesgo:** tests flaky.
- [ ] **W4-H2-T2** Cubrir flujo dashboard → ingestión → summary → focus → reviewer.  
  **DoD:** journey principal automatizado.  
  **Riesgo:** integración final no validada.
- [ ] **W4-H2-T3** Guardar screenshots, traces y artifacts.  
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

### Épica W4-E3 — Cierre documental

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
