# StoryPlan — Aura en 4 semanas

Este plan organiza Aura en 4 sprints de 7 días con foco en control técnico, visibilidad temprana y entregas pequeñas. La UI crecerá en paralelo al backend para que cada semana exista una forma visible de validar progreso desde pantalla.

## Criterios operativos del plan

- Trabajar en slices verticales pequeños: contrato + caso de uso + adaptador + UI mínima + test.
- Evitar tareas grandes y opacas: cada tarea debe poder revisarse, reorientarse o pausarse sin romper el plan.
- Construir dashboard/UI desde la semana 1 para observar estado del sistema, mocks, ingestión, triaje y reviewer.
- Toda tarea cerrada debe incluir evidencia verificable: código, test, telemetría o demo visible.

## Vista ejecutiva

| Semana | Objetivo | Resultado visible |
| --- | --- | --- |
| 1. Cimientos | Base técnica y arquitectura ejecutable | Dashboard inicial, scaffolding auth, kernel esqueleto, entorno listo |
| 2. Ingestión | Entradas reales/mock de Teams y Outlook + summary + realineación delegated auth | Bandeja de ingestión, tarjeta de Morning Summary y flujo base de Entra ID/Graph |
| 3. Deep Work & PRs | Decisión de interrupciones + reviewer técnico | Vista de foco, cola priorizada y panel de revisión PR |
| 4. Cierre | Observabilidad, E2E, documentación y demo | Logs trazables, flujos Playwright, demo end-to-end |

---

## Semana 1 — Cimientos

### Objetivo

Dejar una base ejecutable sobre .NET 9 con Docker, Qdrant, contratos de autenticación listos para el modelo delegado de Entra ID y skeleton del kernel para que la semana 2 no empiece sobre arena.

### Tareas

1. **Bootstrap de solución .NET 9 y estructura Clean Architecture**  
   **[Prioridad: P0]**  
   **DoD:** solución creada con proyectos `Aura.Api`, `Aura.Application`, `Aura.Domain`, `Aura.Infrastructure`, `Aura.Workers`, `Aura.UnitTests`, `Aura.IntegrationTests`, `Aura.E2E`, `Aura.ArchitectureTests`; compilación verde; referencias entre capas validadas.  
   **Riesgo:** arrastrar una estructura incorrecta desde el inicio y contaminar dependencias entre capas.

2. **Docker Compose con Qdrant y configuración local reproducible**  
   **[Prioridad: P0]**  
   **DoD:** `docker-compose` levanta Qdrant, variables documentadas, healthcheck operativo y configuración consumible desde `Aura.Infrastructure`.  
   **Riesgo:** bloqueo temprano por networking, puertos o configuración inconsistente entre local y CI.

3. **Skeleton del Kernel de Aura**  
   **[Prioridad: P0]**  
   **DoD:** contratos base definidos para plugins, work items normalizados, pipeline de ejecución, scheduler y puertos principales; existe un flujo “hello kernel” ejecutable de punta a punta.  
   **Riesgo:** diseñar un kernel demasiado abstracto o demasiado acoplado a Graph/Teams.

4. **Auth desacoplada con scaffolding temporal para desarrollo**  
   **[Prioridad: P0]**  
   **DoD:** autenticación y autorización desacopladas por puerto/adaptador; existe scaffolding temporal para avanzar en local; los contratos quedan preparados para el flujo real delegado de Entra ID sin redefinir el target.  
   **Riesgo:** convertir el scaffolding local en arquitectura de destino y generar deuda de identidad.

5. **Dashboard inicial de progreso técnico**  
   **[Prioridad: P1]**  
   **DoD:** pantalla inicial con estado de servicios, estado del scaffolding/auth local, estado de Qdrant y módulos pendientes/en curso/completados.  
   **Riesgo:** dejar la UI para el final y perder visibilidad del avance real.

6. **Pipeline base de calidad y observabilidad mínima**  
   **[Prioridad: P1]**  
   **DoD:** logs estructurados mínimos, `EditorConfig`, analyzers, convención de tests y comando único para validar solución localmente.  
   **Riesgo:** deuda técnica temprana que luego haga lento cada sprint.

### Entregable visible de la semana

- Dashboard arrancando con indicadores de entorno.
- Login/scaffolding local funcionando sin competir con el target delegado real.
- Kernel mínimo aceptando un plugin dummy.

---

## Semana 2 — Ingestión

### Objetivo

Implementar el flujo de ingestión inicial para Teams y Outlook, normalizar eventos, materializar el sistema de Morning Summary y aterrizar el flujo real de autenticación delegada + Graph sobre el despliegue local Docker-first.

### Tareas

