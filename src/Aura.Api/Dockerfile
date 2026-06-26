FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files for restore caching
COPY Aura.sln .
COPY src/Aura.Api/Aura.Api.csproj src/Aura.Api/
COPY src/Aura.Application/Aura.Application.csproj src/Aura.Application/
COPY src/Aura.Domain/Aura.Domain.csproj src/Aura.Domain/
COPY src/Aura.Infrastructure/Aura.Infrastructure.csproj src/Aura.Infrastructure/
RUN dotnet restore src/Aura.Api/Aura.Api.csproj

# Copy source and build
COPY src/ src/
RUN dotnet publish src/Aura.Api/Aura.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Aura.Api.dll"]
