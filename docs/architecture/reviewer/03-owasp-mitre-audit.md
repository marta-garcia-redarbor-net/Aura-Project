# Reviewer — Auditoría OWASP y MITRE

> Placeholder. Este documento debe definir cómo se auditan riesgos de seguridad usando OWASP, CWE y MITRE ATT&CK/CAPEC.

## Quick path

1. Identificar superficies de ataque por tipo de cambio.
2. Diseñar reglas y evidencias por categoría de riesgo.
3. Establecer criterios de escalado a security review.

## Debe cubrir

- Contrato `ISecurityAuditEngine`.
- Taxonomía OWASP/CWE/MITRE aplicable.
- Reglas por contexto: auth, secrets, input validation, data exposure.
- Evidencia mínima para `Security Escalation`.
- Tests de reglas críticas y casos borde.

## Pendiente

- [ ] Completar catálogo de controles y criterios de severidad.