1. **Contrato canónico de WorkItem y normalización de eventos**  
   **[Prioridad: P0]**  
   **DoD:** modelo canónico versionado; mappers definidos; casos de edge para idempotencia y campos obligatorios cubiertos con tests.  
   **Riesgo:** inconsistencias entre conectores que luego rompan triaje y summary.

2. **Plugin de Teams con datos mockeados/controlados**  
   **[Prioridad: P0]**  
   **DoD:** conector obtiene mensajes/eventos mock, transforma a `WorkItem`, guarda checkpoint y expone errores manejables.  
   **Riesgo:** semántica incorrecta de mensajes, menciones o prioridades.

3. **Plugin de Outlook con datos mockeados/controlados**  
   **[Prioridad: P0]**  
   **DoD:** correos/eventos mock se ingieren, clasifican y normalizan con reglas explícitas; soporte inicial a prioridad, remitente y deadlines.  
   **Riesgo:** sobrecargar el modelo con heurísticas pobres o demasiado rígidas.

4. **Orquestador de ingestión con checkpoints e idempotencia**  
   **[Prioridad: P0]**  
   **DoD:** ejecución repetida no duplica items; existe almacenamiento de checkpoint; fallos parciales no corrompen estado.  
   **Riesgo:** duplicados silenciosos que destruyan confianza en el sistema.

5. **Motor inicial de Morning Summary**  
   **[Prioridad: P0]**  
   **DoD:** composición diaria según `Project/System Settings` (incluye `timezoneId` y `targetLocalTime`, configurable; default esperado `09:00`), resolución de timezone con cadena configurada -> sistema -> UTC, emisión idempotente de un único summary por usuario/día local y salida serializable; tests de composición y scheduling/timezone.  
   **Alcance del slice inicial de timezone:** scheduling/due-state e idempotencia; no abarca semántica timezone-aware de ventanas de datos o ranking.  
   **Riesgo:** producir un resumen plano, ruidoso y sin valor ejecutivo.

6. **Vista UI de bandeja de ingestión y preview del Morning Summary**  
   **[Prioridad: P1]**  
   **DoD:** dashboard muestra items ingeridos por origen, último sync, errores y preview del summary del día.  
   **Riesgo:** tener backend correcto pero sin forma de validar visualmente si lo que entra tiene sentido.

7. **Telemetría de ingestión y summary**  
   **[Prioridad: P1]**  
   **DoD:** logs y métricas mínimas para tiempo de sync, items procesados, duplicados evitados y summaries generados.  
   **Riesgo:** operar a ciegas ante fallos de polling, ranking o checkpoints.

8. **Autenticación delegada end-to-end con Entra ID**  
   **[Prioridad: P0]**  
   **DoD:** primer login interactivo en `Aura.UI`; `Aura.Api` valida JWT real; `oid` se usa como identidad canónica; cache MSAL persistida en SQLite; renovación silent antes de re-login; re-auth requerida si la renovación falla.  
   **Riesgo:** seguir construyendo sobre auth mock o identidades inconsistentes entre hosts.

9. **Integración de Microsoft Graph con tokens delegados del usuario**  
   **[Prioridad: P0]**  
   **DoD:** Calendar/Teams/Outlook usan tokens delegados; `ClientId` y `TenantId` salen de la App Registration; no se requiere `ClientSecret`; workers reutilizan cache delegada en vez de credenciales app-only.  
   **Riesgo:** documentar una cosa e implementar otra, especialmente alrededor de renovación de token y contexto de usuario.

10. **Despliegue local Docker-first con hosts separados**  
    **[Prioridad: P0]**  
    **DoD:** `Aura.UI`, `Aura.Api` y `Aura.Workers` corren separados con Docker Compose, comparten configuración coherente de Entra ID/Graph y persisten SQLite en volúmenes locales.  
    **Riesgo:** colapsar la topología de hosts o inventar infraestructura que todavía no forma parte del alcance.

### Entregable visible de la semana

- Dashboard mostrando feed de Teams/Outlook.
- Preview del Morning Summary.
- Estado de último sync y errores visibles.
- Flujo base de Entra ID + Graph delegado funcionando sobre el entorno local Docker-first.

---

## Semana 3 — Deep Work & PRs

### Objetivo

Convertir la ingestión en decisiones de atención útiles y habilitar una primera versión del reviewer técnico para PRs con foco en seguridad y evidencia.

### Tareas

1. **Modelo de estado de foco: Deep Work vs Window of Opportunity**  
   **[Prioridad: P0]**  
   **DoD:** estados y transiciones definidas; políticas configurables; tests de transición y reglas por severidad/contexto.  
   **Riesgo:** comportamiento errático que interrumpa cuando no debe o silencie urgencias reales.

