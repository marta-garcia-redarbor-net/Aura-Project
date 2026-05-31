# Triáje — Motor de interrupciones proactivo

> Placeholder. Este documento debe definir cuándo Aura interrumpe al usuario y cuándo difiere información para proteger foco.

## Quick path

1. Delimitar eventos interruptivos vs batchables.
2. Diseñar políticas por severidad, contexto y presupuesto atencional.
3. Medir falsos positivos, latencia y valor percibido.

## Debe cubrir

- Contrato `IInterruptionPolicyEngine`.
- Umbrales por severidad, seguridad, bloqueos y reuniones.
- Supresión, batching y escalado.
- Observabilidad de interrupciones evitadas/emitidas.
- Tests de reglas y simulaciones de carga mental.

## Pendiente

- [ ] Completar políticas de interrupción y diferimiento.
