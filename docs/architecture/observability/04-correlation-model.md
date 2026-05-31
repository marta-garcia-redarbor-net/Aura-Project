# Observabilidad — Correlación de logs, métricas y traces

> Placeholder. Este documento debe definir el modelo de correlación para unir señales técnicas, de negocio y costo.

## Quick path

1. Definir `correlation_id`, claves de negocio y convenciones.
2. Diseñar cómo logs, métricas y traces comparten contexto.
3. Verificar búsqueda operativa y auditoría end-to-end.

## Debe cubrir

- Claves de correlación por request, workflow, job y usuario.
- Convenciones de logging estructurado.
- Relación entre métricas IA, spans y eventos de dominio.
- Estrategia de retención y privacidad.
- Pruebas de observabilidad y troubleshooting.

## Pendiente

- [ ] Completar modelo de correlación transversal.
