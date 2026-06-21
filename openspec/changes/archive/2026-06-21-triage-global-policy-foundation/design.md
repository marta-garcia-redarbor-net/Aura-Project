# Design: Triage Global Policy Foundation

> Documentation/architecture-only change. No code, no contracts, no runtime behavior modified.

## Technical Approach

Rewrite seven placeholder docs and add one `StoryBacklog.md` entry to make the ingestion/triage boundary explicit, auditable, and stable enough to anchor future implementation changes. The two-stage model is the core architectural claim that every doc must reinforce:

```
Connector (Infrastructure)            Global Triage Engine (Application)
──────────────────────────            ─────────────────────────────────
  normalize payload                     receive canonical WorkItem
  extract source-specific signals  →    apply interrupt-vs-queue policy
  compute preliminary score             use preferences/feedback/history
  write to WorkItem.Metadata            emit interrupt or defer decision
```

Connectors are source adapters — they must not own the final interrupt decision. The triage engine is the single authority for that decision.

## Architecture Decisions

| Decision | Choice | Alternatives rejected | Rationale |
|----------|--------|-----------------------|-----------|
| Boundary declaration style | Explicit "connectors pre-score only; triage engine decides" sentence in every doc that touches scoring | Implicit by omission | Docs that omit the boundary cause developers to add logic to connectors; explicit wording is the cheapest enforcement |
| Learning/refinement anchor | Explicit preferences, feedback, history — named inputs only | "learns over time" / opaque calibration | Opaque learning is untestable and undermines user trust; the `aura-triage-rules` skill hard-rules require explicit anchors |
| Focus Mode | Explicitly deferred, reason stated in `03-focus-state-machine.md` | Draft partial design now | Partial state machine docs create false confidence; deferred with a reason is clearer than an empty placeholder |
| Teams preliminary scoring | `StoryBacklog.md` future task, not designed now | Draft a scoring scheme | No content-signal data yet; backlog entry is the minimum honest representation |
| Architecture map update | Optional: add `ITriageEngine` naming note to `02-architecture-map.md` | Full contract redesign | Contract naming is in-flight; a naming note is enough to anchor the boundary without locking implementation |

## Data Flow

The doc edits must consistently reinforce this signal path:

```
Source event (Teams / Outlook / Calendar / GitHub)
  │
  ▼
Connector Adapter (Infrastructure)
  ├── Normalize → canonical WorkItem
  ├── Extract source signals (importance, sender, subject, content-type…)
  ├── Compute preliminary score → WorkItem.Metadata["<source>.scoring.*"]
  └── Emit WorkItem to Application layer
        │
        ▼
      Global Triage Engine (Application / IInterruptionPolicyEngine)
        ├── Receive preliminary score + signals
        ├── Apply global rules (severity, attentional budget, user preferences)
        ├── Consult feedback history and explicit preference overrides
        └── Emit: INTERRUPT | QUEUE | DEFER
```

Focus Mode (`IFocusStateResolver`) is a future consumer of the triage engine output — **not designed in this change**.

## File Changes

| File | Action | What changes |
|------|--------|--------------|
| `docs/architecture/triage/00-overview.md` | Modify | Replace placeholder with two-stage model overview; state global engine owns interrupt-vs-queue; list explainability/auditability/adjustability requirements |
| `docs/architecture/triage/02-proactive-interruptions.md` | Modify | Replace placeholder with auditable interrupt-vs-defer policy contract; state rules must be explainable; note preliminary score is an input, not the decision |
| `docs/architecture/triage/03-focus-state-machine.md` | Modify | Replace placeholder body with explicit deferral notice and reason; keep section heading intact for future lookup |
| `docs/architecture/triage/04-priority-scoring.md` | Modify | Replace placeholder with global scoring model description; define refinement inputs as preferences/feedback/history; contrast with connector-local preliminary scores |
| `docs/architecture/ingestion/00-overview.md` | Modify | Add boundary note after Outlook scoring block: preliminary scores feed the triage engine; connectors do not own the interrupt decision |
| `docs/architecture/ingestion/01-microsoft-graph-teams.md` | Modify | Replace placeholder with Teams connector normalization description; note content-based preliminary scoring is a future capability (StoryBacklog); no scoring section yet |
| `docs/architecture/ingestion/02-microsoft-graph-outlook.md` | Modify | Replace placeholder with Outlook connector description; reference the existing multi-signal scoring as a preliminary score example; contrast with triage engine final decision |
| `docs/ai/02-architecture-map.md` | Modify | Optional: add `ITriageEngine` / `IInterruptionPolicyEngine` naming note under Triage contracts to surface the boundary |
| `StoryBacklog.md` | Modify | Add future task: "Teams connector content-based preliminary scoring" under appropriate backlog epic |

## Interfaces / Contracts

No new contracts. Existing contracts from `02-architecture-map.md` are referenced in documentation only:

- `IInterruptionPolicyEngine` — declared owner of interrupt-vs-queue decisions in triage docs
- `IPriorityScoringService` — referenced in `04-priority-scoring.md` as the global scoring contract
- `IFocusStateResolver` — referenced in `03-focus-state-machine.md` as deferred

## Testing Strategy

| Layer | What to Verify | Approach |
|-------|---------------|----------|
| Doc review | Boundary sentence present in each modified doc | Manual checklist against proposal success criteria |
| Doc review | No doc implies connector owns interrupt decision | Grep for "interrupt" / "defer" in `docs/architecture/ingestion/` |
| Doc review | Focus Mode deferred with explicit reason | Read `03-focus-state-machine.md` after edit |
| Backlog | StoryBacklog task exists with clear DoD | Read `StoryBacklog.md` after edit |

## Migration / Rollout

No migration required. Pure documentation edits; no runtime, schema, or contract impact. Rollback = `git revert`.

## Open Questions

- [ ] Should `ITriageEngine` be introduced as a named contract in `02-architecture-map.md`, or is `IInterruptionPolicyEngine` sufficient to name the decision authority? (Low risk — resolve at tasks time.)
