# Delta for Pull Request API

## MODIFIED Requirements

### Requirement: PullRequestDto Mapping

The system MUST map each WorkItem to a `PullRequestDto` extracting PR-specific fields
from `Metadata` using `pr.*` keys. The DTO MUST include: `Id` (from ExternalId parsed
as int), `Title`, `RepoName` (from `Metadata["pr.repo"]`), `Author`
(from `Metadata["pr.author"]`), `Status` (from `Metadata["pr.status"]` — "passing",
"running", "failed", "pending"), `ReviewerCount` (from `Metadata["pr.reviewerCount"]`),
`CommentCount` (from `Metadata["pr.commentCount"]`), `FileCount`
(from `Metadata["pr.fileCount"]`), `SourceLink`, `IsDraft`, `Priority`, `PriorityScore`,
`CreatedAt`, `UpdatedAt`, and `AttentionScope` (from `Metadata["pr.attentionScope"]` —
`direct` | `group` | `both` | `none` | `unknown`; defaults to `unknown` when key absent).

Fields without real metadata keys (`BranchName`, `SourceBranchName`, `BuildStatus` as
separate field, `ReviewRequired`, `ReviewChangesRequested`) SHALL use documented safe
defaults (empty string, "pending", 0).

NOTE: The endpoint uses the metadata keys that the ingestion pipeline
(`PrReviewWorkItemMapper`) actually writes. See `design.md` for the authoritative key
mapping.

(Previously: `PullRequestDto` did not include `AttentionScope`; reviewer data was limited
to count. `AttentionScope` is additive and appended last to avoid positional record breaks.)

#### Scenario: Metadata fields extracted correctly

- GIVEN a WorkItem with `Metadata["pr.status"]="passing"`, `Metadata["pr.reviewerCount"]="2"`, `Metadata["pr.commentCount"]="5"`
- WHEN mapped to `PullRequestDto`
- THEN `Status` SHALL be "passing", `ReviewerCount` SHALL be 2, `CommentCount` SHALL be 5

#### Scenario: Missing metadata keys default safely

- GIVEN a WorkItem with no `pr.status` key in Metadata
- WHEN mapped to `PullRequestDto`
- THEN `Status` SHALL default to "pending"

#### Scenario: Numeric metadata parsed safely

- GIVEN a WorkItem with `Metadata["pr.reviewerCount"]="invalid"`
- WHEN mapped to `PullRequestDto`
- THEN `ReviewerCount` SHALL default to 0

#### Scenario: AttentionScope extracted from metadata

- GIVEN a WorkItem with `Metadata["pr.attentionScope"]="direct"`
- WHEN mapped to `PullRequestDto`
- THEN `AttentionScope` SHALL be `direct`

#### Scenario: AttentionScope defaults to unknown when absent

- GIVEN a WorkItem with no `pr.attentionScope` key in Metadata
- WHEN mapped to `PullRequestDto`
- THEN `AttentionScope` SHALL be `unknown`
