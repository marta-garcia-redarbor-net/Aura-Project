# Test Cleanup Specification

## Purpose

Ensures test projects are free of duplicate package references and placeholder files that cause build instability and confuse contributors.

## Requirements

### Requirement: No Duplicate Package References

Each `.csproj` file MUST NOT contain duplicate `<PackageReference>` entries for the same package. When multiple versions are referenced, only the latest compatible version MUST be retained.

#### Scenario: Duplicate Playwright reference removed

- GIVEN `tests/Aura.E2E/Aura.E2E.csproj` references both `Microsoft.Playwright` v1.52.0 and v1.54.0
- WHEN the duplicate is resolved
- THEN only the v1.54.0 reference remains
- AND `dotnet restore` succeeds without warnings about version conflicts

#### Scenario: Build succeeds after dedup

- GIVEN a csproj had duplicate package references
- WHEN `dotnet build` is run
- THEN the build completes without package-related warnings or errors

### Requirement: Placeholder Test File Removal

All `UnitTest1.cs` placeholder files MUST be removed from test projects. No test project SHOULD contain a file named `UnitTest1.cs` or equivalent scaffold.

#### Scenario: No placeholder files exist

- GIVEN the solution contains test projects `Aura.UnitTests`, `Aura.IntegrationTests`, `Aura.ArchitectureTests`, and `Aura.E2E`
- WHEN all test projects are scanned for `UnitTest1.cs`
- THEN no such file exists in any test project

#### Scenario: Remaining tests still pass

- GIVEN placeholder files have been deleted
- WHEN `dotnet test Aura.sln` is executed
- THEN all existing tests pass (no compilation errors from removed files)
