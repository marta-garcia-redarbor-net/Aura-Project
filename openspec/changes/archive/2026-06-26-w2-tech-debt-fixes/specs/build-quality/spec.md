# Build Quality Specification

## Purpose

Enforces compilation strictness and NuGet package health to prevent accumulation of warnings and dependency drift.

## Requirements

### Requirement: TreatWarningsAsErrors

The `Directory.Build.props` file MUST set `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`. All compilation warnings MUST be resolved before the setting is enabled, or treated as blocking.

#### Scenario: Build fails on warnings

- GIVEN `TreatWarningsAsErrors` is `true` in `Directory.Build.props`
- WHEN `dotnet build Aura.sln` is executed
- THEN the build succeeds only if zero warnings are produced
- AND any warning is promoted to a build error

#### Scenario: Existing warnings resolved

- GIVEN `TreatWarningsAsErrors` is enabled
- WHEN the full solution is built
- THEN the build completes with zero warnings and zero errors

### Requirement: NuGet Package Compatibility

All NuGet packages in the solution MUST be compatible with the target framework (.NET 9). Packages targeting .NET 10 or unreleased frameworks MUST be downgraded to the latest stable version compatible with .NET 9.

#### Scenario: Restore succeeds after version alignment

- GIVEN package versions have been adjusted for .NET 9 compatibility
- WHEN `dotnet restore Aura.sln` is executed
- THEN restore completes without version conflict warnings
- AND no package references a framework newer than `net9.0`

#### Scenario: Build succeeds after downgrade

- GIVEN packages have been downgraded from v10.x to v9.x equivalents
- WHEN `dotnet build Aura.sln` is executed
- THEN the build completes without errors
- AND `dotnet test Aura.sln` passes all existing tests
