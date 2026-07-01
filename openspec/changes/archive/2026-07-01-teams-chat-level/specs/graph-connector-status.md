# Delta for graph-connector-status

## ADDED Requirements

### Requirement: lastMessageReadDateTime Mapping

`GraphTeamsSourceProvider` MUST map `lastMessageReadDateTime` from the Graph chat response to `TeamsMessageDto.LastMessageReadAt`. When `lastMessageReadDateTime` is null (e.g., chat never opened), the DTO field SHALL be null. `TeamsMessageDto` MUST also include `LastMessageAt` (mapped from `lastMessageDateTime`) and `UnreadCount` (mapped from `unreadCount`).

#### Scenario: lastMessageReadDateTime present maps to DTO

- GIVEN a Graph chat response with `lastMessageReadDateTime = "2026-06-30T14:00:00Z"`
- WHEN `GraphTeamsSourceProvider` maps the response
- THEN `TeamsMessageDto.LastMessageReadAt` is set to `2026-06-30T14:00:00Z`

#### Scenario: null lastMessageReadDateTime maps to null

- GIVEN a Graph chat response where `lastMessageReadDateTime` is null
- WHEN `GraphTeamsSourceProvider` maps the response
- THEN `TeamsMessageDto.LastMessageReadAt` is null

#### Scenario: lastMessageDateTime maps to LastMessageAt

- GIVEN a Graph chat response with `lastMessageDateTime = "2026-06-30T15:00:00Z"`
- WHEN `GraphTeamsSourceProvider` maps the response
- THEN `TeamsMessageDto.LastMessageAt` is set to `2026-06-30T15:00:00Z`

#### Scenario: unreadCount maps to UnreadCount

- GIVEN a Graph chat response with `unreadCount = 3`
- WHEN `GraphTeamsSourceProvider` maps the response
- THEN `TeamsMessageDto.UnreadCount` equals `3`
