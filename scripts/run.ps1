param(
  # Folder that contains host.json for the Azure Function App
  [string]$ApiFunctionDir = "Z:\repos\chronicis\src\Chronicis.Api",

  # Web client project (.NET)
  [string]$WebProject = "Z:\repos\chronicis\src\Chronicis.Client\Chronicis.Client.csproj",

  # Health check endpoint for the Functions host
  # Change this to whatever your API exposes. If you do not have one, consider adding a simple HTTP trigger like /api/health.
  [string]$ApiHealthUrl = "http://localhost:7071/api/health",

  # Web URL to open in browser (set to your actual dev URL)
  [string]$WebUrl = "https://localhost:5001",

  # Max seconds to wait for API health before starting web
  [int]$ApiHealthTimeoutSeconds = 60,

  # Poll interval for health checks and log pumping
  [int]$PollMilliseconds = 250,

  # If set, does not auto-open browser and keeps output minimal for agent runs
  [switch]$Headless
)

$ErrorActionPreference = "Stop"

function Require-Command {
  param([string]$Name)
  $cmd = Get-Command $Name -ErrorAction SilentlyContinue
  if (-not $cmd) {
    throw "Required command '$Name' not found on PATH. If you can run it manually, ensure the same shell environment is used here."
  }
}

function Ensure-File {
  param([string]$Path)
  if (-not (Test-Path $Path)) {
    New-Item -ItemType File -Path $Path | Out-Null
  }
}

function Write-Prefixed {
  param(
    [string]$Prefix,
    [string]$Line,
    [ConsoleColor]$Color = [ConsoleColor]::Gray
  )
  if ([string]::IsNullOrWhiteSpace($Line)) { return }
  Write-Host ("[{0}] {1}" -f $Prefix, $Line) -ForegroundColor $Color
}

