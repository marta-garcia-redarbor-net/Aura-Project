# Calidad — Architecture Tests y Quality Gates

Aura no delega el cumplimiento de su Clean Architecture únicamente a revisiones de código manuales (PR reviews). Las reglas de diseño estructural se codifican, se compilan y se evalúan automáticamente.

Para ello, empleamos **ArchUnitNET**, ejecutado como parte de la suite `Aura.ArchitectureTests` (~84 tests).

## Reglas Inquebrantables de Aura

El proyecto define una serie de "Quality Gates" estructurales que rompen la compilación en caso de infracción:

### 1. Invariantes de Capas (Clean Architecture)
- `Domain` no puede referenciar **absolutamente a nadie**. Es el centro del hexágono.
- `Application` solo puede referenciar a `Domain`.
- `Infrastructure`, `Api` y `Workers` pueden referenciar a `Application` y `Domain`, pero **jamás entre sí** (ej. la API no puede instanciar directamente un adaptador de Infraestructura).

### 2. Aislamiento de Casos de Uso
- Los Casos de Uso (`UseCases`) en la capa de Aplicación deben depender exclusivamente de abstracciones/interfaces (Puertos) y no de clases concretas (Adaptadores).
- Solo el registro de Inyección de Dependencias (`StoreRegistrationExtensions.cs`, `DependencyInjection.cs`) tiene permitido referenciar ambas interfaces y concreciones para enlazarlas.

### 3. Exposición de Entidades
- Las clases del Dominio se exponen, pero la persistencia interna (por ejemplo, las entidades de Entity Framework como `SemanticOutboxEntryEntity` en la capa de Infraestructura) deben ser `internal` o estar restringidas para evitar que se filtren hacia el Dominio o la API.

### 4. Naming Conventions y Atributos
- Todos los Casos de Uso deben terminar en la palabra `UseCase`.
- Todos los Adaptadores de integración externa (Graph, Teams) deben terminar en la palabra `Adapter` o `Client`.

## Ejecución Continua

Esta validación estática del código se ejecuta cada vez que se lanza `dotnet test`. Si un desarrollador añade un `using Aura.Infrastructure;` dentro de `Aura.Domain/WorkItems/WorkItem.cs`, el test `DomainShouldNotDependOnOtherLayers` fallará inmediatamente, bloqueando el avance en local e impidiendo la mezcla en el repositorio.