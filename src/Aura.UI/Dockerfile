# syntax=docker/dockerfile:1
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG TARGETARCH=amd64
ARG VERSION=0.0.0-local
WORKDIR /src

# Copy solution and project files for restore caching
COPY Aura.sln .
COPY src/Aura.UI/Aura.UI.csproj src/Aura.UI/
COPY src/Aura.Application/Aura.Application.csproj src/Aura.Application/
COPY src/Aura.Domain/Aura.Domain.csproj src/Aura.Domain/
COPY src/Aura.Infrastructure/Aura.Infrastructure.csproj src/Aura.Infrastructure/
RUN dotnet restore -a $TARGETARCH src/Aura.UI/Aura.UI.csproj

# Copy source and build
COPY src/ src/
RUN dotnet publish src/Aura.UI/Aura.UI.csproj -c Release -a $TARGETARCH -o /app/publish --no-restore -p:Version=$VERSION

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
ENV ASPNETCORE_URLS=http://+:8080
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1
ENTRYPOINT ["dotnet", "Aura.UI.dll"]
