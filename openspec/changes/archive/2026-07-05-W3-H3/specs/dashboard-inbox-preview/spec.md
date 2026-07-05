# Delta for dashboard-inbox-preview

## ADDED Requirements

### Requirement: PriorityScore on Dashboard DTOs

`InboxItemPreviewDto` and all dashboard-scope DTOs MUST carry a nullable `PriorityScore` property. Items SHALL be rendered in descending PriorityScore order in dashboard views. Items with equal score SHALL be sub-ordered by `capturedAtUtc` DESC.

#### Scenario: DTO carries PriorityScore

- GIVEN a pending `WorkItem` with `PriorityScore = 85`
- WHEN `GET /dashboard/preview` is called
- THEN the corresponding DTO has `priorityScore: 85`

#### Scenario: Dashboard items sorted by priority

- GIVEN pending items with PriorityScore 90, 60, and null (Normal → 50)
- WHEN the dashboard inbox panel renders
- THEN items display in order: 90, 60, 50

### Requirement: Priority Counts and Banding

The dashboard MUST display two summary stats: total pending count and high-priority count. The system SHALL define high-priority as `PriorityScore >= 75` or the Critical-priority derived default. Groups with non-zero high-priority count SHALL display a visual badge.

#### Scenario: Dashboard shows pending and high-priority counts

- GIVEN 15 pending items, 4 of which have PriorityScore >= 75
- WHEN the dashboard renders
- THEN "15 pending" and "4 high priority" are visible as summary stats

#### Scenario: Zero high-priority renders zero

- GIVEN no items have PriorityScore >= 75
- WHEN the dashboard renders
- THEN the high-priority count displays "0"

### Requirement: Top-3 Priority Highlighting

The dashboard inbox panel MUST highlight the top 3 highest-priority items. Each highlighted item MUST display a visible importance badge. Items with equal PriorityScore SHALL be sub-ordered by `capturedAtUtc` DESC to determine the top-3 boundary.

#### Scenario: Top-3 highlighted with badge

- GIVEN 10 pending items, the top 3 have scores 95, 90, 85
- WHEN the dashboard inbox panel renders
- THEN the first 3 items display an importance badge
- AND items 4+ do not display the badge

#### Scenario: Fewer than 3 items all highlighted

- GIVEN only 2 pending items exist
- WHEN the dashboard inbox panel renders
- THEN both items display the importance badge

#### Scenario: Tie at boundary includes all tied items

- GIVEN items with scores: 95, 90, 85, 85 (fourth tied with third)
- WHEN the dashboard inbox panel renders
- THEN all four items with score 85+ are highlighted
- AND items below 85 are not highlighted
