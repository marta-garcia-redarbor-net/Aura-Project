# Design: W2-H4 — Outlook Plugin Mapping and Initial Classification

## Technical Approach

Mirror the Teams connector slice (DTO + Mapper + Adapter) inside
`Aura.Infrastructure.Adapters.Connectors.Outlook`. The Outlook ACL translates
raw `OutlookEmailDto` payloads into canonical `WorkItem` instances using the same
`TryMap` / batch-skip / metadata-traceability conventions proven by `TeamsWorkItemMapper`.
Outlook adds two extensions: **multi-signal additive priority scoring**
(`Importance` + subject cues + sender weight + body cues) and **deadline-cue scanning**
over subject/body text. All scoring inputs are always written to `WorkItem.Metadata`
so every classification decision is explainable and testable in isolation.

No new ports, no new domain types, no shared normalization layer in this slice.

---

## Architecture Decisions

| Decision | Choice | Rejected | Rationale |
|---|---|---|---|
| ACL location | `Infrastructure.Adapters.Connectors.Outlook/` | Application-level shared normalizer | Single-provider slice; premature abstraction; Teams precedent |
| Priority model | Additive multi-signal score: Importance + subject cues + sender weight + body cues | Importance-only; subject-keyword-only | Importance may be absent/unreliable; sender and body carry independent signal; no single source is authoritative |
| Scoring function | Bounded integer weights per signal → summed score → threshold map | Rule cascade; max-wins | Additive is transparent, testable per signal, and maps deterministically; weights are constants, not magic |
| Deadline cues | Separate scan on `Subject` then `BodyPreview`; writes Metadata only — does not contribute to priority score | Fold into priority scoring | Deadline is a distinct classification concern; mixing it into the score conflates priority with urgency |
| `Source` value | `"inbox"` | Provider identifier | Neutral, domain-meaningful; mirrors `"messages"` for Teams |
| Architecture guard | New `OutlookConnectorBoundaryTests` class | Extend existing `IngestionArchitectureTests` | Co-locates boundary coverage with its subject; mirrors per-concern pattern |

---

## Data Flow

```
OutlookConnectorAdapter.ExecuteAsync(request, ct)
    │
    ├─ fixtureProvider() → IReadOnlyList<OutlookEmailDto>
    │
    └─ foreach payload
          │
          ├─ OutlookWorkItemMapper.TryMap(dto, out workItem)
          │       │
          │       ├─ [Guard]    ExternalId null/empty → return false (skip, log)
          │       ├─ [Title]    Subject → default "Outlook email {ExternalId}"
          │       ├─ [Source]   always "inbox"
          │       ├─ [Type]     WorkItemSourceType.OutlookEmail
          │       │
          │       ├─ [Priority] ResolvePriority(importance, subject, sender, body, metadata)
          │       │               importanceWeight = score(Importance)     // 0 | +1 | +3 | -1
          │       │               subjectWeight    = scanSubjectCues()     // 0 | +1
          │       │               senderWeight     = lookupSenderRule()    // 0 | +1 | +2
          │       │               bodyWeight       = scanBodyCues()        // 0 | +1 | +2
          │       │               totalScore       = sum of all weights
          │       │               ── thresholds ───────────────────────────
          │       │               score ≥ 6 → Critical
          │       │               score ≥ 2 → High
          │       │               score ≥ 0 → Medium
          │       │               score  < 0 → Low
          │       │               ── always writes to metadata ────────────
          │       │               outlook.importance.raw       (value or "absent")
          │       │               outlook.scoring.subjectCues  (matched tokens or "none")
          │       │               outlook.scoring.senderWeight (int as string)
          │       │               outlook.scoring.bodyCues     (matched tokens or "none")
          │       │               outlook.scoring.totalScore   (int as string)
          │       │
          │       ├─ [Deadline] ScanDeadlineCues(Subject, BodyPreview, metadata)
          │       │               hit  → outlook.deadline.cue + outlook.deadline.source
          │       │               miss → no entry written
          │       │
          │       └─ [Meta]     BuildMetadata: outlook.sender, outlook.conversationId,
          │                     absent/defaulted title/subject entries
          │
          ├─ success → buffer.Enqueue(workItem);  mappedCount++
          └─ failure → skippedCount++;  Log.OutlookEmailSkipped(externalId)

    └─ return ConnectorExecutionResult(identity, mappedCount, status?, failureReason?, windowEnd)
```

---

## Interfaces / Contracts

### Signal weights (constants in `OutlookWorkItemMapper`)

| Signal | Condition | Weight |
|--------|-----------|--------|
| Importance | `"High"` | +3 |
| Importance | `"Normal"` | +1 |
| Importance | `"Low"` | −1 |
| Importance | absent / unrecognized | 0 |
| Subject cues | high-priority keyword matched (see spec for patterns) | +1 |
| Subject cues | none | 0 |
| Sender weight | high-tier rule match (rule-based, local) | +2 |
| Sender weight | medium-tier rule match | +1 |
| Sender weight | unknown | 0 |
| Body cues | high-priority pattern matched (see spec for patterns) | +2 |
| Body cues | medium-priority pattern | +1 |
| Body cues | none | 0 |

Score thresholds: `≥ 6 → Critical` | `≥ 2 → High` | `≥ 0 → Medium` | `< 0 → Low`

### Metadata key convention (`outlook.*`)

