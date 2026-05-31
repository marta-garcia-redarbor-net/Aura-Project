# Atomización de tareas

Las tareas en Aura se definen de forma **atómica y guiable**: un objetivo técnico, evidencia verificable, alcance acotado.

---

## Qué hace atómica a una tarea

| Criterio | Descripción |
|----------|-------------|
| Un objetivo | La tarea hace exactamente una cosa. No "implementar ingestión"; sí "crear `GraphTeamsConnector` que mapea mensajes a `WorkItem`". |
| Evidencia verificable | Al terminar, hay algo que se puede ver: test verde, endpoint respondiendo, pantalla mostrando datos. |
| Alcance cerrado | No abre frentes nuevos. Si aparece algo fuera de scope, se crea una tarea nueva. |
| UI incluida | Si la tarea produce datos nuevos, incluye la pantalla mínima que los muestra. |
| Tests incluidos | Tests y telemetría van en el mismo commit. |

---

## Cómo partir una tarea que es demasiado grande

1. Identificar el contrato (interfaz o endpoint) que se va a implementar.
2. Partir por: primero el contrato + tests, luego la implementación, luego la UI.
3. Si la UI es compleja, puede ir en una tarea separada pero del mismo sprint.

---

## Ejemplo de partición correcta

**Mal:** "Implementar sistema de triáje."

**Bien:**
```
T-01: Definir IFocusStateResolver + unit tests de transiciones (DeepWork → Window → Away)
T-02: Implementar FocusStateMachineService con reglas de dominio
T-03: Exponer GET /api/focus-state en Aura.Api con DTO y Swagger
T-04: Renderizar estado de foco en dashboard (componente FocusBadge)
T-05: Instrumentar ActivitySource en FocusStateMachineService
```

Cada tarea tiene evidencia verificable. Se pueden revisar, redirigir y validar de forma independiente.

---

## Señales de que una tarea no es atómica

- El título tiene "y" o "/": "Implementar conector Y normalizar eventos".
- No se puede describir qué se ve al terminar.
- El cambio toca más de dos capas sin contrato previo.
- Los tests son "por agregar después".
