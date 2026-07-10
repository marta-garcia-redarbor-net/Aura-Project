# Design: Demo-Ready Decisioning, Traceability, and Curated Scenarios

## Technical Approach

Insert two new ports — `IDecisionContextRetriever` and `ILlmDecisionAdvisor` — into the existing
`InterruptionPolicyEngine` pipeline **after** the deterministic verdict. Extend
`InterruptionDecisionRecord` with three trace fields persisted as additive nullable columns in the
`InterruptionDecisions` EF table. Surface the full trace through the existing API seam and a
row-level progressive-disclosure panel in `DecisionLog.razor`. Null implementations guard both
ports behind a feature flag; the deterministic verdict remains authoritative in all degradation
paths. Curated seed items are signal-refined to guarantee distinct INTERRUPT/QUEUE/DEFER paths per
source type.

## Architecture Decisions

| Decision | Options | Tradeoff | Chosen |
|----------|---------|----------|--------|
| LLM port definition | Application / Infrastructure | Infrastructure: leaks SDK detail upward; Application: clean capability contract | `ILlmDecisionAdvisor` in `Aura.Application/Ports` |
| Decision-context retriever | Reuse `ISemanticContextRetriever` / dedicated port | Reuse: couples decisioning query scope to general retrieval contract; Dedicated: bounded context | New `IDecisionContextRetriever` in `Aura.Application/Ports` |
| Trace persistence | New table / additive columns | New table: migration complexity; Additive: backward-safe, matches existing string-column pattern | 3 nullable TEXT columns on `InterruptionDecision` entity |
| Guardrail location | LLM adapter / engine | Adapter: no override policy access; Engine: owns policy and can apply critical-override rule | Engine applies guardrail post-advisor-response |
| Degradation strategy | Exception propagation / Null objects | Propagation: blocks pipeline; Null: clean, testable, zero latency cost | Null implementations registered by feature flag |
| UI progressive disclosure | Modal / in-row expansion | Modal: loses row comparison context; In-row: jury sees summary and detail side-by-side | Native `<details>` expansion below each table row |

## Data Flow

```
WorkItem (seeded or ingested)
  └─→ InterruptionPolicyEngine.EvaluateAsync()
        1. ResolveTargetUserId, FocusState, Policy
        2. Override check  ─→ short-circuit (no advisor step)
        3. BuildNormalizedSignals + PriorityScore
        4. Rule evaluation loop  ─→  deterministicVerdict
        5. [NEW] IDecisionContextRetriever.RetrieveAsync(item)
                   └─→ QdrantDecisionContextAdapter
                         delegates to ISemanticContextRetriever (SemanticQuery from item title)
                         timeout: 5 s; catches any exception → returns []
        6. [NEW] ILlmDecisionAdvisor.EvaluateAsync(AdvisoryRequest)
                   └─→ MeaiLlmDecisionAdvisorAdapter
                         builds structured prompt; calls IChatClient; parses JSON response
                         timeout: 10 s; catches any exception → AdvisoryResponse(llm-unavailable)
        7. Guardrail evaluation (engine) → finalVerdict + guardrailOutcome
        8. IInterruptionDecisionStore.RecordAsync(InterruptionDecisionRecord — full trace)
  └─→ GET /api/triage/decisions  ─→  DecisionLogItemResponse (extended)
  └─→ DecisionLog.razor
        Table row (summary + GuardrailOutcome badge)
          └─→ [expand] Trace panel (rules fired · semantic context · LLM rationale · outcome)
```

**Guardrail logic (in engine):**

| Condition | GuardrailOutcome | FinalVerdict |
|-----------|-----------------|--------------|
| `SuggestedVerdict` null or equals deterministic | `"confirmed"` | deterministic |
| `SuggestedVerdict` differs AND item is critical-override | `"blocked"` | deterministic |
| `SuggestedVerdict` differs AND NOT critical-override | `"adjusted"` | SuggestedVerdict |
| Advisor unavailable / timeout / parse failure | `"llm-unavailable"` | deterministic |

