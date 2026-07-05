# Design: W3-H2-T4 — Teams Connector Content-Based Preliminary Scoring

## Technical Approach

Add a `ScoreContent()` private method to `TeamsWorkItemMapper` (mirroring `OutlookWorkItemMapper.ResolvePriority` structure) that computes three sub-scores from title, body, and mention detection, then emits `teams.scoring.*` and `teams.deadline.*` metadata. No sender-specific scoring is needed — VIP sender detection is handled at the global scoring level via `IPriorityScoringService`. The mapper never uses the score to override `WorkItem.Priority` — it's metadata-only for downstream consumers. `TryMap` calls `ScoreContent()` and `ScanDeadlineCues()` after `BuildMetadata()`, before the `WorkItem` constructor.

## Architecture Decisions

| Option | Tradeoff | Decision |
|--------|----------|----------|
| **Scope** — score in mapper vs new service | New service adds complexity; mapper already has all field access | **Score lives in mapper** (matches proposal) |
| **Priority override** — metadata vs inline (Outlook pattern) | Outlook uses score to set `WorkItem.Priority`; Teams has separate `ResolvePriority` from priority flag | **Metadata-only** — Teams priority flag overrides content score |
| **Sender weight** — include vs defer to global VIP | Teams sender is display name with no reliable role info; VIP sender already handled globally by `IPriorityScoringService` | **Omitted** — global VIP covers it (decision confirmed with product owner) |
| **Title weight range** — 0–1 (Outlook) vs 0–3 | Teams has no `Importance` flag; extended title range compensates for lost dimension | **0–3** with strong-urgency escalator |
| **Mention detection** — new dimension vs absorbed into body | `@`-mention is a Teams-native urgency signal with no Outlook equivalent | **Dedicated 0–1 weight** |

## Data Flow

```
TryMap(message)
  ├─ BuildMetadata(message)              ← unchanged
  ├─ ScoreContent(message, metadata)      ← NEW
  │    ├─ ScoreTitle()    → titleTokens, titleWeight (0–3)
  │    ├─ ScoreBody()     → bodyTokens, bodyWeight (0–3)
  │    ├─ DetectMention() → mentionWeight (0–1)
  │    └─ Emit: teams.scoring.*, action_needed, teams.deadline.*
  ├─ ResolvePriority(priority, metadata)  ← unchanged (priority flag wins)
  └─ new WorkItem(...)                    ← score in metadata only
```

## Scoring Formula

| Component | Input | Weight | Condition |
|-----------|-------|--------|-----------|
| Title | `message.Title` | 3 | Any match from `TitlePriorityCues` AND strong urgency token ("urgent", "asap", "blocker", "incident") or ≥2 matches |
| | | 1 | Single non-strong match |
| | | 0 | No match |
| Body | `message.BodyPreview` | 3 | Any match from `BodyHighPriorityCues` |
| | | 1 | Any match from `BodyMediumPriorityCues` |
| | | 0 | No match |
| Mention | `message.BodyPreview` | 1 | `"@"` character present |
| | | 0 | Absent |

**Total**: 0–7. **Threshold**: ≥5 → Critical signal, ≥2 → High, ≥0 → Medium. No sender weight — VIP sender handled globally by `IPriorityScoringService`. Note: Teams scores are never negative (no low-importance dimension), so `Low` priority cannot be reached via content score — that's fine since `ResolvePriority` from the flag handles it.

## Cue Array Alignment vs Outlook

| Array | Outlook (email-based) | Teams (display-name based) | Rationale |
|-------|----------------------|---------------------------|-----------|
| `TitlePriorityCues` (Subject) | "urgent", "escalation", "incident", "asap" | **Same + "blocker"** | Teams channels discuss blockers more naturally |
| `BodyHighPriorityCues` | "production down", "sev1", "immediate" | **Same + "broken"** | Teams uses "broken" in chat context |
| `BodyMediumPriorityCues` | "follow up", "review today", "needs attention" | **Same** — literal reuse | Business language is connector-agnostic |
| Sender cues | "ceo@", "cto@", "vp@" etc. | **Omitted** — VIP sender handled globally | Teams has display names only; global `IPriorityScoringService` handles VIP via email/display-name patterns |
| `DeadlineCues` | "due", "by eod", "by end of day", "deadline", "until" | **Exact copy** | Deadline language is not connector-specific |
| Mention detection | N/A | `@` in BodyPreview | **New dimension** — Teams-native signal |

## Metadata Key Design

All new keys added to `WorkItemSignalKeys.cs`:

