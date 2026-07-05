# Interruption Policy Engine Specification

## Purpose

Define the explainable final decision contract that turns scoring and user context into `INTERRUPT`, `QUEUE`, or `DEFER`.

## Requirements

### Requirement: Explainable Final Decision Authority

The system MUST evaluate a normalized context that includes priority-scoring output and focus-state input, then return exactly one final verdict: `INTERRUPT`, `QUEUE`, or `DEFER`. Connector preliminary scores and explicit content cues MUST remain input signals only. The interruption policy engine SHALL be the sole authority for the final decision.

The engine MUST persist every verdict to the decision store — `INTERRUPT`, `QUEUE`, and `DEFER` records SHALL be retained. Each persisted record MUST include: `WorkItem` ID, title, source type, verdict, `PriorityScore`, explanation text, UTC timestamp, and the `FocusStateType` at evaluation time.

#### Scenario: Receptive context can interrupt

- GIVEN a user is in `WindowOfOpportunity`
- AND the normalized context shows urgent, action-needed work with high-interruption relevance
- WHEN the engine evaluates the item
- THEN the final verdict is `INTERRUPT` with an explanation that names the decisive signals

#### Scenario: Unavailable context defers interruption

- GIVEN a user is in `Away`
- AND the item does not meet the user's critical-interruption rules
- WHEN the engine evaluates the item
- THEN the final verdict is `DEFER`
- AND the explanation cites focus-state constraints and the evaluated signals

#### Scenario: All verdicts persisted with full shape

- GIVEN the engine produces a `QUEUE` verdict for an item
- WHEN the verdict is recorded
- THEN the decision store contains a record with: `workItemId`, `title`, `sourceType`, `verdict: "QUEUE"`, `priorityScore`, `explanation`, `timestamp`, and `focusState`

---

### Requirement: Decision Query API

`GET /api/triage/decisions` MUST return paginated decision history. Each record MUST include: `workItemId`, `title`, `sourceType`, `decision`, `priorityScore`, `explanation`, `timestamp`, `focusState`. Results SHALL be sorted by `timestamp` DESC. The endpoint MUST support `?page` and `?pageSize` query parameters.

#### Scenario: Returns paginated decisions

- GIVEN 25 persisted decisions exist
- WHEN `GET /api/triage/decisions?page=1&pageSize=10` is called
- THEN 10 records are returned, sorted by timestamp DESC
- AND the response includes total count metadata

#### Scenario: Empty history returns empty page

- GIVEN no decisions have been persisted yet
- WHEN `GET /api/triage/decisions` is called
- THEN the response is HTTP 200 with an empty items array
- AND `totalCount` equals 0

#### Scenario: Record includes full decision detail

- GIVEN a persisted `INTERRUPT` decision with `PriorityScore = 88` during `WindowOfOpportunity`
- WHEN the API is called
- THEN the record contains `workItemId`, `title`, `sourceType`, `decision: "INTERRUPT"`, `priorityScore: 88`, `explanation`, `timestamp`, and `focusState: "WindowOfOpportunity"`

---

### Requirement: Explicit Per-User Adjustment Handling

The system MUST support explicit per-user adjustment behavior without opaque learning. Narrow overrides from explicit user feedback MUST auto-apply to the next similar case for that same user. Broader or riskier generalizations MUST be represented as review-first suggestions and MUST NOT silently change final decision behavior.

#### Scenario: Narrow override applies to the next similar case

- GIVEN a user explicitly overrides a prior decision for a specific similar pattern
- WHEN a new case with the same user and similar pattern is evaluated
- THEN the engine applies that override automatically
- AND the explanation identifies the explicit per-user override

#### Scenario: Broad generalization waits for review

- GIVEN feedback implies a broader rule that could affect many future cases
- WHEN the system derives an adjustment candidate
- THEN the candidate is marked as review-first guidance
- AND the engine continues using currently approved rules until the user approves the broader change
