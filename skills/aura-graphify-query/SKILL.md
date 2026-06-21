---
name: aura-graphify-query
description: "Trigger: explorar codebase, estructura de X, dependencias de Y, donde esta Z, que usa X, llamadas a Y, que implementa, que clases, que hereda, flujo de. Query the Graphify knowledge graph instead of reading raw files."
license: Apache-2.0
metadata:
  author: gentleman-programming
  version: "1.0"
---

# aura-graphify-query

## Activation Contract

Usa esta skill cuando el agente necesite orientarse en el codebase: entender estructura,
encontrar dependencias, localizar implementaciones o trazar flujos entre capas.

El grafo en `graphify-out/graph.json` ya contiene toda la información estructural del proyecto.
Consultarlo cuesta ~300 tokens. Leer los archivos equivalentes cuesta ~8.000 tokens.

## Hard Rules

- SIEMPRE consultá el grafo antes de leer archivos cuando la pregunta es estructural.
- Usá `graphify query` para preguntas conceptuales o de búsqueda amplia.
- Usá `graphify path` para relaciones entre dos entidades concretas.
- Usá `graphify explain` para entender un concepto o clase específica en profundidad.
- Solo leé el archivo fuente cuando necesitás la implementación real (lógica, código exacto).
- No leás `GRAPH_REPORT.md` salvo para revisión arquitectónica amplia — es más costoso que una query.
- Si `graphify-out/graph.json` no existe, caé a la lectura normal de archivos sin error.

## Decision Gates

| Pregunta | Acción |
|----------|--------|
| "¿Dónde está la clase X?" | `graphify query "X"` |
| "¿Qué depende de Y?" | `graphify query "dependencies Y"` |
| "¿Qué implementa la interfaz Z?" | `graphify query "implements Z"` |
| "¿Cómo fluye desde A hasta B?" | `graphify path "A" "B"` |
| "¿Qué hace el concepto X?" | `graphify explain "X"` |
| "Necesito ver la implementación exacta de un método" | Leer el archivo fuente directamente |
| "Necesito modificar código" | Leer el archivo fuente directamente |

## Execution Steps

1. Verificá que `graphify-out/graph.json` existe antes de ejecutar cualquier comando.
2. Elegí el comando adecuado según el tipo de pregunta (query / path / explain).
3. Formulá la query en inglés — Graphify indexa identificadores en el lenguaje del código.
4. Si el resultado no es suficiente, refiná la query antes de recurrir a leer archivos.
5. Usá el resultado del grafo como contexto para responder; no lo expandás innecesariamente.

## Commands Reference

```powershell
# Búsqueda general por concepto o clase
graphify query "WorkItem ingestion"

# Relación entre dos entidades
graphify path "IWorkItemRepository" "WorkItemRepository"

# Explicación profunda de un concepto
graphify explain "FocusStateMachine"

# Arquitectura amplia (solo cuando las queries no alcanzan)
# cat graphify-out/GRAPH_REPORT.md
```

## Limits

- Esta skill NO reemplaza la lectura de código cuando se necesita lógica de implementación.
- Esta skill NO aplica para modificaciones de código — solo para orientación y comprensión.
- Si el grafo está desactualizado (`graphify . --update --no-viz`), las queries pueden devolver
  nodos obsoletos. En ese caso, verificá con el archivo fuente.

## References

- `graphify-out/GRAPH_REPORT.md` — resumen de nodos clave y conexiones sorpresivas
- `docs/ai/01-operating-rules.md` — reglas generales de operación del agente
- `docs/ai/02-architecture-map.md` — mapa de capas de Aura
