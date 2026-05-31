# Triáje — Morning Summary

> Placeholder. Este documento debe definir el resumen ejecutivo diario de las 09:00 AM con foco en impacto, urgencia y claridad.

## Quick path

1. Definir inputs: calendario, correos, PRs, bloqueos y prioridades.
2. Diseñar composición del resumen por usuario y timezone.
3. Validar formato, ranking y señales de seguimiento.

## Debe cubrir

- Contratos `IMorningSummaryScheduler` y `IMorningSummaryComposer`.
- Reglas de priorización y agrupación.
- Generación idempotente por ventana diaria.
- Telemetría de entrega, lectura y utilidad.
- Tests de composición y edge cases temporales.

## Pendiente

- [ ] Completar formato final, ranking y condiciones de envío del resumen.
