# Delta for teams-connector-mapping

## ADDED Requirements

### Requirement: TCM-SCORE-1 — Content-Based Preliminary Scoring

The mapper MUST compute a preliminary score from Teams message content using cue-based keyword detection and mention detection. The score MUST be additive from three sub-scores: title cues (0–3), body cues (0–3), and mention detection (0–1). Total score range MUST be 0–7. The score MUST be emitted as metadata only; the mapper MUST NOT use the score to override `WorkItem.Priority`. VIP sender detection is handled at the global scoring level (`IPriorityScoringService`), not in the connector mapper.

#### Scenario: Message with urgent title, sev1 body, and @mention scores 7

- GIVEN a Teams message with Title containing "urgent", BodyPreview containing "sev1", and BodyPreview containing "@"
- WHEN the mapper computes the preliminary score
- THEN total score equals 7 (3 title + 3 body + 1 mention)

#### Scenario: Message with no content cues scores 0

- GIVEN a Teams message with Title and BodyPreview containing no cue keywords and no "@" in body
- WHEN the mapper computes the preliminary score
- THEN `teams.scoring.totalScore` equals "0"
- AND no `teams.scoring.titleCues` or `teams.scoring.bodyCues` keys contain matched tokens

#### Scenario: Preliminary score does not override WorkItem.Priority

- GIVEN a Teams message with content scoring 7 but explicit priority "low"
- WHEN the mapper produces the WorkItem
- THEN `WorkItem.Priority` equals Low (resolved from priority flag, not from content score)
- AND `teams.scoring.totalScore` is emitted as metadata with value "7"

---

### Requirement: TCM-SCORE-2 — Scoring Cue Arrays

The mapper MUST define static `string[]` fields for cue-based matching, mirroring the `OutlookWorkItemMapper` pattern: `TitlePriorityCues` (title urgency keywords), `BodyHighPriorityCues` (body high-severity), `BodyMediumPriorityCues` (body medium-severity), and `DeadlineCues` (same as Outlook: "due", "by eod", "deadline", "until"). No sender-specific cue arrays are needed — VIP sender detection is handled at the global scoring level via `WorkItemSignalKeys.VipSenderSignal`. Matching MUST be case-insensitive and partial-match via `Contains`.

#### Scenario: Case-insensitive title cue match detects "urgent"

- GIVEN a Teams message with Title = "URGENT: server incident"
- WHEN the mapper scores the title
- THEN `TitlePriorityCues` match returns "urgent" as a detected cue
- AND the title weight is at least 1

#### Scenario: Body medium cue matched when no high cue present

- GIVEN a Teams message with BodyPreview = "Please follow up on this" and no high-severity cues present
- WHEN the mapper scores the body
- THEN `BodyMediumPriorityCues` match returns "follow up"
- AND body weight equals 1 (medium tier)

---

### Requirement: TCM-SCORE-3 — Mention Detection

The mapper MUST detect `@`-mentions in `BodyPreview`. Presence of `@` anywhere in `BodyPreview` MUST set mention weight to 1. Absence MUST set mention weight to 0.

#### Scenario: @mention detected raises mention weight

- GIVEN a Teams message with BodyPreview = "Can you look at this @John?"
- WHEN mention detection runs
- THEN mention weight equals 1
- AND `teams.scoring.mentionDetected` = "True"

#### Scenario: No @ in body yields zero mention weight

- GIVEN a Teams message with BodyPreview = "Please review the document"
- WHEN mention detection runs
- THEN mention weight equals 0
- AND `teams.scoring.mentionDetected` = "False"

---

### Requirement: TCM-SCORE-4 — Deadline Scan

The mapper MUST scan Title then BodyPreview for deadline cues using the `DeadlineCues` array and a date pattern regex (mirroring `OutlookWorkItemMapper.ScanDeadlineCues`). First match wins; body is scanned only if title has no match. Emit `teams.deadline.cue` and `teams.deadline.source` metadata when a deadline is found.

#### Scenario: Deadline cue found in title

- GIVEN a Teams message with Title = "Report due Friday" and BodyPreview = "See attached"
- WHEN the deadline scan runs
- THEN `teams.deadline.cue` contains contextual text around "due"
- AND `teams.deadline.source` equals "title"

#### Scenario: Date pattern in body when title has no deadline cue

- GIVEN a Teams message with Title = "Quick question" and BodyPreview = "Meeting on 05/15"
- WHEN the deadline scan runs
- THEN `teams.deadline.cue` contains "Meeting on 05/15"
- AND `teams.deadline.source` equals "body"

#### Scenario: No deadline cue found skips metadata

- GIVEN a Teams message with Title = "Weekly update" and BodyPreview = "All good here"
- WHEN the deadline scan runs
- THEN no `teams.deadline.*` metadata keys are emitted

---

### Requirement: TCM-SCORE-5 — Scoring Metadata Emission

The mapper MUST emit the following metadata keys on valid payloads: `teams.scoring.titleCues`, `teams.scoring.bodyCues`, `teams.scoring.mentionDetected`, `teams.scoring.totalScore`, and the canonical `action_needed`. Keys MUST be absent (not emitted with empty/default values) when no input data is available. No sender-specific scoring metadata is emitted — VIP sender detection is handled globally by `IPriorityScoringService`.

#### Scenario: All scoring metadata present on scored message

- GIVEN a Teams message with title cues, body cues, and @mention detected
- WHEN the mapper produces the WorkItem
- THEN metadata contains `teams.scoring.titleCues`, `.bodyCues`, `.mentionDetected`, `.totalScore`
- AND `action_needed` equals "True"

#### Scenario: No input data skips optional scoring keys

- GIVEN a Teams message with no Title and no BodyPreview
- WHEN the mapper produces the WorkItem
- THEN metadata keys `teams.scoring.titleCues`, `.bodyCues` are absent
- AND `action_needed` is absent

---

### Requirement: TCM-SCORE-6 — Boundary: No Downstream Service References

The mapper MUST NOT reference `IPriorityScoringService`, `IInterruptionPolicyEngine`, or any scoring / policy service type. Scoring is preliminary metadata emission only; final priority resolution belongs to downstream services. This boundary mirrors `OutlookWorkItemMapper`, which also emits scoring metadata without consuming it.

#### Scenario: Architecture test rejects scoring service leakage

- GIVEN the Teams mapper references a scoring service type
- WHEN architecture tests run
- THEN at least one test fails identifying the offending dependency

#### Scenario: Mapper emits metadata but does not invoke scoring service

- GIVEN a Teams message with scoring cues detected
- WHEN the mapper processes the message
- THEN `teams.scoring.*` keys are emitted
- AND no call to `IPriorityScoringService` or `IInterruptionPolicyEngine` occurs