function Wait-For-HttpReadyWithCurl {
  param(
    [string]$Url,
    [int]$TimeoutSeconds = 60,
    [int]$PollMilliseconds = 250
  )

  $start = Get-Date
  $spinner = @('|','/','-','\')
  $spinIndex = 0
  $lastCode = ""

  while ($true) {
    $elapsed = (Get-Date) - $start
    if ($elapsed.TotalSeconds -ge $TimeoutSeconds) {
      Write-Host ""
      if ($lastCode) {
        Write-Host "Last HTTP code seen: $lastCode" -ForegroundColor Yellow
      }
      return $false
    }

    # curl.exe returns a status code even if it gets a response with non-200
    # -s silent, -o NUL discard body, -m max time seconds, -w write out format
    $code = & curl.exe -s -o NUL -m 2 -w "%{http_code}" $Url 2>$null

    if ($code -and $code -ne "000") {
      Write-Host "`rAPI health: ready (HTTP $code).                                     "
      return $true
    }

    $lastCode = $code
    $remaining = [Math]::Max(0, $TimeoutSeconds - [int]$elapsed.TotalSeconds)
    $ch = $spinner[$spinIndex % $spinner.Length]
    $spinIndex++

    Write-Host -NoNewline "`rAPI health: waiting $Url  (${remaining}s remaining)  $ch"
    Start-Sleep -Milliseconds $PollMilliseconds
  }
}
function Wait-For-WebReadyWithCurl {
  param(
    [string]$Url,
    [int]$TimeoutSeconds = 60,
    [int]$PollMilliseconds = 250
  )

  $start = Get-Date
  $spinner = @('|','/','-','\')
  $spinIndex = 0

  while ($true) {
    $elapsed = (Get-Date) - $start
    if ($elapsed.TotalSeconds -ge $TimeoutSeconds) {
      Write-Host ""
      return $false
    }

    $code = & curl.exe -s -o NUL -m 2 -w "%{http_code}" $Url 2>$null

    if ($code -and $code -ne "000") {
      Write-Host "`rWeb: ready (HTTP $code).                                     "
      return $true
    }

    $remaining = [Math]::Max(0, $TimeoutSeconds - [int]$elapsed.TotalSeconds)
    $ch = $spinner[$spinIndex % $spinner.Length]
    $spinIndex++

    Write-Host -NoNewline "`rWeb: waiting $Url  (${remaining}s remaining)  $ch"
    Start-Sleep -Milliseconds $PollMilliseconds
  }
}


function Read-NewLinesFromFile {
  param(
    [string]$Path,
    [ref]$Position
  )

  if (-not (Test-Path $Path)) { return @() }

  $fs = $null
  $sr = $null
  try {
    $fs = [System.IO.File]::Open($Path, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, [System.IO.FileShare]::ReadWrite)
    if ($Position.Value -gt $fs.Length) {
      # Log rotated or truncated
      $Position.Value = 0
    }

    $fs.Seek($Position.Value, [System.IO.SeekOrigin]::Begin) | Out-Null
    $sr = New-Object System.IO.StreamReader($fs)

    $text = $sr.ReadToEnd()
    $Position.Value = $fs.Position

    if ([string]::IsNullOrEmpty($text)) { return @() }

    # Normalize newlines and split
    $text = $text -replace "`r`n", "`n"
    $text = $text -replace "`r", "`n"
    $lines = $text.Split("`n")

    # Drop last empty fragment if file ended with newline
    if ($lines.Count -gt 0 -and $lines[-1] -eq "") {
      $lines = $lines[0..($lines.Count - 2)]
    }

    return $lines
  }
  finally {
    if ($sr) { $sr.Dispose() }
    if ($fs) { $fs.Dispose() }
  }
}

# ---- Preconditions ----
Require-Command "dotnet"
Require-Command "func"

# ---- Logs (stdout/stderr must be different files) ----
$apiOut = Join-Path $PSScriptRoot "logs\api.out.log"
$apiErr = Join-Path $PSScriptRoot "logs\api.err.log"
$webOut = Join-Path $PSScriptRoot "logs\web.out.log"
$webErr = Join-Path $PSScriptRoot "logs\web.err.log"

Remove-Item $apiOut, $apiErr, $webOut, $webErr -ErrorAction SilentlyContinue
Ensure-File $apiOut
Ensure-File $apiErr
Ensure-File $webOut
Ensure-File $webErr

# ---- Start API (Azure Functions) ----
Write-Host "Starting API (Azure Functions)..."
Write-Host "  Directory: $ApiFunctionDir"
Write-Host "  Command: func start"

$apiProc = Start-Process -FilePath "func" `
  -ArgumentList @("start") `
  -WorkingDirectory $ApiFunctionDir `
  -RedirectStandardOutput $apiOut `
  -RedirectStandardError  $apiErr `
  -PassThru `
  -NoNewWindow

# ---- Wait for API to become healthy ----
if (-not $Headless) {
  Write-Host ""
}

$apiReady = Wait-For-HttpReadyWithCurl -Url $ApiHealthUrl -TimeoutSeconds $ApiHealthTimeoutSeconds -PollMilliseconds $PollMilliseconds
if (-not $apiReady) {
  Write-Host ""
  Write-Host "API did not become healthy within $ApiHealthTimeoutSeconds seconds." -ForegroundColor Yellow
  Write-Host "Check logs:" -ForegroundColor Yellow
  Write-Host "  $apiErr" -ForegroundColor Yellow
  Write-Host "  $apiOut" -ForegroundColor Yellow
  throw "Aborting before starting web."
}

# ---- Start Web ----
Write-Host "Starting Web..."
Write-Host "  Project: $WebProject"
Write-Host "  Command: dotnet run --project $WebProject"

$webProc = Start-Process -FilePath "dotnet" `
  -ArgumentList @("run", "--project", $WebProject) `
  -RedirectStandardOutput $webOut `
  -RedirectStandardError  $webErr `
  -PassThru `
  -NoNewWindow

# ---- Auto-open browser (unless headless) ----
if (-not $Headless) {
  Write-Host ""
  Write-Host "Opening browser: $WebUrl"
  try { Start-Process $WebUrl | Out-Null } catch {}
}

Write-Host ""
Write-Host "API PID: $($apiProc.Id)"
Write-Host "WEB PID: $($webProc.Id)"
Write-Host ""
Write-Host "Logs:"
Write-Host "  API stdout: $apiOut"
Write-Host "  API stderr: $apiErr"
Write-Host "  WEB stdout: $webOut"
Write-Host "  WEB stderr: $webErr"
Write-Host ""
Write-Host "Live logs below. Press Ctrl+C to stop both."
Write-Host ""

# ---- Live log pump with colors (includes stderr in this terminal) ----
$posApiOut = 0L
$posApiErr = 0L
$posWebOut = 0L
$posWebErr = 0L

try {
  while ($true) {
    # If either process exits, stop the other
    if ($apiProc.HasExited) {
      Write-Host ""
      Write-Host "API exited (code $($apiProc.ExitCode)). Stopping Web..." -ForegroundColor Yellow
      if (-not $webProc.HasExited) {
        try { Stop-Process -Id $webProc.Id -Force } catch {}
      }
      break
    }

    if ($webProc.HasExited) {
      Write-Host ""
      Write-Host "Web exited (code $($webProc.ExitCode)). Stopping API..." -ForegroundColor Yellow
      if (-not $apiProc.HasExited) {
        try { Stop-Process -Id $apiProc.Id -Force } catch {}
      }
      break
    }

    # Pump new lines from each log file
    foreach ($line in (Read-NewLinesFromFile -Path $apiOut -Position ([ref]$posApiOut))) {
      #Write-Prefixed -Prefix "API" -Line $line -Color ([ConsoleColor]::Cyan)
    }
    foreach ($line in (Read-NewLinesFromFile -Path $apiErr -Position ([ref]$posApiErr))) {
      Write-Prefixed -Prefix "API-ERR" -Line $line -Color ([ConsoleColor]::Red)
    }
    foreach ($line in (Read-NewLinesFromFile -Path $webOut -Position ([ref]$posWebOut))) {
      #Write-Prefixed -Prefix "WEB" -Line $line -Color ([ConsoleColor]::Green)
    }
    foreach ($line in (Read-NewLinesFromFile -Path $webErr -Position ([ref]$posWebErr))) {
      Write-Prefixed -Prefix "WEB-ERR" -Line $line -Color ([ConsoleColor]::Orange)
    }

    Start-Sleep -Milliseconds $PollMilliseconds
  }
}
finally {
  foreach ($p in @($apiProc, $webProc)) {
    if ($null -ne $p -and -not $p.HasExited) {
      try { Stop-Process -Id $p.Id -Force } catch {}
    }
  }

  Write-Host ""
  Write-Host "========== Last 80 lines: API stderr ==========" -ForegroundColor Yellow
  if (Test-Path $apiErr) { Get-Content $apiErr -Tail 80 } else { Write-Host "(api.err.log not found)" }

  Write-Host ""
  Write-Host "========== Last 80 lines: WEB stderr ==========" -ForegroundColor Yellow
  if (Test-Path $webErr) { Get-Content $webErr -Tail 80 } else { Write-Host "(web.err.log not found)" }

  Write-Host ""
  Write-Host "Done."
}
