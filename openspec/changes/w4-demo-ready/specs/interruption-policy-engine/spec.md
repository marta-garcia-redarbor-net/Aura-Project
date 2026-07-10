# Delta for Interruption Policy Engine

## MODIFIED Requirements

### Requirement: Explainable Final Decision Authority

The system MUST evaluate a normalized context that includes priority-scoring output and
focus-state input, then return exactly one final verdict: `INTERRUPT`, `QUEUE`, or `DEFER`.
Connector preliminary scores and explicit content cues MUST remain input signals only. The
interruption policy engine SHALL be the sole authority for the final decision.

After the deterministic verdict is produced, the engine MUST perform semantic retrieval and MUST
consult the LLM decision advisor before emitting the final verdict. The LLM MAY adjust the
verdict only within explicit guardrails; the deterministic verdict MUST be preserved when
guardrails are not satisfied or when the LLM is unavailable.

The engine MUST persist every verdict to the decision store — `INTERRUPT`, `QUEUE`, and `DEFER`
records SHALL be retained. Each persisted record MUST include: `WorkItem` ID, title, source type,
verdict, `PriorityScore`, explanation text, UTC timestamp, `FocusStateType` at evaluation time,
`retrievedSemanticContext`, `llmRationale`, and `guardrailOutcome`.
(Previously: persisted record included only core fields; no LLM advisory step; no semantic retrieval at decision time)

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

#### Scenario: All verdicts persisted with full trace shape

- GIVEN the engine produces a `QUEUE` verdict for an item
- WHEN the verdict is recorded
- THEN the decision store contains a record with: `workItemId`, `title`, `sourceType`,
  `verdict: "QUEUE"`, `priorityScore`, `explanation`, `timestamp`, `focusState`,
  `retrievedSemanticContext`, `llmRationale`, and `guardrailOutcome`

#### Scenario: LLM unavailable does not block the decision

- GIVEN the LLM advisor is unavailable during evaluation
- WHEN the engine completes the decision flow
- THEN the final verdict equals the deterministic verdict
- AND the record is persisted with `guardrailOutcome: "llm-unavailable"`

---

### Requirement: Decision Query API

`GET /api/triage/decisions` MUST return paginated decision history. Each record MUST include:
`workItemId`, `title`, `sourceType`, `decision`, `priorityScore`, `explanation`, `timestamp`,
`focusState`, `retrievedSemanticContext`, `llmRationale`, and `guardrailOutcome`. Results SHALL
be sorted by `timestamp` DESC. The endpoint MUST support `?page` and `?pageSize` query parameters.
(Previously: response record did not include `retrievedSemanticContext`, `llmRationale`, or `guardrailOutcome`)

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

#### Scenario: Record includes full decision trace

- GIVEN a persisted `INTERRUPT` decision with `PriorityScore = 88` during `WindowOfOpportunity`
- WHEN the API is called
- THEN the record contains `workItemId`, `title`, `sourceType`, `decision: "INTERRUPT"`,
  `priorityScore: 88`, `explanation`, `timestamp`, `focusState: "WindowOfOpportunity"`,
  `retrievedSemanticContext`, `llmRationale`, and `guardrailOutcome`