## File Changes

| File | Action | Description |
|------|--------|-------------|
| `src/Aura.Application/Ports/IDecisionContextRetriever.cs` | Create | Port: `WorkItem → IReadOnlyList<DecisionContextItem>` with per-call timeout |
| `src/Aura.Application/Ports/ILlmDecisionAdvisor.cs` | Create | Port: `AdvisoryRequest → AdvisoryResponse` |
| `src/Aura.Application/Models/DecisionContextItem.cs` | Create | `(CanonicalSourceId, ContentSnippet, SourceType, RelevanceScore)` — no Qdrant types |
| `src/Aura.Application/Models/AdvisoryRequest.cs` | Create | `(WorkItem, DeterministicVerdict, NormalizedSignals, RetrievedContext)` |
| `src/Aura.Application/Models/AdvisoryResponse.cs` | Create | `(SuggestedVerdict?, Rationale, GuardrailOutcome, FailureReason?)` |
| `src/Aura.Application/Models/InterruptionDecisionRecord.cs` | Modify | Add `RetrievedSemanticContext?`, `LlmRationale?`, `GuardrailOutcome` (default `"confirmed"`) |
| `src/Aura.Infrastructure/Adapters/LlmAdvisor/MeaiLlmDecisionAdvisorAdapter.cs` | Create | `IChatClient` implementation; structured JSON prompt; 10 s timeout |
| `src/Aura.Infrastructure/Adapters/LlmAdvisor/NullLlmDecisionAdvisor.cs` | Create | Returns `"confirmed"` immediately; used when feature flag is off |
| `src/Aura.Infrastructure/Adapters/LlmAdvisor/LlmAdvisorOptions.cs` | Create | `Enabled`, `TimeoutSeconds` (10), `ConfidenceThreshold` (0.7), `ModelId` |
| `src/Aura.Infrastructure/Adapters/LlmAdvisor/DependencyInjection.cs` | Create | Registers real or Null advisor based on `LlmAdvisor:Enabled` |
| `src/Aura.Infrastructure/Adapters/SemanticIndex/QdrantDecisionContextAdapter.cs` | Create | Wraps `ISemanticContextRetriever`; builds `SemanticQuery` from `WorkItem.Title`; maps to `DecisionContextItem`; swallows exceptions |
| `src/Aura.Infrastructure/Adapters/SemanticIndex/NullDecisionContextRetriever.cs` | Create | Returns `[]`; registered when Qdrant or flag is off |
| `src/Aura.Infrastructure/Adapters/Persistence/AuraDbContext.cs` | Modify | Add 3 nullable `string?` columns to `InterruptionDecision` entity |
| `src/Aura.Infrastructure/Adapters/Decisions/EfInterruptionDecisionStore.cs` | Modify | Map new columns on write (JSON serialize list); deserialize on read |
| `src/Aura.Infrastructure/Adapters/Services/InterruptionPolicyEngine.cs` | Modify | Inject `IDecisionContextRetriever` + `ILlmDecisionAdvisor`; add steps 5–7; pass full trace to `RecordAsync` |
| `src/Aura.Infrastructure/Adapters/SeedData/SeedDataHostedService.cs` | Modify | Refine signal metadata per source to produce guaranteed distinct verdict paths |
| `src/Aura.UI/Models/DecisionLogResponse.cs` | Modify | Add `IReadOnlyList<DecisionContextItemResponse>?`, `LlmRationale?`, `GuardrailOutcome?` |
| `src/Aura.UI/Pages/DecisionLog.razor` | Modify | Add `GuardrailOutcome` column; convert row click from navigation to `<details>` expansion; render trace panel |
| `src/Aura.Api/Endpoints/TriageEndpoints.cs` | Modify | Map new fields from `InterruptionDecisionRecord` into `DecisionLogItemResponse` |
| EF migration (auto-generated) | Create | Additive: 3 nullable TEXT columns on `InterruptionDecisions`; no backfill |
| `tests/Aura.UnitTests/Adapters/Services/InterruptionPolicyEngineTraceTests.cs` | Create | Guardrail scenarios; full trace shape in persisted record |
| `tests/Aura.UnitTests/Adapters/LlmAdvisor/GuardrailTests.cs` | Create | confirmed / adjusted / blocked / llm-unavailable paths |
| `tests/Aura.UnitTests/Adapters/SemanticIndex/QdrantDecisionContextAdapterTests.cs` | Create | Timeout → `[]`; exception → `[]`; success → mapped items |
| `tests/Aura.IntegrationTests/Adapters/Decisions/EfDecisionStoreTraceTests.cs` | Create | Write + read full record with new columns via SQLite in-memory |
| `tests/Aura.ArchitectureTests/` | Modify | Assert `IDecisionContextRetriever` and `ILlmDecisionAdvisor` defined in Application; no Qdrant SDK types in Domain/Application |

