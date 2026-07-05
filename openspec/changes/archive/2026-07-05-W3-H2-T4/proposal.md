# Proposal: W3-H2-T4 — Teams connector content-based preliminary scoring

## Intent

The Teams `WorkItemMapper` currently maps messages to `WorkItem` with metadata (sender, snippet, length bucket, time-criticality from priority flag) but has NO content-based scoring — no keyword analysis, no mention detection, no preliminary score. The Outlook connector already has full content-based preliminary scoring. This change brings Teams to parity so that `IPriorityScoringService` and `IInterruptionPolicyEngine` receive the same quality of canonical signals regardless of source connector.

## Scope

### In Scope
- Add Teams-specific title cue, body cue, and mention detection to `TeamsWorkItemMapper` (no sender weight — VIP sender handled globally by `IPriorityScoringService`)
- Implement scoring formula: `TitleWeight(0-3) + BodyWeight(0-3) + MentionWeight(0-1)`
- Emit `teams.scoring.*` and `teams.deadline.*` metadata keys on `WorkItem.Metadata`
- Emit canonical `action_needed` signal
- Add Teams-specific cue arrays (title cues, body high/medium, deadline cues)
- Update unit tests in `TeamsWorkItemMapperTests.cs`
- Keep mapper as single location of preliminary scoring (no new service classes)

### Out of Scope
- Changes to `IPriorityScoringService`, `IInterruptionPolicyEngine`, or `InterruptionPolicyEngine`
- Any chat-specific scoring differentiation (chats and messages use the same scoring formula)
- Outlook scoring changes or refactoring
- Qdrant, semantic indexing, or ML-based scoring
- Per-user cue configuration or VIP sender lists
- UI changes, dashboard, or morning summary
- Audit trail or outbox changes

## Capabilities

> This section is the CONTRACT between proposal and specs phases.

### New Capabilities
- None — scoring lives inside the modified existing capabilities below.

### Modified Capabilities
- `teams-connector-mapping`: add content-based preliminary scoring signals (`teams.scoring.*`, `teams.deadline.*`, `action_needed`) to the mapper output. The spec expands to cover keyword cue arrays, mention detection, scoring formula, and threshold-to-priority mapping.
- `priority-scoring`: document `teams.scoring.*` and `teams.deadline.*` as canonical metadata inputs that the global scorer consumes.

## Approach

Mirror the `OutlookWorkItemMapper` pattern with Teams-appropriate adaptations:

1. **Cue arrays** — static `string[]` fields on `TeamsWorkItemMapper`:
   - `TitlePriorityCues`: title/subject keyword patterns (urgent, asap, blocker, incident, etc.)
   - `BodyHighPriorityCues`: body patterns (production down, sev1, broken, immediate)
   - `BodyMediumPriorityCues`: body patterns (follow up, needs attention, review)
   - `DeadlineCues`: same as Outlook (due, by eod, deadline, until, date patterns)
   - No sender-specific cue arrays — VIP sender detection is handled globally by `IPriorityScoringService`

2. **Mention detection** — check `BodyPreview` for `@` character presence

3. **Scoring formula** (integrated into `TryMap` → new `ScoreContent` private method):
   - `TitleWeight` = strong urgency (multiple cues or strong token) → 3, single cue → 1, none → 0
   - `BodyWeight` = high matches → 3, medium matches → 1, none → 0
   - `MentionWeight` = `@` detected → 1, none → 0
   - `totalScore` = sum of three weights (range 0–7)
   - Priority mapping: >=5 → Critical, >=2 → High, >=0 → Medium

4. **Deadline scan** — identical pattern to `OutlookWorkItemMapper.ScanDeadlineCues`, scanning title then body for deadline keywords and date patterns

5. **Fallback** — No scoring metadata written when inputs are absent (keys simply absent from metadata, treated as zero by downstream scorer)

## Signal Design

### Scoring metadata keys

| Key | Type | Source |
|-----|------|--------|
| `teams.scoring.titleCues` | comma-separated or `"none"` | Matched tokens from Title |
| `teams.scoring.bodyCues` | comma-separated or `"none"` | Matched tokens from BodyPreview |
| `teams.scoring.mentionDetected` | bool string | `"True"` or `"False"` |
| `teams.scoring.totalScore` | int string | Aggregate 0-7 |

