# Delta for Environment Configuration

## ADDED Requirements

### Requirement: HTTPS Redirect in Production

The system MUST redirect all HTTP requests to HTTPS when running in non-Development environments. In Development, HTTP MUST remain accessible without redirection.

#### Scenario: Production redirects HTTP to HTTPS

- GIVEN `ASPNETCORE_ENVIRONMENT` is `Production`
- WHEN an HTTP request arrives on port 8080
- THEN the system responds with HTTP 307 redirect to the HTTPS URL
- AND the Location header uses the `https://` scheme

#### Scenario: Development allows plain HTTP

- GIVEN `ASPNETCORE_ENVIRONMENT` is `Development`
- WHEN an HTTP request arrives
- THEN the request is processed normally without redirection
