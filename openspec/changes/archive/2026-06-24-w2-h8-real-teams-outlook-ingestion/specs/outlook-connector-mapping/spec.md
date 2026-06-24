# Delta for Outlook Connector Mapping

## MODIFIED Requirements

### Requirement: Outlook Field Mapping

The adapter MUST map a valid Outlook email payload to a canonical `WorkItem` with
`SourceType = OutlookEmail`. All required `WorkItem` fields MUST be populated from
corresponding email payload fields, including read-only metadata fields: deep link and snippet.
(Previously: Mapped core fields but did not mandate deep link and snippet from real Graph API data.)

#### Scenario: Valid email payload produces canonical WorkItem

- GIVEN an Outlook payload with all required fields present
- WHEN the adapter maps the payload
- THEN a `WorkItem` is returned with `SourceType = OutlookEmail` and all required fields populated, including deep link and snippet

#### Scenario: WorkItem SourceType is always OutlookEmail

- GIVEN any Outlook payload that produces a WorkItem
- WHEN the WorkItem is inspected
- THEN `SourceType` equals `OutlookEmail`