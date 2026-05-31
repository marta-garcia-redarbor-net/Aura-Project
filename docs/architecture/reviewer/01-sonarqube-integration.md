# Reviewer — Integración con SonarQube API

> Placeholder. Este documento debe definir cómo Aura consulta SonarQube y transforma findings en evidencia de revisión.

## Quick path

1. Delimitar proyectos, branches y quality gates a consultar.
2. Diseñar proveedor `IStaticAnalysisProvider`.
3. Mapear findings a severidad, trazabilidad y decisión.

## Debe cubrir

- Endpoints/API de SonarQube a usar.
- Manejo de autenticación, timeouts y retries.
- Normalización de issues, coverage y maintainability.
- Correlación con PR, branch y commit.
- Tests de integración y contratos.

## Pendiente

- [ ] Completar contrato de consumo y mapping de SonarQube.
