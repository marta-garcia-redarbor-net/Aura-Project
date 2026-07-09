# Delta for Restricted Access View

## MODIFIED Requirements

### Requirement: Centered Login Card

The system SHALL display a centered login card with the application name and two buttons: "Sign in with Microsoft" (`data-testid="login-microsoft-btn"`) and "Explore Demo Mode" (`data-testid="login-demo-btn"`). Both buttons MUST be visible regardless of the `UseEntraId` configuration value. All interactive elements MUST carry `data-testid` attributes.
(Previously: Card showed only one button based on `UseEntraId` mode — Microsoft button in Entra mode, dev button otherwise.)

#### Scenario: Login card shows both buttons regardless of config

- GIVEN the restricted access view is rendered
- WHEN the user views the login card
- THEN the card displays "Aura" as the application title
- AND "Sign in with Microsoft" button is visible with `data-testid="login-microsoft-btn"`
- AND "Explore Demo Mode" button is visible with `data-testid="login-demo-btn"`

#### Scenario: Login card shows both buttons when UseEntraId is true

- GIVEN `UseEntraId=true` and the restricted access view is rendered
- WHEN the user views the login card
- THEN both "Sign in with Microsoft" and "Explore Demo Mode" buttons are visible
- AND neither button is hidden or disabled
