---
name: aura-blazor-ui-slice
description: "Trigger: UI incremental, agregar pantalla, slice visual, frontend del feature, blazor component. Diseña slices de UI en Blazor Server alineados con Aura.Api y Playwright." 
license: Apache-2.0
metadata:
  author: gentleman-programming
  version: "1.0"
---

# aura-blazor-ui-slice

## Activation Contract

Usa esta skill cuando un slice funcional de Aura necesite representación visual en el dashboard o en una pantalla nueva de Blazor Server. Su objetivo es mantener UI visible, incremental y sin lógica de negocio duplicada.

## Hard Rules

- Cada slice visible debe dejar al menos una página, panel o componente navegable.
- La UI consume sólo DTOs y endpoints de `Aura.Api`.
- No pongas lógica de negocio en componentes Blazor; sólo presentación, estado de vista y navegación.
- Diseñá componentes pequeños, con nombres claros y responsabilidad única.
- Si el slice produce datos visibles, añadí estados de carga, vacío y error.
- Prepará selectores o estructuras estables para Playwright cuando el flujo sea crítico.

## Decision Gates

| Situación | Acción |
| --- | --- |
| El backend produce datos nuevos visibles | Crear componente o panel mínimo en el mismo slice |
| La pantalla necesita refresco en vivo | Evaluar SignalR sin meter lógica de negocio en el componente |
| El cambio afecta varias vistas | Partir en componentes pequeños antes de maquetar todo |
| No existe endpoint o DTO claro | Pedir contrato en `Aura.Api` antes de construir UI |
| El componente empieza a decidir reglas de negocio | Mover esa lógica a Application/Api y dejar sólo estado de vista |

## Execution Steps

1. Identificá qué resultado del slice debe verse en pantalla.
2. Elegí la mínima representación útil: página, panel, tarjeta, tabla o badge.
3. Confirmá el endpoint/DTO que alimenta la UI.
4. Diseñá el componente Blazor con estados de carga, vacío, error y éxito si aplica.
5. Añadí hooks estables para Playwright cuando el flujo sea verificable por usuario.
6. Devolvé la estructura UI recomendada y cómo encaja en el dashboard incremental.

## Output Contract

Devolver:
- Componente, panel o página recomendada.
- Datos y endpoint que consume.
- Estados visuales requeridos.
- Ubicación sugerida dentro del dashboard o navegación.
- Riesgos de meter lógica de negocio en UI.
- Recomendación de validación visual/E2E si aplica.

## References

- `docs/ai/04-ui-incremental-strategy.md`
- `docs/ai/03-delivery-rules.md`
- `docs/ai/06-skill-catalog.md`
- `Agents.md`
