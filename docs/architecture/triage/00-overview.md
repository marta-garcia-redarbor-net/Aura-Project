# Triage — Overview

Aura triage follows a two-stage model with a strict ownership boundary:

1. **Connector adapters (Infrastructure)** normalize provider payloads into canonical `WorkItem`s,
   extract source-specific signals, and compute **preliminary** scores.
2. The **global triage engine (Application)** is the single authority that makes the final
   **interrupt-vs-queue-or-defer** decision.

Connectors MUST NOT own final interruption decisions.

Connectors may emit canonical metadata such as sender, snippet, target-user hints, and traceable explicit cues,
but those remain policy inputs only.

## Quick path

1. Normalize external events into canonical `WorkItem` records.
2. Attach source signals and preliminary scores in metadata.
3. Apply global triage policy to emit `INTERRUPT`, `QUEUE`, or `DEFER`.

## Decision authority and governance

- Final decision authority: `IInterruptionPolicyEngine` (global triage engine).
- Rule governance requirements:
  - **Explainable**: every decision is human-readable.
  - **Auditable**: decision inputs and rationale can be inspected later.
  - **User-adjustable**: users can tune policy inputs and overrides.
  - **Per-user bounded**: narrow explicit overrides can auto-apply for the same user, while broader or riskier generalizations remain review-first.

## Target-user resolution

The global engine resolves the target user in this order:

1. `assignedTo`
2. explicit connector owner/responsible metadata
3. unresolved target user means the engine must not interrupt and falls back to `QUEUE` or `DEFER`

## Refinement model

Rule refinement is anchored only to explicit, inspectable inputs:

- Explicit user preferences
- Explicit user feedback
- Historical decision outcomes

Aura does not use opaque or silent self-learning to change triage behavior.

## Audit trail

Every interruption verdict is persisted through the notification pipeline for explainability:

1. **Outbox**: `NotificationOutboxEntry` stores `Explanation`, `Decision`, `TargetUserId`, and `RuleResults` (JSON) alongside the notification payload.
2. **Worker**: `WorkItemNotificationWorker` reconstructs the full `InterruptionVerdict` from persisted fields — or falls back to a synthetic default for pre-migration entries.
3. **Dispatcher**: `SignalRWorkItemNotificationDispatcher` forwards all audit fields (Explanation, Decision, TargetUserId, RuleResults) in the SignalR payload, enabling downstream consumers (e.g. the Blazor UI) to render explainable interruption cards.

This chain ensures every decision is traceable from policy evaluation through to the user's screen, satisfying the **auditable** governance requirement.

## Advisory guardrails and fallback semantics

Aura keeps deterministic triage as the source of truth and treats LLM input as advisory only.

- Deterministic verdict is produced first by `IInterruptionPolicyEngine`.
- Decision-time semantic context is retrieved via `IDecisionContextRetriever`.
- Advisory output is evaluated under guardrails and recorded as one of:
  - `confirmed` — advisory agrees with deterministic verdict.
  - `adjusted` — advisory suggests a different verdict and adjustment is allowed.
  - `blocked` — advisory tries to change a critical deterministic interrupt; change is rejected.
  - `llm-unavailable` — timeout/error/invalid advisory payload; deterministic verdict is retained.

Rollback/fallback behavior is explicit:

- Qdrant/semantic retrieval failure => empty context list, deterministic flow continues.
- LLM timeout/error/invalid JSON => `llm-unavailable`, deterministic flow continues.
- No target user => no interruption is allowed; decision remains `QUEUE`/`DEFER` by deterministic rules.

## Manual demo runbook — REAL LLM + Qdrant participation

This runbook enables a local/manual demo where decision-time retrieval uses Qdrant and advisory calls use a real chat model.

### Required configuration

For local development (`src/Aura.Api/appsettings.Development.json`):

- `DemoMode:Enabled = true`
- `Qdrant:Host = localhost`, `Qdrant:GrpcPort = 6334`
- `EmbeddingProvider:Provider = Ollama`
- `EmbeddingProvider:Endpoint = http://localhost:11434`
- `EmbeddingProvider:DeploymentName = nomic-embed-text` (embedding model)
- `LlmAdvisor:Enabled = true`
- `LlmAdvisor:Provider = Ollama`
- `LlmAdvisor:Endpoint = http://localhost:11434`
- `LlmAdvisor:ModelId = llama3.1:8b-instruct` (chat model, explicit)

For production-oriented surfaces (`appsettings.json` / environment variables):

- Keep `LlmAdvisor:Enabled = false` by default.
- Override with environment variables when enabling in deployment:
  - `LlmAdvisor__Enabled=true`
  - `LlmAdvisor__Provider=Ollama`
  - `LlmAdvisor__Endpoint=<reachable-chat-endpoint>`
  - `LlmAdvisor__ModelId=<chat-model-id>`

### Required Ollama models

- Embedding model: `nomic-embed-text`
- Chat/advisor model: `llama3.1:8b-instruct`

Example commands:

```bash
ollama pull nomic-embed-text
ollama pull llama3.1:8b-instruct
```

### Startup order

1. Start Qdrant (`docker compose up qdrant` or equivalent) and confirm it is healthy.
2. Start Ollama and ensure both models above are available.
3. Start API (`dotnet run --project src/Aura.Api`).
4. Trigger demo/seed flow so decisions are generated (existing demo endpoints/UI flow).
5. Open decision log (`/triage/decisions`) and expand trace rows.

### Where to inspect evidence

1. **UI trace panel** in `/triage/decisions`:
   - `GuardrailOutcome`
   - `LLM Rationale`
   - `Semantic Context` list
2. **API contract** `GET /api/triage/decisions`:
   - `retrievedSemanticContext`
   - `llmRationale`
   - `guardrailOutcome`
3. **API logs** from `InterruptionPolicyEngine` advisory telemetry (`guardrail`, retrieval latency, advisor latency, fallback reason).

### Degradation behavior to expect

- Qdrant unavailable/timeouts => `retrievedSemanticContext: []`, decision continues.
- LLM unavailable/invalid response/timeout => `guardrailOutcome: llm-unavailable`, deterministic verdict preserved.
- Missing `LlmAdvisor:ModelId` => advisor chat client is unavailable by design; deterministic path remains active.

## Decision Log trace panel behavior

`/triage/decisions` renders a progressive-disclosure trace for each decision.

Summary row shows the compact operational view (timestamp, title, source, score, decision, focus state, explanation, guardrail).

Expandable detail panel is ordered intentionally for cognitive load control:

1. **Summary** (final verdict + guardrail outcome)
2. **Rules Fired** (deterministic explanation)
3. **LLM Rationale** (advisory narrative or explicit unavailable message)
4. **Semantic Context** (retrieved context items sorted by relevance)

The order above is part of the UX contract and covered by tests.

## Scope note

- **In scope now**: global policy boundary, governance, refinement anchors, and audit trail.
- **Out of scope now**: Focus Mode design/state machine (explicitly deferred).
