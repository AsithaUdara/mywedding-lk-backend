# Clean and Test Script for MyWedding API
# Usage: Right-click -> Run with PowerShell, or:
#   powershell -ExecutionPolicy Bypass -File .\backend\scripts\clean_and_test.ps1

$ErrorActionPreference = "Stop"

function Get-ApiUrl {
  param(
    [string]$ApiProjectDir
  )
  $launchSettings = Join-Path $ApiProjectDir "Properties/launchSettings.json"
  if (Test-Path $launchSettings) {
    $json = Get-Content $launchSettings -Raw | ConvertFrom-Json
    $profiles = $json.profiles
    foreach ($key in $profiles.PSObject.Properties.Name) {
      $url = $profiles.$key.applicationUrl
      if ($url) { return $url.Split(';')[0] } # Prefer first URL if multiple
    }
  }
  return "http://localhost:5141" # Fallback: observed default in logs
}

function Wait-ApiReady {
  param(
    [string]$BaseUrl,
    [int]$TimeoutSec = 60
  )
  $Deadline = (Get-Date).AddSeconds($TimeoutSec)
  while ((Get-Date) -lt $Deadline) {
    try {
      Invoke-WebRequest -Uri "$BaseUrl/" -UseBasicParsing -TimeoutSec 3 | Out-Null
      Write-Host "API responded at $BaseUrl" -ForegroundColor Green
      return
    } catch {
      Start-Sleep -Milliseconds 800
    }
  }
  throw "API did not become ready within $TimeoutSec seconds at $BaseUrl"
}

function Get-ConnectionString {
  param(
    [string]$ApiProjectDir
  )
  $settings = Join-Path $ApiProjectDir "appsettings.Development.json"
  if (-not (Test-Path $settings)) { throw "Missing appsettings.Development.json at $ApiProjectDir" }
  $json = Get-Content $settings -Raw | ConvertFrom-Json
  $conn = $json.ConnectionStrings.DefaultConnection
  if (-not $conn) { throw "DefaultConnection not found in appsettings.Development.json" }
  return $conn
}

function Exec-Sql {
  param(
    [string]$ConnectionString,
    [string]$Sql
  )
  Add-Type -AssemblyName System.Data
  $conn = New-Object System.Data.SqlClient.SqlConnection $ConnectionString
  $conn.Open()
  try {
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = $Sql
    $cmd.CommandTimeout = 60
    $cmd.ExecuteNonQuery() | Out-Null
  } finally {
    $conn.Close()
  }
}

function Query-SqlScalar {
  param(
    [string]$ConnectionString,
    [string]$Sql
  )
  Add-Type -AssemblyName System.Data
  $conn = New-Object System.Data.SqlClient.SqlConnection $ConnectionString
  $conn.Open()
  try {
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = $Sql
    $cmd.CommandTimeout = 60
    return $cmd.ExecuteScalar()
  } finally {
    $conn.Close()
  }
}

function Invoke-Api {
  param(
    [string]$Method,
    [string]$Url,
    [hashtable]$Headers,
    [object]$Body = $null
  )
  if ($Body -ne $null) {
    return Invoke-RestMethod -Method $Method -Uri $Url -Headers $Headers -ContentType "application/json" -Body ($Body | ConvertTo-Json -Depth 5)
  } else {
    return Invoke-RestMethod -Method $Method -Uri $Url -Headers $Headers
  }
}

# Paths
$SolutionPath = "c:\Users\ASUS\Desktop\MyWeddingLK\backend\MyWedding.sln"
$ApiDir = "c:\Users\ASUS\Desktop\MyWeddingLK\backend\src\Presentation\MyWedding.API"
$BaseUrl = Get-ApiUrl -ApiProjectDir $ApiDir

Write-Host "Building solution..." -ForegroundColor Cyan
& dotnet build $SolutionPath -c Debug | Out-Null
Write-Host "Build complete." -ForegroundColor Green

Write-Host "Starting API in background..." -ForegroundColor Cyan
$apiProc = Start-Process -FilePath "dotnet" -ArgumentList "run" -WorkingDirectory $ApiDir -NoNewWindow -PassThru
try {
  Wait-ApiReady -BaseUrl $BaseUrl -TimeoutSec 90
} catch {
  Write-Warning $_
  Write-Warning "Attempting to continue; ensure API is reachable at $BaseUrl"
}

# Database cleanup
Write-Host "Cleaning database (EventOrganizers -> WeddingEvents -> Users)..." -ForegroundColor Cyan
$connectionString = Get-ConnectionString -ApiProjectDir $ApiDir
$cleanupSql = @"
DELETE FROM [dbo].[EventOrganizers];
DELETE FROM [dbo].[WeddingEvents];
DELETE FROM [dbo].[Users];
"@
Exec-Sql -ConnectionString $connectionString -Sql $cleanupSql
Write-Host "Database cleaned." -ForegroundColor Green

# Tokens and inputs
Write-Host "Paste Firebase ID tokens when prompted (Alice then Bob)." -ForegroundColor Yellow
$aliceToken = Read-Host "Alice Owner Firebase ID Token"
$bobToken   = Read-Host "Bob Invitee Firebase ID Token"

# Sync users
Write-Host "Syncing users via /api/auth/sync-user..." -ForegroundColor Cyan
$aliceHeaders = @{ Authorization = "Bearer $aliceToken" }
$bobHeaders   = @{ Authorization = "Bearer $bobToken" }
try { Invoke-Api -Method Post -Url "$BaseUrl/api/auth/sync-user" -Headers $aliceHeaders -Body @{} | Out-Null } catch { Write-Warning "Alice sync failed: $_" }
try { Invoke-Api -Method Post -Url "$BaseUrl/api/auth/sync-user" -Headers $bobHeaders   -Body @{} | Out-Null } catch { Write-Warning "Bob sync failed: $_" }
Write-Host "Sync attempts completed." -ForegroundColor Green

# Create event (manual step)
Write-Host "Create the event as Alice (manual), then paste the eventId." -ForegroundColor Yellow
$eventId = Read-Host "EventId (GUID)"
if (-not [Guid]::TryParse($eventId, [ref]([Guid]::Empty))) {
  Write-Warning "Invalid EventId GUID; script will continue but invite will likely fail."
}

# Invite Bob
Write-Host "Inviting Bob to the event..." -ForegroundColor Cyan
$inviteBody = @{ inviteeEmail = "bob.invitee@mywedding.com"; role = "Editor"; permissionLevel = "Editor" }
try {
  $inviteResp = Invoke-Api -Method Post -Url "$BaseUrl/api/events/$eventId/organizers" -Headers $aliceHeaders -Body $inviteBody
  Write-Host "Invite response:" -ForegroundColor Green
  $inviteResp | ConvertTo-Json -Depth 5
} catch {
  Write-Warning "Invite failed: $_"
}

# Verify DB rows
$rows = Query-SqlScalar -ConnectionString $connectionString -Sql "SELECT COUNT(*) FROM [dbo].[EventOrganizers] WHERE [EventId] = '$eventId'"
Write-Host ("Organizer rows for event {0}: {1}" -f $eventId, $rows) -ForegroundColor Green

Write-Host "Done. Press Enter to stop the API process." -ForegroundColor Cyan
[void]([Console]::ReadLine())
try { if ($apiProc -and !$apiProc.HasExited) { $apiProc.CloseMainWindow() | Out-Null; Start-Sleep 1; $apiProc.Kill() | Out-Null } } catch {}
