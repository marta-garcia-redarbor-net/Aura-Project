# Logout Button Specification

## Purpose

The logout button provides a sign-out mechanism in the header that clears the session and redirects the user to `/`.

## Requirements

### Requirement: Header Sign-Out Button

The header MUST include a sign-out button that triggers authentication scheme-specific sign-out and redirects to `/`.

#### Scenario: OIDC sign-out

- GIVEN the application is running in OIDC authentication mode
- WHEN the user clicks the sign-out button
- THEN the system SHALL call `SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme)`
- AND the user SHALL be redirected to `/`

#### Scenario: Dev cookie sign-out

- GIVEN the application is running in development cookie authentication mode
- WHEN the user clicks the sign-out button
- THEN the system SHALL call `SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme)`
- AND the user SHALL be redirected to `/`

#### Scenario: Button visibility for authenticated users

- GIVEN an authenticated user is viewing any page
- WHEN the header renders
- THEN the sign-out button SHALL be visible
- AND the button SHALL be inactive or hidden when no session exists

### Requirement: Session Clearance on Sign-Out

The sign-out process MUST clear the user session completely before redirecting.

#### Scenario: Session is fully cleared

- GIVEN the user clicks the sign-out button
- WHEN the sign-out completes
- THEN all session cookies and tokens SHALL be cleared
- AND the user SHALL no longer be in an authenticated state
- AND the redirect to `/` SHALL occur after clearance