| Const Name | Key Value | Type Emitted | When |
|------------|-----------|--------------|------|
| `TeamsScoringTitleCues` | `teams.scoring.titleCues` | comma-sep or `"none"` | Always if Title present |
| `TeamsScoringBodyCues` | `teams.scoring.bodyCues` | comma-sep or `"none"` | Always if BodyPreview present |
| `TeamsScoringMentionDetected` | `teams.scoring.mentionDetected` | `"True"` / `"False"` | Always if BodyPreview present |
| `TeamsScoringTotalScore` | `teams.scoring.totalScore` | int-string `"0"`–`"7"` | Always (scoring ran) |
| `TeamsDeadlineCue` | `teams.deadline.cue` | string context excerpt | Only on match |
| `TeamsDeadlineSource` | `teams.deadline.source` | `"title"` or `"body"` | Only on match |

**Canonical**: `WorkItemSignalKeys.ActionNeededSignal` → `"True"` when any cue matched. `WorkItemSignalKeys.TimeCriticalitySignal` already emitted by `ResolvePriority` — unchanged.

## Boundary Design

Enforcement via three mechanisms:

1. **No using directives**: `TeamsWorkItemMapper.cs` must not add `using Aura.Application.Services;` or any policy service namespace.
2. **No service constructor injection**: the mapper takes no dependencies (internal sealed class with parameterless constructor).
3. **Architecture test**: add a NetArchTest rule in `Aura.ArchitectureTests` asserting that `TeamsWorkItemMapper` has no dependency on `IPriorityScoringService` or `IInterruptionPolicyEngine`. Pattern: `Classes().That().Are(typeof(TeamsWorkItemMapper)).Should().NotHaveDependencyOn("Aura.Application.Services")`.

## File Changes

| File | Action | Description |
|------|--------|-------------|
| `src/Aura.Infrastructure/Adapters/Connectors/Teams/TeamsWorkItemMapper.cs` | Modify | Add cue arrays, `ScoreContent()`, `ScanDeadlineCues()`, metadata emission |
| `src/Aura.Application/Models/WorkItemSignalKeys.cs` | Modify | Add 6 `Teams*` const keys |
| `tests/Aura.UnitTests/Ingestion/Teams/TeamsWorkItemMapperTests.cs` | Modify | Add ~12 new tests (scoring, mention, deadline, boundary) |

## Interfaces / Contracts

All internal to the mapper. No new interfaces. The `ScoreContent` method signature:

```csharp
private static void ScoreContent(
    TeamsMessageDto message,
    Dictionary<string, string> metadata)
```

Reuses these private helpers (mirroring Outlook):

```csharp
private static (int Weight, IReadOnlyList<string> Tokens) ScoreTitle(string? title)
private static (int Weight, IReadOnlyList<string> Tokens) ScoreBody(string? body)
private static int DetectMention(string? body)
private static void ScanDeadlineCues(string? title, string? body, Dictionary<string, string> metadata)
private static bool TryFindDeadlineCue(string? value, out string cue)
private static List<string> MatchTokens(string source, IEnumerable<string> tokens)  // shareable with Outlook
private static string ExtractCueContext(string source, int matchIndex, int matchLength)  // shareable with Outlook
```

`MatchTokens` and `ExtractCueContext` are verbatim copies of the Outlook equivalents. Consider extracting to a shared utility class in a follow-up — not in scope for this change.

## Testing Strategy

| Layer | What | Approach |
|-------|------|----------|
| Unit | Title scoring: strong urgency (weight 3), single cue (weight 1), no match (weight 0) | 3 test cases × `TitlePriorityCues` |
| Unit | Body scoring: high (weight 3), medium (weight 1), none (weight 0) | 3 test cases × body cue arrays |
| Unit | Mention detection: `@` present → True, absent → False | 2 test cases |
| Unit | Deadline: title match, body fallback, date pattern, no match | 4 test cases (mirroring Outlook tests) |
| Unit | Scoring metadata: all keys emitted, absent fields skip keys | 2 test cases |
| Unit | Boundary: priority flag overrides content score (score=7, priority=Low → WorkItem.Priority=Low) | 1 test case |
| Arch | No scoring service dependency | 1 NetArchTest rule |

Total: **~16 new test methods**.

## Risk Mitigations

| Risk | Mitigation |
|------|------------|
| Cue drift between Outlook and Teams arrays | `DeadlineCues` reused exactly; title/body cues shown in alignment table above with rationale for each divergence |
| Scoring keys forgotten on new connectors | Architecture test pattern can be replicated per connector |

## Migration / Rollout

No migration required. All new metadata is additive — existing consumers ignore unrecognized keys. Rollback: revert mapper + keys file.

## Open Questions

- [ ] Extract `MatchTokens` and `ExtractCueContext` to shared utility as part of this change or defer to a follow-up?
