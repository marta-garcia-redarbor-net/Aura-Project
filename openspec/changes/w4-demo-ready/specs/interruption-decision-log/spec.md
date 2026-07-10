# Delta for Interruption Decision Log

## ADDED Requirements

### Requirement: Decision Trace Inspection Panel

The page at `/triage/decisions` MUST provide a per-row trace inspection panel (expand or detail
view) that exposes the full decision trace for each work item. The panel MUST display: rules
fired, retrieved semantic context (top-K items with relevance score), LLM rationale, guardrail
outcome, and both the deterministic and final verdicts when they differ.

#### Scenario: Trace panel shows full evidence

- GIVEN a decision row exists with LLM rationale and retrieved semantic context
- WHEN the user expands or opens the trace panel for that row
- THEN rules fired, semantic context items, LLM rationale, guardrail outcome, and final verdict are all visible

#### Scenario: Trace panel for LLM-unavailable decision

- GIVEN a decision row where the LLM was unavailable during evaluation
- WHEN the user opens the trace panel
- THEN `guardrailOutcome: "llm-unavailable"` is displayed
- AND the panel confirms the deterministic verdict was used as the final verdict

#### Scenario: Trace panel for LLM-adjusted decision

- GIVEN a decision row where the LLM adjusted the deterministic verdict
- WHEN the user opens the trace panel
- THEN `guardrailOutcome: "adjusted"` is displayed with the full LLM rationale
- AND both the deterministic and final verdicts are shown for comparison

---

## MODIFIED Requirements

### Requirement: Decision History Table Columns

The decision history table MUST display these columns: Timestamp, Title, Source, Priority Score,
Decision, Focus State, Explanation, Guardrail Outcome. The table SHALL be sorted by Timestamp
DESC by default. Each row SHALL be expandable or clickable to open the decision trace inspection
panel.
(Previously: columns did not include Guardrail Outcome; clicking a row navigated to the work item detail view rather than opening the trace panel)

#### Scenario: All columns rendered correctly

- GIVEN a decision with title "Urgent PR review", source "pr-review", score 88,
  decision "INTERRUPT", focus "WindowOfOpportunity", guardrailOutcome "confirmed"
- WHEN the table renders
- THEN every column is populated with the correct value including guardrail outcome

#### Scenario: Row interaction opens trace panel

- GIVEN the user expands or clicks a decision row
- WHEN the interaction is registered
- THEN the decision trace inspection panel opens for that row's work item
