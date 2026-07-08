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

### Principios generales

- Autenticación recomendada: **Managed Identity / Entra ID** para Graph, **GitHub App** para PRs.
- Validar toda entrada antes de procesar (OWASP Input Validation).
- No loguear secretos, tokens ni PII.
- Aplicar least privilege en permisos de Graph y GitHub.
- No committear credenciales, tokens, API keys ni secretos en el repositorio.

### OWASP Top 10 — Cobertura obligatoria

| OWASP | Medida | Verificación |
|-------|--------|-------------|
| **A01** Broken Access Control | Endpoints requieren auth por defecto; autorización explícita por rol | Cada endpoint nuevo debe tener `.RequireAuthorization()` o justificación documentada |
| **A02** Cryptographic Failures | HTTPS en producción, JWT con algoritmo seguro, secrets fuera del repo | Todo endpoint en producción debe servir por HTTPS |
| **A03** Injection | Consultas parametrizadas (SQLite/EF Core), FluentValidation en DTOs | No concatenar strings en queries SQL |
| **A04** Insecure Design | Rate limiting, validación de entrada, principio de mínimo privilegio | Toda entrada de usuario pasa por validador |
| **A05** Security Misconfiguration | Security headers (CSP, X-Frame-Options, HSTS), CORS acotado | No deshabilitar security headers sin aprobación |
| **A06** Vulnerable Components | Dependabot semanal + `dotnet list package --vulnerable` en CI | No mergear PRs con vulnerabilidades critical/high |
| **A07** Identification/Auth Failures | Entra ID delegado, MSAL token cache, renovación silent | No implementar auth custom ni almacenar passwords |
| **A09** Logging/Monitoring | Correlation ID en toda request, logs estructurados, panel de errores | Cada flujo completo debe tener trazabilidad |

### MITRE ATT&CK — Defensas implementadas

| Táctica MITRE | Defensa en Aura |
|---------------|-----------------|
| **TA0001** Initial Access | Autenticación Entra ID delegada; sin endpoints públicos sin auth |
| **TA0005** Defense Evasion | Logs estructurados con correlation ID; auditoría de decisiones de triaje |
| **TA0006** Credential Access | Secrets fuera del repo; MSAL cache en SQLite cifrada por el SO |
| **TA0007** Discovery | Rate limiting evita enumeración de endpoints |
| **TA0009** Collection | CORS acotado impide exfiltración desde otros orígenes |
| **TA0010** Exfiltration | Rate limiting + security headers mitigan filtración de datos |

### Checklist pre-commit para el agente

- [ ] ¿El código expone credenciales, tokens o secretos?
- [ ] ¿Las queries usan parámetros (no concatenación)?
- [ ] ¿Los endpoints nuevos requieren autenticación?
- [ ] ¿Las respuestas HTTP incluyen security headers?
- [ ] ¿Los DTOs de entrada tienen validación?
- [ ] ¿Se ha ejecutado `dotnet list package --vulnerable` y no hay critical/high?

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