| Key | Written when | Value |
|---|---|---|
| `outlook.sender` | `SenderAddress` present | address value |
| `outlook.conversationId` | `ConversationId` present | id value |
| `outlook.subject.raw` | `Subject` absent | `"absent"` |
| `outlook.subject.resolution` | `Subject` absent | `"defaulted"` |
| `outlook.importance.raw` | **Always** (scoring input) | raw string value or `"absent"` |
| `outlook.scoring.subjectCues` | **Always** (scoring input) | matched cue tokens (comma-sep) or `"none"` |
| `outlook.scoring.senderWeight` | **Always** (scoring input) | integer weight as string |
| `outlook.scoring.bodyCues` | **Always** (scoring input) | matched cue tokens (comma-sep) or `"none"` |
| `outlook.scoring.totalScore` | **Always** | integer score as string |
| `outlook.deadline.cue` | Deadline keyword/pattern hit | first 100 chars of matched context |
| `outlook.deadline.source` | Deadline keyword/pattern hit | `"subject"` or `"body"` |

### Deadline scan keywords (case-insensitive; subject first, body fallback)

`"due"`, `"by eod"`, `"by end of day"`, `"deadline"`, `"until"`, simple date pattern `\d{1,2}[\/\-]\d{1,2}`

### Log event IDs (sequential after Teams 3201–3202)

| EventId | Level | Message |
|---|---|---|
| 3203 | Information | Outlook adapter executed: source, tenant, window, mapped, skipped |
| 3204 | Warning | Outlook email skipped — missing required fields. ExternalId |

---

## File Changes

| File | Action | Description |
|---|---|---|
| `src/Aura.Infrastructure/Adapters/Connectors/Outlook/OutlookEmailDto.cs` | Create | Outlook payload record; all fields nullable; `internal sealed record` |
| `src/Aura.Infrastructure/Adapters/Connectors/Outlook/OutlookWorkItemMapper.cs` | Create | `TryMap` → multi-signal `ResolvePriority` + deadline scan + metadata traceability |
| `src/Aura.Infrastructure/Adapters/Connectors/Outlook/OutlookConnectorAdapter.cs` | Create | `IConnectorAdapter` impl; injectable `fixtureProvider`; source-generated `Log` partial class |
| `src/Aura.Infrastructure/Adapters/Connectors/DependencyInjection.cs` | Modify | Add `services.AddScoped<IConnectorAdapter, OutlookConnectorAdapter>()` |
| `tests/Aura.UnitTests/Ingestion/Outlook/OutlookWorkItemMapperTests.cs` | Create | Mapping, partial-payload, per-signal, combined-signal, and deadline-cue unit tests |
| `tests/Aura.UnitTests/Ingestion/Outlook/OutlookConnectorAdapterTests.cs` | Create | Batch execution, partial-failure, default-fixture-path tests |
| `tests/Aura.ArchitectureTests/OutlookConnectorBoundaryTests.cs` | Create | Assert `Aura.Application` and `Aura.Domain` have no dependency on Outlook namespace |

---

## Testing Strategy

| Layer | What to test | Approach |
|---|---|---|
| Unit — Mapper | Valid payload → canonical `WorkItem` with all fields | `Assert.Equal` on each mapped property |
| Unit — Mapper | Missing `ExternalId` → `TryMap` returns false | Guard condition |
| Unit — Mapper | Missing `Subject` → default title + two metadata entries | Two-key metadata assertion |
| Unit — Mapper | `Importance = "High"` alone → `Priority.High`; all scoring metadata keys present | Score = 3 → High threshold; five key assertions |
| Unit — Mapper | `Importance = "Normal"` alone → `Priority.Medium` | Score = 1 → Medium |
| Unit — Mapper | `Importance = "Low"` alone → `Priority.Low` | Score = −1 → Low |
| Unit — Mapper | Absent `Importance` + high-tier sender → `Priority.High` | senderWeight = 2, totalScore = 2 → High |
| Unit — Mapper | Absent `Importance` + high-priority body cue → `Priority.High` | bodyWeight = 2, totalScore = 2 → High |
| Unit — Mapper | All signals absent → `Priority.Medium` | totalScore = 0 → Medium sentinel |
| Unit — Mapper | All signals at max → `Priority.Critical` | totalScore ≥ 6 → Critical |
| Unit — Mapper | All five scoring metadata keys always written | `ContainsKey` true for each key regardless of signal values |
| Unit — Mapper | Subject with deadline keyword → `outlook.deadline.cue` + `outlook.deadline.source` | Two-key assertion |
| Unit — Mapper | Subject without deadline keyword → no deadline metadata keys | `ContainsKey` false |
| Unit — Adapter | All valid fixtures → enqueued count == mapped count, `Success` status | `buffer.Received(n)` via NSubstitute |
| Unit — Adapter | One invalid fixture in batch → others enqueued, `PartialFailure` status | Mixed fixture list |
| Architecture | No Outlook namespace type in `Aura.Application` assembly | NetArchTest `ShouldNot().HaveDependencyOn(...)` |
| Architecture | No Outlook namespace type in `Aura.Domain` assembly | NetArchTest `ShouldNot().HaveDependencyOn(...)` |

---

## Migration / Rollout

No migration required. Outlook folder and DI registration are additive; revert removes them completely with no schema or external state impact.

---

## Open Questions

- [x] ~~Should subject keywords elevate priority to `Critical`?~~ **Resolved:** Multi-signal scoring reaches `Critical` when total score ≥ 6 (e.g., `Importance = "High"` + subject cue + high-tier sender = 3+1+2). No special-case rule required.
