# Aura - Asistente de Ingeniería y Triaje Cognitivo

Trabajo de Fin de Máster (TFM) - Máster en Arquitectura de Software

## Entregables del Proyecto

* **URL de Despliegue (Azure Container Apps):** [Acceder a Aura](https://aura-ui-dev.bluesea-6c67d090.francecentral.azurecontainerapps.io)
  *(Nota: Al estar alojado en un entorno Serverless, el primer acceso puede tardar unos segundos en "despertar" los contenedores).*
* **Presentación (Slides):** [Ver Google Slides](https://docs.google.com/presentation/d/1IEmdA8vuYvKjR7un0RLn9d1EoCalMYIdIvW8OJWY3GI/edit?usp=sharing)
* **Vídeo Explicativo:** [Ver video](https://drive.google.com/file/d/1uWpzTZBQNTWHaK18FtdL_270IjKmI_Go/view?usp=sharing)

---

## a. Descripción general del proyecto

Aura es un asistente cognitivo diseñado para proteger la productividad de los desarrolladores y reducir su carga mental. El ecosistema actual bombardea a los ingenieros con notificaciones (Teams, Outlook, GitHub, alertas), forzando un *context-switching* constante que destruye los bloques de trabajo profundo (*Deep Work*). 

Aura resuelve este problema centralizando la ingestión de estas señales y evaluándolas a través de un **Motor de Triaje Inteligente**. Combinando reglas deterministas, el contexto de calendario del usuario, memoria histórica vectorial y el análisis de un LLM, Aura decide de forma autónoma si una señal debe interrumpir al usuario ahora mismo, encolarse para después, o incluirse en el resumen de la mañana siguiente (*Morning Summary*).

## b. Stack tecnológico utilizado

El proyecto se ha construido aplicando **Clean Architecture**, **Spec-Driven Development (SDD)** y **Test-Driven Development (TDD)**:

* **Plataforma / Runtime:** .NET 9, C#, ASP.NET Core
* **Frontend:** Blazor Server interactivo con comunicación en tiempo real vía SignalR
* **Base de datos relacional:** SQLite (entorno local/demo) y Azure SQL (preparado para producción)
* **Base de datos vectorial:** Qdrant (para indexación y búsqueda semántica de decisiones históricas)
* **IA / Embeddings:** Ollama (soporte local) / Azure OpenAI
* **Autenticación:** Microsoft Entra ID (identidad delegada corporativa) + Auth Mock para evaluación/demo
* **Infraestructura y Despliegue:** Docker, Docker Compose, Azure Container Apps (ACA), Bicep (IaC), GitHub Actions (CI/CD)
* **Observabilidad:** OpenTelemetry (Trazas, Logs, Métricas) exportado a Azure Application Insights
* **Testing:** xUnit, bUnit (componentes UI), WebApplicationFactory (integración) y ArchUnitNET (arquitectura)

## c. Información sobre su instalación y ejecución

La forma recomendada y más sencilla de ejecutar Aura en un entorno local es utilizando Docker. De esta forma, el sistema completo (Frontend, API, Background Workers y Base de datos Vectorial) se levanta sin necesidad de instalar SDKs de desarrollo.

**Prerrequisitos:**
* Tener instalado **Docker** y **Docker Compose** (ej. Docker Desktop).
* [Opcional] Tener **Ollama** instalado localmente con el modelo `nomic-embed-text` (si se desea usar embeddings locales en lugar de la configuración cloud predeterminada).

**Pasos de ejecución:**

1. Clonar el repositorio:
   ```bash
   git clone https://github.com/marta-garcia-redarbor-net/Aura
   cd Aura
   ```
2. Iniciar la infraestructura completa con Docker Compose:
   ```bash
   docker compose up --build -d
   ```
   *Este comando construirá las imágenes de .NET y levantará los contenedores de `aura-ui` (puerto 5180), `aura-api` (puerto 5190), `aura-workers` y `aura-qdrant`.*

3. Acceder a la interfaz web: 
   Navegar a `http://localhost:5180` en el navegador web.

*(Nota para evaluación: Por defecto, el entorno arranca en modo desarrollo con la configuración `UseEntraId: false` y `DemoMode:Enabled: true`. Esto inyecta datos semilla y permite usar el login de demostración sin necesidad de configurar una App Registration real en Azure).*

### Para desarrollo y depuración (Debugging)

Si se desea modificar el código, depurar el sistema o ejecutar los tests (más de 1.500 pruebas automatizadas), se requiere un entorno de desarrollo completo:

**Prerrequisitos adicionales:**
* **SDK de .NET 9** instalado.
* IDE compatible (Visual Studio 2022, JetBrains Rider o VS Code).
* Tener **Ollama** ejecutándose localmente con el modelo `nomic-embed-text` descargado (`ollama pull nomic-embed-text`). Esto es necesario para la generación de embeddings si no se configuran claves de Azure OpenAI.

**Pasos para depuración local:**

1. Levantar *únicamente* la infraestructura de dependencias (Qdrant) usando Docker:
   ```bash
   docker compose up -d qdrant
   ```
2. Compilar la solución para asegurar que todas las dependencias están resueltas:
   ```bash
   dotnet build Aura.sln
   ```
3. Ejecutar los 3 procesos del sistema por separado (desde el IDE o en terminales distintas):
   ```bash
   dotnet run --project src/Aura.Api
   dotnet run --project src/Aura.Workers
   dotnet run --project src/Aura.UI
   ```
4. Navegar a la interfaz de usuario en `http://localhost:5190`. La API estará disponible en `http://localhost:5180` (donde se puede consultar `http://localhost:5180/health`).

Para ejecutar la batería completa de validación (tests unitarios, de integración y arquitectura):
```bash
dotnet test Aura.sln
```

## d. Estructura del proyecto

El código está estructurado siguiendo estrictamente Clean Architecture y separación por responsabilidades funcionales:

* `src/Aura.Domain/`: Entidades core, Value Objects, Enums y reglas de negocio puras. No tiene dependencias externas.
* `src/Aura.Application/`: Casos de uso, orquestación del triaje, contratos de puertos (Ports).
* `src/Aura.Infrastructure/`: Implementación de adaptadores (Bases de datos, Graph API, Qdrant, LLMs, Entra ID).
* `src/Aura.Api/`: Host ASP.NET Core que expone los endpoints REST y Hubs de SignalR.
* `src/Aura.Workers/`: Procesos en segundo plano (BackgroundServices) para ingesta asíncrona y sincronización vectorial.
* `src/Aura.UI/`: Aplicación frontend en Blazor Server.
* `tests/`: 
  * `Aura.UnitTests`: Pruebas de reglas de negocio y componentes UI (bUnit).
  * `Aura.IntegrationTests`: Pruebas E2E de APIs y persistencia real.
  * `Aura.ArchitectureTests`: Reglas automatizadas que garantizan los límites de Clean Architecture.
* `docs/` y `openspec/`: Documentación técnica y especificaciones de diseño.

## e. Funcionalidades principales

1. **Ingestión Normalizada:** Conectores que transforman inputs de Microsoft Teams, Outlook, Calendario y Azure DevOps (Pull Requests) en un formato canónico estandarizado.
2. **Focus State Machine:** Algoritmo que calcula el nivel de disponibilidad del desarrollador (Ej: *Deep Work*, *Window of Opportunity*) cruzando su calendario con horarios configurados.
3. **Triaje y Decisión:** Un motor de reglas evalúa la urgencia del ítem, apoyándose en Qdrant (para buscar cómo se manejó un caso similar en el pasado) y un LLM (para validar contexto), decidiendo la acción final: Interrumpir, Encolar o Diferir.
4. **Dashboard Estratégico:** Una interfaz que aplica la regla de carga cognitiva baja (mostrando solo el *Top 3* de prioridades por canal), previsualizaciones de bandeja de entrada y estado de salud de la infraestructura.
5. **Morning Summary:** Un algoritmo de ranqueo nocturno que prepara un resumen ejecutivo para el inicio de la jornada laboral, agilizando el arranque del día.
6. **Trazabilidad Completa:** Panel de "Decision Log" donde se puede auditar por qué la IA o el motor determinista tomaron cada decisión de interrupción.

## f. Usuario y contraseña de prueba

Dado que las políticas corporativas pueden dificultar la prueba de integraciones reales en directo, la plataforma incluye un entorno Dual-Auth.

Para la corrección del TFM:
* **Entorno Desplegado (ACA) / Entorno Local en Modo Demo:** Al entrar a la web, simplemente se debe hacer clic en el botón **"Explore Demo Mode"**. El sistema asignará automáticamente una sesión de evaluación (usuario mock) sin requerir usuario ni contraseña real, permitiendo navegar por la interfaz y ver los datos de demostración precargados por el simulador interno.
* *Nota:* Las credenciales administrativas o corporativas de Azure, de ser requeridas específicamente para revisión de infraestructura, se proporcionarán mediante el formulario de entrega oficial del TFM.