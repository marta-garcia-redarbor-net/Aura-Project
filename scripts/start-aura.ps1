#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Resumes all Aura services after a stop. Scales ACA apps back up and resumes SQL.
.NOTES
    Run: ./scripts/start-aura.ps1
    To stop: ./scripts/stop-aura.ps1
#>

$ErrorActionPreference = 'Stop'
$rg = 'aura-rg'

Write-Host "=== Starting Aura ===" -ForegroundColor Yellow

# 1. Resume Azure SQL Database
Write-Host "→ Resuming SQL Database..." -ForegroundColor Cyan
$sqlServer = az resource list --resource-group $rg --resource-type Microsoft.Sql/servers --query "[0].name" -o tsv
$sqlDb = 'aura-db'
if ($sqlServer) {
    az sql db resume --name $sqlDb --server $sqlServer --resource-group $rg --output none 2>&1 | Out-Null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "  ✓ SQL Database resumed" -ForegroundColor Green
    } else {
        Write-Host "  ⚠ SQL resume failed (may not be paused)" -ForegroundColor DarkYellow
    }
}

# 2. Scale ACA apps back up
$apps = @{
    'aura-api-dev'      = 0   # auto-scale from 0
    'aura-ui-dev'       = 0   # auto-scale from 0
    'aura-workers-dev'  = 1   # always-on background worker
    'aura-qdrant-dev'   = 1   # always-on vector store
}

foreach ($app in $apps.Keys) {
    $minReplicas = $apps[$app]
    Write-Host "→ Starting $app (min: $minReplicas)..." -ForegroundColor Cyan
    az containerapp update `
        --name $app `
        --resource-group $rg `
        --min-replicas $minReplicas `
        --max-replicas 3 `
        --output none 2>&1 | Out-Null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "  ✓ $app started" -ForegroundColor Green
    } else {
        Write-Host "  ⚠ $app update failed" -ForegroundColor DarkYellow
    }
}

Write-Host ""
Write-Host "=== Aura running ===" -ForegroundColor Green
Write-Host "→ Run ./scripts/stop-aura.ps1 to stop" -ForegroundColor Cyan
