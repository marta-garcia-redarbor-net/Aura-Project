---
name: aura-plugin-design
description: "Trigger: diseñar plugin, nuevo conector, nuevo adaptador, plugin design, conector intercambiable, adaptador por capacidad. Diseña adaptadores intercambiables por capacidad del dominio en Aura." 
license: Apache-2.0
metadata:
  author: gentleman-programming
  version: "1.0"
---

# aura-plugin-design

## Activation Contract

Usa esta skill al diseñar una integración externa nueva o al revisar si un adaptador está demasiado acoplado a un proveedor concreto. Su objetivo es definir conectores intercambiables por capacidad del dominio, no por marca del proveedor.

## Hard Rules

- Diseñá puertos por capacidad del dominio, no por proveedor.
- Si dos proveedores resuelven la misma capacidad, deben colgar del mismo puerto.
- Los tipos del SDK externo no pueden salir de `Infrastructure`.
- Todo adaptador debe mapear a un modelo canónico propio de Aura.
- Exigí idempotencia, checkpoints, resiliencia y telemetría desde el primer día.
- Si el diseño habla en términos de `Teams`, `Slack`, `Graph` o `GitHub` dentro de `Domain` o `Application`, está mal cortado.

## Decision Gates

| Situación | Acción |
| --- | --- |
| Hay varios proveedores para la misma capacidad | Definir un solo puerto del dominio y varias implementaciones en Infrastructure |
| El proveedor expone tipos ricos del SDK | Traducirlos en el adaptador a DTOs/modelos canónicos propios |
| La integración requiere sincronización incremental | Diseñar checkpoint store e idempotencia desde el inicio |
| El adaptador produce datos visibles o accionables | Exigir telemetría y UI mínima si aplica al slice |
| La propuesta nombra el puerto por proveedor | Renombrar por capacidad del dominio antes de avanzar |

## Execution Steps

1. Identificá la capacidad del negocio que se quiere cubrir.
2. Definí el puerto del dominio con nombre neutral al proveedor.
3. Diseñá el modelo canónico que el puerto debe producir o consumir.
4. Ubicá cada proveedor como implementación intercambiable dentro de `Infrastructure`.
5. Añadí requirements de checkpoint, idempotencia, resiliencia y telemetría.
6. Devolvé el diseño recomendando nombres, límites y riesgos de acoplamiento.

## Output Contract

Devolver:
- Capacidad del dominio identificada.
- Puerto recomendado con nombre neutral.
- Implementaciones proveedor-específicas candidatas.
- Modelo canónico a mapear.
- Requisitos de idempotencia, checkpoint, resiliencia y telemetría.
- Riesgos de acoplamiento detectados.
- Recomendación de estructura por carpetas/capas si aplica.

## References

- `docs/ai/02-architecture-map.md`
- `docs/ai/03-delivery-rules.md`
- `docs/ai/06-skill-catalog.md`
- `Agents.md`
