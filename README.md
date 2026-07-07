# Aura

Aura es un asistente de ingeniería que reduce la carga mental del equipo mediante la ingestión multi-fuente de señales (Teams, Outlook, Calendar, GitHub), triaje cognitivo y revisión técnica con evidencia verificable.

El proyecto nace como Trabajo de Fin de Máster, aplicando Clean Architecture, TDD, observabilidad y despliegue cloud-native sobre .NET 9.

---

## Stack tecnológico

| Capa | Tecnología |
|------|-----------|
| **Runtime** | .NET 9 / ASP.NET Core |
| **Frontend** | Blazor Server + SignalR |
| **Base de datos** | SQLite (desarrollo), Azure SQL (producción) |
| **Vector store** | Qdrant (Docker local) |
| **Embeddings** | Ollama (nomic-embed-text) o Azure OpenAI |
| **Autenticación** | Microsoft Entra ID (delegada) |
| **Despliegue** | Docker Compose (local) / Azure Container Apps (nube) |
| **Infraestructura** | Bicep + GitHub Actions |
| **Testing** | xUnit, bUnit, Playwright, ArchUnitNET |
| **Observabilidad** | OpenTelemetry + logs estructurados con correlación |

---

## Funcionalidades principales

### Ingestion multi-fuente
- **Teams**: captura menciones y mensajes de canales vía Microsoft Graph.
- **Outlook**: ingestión de correos con prioridad por remitente y asunto.
- **Calendar**: reuniones del día con alertas programadas (60', 10', 5' antes).
- **Azure DevOps**: listado de PRs pendientes con estado y reviewers.

### Triaje y productividad
- **Focus State Machine**: detecta si estás en foco profundo, ventana de oportunidad, ausente o recuperación, combinando calendario y horario laboral.
- **Motor de interrupciones**: evalúa cada work item entrante y decide si interrumpir, encolar o diferir según scoring de prioridad.
- **Prioridad con scoring**: asigna una puntuación numérica a cada item basada en contenido, fuente y señales temporales.
- **Morning Summary**: resumen diario priorizado con soporte multi-zona horaria.

### Dashboard
- Tarjetas de resumen por fuente (Teams, Outlook, Calendar, PRs).
- Panel de estado del sistema con health checks (API, Qdrant, auth).
- Panel de errores recientes con IDs de correlación.
- Indicador de estado de foco actual con override manual.
- Notificaciones en tiempo real vía SignalR (alertas de reuniones, notificaciones de items).

### Observabilidad
- Logs estructurados con `[LoggerMessage]` y correlation ID.
- Middleware de correlación con trazabilidad request → response.
- Spans de actividad (ActivitySource) en API, workers y adaptadores.
- Panel de errores recientes con visualización en dashboard.

### Demo mode
- Simulación de datos semilla precargados (Teams, Outlook, Calendar, PRs).
- Datos demostrables sin necesidad de conectores reales ni Graph API.
- (`/api/demo/start-simulation`)

---

## Estructura del proyecto

```text
Aura.sln
├── src/
│   ├── Aura.Api/              # ASP.NET Core host (API REST + SignalR)
│   ├── Aura.Application/      # Casos de uso, puertos, servicios
│   ├── Aura.Domain/           # Entidades, value objects, enums
│   ├── Aura.Infrastructure/   # Adaptadores, persistencia, conectores
│   ├── Aura.Workers/          # Background services (ingestión, alertas, embeddings)
│   └── Aura.UI/               # Blazor Server (dashboard, navegación)
│
├── tests/
│   ├── Aura.UnitTests/        # Tests unitarios (xUnit + bUnit)
│   ├── Aura.IntegrationTests/ # Tests de integración (WebApplicationFactory)
│   ├── Aura.E2E/              # Tests end-to-end (Playwright)
│   └── Aura.ArchitectureTests/ # Tests arquitectónicos (ArchUnitNET)
│
├── infra/                     # Módulos Bicep para Azure
├── scripts/                   # Scripts auxiliares (smoke test, demo)
├── docs/                      # Documentación de arquitectura
└── openspec/                  # Especificaciones SDD
```

---

## Instalación y ejecución

### Prerrequisitos

- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- .NET 9 SDK (versión en [`global.json`](global.json))
- [Ollama](https://ollama.com/) (para embeddings locales)

### Pasos

```powershell
# 1. Clonar el repositorio
git clone https://github.com/marta-garcia-redarbor-net/Aura
cd Aura

# 2. Copiar configuración de entorno
Copy-Item .env.example .env

# 3. Iniciar dependencias (Qdrant)
docker compose up -d

# 4. Descargar modelo de embeddings (si no está ya en Ollama)
ollama pull nomic-embed-text

# 5. Restaurar, compilar y testear
dotnet restore Aura.sln
dotnet build Aura.sln
dotnet test Aura.sln

# 6. Iniciar la API
dotnet run --project src/Aura.Api

# 7. En otra terminal, iniciar los workers
dotnet run --project src/Aura.Workers

# 8. En otra terminal, iniciar la UI
dotnet run --project src/Aura.UI
```

La UI estará disponible en `http://localhost:5180` y la API en `http://localhost:5190`.

### Ejecución con Docker Compose (todo en uno)

```powershell
docker compose up --build -d
```

Esto levanta `aura-api` (puerto 5190), `aura-ui` (puerto 5180), `aura-workers` y `aura-qdrant`.

### Verificar estado

```powershell
Invoke-RestMethod http://localhost:5190/health
```

### Datos de demostración

Con `"SeedData": { "Enabled": true }` en `appsettings.Development.json`, al arrancar
los workers se cargan datos semilla en los conectores de Teams, Outlook, Calendar y PRs.

> **URL de despliegue (ACA):** [https://aura-ui-dev.bluesea-6c67d090.francecentral.azurecontainerapps.io](https://aura-ui-dev.bluesea-6c67d090.francecentral.azurecontainerapps.io)

---

## Seguridad y autenticación

### Modelo de autenticación

Aura utiliza **autenticación delegada con Microsoft Entra ID** — el usuario se autentica desde la UI, y su token se reenvía a la API para todas las operaciones. No se usan credenciales de aplicación ni client secrets en el flujo nominal.

### Modos de ejecución

| Modo | Configuración (`UseEntraId`) | Comportamiento |
|------|------------------------------|----------------|
| **Desarrollo** | `false` (default) | Autenticación mock vía cookie. La UI permite login sin Entra ID. Endpoint `/api/auth/mock-login` disponible para desarrollo. |
| **Producción** | `true` | Login interactivo con Microsoft Entra ID. La UI usa OIDC (`Microsoft.Identity.Web`). La API valida JWTs reales. |

### Flujo de autenticación

```text
Usuario abre Aura.UI
  → Inicia sesión (Entra ID en producción, cookie mock en desarrollo)
  → UI recibe token delegado con oid
  → UI reenvía bearer token a Aura.Api + SignalR
  → API valida JWT (real o mock según modo)
  → oid se usa como identidad canónica del usuario
```

### Token cache

El estado de los tokens MSAL se persiste en una base de datos SQLite (`token_cache.db`), lo que permite renovación silenciosa sin pedir login en cada reinicio.

### Autorización

Los endpoints de la API requieren autenticación por defecto, excepto health checks y mock-login (solo en desarrollo). Los workers utilizan el token cache para operaciones delegadas con Microsoft Graph.

### Conectores Graph

Los conectores de Teams, Outlook y Calendar llaman a Microsoft Graph **con el token delegado del usuario autenticado**, nunca con app-only credentials. Si Graph no está disponible, los conectores fallan de forma controlada sin bloquear el sistema.

> _Para desarrollo local sin Graph: los conectores usan datos mock/fixtures embebidos automáticamente._

---

## Usuario y contraseña de prueba

| Rol | Usuario | Contraseña |
|-----|---------|------------|
| ⭐ **Evaluador TFM** | `evaluador.tfm@garciasanmartagmail.onmicrosoft.com` | `AuraTFM2024!` |
| 🛠️ **Admin Azure** | Tu cuenta personal | La de tu cuenta Microsoft |

> **Nota:** La aplicación está desplegada en Azure Container Apps. Si al acceder no responde, puede estar detenida para ahorrar costes. Ejecuta `./scripts/start-aura.ps1` desde el repositorio para iniciarla y espera 1-2 minutos.

---

## Seguridad

### Security Headers

Todas las respuestas HTTP incluyen headers de seguridad:

| Header | Valor | Propósito |
|--------|-------|-----------|
| `X-Content-Type-Options` | `nosniff` | Previene MIME-sniffing |
| `X-Frame-Options` | `DENY` | Previene clickjacking |
| `Content-Security-Policy` | `default-src 'self'` | Restringe origen de recursos |
| `Strict-Transport-Security` | `max-age=31536000; includeSubDomains` | Fuerza HTTPS (solo producción) |

### Rate Limiting

Protección contra abuso por IP con ventanas deslizantes:

| Policy | Límite | Ventana | Endpoints |
|--------|--------|---------|-----------|
| Default | 100 req | 60s | Todos los endpoints API |
| Auth | 10 req | 60s | `/api/auth/*` |

Cuando se excede el límite, la API responde HTTP 429 con header `Retry-After`.

### Validación de Input

Los DTOs de endpoints POST/PUT se validan con FluentValidation. Requests inválidos reciben HTTP 422 con errores estructurados por campo.

### HTTPS Redirect

En producción, todas las peticiones HTTP se redirigen a HTTPS (307). En desarrollo, HTTP permanece accesible.

### Escaneo de Vulnerabilidades

- **Dependabot**: monitoreo semanal de dependencias NuGet.
- **CI audit**: `dotnet list package --vulnerable` en cada PR — falla solo en vulnerabilidades críticas o altas.

---

## Slides

> _Pendiente — enlace a presentación._

---

## Vídeo

> _Pendiente — enlace a vídeo explicativo._

---

## Licencia

Proyecto académico — Trabajo de Fin de Máster.
