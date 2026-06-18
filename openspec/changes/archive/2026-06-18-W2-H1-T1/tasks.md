# Tasks: Refine Mandatory WorkItem Fields

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | 260-360 |
| 400-line budget risk | Medium |
| Chained PRs recommended | No |
| Suggested split | single PR |
| Delivery strategy | ask-always |
| Chain strategy | pending |

Decision needed before apply: Yes
Chained PRs recommended: No
Chain strategy: pending
400-line budget risk: Medium

### Suggested Work Units

| Unit | Goal | Likely PR | Notes |
|------|------|-----------|-------|
| 1 | Implement mandatory WorkItem contract with tests and worker wiring | PR 1 | Base: main/feature branch per team flow; includes tests + observability updates |

## Phase 1: RED — Contract Tests First

- [x] 1.1 RED: In `tests/Aura.UnitTests/WorkItems/WorkItemTests.cs`, add failing tests for required fields (`externalId`, `title`, `source`, `sourceType`, `priority`, `metadata`) and keep `capturedAtUtc` out of reject-on-missing cases.
- [x] 1.2 RED: In `tests/Aura.UnitTests/WorkItems/WorkItemTests.cs`, add failing tests for closed `sourceType`, `correlationId` generation/preservation, `capturedAtUtc` fallback/equality, and `schemaVersion == "v1"`.
- [x] 1.3 RED: In `tests/Aura.UnitTests/Kernel/PluginRegistryTests.cs`, update constructors/usages so tests fail until the new WorkItem contract is wired.

## Phase 2: GREEN — Domain Contract Implementation

- [x] 2.1 Create `src/Aura.Domain/WorkItems/WorkItemSourceType.cs` with values: `TeamsMessage`, `SlackMessage`, `OutlookEmail`, `CalendarAppointment`, `PrReview`, `TodoTask`.
- [x] 2.2 Create `src/Aura.Domain/WorkItems/WorkItemPriority.cs` with values: `Critical`, `High`, `Medium`, `Low`.
- [x] 2.3 GREEN: Modify `src/Aura.Domain/WorkItems/WorkItem.cs` constructor to require mandatory fields and reject null/empty inputs with argument validation.
- [x] 2.4 GREEN: In `src/Aura.Domain/WorkItems/WorkItem.cs`, normalize `CorrelationId` fallback, `CapturedAtUtc` fallback to `DateTimeOffset.UtcNow`, and fixed `SchemaVersion = "v1"`.

## Phase 3: GREEN — Application/Worker Wiring

- [x] 3.1 Modify `src/Aura.Application/Kernel/PluginRegistry.cs` to construct `WorkItem` with new enum-based parameters and metadata contract.
- [x] 3.2 Modify `src/Aura.Workers/HelloKernelWorker.cs` to pass mandatory caller inputs (`externalId`, `sourceType`, `priority`, `metadata`) plus `correlationId`, and pass `capturedAtUtc` only when source timestamp exists.
- [x] 3.3 Update `src/Aura.Workers/HelloKernelWorker.cs` structured logging to include `ExternalId`, `SourceType`, `Priority`, `CorrelationId` for the same delivery slice.

## Phase 4: REFACTOR — Clean Architecture and Test Maintainability

- [x] 4.1 REFACTOR: Consolidate test setup in `tests/Aura.UnitTests/WorkItems/WorkItemTests.cs` (builder/helper) without weakening scenario assertions.
- [x] 4.2 REFACTOR: Validate boundaries in `src/Aura.Domain/WorkItems/WorkItem.cs`, `src/Aura.Domain/WorkItems/WorkItemSourceType.cs`, `src/Aura.Domain/WorkItems/WorkItemPriority.cs`, and `src/Aura.Application/Kernel/PluginRegistry.cs` (no SDK/framework leakage into Domain/Application contracts).
- [x] 4.3 REFACTOR: Align naming/comments in `src/Aura.Domain/WorkItems/WorkItem.cs` with the `work-item-contract` and `plugin-kernel` specs.

## Phase 5: Verification

- [x] 5.1 Run `dotnet test Aura.sln` and ensure all RED scenarios are GREEN, including state transition scenarios from `plugin-kernel` spec.
- [x] 5.2 Run `dotnet build Aura.sln` to verify compile integrity across Domain, Application, and Workers.
- [x] 5.3 Confirm each spec scenario maps to at least one unit test in `WorkItemTests` or `PluginRegistryTests` and record mapping in PR notes.

### Spec Scenario Mapping (for PR notes)

- `Mandatory Field Presence / All mandatory fields provided` → `WorkItemTests.NewWorkItem_SetsProperties`
- `Mandatory Field Presence / Missing mandatory field` →
  - `WorkItemTests.Constructor_EmptyExternalId_ThrowsArgumentException`
  - `WorkItemTests.Constructor_EmptyTitle_ThrowsArgumentException`
  - `WorkItemTests.Constructor_EmptySource_ThrowsArgumentException`
  - `WorkItemTests.Constructor_InvalidSourceType_ThrowsArgumentException`
  - `WorkItemTests.Constructor_InvalidPriority_ThrowsArgumentException`
  - `WorkItemTests.Constructor_NullMetadata_ThrowsArgumentNullException`
- `sourceType Closed-Set Validation / Valid sourceType` → `WorkItemTests.NewWorkItem_SetsProperties`
- `sourceType Closed-Set Validation / Invalid sourceType` → `WorkItemTests.Constructor_InvalidSourceType_ThrowsArgumentException`
- `correlationId Normalization / provided by caller` → `WorkItemTests.Constructor_CallerProvidedCorrelationId_IsPreserved`
- `correlationId Normalization / absent` → `WorkItemTests.Constructor_EmptyCorrelationId_GeneratesCorrelationId`
- `capturedAtUtc Resolution / source timestamp provided` → `WorkItemTests.Constructor_CapturedAtUtcProvided_IsPreserved`
- `capturedAtUtc Resolution / source timestamp absent` → `WorkItemTests.Constructor_CapturedAtUtcMissing_FallsBackToCurrentUtc`
- `Fixed schemaVersion / schemaVersion on every constructed item` → `WorkItemTests.Constructor_SchemaVersion_IsAlwaysV1`
- `Metadata Shape / Empty metadata accepted` → `WorkItemTests.Constructor_EmptyMetadata_IsAccepted`
- `Metadata Shape / Null metadata rejected` → `WorkItemTests.Constructor_NullMetadata_ThrowsArgumentNullException`
- `Plugin Kernel / Valid state transition` →
  - `WorkItemTests.MarkProcessing_FromPending_Succeeds`
  - `WorkItemTests.MarkCompleted_FromProcessing_Succeeds`
  - `WorkItemTests.MarkFaulted_FromProcessing_SetsStatusAndReason`
- `Plugin Kernel / Invalid state transition` →
  - `WorkItemTests.MarkCompleted_FromPending_Throws`
  - `WorkItemTests.MarkProcessing_FromCompleted_Throws`
  - `WorkItemTests.MarkFaulted_FromPending_Throws`
  - `WorkItemTests.MarkCompleted_FromFaulted_Throws`
  - `WorkItemTests.MarkFaulted_FromCompleted_Throws`
