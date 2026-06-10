# API Authentication Specification

## Purpose

Defines how users authenticate with the Aura API and how their identity context is established across layers without coupling to external identity providers during local development.

## Requirements

### Requirement: Mock Login Generation

The system MUST provide an endpoint to generate a local, symmetric JWT for development purposes.

#### Scenario: Successful Mock Login

- GIVEN the application is running in a development environment
- WHEN a client sends a POST request to `/api/auth/mock-login`
- THEN the API returns a valid JWT token containing basic mock user claims

### Requirement: API Authorization Enforcement

The system MUST enforce authentication on protected endpoints using JWT Bearer token validation.

#### Scenario: Access without token

- GIVEN a protected API endpoint
- WHEN a client sends a request without an Authorization header
- THEN the API rejects the request with a 401 Unauthorized status

#### Scenario: Access with valid mock token

- GIVEN a valid JWT token obtained from the mock login endpoint
- WHEN a client sends a request to a protected API endpoint with the token as a Bearer token
- THEN the API accepts the request and processes it successfully

### Requirement: Identity Decoupling

The system MUST represent the current authenticated user inside the Application layer using pure domain models, completely decoupled from infrastructure SDKs or Microsoft Entra ID.

#### Scenario: Retrieving current user context

- GIVEN a valid authenticated request
- WHEN an Application layer service requests the current user via `ICurrentUserService`
- THEN it receives an `AuraUser` model containing only domain-relevant identity information
