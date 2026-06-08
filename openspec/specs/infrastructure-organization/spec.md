# Infrastructure Organization Specification

## Purpose

Defines the structural organization, boundaries, and dependency injection conventions for the `Aura.Infrastructure` and `Aura.Application` layers. This ensures strict adherence to Clean Architecture principles by isolating external SDKs and centralizing DI registrations.

## Requirements

### Requirement: Adapter-Centric Organization

The system MUST organize all infrastructure implementations by their primary adapter responsibility rather than generic technical concerns.

#### Scenario: Infrastructure file placement
- GIVEN a new or existing infrastructure implementation
- WHEN it is added to the `Aura.Infrastructure` project
- THEN it MUST be placed inside a specific `Adapters/{Responsibility}/` folder or a `Shared/{Concept}/` folder
- AND it MUST NOT be placed in the project root or generic technical folders.

### Requirement: Unified Infrastructure Dependency Injection

The `Aura.Infrastructure` layer MUST expose exactly one unified extension method for dependency injection registration.

#### Scenario: Registering infrastructure services
- GIVEN a host application such as `Aura.Workers`
- WHEN it needs to register infrastructure services
- THEN it MUST call a single `AddAuraInfrastructure` extension method provided by the infrastructure layer
- AND individual adapter DI extensions MUST NOT be directly exposed or required by the host.

### Requirement: Dedicated Application Dependency Injection

The `Aura.Application` layer MUST manage the registration of its own domain and application services independently from infrastructure.

#### Scenario: Registering application services
- GIVEN a host application such as `Aura.Workers`
- WHEN it needs to register application-layer services (e.g., `BasicSemanticChunkExtractor`)
- THEN it MUST call a single `AddAuraApplication` extension method provided by the application layer
- AND application services MUST NOT be registered inside `Aura.Infrastructure` DI extensions.