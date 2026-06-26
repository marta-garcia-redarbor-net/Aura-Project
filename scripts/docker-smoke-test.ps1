# =============================================================================
# Aura Docker Local Development — Smoke Test
# =============================================================================
# Verifies that all containers start and communicate correctly.
# Usage: .\scripts\docker-smoke-test.ps1
# =============================================================================

param(
    [int]$TimeoutSeconds = 60,
    [int]$RetryIntervalSeconds = 5
)

$ErrorActionPreference = "Stop"
$failed = $false

function Write-Step {
    param([string]$Message)
    Write-Host "`n>> $Message" -ForegroundColor Cyan
}

function Write-Pass {
    param([string]$Message)
    Write-Host "   PASS: $Message" -ForegroundColor Green
}

function Write-Fail {
    param([string]$Message)
    Write-Host "   FAIL: $Message" -ForegroundColor Red
    $script:failed = $true
}

# ---------------------------------------------------------------------------
# 1. Check all containers are running
# ---------------------------------------------------------------------------
Write-Step "Checking all containers are running..."

$expectedServices = @("aura-qdrant", "aura-api", "aura-ui", "aura-workers")
$runningContainers = docker compose ps --format "{{.Name}}" 2>&1

foreach ($service in $expectedServices) {
    if ($runningContainers -match $service) {
        Write-Pass "$service is running"
    } else {
        Write-Fail "$service is NOT running"
    }
}

# ---------------------------------------------------------------------------
# 2. Wait for API health check
# ---------------------------------------------------------------------------
Write-Step "Waiting for API health check (timeout: ${TimeoutSeconds}s)..."

$apiPort = if ($env:API_PORT) { $env:API_PORT } else { "5190" }
$apiUrl = "http://localhost:$apiPort/health"
$elapsed = 0

while ($elapsed -lt $TimeoutSeconds) {
    try {
        $response = Invoke-WebRequest -Uri $apiUrl -TimeoutSec 5 -UseBasicParsing -ErrorAction Stop
        if ($response.StatusCode -eq 200) {
            Write-Pass "API health check returned 200"
            break
        }
    } catch {
        # Not ready yet
    }
    Start-Sleep -Seconds $RetryIntervalSeconds
    $elapsed += $RetryIntervalSeconds
}

if ($elapsed -ge $TimeoutSeconds) {
    Write-Fail "API health check timed out after ${TimeoutSeconds}s"
}

# ---------------------------------------------------------------------------
# 3. Check UI is accessible
# ---------------------------------------------------------------------------
Write-Step "Checking UI is accessible..."

$uiPort = if ($env:UI_PORT) { $env:UI_PORT } else { "5180" }
$uiUrl = "http://localhost:$uiPort/"

try {
    $response = Invoke-WebRequest -Uri $uiUrl -TimeoutSec 10 -UseBasicParsing -ErrorAction Stop
    if ($response.StatusCode -eq 200) {
        Write-Pass "UI returned 200 at $uiUrl"
    } else {
        Write-Fail "UI returned $($response.StatusCode) at $uiUrl"
    }
} catch {
    Write-Fail "UI is not accessible at $uiUrl — $_"
}

# ---------------------------------------------------------------------------
# 4. Check Qdrant is accessible
# ---------------------------------------------------------------------------
Write-Step "Checking Qdrant is accessible..."

try {
    $response = Invoke-WebRequest -Uri "http://localhost:6333/healthz" -TimeoutSec 5 -UseBasicParsing -ErrorAction Stop
    if ($response.StatusCode -eq 200) {
        Write-Pass "Qdrant healthz returned 200"
    } else {
        Write-Fail "Qdrant healthz returned $($response.StatusCode)"
    }
} catch {
    Write-Fail "Qdrant is not accessible — $_"
}

# ---------------------------------------------------------------------------
# 5. Check Workers container logs for crash loops
# ---------------------------------------------------------------------------
Write-Step "Checking Workers container logs for crash loops..."

$workersLogs = docker logs aura-workers --tail 50 2>&1
$crashIndicators = @("Unhandled exception", "StackOverflowException", "OutOfMemoryException", "Fatal error")
$hasCrash = $false

foreach ($indicator in $crashIndicators) {
    if ($workersLogs -match $indicator) {
        Write-Fail "Workers container shows crash indicator: $indicator"
        $hasCrash = $true
        break
    }
}

if (-not $hasCrash) {
    Write-Pass "Workers container logs show no crash indicators"
}

# ---------------------------------------------------------------------------
# Summary
# ---------------------------------------------------------------------------
Write-Host "`n" -NoNewline
if ($failed) {
    Write-Host "SMOKE TEST FAILED" -ForegroundColor Red
    exit 1
} else {
    Write-Host "SMOKE TEST PASSED" -ForegroundColor Green
    exit 0
}
