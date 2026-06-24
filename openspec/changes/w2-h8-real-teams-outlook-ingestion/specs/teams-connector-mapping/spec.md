# Delta for Teams Connector Mapping

## MODIFIED Requirements

### Requirement: Teams Field Mapping

The adapter MUST map a valid Teams message payload to a canonical `WorkItem` with
`SourceType = TeamsMessage`. All required `WorkItem` fields MUST be populated from
the corresponding Teams payload fields, including read-only metadata fields: deep link and snippet.
(Previously: Mapped core fields but did not mandate deep link and snippet from real Graph API data.)

#### Scenario: Valid Teams payload produces canonical WorkItem

- GIVEN a Teams message payload with all required fields present
- WHEN the adapter maps the payload
- THEN a `WorkItem` is returned with `SourceType = TeamsMessage` and all fields populated, including deep link and snippet

#### Scenario: WorkItem SourceType is always TeamsMessage

- GIVEN any Teams message payload that produces a WorkItem
- WHEN the resulting WorkItem is inspected
- THEN `SourceType` equals `TeamsMessage`