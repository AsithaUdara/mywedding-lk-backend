param(
  [Parameter(Mandatory=$true)] [string]$AliceToken,
  [Parameter(Mandatory=$true)] [string]$BobToken,
  [Parameter(Mandatory=$false)] [string]$EventId,
  [Parameter(Mandatory=$false)] [switch]$AutoCreateEvent,
  [Parameter(Mandatory=$false)] [string]$EventName = "Test Wedding",
  [Parameter(Mandatory=$false)] [string]$EventDate = (Get-Date).AddDays(7).ToString("o"),
  [Parameter(Mandatory=$false)] [string]$BaseUrl
)

$ErrorActionPreference = "Stop"

function Get-ApiUrl {
  param([string]$ApiProjectDir)
  $launchSettings = Join-Path $ApiProjectDir "Properties/launchSettings.json"
  if (Test-Path $launchSettings) {
    $json = Get-Content $launchSettings -Raw | ConvertFrom-Json
    $profiles = $json.profiles
    foreach ($key in $profiles.PSObject.Properties.Name) {
      $url = $profiles.$key.applicationUrl
      if ($url) { return $url.Split(';')[0] }
    }
  }
  return "http://localhost:5141"
}

function Wait-ApiReady { param([string]$Url,[int]$TimeoutSec=60)
  $deadline = (Get-Date).AddSeconds($TimeoutSec)
  while ((Get-Date) -lt $deadline) {
    try { Invoke-WebRequest -Uri "$Url/" -UseBasicParsing -TimeoutSec 3 | Out-Null; return }
    catch { Start-Sleep -Milliseconds 800 }
  }
  throw "API didn't become ready within $TimeoutSec seconds: $Url"
}

function Get-ConnectionString { param([string]$ApiProjectDir)
  $dev = Join-Path $ApiProjectDir "appsettings.Development.json"
  $base = Join-Path $ApiProjectDir "appsettings.json"
  $conn = $null
  if (Test-Path $dev) {
    try { $conn = (Get-Content $dev -Raw | ConvertFrom-Json).ConnectionStrings.DefaultConnection } catch {}
  }
  if (-not $conn -and (Test-Path $base)) {
    try { $conn = (Get-Content $base -Raw | ConvertFrom-Json).ConnectionStrings.DefaultConnection } catch {}
  }
  if (-not $conn) { throw "DefaultConnection missing in appsettings.*.json under $ApiProjectDir" }
  return $conn
}

function Exec-Sql { param([string]$Conn,[string]$Sql)
  Add-Type -AssemblyName System.Data
  $c = New-Object System.Data.SqlClient.SqlConnection $Conn
  $c.Open(); try { $cmd = $c.CreateCommand(); $cmd.CommandText = $Sql; $cmd.CommandTimeout = 60; $cmd.ExecuteNonQuery() | Out-Null }
  finally { $c.Close() }
}

function Query-SqlScalar { param([string]$Conn,[string]$Sql)
  Add-Type -AssemblyName System.Data
  $c = New-Object System.Data.SqlClient.SqlConnection $Conn
  $c.Open(); try { $cmd = $c.CreateCommand(); $cmd.CommandText = $Sql; $cmd.CommandTimeout = 60; return $cmd.ExecuteScalar() }
  finally { $c.Close() }
}

function Invoke-Json { param([string]$Method,[string]$Url,[hashtable]$Headers,[object]$Body=$null)
  if ($Body -ne $null) { return Invoke-RestMethod -Method $Method -Uri $Url -Headers $Headers -ContentType "application/json" -Body ($Body | ConvertTo-Json -Depth 5) }
  else { return Invoke-RestMethod -Method $Method -Uri $Url -Headers $Headers }
}

$SolutionPath = "c:\Users\ASUS\Desktop\MyWeddingLK\backend\MyWedding.sln"
$ApiDir = "c:\Users\ASUS\Desktop\MyWeddingLK\backend\src\Presentation\MyWedding.API"
if (-not $BaseUrl) { $BaseUrl = Get-ApiUrl -ApiProjectDir $ApiDir }

