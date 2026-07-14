# Aura - Arquitectura Final y Decisiones Clave (TFM)

Este documento condensa la arquitectura implementada en Aura y las decisiones técnicas fundamentales ("Trade-offs") adoptadas para el desarrollo del Trabajo de Fin de Máster.

## Visión Arquitectónica: Clean Architecture + Modular Monolith

Aura se estructura bajo el patrón de **Arquitectura Limpia** (Clean Architecture), fuertemente inspirado en Arquitectura Hexagonal (Ports and Adapters). La meta principal es asegurar que la lógica de Triaje Inteligente (el "cerebro" del asistente) sea agnóstica a los detalles de cómo se conectan las plataformas (Teams, Outlook) o dónde se guarda la memoria vectorial.

El sistema se compone de cinco anillos/proyectos principales:

1. **Aura.Domain (Capa Central):** Modelos canónicos (`WorkItem`, `SemanticChunk`) y enums inmutables. Es el núcleo puro de C#.
2. **Aura.Application (Casos de Uso):** Contiene las reglas del negocio, el algoritmo de `MorningSummaryRankingPolicy` y los contratos (Interfaces/Puertos).
3. **Aura.Infrastructure (Adaptadores):** Donde reside el código que ensucia las manos con el exterior: llamadas a Microsoft Graph, integración con Qdrant, EF Core con SQLite/Azure SQL, y el cliente de LLMs.
4. **Aura.Api (Host de Entrada):** Expone los endpoints REST y los Hubs de SignalR. 
5. **Aura.Workers (Host de Fondo):** Procesos `BackgroundService` asíncronos que ejecutan el polling, la ingestión de calendarios y la sincronización con la base vectorial.

Todo esto está contenido en una solución .NET 9, desplegada como un "Monolito Modular". Separamos lógicamente las responsabilidades en distintos contenedores Docker (`aura-api`, `aura-workers`, `aura-ui`) permitiendo escalabilidad en la nube, pero manteniendo la simplicidad de un único repositorio (Monorepo).

## Decisiones Técnicas Clave (Trade-Offs)

Durante el TFM, se tomaron decisiones arquitectónicas críticas que condicionan cómo opera el sistema:

### 1. Dual Auth Mode (Entra ID + Demo Mode)

* **El Problema:** La integración con Microsoft Graph API requiere el registro de una aplicación corporativa. Las políticas de seguridad bloquean el acceso al correo y Teams de usuarios reales desde entornos de prueba locales o tenants académicos sin privilegios de administrador global.
* **La Decisión:** Implementar un **Dual Auth Mode** mediante `SmartBearer` Routing.
* **Impacto:** En producción (`UseEntraId: true`), la API exige tokens reales de Entra ID validando firmas contra los endpoints de Microsoft. En modo evaluación/desarrollo (`UseEntraId: false`), el sistema inyecta tokens JWT simulados ("Mock JWT"). Esto permite al evaluador del TFM navegar, hacer *login* y utilizar el Dashboard usando la opción "Demo Mode", sin depender de credenciales externas ni permisos corporativos.

### 2. Memoria Vectorial y LLMs "Asesores", no "Decisores"

* **El Problema:** La Inteligencia Artificial Generativa alucina y no es determinista. Dejar que un LLM decida sin barreras si ocultar o borrar una notificación de trabajo es riesgoso para un producto de productividad.
* **La Decisión:** El LLM (Ollama o Azure OpenAI) actúa bajo el patrón **Advisory (Asesor)**, no como motor decisor.
* **Impacto:** 
  1. Cuando entra una señal, se extraen embeddings de texto (con `nomic-embed-text`) y se guardan en **Qdrant** (Base de Datos Vectorial).
  2. Al evaluar una nueva interrupción, se busca en Qdrant un caso similar del pasado.
  3. El contexto recuperado se envía al LLM para pedir un *consejo*.
  4. El **Motor de Reglas Determinista** (escrito en C#) analiza el score matemático, el consejo del LLM y el estado actual de la agenda del usuario para dictaminar el fallo final (*INTERRUPT*, *QUEUE*, *DEFER*). 
  Aura mantiene el control.

### 3. Observabilidad Nativa (OpenTelemetry)

* **El Problema:** Al tener múltiples workers asíncronos consumiendo colas y APIs, depurar fallos en producción o entender cuellos de botella se vuelve imposible si solo existen logs por consola.
* **La Decisión:** Implementación exhaustiva de **OpenTelemetry**.
* **Impacto:** Se crearon `ActivitySource` propios y métodos `[LoggerMessage]` hiper-optimizados. Cada petición HTTP recibe un `CorrelationId` que viaja desde el Frontend (UI) hasta el Worker de sincronización y el log de la BD. En el entorno desplegado en Azure Container Apps, toda la telemetría fluye en tiempo real hacia **Azure Application Insights**, permitiendo ver métricas de negocio y cascadas de tiempo (Traces).

### 4. Despliegue Docker-First y SQLite

* **El Problema:** El TFM debe ser fácilmente evaluable y reproducible por tribunales sin configuraciones tediosas. Usar SQL Server requeriría instalaciones pesadas o cobros adicionales en nube.
* **La Decisión:** Todo el ecosistema arranca con un solo comando `docker compose up`. La persistencia transaccional y la caché temporal recaen en bases de datos **SQLite** embebidas en los volúmenes de Docker.
* **Impacto:** Se prioriza la agilidad de desarrollo y evaluación local. Dado el uso de EF Core (Entity Framework), la migración futura a Azure SQL o PostgreSQL en producción está garantizada modificando únicamente una cadena de conexión y un registro en la Inyección de Dependencias.