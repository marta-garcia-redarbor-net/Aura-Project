# Delta for Connector Execution

## ADDED Requirements

### Requirement: Continuous Polling Execution

The ConnectorExecutionWorker MUST run as a continuous background polling service. The worker MUST NOT stop the application host after a single execution cycle. The loop MUST exit only when a cancellation is requested via the stopping token.

#### Scenario: Worker polls at configured interval

- GIVEN the worker is started with a `PollingInterval` of 5 minutes
- WHEN the worker completes one execution cycle
- THEN the worker waits `PollingInterval` before starting the next cycle
- AND the application host remains running

#### Scenario: Worker stops gracefully on cancellation

- GIVEN the worker is in a delay between polling cycles
- WHEN a cancellation is requested via the stopping token
- THEN the delay completes or is cancelled immediately
- AND the worker exits without calling `StopApplication`

#### Scenario: Worker continues after adapter failure

- GIVEN one adapter throws an exception during execution
- WHEN the execution cycle completes (success or failure)
- THEN the worker logs the error
- AND the worker continues to the next polling cycle without stopping

### Requirement: Fresh DI Scope Per Iteration

The worker MUST create a new `IServiceScope` at the start of each polling iteration. All dependencies resolved within the iteration MUST come from that scope. The scope MUST be disposed when the iteration ends, whether it succeeds or fails.

#### Scenario: Each iteration resolves fresh dependencies

- GIVEN the worker starts a new polling iteration
- WHEN services are resolved for the iteration
- THEN they come from a new DI scope
- AND the scope is disposed after the iteration completes

#### Scenario: Scope disposal on exception

- GIVEN the worker is in the middle of an iteration and an unhandled exception occurs
- WHEN the catch block executes
- THEN the scope is disposed in a finally block
- AND the worker continues to the next polling cycle

### Requirement: Configurable Polling Interval

The worker MUST support a configurable polling interval with a default of 5 minutes. The interval SHOULD be read from application configuration. A `PollingInterval` property with a fallback default MUST be present.

#### Scenario: Default interval applied when unconfigured

- GIVEN no custom polling interval is configured
- WHEN the worker starts
- THEN the polling interval defaults to 5 minutes

#### Scenario: Custom interval from configuration

- GIVEN configuration specifies a polling interval of 10 minutes
- WHEN the worker starts
- THEN the worker polls every 10 minutes

### Requirement: Application Lifetime Independence

The worker MUST NOT depend on `IHostApplicationLifetime`. The host lifecycle MUST be managed by the runtime, not by any worker. Any previous `IHostApplicationLifetime` dependency MUST be removed.

#### Scenario: Worker does not inject application lifetime

- GIVEN the worker class is instantiated
- WHEN its constructor parameters are inspected
- THEN `IHostApplicationLifetime` is not among them
