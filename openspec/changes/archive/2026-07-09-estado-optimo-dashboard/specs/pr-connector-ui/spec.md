# Delta for PR Connector UI

## ADDED Requirements

### Requirement: PrioritySummaryCard — Empty State Model Properties

`PrioritySummaryCard` MUST expose five new properties: `EmptyIcon` (string), `EmptyTitle` (string), `EmptySubtitle` (string), `EmptyFooterLabel` (string), `EmptyFooterUrl` (string). `PrioritySummaryService.BuildCards()` MUST populate these per card. Cards with items still populate the properties but they are not rendered.

#### Scenario: Model carries empty-state properties

- GIVEN a `PrioritySummaryCard` is constructed
- WHEN the record is created
- THEN `EmptyIcon`, `EmptyTitle`, `EmptySubtitle`, `EmptyFooterLabel`, and `EmptyFooterUrl` are set to non-null values

#### Scenario: BuildCards populates per-card values

- GIVEN `PrioritySummaryService.BuildCards()` executes
- WHEN cards are built for all four sources
- THEN each card has distinct empty-state values matching the proposal table

### Requirement: PR Card Empty State Rendering

When the Pull Requests card has zero items, it MUST render its card-specific positive empty state. The card MUST display icon `verified`, title "Queue Empty", subtitle "No pending reviews. Your workspace is clear." A footer link "View All Repositories" pointing to the card's `ViewAllUrl` MUST be visible.

#### Scenario: PR card empty state

- GIVEN the PR card has 0 items and `IsPrCard = true`
- WHEN `PrioritySummaryCards.razor` renders
- THEN the card displays icon `verified`, title "Queue Empty", subtitle "No pending reviews. Your workspace is clear."
- AND a footer link "View All Repositories" is visible
