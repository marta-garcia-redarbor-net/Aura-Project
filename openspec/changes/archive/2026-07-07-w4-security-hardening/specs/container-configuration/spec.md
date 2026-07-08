# Delta for Container Configuration

## ADDED Requirements

### Requirement: HTTPS Port Mapping in Docker Compose

The `docker-compose.yml` MUST expose an HTTPS port mapping for the API service (e.g., host port mapped to container HTTPS port). The `.env.example` MUST document the new HTTPS port variable.

#### Scenario: HTTPS port accessible via Docker Compose

- GIVEN `docker-compose.yml` includes an HTTPS port mapping for `aura-api`
- WHEN `docker compose up` is executed
- THEN the API is reachable via HTTPS on the configured host port

#### Scenario: .env.example documents HTTPS port

- GIVEN `.env.example` exists at the repository root
- WHEN its contents are inspected
- THEN an `API_HTTPS_PORT` (or equivalent) variable is listed with a placeholder and comment
