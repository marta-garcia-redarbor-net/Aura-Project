# Calidad — Visión general

Este documento describe la estrategia integral de calidad de Aura, diseñada para garantizar que el sistema evolucione de forma predecible y segura. Al tratarse de un asistente cognitivo donde la IA interactúa con el flujo de trabajo del usuario, la confianza en las decisiones deterministas es crítica.

Aura protege su Clean Architecture y su lógica de negocio mediante una batería de más de 1.500 pruebas automatizadas que se ejecutan en milisegundos.

## Pirámide de validación

La estrategia de testing sigue una pirámide estricta:

1. **Unit Tests (xUnit + bUnit):** Validan el comportamiento del Dominio puro, Casos de Uso, políticas de triaje, scoring y renderizado de componentes Blazor aislados. Representan la base de la pirámide (~1225 tests).
2. **Integration Tests (WebApplicationFactory):** Validan los adaptadores de Infraestructura (Qdrant, SQLite, Entity Framework), la persistencia, la serialización de modelos y los middlewares de la API (auth, observabilidad, rate limiting). (~170 tests).
3. **E2E Tests HTTP-only:** Validan el ensamblaje completo (UI -> API -> Domnio) simulando respuestas de servicios externos mediante clientes HTTP tipados, asegurando que las distintas capas se integran correctamente y el dashboard renderiza los estados esperados. (~45 tests).
4. **Architecture Tests (ArchUnitNET):** Aseguran que nadie introduzca dependencias circulares ni rompa la dirección de las dependencias de Clean Architecture. (~84 tests).

## Herramientas y estándares

- **Framework principal:** xUnit + NSubstitute + FluentAssertions.
- **Componentes UI:** bUnit.
- **Arquitectura:** ArchUnitNET.
- **Format y estilo:** `.editorconfig` con reglas estrictas y `dotnet format`.
- **Estructura de tests:** Patrón *Arrange-Act-Assert* riguroso.
- **Metodología:** Desarrollo guiado por especificaciones (SDD) y pruebas (TDD). Ninguna funcionalidad del motor de triaje se fusionó sin su correspondiente validación previa.

En los siguientes documentos de esta sección se detalla la implementación técnica de cada nivel de esta pirámide.