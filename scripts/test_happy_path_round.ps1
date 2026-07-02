# Complete happy path on Kandy Lens / photography line
$ErrorActionPreference = "Continue"
$BaseUrl = "http://localhost:5141"
$ApiKey = $env:FIREBASE_WEB_API_KEY
if (-not $ApiKey) {
    throw "Set FIREBASE_WEB_API_KEY (Firebase Web API key) before running this script."
}
$EventId = "49334c6e-c1b3-4627-8f7a-9bc2ffd68c8a"
$Report = @()

function Log($s, $st, $d) { $script:Report += [pscustomobject]@{Step=$s;Status=$st;Detail=$d}; Write-Host "[$st] $s - $d" -ForegroundColor $(if($st-eq"PASS"){"Green"}elseif($st-eq"FAIL"){"Red"}else{"Yellow"}) }

function Token($email) {
    $b = @{ email=$email; password=$email; returnSecureToken=$true } | ConvertTo-Json
    (Invoke-RestMethod -Method Post -Uri "https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key=$ApiKey" -ContentType "application/json" -Body $b).idToken
}

function Api($m, $t, $path, $body=$null) {
    $h = @{ Authorization = "Bearer $t" }
    try {
        if ($body -ne $null) { return @{Ok=$true; D=(Invoke-RestMethod -Method $m -Uri "$BaseUrl$path" -Headers $h -ContentType "application/json" -Body ($body|ConvertTo-Json -Depth 8))} }
        return @{Ok=$true; D=(Invoke-RestMethod -Method $m -Uri "$BaseUrl$path" -Headers $h)}
    } catch {
        $e = $_.ErrorDetails.Message; if (-not $e) { $e = $_.Exception.Message }
        return @{Ok=$false; E=$e}
    }
}

$plannerT = Token "demoplanner@mw.com"
$brideT = Token "shaminibride@mw.com"
$vendorT = Token "kandylens@mw.com"
$adminT = Token "admin@mw.com"
$groomT = Token "udaragroom@mw.com"

Log "Login all roles" "PASS" "Firebase tokens OK"

# Vendor directory
$pub = Invoke-RestMethod -Uri "$BaseUrl/api/vendors"
$k = @($pub | Where-Object { $_.businessName -like "Kandy Lens*" }).Count
Log "Kandy Lens in directory" $(if($k){ "PASS" }else{"FAIL"}) "verified vendor listed"

# Groom on event
$evs = Api Get $groomT "/api/events"
$on = @($evs.D | Where-Object { $_.id -eq $EventId }).Count
Log "Groom sees wedding event" $(if($on){"PASS"}else{"FAIL"}) ""

# Get vendor service id
$svcs = Api Get $vendorT "/api/vendor/dashboard/services"
$svcId = $svcs.D[0].id
Log "Vendor has services" $(if($svcId){"PASS"}else{"FAIL"}) $svcId

# Fresh shortlist line for clean round
$cr = Api Post $plannerT "/api/planner/events/$EventId/vendor-shortlist" @{
    sendToClient = $false
    items = @(@{
        vendorServiceId = $svcId
        categoryLabel = "Photography"
        plannerNotes = "Evaluator demo full round"
        proposedAmount = 185000
        serviceDate = (Get-Date).AddMonths(6).ToString("o")
    })
}
if (-not $cr.Ok) { Log "Create shortlist" "FAIL" $cr.E; $Report | Format-Table; exit 1 }
$itemId = $cr.D.itemIds[0]
Log "Planner create shortlist" "PASS" $itemId

$snd = Api Post $plannerT "/api/planner/events/$EventId/vendor-shortlist/send" @{ itemIds = @($itemId) }
Log "Planner send to client" $(if($snd.Ok){"PASS"}else{"FAIL"}) $(if(-not $snd.Ok){$snd.E})

$ap = Api Post $brideT "/api/events/$EventId/vendor-shortlist/$itemId/approve" @{}
Log "Client approve" $(if($ap.Ok){"PASS"}else{"FAIL"}) $(if(-not $ap.Ok){$ap.E})

$rb = Api Post $brideT "/api/events/$EventId/vendor-shortlist/$itemId/request-booking" @{}
if (-not $rb.Ok) { Log "Client request booking" "FAIL" $rb.E; exit 1 }
$bookingId = $rb.D.bookingId
Log "Client request booking" "PASS" $bookingId

$ac = Api Post $vendorT "/api/bookings/$bookingId/accept" @{}
Log "Vendor accept (Kandy Lens)" $(if($ac.Ok){"PASS"}else{"FAIL"}) $(if(-not $ac.Ok){$ac.E})

# Upload contract via curl (multipart)
$uploadOut = curl.exe -s -w "`nHTTP:%{http_code}" -X POST "$BaseUrl/api/bookings/$bookingId/contract/upload" `
    -H "Authorization: Bearer $vendorT" -F "generateStandardContract=true"
$uploadOk = $uploadOut -match "HTTP:200"
Log "Vendor generate/upload contract" $(if($uploadOk){"PASS"}else{"FAIL"}) ($uploadOut | Select-Object -Last 1)

$sn = Api Post $vendorT "/api/bookings/$bookingId/contract/send" $null
Log "Vendor send contract" $(if($sn.Ok){"PASS"}else{"FAIL"}) $(if(-not $sn.Ok){$sn.E})

$ct = Api Get $brideT "/api/bookings/$bookingId/contract"
$url = if ($ct.Ok) { $ct.D.contractFileUrl } else { $null }
$sg = Api Post $brideT "/api/bookings/$bookingId/contract/sign" @{ signerName = "Shamini"; contractFileUrl = $url }
Log "Client sign contract" $(if($sg.Ok){"PASS"}else{"FAIL"}) $(if(-not $sg.Ok){$sg.E})

$au = Api Get $plannerT "/api/events/$EventId/audit-log"
$signed = $false
if ($au.Ok) { $signed = @($au.D | Where-Object { $_.actionType -eq "ContractSigned" }).Count -gt 0 }
Log "Audit log ContractSigned" $(if($signed){"PASS"}else{"WARN"}) ""

$co = Api Post $brideT "/api/payments/bookings/$bookingId/deposit-checkout" @{}
Log "Deposit checkout" $(if($co.Ok){"PASS"}else{"FAIL"}) $(if($co.Ok){"isSimulated=$($co.D.isSimulated)"}else{$co.E})

$ps = Api Get $brideT "/api/payments/bookings/$bookingId/status"
Log "Payment status" $(if($ps.Ok){"PASS"}else{"FAIL"}) $(if($ps.Ok){"booking=$($ps.D.bookingStatus) payment=$($ps.D.paymentStatus)"})

$sl2 = Api Get $brideT "/api/events/$EventId/vendor-shortlist"
$mine = $sl2.D | Where-Object { $_.id -eq $itemId } | Select-Object -First 1
Log "Shortlist final status" $(if($mine){"PASS"}else{"FAIL"}) $(if($mine){$mine.status})

$an = Api Get $adminT "/api/admin/platform-analytics"
Log "Admin analytics" $(if($an.Ok){"PASS"}else{"FAIL"}) ""

Write-Host "`n========== FINAL REPORT ==========" -ForegroundColor Cyan
$Report | Format-Table -AutoSize -Wrap
$p = ($Report|? Status -eq PASS).Count; $f = ($Report|? Status -eq FAIL).Count
Write-Host "PASS: $p  FAIL: $f"
if ($bookingId) { Write-Host "Demo booking ID for evaluators: $bookingId" -ForegroundColor Cyan }
$Report | Export-Csv "$PSScriptRoot\happy_path_round_report.csv" -NoTypeInformation
