# Delta for dashboard-inbox-preview

## MODIFIED Requirements

### Requirement: Preview Endpoint Contract

`GET /dashboard/preview` MUST return inbox-by-source groups filtered to `Status = Pending` items only. Each inbox item MUST include: title/subject, source, relative timestamp, relevance score, unread count, and brief suggested action. `WorkItem` and domain aggregates MUST NOT appear in any response type.
(Previously: no status filter; no unread count in inbox items)

#### Scenario: Only Pending items returned

- GIVEN completed and pending WorkItems exist across sources
- WHEN `GET /dashboard/preview` is called
- THEN the response contains only items with Pending status
- AND no Completed or Faulted items are present

#### Scenario: Inbox items include UnreadCount

- GIVEN multiple pending inbox items with varying unread counts
- WHEN `GET /dashboard/preview` is called
- THEN each inbox item DTO carries an `UnreadCount` property matching the source metadata

#### Scenario: All items completed returns empty inbox

- GIVEN all WorkItems have been read and marked Completed
- WHEN `GET /dashboard/preview` is called
- THEN the inbox groups are empty
- AND the response is HTTP 200

## ADDED Requirements

### Requirement: UnreadCount on InboxItemPreviewDto

`InboxItemPreviewDto` MUST expose an `UnreadCount` property as `{ get; init; }`. This property SHALL NOT be part of the positional constructor — it is added via init-only setter to avoid breaking the existing positional record contract.

#### Scenario: UnreadCount populated from Metadata

- GIVEN a WorkItem with `Metadata["unreadCount"] = "5"`
- WHEN the DTO projection runs
- THEN `InboxItemPreviewDto.UnreadCount` equals `5`

#### Scenario: UnreadCount absent defaults to zero

- GIVEN a WorkItem without `unreadCount` in Metadata
- WHEN the DTO projection runs
- THEN `InboxItemPreviewDto.UnreadCount` equals `0`
