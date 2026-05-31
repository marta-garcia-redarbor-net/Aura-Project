---
name: aura-backlog-slicer
description: "Trigger: partir historia, slice backlog, atomizar tarea, descomponer US. Divide trabajo grande de Aura en slices atómicos, guiables y verificables."
license: Apache-2.0
metadata:
  author: gentleman-programming
  version: "1.0"
---

# aura-backlog-slicer

## Activation Contract

Usa esta skill cuando una historia, épica o tarea sea demasiado grande, ambigua o toque demasiados frentes. Su objetivo es convertir trabajo difuso en subtareas atómicas que el usuario pueda revisar, redirigir y validar sin perder control.

## Hard Rules

- Partí por slices verticales pequeños: contrato, implementación mínima, test y UI mínima si aplica.
- Cada subtarea debe tener un solo objetivo técnico claro.
- Cada subtarea debe dejar evidencia verificable: test, endpoint, pantalla, log o trace.
- Si una tarea no deja evidencia visible o verificable, seguí partiéndola.
- No mezcles varios conectores, varios dominios o varias decisiones arquitectónicas en una sola subtarea.
- Si aparece trabajo fuera de alcance, proponé una subtarea nueva; no lo escondas dentro de otra.

## Decision Gates

| Situación | Acción |
| --- | --- |
| La tarea toca varias capas sin contrato previo | Partir en contrato -> implementación -> exposición -> validación |
| La tarea genera datos visibles para usuario | Incluir subtarea de UI mínima en el mismo sprint |
| La tarea es sólo infraestructura interna | No forzar UI; exigir evidencia técnica verificable |
| El título tiene "y" o "/" | Separar en dos o más subtareas |
| No se puede escribir un DoD concreto | Seguir refinando antes de ejecutar |

## Execution Steps

1. Identificá el objetivo funcional real y el resultado observable esperado.
2. Detectá contratos, capas y riesgos involucrados.
3. Partí el trabajo en subtareas pequeñas y ordenadas.
4. Para cada subtarea, definí: objetivo, DoD, riesgo y dependencia previa si existe.
5. Verificá que cada subtarea pueda validarse por separado en una sesión razonable.
6. Devolvé el backlog refinado en orden de ejecución recomendado.

## Output Contract

Devolver:
- Historia o tarea refinada.
- Lista numerada de subtareas atómicas.
- DoD por subtarea.
- Riesgo por subtarea.
- Orden recomendado de ejecución.
- Alertas si alguna subtarea sigue siendo demasiado grande o poco verificable.

## References

- `docs/ai/05-task-atomization.md`
- `docs/ai/03-delivery-rules.md`
- `docs/ai/06-skill-catalog.md`
- `StoryBacklog.md`
