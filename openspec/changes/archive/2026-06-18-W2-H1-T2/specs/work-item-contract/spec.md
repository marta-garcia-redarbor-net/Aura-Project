# Delta for work-item-contract

## ADDED Requirements

### Requirement: Mandatory Field Whitespace Rejection

The `WorkItem` constructor MUST treat whitespace-only strings for `externalId`, `title`,
and `source` as absent and MUST reject construction.

(Supplements: Mandatory Field Presence — whitespace boundary coverage)

#### Scenario: externalId whitespace-only rejected

- GIVEN a caller supplies `externalId` as a whitespace-only string (e.g. `"   "`)
- WHEN the `WorkItem` is constructed
- THEN construction is rejected with an argument validation error

#### Scenario: title whitespace-only rejected

- GIVEN a caller supplies `title` as a whitespace-only string
- WHEN the `WorkItem` is constructed
- THEN construction is rejected with an argument validation error

#### Scenario: source whitespace-only rejected

- GIVEN a caller supplies `source` as a whitespace-only string
- WHEN the `WorkItem` is constructed
- THEN construction is rejected with an argument validation error

### Requirement: correlationId Whitespace Auto-Generation

A whitespace-only `correlationId` MUST be treated as absent; the system MUST
auto-generate a unique identifier.

(Supplements: correlationId Normalization — whitespace boundary coverage)

#### Scenario: correlationId whitespace-only triggers auto-generation

- GIVEN a caller supplies `correlationId` as a whitespace-only string (e.g. `"   "`)
- WHEN the `WorkItem` is constructed
- THEN `correlationId` is set to a non-empty system-generated value

---

### Requirement: capturedAtUtc Boundary Inputs

When `capturedAtUtc` is supplied as `DateTimeOffset.MinValue`, the system MUST
treat it as absent and substitute the current UTC time. When a caller supplies a
non-zero-offset `DateTimeOffset`, the system SHOULD preserve the supplied value
without rejection.

(Supplements: capturedAtUtc Resolution — `DateTimeOffset?` boundary coverage)

#### Scenario: DateTimeOffset.MinValue treated as absent

- GIVEN a caller supplies `capturedAtUtc = DateTimeOffset.MinValue`
- WHEN the `WorkItem` is constructed
- THEN `capturedAtUtc` is set to the current UTC time, not `DateTimeOffset.MinValue`

#### Scenario: Local-offset DateTimeOffset preserved without rejection

- GIVEN a caller supplies `capturedAtUtc` as a `DateTimeOffset` with a non-zero local offset
- WHEN the `WorkItem` is constructed
- THEN `capturedAtUtc` is accepted and set to the supplied value

---

### Requirement: Metadata Populated Dictionary Accepted

A non-null `Metadata` dictionary with one or more string key-value entries MUST be
accepted and all entries MUST be accessible on the constructed `WorkItem`.

(Supplements: Metadata Shape — populated dictionary positive coverage)

#### Scenario: Populated metadata accepted and preserved

- GIVEN a caller supplies a `Metadata` dictionary with one or more entries (e.g. `{ "key": "value" }`)
- WHEN the `WorkItem` is constructed
- THEN construction succeeds and all supplied entries are accessible via `Metadata`

---

## Risk Notes

**Duplicate inputs**: Two `WorkItem` instances created from the same `externalId` and
`source` combination have no deduplication rule in the current contract. This is documented
candidate coverage for a future backlog item — no deduplication assertion is imposed by
this change.