### Deadline metadata keys

| Key | Type | Source |
|-----|------|--------|
| `teams.deadline.cue` | string | Extracted cue context |
| `teams.deadline.source` | `"title"` or `"body"` | Where deadline was found |

### Canonical signals

| Key | Value | When |
|-----|-------|------|
| `action_needed` | `"True"` | Any title, body, or deadline cue matched |
| `message_length_bucket` | `"short"` / `"long"` | Based on BodyPreview length (already emitted) |

## Boundary Rules

- `TeamsWorkItemMapper` writes scoring metadata ONLY. It has zero references to `IPriorityScoringService` or `IInterruptionPolicyEngine`.
- The preliminary score in `teams.scoring.totalScore` is an INPUT to the global scorer, not a final decision.
- Priority resolution in `TryMap` continues to use the existing `ResolvePriority` (from priority flag). The content score is emitted as metadata for downstream evaluation, NOT used to override `WorkItem.Priority` at the mapper level.
- Cue arrays are static and compiled into the mapper, matching Outlook convention. Future per-user customization belongs in `IUserTriagePolicyProvider`, not here.

## Files to Touch

| File | Action | Description |
|------|--------|-------------|
| `src/Aura.Infrastructure/Adapters/Connectors/Teams/TeamsWorkItemMapper.cs` | Modify | Add cue arrays, `ScoreContent()`, `ScanDeadlineCues()`, metadata emission |
| `src/Aura.Application/Models/WorkItemSignalKeys.cs` | Modify | Add `Teams...` const keys for new metadata entries |
| `tests/Aura.UnitTests/Ingestion/Teams/TeamsWorkItemMapperTests.cs` | Modify | Add scoring + deadline test cases |
| `docs/architecture/ingestion/01-microsoft-graph-teams.md` | Modify | Update placeholder with scoring signals documented |
| `openspec/specs/teams-connector-mapping/spec.md` | Modify | Add preliminary scoring requirements and scenarios |
| `openspec/specs/priority-scoring/spec.md` | Modify | Add Teams scoring keys to canonical inputs doc |

## Non-Goals

- No new service classes — all scoring logic stays inside `TeamsWorkItemMapper`
- No `IUserTriagePolicyProvider` or per-user VIP list changes
- No chat vs. message scoring differentiation
- No changes to `TeamsMessageDto` — all needed fields already exist
- No changes to `TeamsConnectorAdapter` or other pipeline components

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| Cue drift between Outlook and Teams arrays (identical business language handled differently) | Low | Reuse same `DeadlineCues` and align title/body cue design with Outlook; note any intentional divergence |
| `priority-scoring` already extended in W3-H2-A and this extends it again — spec conflicts | Low | This proposal adds `teams.scoring.*` keys as documented canonical inputs; no behavioral change to the scoring service itself |

## Rollback Plan

Revert `TeamsWorkItemMapper.cs` to prior revision. Remove added const keys from `WorkItemSignalKeys.cs`. Revert spec changes. No schema or persistent state changes involved — pure code rollback.

## Dependencies

- `openspec/specs/teams-connector-mapping/spec.md` — must read to extend
- `openspec/specs/priority-scoring/spec.md` — must read to add Teams keys
- `OutlookWorkItemMapper.cs` — reference pattern for cue arrays and scoring structure

## Success Criteria

- [ ] `TeamsWorkItemMapper` emits `teams.scoring.*` metadata on valid payloads
- [ ] `TeamsWorkItemMapper` emits `teams.deadline.*` when deadline cues found
- [ ] `TeamsWorkItemMapper` sets `action_needed` correctly
- [ ] Keyword matching is case-insensitive and partial-match (same as Outlook)
- [ ] Mention detection and deadline scan have dedicated tests
- [ ] All existing mapper tests remain green (no regression)
- [ ] No references to `IPriorityScoringService` or `IInterruptionPolicyEngine` in the mapper
- [ ] New signal keys documented in `WorkItemSignalKeys.cs`
