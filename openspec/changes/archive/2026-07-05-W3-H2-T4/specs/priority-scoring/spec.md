# Delta for priority-scoring

## ADDED Requirements

### Requirement: Canonical Teams Scoring Inputs

The system MUST recognize `teams.scoring.*` and `teams.deadline.*` metadata keys emitted by the Teams connector as canonical inputs. The recognized keys are: `teams.scoring.titleCues`, `teams.scoring.bodyCues`, `teams.scoring.mentionDetected`, `teams.scoring.totalScore`, `teams.deadline.cue`, `teams.deadline.source`. No `teams.scoring.senderWeight` key is emitted — VIP sender detection is handled globally by `IPriorityScoringService`. The `totalScore` MUST be treated as a preliminary signal, not a final priority — the global scorer applies its own rules over these inputs, consistent with how `outlook.scoring.totalScore` is consumed.

#### Scenario: Teams scoring keys are recognized canonical inputs

- GIVEN a WorkItem with `teams.scoring.totalScore` = "7" and `teams.scoring.titleCues` = "urgent"
- WHEN priority scoring evaluates it
- THEN the result references these Teams keys as canonical inputs in its explanation
- AND `totalScore` is treated as a preliminary signal, not an authoritative priority

#### Scenario: Absent Teams scoring keys treated as zero contribution

- GIVEN a WorkItem with no `teams.scoring.*` keys (e.g., from a connector that does not emit them)
- WHEN priority scoring evaluates it
- THEN Teams-derived signals contribute zero to the scoring explanation
- AND no error is raised for the absent keys