## Interfaces / Contracts

```csharp
// Aura.Application/Ports — only non-obvious signatures shown

interface IDecisionContextRetriever
{
    Task<IReadOnlyList<DecisionContextItem>> RetrieveAsync(WorkItem item, CancellationToken ct);
}

interface ILlmDecisionAdvisor
{
    Task<AdvisoryResponse> EvaluateAsync(AdvisoryRequest request, CancellationToken ct);
}

// Models
record DecisionContextItem(string CanonicalSourceId, string ContentSnippet, string SourceType, double RelevanceScore);
record AdvisoryRequest(WorkItem Item, string DeterministicVerdict,
    IReadOnlyDictionary<string, NormalizedSignal> Signals,
    IReadOnlyList<DecisionContextItem> Context);
record AdvisoryResponse(string? SuggestedVerdict, string Rationale,
    string GuardrailOutcome, string? FailureReason = null);

// Extended record (additive, default-safe)
record InterruptionDecisionRecord(
    Guid WorkItemId, string Title, string SourceType,
    string Decision, int? PriorityScore, string Explanation,
    DateTimeOffset Timestamp, string FocusState,
    IReadOnlyList<DecisionContextItem>? RetrievedSemanticContext = null,
    string? LlmRationale = null,
    string GuardrailOutcome = "confirmed");
```

**LLM prompt contract** (structured JSON response expected):

```json
{ "suggestedVerdict": "INTERRUPT|QUEUE|DEFER|null", "rationale": "…", "confidence": 0.87 }
```

Parse failure → treat as `llm-unavailable`.

## UI Design — Progressive Disclosure

```
TABLE ROW  ──  Timestamp · Title · Source · Score · Decision · FocusState · GuardrailOutcome
               [click / keyboard Enter]
               ↓
  ┌─ TRACE PANEL (inline <details>) ───────────────────────────────────────────┐
  │  Summary        QUEUE → INTERRUPT (guardrail: adjusted, confidence 0.87)   │
  │  Rules Fired    VipSenderRule ✓  ScoreThresholdRule ✗  KeywordMatchRule ✗  │
  │  LLM Rationale  "Sender is a VIP contact. Retrieved context confirms…"     │
  │  Semantic Ctx   ▶ 3 items  [expand to see ranked snippets + scores]        │
  │  Guardrail      🟡 adjusted — deterministic: QUEUE → final: INTERRUPT      │
  └────────────────────────────────────────────────────────────────────────────┘
```

"Semantic Ctx" is a nested `<details>` (level 3) — collapsed by default to prevent overwhelming
the jury. The panel is keyboard-navigable and uses existing `dashboard-panel` CSS classes.

## Curated Seed — Signal Design

Each source type must produce one INTERRUPT, one QUEUE, one DEFER. Required signal metadata to
set deterministically:

