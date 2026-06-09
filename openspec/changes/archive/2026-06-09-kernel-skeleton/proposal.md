# Proposal: Kernel Skeleton

Aura requires a core Kernel pipeline to process incoming work items independently of the API layer, forming the backbone of the triaging system.

## Intent

Implement a minimal executable kernel (StoryBacklog H4) that introduces the `WorkItem` entity and a plugin registry. This enables sequential processing of work items via a pipeline, keeping this foundational logic strictly within `Domain`, `Application`, and `Workers`, fully decoupled from upcoming API and Auth features (H5).

## Scope

### In Scope
- Define `WorkItem` in `Aura.Domain`.
- Create `IPlugin` and `IPluginRegistry` contracts in `Aura.Application/Kernel`.
- Implement a sequential pipeline `PluginRegistry` in `Aura.Application/Kernel`.
- Add a dummy plugin `HelloPlugin` in `Aura.Application/Kernel/Plugins`.
- Create `HelloKernelWorker` in `Aura.Workers` to execute a dummy work item.

### Out of Scope
- Integration with external sources (Teams, GitHub, etc.).
- The `Aura.Api` layer, Auth, and Web UI.
- Real plugins (e.g., Semantic Indexing plugin).

## Capabilities

### New Capabilities
- `plugin-kernel`: Core execution pipeline defining how `WorkItem` instances are processed by sequential plugins.

### Modified Capabilities
- None

## Approach

Use a **Pipeline / Middleware Pattern**. `IPluginRegistry` will manage an `IEnumerable<IPlugin>`. `HelloKernelWorker` will instantiate a dummy `WorkItem`, resolve the registry, and execute the plugins sequentially. This ensures a clean architecture without overdesigning an event bus for the initial slice.

## Affected Areas

| Area | Impact | Description |
|------|--------|-------------|
| `Aura.Domain/WorkItems/` | New | `WorkItem` entity |
| `Aura.Application/Kernel/` | New | `IPlugin`, `IPluginRegistry`, `PluginRegistry` |
| `Aura.Application/DependencyInjection.cs` | Modified | Add `AddKernel()` extension method |
| `Aura.Workers/HelloKernelWorker.cs` | New | Entry point for execution flow |
| `Aura.Workers/Program.cs` | Modified | Register `HelloKernelWorker` |

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| Domain Anemia (`WorkItem`) | Medium | Start minimal but encapsulate state changes inside `WorkItem` methods rather than public setters. |
| Merge conflicts with API changes | Low | Isolate DI registrations into an `AddKernel()` extension method. |

## Rollback Plan

Revert the commits introducing the `WorkItem` domain, `Kernel` application folder, and `HelloKernelWorker`. Since it's an additive, isolated feature, removal has zero impact on existing workers.

## Dependencies

- None. Strictly internal `Domain` and `Application` components.

## Success Criteria

- [ ] `WorkItem` exists in `Domain` with encapsulated state.
- [ ] `PluginRegistry` successfully discovers and executes the dummy `HelloPlugin`.
- [ ] `HelloKernelWorker` runs without errors when the solution starts.
- [ ] No references to SDKs or Infrastructure in `Domain` and `Application`.