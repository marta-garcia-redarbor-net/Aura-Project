# Priority Scoring Specification

## Purpose

Define deterministic, explainable global scoring that turns canonical work-item signals into policy-ready priority input.

## Requirements

### Requirement: Canonical Explainable Scoring Inputs

The system MUST derive priority scoring from canonical `WorkItem.Metadata` plus normalized evaluation context. Approved business inputs are urgency, sender, and content-derived explicit cues that remain traceable inputs, not opaque final authority. Normalized signals MAY include `vip_sender`, `action_needed`, `ack_only`, `time_criticality`, and `message_length_bucket`. Qdrant or any semantic index MUST NOT be the source of truth for interruption scoring.

#### Scenario: Canonical inputs drive the explanation

- GIVEN a work item with canonical urgency, sender, and explicit cues in metadata
- WHEN priority scoring evaluates it
- THEN the result references only canonical inputs and normalized signals in its explanation
- AND no semantic index is treated as authoritative input

#### Scenario: Content cues remain traceable

- GIVEN content-derived cues contribute to the score
- WHEN the score explanation is inspected
- THEN each content cue is named as an explicit factor
- AND the final score is not attributed to opaque model authority

---

### Requirement: Deterministic Per-User Scoring Rules

Priority scoring MUST be deterministic for the same canonical inputs and the same user-specific rule set. Rule adjustments MUST remain explicit, per-user, and user-adjustable. The system MUST NOT silently self-recalibrate scoring behavior.

#### Scenario: Same user and same inputs produce the same score

- GIVEN the same user rule set and the same canonical scoring inputs
- WHEN priority scoring evaluates the item more than once
- THEN the same score and factor explanation are returned each time

#### Scenario: Explicit per-user differences are respected

- GIVEN two users with different explicit scoring rules
- WHEN the same canonical work item is evaluated for each user
- THEN different score outputs are allowed
- AND the difference is explainable from the users' explicit rule sets
