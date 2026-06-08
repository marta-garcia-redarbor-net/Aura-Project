# Qdrant Local Environment Specification

## Purpose

Establishes a reproducible local environment with a persistent vector store (Qdrant) via Docker Compose, and provides verifiable connectivity evidence via an API health endpoint.

## Requirements

### Requirement: Local Qdrant Container

The system MUST provide a Docker Compose configuration that runs a local Qdrant instance using environment variables for configuration.

#### Scenario: Developer starts local environment

- GIVEN the developer has Docker installed
- WHEN they execute `docker-compose up -d`
- THEN a Qdrant container MUST start successfully
- AND it MUST expose ports configured via `.env` (default 6333 and 6334)

### Requirement: Qdrant Health Check Integration

The `Aura.Api` application MUST expose a `/health` endpoint that includes the connection status to the Qdrant vector store, implemented via `Aura.Infrastructure`.

#### Scenario: API queries health endpoint when Qdrant is healthy

- GIVEN the local Qdrant container is running and accessible
- WHEN a client sends a GET request to `/health`
- THEN the API MUST return an HTTP 200 OK status
- AND the response MUST indicate the Qdrant service is healthy

#### Scenario: API queries health endpoint when Qdrant is inaccessible

- GIVEN the Qdrant container is stopped or network is unreachable
- WHEN a client sends a GET request to `/health`
- THEN the API MUST return an HTTP 503 Service Unavailable status
- AND the response MUST indicate the Qdrant service is unhealthy

### Requirement: Clean Architecture Compliance

The Qdrant SDK, connection logic, and health check implementations MUST remain strictly within the `Aura.Infrastructure` layer.

#### Scenario: Validation of dependency boundaries

- GIVEN the project structure for `Aura`
- WHEN analyzing dependencies
- THEN `Aura.Domain` and `Aura.Application` MUST NOT contain references to Qdrant libraries or types
- AND `Aura.Infrastructure` MUST contain the `QdrantHealthCheck` implementation