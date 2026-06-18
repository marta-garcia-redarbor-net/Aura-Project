# Plugin Kernel Specification

## Purpose

The Plugin Kernel provides a core execution pipeline to process `WorkItem` entities sequentially without dependencies on external APIs, SDKs, or the web layer. It ensures triaging logic remains decoupled and pure.

## Requirements

### Requirement: WorkItem State Encapsulation

The `WorkItem` entity MUST encapsulate its own state and MUST NOT expose public setters for state mutations. It SHALL reside entirely in the `Domain` layer without external dependencies. Construction MUST satisfy the full mandatory-field contract defined in the `work-item-contract` specification.

#### Scenario: Valid state transition
- GIVEN a `WorkItem` in an initial state
- WHEN a valid domain operation is invoked to transition its state
- THEN the `WorkItem` updates its internal state successfully
- AND exposes the new state via public getters

#### Scenario: Invalid state transition
- GIVEN a `WorkItem` in an initial state
- WHEN an invalid domain operation is attempted
- THEN the `WorkItem` rejects the transition and maintains its current state

### Requirement: Sequential Plugin Execution

The `PluginRegistry` MUST execute registered plugins sequentially against a given `WorkItem`.

#### Scenario: Successful execution
- GIVEN a `PluginRegistry` with registered plugins
- AND a valid `WorkItem`
- WHEN the execution pipeline is triggered
- THEN each plugin MUST execute in order
- AND the `WorkItem` state MUST reflect the cumulative changes applied by all plugins

#### Scenario: Empty registry execution
- GIVEN a `PluginRegistry` with NO registered plugins
- WHEN the execution pipeline is triggered with a `WorkItem`
- THEN the execution MUST complete successfully without modifying the `WorkItem`

### Requirement: Resilient Plugin Execution

The execution pipeline MUST safely handle failures within individual plugins to prevent process crashes.

#### Scenario: Plugin failure handling
- GIVEN a sequence of plugins
- WHEN a plugin throws an exception during `WorkItem` processing
- THEN the pipeline MUST catch the exception
- AND log the error
- AND abort subsequent plugin execution for that specific `WorkItem`
- AND preserve the overall worker process stability

### Requirement: Architectural Layer Constraints

The `IPlugin` and `IPluginRegistry` contracts, along with the `PluginRegistry` implementation, MUST reside in the `Application` layer. They SHALL NOT depend on infrastructure concerns or external SDKs. `Workers` MUST solely act as the orchestration entry point.

#### Scenario: Dependency Injection Initialization
- GIVEN an orchestration layer (e.g., `HelloKernelWorker`)
- WHEN the worker starts
- THEN it MUST successfully resolve `IPluginRegistry` via Dependency Injection
- AND the registry MUST contain all configured plugins (e.g., `HelloPlugin`)
- AND no infrastructure details are leaked into the kernel components
