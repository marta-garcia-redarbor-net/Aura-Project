# Aura — Visión general y flujos

Aura reduce la carga mental del equipo de ingeniería ingiriendo señales de múltiples fuentes (Teams, Outlook, Calendar, GitHub), priorizándolas según contexto cognitivo del usuario y ejecutando revisiones técnicas híbridas con evidencia verificable.

---

## Flujos end-to-end

### Ingestión → Triáje

```
Conector externo
  → normaliza a WorkItem canónico
  → scoring (impacto, deadline, dependencias, riesgo)
  → FocusStateMachine decide:
      DeepWork     → sólo severidad alta, seguridad o reunión inminente
      Window       → batch de baja/media urgencia
      09:00 AM     → Morning Summary ejecutivo
```

### PR → Revisión inteligente

```
GitHub PR ingresa
  → SonarQube: code smells, bugs, coverage gates
  → Dependabot: librerías vulnerables, severidad, fix disponible
  → OWASP/MITRE: riesgos según contexto del cambio
  → SemanticValidator: diff + tests + acceptance criteria
  → ReveiwDecisionEngine:
      Approved | Changes Requested | Security Escalation | Needs Human Review
```

### Observabilidad transversal

```
Cada paso emite Activity (OpenTelemetry)
  → correlation id por request/job/workflow
  → métricas: tokens, costo, latencia p50/p95/p99, retry count
  → dashboards: costo por feature/plugin/modelo, latencia por proveedor
```

---

## Siguientes ficheros

- Reglas operativas para agentes IA → [`01-operating-rules.md`](./01-operating-rules.md)
- Mapa de capas y contratos → [`02-architecture-map.md`](./02-architecture-map.md)
