# Tasks: W3-H2-T4 — Teams Connector Content-Based Preliminary Scoring

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | ~350-420 |
| 400-line budget risk | Medium |
| Chained PRs recommended | No |
| Suggested split | Single PR |
| Delivery strategy | ask-on-risk |
| Chain strategy | size-exception |

Decision needed before apply: No

## Phase 1: Foundation — Signal Keys

- [x] 1.1 Add 6 `Teams*` const keys to `WorkItemSignalKeys.cs`: `TeamsScoringTitleCues`, `TeamsScoringBodyCues`, `TeamsScoringMentionDetected`, `TeamsScoringTotalScore`, `TeamsDeadlineCue`, `TeamsDeadlineSource` (TCM-SCORE-5)

## Phase 2: Core — Mapper Scoring Logic

- [x] 2.1 Add 4 cue arrays to `TeamsWorkItemMapper.cs`: `TitlePriorityCues`, `BodyHighPriorityCues`, `BodyMediumPriorityCues`, `DeadlineCues` (TCM-SCORE-2)
- [x] 2.2 Add `ScoreTitle()` with 0/1/3 escalator: strong urgency or ≥2 matches → 3, single non-strong → 1, none → 0 (TCM-SCORE-1)
- [x] 2.3 Add `ScoreBody()` with high(3)/medium(1)/none(0) tiers (TCM-SCORE-1)
- [x] 2.4 Add `DetectMention()` checking `@` in `BodyPreview`, returning 0/1 (TCM-SCORE-3)
- [x] 2.5 Add `MatchTokens()` and `ExtractCueContext()` verbatim from `OutlookWorkItemMapper` (TCM-SCORE-2)
- [x] 2.6 Add `ScoreContent()` calling helpers, emitting `teams.scoring.*` and `action_needed` into metadata (TCM-SCORE-1, TCM-SCORE-5)
- [x] 2.7 Add `ScanDeadlineCues()`/`TryFindDeadlineCue()` scanning title→body, emitting `teams.deadline.*` (TCM-SCORE-4)
- [x] 2.8 Wire `ScoreContent()` and `ScanDeadlineCues()` into `TryMap` after `BuildMetadata()`, before `WorkItem` constructor (TCM-SCORE-1)
- [x] 2.9 Verify `ResolvePriority` still handles `WorkItem.Priority` — score is metadata-only, no priority override (TCM-SCORE-1 scenario 3)

## Phase 3: Testing

- [x] 3.1 Title scoring: strong urgency weight 3, single cue weight 1, no match weight 0 (3 tests, TCM-SCORE-1, -2)
- [x] 3.2 Body scoring: high cue weight 3, medium cue weight 1, no match weight 0 (3 tests, TCM-SCORE-1)
- [x] 3.3 Mention: `@` present → weight 1/True, absent → weight 0/False (2 tests, TCM-SCORE-3)
- [x] 3.4 Deadline: title match, body fallback, date pattern, no match (4 tests, TCM-SCORE-4)
- [x] 3.5 Scoring metadata: all keys emitted, absent body/title skips keys, `action_needed` (3 tests, TCM-SCORE-5)
- [x] 3.6 Boundary: content score 7 + priority Low → `WorkItem.Priority` = Low (1 test, TCM-SCORE-1)
- [x] 3.7 Verify all existing tests remain green (regression guard)

## Phase 4: Architecture Boundary

- [x] 4.1 Add NetArchTest rule: `TeamsWorkItemMapper` has no dependency on `IPriorityScoringService`/`IInterruptionPolicyEngine` (TCM-SCORE-6)
- [x] 4.2 Verify no `using` pointing to `Aura.Application.Services` or policy namespaces in mapper (TCM-SCORE-6)
