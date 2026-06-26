# Environment Configuration Specification

## Purpose

Ensures development environment configuration does not leak real credentials into version control and follows secure-by-default practices.

## Requirements

### Requirement: No Real Credentials in `.env`

The `.env` file MUST NOT contain real Azure AD client IDs, tenant IDs, client secrets, or any production credentials. All credential values MUST be replaced with identifiable placeholders.

#### Scenario: Placeholders present in `.env`

- GIVEN the `.env` file exists at the project root
- WHEN its contents are inspected
- THEN all credential values are placeholders (e.g., `YOUR_CLIENT_ID`, `YOUR_TENANT_ID`)
- AND no value matches a real GUID or secret format

#### Scenario: Application starts with placeholder credentials

- GIVEN `.env` contains only placeholder values
- WHEN the application starts
- THEN it logs a warning indicating dev-only mode
- AND no runtime exception occurs from missing credentials

### Requirement: `.env` in `.gitignore`

The `.env` file MUST be listed in `.gitignore` to prevent accidental commits of local credential overrides.

#### Scenario: `.env` is git-ignored

- GIVEN the `.gitignore` file exists at the repository root
- WHEN `.gitignore` is inspected
- THEN `.env` is listed as an ignored pattern
- AND `git status` does not show `.env` as an untracked or modified file (when uncommitted)
