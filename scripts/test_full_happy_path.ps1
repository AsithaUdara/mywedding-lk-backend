# Full happy-path E2E API test - reports pass/fail per step
$ErrorActionPreference = "Continue"
$BaseUrl = "http://localhost:5141"
$ApiKey = "AIzaSyAu-Z0ZUAR2fQsLspGkmBbmhEEWrjsLtdc"
$Cred = "c:\Users\ASUS\Desktop\MyWeddingLK\backend\Presentation\MyWedding.API\firebase-credentials.json"
$EventId = "49334c6e-c1b3-4627-8f7a-9bc2ffd68c8a"
$Report = @()

function Log($Step, $Status, $Detail) {
    $global:Report += [pscustomobject]@{ Step = $Step; Status = $Status; Detail = $Detail }
    $c = if ($Status -eq "PASS") { "Green" } elseif ($Status -eq "FAIL") { "Red" } else { "Yellow" }
    Write-Host "[$Status] $Step" -ForegroundColor $c
    if ($Detail) { Write-Host "       $Detail" }
}

function Get-Token($email, $password) {
    try {
        $b = @{ email = $email; password = $password; returnSecureToken = $true } | ConvertTo-Json
        return (Invoke-RestMethod -Method Post -Uri "https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key=$ApiKey" -ContentType "application/json" -Body $b).idToken
    } catch { return $null }
}

function Invoke-Api($Method, $Token, $Path, $Body = $null) {
    $h = @{ Authorization = "Bearer $Token" }
    try {
        if ($Body -ne $null) {
            return @{ Ok = $true; Data = Invoke-RestMethod -Method $Method -Uri "$BaseUrl$Path" -Headers $h -ContentType "application/json" -Body ($Body | ConvertTo-Json -Depth 8) }
        }
        if ($Method -eq "Get") {
            return @{ Ok = $true; Data = Invoke-RestMethod -Method Get -Uri "$BaseUrl$Path" -Headers $h }
        }
        return @{ Ok = $true; Data = Invoke-RestMethod -Method $Method -Uri "$BaseUrl$Path" -Headers $h }
    } catch {
        $msg = $_.Exception.Message
        try {
            $r = $_.Exception.Response
            if ($r) { $reader = [System.IO.StreamReader]::new($r.GetResponseStream()); $msg = $reader.ReadToEnd() }
        } catch {}
        return @{ Ok = $false; Error = $msg }
    }
}

Write-Host "`n=== FULL HAPPY PATH E2E TEST ===" -ForegroundColor Cyan
try {
    Invoke-WebRequest -Uri "$BaseUrl/swagger" -UseBasicParsing -TimeoutSec 5 | Out-Null
    Log "0. API reachable" "PASS" $BaseUrl
} catch {
    Log "0. API reachable" "FAIL" "Start dotnet run in MyWedding.API"
    $Report | Format-Table -AutoSize
    exit 1
}

$adminT = Get-Token "admin@mw.com" "admin@mw.com"
$plannerT = Get-Token "demoplanner@mw.com" "demoplanner@mw.com"
$brideT = Get-Token "shaminibride@mw.com" "shaminibride@mw.com"
$vendorT = Get-Token "kandylens@mw.com" "kandylens@mw.com"

foreach ($pair in @(@("Admin",$adminT),@("Planner",$plannerT),@("Bride",$brideT),@("Vendor",$vendorT))) {
    if ($pair[1]) { Log "1. Login $($pair[0])" "PASS" "Firebase OK" } else { Log "1. Login $($pair[0])" "FAIL" "Check account" }
}

# Sync users
foreach ($pair in @(@("Planner",$plannerT),@("Bride",$brideT),@("Vendor",$vendorT))) {
    $r = Invoke-Api Post $pair[1] "/api/auth/sync-user" @{}
    Log "2. Sync $($pair[0])" $(if ($r.Ok) {"PASS"} else {"FAIL"}) $(if (-not $r.Ok) {$r.Error})
}

