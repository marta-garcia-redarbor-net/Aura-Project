# Delta for Container Config

## ADDED

### Req: ACA Health Probes

Expose `GET /health`: 200 healthy, 503 if deps fail.

#### Scenario: Healthy

- GIVEN container running
- WHEN `GET /health`
- THEN 200 `{"status":"Healthy"}`

#### Scenario: Unhealthy

- GIVEN db unreachable
- WHEN `GET /health`
- THEN 503

## MODIFIED

### Req: Multi-Arch

Dockerfiles MUST support `buildx` for amd64 and arm64.
(Previously: single-arch)

#### Scenario: Buildx manifest

- GIVEN `buildx build --platform linux/amd64,linux/arm64`
- WHEN manifest inspected
- THEN both platforms present

### Req: ASPNETCORE_URLS

Add `ASPNETCORE_URLS=http://*:8080` for ACA.
(Previously: no constraint)

#### Scenario: Port

- GIVEN container on ACA
- WHEN port checked
- THEN binds `http://*:8080`
