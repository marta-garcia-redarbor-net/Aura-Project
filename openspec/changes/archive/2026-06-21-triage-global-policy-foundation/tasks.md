# Tasks: Triage Global Policy Foundation

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | 220-340 |
| 400-line budget risk | Low |
| Chained PRs recommended | No |
| Suggested split | Single PR |
| Delivery strategy | ask-always |
| Chain strategy | pending |

Decision needed before apply: Yes
Chained PRs recommended: No
Chain strategy: pending
400-line budget risk: Low

### Suggested Work Units

| Unit | Goal | Likely PR | Notes |
|------|------|-----------|-------|
| 1 | Document two-stage triage policy boundary + backlog follow-up | PR 1 | Base: main; includes spec-scenario verification and no-code diff check |

## Phase 1: Foundation / Boundary Framing

- [x] 1.1 Define the canonical boundary statement in `docs/architecture/triage/00-overview.md` and `docs/architecture/ingestion/00-overview.md`: connectors normalize/extract/pre-score; global triage decides interrupt-vs-queue.
- [x] 1.2 Resolve contract naming note in `docs/ai/02-architecture-map.md` (`IInterruptionPolicyEngine` authority, optional `ITriageEngine` alias) and keep wording consistent with Clean Architecture boundaries.
- [x] 1.3 Reserve a future-work slot in `StoryBacklog.md` under triage/ingestion roadmap for Teams content-based preliminary scoring (explicitly marked as future).

## Phase 2: Core Documentation Updates

- [x] 2.1 Replace placeholder in `docs/architecture/triage/00-overview.md` with the two-stage model, decision authority, and governance constraints (explainable/auditable/user-adjustable).
- [x] 2.2 Replace placeholder in `docs/architecture/triage/02-proactive-interruptions.md` with auditable policy rules where connector preliminary scores are input signals, not final decisions.
- [x] 2.3 Replace placeholder in `docs/architecture/triage/04-priority-scoring.md` with global scoring/refinement guidance anchored to explicit preferences, explicit feedback, and decision history.
- [x] 2.4 Replace placeholder body in `docs/architecture/triage/03-focus-state-machine.md` with an explicit Focus Mode deferral notice and rationale (out of scope for this change).
- [x] 2.5 Update `docs/architecture/ingestion/00-overview.md` to clarify Outlook/Teams scoring as source-specific preliminary scoring written to metadata before triage.
- [x] 2.6 Replace placeholder in `docs/architecture/ingestion/01-microsoft-graph-teams.md` with Teams normalization + source-signal extraction scope and backlog reference for future content-based preliminary scoring.
- [x] 2.7 Replace placeholder in `docs/architecture/ingestion/02-microsoft-graph-outlook.md` with Outlook normalization + existing multi-signal preliminary scoring as contrast to global triage final decisioning.
- [x] 2.8 Add the new StoryBacklog item in `StoryBacklog.md` with a clear DoD focused on future Teams content-based preliminary scoring evidence.

## Phase 3: Specification Trace Verification

- [x] 3.1 Verify `openspec/changes/triage-global-policy-foundation/specs/triage-global-policy/spec.md` Requirement “Two-Stage Pipeline Boundary” and “Global Triage Decision Authority” are explicitly covered in triage and ingestion docs.
- [x] 3.2 Verify Requirement “Rule Governance” and “Refinement Anchoring” are satisfied in `docs/architecture/triage/02-proactive-interruptions.md` and `docs/architecture/triage/04-priority-scoring.md`.
- [x] 3.3 Verify Requirement “Focus Mode Deferral” in `docs/architecture/triage/03-focus-state-machine.md` and “Teams Preliminary Scoring Backlog” in `StoryBacklog.md`.
- [x] 3.4 Run a targeted wording scan in `docs/architecture/ingestion/*.md` to confirm no connector text claims final interrupt-vs-queue ownership.

## Phase 4: Cleanup / Delivery Guard

- [x] 4.1 Ensure all edited technical content is in English and remove placeholder language that contradicts the finalized boundary.
- [x] 4.2 Confirm final diff is documentation-only (`docs/**/*.md`, `StoryBacklog.md`, `openspec/changes/triage-global-policy-foundation/tasks.md`) with no source-code file changes.
