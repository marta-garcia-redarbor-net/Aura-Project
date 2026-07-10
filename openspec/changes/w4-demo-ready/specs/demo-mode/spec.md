# Delta for Demo Mode

## ADDED Requirements

### Requirement: Curated Source Scenario Coverage

The demo seed data MUST include curated, realistic scenarios for Teams, Outlook, and GitHub PR
sources. Each source MUST contain at least three work items of varying urgency and content type,
designed to produce distinct decision paths: at least one `INTERRUPT`, one `QUEUE`, and one
`DEFER` verdict per source. Seed items MUST NOT use generic or randomly generated placeholder
content.

#### Scenario: Teams scenarios produce distinct verdicts

- GIVEN demo mode is enabled and the seed data is loaded
- WHEN the interruption engine evaluates Teams-sourced items
- THEN the decision log contains at least one `INTERRUPT`, one `QUEUE`, and one `DEFER` verdict from Teams items

#### Scenario: Outlook scenarios produce distinct verdicts

- GIVEN demo mode is enabled and the seed data is loaded
- WHEN the interruption engine evaluates Outlook-sourced items
- THEN the decision log contains at least one `INTERRUPT`, one `QUEUE`, and one `DEFER` verdict from Outlook items

#### Scenario: PR scenarios produce distinct verdicts

- GIVEN demo mode is enabled and the seed data is loaded
- WHEN the interruption engine evaluates PR-sourced items
- THEN the decision log contains at least one `INTERRUPT`, one `QUEUE`, and one `DEFER` verdict from PR items

#### Scenario: Jury trace is meaningful for each seeded scenario

- GIVEN the jury opens the decision trace panel for any seeded work item
- WHEN the trace panel is displayed
- THEN the LLM rationale and retrieved semantic context are non-empty and relevant to the item's source domain
