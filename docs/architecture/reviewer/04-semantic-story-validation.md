# Reviewer — Validación semántica contra User Story

> Placeholder. Este documento debe definir cómo Aura valida si el cambio realmente cumple la User Story y sus acceptance criteria.

## Quick path

1. Estructurar User Story y acceptance criteria en un formato trazable.
2. Diseñar `IUserStoryTraceabilityService` y `ISemanticRequirementValidator`.
3. Combinar diff, tests y evidencia técnica para decidir cumplimiento.

## Debe cubrir

- Estrategia de trazabilidad requisito → diff → test.
- Reglas para evidencia insuficiente o ambigua.
- Uso de LLM como apoyo, no como única fuente.
- Explainability de la decisión semántica.
- Tests de validación con historias cumplidas e incumplidas.

## Pendiente

- [ ] Completar mecanismo de trazabilidad semántica y sus límites.