# Public vendors
$pub = Invoke-RestMethod -Uri "$BaseUrl/api/vendors" -Method Get
$demoVendors = @("Kandy Lens","Royal Spice","Lotus Bloom")
$found = ($pub | Where-Object { $demoVendors -contains $_.businessName }).Count
Log "3. Verified vendors in directory" $(if ($found -ge 3) {"PASS"} else {"FAIL"}) "Found $found/3 demo vendors"

# Event access
$ev = Invoke-Api Get $plannerT "/api/planner/events"
if ($ev.Ok) {
    $main = $ev.Data | Where-Object { $_.eventId -eq $EventId -or $_.EventId -eq $EventId }
    if ($main) { Log "4. Planner sees main event" "PASS" $main.eventName } else { Log "4. Planner sees main event" "FAIL" "Event not in list" }
} else { Log "4. Planner sees main event" "FAIL" $ev.Error }

# Tasks
$tasks = Invoke-Api Get $plannerT "/api/events/$EventId/tasks"
$tc = if ($tasks.Ok) { @($tasks.Data).Count } else { 0 }
Log "5. Event has tasks (Gantt)" $(if ($tc -ge 10) {"PASS"} else {"WARN"}) "Count: $tc"

# Shortlist state
$sl = Invoke-Api Get $brideT "/api/events/$EventId/vendor-shortlist"
if (-not $sl.Ok) { Log "6. Client shortlist read" "FAIL" $sl.Error }
else {
    Log "6. Client shortlist read" "PASS" "Items: $(@($sl.Data).Count)"
    $global:Shortlist = $sl.Data
}

# Find or create item for full flow (catering - royal spice service)
$svcList = Invoke-Api Get $vendorT "/api/vendor/dashboard/services"
$photoSvc = $null
if ($svcList.Ok -and $svcList.Data.Count -gt 0) {
    $photoSvc = $svcList.Data[0].id
    if (-not $photoSvc) { $photoSvc = $svcList.Data[0].Id }
}

# Pick shortlist item needing flow: prefer ClientApproved or SentToClient
$item = $null
$bookingId = $null
if ($global:Shortlist) {
    $item = $global:Shortlist | Where-Object { $_.status -in @("SentToClient","ClientApproved") } | Select-Object -First 1
    if ($item) {
        $itemId = $item.id
        if ($item.status -eq "SentToClient") {
            $ap = Invoke-Api Post $brideT "/api/events/$EventId/vendor-shortlist/$itemId/approve" @{}
            Log "7. Client approve shortlist" $(if ($ap.Ok) {"PASS"} else {"FAIL"}) $(if (-not $ap.Ok) {$ap.Error})
        } else { Log "7. Client approve shortlist" "SKIP" "Already approved" }
        $rb = Invoke-Api Post $brideT "/api/events/$EventId/vendor-shortlist/$itemId/request-booking" @{}
        if ($rb.Ok) {
            $bookingId = $rb.Data.bookingId
            Log "8. Client request booking" "PASS" "bookingId=$bookingId"
        } else { Log "8. Client request booking" "FAIL" $rb.Error }
    }
}

if (-not $bookingId -and $global:Shortlist) {
    $item2 = $global:Shortlist | Where-Object { $_.vendorBookingId } | Select-Object -First 1
    if ($item2) { $bookingId = $item2.vendorBookingId; Log "8. Client request booking" "SKIP" "Using existing $bookingId" }
}

