# Delta for Dashboard Inbox Preview

## ADDED Requirements

### Requirement: PrioritySummaryCard Per-Card Empty State

When a PrioritySummaryCard has zero items, the component MUST render a card-specific positive empty state instead of the generic "No items" message. Each card's empty state MUST display: an icon (Material Symbol), a title, a subtitle, and a footer link. The footer link MUST remain visible in the empty state (previously hidden). Empty-state properties are supplied by the `PrioritySummaryCard` model (see `pr-connector-ui` delta for model contract).

#### Scenario: Teams Mentions card empty state

- GIVEN the Teams Mentions card has 0 items
- WHEN `PrioritySummaryCards.razor` renders
- THEN the card displays icon `check_circle`, title "Inbox Zero", subtitle "Everything is optimal. Your cognitive load is clear."
- AND a footer link "View All Mentions" pointing to the card's `ViewAllUrl` is visible

#### Scenario: Outlook card empty state

- GIVEN the Outlook card has 0 items
- WHEN `PrioritySummaryCards.razor` renders
- THEN the card displays icon `mark_email_read`, title "All Caught Up", subtitle "Take a deep breath."
- AND a footer link "See All Emails" pointing to the card's `ViewAllUrl` is visible

#### Scenario: Schedule Today card empty state

- GIVEN the Schedule Today card has 0 items
- WHEN `PrioritySummaryCards.razor` renders
- THEN the card displays icon `event_available`, title "Schedule Clear", subtitle "No meetings for today. Enjoy your focused time."
- AND a footer link "View Full Schedule" pointing to the card's `ViewAllUrl` is visible

#### Scenario: Non-empty card unaffected

- GIVEN a card has 1 or more items
- WHEN `PrioritySummaryCards.razor` renders
- THEN the card displays its normal item list — no empty-state icon, title, or subtitle is shown
