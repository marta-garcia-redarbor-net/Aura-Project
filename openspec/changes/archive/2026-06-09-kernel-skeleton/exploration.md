## Exploration: kernel-skeleton

### Current State
Aura currently has a Worker layer (`SemanticIndexSyncWorker`, `Worker`) and Application ports, but no core Kernel or Plugin system to process incoming work items. There is no base `WorkItem` model in the Domain. 

### Affected Areas
- `Aura.Domain/WorkItems/` (New) — To house the `WorkItem` entity.
- `Aura.Application/Kernel/` (New) — To house `IPlugin`, `IPluginRegistry`, and the concrete `PluginRegistry`.
- `Aura.Application/Kernel/Plugins/` (New) — For the dummy plugin.
- `Aura.Application/DependencyInjection.cs` — Will be modified to register the Kernel components.
- `Aura.Workers/HelloKernelWorker.cs` (New) — The entry point for the hello-kernel execution flow.
- `Aura.Workers/Program.cs` — To register the new worker.

### Approaches
1. **Pipeline / Middleware Pattern (Recommended)**
   - **Description**: A list of `IPlugin` instances executed sequentially on a `WorkItem`.
   - **Pros**: Simple, easy to implement in week 1, matches Clean Architecture, highly decoupled.
   - **Cons**: Sequential execution means a slow plugin blocks the pipeline (can be evolved later).
   - **Effort**: Low

2. **Event-Driven / Pub-Sub**
   - **Description**: Publishing `WorkItemCreated` and letting plugins subscribe.
   - **Pros**: Fully asynchronous, easy to parallelize.
   - **Cons**: Overdesign for a "kernel skeleton", harder to trace execution flow and aggregate results.
   - **Effort**: Medium

### Recommendation
Proceed with **Approach 1 (Pipeline / Middleware Pattern)**. It fulfills the W1-H4 requirements strictly without overdesign.
- Place `WorkItem` in `Aura.Domain/WorkItems`.
- Place `IPlugin` and `IPluginRegistry` in `Aura.Application/Kernel`.
- Implement `PluginRegistry` in `Aura.Application/Kernel` leveraging constructor injection (`IEnumerable<IPlugin>`) to remain framework-agnostic.
- Create `HelloKernelWorker` in `Aura.Workers` that instantiates a dummy `WorkItem`, resolves `IPluginRegistry`, and executes the pipeline.

### Key Decisions for H4/H5 Independence
- **No Api Layer Changes**: H4 is strictly confined to `Domain`, `Application`, and `Workers`. H5 will touch `Api`, `Infrastructure`, and `Application/Auth`.
- **Isolated Registration**: H4 should expose a dedicated `AddKernel()` extension method in `Aura.Application/DependencyInjection.cs` to minimize merge conflicts with H5's `AddAuth()`.
- **Worktree Separation**: H4 and H5 can be developed in entirely separate worktrees; the only common file modified is `Aura.Application/DependencyInjection.cs` (merge conflict easily resolvable).

### Risks
- **Domain Anemia**: `WorkItem` might start as an anemic data bag. We should ensure it encapsulates state properly as the kernel evolves.

### Ready for Proposal
Yes — The orchestrator should proceed to `sdd-propose` and formally present this pipeline architecture and separation strategy.