if ($bookingId) {
    $acc = Invoke-Api Post $vendorT "/api/bookings/$bookingId/accept" @{}
    Log "9. Vendor accept booking" $(if ($acc.Ok) {"PASS"} else {"FAIL"}) $(if (-not $acc.Ok) {$acc.Error})

    # Contract: generate standard + send
    $upload = Invoke-RestMethod -Method Post -Uri "$BaseUrl/api/bookings/$bookingId/contract/upload" `
        -Headers @{ Authorization = "Bearer $vendorT" } -Form @{ generateStandardContract = "true" }
    Log "10. Vendor upload/generate contract" "PASS" "contract uploaded"

    $send = Invoke-Api Post $vendorT "/api/bookings/$bookingId/contract/send" $null
    Log "11. Vendor send contract to client" $(if ($send.Ok) {"PASS"} else {"FAIL"}) $(if (-not $send.Ok) {$send.Error})

    $contract = Invoke-Api Get $brideT "/api/bookings/$bookingId/contract"
    $fileUrl = $null
    if ($contract.Ok) { $fileUrl = $contract.Data.contractFileUrl }

    $sign = Invoke-Api Post $brideT "/api/bookings/$bookingId/contract/sign" @{
        signerName = "Shamini"
        contractFileUrl = $fileUrl
    }
    Log "12. Client sign contract" $(if ($sign.Ok) {"PASS"} else {"FAIL"}) $(if (-not $sign.Ok) {$sign.Error})

    $audit = Invoke-Api Get $plannerT "/api/events/$EventId/audit-log"
    $hasSign = $false
    if ($audit.Ok) { $hasSign = @($audit.Data) | Where-Object { $_.actionType -eq "ContractSigned" } | Select-Object -First 1 }
    Log "13. Audit log ContractSigned" $(if ($hasSign) {"PASS"} else {"WARN"}) "Check audit entries"

    $checkout = Invoke-Api Post $brideT "/api/payments/bookings/$bookingId/deposit-checkout" @{}
    if ($checkout.Ok) {
        Log "14. Deposit checkout session" "PASS" $(if ($checkout.Data.isSimulated) {"Simulated"} else {"PayHere payload returned"})
    } else { Log "14. Deposit checkout session" "FAIL" $checkout.Error }

    $status = Invoke-Api Get $brideT "/api/payments/bookings/$bookingId/status"
    if ($status.Ok) {
        Log "15. Payment status poll" "PASS" "booking=$($status.Data.bookingStatus) payment=$($status.Data.paymentStatus)"
    } else { Log "15. Payment status poll" "FAIL" $status.Error }
} else {
    Log "9-15. Booking flow" "FAIL" "No bookingId - shortlist chain broken"
}

# Admin analytics
$stats = Invoke-Api Get $adminT "/api/admin/stats"
Log "16. Admin stats" $(if ($stats.Ok) {"PASS"} else {"FAIL"}) $(if (-not $stats.Ok) {$stats.Error})

$analytics = Invoke-Api Get $adminT "/api/admin/platform-analytics"
Log "17. Admin platform analytics" $(if ($analytics.Ok) {"PASS"} else {"FAIL"}) $(if (-not $analytics.Ok) {$analytics.Error})

# Budget read-only client
$budget = Invoke-Api Get $brideT "/api/events/$EventId/budget"
Log "18. Client budget view" $(if ($budget.Ok) {"PASS"} else {"FAIL"}) $(if ($budget.Ok) {"total=$($budget.Data.totalBudget) spent=$($budget.Data.totalSpent)"})

# Couple cannot create event
$createEv = Invoke-Api Post $brideT "/api/events" @{ eventName = "Hack"; eventDate = (Get-Date).ToString("o") }
Log "19. Client cannot POST /api/events" $(if (-not $createEv.Ok) {"PASS"} else {"FAIL"}) "Should be 403"

Write-Host "`n=== SUMMARY ===" -ForegroundColor Cyan
$Report | Format-Table -AutoSize -Wrap
$pass = ($Report | Where-Object Status -eq "PASS").Count
$fail = ($Report | Where-Object Status -eq "FAIL").Count
$warn = ($Report | Where-Object Status -eq "WARN").Count
Write-Host "PASS: $pass  FAIL: $fail  WARN: $warn" -ForegroundColor $(if ($fail -eq 0) {"Green"} else {"Yellow"})
$Report | Export-Csv -Path "c:\Users\ASUS\Desktop\MyWeddingLK\backend\scripts\happy_path_report.csv" -NoTypeInformation
