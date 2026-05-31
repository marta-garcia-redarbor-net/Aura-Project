# Calidad — Architecture Tests y quality gates

> Placeholder. Este documento debe definir cómo Aura protege Clean Architecture con tests y quality gates automatizados.

## Quick path

1. Definir invariantes arquitectónicos que nunca deben romperse.
2. Diseñar `Aura.ArchitectureTests` y reglas por capa.
3. Integrar gates en CI/CD con feedback claro.

## Debe cubrir

- Reglas de dependencia entre `Domain`, `Application`, `Infrastructure` y `Api`.
- Tests para puertos/adaptadores y separación de concerns.
- Gates mínimos de cobertura, análisis y seguridad.
- Política de bloqueo vs warning.
- Reportes y troubleshooting de fallas de arquitectura.

## Pendiente

- [ ] Completar architecture tests y quality gates iniciales.
