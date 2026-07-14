# Checklist de Evaluación (Guía para el Tribunal del TFM)

Esta guía rápida está diseñada para que el tribunal pueda evaluar Aura de forma ágil, comprobando la funcionalidad, la arquitectura y los despliegues de forma guiada.

## 1. Acceso al Entorno Desplegado (Azure Container Apps)

El proyecto está publicado en la nube bajo una arquitectura Serverless.

* [ ] **Paso 1:** Navega a [https://aura-ui-dev.bluesea-6c67d090.francecentral.azurecontainerapps.io](https://aura-ui-dev.bluesea-6c67d090.francecentral.azurecontainerapps.io).
* [ ] **Paso 2:** *(Opcional)* Si la página da error al instante, recarga tras 15 segundos. Azure "duerme" los contenedores si no reciben tráfico para ahorrar costes.
* [ ] **Paso 3:** En la pantalla principal (Landing Page), haz clic en el botón de **Login / Access Aura**.
* [ ] **Paso 4:** El sistema detectará que está en "Demo Mode" (sin requerir credenciales de Microsoft Entra ID reales) y te asignará una sesión simulada para permitir la revisión del Dashboard.

## 2. Revisión Funcional en la Interfaz (UI)

Una vez en el Dashboard, te invitamos a revisar los siguientes elementos:

* [ ] **Regla del "Top 3" (Cognitive Load):** Observa las tarjetas (Cards) bajo el título del Dashboard. Aunque haya decenas de notificaciones pendientes, el sistema agrupa y solo muestra el conteo crítico para no abrumar al ingeniero.
* [ ] **Focus State Panel:** Comprueba el indicador de estado. Mostrará "Deep Work", "Window of Opportunity" o "Recovery" según el estado en el que se encuentre el sistema.
* [ ] **Morning Summary Preview:** Comprueba cómo el panel inferior agrupa las tareas pospuestas (Deferred) para el día siguiente, preparadas para ser revisadas por la mañana.
* [ ] **Trazabilidad (Decision Log):** En el menú lateral, dirígete a `Decision Log` (Triaje). Revisa los registros tabulares. Cada decisión (ej. por qué se ocultó un email o se mostró un mensaje de Teams) está trazada, mostrando el puntaje, la regla activada y el contexto recuperado de la base vectorial (Qdrant).

## 3. Revisión del Código y Testing (Local)

Para evaluar la rigurosidad técnica y la estrategia de TDD / Clean Architecture:

* [ ] **Paso 1:** Clona el repositorio desde GitHub.
* [ ] **Paso 2:** Abre la solución (`Aura.sln`) en tu IDE preferido (Rider, Visual Studio) o usa la terminal.
* [ ] **Paso 3:** Ejecuta la batería de pruebas automatizadas:
  ```bash
  dotnet test Aura.sln
  ```
* [ ] **Paso 4:** Comprueba el resultado. Verás más de 1.500 pruebas en verde (`Passed`), ejecutadas en apenas unos segundos. Esto incluye:
  * Pruebas Unitarias de lógica de negocio (xUnit).
  * Pruebas de renderizado de componentes Blazor (bUnit).
  * Pruebas de Arquitectura que garantizan que el Dominio no toca Infraestructura (ArchUnitNET).
  * Pruebas E2E de integración de API y UI (WebApplicationFactory HTTP-only).

## 4. Revisión de Arquitectura

Para verificar la aplicación de los principios del máster:

* [ ] **Domain:** Revisa la carpeta `src/Aura.Domain/`. Comprobarás que no hay dependencias de bases de datos, APIs de Microsoft ni Entity Framework. El núcleo es C# puro.
* [ ] **Adapters:** Revisa la carpeta `src/Aura.Infrastructure/Adapters`. Aquí encontrarás la implementación técnica real: `QdrantSemanticIndexAdapter`, `TeamsConnectorAdapter`, `EfInterruptionDecisionStore`.
* [ ] **Documentación Extra:** Lee el documento unificado [`docs/architecture/FINAL-ARCHITECTURE.md`](docs/architecture/FINAL-ARCHITECTURE.md) donde se desgranan las grandes decisiones y *trade-offs* del proyecto (Dual Auth Mode, uso de LLMs como asesores y no decisores).

## 5. Revisión de Observabilidad (Opcional - Requiere Portal Azure)

Si se dispone de acceso al portal de Azure del proyecto:

* [ ] Accede al recurso `Application Insights` asociado al proyecto.
* [ ] Revisa el mapa de la aplicación para ver la topología (UI -> API -> Qdrant).
* [ ] Busca transacciones y logs. Verás que todos los logs utilizan `[LoggerMessage]` para un alto rendimiento y están atados a un `CorrelationId` rastreable.