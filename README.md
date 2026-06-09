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

## Local Environment

### Prerequisites

- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (or Docker Engine) running locally.
- .NET 9 SDK (see `global.json`).

### Start Qdrant

```powershell
# Copy the environment template and adjust ports if needed
Copy-Item .env.example .env

# Start the Qdrant container
docker-compose up -d
```

Qdrant will be available at `http://localhost:6333` (HTTP) and `localhost:6334` (gRPC).

### Configure secrets

The API requires an Azure OpenAI API key for the embedding provider. This value is **not** committed to source control — use [.NET user-secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets) instead:

```powershell
dotnet user-secrets set "EmbeddingProvider:ApiKey" "<your-azure-openai-key>" --project src/Aura.Api
```

> **Tip**: `Endpoint` and `DeploymentName` defaults live in `appsettings.Development.json`. Override them via user-secrets too if your Azure OpenAI resource differs.

### Run the API

```powershell
dotnet run --project src/Aura.Api
```

### Verify health

```powershell
Invoke-RestMethod http://localhost:5180/health
```

A `200 OK` response confirms the API can reach Qdrant. A `503` indicates Qdrant is not reachable.

> **Note**: The default URL uses the `http` launch profile (`localhost:5180`). If you override `--urls`, adjust accordingly.

### Stop

```powershell
docker-compose down
```

## Notas

- SDK fijado en `global.json`: `9.0.306`.
- La solución compila correctamente en el estado actual.
- Puede aparecer `NU1900` si el entorno tiene feeds privados heredados de NuGet no accesibles; no bloquea el build base, pero conviene limpiarlo antes de endurecer gates.