| Source | Item | Signals to set | Expected verdict |
|--------|------|----------------|-----------------|
| Teams | `teams-seed-001` (prod incident) | `canonicalSender=VIP`, `actionNeeded=true`, `timeCriticality=High` | INTERRUPT |
| Teams | `teams-seed-003` (API question) | no VIP, no deadline | QUEUE |
| Teams | `teams-seed-005` (arch proposal) | priority=Low, `timeCriticality=Low`, focus=Away-like | DEFER |
| Outlook | `outlook-seed-001` (prod down) | `canonicalSender=VIP`, `actionNeeded=true` | INTERRUPT |
| Outlook | `outlook-seed-003` (weekly status) | medium importance, deadline=false | QUEUE |
| Outlook | `outlook-seed-006` (hackathon invite) | priority=Low, no urgency signals | DEFER |
| PR | `pr-seed-001` (hotfix critical) | `assignedTo=currentUser`, `pr.reviewerCount≥2` | INTERRUPT |
| PR | `pr-seed-003` (reporting feature) | priority=High, not VIP, no deadline | QUEUE |
| PR | `pr-seed-005` (dep update draft) | `pr.isDraft=true`, priority=Low | DEFER |

`SeedDataHostedService` also calls `IInterruptionPolicyEngine.EvaluateAsync` on each item after
seeding so traces are generated and visible in the UI on first load.

## Testing Strategy

| Layer | What | Approach |
|-------|------|----------|
| Unit | Guardrail: confirmed / adjusted / blocked / llm-unavailable | Mock `ILlmDecisionAdvisor`; assert `GuardrailOutcome` in persisted record |
| Unit | `QdrantDecisionContextAdapter`: timeout → `[]`, exception → `[]` | Mock `ISemanticContextRetriever`; inject delay via `CancellationToken` |
| Unit | `InterruptionPolicyEngine`: full trace shape in `RecordAsync` call | Mock all ports; verify `InterruptionDecisionRecord` fields |
| Integration | EF store write/read new columns | SQLite in-memory `AuraDbContext`; round-trip serialization |
| Architecture | Port definitions in Application; no Qdrant SDK in Application/Domain | `NetArchTest` rules |
| UI | Trace panel renders; `<details>` toggle; GuardrailOutcome badge visible | bUnit component test or Playwright smoke against seeded data |

## Migration / Rollout

**EF migration**: adds `RetrievedSemanticContext TEXT NULL`, `LlmRationale TEXT NULL`,
`GuardrailOutcome TEXT NULL` to `InterruptionDecisions`. Existing rows default to NULL; read path
treats NULL as empty / `"confirmed"`. No data backfill. Rollback: drop the three columns (no
orphan data).

**Feature flags**: `LlmAdvisor:Enabled = false` (default) and `SeedData:Enabled = true` (existing)
in `appsettings`. When `LlmAdvisor:Enabled = false`, DI registers Null implementations — the
pipeline runs identically to the pre-change state with `GuardrailOutcome = "confirmed"` on all
records.

## Implementation Slices (Work Units)

| # | Slice | Scope | Budget estimate |
|---|-------|-------|-----------------|
| 1 | Foundation | New ports + models + `QdrantDecisionContextAdapter` + `NullDecisionContextRetriever` + EF extension + migration + unit tests | ~150 lines |
| 2 | LLM Advisor | New advisor port + models + `MeaiLlmDecisionAdvisorAdapter` + `NullLlmDecisionAdvisor` + guardrail in engine + unit tests | ~180 lines |
| 3 | Trace Surface | API response extension + `DecisionLog.razor` trace panel + UI tests | ~130 lines |
| 4 | Curated Scenarios | Refined seed metadata + engine evaluation at seed time + integration test | ~100 lines |

Total forecast: ~560 lines — **Low** 400-line budget risk. Single PR acceptable.

## Open Questions

- [ ] `IChatClient` registration: does the existing embedding DI chain already expose a registered
  `IChatClient`, or does `LlmAdvisorDependencyInjection` need to register one (OpenAI / Ollama)
  matching `EmbeddingProviderOptions` provider selection?
- [ ] Should `LlmAdvisor:Enabled` be a sub-key of `DemoMode` or a standalone section in
  `appsettings`? (Standalone is cleaner if the advisor is intended beyond demo mode.)
