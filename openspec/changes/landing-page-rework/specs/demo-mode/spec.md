# Delta for Demo Mode

## MODIFIED Requirements

### Requirement: Demo Mode Visibility

The system MUST determine demo mode visibility based on the `aura_demo_mode` claim in the current user's authentication cookie. When the claim is present with value `"true"`, demo controls (enter demo data, reset demo) MUST be visible. When the claim is absent, demo controls MUST be hidden. The `DemoMode__Enabled` configuration toggle MUST NOT control demo UI visibility.
(Previously: `DemoMode__Enabled` config toggle controlled button visibility.)

#### Scenario: Demo claim shows demo controls

- GIVEN the user authenticated via `/login/demo`
- AND the auth cookie contains `aura_demo_mode=true`
- WHEN the dashboard loads
- THEN "Load Sample Data" button is rendered
- AND "Reset Demo" button is rendered

#### Scenario: Real auth hides demo controls

- GIVEN the user authenticated via Microsoft Entra ID
- AND the auth cookie does NOT contain `aura_demo_mode`
- WHEN the dashboard loads
- THEN "Load Sample Data" button is NOT rendered
- AND "Reset Demo" button is NOT rendered

#### Scenario: Config toggle no longer drives visibility

- GIVEN `DemoMode__Enabled=true` in configuration
- AND the user authenticated via Microsoft Entra ID (no demo claim)
- WHEN the dashboard loads
- THEN demo controls are NOT rendered
- AND the config value has no effect on UI visibility

### Requirement: Demo Data Loading

Buttons MUST use standard ports for data loading. When Qdrant is unreachable, the system MUST degrade gracefully — items persist without embeddings.
(Previously: Unchanged — retained for archive completeness.)

#### Scenario: Qdrant fallback

- GIVEN demo controls are visible (aura_demo_mode claim present)
- AND Qdrant is unreachable
- WHEN "Load Sample Data" is clicked
- THEN items persist without embeddings
- AND no unhandled error is shown
