---
name: aura-clean-arch-guard
description: "Trigger: verificar arquitectura, clean arch check, architecture guard, dependencias de capa, esto en qué capa va. Valida límites de Clean Architecture en Aura antes de implementar." 
license: Apache-2.0
metadata:
  author: gentleman-programming
  version: "1.0"
---

# aura-clean-arch-guard

## Activation Contract

Usa esta skill antes de diseñar o implementar cambios que toquen varias capas o generen dudas sobre dónde debe vivir una responsabilidad. Su función es detectar violaciones de arquitectura antes de escribir código y proponer una estructura correcta.

## Hard Rules

- `Domain` no depende de `Infrastructure`, SDKs, transporte ni frameworks externos.
- `Application` contiene casos de uso y contratos; no conoce SDKs externos.
- `Infrastructure` implementa adaptadores, persistencia, observabilidad y conectores externos.
- `Api` y `Workers` orquestan entrada/salida; no absorben lógica de dominio compleja.
- Las dependencias externas entran sólo por puertos y adaptadores.
- Si una tarea toca varias capas sin contratos definidos, detené la implementación y proponé primero puertos, DTOs y límites.

## Decision Gates

| Situación | Acción |
| --- | --- |
| Hay duda sobre en qué capa va algo | Clasificar por responsabilidad, no por comodidad |
| Un caso de uso necesita datos externos | Definir puerto en Application e implementar adaptador en Infrastructure |
| Un endpoint contiene reglas de negocio | Mover la regla a Domain/Application |
| La tarea introduce SDKs o HTTP clients | Confinarlos a Infrastructure |
| El cambio mezcla varias decisiones arquitectónicas | Pedir dividir la tarea antes de implementar |

## Execution Steps

1. Identificá las capas afectadas por la propuesta.
2. Clasificá cada responsabilidad: dominio, caso de uso, adaptador, transporte, observabilidad o background.
3. Detectá dependencias prohibidas o acoplamientos tempranos.
4. Si faltan contratos, proponé primero interfaces, DTOs y fronteras.
5. Emití un veredicto: válido, riesgoso o incorrecto.
6. Devolvé la estructura recomendada antes de permitir implementación.

## Output Contract

Devolver:
- Veredicto arquitectónico.
- Capas afectadas.
- Violaciones o riesgos detectados.
- Ubicación correcta recomendada para cada responsabilidad.
- Contratos/puertos que faltan si aplica.
- Recomendación de partir la tarea si sigue siendo arquitectónicamente ambigua.

## References

- `docs/ai/02-architecture-map.md`
- `docs/ai/03-delivery-rules.md`
- `AGENTS.md`
- `docs/ai/06-skill-catalog.md`
