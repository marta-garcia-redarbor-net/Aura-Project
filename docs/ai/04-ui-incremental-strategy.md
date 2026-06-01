# Estrategia de UI incremental

Aura construye su interfaz **en paralelo al backend**. No existe backend invisible. Cada slice de funcionalidad tiene representación visual demostrable.

**Decisión actual:** el frontend de Aura se construye con **Blazor Server** dentro del ecosistema .NET.

---

## Por qué

- El progreso necesita ser visible para validar dirección y mantener confianza en el avance.
- Un backend sin UI no es demostrable ni validable por stakeholders.
- La UI incremental fuerza contratos de API claros desde etapas tempranas.

---

## Regla de slice vertical

Cada historia de usuario produce un slice completo:

```
Endpoint / Worker
  → Application use case
  → Domain logic
  → UI: al menos una pantalla, panel o componente que muestre el resultado
  → Tests y telemetría
```

No se cierra ninguna historia sin su contraparte visible.

---

## Prioridad de pantallas por fase

| Fase | Pantalla mínima esperada |
|------|--------------------------|
| Semana 1 (Cimientos) | Health check dashboard, configuración de conectores. |
| Semana 2 (Ingestión) | Lista de WorkItems normalizados en tiempo real. |
| Semana 3 (Triáje) | Morning Summary renderizado, estado de foco del usuario. |
| Semana 4 (Reviewer) | Panel de revisión con score, findings y decisión por PR. |

---

## Contratos de UI → API

- La UI consume exclusivamente los endpoints de `Aura.Api`.
- Sin lógica de negocio en el frontend; sólo presentación y navegación.
- Cada endpoint expone DTOs versionados y documentados (Swagger/OpenAPI).

---

## Stack frontend decidido

| Tema | Decisión |
|------|----------|
| Framework UI | **Blazor Server** |
| Objetivo | Acelerar dashboard interno y slices visibles sin abrir una SPA separada antes de tiempo |
| Integración | Misma solución .NET, componentes UI y consumo de `Aura.Api` |
| Tiempo real | SignalR cuando el slice requiera refresco en vivo |
| Límite | Sin lógica de negocio en componentes; sólo presentación, estado de vista y navegación |

---

## Herramientas de UI

- **Frontend**: Blazor Server.
- **Tiempo real**: SignalR para actualizaciones en vivo de ingestión y estado de foco.
- **Componentes**: Atomic Design; componentes de presentación sin lógica de dominio.
