# Reglas de operación para agentes IA

Reglas que todo agente debe respetar al trabajar en el código de Aura. Son no negociables.

---

## Antes de generar código

1. Identificá a qué capa pertenece el cambio: Domain, Application, Infrastructure, Api, Workers.
2. Verificá que no estás introduciendo dependencias cruzadas prohibidas (ver [`02-architecture-map.md`](./02-architecture-map.md)).
3. Si hay un skill disponible para la tarea, cargalo antes de responder.
4. Confirmá que el cambio tiene tests y telemetría en el mismo scope.

---

## Reglas de colaboración

- Las respuestas al usuario deben ser concisas por defecto, sin omitir decisiones, riesgos ni siguiente paso.
- Evitá redundancia, contexto accesorio y explicaciones largas si no cambian la decisión.
- Al proponer tests, priorizá sólo los que aportan valor real sobre comportamiento, riesgo o integración.
- No agregar tests que sólo validen cableado trivial, duplican cobertura existente o no cambian la confianza sobre el cambio.

---

## Reglas de código

| Regla | Obligatorio |
|-------|-------------|
| Async/await en toda la pila I/O | Sí |
| `HttpClientFactory` para clientes HTTP | Sí |
| Resiliencia: timeout + retry con jitter + circuit breaker | Sí |
| Correlation ID en cada request/job/workflow | Sí |
| Un solo punto de mapeo por frontera de capa | Sí |
| Inyección de dependencias por interfaz | Sí |
| Sin lógica de negocio en controllers/endpoints | Sí |
| Sin referencias a `Infrastructure` desde `Domain` | Sí |

---

## Reglas de seguridad

- Autenticación recomendada: **Managed Identity / Entra ID** para Graph, **GitHub App** para PRs.
- Validar toda entrada antes de procesar.
- No loguear secretos, tokens ni PII.
- Aplicar least privilege en permisos de Graph y GitHub.

---

## Reglas de observabilidad

- Toda operación relevante emite un `Activity` (OpenTelemetry) con correlation id.
- Métricas IA: input tokens, output tokens, costo estimado, modelo usado.
- Nunca instrumentar "después"; la telemetría es parte del DoD.

---

## Reglas de calidad

- TDD como estrategia preferida; tests en el mismo commit que el código.
- Menos tests, mejor elegidos: cubrir comportamiento crítico, regresiones probables e integración relevante antes que inflar cobertura mecánica.
- `Domain` sólo depende de abstracciones propias.
- Casos de uso no contienen lógica de transporte ni SDK.
- Conventional Commits obligatorio para versionado y ChangeLog automático.

---

## Cuándo detenerse y preguntar

- Si el alcance de la tarea cruza más de una capa sin contrato claro.
- Si la tarea requiere acceso a secretos o credenciales reales.
- Si la decisión implica un cambio de arquitectura no cubierto en este árbol de docs.
