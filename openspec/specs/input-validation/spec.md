# Input Validation Specification

## Purpose

Server-side DTO validation using FluentValidation to reject malformed or malicious input before it reaches business logic.

## Requirements

### Requirement: FluentValidation on POST/PUT DTOs

Every DTO received by POST or PUT endpoints MUST have an associated FluentValidation validator registered via DI.

#### Scenario: Valid DTO passes validation

- GIVEN a POST endpoint receives a well-formed DTO
- WHEN all fields satisfy the validator rules
- THEN the request proceeds to the endpoint handler

#### Scenario: Invalid DTO returns 422

- GIVEN a POST endpoint receives a DTO with invalid field values
- WHEN the FluentValidation validator detects rule violations
- THEN the system responds with HTTP 422 Unprocessable Entity
- AND the response body contains a structured list of field-level validation errors

### Requirement: Required Field Validation

Validators MUST reject DTOs where required fields are missing, null, or empty strings.

#### Scenario: Missing required field

- GIVEN a DTO with a required `Name` field
- WHEN a POST request sends `{ "Name": "" }`
- THEN the system responds with HTTP 422
- AND the error body references the `Name` field

### Requirement: Format and Range Validation

Validators MUST enforce field format constraints (e.g., email format, URL format) and numeric range constraints.

#### Scenario: Invalid email format

- GIVEN a DTO with an `Email` field validated for email format
- WHEN a POST request sends `{ "Email": "not-an-email" }`
- THEN the system responds with HTTP 422
- AND the error body references the `Email` field with a format message

#### Scenario: Numeric value out of range

- GIVEN a DTO with a `Priority` field constrained to 1–5
- WHEN a POST request sends `{ "Priority": 10 }`
- THEN the system responds with HTTP 422
- AND the error body references the `Priority` field with a range message
