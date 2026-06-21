# Proposal: Triage Global Policy Foundation

> **Scope is documentation/architecture only.** No implementation code changes to triage or connector behavior land in this change.

## Intent

The triage docs are placeholders and the ingestion/triage boundary is implied, not stated. Connectors already normalize source inputs into canonical `WorkItem`s and (Outlook) compute preliminary scores, but no doc declares the global policy boundary: connectors normalize and pre-score locally, while a shared triage engine owns the final interrupt-vs-queue decision. This change makes that boundary explicit, explainable, and auditable in the architecture docs.

## Scope

### In Scope
- Document that connectors normalize source inputs into canonical `WorkItem`s.
- Document that connectors extract source-specific signals and compute preliminary scores.
- Document a global triage engine as the final interrupt-vs-queue decision authority.
- State that triage rules MUST be explainable, auditable, and user-adjustable.
- State that refinement starts from explicit preferences, feedback, and history — not opaque self-learning.
- Explicitly defer Focus Mode and mark it out of scope for now.
- Add a `StoryBacklog.md` task for future Teams connector content-based preliminary scoring.

### Out of Scope
- Any code change to triage engine, scoring, or connector adapters.
- Focus Mode design or its state machine (deferred).
- OpenSpec spec deltas / new requirements (no behavior changes in this change).

## Capabilities

### New Capabilities
- None. This change documents an architectural boundary; it introduces no spec-level requirements. The global triage engine capability will be formalized in a later implementation change.

### Modified Capabilities
- None. No existing capability's requirements change. `outlook-connector-mapping` and `teams-connector-mapping` are referenced for contrast only, not modified.

## Approach

Approach 1 + 3 from exploration: describe a two-stage model with an explicit boundary (connectors normalize + pre-score; triage decides), and anchor refinement to explicit user preferences, feedback, and history. Use precise wording so triage never appears to own source-specific parsing and "learning" never reads as opaque self-training.

## Affected Areas

| Area | Impact | Description |
|------|--------|-------------|
| `docs/architecture/triage/00-overview.md` | Modified | State global triage model and decision boundary |
| `docs/architecture/triage/02-proactive-interruptions.md` | Modified | Auditable interrupt-vs-defer rules |
| `docs/architecture/triage/03-focus-state-machine.md` | Modified | Mark explicitly deferred / out of scope |
| `docs/architecture/triage/04-priority-scoring.md` | Modified | Explainable, adjustable global scoring + learned preferences |
| `docs/architecture/ingestion/00-overview.md` | Modified | Align canonical `WorkItem` + preliminary scoring wording |
| `docs/architecture/ingestion/01-microsoft-graph-teams.md` | Modified | Contrast future content-based preliminary scoring |
| `docs/architecture/ingestion/02-microsoft-graph-outlook.md` | Modified | Reference existing source-specific scoring as contrast |
| `docs/ai/02-architecture-map.md` | Modified | Optional triage engine boundary contract naming |
| `StoryBacklog.md` | Modified | Add future Teams content-based scoring task |

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| Docs still imply source-owned decisions | Med | State the global boundary plainly; connectors pre-score only |
| "Learning" drifts toward opaque automation | Med | Anchor refinement to explicit preferences/feedback/history |
| Focus Mode scope creep | Low | Keep Focus Mode explicitly deferred |

## Rollback Plan

Revert the documentation and `StoryBacklog.md` edits via git. No runtime, schema, or contract impact — pure docs.

## Dependencies

- Exploration artifact `openspec/changes/triage-global-policy-foundation/exploration.md`.

## Success Criteria

- [ ] Triage docs state the global engine owns the final interrupt-vs-queue decision.
- [ ] Docs state connectors normalize and compute preliminary scores only.
- [ ] Rules are described as explainable, auditable, and user-adjustable.
- [ ] Refinement is anchored to explicit preferences/feedback/history.
- [ ] Focus Mode is explicitly deferred and out of scope.
- [ ] `StoryBacklog.md` has a future Teams content-based scoring task.
