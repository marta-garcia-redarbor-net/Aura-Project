---
name: aura-triage-rules
description: "Trigger: reglas de triáje, motor de interrupciones, focus state, priority scoring, morning summary rules, feedback de triáje. Diseña reglas de triáje explícitas, testeables y trazables en Aura." 
license: Apache-2.0
metadata:
  author: gentleman-programming
  version: "1.0"
---

# aura-triage-rules

## Activation Contract

Usa esta skill al diseñar o modificar la lógica de triáje de Aura: estados de foco, scoring, interrupciones, morning summary y aprendizaje explícito. Su objetivo es mantener reglas separadas, explicables y controladas por el usuario.

## Hard Rules

- Separá `Focus State`, `Priority Scoring`, `Interruption Policy` y `Morning Summary Rules`.
- Toda decisión de triáje debe poder explicarse en lenguaje humano.
- El aprendizaje empieza por preferencias persistidas, feedback explícito e historial de decisiones; no por autoaprendizaje opaco.
- No mezcles reglas de dominio con UI, controllers o componentes Blazor.
- Si una regla no puede testearse o explicarse, todavía no está lista.
- No recalibres automáticamente reglas críticas sin confirmación del usuario.

## Decision Gates

| Situación | Acción |
| --- | --- |
| Se diseña estado de foco | Modelar transiciones, guards y señales de entrada por separado |
| Se diseña scoring | Definir factores, pesos, explainability y tests de regresión |
| Se decide interrumpir o diferir | Exigir razón explícita y política auditable |
| El usuario da feedback o una instrucción nueva | Persistir preferencia, override o feedback; no convertirlo en magia implícita |
| Se propone aprendizaje automático silencioso | Rechazarlo o convertirlo en sugerencia auditable para aprobación humana |

## Execution Steps

1. Identificá qué parte del triáje estás tocando: foco, scoring, interrupción, summary o aprendizaje.
2. Separá inputs, reglas, outputs y explicación humana esperada.
3. Definí contratos para preferencias, historial, feedback y overrides si el cambio afecta aprendizaje.
4. Verificá que cada decisión pueda trazarse a señales concretas.
5. Asegurá tests de dominio y regresión para reglas críticas.
6. Devolvé la propuesta con reglas, riesgos y límites de aprendizaje explícitos.

## Output Contract

Devolver:
- Reglas separadas por responsabilidad.
- Inputs, outputs y explicación humana de cada decisión.
- Contratos sugeridos para preferencias, feedback, historial y overrides si aplica.
- Riesgos de ruido, sesgo o caja negra.
- Recomendación de tests de dominio y regresión.
- Señales de que una política debe seguir refinándose antes de implementar.

## References

- `docs/architecture/triage/00-overview.md`
- `docs/architecture/triage/01-morning-summary.md`
- `docs/architecture/triage/02-proactive-interruptions.md`
- `docs/architecture/triage/03-focus-state-machine.md`
- `docs/architecture/triage/04-priority-scoring.md`
- `docs/ai/03-delivery-rules.md`
- `docs/ai/06-skill-catalog.md`
