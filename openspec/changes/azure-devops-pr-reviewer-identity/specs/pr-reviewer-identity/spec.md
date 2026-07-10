# PR Reviewer Identity Specification

## Purpose

Defines how Aura captures stable reviewer identity metadata during ADO PR ingestion
and derives per-user `AttentionScope` (`direct` | `group` | `both` | `none` | `unknown`)
from persisted `oid`-based signals. No ADO or Graph SDK types cross into Application or Domain.

## Requirements

### Requirement: Reviewer Identity Capture

`PrReviewDto` MUST include `Oid` (nullable string), `DisplayName`, and `IsContainer` (bool)
per reviewer. `AzureDevOpsPrProvider` MUST populate these fields from the ADO reviewer
payload when present.

#### Scenario: Reviewer with oid parsed

- GIVEN an ADO reviewer payload with a stable `id` field and `isContainer=false`
- WHEN the provider parses the reviewer list
- THEN each reviewer record SHALL have `Oid` set and `IsContainer=false`

#### Scenario: Reviewer without oid

- GIVEN an ADO reviewer payload that omits the `id` field
- WHEN the provider parses the reviewer list
- THEN `Oid` SHALL be `null` and `DisplayName` SHALL be preserved

#### Scenario: Group reviewer detected

- GIVEN an ADO reviewer payload with `isContainer=true`
- WHEN the provider parses the reviewer
- THEN `IsContainer` SHALL be `true` on the reviewer record

### Requirement: Reviewer Identity Persistence

`PrReviewWorkItemMapper` MUST write per-reviewer identity keys to `WorkItem.Metadata`.
MUST write `pr.reviewer.<n>.oid` (omitted when null), `pr.reviewer.<n>.displayName`,
and `pr.reviewer.<n>.isContainer` for each reviewer. `pr.reviewerCount` MUST be preserved.

#### Scenario: Identity keys persisted

- GIVEN a `PrReviewDto` with 2 reviewers both having non-null `Oid`
- WHEN mapped to `WorkItem` metadata
- THEN `pr.reviewer.0.oid`, `pr.reviewer.0.displayName`, `pr.reviewer.1.oid`,
  and `pr.reviewer.1.displayName` SHALL all be present

#### Scenario: Null oid key omitted

- GIVEN a reviewer with `Oid=null`
- WHEN mapped to `WorkItem` metadata
- THEN `pr.reviewer.<n>.oid` SHALL be absent and `pr.reviewer.<n>.displayName` SHALL be written

### Requirement: AttentionScope Derivation

`PullRequestMapper` MUST derive `AttentionScope` by reading `pr.reviewer.*` metadata keys
from `WorkItem`. MUST NOT reference ADO or Graph SDK types. Derivation MUST follow this
priority table:

| Condition | Result |
|-----------|--------|
| Current user `oid` matches a non-group reviewer | `direct` |
| Current user `oid` matches a group reviewer | `group` |
| Current user `oid` matches both types | `both` |
| No `oid` match; display-name match found | `direct` (flagged fallback) |
| No match found | `none` |
| Current user `oid` cannot be resolved | `unknown` |

The derived value MUST be written to `Metadata["pr.attentionScope"]` by the mapper
before the DTO is returned.

#### Scenario: Direct match by oid

- GIVEN `pr.reviewer.0.oid="user-abc"` and `pr.reviewer.0.isContainer=false`
- WHEN mapped with current user oid `"user-abc"`
- THEN `AttentionScope` SHALL be `direct`

#### Scenario: Group match by oid

- GIVEN `pr.reviewer.0.oid="grp-xyz"` and `pr.reviewer.0.isContainer=true`
- WHEN mapped with current user oid `"grp-xyz"`
- THEN `AttentionScope` SHALL be `group`

#### Scenario: No match returns none

- GIVEN all reviewer oids differ from the current user oid
- WHEN derivation runs
- THEN `AttentionScope` SHALL be `none`

#### Scenario: Unknown when user oid unavailable

- GIVEN the current user oid cannot be resolved at mapping time
- WHEN derivation runs
- THEN `AttentionScope` SHALL be `unknown`

#### Scenario: Display-name fallback is flagged

- GIVEN a reviewer with `Oid=null` and `DisplayName="Jane Doe"`
- WHEN current user display name matches `"Jane Doe"` and oid is unavailable
- THEN `AttentionScope` SHALL be `direct`
- AND `Metadata["pr.attentionScope.fallback"]` SHALL be set to `"displayName"`

### Requirement: Architecture Boundary

No ADO or Graph SDK types SHALL appear in `Aura.Application` or `Aura.Domain`.
`PullRequestMapper` MUST consume only normalized string metadata keys from `WorkItem`.
`Aura.ArchitectureTests` MUST include a rule verifying this boundary.

#### Scenario: SDK types absent from Application

- GIVEN the implementation is complete
- WHEN architecture tests run
- THEN no `Microsoft.TeamFoundation.*` or Azure DevOps SDK types SHALL be referenced
  from `Aura.Application` or `Aura.Domain` assemblies