2. **Motor de triaje proactivo**  
   **[Prioridad: P0]**  
   **DoD:** scoring de prioridad implementado; interrupciones solo cuando las políticas lo permiten; cola diferida para low/medium items.  
   **Riesgo:** ruido excesivo que destruya el valor del producto.

3. **UI de foco y cola priorizada**  
   **[Prioridad: P1]**  
   **DoD:** dashboard muestra estado actual de foco, por qué un item fue interrumpido o diferido, y backlog priorizado.  
   **Riesgo:** reglas invisibles que impidan auditar decisiones del motor.

4. **Conector inicial de PRs y modelo de evidencia**  
   **[Prioridad: P0]**  
   **DoD:** PR mockeado/real de laboratorio entra al pipeline; diff, metadatos y criterios de aceptación quedan representados en un modelo común.  
   **Riesgo:** reviewer sin trazabilidad suficiente para justificar decisiones.

5. **Integración de SonarQube como proveedor de análisis estático**  
   **[Prioridad: P0]**  
   **DoD:** adaptador consulta findings, severidades y quality gate; resultado se integra al modelo de evidencia del reviewer.  
   **Riesgo:** dependencia externa inestable o semántica pobre de findings.

6. **Auditoría OWASP para reviewer**  
   **[Prioridad: P0]**  
   **DoD:** reglas iniciales de seguridad aplicadas al diff/contexto; findings clasificados y explicables; decisión final del reviewer considera seguridad + análisis estático.  
   **Riesgo:** falsos positivos masivos o falsa sensación de seguridad.

7. **Panel UI del agente de revisión**  
   **[Prioridad: P1]**  
   **DoD:** vista con score, evidencias, findings SonarQube/OWASP y estado final `Approved/Changes Requested/Security Escalation/Needs Human Review`.  
   **Riesgo:** no poder demostrar el valor del reviewer durante la demo o el TFM.

### Entregable visible de la semana

- Dashboard mostrando modo Deep Work/Window.
- Cola de interrupciones explicada.
- Reviewer con panel de evidencia para PRs.

---

## Semana 4 — Cierre

### Objetivo

Cerrar el ciclo con observabilidad útil, validación E2E desde UI, documentación del TFM y un Demo Mode estable que muestre el valor del producto completo.

### Tareas

1. **Observabilidad base centrada en logs estructurados y correlación**  
   **[Prioridad: P0]**  
   **DoD:** logs con correlation id por request/job; eventos clave de ingestión, triaje y reviewer visibles; errores accionables desde dashboard o consola.  
   **Riesgo:** imposible depurar demo o investigación del TFM ante incidencias.

2. **Playwright E2E para flujo principal desde dashboard**  
   **[Prioridad: P0]**  
   **DoD:** suite valida login delegado, estado del entorno, ingestión, preview del morning summary, decisión de foco y consulta del reviewer; artifacts de screenshots/traces disponibles.  
   **Riesgo:** flujos rotos en integración final pese a tener tests unitarios verdes.

3. **Demo Mode con datos coherentes de punta a punta**  
   **[Prioridad: P0]**  
   **DoD:** modo demostración precarga escenarios de Teams, Outlook, summary, Deep Work y PR review; reinicio sencillo; narrativa demo reproducible.  
   **Riesgo:** demo frágil dependiente de servicios externos o datos no deterministas.

4. **Documentación técnica para TFM**  
   **[Prioridad: P1]**  
   **DoD:** documentación cubre arquitectura, decisiones, trade-offs, flujos, testing y observabilidad; material reutilizable en memoria y demo.  
   **Riesgo:** mucho código y poca trazabilidad académica/arquitectónica.

5. **Hardening final de UX del dashboard**  
   **[Prioridad: P1]**  
   **DoD:** navegación consistente, estados vacíos, errores visibles, etiquetas comprensibles y recorrido demo sin ambigüedades.  
   **Riesgo:** producto técnicamente correcto pero difícil de explicar y evaluar.

6. **Checklist de release interno**  
   **[Prioridad: P2]**  
   **DoD:** checklist de arranque local, variables, pasos demo, comandos de test y riesgos conocidos disponible en repo.  
   **Riesgo:** dependencia excesiva de memoria humana para operar la entrega.

### Entregable visible de la semana

- Demo end-to-end navegable desde dashboard.
- Logs correlacionados.
- Suite Playwright ejecutando journeys críticos.

---

## Estrategia TDD

### Principio rector

Aura se construirá con TDD por capas: primero dominio y contratos, después integración y finalmente validación E2E desde UI. Playwright NO reemplaza los tests de dominio; los cierra sobre una experiencia real de usuario.

### Pirámide de validación

