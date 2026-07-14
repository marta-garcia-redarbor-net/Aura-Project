# Calidad — Tests E2E y Límites del Frontend

La suite `Aura.E2E` valida los "journeys" (viajes de usuario) críticos del sistema, garantizando que el ensamblaje completo (UI, Casos de Uso y Persistencia) responde de forma coordinada.

## La decisión sobre Playwright y Blazor Server

Originalmente, Aura se planteó probarse de principio a fin usando un navegador Chromium orquestado por **Playwright**. Sin embargo, durante el desarrollo, descubrimos una limitación fundamental relacionada con la arquitectura de **Blazor Server**.

Blazor Server confía en una conexión persistente **SignalR (WebSockets)** para la actualización del DOM tras la carga inicial (prerenderizado). En el entorno de test (usando `WebApplicationFactory` con Kestrel hospedado para test), el *handshake* del WebSocket de SignalR experimenta *timeouts* crónicos porque el cliente de prueba bloquea la ejecución o el contexto de la aplicación no termina de estabilizar la conexión asíncrona rápidamente, derivando en tests extremadamente frágiles (*flaky*).

### La Solución: HTTP-only Smoke Tests

En lugar de sacrificar la cobertura E2E, pivotamos hacia un modelo de **Smoke Tests HTTP-only mediante WebApplicationFactory**. 

Dado que Blazor realiza un prerenderizado en el servidor (Static Server-Side Rendering) de la estructura HTML antes de abrir el socket de SignalR, podemos capturar el HTML resultante de esa petición HTTP GET inicial.

1. **Aislamiento en red:** Se levanta el Host de Blazor en memoria sin levantar navegadores reales.
2. **Dependencias controladas:** Todos los clientes HTTP del frontend (`IDashboardApiClient`, `IPullRequestsApiClient`, etc.) se reemplazan por *Stubs* dentro del contenedor de inyección de dependencias del test.
3. **Aserciones sobre `data-testid`:** Inspeccionamos el HTML devuelto buscando marcadores estables (ej. `data-testid="dashboard-shell"`). Si el marcador está, significa que Blazor procesó los datos del Stub y completó el ciclo de vida del componente correctamente.

### Cobertura E2E actual

Con esta estrategia, disponemos de ~45 tests E2E altamente veloces y deterministas que cubren:

- Renderizado del **Dashboard principal** (Estados: Loading, Empty, Populated, Error).
- Tarjetas de resumen y panel de **System Status**.
- Renderizado del panel de progreso y sync (Inbox y Morning Summary previews).
- Vistas detalladas: **Pull Requests** y **Decision Log** (triaje).
- La **Landing Page** y su respuesta a usuarios anónimos.

Los tests de navegador puro de Playwright permanecen en el repositorio bajo la categoría de `PlaywrightTests` pero marcados como `[Fact(Skip)]`, sirviendo como andamiaje (*bootstrap*) en caso de que en el futuro se migre el frontend a *Blazor WebAssembly* o a un framework cliente independiente (React/Vue), donde Playwright resulta óptimo.