# Catálogo de skills — Aura

Propuesta de skills de proyecto para guiar a los agentes IA en flujos repetibles y específicos de Aura.
Ninguna skill está implementada aún; este documento es el backlog de creación.

---

## Formato de cada entrada

- **Trigger:** palabras clave que activan la skill.
- **Propósito:** qué hace la skill.
- **Cuándo usar:** contexto de activación.
- **Cuándo NO usar:** límites explícitos.
- **Prioridad:** alta / media / baja.

---

## Catálogo

### 1. `aura-backlog-slicer`

**Estado:** Implementada en [`skills/aura-backlog-slicer/SKILL.md`](../../skills/aura-backlog-slicer/SKILL.md)

| Campo | Detalle |
|-------|---------|
| Trigger | "partir historia", "slice backlog", "atomizar tarea", "descomponer US" |
| Propósito | Partir una User Story o épica en tareas atómicas y guiables siguiendo el patrón de slice vertical de Aura. |
| Cuándo usar | Siempre que se recibe una historia nueva o una tarea demasiado grande. |
| Cuándo NO usar | Para tareas técnicas de infraestructura sin User Story asociada. |
| Prioridad | **Alta** — es el flujo de planificación más frecuente. |

---

### 2. `aura-clean-arch-guard`

**Estado:** Implementada en [`skills/aura-clean-arch-guard/SKILL.md`](../../skills/aura-clean-arch-guard/SKILL.md)

| Campo | Detalle |
|-------|---------|
| Trigger | "verificar arquitectura", "dependencias de capa", "clean arch check", "architecture guard" |
| Propósito | Verificar que un cambio propuesto no viola las reglas de dependencia entre capas de Aura antes de escribir código. |
| Cuándo usar | Al inicio de cualquier tarea que toca más de una capa. |
| Cuándo NO usar | Para cambios puramente dentro de `Infrastructure` que no afectan contratos. |
| Prioridad | **Alta** — evita deuda arquitectónica desde el primer commit. |

---

### 3. `aura-ui-progress-slice`

| Campo | Detalle |
|-------|---------|
| Trigger | "UI incremental", "agregar pantalla", "slice visual", "frontend del feature" |
| Propósito | Definir el componente o pantalla mínima que acompaña un slice de backend, respetando la estrategia de UI incremental. |
| Cuándo usar | Cuando se completa un caso de uso que produce datos visibles para el usuario. |
| Cuándo NO usar | Para tareas internas de infraestructura sin impacto directo en pantalla (ej. circuit breaker). |
| Prioridad | **Alta** — es parte del DoD en Aura. |

---

### 4. `aura-plugin-design`

**Estado:** Implementada en [`skills/aura-plugin-design/SKILL.md`](../../skills/aura-plugin-design/SKILL.md)

| Campo | Detalle |
|-------|---------|
| Trigger | "diseñar plugin", "nuevo conector", "nuevo adaptador", "plugin design" |
| Propósito | Guiar el diseño de un nuevo conector o adaptador externo respetando Ports & Adapters, resiliencia y telemetría desde el primer día. |
| Cuándo usar | Al agregar cualquier integración externa nueva (Graph, GitHub, SonarQube, etc.). |
| Cuándo NO usar | Para refactoring interno de un adaptador ya existente sin cambios de contrato. |
| Prioridad | **Alta** — todas las integraciones externas pasan por este patrón. |

---

### 5. `aura-triage-rules`

| Campo | Detalle |
|-------|---------|
| Trigger | "reglas de triáje", "motor de interrupciones", "focus state", "priority scoring", "triage rules" |
| Propósito | Documentar y aplicar las reglas de dominio del sistema de triáje: estados de foco, criterios de interrupción y scoring de prioridad. |
| Cuándo usar | Al implementar o modificar lógica del `FocusStateMachine`, `InterruptionPolicyEngine` o `PriorityScoringService`. |
| Cuándo NO usar | Para cambios en la capa de presentación del triáje sin lógica de dominio nueva. |
| Prioridad | **Alta** — el triáje es el corazón diferenciador de Aura. |

---

### 6. `aura-review-evidence`

| Campo | Detalle |
|-------|---------|
| Trigger | "review evidence", "modelo de evidencia", "reviewer pipeline", "revisión técnica" |
| Propósito | Guiar la construcción o modificación del pipeline de revisión: SonarQube + Dependabot + OWASP/MITRE + validación semántica. |
| Cuándo usar | Al implementar cualquier paso del pipeline del Reviewer o al agregar un nuevo proveedor de análisis. |
| Cuándo NO usar | Para cambios en la UI del reviewer sin modificación del pipeline. |
| Prioridad | **Media** — el Reviewer es fase 2-3 del proyecto. |

---

### 7. `aura-demo-mode`

| Campo | Detalle |
|-------|---------|
| Trigger | "demo mode", "datos de demo", "modo demo", "sandbox data" |
| Propósito | Activar o generar datos de prueba realistas para demostrar Aura sin conectores reales (Graph, GitHub, SonarQube). |
| Cuándo usar | Para demos, onboarding, validación de UI o pruebas E2E sin dependencias externas. |
| Cuándo NO usar | En ambientes de producción o staging con conectores reales activos. |
| Prioridad | **Media** — necesario para iterar la UI sin bloquear en infraestructura. |

---

### 8. `aura-tfm-doc-writer`

| Campo | Detalle |
|-------|---------|
| Trigger | "documentar TFM", "escribir doc técnica", "architecture decision record", "ADR", "doc de diseño" |
| Propósito | Producir documentación técnica de arquitectura (ADRs, overview de dominio, fichas de contrato) en el estilo y estructura de `docs/architecture/`. |
| Cuándo usar | Al documentar una decisión arquitectónica, un nuevo dominio o un contrato de interfaz. |
| Cuándo NO usar | Para documentación operacional de usuario final o guías de instalación. |
| Prioridad | **Baja** — útil pero no bloqueante para el desarrollo. |

---

## Primer batch recomendado para implementar

Implementar en este orden, antes de la Semana 2 del sprint:

| Orden | Skill | Razón |
|-------|-------|-------|
| 1 | `aura-backlog-slicer` | Se usa en cada sesión de planificación. ROI inmediato. |
| 2 | `aura-clean-arch-guard` | Previene deuda desde el primer commit de implementación. |
| 3 | `aura-plugin-design` | Se activa en la primera semana al crear conectores de Graph/GitHub. |
| 4 | `aura-ui-progress-slice` | Necesario para cumplir el DoD de UI incremental desde Semana 2. |
| 5 | `aura-triage-rules` | Semana 3; el triáje tiene reglas de dominio complejas que merecen skill dedicada. |

Las skills 6-8 (`review-evidence`, `demo-mode`, `tfm-doc-writer`) pueden esperar a Semana 3-4.