1. **Unit Tests (xUnit):** reglas de negocio, scoring, state machine, composición de summary, decisiones del reviewer.
2. **Integration Tests:** adaptadores Graph/Sonar/Qdrant mockeados o en sandbox, checkpoints, persistencia y orquestación.
3. **E2E con Playwright:** validación del flujo visible del dashboard como prueba de que el sistema realmente entrega valor usable.

## Decisiones arquitectónicas vigentes para este plan

| Área | Decisión vigente |
| --- | --- |
| Auth objetivo | Microsoft Entra ID delegated auth |
| Primer login | Interactivo desde `Aura.UI` |
| Identidad de usuario | `oid` del token validado |
| Ciclo de token | Cache persistente en SQLite + renovación silent con MSAL |
| Falla de renovación | Re-auth obligatoria |
| Graph | Tokens delegados del usuario; no app-only |
| App Registration | `ClientId` y `TenantId` pertenecen a la App Registration de Aura |
| `ClientSecret` | No requerido para este flujo delegado |
| Topología | `Aura.Api`, `Aura.UI` y `Aura.Workers` separados |
| Alcance de entrega | Despliegue local Docker-first |

### Cómo Playwright validará el flujo de usuario desde el dashboard

Playwright validará journeys completos, no widgets aislados:

1. **Arranque y salud del sistema**  
   Verifica que el dashboard muestre API activa, Qdrant disponible, configuración auth delegated disponible y módulos cargados.

2. **Autenticación y contexto de usuario**  
   Ejecuta login interactivo con Entra ID en entorno controlado y comprueba que el dashboard cambie a estado autenticado usando `oid` como identidad.

3. **Ingestión visible**  
   Dispara o consume datos semilla de Teams/Outlook y verifica que los items aparezcan en la bandeja con origen, prioridad y timestamp.

4. **Morning Summary visible**  
   Comprueba que el summary generado muestre ranking, riesgos y acciones sugeridas en pantalla.

5. **Calendar y Graph delegados**  
   Verifica que las reuniones visibles y alertas usen el contexto delegado del usuario autenticado y que una falla de renovación silent derive en re-auth.

6. **Deep Work / Window of Opportunity**  
   Fuerza escenarios controlados y valida que el dashboard explique si un item fue interrumpido o diferido según política.

7. **Reviewer de PRs**  
   Carga un PR de demo y valida que el panel muestre findings de SonarQube/OWASP y una decisión final trazable.

8. **Trazabilidad de fallo**  
   Ante error, Playwright debe guardar screenshot, trace y evidencia para diagnóstico rápido.

### Regla práctica de implementación

Cada slice funcional nuevo debe cerrar con este orden:

1. Test unitario de regla/comportamiento.
2. Implementación mínima para verde.
3. Test de integración del puerto/adaptador.
4. Exposición en UI/dashboard.
5. Caso Playwright si el flujo ya es visible para usuario.

ESO es lo que evita construir “backend invisible” que después nadie puede inspeccionar.

---

## Directriz de crecimiento de UI

La UI no será una fase final; será un tablero de control incremental.

- **Semana 1:** estado del entorno, scaffolding auth, kernel, servicios.
- **Semana 2:** feed de ingestión + preview de Morning Summary + auth delegada real + Graph delegado.
- **Semana 3:** estado de foco + cola priorizada + reviewer panel.
- **Semana 4:** demo guiada, observabilidad visible y hardening de experiencia.

Objetivo: que siempre exista algo funcional y demostrable en pantalla.

---

## Regla para descomposición de tareas

Las tareas deben ser suficientemente atómicas para permitir dirección continua sin perder control del producto.

### Criterio de atomicidad

Una tarea está bien definida si:

- tiene un único objetivo técnico claro,
- toca un área limitada del sistema,
- puede validarse en menos de una sesión de trabajo razonable,
- deja evidencia visible o testeable,
- puede replanificarse sin romper medio sprint.

### Anti-patrones a evitar

- “Implementar ingestión completa”.
- “Construir reviewer”.
- “Hacer UI”.

Eso NO son tareas. Son épicas mal cortadas.

---

## Recomendación de ejecución semanal

- Inicio de semana: confirmar alcance y criterios de aceptación.
- Mitad de semana: revisar demo visible y riesgos reales.
- Fin de semana/sprint: cerrar con evidencia, deuda explícita y siguiente corte atómico.

## Resultado esperado al final de las 4 semanas

Un Aura navegable, demostrable y trazable, con backend modular, UI incremental, autenticación delegada real con Entra ID, Graph consumido con contexto de usuario, despliegue local Docker-first y suficiente documentación para sostener desarrollo técnico y presentación de TFM.
