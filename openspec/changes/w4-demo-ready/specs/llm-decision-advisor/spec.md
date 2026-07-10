# LLM Decision Advisor Specification

## Purpose

Bounded advisory layer that reviews the deterministic interruption verdict, may adjust it only
under explicit guardrails, and emits auditable structured reasoning per work item. The
deterministic rules-based engine remains the sole authority; the LLM is a bounded modifier that
must justify any change.

## Requirements

### Requirement: Bounded Verdict Advisory

The system MUST consult the LLM advisor after the deterministic verdict is produced. The advisor
MAY return an adjusted verdict only when all guardrail conditions are satisfied: the LLM produces
a structured JSON response, the confidence meets the configured threshold, and the adjustment
target is not a critical-override item. The deterministic verdict MUST be used unchanged when any
guardrail condition fails.

#### Scenario: LLM confirms the deterministic verdict

- GIVEN the deterministic engine returns `QUEUE` for a work item
- AND the LLM evaluates the same context and agrees with the assessment
- WHEN the advisor response is received
- THEN the final verdict is `QUEUE`
- AND the trace records `guardrailOutcome: "confirmed"`

#### Scenario: LLM adjusts verdict within guardrails

- GIVEN the deterministic engine returns `QUEUE`
- AND the LLM produces a structured reason that satisfies all guardrail conditions
- WHEN the guardrail evaluates the response
- THEN the final verdict is adjusted to the LLM-suggested value
- AND the trace records `guardrailOutcome: "adjusted"` with the full LLM rationale

#### Scenario: Guardrail blocks overreach

- GIVEN the LLM suggests changing a verdict for a critical-override item
- WHEN the guardrail evaluates the suggestion
- THEN the deterministic verdict is preserved unchanged
- AND the trace records `guardrailOutcome: "blocked"` with the rejection reason

---

### Requirement: Auditable Structured Reasoning

Every advisory invocation MUST produce a structured reasoning object that includes: input signals
summary, deterministic verdict, retrieved semantic context (top-K items), LLM rationale text,
guardrail outcome, and final verdict. The object MUST be persisted as part of the decision record.

#### Scenario: Full reasoning trace produced on success

- GIVEN the LLM advisor completes successfully
- WHEN the structured reasoning object is assembled
- THEN all required fields are present and non-null

#### Scenario: Partial trace produced on LLM failure

- GIVEN the LLM call fails or returns a non-parseable response
- WHEN the trace is assembled
- THEN the trace contains: input signals, deterministic verdict, `guardrailOutcome: "llm-unavailable"`, and failure reason
- AND `finalVerdict` equals the deterministic verdict

---

### Requirement: LLM Unavailability Degradation

If the LLM is unavailable or the call times out, the system MUST fall back to the deterministic
verdict, MUST record the failure in the decision trace, and MUST NOT block or delay the decision
pipeline.

#### Scenario: Timeout degrades without blocking

- GIVEN the LLM call exceeds the configured timeout
- WHEN the timeout fires
- THEN the decision pipeline completes using the deterministic verdict
- AND the trace records `guardrailOutcome: "llm-unavailable"` with the timeout detail

#### Scenario: Semantic retrieval unavailable does not block advisor

- GIVEN Qdrant retrieval is unavailable at decision time
- WHEN the advisor is invoked
- THEN the advisor proceeds with `retrievedSemanticContext: []`
- AND the trace records the retrieval failure reason alongside the empty context
