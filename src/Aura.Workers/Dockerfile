# syntax=docker/dockerfile:1
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG TARGETARCH=amd64
WORKDIR /src

# Copy solution and project files for restore caching
COPY Aura.sln .
COPY src/Aura.Workers/Aura.Workers.csproj src/Aura.Workers/
COPY src/Aura.Application/Aura.Application.csproj src/Aura.Application/
COPY src/Aura.Domain/Aura.Domain.csproj src/Aura.Domain/
COPY src/Aura.Infrastructure/Aura.Infrastructure.csproj src/Aura.Infrastructure/
RUN dotnet restore -a $TARGETARCH src/Aura.Workers/Aura.Workers.csproj

# Copy source and build
COPY src/ src/
RUN dotnet publish src/Aura.Workers/Aura.Workers.csproj -c Release -a $TARGETARCH -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Aura.Workers.dll"]
