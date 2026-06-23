# Exploration: W2-H5-T2 Morning Summary ranking

## Current State

- Morning Summary contracts already exist in `Aura.Application` (`IMorningSummaryComposer`, `IMorningSummaryScheduler`, `IWorkItemReader`, `MorningSummary`, `RankedWorkItem`, `RankingExplanation`, `RankingFactor`).
- The main morning-summary doc now reflects the final approved ranking rule summary.
- Triage docs already establish the global boundary: connectors may pre-score, but final prioritization must stay explainable, auditable, and user-controlled.
- Connector metadata already captures deadline cues in ingestion, so ranking can reuse existing signals without touching connector logic.
- Deterministic ranking implementation and tests are already delivered; remaining warnings are documentation alignment only.

## Affected Areas

- `docs/architecture/triage/01-morning-summary.md` — updated with the final ranking rule and fallback behavior.
- `docs/architecture/triage/04-priority-scoring.md` — updated to describe the preliminary-score boundary used by Morning Summary.
- `src/Aura.Application/Models/` — ranking contracts now carry deterministic interpretation support, including `PreliminaryScore` and signal-key constants.
- `src/Aura.Application/Ports/` and `src/Aura.Application/UseCases/MorningSummary/` — ranking policy and composer orchestration are implemented in `Aura.Application`.
- `tests/Aura.UnitTests/Triage/` — precedence, fallback, composer, and explanation behavior are covered by W2-H5-T2 tests.
- `tests/Aura.ArchitectureTests/MorningSummaryArchitectureTests.cs` — boundary protection now asserts ranking-policy ownership and AI exclusion.
- `StoryBacklog.md` — W2-H5-T2 DoD is still only “prioritized and tested output.”

## Final Decision to Document

The approved ranking decision model is:

1. Primary decision order: **Deadline > Impact > Risk**.
2. If explicit signals do not fully decide order, use connector **preliminary score**.
3. If items still tie, use nearest due date, then oldest item, then stable Id.
4. If all explicit signals are missing, preliminary score is the fallback decision input.
5. If neither explicit signals nor preliminary score exists, classify `insufficient-signals` and place last.
6. Output is an ordered list plus structured per-item explanation.

### Clarification (important)

Preliminary score is **one** decision input with two contexts:

- first use: post-explicit decision input when explicit signals do not fully decide;
- fallback use: when explicit signals are all missing.

It must not be documented as two separate independent rules.

## Recommendation

Post-implementation documentation alignment keeps proposal + exploration + design + architecture docs on the same final model, while preserving Clean Architecture boundaries: connector scoring remains preliminary; Application owns final Morning Summary ranking policy.

This cleanup does not introduce or alter production behavior.

## Risks

- If docs differ in sequence wording, teams may implement inconsistent behavior.
- If preliminary score is described as two rules, reviewers may misread priority intent.
- If ownership boundaries blur, connector code may incorrectly absorb final ranking policy.

## Archive Readiness

The exploration intent has been fully realized: the ranking rule is implemented, verified, and documentation is reconciled with the delivered behavior.
