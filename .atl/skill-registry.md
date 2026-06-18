# Aura Skill Registry

Generated for project-level delegation.

## Registry contract

- Source of truth: each `SKILL.md`
- Scope: project skills only
- Excluded: `sdd-*`, `_shared`, and `skill-registry`
- Precedence: project-level skills win over broader user-level skills

## Indexed skills

| Skill | Trigger | Purpose | Scope | Path |
|---|---|---|---|---|
| aura-backlog-slicer | partir historia, slice backlog, atomizar tarea, descomponer US | Split large Aura work into atomic, verifiable slices | project | `skills/aura-backlog-slicer/SKILL.md` |
| aura-clean-arch-guard | verificar arquitectura, dependencias de capa, clean arch check, architecture guard | Validate Clean Architecture boundaries before implementation | project | `skills/aura-clean-arch-guard/SKILL.md` |
| aura-blazor-ui-slice | UI incremental, agregar pantalla, slice visual, frontend del feature | Define the minimum Blazor Server UI slice for a backend change | project | `skills/aura-blazor-ui-slice/SKILL.md` |
| aura-plugin-design | diseñar plugin, nuevo conector, nuevo adaptador, plugin design | Design new external connectors/adapters with ports, resilience, telemetry | project | `skills/aura-plugin-design/SKILL.md` |
| aura-triage-rules | reglas de triáje, motor de interrupciones, focus state, priority scoring | Apply triage domain rules and scoring behavior | project | `skills/aura-triage-rules/SKILL.md` |
| aura-review-evidence | review evidence, modelo de evidencia, reviewer pipeline, revisión técnica | Guide the reviewer pipeline and evidence handling | project | `skills/aura-review-evidence/SKILL.md` |

## Delegation note

Pass the exact `SKILL.md` path to subagents; do not paraphrase skill intent into compact rules.
