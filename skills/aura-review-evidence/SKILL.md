---
name: aura-review-evidence
description: "Trigger: review evidence, modelo de evidencia, reviewer pipeline, revisión técnica, comentarios en PR, reglas del reviewer. Diseña el reviewer de Aura basado en evidencia verificable y reglas con precedencia clara." 
license: Apache-2.0
metadata:
  author: gentleman-programming
  version: "1.0"
---

# aura-review-evidence

## Activation Contract

Usa esta skill al diseñar o modificar el pipeline del Reviewer de Aura. Su objetivo es que toda decisión y comentario en PR se base en evidencia verificable, reglas trazables y una precedencia explícita de fuentes, nunca en opinión aislada.

## Hard Rules

- El reviewer combina evidencia técnica, seguridad, semántica y contexto del proyecto; no depende de una sola señal.
- El LLM puede ayudar, pero nunca ser la única fuente del veredicto.
- Las reglas deben resolverse con precedencia clara: instrucciones explícitas del usuario, políticas organizacionales, reglas del proyecto, reglas base universales y sugerencias del historial.
- Reglas, overrides, historial y feedback viven en un store estructurado y auditable; Qdrant sólo puede ser apoyo semántico opcional.
- Todo comentario o decisión debe explicar qué regla aplicó, de qué fuente vino y qué evidencia la sostiene.
- Comentario resumen en PR: sí. Comentarios inline: sólo con alta confianza, archivo/línea clara y recomendación accionable.

## Decision Gates

| Situación | Acción |
| --- | --- |
| Falta evidencia suficiente | Escalar a revisión humana o marcar decisión incompleta |
| Hay conflicto entre reglas | Resolver por precedencia y dejar traza del origen |
| El hallazgo no tiene archivo/línea clara | Evitar comentario inline; usar comentario resumen si procede |
| El historial sugiere una regla nueva | Proponer sugerencia auditable, no autoaplicar silenciosamente |
| La PR viola una convención del proyecto | Citar la regla del repo o de la organización junto al hallazgo |

## Execution Steps

1. Identificá las fuentes de reglas y su precedencia.
2. Separá evidencia por tipo: estática, dependencias, seguridad, semántica y contexto del proyecto.
3. Diseñá el modelo de decisión con score, thresholds, explicación y escalado humano.
4. Definí stores para reglas, overrides, historial, feedback y comentarios publicados.
5. Establecé la política de publicación: comentario resumen y condiciones para inline comments.
6. Devolvé la propuesta con contratos, fuentes de verdad, riesgos y límites del sistema.

## Output Contract

Devolver:
- Fuentes de reglas y precedencia.
- Modelo de evidencia y decisión final.
- Contratos sugeridos para rule store, history, feedback y publicación de comentarios.
- Política de comentarios en PR.
- Riesgos de falsos positivos, ruido o caja negra.
- Recomendación de tests y auditoría del reviewer.

## References

- `docs/architecture/reviewer/00-overview.md`
- `docs/architecture/reviewer/01-sonarqube-integration.md`
- `docs/architecture/reviewer/02-dependabot-integration.md`
- `docs/architecture/reviewer/03-owasp-mitre-audit.md`
- `docs/architecture/reviewer/04-semantic-story-validation.md`
- `docs/architecture/reviewer/05-review-evidence-model.md`
- `docs/ai/02-architecture-map.md`
- `docs/ai/06-skill-catalog.md`
