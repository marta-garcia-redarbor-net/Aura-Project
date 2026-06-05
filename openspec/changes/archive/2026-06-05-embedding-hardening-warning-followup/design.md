# Design: Embedding Hardening Warning Follow-up

## Technical Approach

Four targeted interventions to close the verification warnings from the `embedding-provider-hardening` cycle. All work stays inside Infrastructure and test layers — no domain or application changes. Each task is independently verifiable.

## Architecture Decisions

| Decision | Alternatives | Rationale |
|----------|-------------|-----------|
| Remove legacy provider + all references (VectorStore DI factory + DI test assertion) | Keep file and mark `[Obsolete]` | Dead code — `AddMeaiEmbeddingProvider` already overrides the registration in `Program.cs`. Keeping it invites confusion about which provider runs. |
| Timeout test uses `TaskCompletionSource` (never-completing) + 1s Polly timeout | Use `Task.Delay` with real wall-clock | TCS is deterministic and cannot flake in CI. Polly 8 `TimeoutRejectedException` propagates immediately when the inner task stalls. |
| Host composition test in IntegrationTests with manual `ServiceCollection` build | Use `WebApplicationFactory<Program>` | Workers is SDK.Worker, not SDK.Web — no `WebApplicationFactory` available. Building a `ServiceCollection` with the same extension methods (`AddQdrantSemanticIndex` + `AddMeaiEmbeddingProvider`) and resolving key services proves composition without needing external infrastructure. |
| Real OpenAI generator resolution test in existing `EmbeddingDependencyInjectionTests` | New test file | Extends the existing DI test fixture. Calls `BuildServiceProvider().GetRequiredService<IEmbeddingProvider>()` with in-memory config to prove the full MEAI pipeline wires correctly. |

## Data Flow

No runtime data flow changes. All work is test + cleanup:

    Program.cs ──AddQdrantSemanticIndex──→ registers old IEmbeddingProvider (TO BE REMOVED from factory)
        │
        └──────AddMeaiEmbeddingProvider──→ registers new IEmbeddingProvider (wins via last-registration)

After cleanup, `AddQdrantSemanticIndex` no longer registers any `IEmbeddingProvider`. The MEAI extension owns that responsibility exclusively.

## File Changes

| File | Action | Description |
|------|--------|-------------|
| `src/Aura.Infrastructure/VectorStore/AzureOpenAiEmbeddingProvider.cs` | Delete | Legacy V1 provider class — fully superseded by `MeaiEmbeddingProvider`. |
| `tests/Aura.UnitTests/VectorStore/AzureOpenAiEmbeddingProviderTests.cs` | Delete | Tests for removed class. |
| `src/Aura.Infrastructure/VectorStore/DependencyInjection.cs` | Modify | Remove `IEmbeddingProvider` factory registration (lines 47-62) and associated `using`. |
| `tests/Aura.UnitTests/VectorStore/DependencyInjectionTests.cs` | Modify | Remove `AddQdrantSemanticIndex_ResolvesEmbeddingProvider` test; the assertion targets the deleted type. |
| `tests/Aura.IntegrationTests/Embedding/EmbeddingResilienceTests.cs` | Modify | Add `GenerateEmbeddingsAsync_TimeoutExceeded_ThrowsTimeoutRejectedException` using a stalling fake generator with a 1-second timeout. |
| `src/Aura.Workers/Program.cs` | Modify | Append `public partial class Program { }` after top-level statements. |
| `tests/Aura.IntegrationTests/Workers/WorkersHostCompositionTests.cs` | Create | Verify `IEmbeddingProvider`, `ISemanticIndexWriter`, `IHostedService` resolve from `ServiceCollection` wired via the same extension methods as `Program.cs`. |
| `tests/Aura.IntegrationTests/Aura.IntegrationTests.csproj` | Modify | Add `<ProjectReference>` to `Aura.Workers.csproj`. |
| `tests/Aura.UnitTests/Infrastructure/EmbeddingDependencyInjectionTests.cs` | Modify | Add test that calls `BuildServiceProvider().GetRequiredService<IEmbeddingProvider>()` to prove real MEAI pipeline wires (covers OpenAI client + OTel + Polly). |

## Interfaces / Contracts

No new interfaces. No contract changes. `IEmbeddingProvider` is unchanged.

## Testing Strategy

| Layer | What to Test | Approach |
|-------|-------------|----------|
| Unit | Legacy DI reference removed cleanly | Existing VectorStore DI tests minus deleted assertion compile and pass |
| Unit | Real MEAI generator resolves | New test in `EmbeddingDependencyInjectionTests` — build SP, resolve `IEmbeddingProvider`, assert type |
| Integration | Polly timeout fires on stalling generator | `EmbeddingResilienceTests` — fake with `TaskCompletionSource`, 1s timeout, assert `TimeoutRejectedException` |
| Integration | Workers host composition | `WorkersHostCompositionTests` — build `ServiceCollection` with in-memory config, call same DI extensions as `Program.cs`, resolve critical services |

## Migration / Rollout

No migration required. Legacy `AzureOpenAiEmbeddingProvider` is dead code — `AddMeaiEmbeddingProvider` overrides it at runtime already. Removing the old registration is a no-op at runtime.

## Open Questions

- None — all decisions are within scope and non-blocking.
