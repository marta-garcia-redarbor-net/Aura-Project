# Design: Refine Mandatory WorkItem Fields (W2-H1-T1)

## Technical Approach

Extend the `WorkItem` Domain constructor to enforce the full mandatory-field contract
defined in `work-item-contract` spec. Two new Domain enums (`WorkItemSourceType`,
`WorkItemPriority`) encapsulate the closed sets. Normalization — `correlationId`
auto-generation and `capturedAtUtc` inline fallback — is inlined in the constructor
body, following the existing `CreatedAt = DateTimeOffset.UtcNow` pattern.
No new abstractions, ports, or SDK dependencies are introduced.

## Architecture Decisions

| Option | Tradeoff | Decision |
|--------|----------|----------|
| `capturedAtUtc` fallback: inline `DateTimeOffset.UtcNow` | Matches existing `CreatedAt` pattern (WorkItem line 27); fallback fires only when caller omits value; tests always supply explicit timestamps | ✅ **Chosen** |
| `capturedAtUtc` fallback: `IClock` abstraction | Fully injectable; zero precedent in codebase; proposal explicitly defers factory/abstraction; adds port with no current consumer | ❌ Deferred |
| `sourceType`: validated `string` | No new types; runtime-only enforcement; typos undetected at compile time | ❌ Rejected |
| `sourceType`: `WorkItemSourceType` enum | Follows `WorkItemStatus` / `SemanticCollectionType` pattern; compile-time closed-set safety; pure Domain, zero deps | ✅ **Chosen** |
| `sourceType`: value object | Richer encapsulation; proposal explicitly defers | ❌ Deferred |
| `priority`: `WorkItemPriority` enum | Follows domain enum convention; minimal footprint; no existing type to reuse | ✅ **Chosen** |

## Data Flow

```
Caller (Worker / future Adapter)
    │
    ├─ externalId, title, source, sourceType(enum), priority(enum), metadata  ──┐
    ├─ correlationId?  (optional string)                                         │
    └─ capturedAtUtc?  (optional DateTimeOffset)                                 │
                                                                     WorkItem ctor
                                                                          │
                                              ┌───────────────────────────┼──────────────────┐
                                         Validate all               Normalize               Fix
                                         mandatory fields         correlationId            schemaVersion
                                         (ArgumentException)      if null/empty            = "v1"
                                              │                    → Guid.NewGuid()         │
                                         capturedAtUtc ──────────────────────────────────── │
                                         if null → DateTimeOffset.UtcNow (inline)           │
                                              └───────────────────────────┴──────────────────┘
                                                               Immutable entity ready
```

## File Changes

| File | Action | Description |
|------|--------|-------------|
| `src/Aura.Domain/WorkItems/WorkItem.cs` | Modify | Extended constructor: new mandatory params, normalization, guards |
| `src/Aura.Domain/WorkItems/WorkItemSourceType.cs` | Create | Enum: `TeamsMessage`, `SlackMessage`, `OutlookEmail`, `CalendarAppointment`, `PrReview`, `TodoTask` |
| `src/Aura.Domain/WorkItems/WorkItemPriority.cs` | Create | Enum: `Critical`, `High`, `Medium`, `Low` |
| `src/Aura.Workers/HelloKernelWorker.cs` | Modify | Construct `WorkItem` with full mandatory contract |
| `tests/Aura.UnitTests/WorkItems/WorkItemTests.cs` | Modify | Update existing tests + add new invariant scenarios from spec |
| `tests/Aura.UnitTests/Kernel/PluginRegistryTests.cs` | Modify | Update test-helper `WorkItem` construction to full contract |

## Interfaces / Contracts

```csharp
// Extended constructor — Domain layer, no external dependencies
public WorkItem(
    string externalId,
    string title,
    string source,
    WorkItemSourceType sourceType,
    WorkItemPriority priority,
    IReadOnlyDictionary<string, string> metadata,
    string? correlationId = null,
    DateTimeOffset? capturedAtUtc = null)

// Normalization inlined in ctor body:
CorrelationId = string.IsNullOrEmpty(correlationId) ? Guid.NewGuid().ToString() : correlationId;
CapturedAtUtc = capturedAtUtc ?? DateTimeOffset.UtcNow;   // inline — follows CreatedAt pattern
SchemaVersion = "v1";                                       // domain-fixed constant

// New enums — Aura.Domain.WorkItems namespace
public enum WorkItemSourceType
{
    TeamsMessage, SlackMessage, OutlookEmail, CalendarAppointment, PrReview, TodoTask
}

public enum WorkItemPriority { Critical, High, Medium, Low }
```

## Testing Strategy

| Layer | What to Test | Approach |
|-------|-------------|----------|
| Unit | Each mandatory field missing → `ArgumentException` | `[Theory][InlineData]` per field |
| Unit | `correlationId` auto-generated when absent | Assert `!string.IsNullOrEmpty` |
| Unit | `correlationId` preserved when caller-supplied | Assert equals supplied value |
| Unit | `capturedAtUtc` equals caller-supplied timestamp | Assert equality |
| Unit | `capturedAtUtc` fallback ≤ post-construction `UtcNow` | Assert `<=` capture after ctor |
| Unit | `schemaVersion == "v1"` on any valid construction | Single assert |
| Unit | `Metadata` null → rejected; empty → accepted | `ArgumentNullException` vs success |
| Unit | State transitions `Pending → Processing → Completed/Faulted` | Existing tests re-pinned |
| Unit (registry) | PluginRegistry tests compile with new ctor shape | Update test WorkItem construction |

## Migration / Rollout

No migration required. Change is confined to Domain constructor contract, Workers
bootstrap, and unit tests. No persistence, no serialization format, no external
adapter changes in scope.

## Open Questions

None — all design choices resolved above.
