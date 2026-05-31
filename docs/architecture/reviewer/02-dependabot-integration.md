# Reviewer — Integración con Dependabot

> Placeholder. Este documento debe definir cómo Aura incorpora riesgo de dependencias y recomendaciones de actualización.

## Quick path

1. Definir señales de vulnerabilidad y severidad consumidas.
2. Diseñar proveedor `IDependencyRiskProvider`.
3. Integrar findings al score y a la decisión final.

## Debe cubrir

- Fuentes de alerta y datos de remediation.
- Correlación con paquetes afectados y alcance del cambio.
- Política de bloqueo, warning o escalado.
- Observabilidad del riesgo supply-chain.
- Tests para vulnerabilidades críticas y falsos positivos.

## Pendiente

- [ ] Completar estrategia de riesgo de dependencias y remediación.
