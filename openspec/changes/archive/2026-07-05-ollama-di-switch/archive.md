# Archive Report: ollama-di-switch

**Change**: ollama-di-switch
**Archived**: 2026-07-05
**Phase**: sdd-archive (openspec mode)
**Verdict**: PASS WITH WARNINGS — archived intentional-with-warnings

---

## Summary

Made the embedding provider switchable via dependency injection between Azure OpenAI (production) and local Ollama (dev/offline). The switch is a `switch` expression in `DependencyInjection.cs` on `EmbeddingProviderOptions.Provider`. Default remains `"OpenAI"` for backward compatibility.

## Task Completion

| Metric | Value |
|--------|-------|
| Tasks total | 11 |
| Tasks complete | 11 |
| Tasks incomplete | 0 |

All tasks were verified complete (`[x]`) in the persisted tasks artifact before archive.

## Spec Sync

| Domain | Action | Details |
|--------|--------|---------|
| `semantic-index` | Updated | Replaced "Observable and Resilient Embedding Generation" requirement — added provider selection text, 4 new scenarios (Config-driven, Ollama, Default, Invalid provider). 7 scenarios total. |

The delta spec had only `MODIFIED` (no ADDED, REMOVED, or RENAMED sections). The old "Accurate Dependency Injection and Host Composition" scenario was replaced by 4 more specific provider selection scenarios.

## Archive Contents

| Artifact | Status |
|----------|--------|
| `proposal.md` | ✅ Present |
| `specs/semantic-index/spec.md` | ✅ Present |
| `design.md` | ✅ Present |
| `tasks.md` | ✅ Present (11/11 tasks complete) |
| `verify-report.md` | ✅ Present |
| `archive.md` | ✅ This report |

## Verification Summary

- **Build**: ✅ 6/6 projects compile
- **Tests**: ✅ 984/984 passed (0 failed, 0 skipped)
- **Spec compliance**: ✅ 7/7 scenarios compliant
- **CRITICAL issues**: None

### Warnings (carried forward from verify-report)

| Warning | Severity | Justification |
|---------|----------|---------------|
| Used `OllamaSharp` 5.4.25 instead of `Microsoft.Extensions.AI.Ollama` | ⚠️ | `Microsoft.Extensions.AI.Ollama` does not exist on NuGet |
| `OllamaApiClient` natively implements `IEmbeddingGenerator` — no `.AsIEmbeddingGenerator()` needed | ⚠️ | Design assumed same pattern as OpenAI, but OllamaSharp implements MEAI directly |
| Branch coverage 62.5% in `DependencyInjection.cs` | ⚠️ | Uncovered `_ => throw` switch branch is guarded by 100%-covered validator |

## Source of Truth Updated

The main spec `openspec/specs/semantic-index/spec.md` now reflects the Ollama provider selection behavior.

## Reusable Insights

- `Microsoft.Extensions.AI.Ollama` is deprecated/removed from NuGet — use `OllamaSharp` instead for MEAI-compatible Ollama integration.
- `OllamaApiClient` implements `IEmbeddingGenerator<string, Embedding<float>>` directly — no `.AsIEmbeddingGenerator()` wrapper needed.
- A `switch` expression in a DI factory method is simpler than a Strategy pattern when there are only 2-3 provider variants and they share middleware.
