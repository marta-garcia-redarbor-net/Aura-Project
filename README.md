# Aura

Base inicial de Aura sobre `.NET 9` con solución por capas, tests separados y reglas mínimas de calidad.

## Estructura

```text
src/
  Aura.Api/
  Aura.Application/
  Aura.Domain/
  Aura.Infrastructure/
  Aura.Workers/

tests/
  Aura.UnitTests/
  Aura.IntegrationTests/
  Aura.E2E/
  Aura.ArchitectureTests/
```

## Validación local

```powershell
dotnet restore Aura.sln
dotnet build Aura.sln
dotnet test Aura.sln
```

## Notas

- SDK fijado en `global.json`: `9.0.306`.
- La solución compila correctamente en el estado actual.
- Puede aparecer `NU1900` si el entorno tiene feeds privados heredados de NuGet no accesibles; no bloquea el build base, pero conviene limpiarlo antes de endurecer gates.