Write-Host "Stopping any running API processes..." -ForegroundColor Cyan
try { Get-Process -Name "MyWedding.API" -ErrorAction SilentlyContinue | Stop-Process -Force } catch {}
try { Get-Process -Name "dotnet" -ErrorAction SilentlyContinue | Where-Object { $_.MainWindowTitle -like '*MyWedding.API*' } | Stop-Process -Force } catch {}

Write-Host "Building solution..." -ForegroundColor Cyan
& dotnet build $SolutionPath -c Debug | Out-Null
Write-Host "Build complete." -ForegroundColor Green

Write-Host "Starting API in background..." -ForegroundColor Cyan
$apiProc = Start-Process -FilePath "dotnet" -ArgumentList "run" -WorkingDirectory $ApiDir -NoNewWindow -PassThru
try { Wait-ApiReady -Url $BaseUrl -TimeoutSec 90; Write-Host "API ready at $BaseUrl" -ForegroundColor Green }
catch { Write-Warning $_ }

# DB cleanup
Write-Host "Cleaning DB tables..." -ForegroundColor Cyan
$cs = Get-ConnectionString -ApiProjectDir $ApiDir
$cleanup = @"
DELETE FROM [dbo].[EventOrganizers];
DELETE FROM [dbo].[WeddingEvents];
DELETE FROM [dbo].[Users];
"@
Exec-Sql -Conn $cs -Sql $cleanup
Write-Host "DB cleaned." -ForegroundColor Green

# Sync Alice and Bob
$aliceHeaders = @{ Authorization = "Bearer $AliceToken" }
$bobHeaders   = @{ Authorization = "Bearer $BobToken" }
Write-Host "Syncing users via /api/auth/sync-user..." -ForegroundColor Cyan
try { Invoke-Json -Method Post -Url "$BaseUrl/api/auth/sync-user" -Headers $aliceHeaders -Body @{} | Out-Null } catch { Write-Warning "Alice sync failed: $_" }
try { Invoke-Json -Method Post -Url "$BaseUrl/api/auth/sync-user" -Headers $bobHeaders   -Body @{} | Out-Null } catch { Write-Warning "Bob sync failed: $_" }
Write-Host "Sync done." -ForegroundColor Green

# Create event optionally
if ($AutoCreateEvent) {
  Write-Host "Creating event as Alice..." -ForegroundColor Cyan
  # Send eventDate as ISO 8601 string; ASP.NET Core will parse it
  $createBody = @{ eventName = $EventName; eventDate = $EventDate }
  try {
    $createResp = Invoke-Json -Method Post -Url "$BaseUrl/api/events" -Headers $aliceHeaders -Body $createBody
    $EventId = $createResp.EventId
    Write-Host "Created event: $EventId" -ForegroundColor Green
  } catch { throw "Event creation failed: $_" }
}

if (-not $EventId) { throw "EventId is required when -AutoCreateEvent is not set." }

# Invite Bob
Write-Host "Inviting Bob to event $EventId..." -ForegroundColor Cyan
$inviteBody = @{ email = "bob.invitee@mywedding.com"; role = "Planner"; permissionLevel = "Editor" }
try {
  $inviteResp = Invoke-Json -Method Post -Url "$BaseUrl/api/events/$EventId/organizers" -Headers $aliceHeaders -Body $inviteBody
  Write-Host "Invite response:" -ForegroundColor Green
  $inviteResp | ConvertTo-Json -Depth 5
} catch {
  Write-Warning "Invite failed: $($_.Exception.Message)"
  try {
    $resp = $_.Exception.Response
    if ($resp -ne $null) {
      $reader = New-Object System.IO.StreamReader($resp.GetResponseStream())
      $body = $reader.ReadToEnd()
      Write-Host "Response body:" -ForegroundColor Yellow
      Write-Host $body
    }
  } catch {}
}

# Verify DB
$rows = Query-SqlScalar -Conn $cs -Sql "SELECT COUNT(*) FROM [dbo].[EventOrganizers] WHERE [EventId] = '$EventId'"
Write-Host ("Organizer rows for event {0}: {1}" -f $EventId, $rows) -ForegroundColor Green

# Stop API
try { if ($apiProc -and !$apiProc.HasExited) { $apiProc.CloseMainWindow() | Out-Null; Start-Sleep 1; $apiProc.Kill() | Out-Null } } catch {}
