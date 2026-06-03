# Reglas de entrega

Toda tarea se considera terminada sólo cuando cumple estas condiciones.

---

## Definition of Done (DoD)

- [ ] El código compila sin warnings en modo strict.
- [ ] Tests unitarios cubren las reglas de dominio y casos de uso del cambio.
- [ ] Tests de integración cubren conectores o persistencia si aplica.
- [ ] Architecture Tests no detectan dependencias prohibidas.
- [ ] Telemetría instrumentada: `Activity`, métricas y logs con correlation id.
- [ ] Conventional Commit usado; mensaje describe el "qué" y el "por qué".
- [ ] Sin secretos ni credenciales en código o logs.
- [ ] PR o commit incluye evidencia de que el flujo funciona (test output o trace).
- [ ] Toda entrega tiene trazabilidad suficiente del cambio, aunque no exista issue de GitHub.

---

## Trazabilidad obligatoria, issue opcional

En Aura la **trazabilidad es obligatoria**, pero el **issue de GitHub es opcional**.

Una PR, commit o entrega debe incluir al menos **una** de estas fuentes de contexto:

- [ ] Issue de GitHub
- [ ] Artefacto SDD / OpenSpec (`proposal`, `spec`, `design`, `tasks`)
- [ ] Descripción clara en el cuerpo de la PR indicando intención, alcance, verificación y fuera de alcance

### Reglas

- No se exige `Closes #N` para abrir una PR.
- No se exige label `status:approved` para considerar válida una entrega.
- Si no hay issue, la PR debe explicar claramente:
  - qué problema resuelve
  - qué entra en scope
  - qué queda fuera
  - cómo se verificó
- En PRs encadenadas, cada PR debe indicar su posición en la cadena y el slice siguiente/anterior.
- La ausencia de issue **no** exime de evidencia técnica ni de contexto revisable.

---

## Conventional Commits

```
feat(ingestion): add incremental sync for Graph Calendar connector
fix(triage): correct timezone offset in MorningSummaryScheduler
refactor(reviewer): extract SemanticValidator to Application layer
test(domain): add unit tests for FocusStateMachine transitions
chore(ci): add architecture test gate to build pipeline
```

Scope debe mapear al módulo: `ingestion`, `triage`, `reviewer`, `observability`, `quality`, `api`, `workers`.

---

## Quality gates obligatorios

| Gate | Herramienta | Umbral |
|------|------------|--------|
| Code coverage | xUnit + coverlet | ≥ 80% líneas en Domain/Application |
| Architecture rules | ArchUnitNET o NetArchTest | 0 violaciones |
| Style | EditorConfig + StyleCop | 0 warnings en modo strict |
| Seguridad estática | SonarQube | 0 issues críticos o bloqueantes |
| Dependencias vulnerables | Dependabot | 0 CVEs severity high+ sin mitigar |

---

## Stack tecnológico confirmado

| Capa | Tecnología |
|------|-----------|
| Framework | .NET 8 / ASP.NET Core |
| Frontend | Blazor Server |
| Tests | xUnit, Playwright (E2E) |
| Resiliencia | Polly |
| Observabilidad | OpenTelemetry (.NET SDK) |
| Linting | EditorConfig + StyleCop |
| Vector store | Qdrant |
| Contenedores | Docker + Docker Compose |
