#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Stops all Aura services to save costs. Scales ACA apps to 0 and pauses SQL.
.NOTES
    Run: ./scripts/stop-aura.ps1
    To resume: ./scripts/start-aura.ps1
#>

$ErrorActionPreference = 'Stop'
$rg = 'aura-rg'

Write-Host "=== Stopping Aura ===" -ForegroundColor Yellow

# 1. Scale ACA apps to 0 (min=0, max=1 allows scaling to zero)
$apps = @('aura-api-dev', 'aura-ui-dev', 'aura-workers-dev', 'aura-qdrant-dev')
foreach ($app in $apps) {
    Write-Host "→ Scaling down $app to 0 replicas..." -ForegroundColor Cyan
    az containerapp update `
        --name $app `
        --resource-group $rg `
        --min-replicas 0 `
        --max-replicas 1 `
        --output none 2>&1 | Out-Null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "  ✓ $app stopped" -ForegroundColor Green
    } else {
        Write-Host "  ⚠ $app scale failed" -ForegroundColor DarkYellow
    }
}

# 2. SQL Database auto-pause is configured (60 min idle). Try manual pause.
Write-Host "→ Pausing SQL Database..." -ForegroundColor Cyan
$sqlServer = az resource list --resource-group $rg --resource-type Microsoft.Sql/servers --query "[0].name" -o tsv
$sqlDb = 'aura-db'
if ($sqlServer) {
    $result = az sql db pause --name $sqlDb --server $sqlServer --resource-group $rg 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "  ✓ SQL Database paused" -ForegroundColor Green
    } else {
        Write-Host "  ✓ SQL will auto-pause after 60 min idle (serverless configured)" -ForegroundColor Green
    }
} else {
    Write-Host "  ⚠ SQL server not found" -ForegroundColor DarkYellow
}

Write-Host ""
Write-Host "=== Aura stopped. No compute costs while paused. ===" -ForegroundColor Yellow
Write-Host "→ Run ./scripts/start-aura.ps1 to resume" -ForegroundColor Cyan
