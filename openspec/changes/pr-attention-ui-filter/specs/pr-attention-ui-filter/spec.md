# PR Attention UI Filter Specification

## Purpose

The API computes `AttentionScope` per PR (direct, group, both, unknown) but the UI ignores it. This spec adds UI-level attention filtering, a visual badge, and a demo-mode fix so fixture PRs survive the filter.

## Requirements

### Requirement: PullRequestResponse — AttentionScope Field

`PullRequestResponse` MUST include `string AttentionScope` as the last positional parameter, defaulting to `"unknown"`. Existing callers MUST compile without changes.

#### Scenario: Default value when omitted

- GIVEN a `PullRequestResponse` constructed without `AttentionScope`
- THEN the field SHALL equal `"unknown"`

#### Scenario: API value propagated

- GIVEN the API returns a PR with `AttentionScope = "direct"`
- WHEN deserialized into `PullRequestResponse`
- THEN the field SHALL equal `"direct"`

### Requirement: PrPreviewItemResponse — AttentionScope Field

`PrPreviewItemResponse` MUST include `string AttentionScope` as the last positional parameter, defaulting to `"unknown"`.

#### Scenario: Preview item carries attention scope

- GIVEN a `PrPreviewItemResponse` constructed with `AttentionScope = "group"`
- THEN the field SHALL equal `"group"`

### Requirement: BuildCards — Attention Scope Filter

`PrioritySummaryService.BuildCards()` MUST filter PRs before mapping. Only PRs with `AttentionScope` in `{direct, group, both}` SHALL be included. All other values MUST be excluded.

#### Scenario: Only relevant PRs appear in dashboard card

- GIVEN 5 PRs: 2 `"direct"`, 1 `"group"`, 1 `"both"`, 1 `"unknown"`
- WHEN `BuildCards` constructs the PR card
- THEN the card SHALL contain exactly 4 items
- AND the `"unknown"` PR SHALL NOT be present

#### Scenario: All PRs filtered or empty input

- GIVEN a PR list where all items have `AttentionScope = "unknown"` (or the list is empty)
- WHEN `BuildCards` constructs the PR card
- THEN the card SHALL contain 0 items
- AND the empty state SHALL render

### Requirement: Attention Badge Rendering

The dashboard PR mini-table MUST render a visual badge per row. Label mapping: `"direct"` → `"You"`, `"group"` → `"Group"`, `"both"` → `"Both"`. Each scope MUST use a distinct CSS class.

#### Scenario: Badge labels by scope

- GIVEN a PR row with a known `AttentionScope` value
- WHEN rendered in the dashboard mini-table
- THEN the badge SHALL display the mapped label (`"You"`, `"Group"`, or `"Both"`)

#### Scenario: Unknown scope fallback

- GIVEN a PR row with `AttentionScope = "unknown"` (defensive — filter should prevent this)
- WHEN rendered
- THEN no attention badge SHALL be displayed

### Requirement: Demo Mode — Fixture PR Attention Scope

When `PrReviewConnectorAdapter` uses the fixture provider (demo mode), mapped WorkItems MUST have `AttentionScope` set to `"direct"`. This override MUST only apply to fixture data, never to real source-provider data.

#### Scenario: Fixture PRs survive attention filter

- GIVEN demo mode is active (`_sourceProvider` is null)
- WHEN `ExecuteAsync` maps fixture PRs
- THEN each fixture WorkItem SHALL have `AttentionScope = "direct"`

#### Scenario: Real data unaffected by demo override

- GIVEN a real `IMessageSourceProvider<PrReviewDto>` is configured
- WHEN `ExecuteAsync` fetches and maps real PRs
- THEN `AttentionScope` SHALL come from the mapper, not be overridden

### Requirement: Attention Badge Styles

`stitch-dashboard.css` MUST include pill styles for each scope variant (`direct`, `group`, `both`) with visually distinct appearance. Existing styles MUST NOT be removed.

#### Scenario: Badge pill styles defined

- GIVEN the dashboard stylesheet is loaded
- WHEN inspecting CSS rules
- THEN attention badge classes for all three scope variants SHALL exist
