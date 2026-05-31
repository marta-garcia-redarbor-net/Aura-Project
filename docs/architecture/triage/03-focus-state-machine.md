# Triáje — Focus State Machine

> Placeholder. Este documento debe modelar los estados `Deep Work`, `Window of Opportunity`, `Away` y `Recovery`.

## Quick path

1. Definir estados, transiciones y señales de entrada.
2. Diseñar políticas dependientes del estado.
3. Verificar consistencia con tests de dominio y métricas.

## Debe cubrir

- Contrato `IFocusStateResolver`.
- Transiciones válidas y guards.
- Uso de calendario, actividad y preferencias como señales.
- Reglas de fallback frente a contexto incompleto.
- Tests de transición y de invariantes.

## Pendiente

- [ ] Completar diagrama de estados y reglas de transición.
