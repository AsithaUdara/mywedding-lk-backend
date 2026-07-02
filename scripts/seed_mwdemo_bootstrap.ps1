# Bootstraps admin@mw.com into MyWeddingDbDemo (Firebase UID must match SQL Users.Id).
# Requires: API at http://localhost:5141
param(
    [string]$BaseUrl = "http://localhost:5141",
    [string]$ApiKey = "AIzaSyAu-Z0ZUAR2fQsLspGkmBbmhEEWrjsLtdc",
    [string]$BootstrapSecret = "mywedding-local-bootstrap-dev-only",
    [string]$AdminEmail = "admin@mw.com",
    [string]$AdminPassword = "admin@mw.com",
    [string]$ExpectedAdminUid = "zHvTgDy7CZVDYtyUTHNatugwwg82"
)

$ErrorActionPreference = "Stop"

Write-Host "`n=== MyWeddingDbDemo Admin Bootstrap ===" -ForegroundColor Cyan

try {
    Invoke-RestMethod -Method Get -Uri "$BaseUrl/swagger/index.html" -TimeoutSec 5 | Out-Null
}
catch {
    throw "API not reachable at $BaseUrl. Start the API first: cd backend\Presentation\MyWedding.API; dotnet run"
}

$signInBody = @{
    email = $AdminEmail
    password = $AdminPassword
    returnSecureToken = $true
} | ConvertTo-Json

Write-Host "Signing in Firebase: $AdminEmail" -ForegroundColor Cyan
$auth = Invoke-RestMethod -Method Post `
    -Uri "https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key=$ApiKey" `
    -ContentType "application/json" -Body $signInBody

if ($auth.localId -ne $ExpectedAdminUid) {
    throw "Admin Firebase UID mismatch. Expected $ExpectedAdminUid but got $($auth.localId). Aborting."
}
Write-Host "  UID verified: $($auth.localId)" -ForegroundColor Green

Write-Host "Setting admin role via bootstrap endpoint..." -ForegroundColor Cyan
Invoke-RestMethod -Method Post -Uri "$BaseUrl/api/admin/bootstrap/set-role" `
    -ContentType "application/json" `
    -Headers @{ "X-Bootstrap-Secret" = $BootstrapSecret } `
    -Body (@{ userId = $auth.localId; role = "admin" } | ConvertTo-Json) | Out-Null

Write-Host "Syncing admin user to SQL..." -ForegroundColor Cyan
Invoke-RestMethod -Method Post -Uri "$BaseUrl/api/auth/sync-user" `
    -Headers @{ Authorization = "Bearer $($auth.idToken)" } `
    -ContentType "application/json" -Body "{}" | Out-Null

Write-Host "`n=== Admin bootstrap complete ===" -ForegroundColor Green
Write-Host @"
Admin ready in MyWeddingDbDemo:
  Email:    $AdminEmail
  Password: $AdminPassword
  UID:      $($auth.localId)

Next: powershell -ExecutionPolicy Bypass -File backend\scripts\seed_mwdemo_full.ps1
Sign out and sign in once at http://localhost:3000/admin/dashboard after seeding.
"@ -ForegroundColor Cyan
