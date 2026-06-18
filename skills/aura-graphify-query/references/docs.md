# References — aura-graphify-query

## Graphify CLI

- Repo oficial: https://github.com/safishamsi/graphify
- Instalación: `uv tool install graphifyy`
- Integración OpenCode: `graphify opencode install`

## Comandos clave

```powershell
graphify query "<pregunta>"           # subgrafo enfocado (~300 tokens)
graphify path "<NodoA>" "<NodoB>"     # camino entre dos entidades
graphify explain "<concepto>"         # explicación profunda de un nodo
graphify . --update --no-viz          # actualizar grafo tras cambios de código (AST, sin costo)
```

## Artefactos en el repo

- `graphify-out/graph.json` — índice completo (excluido de git, generado localmente)
- `graphify-out/GRAPH_REPORT.md` — resumen de nodos clave (trackeado en git)
