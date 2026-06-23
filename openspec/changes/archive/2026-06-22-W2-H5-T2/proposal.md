# Proposal: W2-H5-T2 Morning Summary Ranking Rule (Post-Implementation Documentation Alignment)

## Intent

Align OpenSpec documentation with the already delivered Morning Summary ranking implementation
and remove stale wording that still reads as planning-only.

Implementation for W2-H5-T2 in `src/` and `tests/` has already been delivered in prior apply work;
this proposal covers only documentation reconciliation in this cleanup batch.

## Scope

### In Scope
- Update proposal/exploration wording so artifacts explicitly reflect that implementation is already delivered.
- Document deterministic policy with primary decision order: **Deadline > Impact > Risk**.
- Document tie resolution path: connector preliminary score, then nearest due date, then oldest item, then stable Id.
- Document fallback behavior: when explicit signals are all missing, preliminary score is the fallback decision input.
- Document insufficient-signals rule: if neither explicit signals nor preliminary score exist, classify as `insufficient-signals` and place last.
- Document output contract: ordered list plus structured per-item explanation aligned with rank order.
- Add a concise design note for `MorningSummaryComposer` constructor/fallback behavior.

### Out of Scope
- Any new production code changes in `src/` or `tests/` in this cleanup batch.
- Connector pre-scoring algorithm changes.
- Scheduler, caching, persistence, API, or UI behavior changes.
- AI-assisted prioritization implementation (remains design-only and out of scope).

## Capabilities

### New Capabilities
- None. Functional behavior is already implemented.

### Modified Capabilities
- `morning-summary-ranking` documentation is aligned to the delivered implementation, including composer constructor/fallback nuance.

## Approach

Apply a documentation reconciliation update across OpenSpec and architecture references so all artifacts describe the
same ranking policy: explicit precedence first, preliminary score as a single post-explicit/fallback
decision input, deterministic tie-break chain, and `insufficient-signals` handling.

Preserve the architecture boundary: connector score is preliminary metadata; final Morning Summary
ranking policy belongs to the Application layer.

## Affected Areas

| Area | Impact | Description |
|------|--------|-------------|
| `openspec/changes/W2-H5-T2/proposal.md` | Modified | Mark change as post-implementation docs reconciliation and align scope |
| `openspec/changes/W2-H5-T2/exploration.md` | Modified | Remove planning-only/doc-only implication and record final implemented model |
| `openspec/changes/W2-H5-T2/design.md` | Modified | Add concise `MorningSummaryComposer` constructor/fallback nuance |
| `docs/architecture/triage/01-morning-summary.md` | Modified | Replace placeholder with final ranking guidance |
| `docs/architecture/triage/04-priority-scoring.md` | Modified | Clarify scoring boundary and Morning Summary rule relationship |

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| Rule interpretation drifts across docs | Med | Keep one canonical sequence and fallback statement across all updated files |
| Preliminary score interpreted as two separate rules | Med | State it as one decision input used post-explicit and as fallback when explicit signals are absent |
| Premature AI coupling | Low | Keep AI-assisted prioritization explicitly design-only and out of scope |

## Rollback Plan

Revert the documentation edits in the updated markdown files.

## Dependencies

- Existing Morning Summary contract semantics and triage architecture boundaries.

## Success Criteria

- [ ] Proposal and exploration both state the same final decision order and tie-break/fallback behavior.
- [ ] Morning Summary architecture doc no longer contains placeholder/TODO wording.
- [ ] Priority scoring doc clearly preserves connector-vs-Application ownership and clarifies preliminary score usage.
- [ ] AI-assisted prioritization remains design-only and out of scope.
