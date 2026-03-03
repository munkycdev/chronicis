$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

param(
    [string]$ConfigPath = ".\scripts\rollout-checkpoint.sample.json",
    [int]$TimeoutSeconds = 20
)

function Get-ConfigValue {
    param(
        [Parameter(Mandatory = $true)]
        [object]$Node,
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    $current = $Node
    foreach ($segment in $Path.Split('.')) {
        if ($null -eq $current.PSObject.Properties[$segment]) {
            throw "Missing required config value: $Path"
        }

        $current = $current.$segment
    }

    return $current
}

if (-not (Test-Path $ConfigPath)) {
    throw "Config file not found: $ConfigPath"
}

$rawConfig = Get-Content -Raw -Path $ConfigPath
$config = $rawConfig | ConvertFrom-Json

$apiBaseUrl = (Get-ConfigValue -Node $config -Path "apiBaseUrl").TrimEnd("/")
$healthEndpoint = Get-ConfigValue -Node $config -Path "healthEndpoint"
$readinessEndpoint = Get-ConfigValue -Node $config -Path "readinessEndpoint"
$maxUnhealthyServices = [int](Get-ConfigValue -Node $config -Path "rollbackThresholds.maxUnhealthyServices")
$maxDegradedServices = [int](Get-ConfigValue -Node $config -Path "rollbackThresholds.maxDegradedServices")
$maxP95LatencyMs = [double](Get-ConfigValue -Node $config -Path "rollbackThresholds.maxP95LatencyMs")
$maxErrorRatePercent = [double](Get-ConfigValue -Node $config -Path "rollbackThresholds.maxErrorRatePercent")
$maxAuthDenialsPercent = [double](Get-ConfigValue -Node $config -Path "rollbackThresholds.maxAuthDenialsPercent")
$maxDataConsistencyDelta = [int](Get-ConfigValue -Node $config -Path "rollbackThresholds.maxDataConsistencyDelta")

$checkpointStage = Get-ConfigValue -Node $config -Path "checkpoint.stage"
$checkpointName = Get-ConfigValue -Node $config -Path "name"
$environment = Get-ConfigValue -Node $config -Path "environment"

$p95LatencyMs = [double](Get-ConfigValue -Node $config -Path "operationalSignals.p95LatencyMs")
$errorRatePercent = [double](Get-ConfigValue -Node $config -Path "operationalSignals.errorRatePercent")
$authDenialsPercent = [double](Get-ConfigValue -Node $config -Path "operationalSignals.authDenialsPercent")
$dataConsistencyDelta = [int](Get-ConfigValue -Node $config -Path "operationalSignals.dataConsistencyDelta")

$statusUri = "$apiBaseUrl$healthEndpoint"
$readyUri = "$apiBaseUrl$readinessEndpoint"

Write-Host ""
Write-Host "Rollout checkpoint: $checkpointName"
Write-Host "Environment: $environment"
Write-Host "Stage: $checkpointStage"
Write-Host "API: $apiBaseUrl"
Write-Host ""

$statusResponse = Invoke-RestMethod -Uri $statusUri -Method Get -TimeoutSec $TimeoutSeconds
$readinessResponse = Invoke-RestMethod -Uri $readyUri -Method Get -TimeoutSec $TimeoutSeconds

$services = @($statusResponse.services)
$unhealthyServices = @($services | Where-Object { $_.status -eq "unhealthy" })
$degradedServices = @($services | Where-Object { $_.status -eq "degraded" })

$statusP95LatencyMs = 0.0
if ($services.Count -gt 0) {
    $orderedLatencies = $services.ResponseTimeMs | Sort-Object
    $p95Index = [Math]::Floor(($orderedLatencies.Count - 1) * 0.95)
    $statusP95LatencyMs = [double]$orderedLatencies[$p95Index]
}

$evaluations = @(
    @{
        Name = "Readiness"
        Current = $readinessResponse.status
        Allowed = "healthy"
        Passed = ($readinessResponse.status -eq "healthy")
    },
    @{
        Name = "Unhealthy services"
        Current = $unhealthyServices.Count
        Allowed = "<= $maxUnhealthyServices"
        Passed = ($unhealthyServices.Count -le $maxUnhealthyServices)
    },
    @{
        Name = "Degraded services"
        Current = $degradedServices.Count
        Allowed = "<= $maxDegradedServices"
        Passed = ($degradedServices.Count -le $maxDegradedServices)
    },
    @{
        Name = "Health endpoint P95 latency (ms)"
        Current = [Math]::Round($statusP95LatencyMs, 2)
        Allowed = "<= $maxP95LatencyMs"
        Passed = ($statusP95LatencyMs -le $maxP95LatencyMs)
    },
    @{
        Name = "Operational P95 latency (ms)"
        Current = [Math]::Round($p95LatencyMs, 2)
        Allowed = "<= $maxP95LatencyMs"
        Passed = ($p95LatencyMs -le $maxP95LatencyMs)
    },
    @{
        Name = "Operational error rate (%)"
        Current = [Math]::Round($errorRatePercent, 2)
        Allowed = "<= $maxErrorRatePercent"
        Passed = ($errorRatePercent -le $maxErrorRatePercent)
    },
    @{
        Name = "Operational auth denials (%)"
        Current = [Math]::Round($authDenialsPercent, 2)
        Allowed = "<= $maxAuthDenialsPercent"
        Passed = ($authDenialsPercent -le $maxAuthDenialsPercent)
    },
    @{
        Name = "Data consistency delta"
        Current = $dataConsistencyDelta
        Allowed = "<= $maxDataConsistencyDelta"
        Passed = ($dataConsistencyDelta -le $maxDataConsistencyDelta)
    }
)

Write-Host "Checkpoint evaluation:"
foreach ($check in $evaluations) {
    $statusLabel = if ($check.Passed) { "PASS" } else { "FAIL" }
    Write-Host ("  [{0}] {1} | current={2} | allowed={3}" -f $statusLabel, $check.Name, $check.Current, $check.Allowed)
}

if ($unhealthyServices.Count -gt 0) {
    Write-Host ""
    Write-Host "Unhealthy services:"
    foreach ($service in $unhealthyServices) {
        Write-Host ("  - {0} ({1})" -f $service.Name, $service.Message)
    }
}

$allPassed = @($evaluations | Where-Object { -not $_.Passed }).Count -eq 0

Write-Host ""
if ($allPassed) {
    Write-Host "Decision: PROCEED"
    exit 0
}

Write-Host "Decision: ROLLBACK"
exit 1
