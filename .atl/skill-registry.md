# Skill Registry — Aura

Last updated: 2026-06-24

## Sources scanned

- `C:\Users\marta.garcia\source\repos\Aura\skills\` (project skills)
- `C:\Users\marta.garcia\.config\opencode\skills\` (user skills)
- `C:\Users\marta.garcia\.codex\skills\` (Codex skills)

## Project Skills

| Skill | Trigger / description | Path |
|---|---|---|
| `aura-backlog-slicer` | partir historia, slice backlog, atomizar tarea, descomponer US. Divide trabajo grande en slices atómicos. | `skills/aura-backlog-slicer/SKILL.md` |
| `aura-blazor-ui-slice` | UI incremental, agregar pantalla, slice visual, frontend del feature, blazor component. Diseña slices Blazor Server. | `skills/aura-blazor-ui-slice/SKILL.md` |
| `aura-clean-arch-guard` | verificar arquitectura, clean arch check, architecture guard, dependencias de capa. Valida límites de Clean Architecture. | `skills/aura-clean-arch-guard/SKILL.md` |
| `aura-graphify-query` | explorar codebase, estructura de X, dependencias de Y, donde esta Z. Query the Graphify knowledge graph. | `skills/aura-graphify-query/SKILL.md` |
| `aura-plugin-design` | diseñar plugin, nuevo conector, nuevo adaptador, adaptador por capacidad. Diseña adaptadores intercambiables. | `skills/aura-plugin-design/SKILL.md` |
| `aura-review-evidence` | review evidence, modelo de evidencia, reviewer pipeline, revisión técnica. Reviewer basado en evidencia verificable. | `skills/aura-review-evidence/SKILL.md` |
| `aura-triage-rules` | reglas de triáje, motor de interrupciones, focus state, priority scoring. Reglas de triáje testeables y trazables. | `skills/aura-triage-rules/SKILL.md` |

## SDD Skills

| Skill | Trigger / description | Path |
|---|---|---|
| `sdd-init` | sdd init, iniciar sdd, openspec init. Initialize SDD context. | `~/.config/opencode/skills/sdd-init/SKILL.md` |
| `sdd-explore` | Explore SDD ideas before committing to a change. | `~/.config/opencode/skills/sdd-explore/SKILL.md` |
| `sdd-propose` | Create an SDD change proposal with intent, scope, and approach. | `~/.config/opencode/skills/sdd-propose/SKILL.md` |
| `sdd-spec` | Write SDD delta specs with requirements and scenarios. | `~/.config/opencode/skills/sdd-spec/SKILL.md` |
| `sdd-design` | Create the SDD technical design and architecture approach. | `~/.config/opencode/skills/sdd-design/SKILL.md` |
| `sdd-tasks` | Break an SDD change into implementation tasks. | `~/.config/opencode/skills/sdd-tasks/SKILL.md` |
| `sdd-apply` | Implement SDD tasks from specs and design. | `~/.config/opencode/skills/sdd-apply/SKILL.md` |
| `sdd-verify` | Execute tests and prove implementation matches specs, design, and tasks. | `~/.config/opencode/skills/sdd-verify/SKILL.md` |
| `sdd-archive` | Archive a completed SDD change by syncing delta specs. | `~/.config/opencode/skills/sdd-archive/SKILL.md` |
| `sdd-onboard` | Walk users through the SDD workflow on the real codebase. | `~/.config/opencode/skills/sdd-onboard/SKILL.md` |

## User Skills (relevant)

| Skill | Trigger / description | Path |
|---|---|---|
| `branch-pr` | Create Gentle AI pull requests with issue-first checks. | `~/.config/opencode/skills/branch-pr/SKILL.md` |
| `chained-pr` | PRs over 400 lines, stacked PRs, review slices. | `~/.config/opencode/skills/chained-pr/SKILL.md` |
| `cognitive-doc-design` | Design docs that reduce cognitive load. | `~/.config/opencode/skills/cognitive-doc-design/SKILL.md` |
| `comment-writer` | Write warm, direct collaboration comments. | `~/.config/opencode/skills/comment-writer/SKILL.md` |
| `issue-creation` | Create Gentle AI issues with issue-first checks. | `~/.config/opencode/skills/issue-creation/SKILL.md` |
| `judgment-day` | Blind dual review, fix confirmed issues, re-judge. | `~/.config/opencode/skills/judgment-day/SKILL.md` |
| `skill-creator` | Create LLM-first skills with valid frontmatter. | `~/.config/opencode/skills/skill-creator/SKILL.md` |
| `skill-improver` | Audit and upgrade existing LLM-first skills. | `~/.config/opencode/skills/skill-improver/SKILL.md` |
| `work-unit-commits` | Plan commits as reviewable work units. | `~/.config/opencode/skills/work-unit-commits/SKILL.md` |

## Loading protocol

1. Match task context and target files against the `Trigger / description` column.
2. Pass only the matching `Path` values to the subagent under `## Skills to load before work`.
3. Instruct the subagent to read those exact `SKILL.md` files before work.
4. If no matching skill exists, proceed without project skill injection and report `skill_resolution: none`.
