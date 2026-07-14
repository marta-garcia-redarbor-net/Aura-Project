# Calidad — Estrategia TDD con xUnit

Aura aplica Test-Driven Development (TDD) desde el centro hacia afuera (Inside-Out). Toda la lógica de negocio pura se valida en la suite `Aura.UnitTests` sin requerir bases de datos ni servicios externos.

## Dominio y Casos de Uso (Unit Tests)

La regla principal para el Dominio es la **testabilidad asíncrona y sin estado**. 

- **Scoring y Triaje:** Los algoritmos de ranqueo (ej. `MorningSummaryRankingPolicy`) se prueban exhaustivamente frente a múltiples variaciones de datos de entrada para garantizar que el scoring matemático es determinista.
- **Timezones:** Las políticas que dependen de la hora o fecha reciben proveedores inyectados o parámetros explícitos (`DateTimeOffset`) para evitar tests dependientes del reloj del sistema.
- **Mocks:** Se utiliza `NSubstitute` exclusivamente en la capa de Casos de Uso (Application) para falsear el comportamiento de los puertos (IWorkItemStore, IInterruptionDecisionStore). El Dominio puro no contiene mocks, se prueba instanciando agregados directamente.

## Integración de Componentes UI (bUnit)

Para el frontend Blazor (`Aura.UI`), TDD se aplica utilizando **bUnit**.
En lugar de depender de tests frágiles basados en Selenium/Playwright para validar la lógica de presentación, usamos bUnit para renderizar componentes en memoria.

- Comprobamos el renderizado condicional (ej. `DashboardViewStateKind`).
- Validamos parámetros en cascada como `AuthenticationState`.
- Verificamos la presencia de atributos `data-testid` que luego consumirán los tests E2E.

## Integración Real (Integration Tests)

Los adaptadores de infraestructura se prueban contra instancias reales o en memoria en la suite `Aura.IntegrationTests`:

- **SQLite In-Memory / Ficheros:** Las migraciones de EF Core y los repositorios se testean contra bases de datos reales para garantizar la fidelidad del SQL generado.
- **Qdrant:** Se valida la conexión y mapeo de puntos usando fixtures si Testcontainers/Docker está disponible.
- **Middlewares ASP.NET:** Se emplea `WebApplicationFactory` para comprobar que los headers de seguridad, Rate Limiting y CORS se aplican correctamente en el flujo HTTP completo.

## Naming Convention y Estructura

Cada test en Aura sigue la convención:
`NombreMetodoOComponente_EstadoEscenario_ComportamientoEsperado`

Ejemplo: `GetTriageDecisions_WithPagination_ReturnsRespectedPageSize`

Dentro de cada test, la separación `Arrange`, `Act` y `Assert` es obligatoria visualmente (mediante saltos de línea o comentarios) para facilitar la revisión de código por otros miembros del equipo.