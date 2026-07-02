# Seeds MyWeddingDbDemo with 20 verified Sri Lankan vendors and cross-role demo data.
# Prerequisite: API running + seed_mwdemo_bootstrap.ps1 completed.
param(
    [string]$BaseUrl = "http://localhost:5141",
    [string]$ApiKey = "AIzaSyAu-Z0ZUAR2fQsLspGkmBbmhEEWrjsLtdc",
    [string]$BootstrapSecret = "mywedding-local-bootstrap-dev-only",
    [string]$SqlServer = "DESKTOP-SD2ALLO\SQLEXPRESS",
    [string]$Database = "MyWeddingDbDemo",
    [switch]$Reset
)

$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$VendorJsonPath = Join-Path $ScriptDir "data\mwdemo_vendors.json"
$ReportPath = Join-Path (Split-Path -Parent (Split-Path -Parent $ScriptDir)) "docs\DEMO_DATABASE_REPORT.md"

$seedSummary = @{
    Users = @{}
    ServiceIds = @{}
    EventIds = @{}
    BookingIds = @{}
    VendorRows = @()
}

function Ensure-FirebaseUser {
    param([string]$Email, [string]$Password, [string]$DisplayName)
    $signUpBody = @{
        email = $Email
        password = $Password
        displayName = $DisplayName
        returnSecureToken = $true
    } | ConvertTo-Json
    try {
        $r = Invoke-RestMethod -Method Post `
            -Uri "https://identitytoolkit.googleapis.com/v1/accounts:signUp?key=$ApiKey" `
            -ContentType "application/json" -Body $signUpBody
        Write-Host "  Created Firebase: $Email" -ForegroundColor Green
        return @{ uid = $r.localId; token = $r.idToken }
    }
    catch {
        $signInBody = @{ email = $Email; password = $Password; returnSecureToken = $true } | ConvertTo-Json
        $r = Invoke-RestMethod -Method Post `
            -Uri "https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key=$ApiKey" `
            -ContentType "application/json" -Body $signInBody
        Write-Host "  Existing Firebase: $Email" -ForegroundColor Yellow
        return @{ uid = $r.localId; token = $r.idToken }
    }
}

function Set-Role {
    param([string]$Uid, [string]$Role)
    Invoke-RestMethod -Method Post -Uri "$BaseUrl/api/admin/bootstrap/set-role" `
        -ContentType "application/json" `
        -Headers @{ "X-Bootstrap-Secret" = $BootstrapSecret } `
        -Body (@{ userId = $Uid; role = $Role } | ConvertTo-Json) | Out-Null
}

function Sync-User {
    param([string]$Token)
    Invoke-RestMethod -Method Post -Uri "$BaseUrl/api/auth/sync-user" `
        -Headers @{ Authorization = "Bearer $Token" } `
        -ContentType "application/json" -Body "{}" | Out-Null
}

function Api-Call {
    param(
        [string]$Method,
        [string]$Token,
        [string]$Path,
        [object]$Body = $null,
        [int]$Depth = 8
    )
    $headers = @{ Authorization = "Bearer $Token" }
    try {
        if ($null -ne $Body) {
            return Invoke-RestMethod -Method $Method -Uri "$BaseUrl$Path" -Headers $headers `
                -ContentType "application/json" -Body ($Body | ConvertTo-Json -Depth $Depth)
        }
        return Invoke-RestMethod -Method $Method -Uri "$BaseUrl$Path" -Headers $headers
    }
    catch {
        $reader = [System.IO.StreamReader]::new($_.Exception.Response.GetResponseStream())
        $err = $reader.ReadToEnd()
        throw "$Method $Path failed: $err"
    }
}

function Api-Post { param([string]$Token, [string]$Path, [object]$Body) Api-Call -Method Post -Token $Token -Path $Path -Body $Body }
function Api-Patch { param([string]$Token, [string]$Path, [object]$Body) Api-Call -Method Patch -Token $Token -Path $Path -Body $Body }
function Api-Put { param([string]$Token, [string]$Path, [object]$Body) Api-Call -Method Put -Token $Token -Path $Path -Body $Body }

function Invoke-DemoSql {
    param([string]$Query)
    if (Get-Command Invoke-Sqlcmd -ErrorAction SilentlyContinue) {
        try {
            $params = @{
                ServerInstance = $SqlServer
                Database = $Database
                Query = $Query
            }
            if ((Get-Command Invoke-Sqlcmd).Parameters.ContainsKey('TrustServerCertificate')) {
                $params.TrustServerCertificate = $true
            }
            Invoke-Sqlcmd @params | Out-Null
            return
        }
        catch {
            Write-Host "  Invoke-Sqlcmd failed, trying sqlcmd.exe..." -ForegroundColor Yellow
        }
    }
    $tempFile = [System.IO.Path]::GetTempFileName()
    try {
        Set-Content -Path $tempFile -Value $Query -Encoding UTF8
        $result = sqlcmd -S $SqlServer -d $Database -i $tempFile -C -b 2>&1
        if ($LASTEXITCODE -ne 0) {
            throw "SQL failed: $result"
        }
    }
    finally {
        Remove-Item $tempFile -Force -ErrorAction SilentlyContinue
    }
}

function Verify-VendorIfNeeded {
    param([string]$AdminToken, [string]$VendorUid)
    try {
        Api-Patch -Token $AdminToken -Path "/api/admin/vendors/$VendorUid/verify" -Body @{}
        Write-Host "    Verified" -ForegroundColor Green
    }
    catch {
        Write-Host "    Verify skipped (may already be verified)" -ForegroundColor Yellow
    }
}

function Complete-ContractFlow {
    param(
        [string]$ClientToken,
        [string]$VendorToken,
        [string]$BookingId,
        [string]$SignerName
    )
    try {
        $uploadOut = curl.exe -s -w "`nHTTP:%{http_code}" -X POST "$BaseUrl/api/bookings/$BookingId/contract/upload" `
            -H "Authorization: Bearer $VendorToken" -F "generateStandardContract=true"
        if ($uploadOut -notmatch "HTTP:200") {
            return $false
        }

        Api-Post -Token $VendorToken -Path "/api/bookings/$BookingId/contract/send" -Body $null | Out-Null
        $ct = Api-Call -Method Get -Token $ClientToken -Path "/api/bookings/$BookingId/contract"
        Api-Post -Token $ClientToken -Path "/api/bookings/$BookingId/contract/sign" -Body @{
            signerName = $SignerName
            contractFileUrl = $ct.contractFileUrl
        } | Out-Null
        return $true
    }
    catch {
        Write-Host "  Contract flow skipped for $BookingId" -ForegroundColor Yellow
        return $false
    }
}

function Set-BookingTargetStatus {
    param(
        [string]$VendorToken,
        [string]$BookingId,
        [ValidateSet("ContractSigned", "Confirmed")]
        [string]$TargetStatus
    )
    Api-Patch -Token $VendorToken -Path "/api/bookings/$BookingId/status" -Body @{ status = $TargetStatus } | Out-Null
}

function Advance-ShortlistItemBooking {
    param(
        [string]$ClientToken,
        [string]$VendorToken,
        [string]$EventId,
        [string]$ItemId,
        [bool]$NeedApprove,
        [ValidateSet("Requested", "AwaitingPayment", "ContractSigned", "Confirmed")]
        [string]$TargetStatus,
        [string]$SignerName = "Shamini"
    )

    if ($NeedApprove) {
        Api-Post -Token $ClientToken -Path "/api/events/$EventId/vendor-shortlist/$ItemId/approve" -Body @{} | Out-Null
    }

    $rb = Api-Post -Token $ClientToken -Path "/api/events/$EventId/vendor-shortlist/$ItemId/request-booking" -Body @{}
    $bookingId = $rb.bookingId

    if ($TargetStatus -eq "Requested") {
        return $bookingId
    }

    Api-Post -Token $VendorToken -Path "/api/bookings/$bookingId/accept" -Body @{} | Out-Null

    if ($TargetStatus -eq "AwaitingPayment") {
        return $bookingId
    }

    if (-not (Complete-ContractFlow -ClientToken $ClientToken -VendorToken $VendorToken -BookingId $bookingId -SignerName $SignerName)) {
        Set-BookingTargetStatus -VendorToken $VendorToken -BookingId $bookingId -TargetStatus $TargetStatus
        return $bookingId
    }

    if ($TargetStatus -eq "ContractSigned") {
        return $bookingId
    }

    Set-BookingTargetStatus -VendorToken $VendorToken -BookingId $bookingId -TargetStatus "Confirmed"
    return $bookingId
}

function New-ShortlistBooking {
    param(
        [string]$PlannerToken,
        [string]$ClientToken,
        [string]$VendorToken,
        [string]$EventId,
        [string]$ServiceId,
        [string]$CategoryLabel,
        [decimal]$Amount,
        [string]$ServiceDate,
        [ValidateSet("Requested", "AwaitingPayment", "ContractSigned", "Confirmed")]
        [string]$TargetStatus,
        [string]$SignerName = "Shamini"
    )

    $short = Api-Post -Token $PlannerToken -Path "/api/planner/events/$EventId/vendor-shortlist" -Body @{
        sendToClient = $false
        items = @(@{
            vendorServiceId = ([guid]$ServiceId).ToString()
            categoryLabel = $CategoryLabel
            plannerNotes = "Demo seed booking ($TargetStatus)"
            proposedAmount = [decimal]$Amount
            serviceDate = $ServiceDate
        })
    }
    $itemId = $short.itemIds[0]
    Api-Post -Token $PlannerToken -Path "/api/planner/events/$EventId/vendor-shortlist/send" -Body @{
        itemIds = @($itemId)
    } | Out-Null
    Api-Post -Token $ClientToken -Path "/api/events/$EventId/vendor-shortlist/$itemId/approve" -Body @{} | Out-Null
    $rb = Api-Post -Token $ClientToken -Path "/api/events/$EventId/vendor-shortlist/$itemId/request-booking" -Body @{}
    $bookingId = $rb.bookingId

    if ($TargetStatus -eq "Requested") {
        return $bookingId
    }

    Api-Post -Token $VendorToken -Path "/api/bookings/$bookingId/accept" -Body @{} | Out-Null

    if ($TargetStatus -eq "AwaitingPayment") {
        return $bookingId
    }

    if (-not (Complete-ContractFlow -ClientToken $ClientToken -VendorToken $VendorToken -BookingId $bookingId -SignerName $SignerName)) {
        Set-BookingTargetStatus -VendorToken $VendorToken -BookingId $bookingId -TargetStatus $TargetStatus
        return $bookingId
    }

    if ($TargetStatus -eq "ContractSigned") {
        return $bookingId
    }

    Set-BookingTargetStatus -VendorToken $VendorToken -BookingId $bookingId -TargetStatus "Confirmed"
    return $bookingId
}

Write-Host "`n=== MyWeddingDbDemo Full Seed ===" -ForegroundColor Cyan

if ($Reset) {
    Write-Host "-Reset: drop MyWeddingDbDemo manually, run ef database update, bootstrap, then re-run this script." -ForegroundColor Yellow
}

try {
    Invoke-RestMethod -Method Get -Uri "$BaseUrl/swagger/index.html" -TimeoutSec 5 | Out-Null
}
catch {
    throw "API not reachable at $BaseUrl. Start the API first."
}

if (-not (Test-Path $VendorJsonPath)) {
    throw "Vendor data file not found: $VendorJsonPath"
}

$vendorData = Get-Content $VendorJsonPath -Raw | ConvertFrom-Json

# --- Core accounts (admin assumed bootstrapped) ---
$coreAccounts = @(
    @{ Key = "admin";   Email = "admin@mw.com";        Password = "admin@mw.com";        Display = "Platform Admin"; Role = "admin" },
    @{ Key = "bride";   Email = "shaminibride@mw.com"; Password = "shaminibride@mw.com"; Display = "Shamini";        Role = "user" },
    @{ Key = "groom";   Email = "udaragroom@mw.com";   Password = "udaragroom@mw.com";   Display = "Udara";          Role = "user" },
    @{ Key = "planner"; Email = "demoplanner@mw.com";  Password = "demoplanner@mw.com";  Display = "Nethmi Perera";  Role = "planner" },
    @{ Key = "client2"; Email = "priya.client@mw.com"; Password = "priya.client@mw.com"; Display = "Priya Fernando"; Role = "user" }
)

Write-Host "`n--- Core accounts ---" -ForegroundColor Cyan
foreach ($a in $coreAccounts) {
    Write-Host "Account: $($a.Email)"
    $fb = Ensure-FirebaseUser -Email $a.Email -Password $a.Password -DisplayName $a.Display
    Set-Role -Uid $fb.uid -Role $a.Role
    Sync-User -Token $fb.token
    $seedSummary.Users[$a.Key] = @{ Uid = $fb.uid; Email = $a.Email; Token = $fb.token }
}

# --- Vendors from JSON ---
Write-Host "`n--- 20 vendors ---" -ForegroundColor Cyan
$coverSql = @()
foreach ($vd in $vendorData.vendors) {
    Write-Host "Vendor: $($vd.businessName)"
    $fb = Ensure-FirebaseUser -Email $vd.email -Password $vd.email -DisplayName $vd.displayName
    Set-Role -Uid $fb.uid -Role "vendor"
    Sync-User -Token $fb.token

    $firstName = ($vd.businessName -split "\s+")[0]
    Api-Post -Token $fb.token -Path "/api/vendors/register" -Body @{
        userId = $fb.uid
        email = $vd.email
        firstName = $firstName
        lastName = "Team"
        businessName = $vd.businessName
        category = $vd.category
        city = $vd.city
        contactPhone = $vd.phone
    } | Out-Null

    Verify-VendorIfNeeded -AdminToken $seedSummary.Users.admin.Token -VendorUid $fb.uid

    Api-Put -Token $fb.token -Path "/api/vendor/dashboard/profile" -Body @{
        businessName = $vd.businessName
        businessDescription = $vd.description
        websiteUrl = $vd.website
        contactPhone = $vd.phone
        city = $vd.city
        province = $vd.province
    } | Out-Null

    if ($vd.coverImageUrl) {
        $escapedUrl = $vd.coverImageUrl.Replace("'", "''")
        $coverSql += "UPDATE Vendors SET CoverImageUrl = N'$escapedUrl' WHERE UserId = N'$($fb.uid)';"
    }

    $serviceCount = 0
    $existingServices = @()
    try {
        $existingServices = @(Api-Call -Method Get -Token $fb.token -Path "/api/vendor/dashboard/services")
    }
    catch { }

    foreach ($svc in $vd.services) {
        if (@($existingServices | Where-Object { $_.serviceName -eq $svc.name }).Count -gt 0) {
            $existingId = ($existingServices | Where-Object { $_.serviceName -eq $svc.name } | Select-Object -First 1).id
            if ($existingId -is [System.Array]) { $existingId = $existingId[0] }
            $key = "$($vd.slug)|$($svc.name)"
            $seedSummary.ServiceIds[$key] = "$existingId"
            $serviceCount++
            Write-Host "    Service exists: $($svc.name)" -ForegroundColor Yellow
            continue
        }

        $galleryUrls = @($svc.galleryUrls | ForEach-Object { [string]$_ })
        if ($galleryUrls.Count -eq 0 -and $svc.primaryImageUrl) {
            $galleryUrls = @([string]$svc.primaryImageUrl)
        }

        $serviceBody = @{
            serviceName = [string]$svc.name
            description = [string]$svc.description
            basePrice = [decimal]$svc.basePrice
            pricingType = 3
            categoryId = [string]$vd.categoryId
            isActive = $true
            primaryImageUrl = [string]$svc.primaryImageUrl
            galleryUrls = $galleryUrls
        }
        if (-not [string]::IsNullOrWhiteSpace([string]$svc.tagline)) {
            $serviceBody.tagline = [string]$svc.tagline
        }

        $created = Api-Post -Token $fb.token -Path "/api/vendor/dashboard/services" -Body $serviceBody
        $key = "$($vd.slug)|$($svc.name)"
        $seedSummary.ServiceIds[$key] = "$($created.id)"
        $serviceCount++
    }

    $seedSummary.Users[$vd.slug] = @{ Uid = $fb.uid; Email = $vd.email; Token = $fb.token }
    $seedSummary.VendorRows += [pscustomobject]@{
        Slug = $vd.slug
        BusinessName = $vd.businessName
        Category = $vd.category
        City = $vd.city
        Uid = $fb.uid
        ServiceCount = $serviceCount
        CoverImageUrl = $vd.coverImageUrl
    }
}

if ($coverSql.Count -gt 0) {
    Write-Host "`nApplying vendor cover images via SQL..." -ForegroundColor Cyan
    Invoke-DemoSql -Query ($coverSql -join "`n")
}

# --- Planner profile ---
Write-Host "`n--- Planner profile ---" -ForegroundColor Cyan
try {
    Api-Post -Token $seedSummary.Users.planner.Token -Path "/api/planner/signup" -Body @{
        businessName = "Royal Kandyan Weddings by Nethmi"
        businessDescription = "Boutique wedding planning across Kandy, Colombo, and the hill country."
        contactPhone = "0777123456"
        city = "Kandy"
    } | Out-Null
}
catch {
    Write-Host "  Planner signup skipped (may already exist)" -ForegroundColor Yellow
}
Api-Post -Token $seedSummary.Users.planner.Token -Path "/api/planner/subscription" -Body @{
    tier = "3"
    monthlyFee = 14999
} | Out-Null

# --- Events ---
$weddingDate = (Get-Date).AddMonths(6).Date.ToString("yyyy-MM-ddT12:00:00Z")
Write-Host "`n--- Events ---" -ForegroundColor Cyan
$ev1 = Api-Post -Token $seedSummary.Users.planner.Token -Path "/api/planner/events" -Body @{
    eventName = "Shamini and Udara - Royal Kandyan Wedding"
    eventDate = $weddingDate
    totalBudget = 3500000
    clientEmail = $seedSummary.Users.bride.Email
}
$event1Id = $ev1.eventId
$seedSummary.EventIds["main"] = $event1Id
Write-Host "  Main event: $event1Id (tasks: $($ev1.tasksGenerated))" -ForegroundColor Green

Api-Patch -Token $seedSummary.Users.planner.Token -Path "/api/planner/events/$event1Id/stage" -Body @{ stage = "Planning" }

$inv = Api-Post -Token $seedSummary.Users.planner.Token -Path "/api/invitations/invite" -Body @{
    eventId = $event1Id
    email = $seedSummary.Users.groom.Email
    role = 1
    permissionLevel = 1
}
if ($inv.acceptUrl -match "token=([^&]+)") {
    Api-Post -Token $seedSummary.Users.groom.Token -Path "/api/invitations/accept" -Body @{ token = $Matches[1] } | Out-Null
    Write-Host "  Groom joined main event" -ForegroundColor Green
}

$ev2 = Api-Post -Token $seedSummary.Users.planner.Token -Path "/api/planner/events" -Body @{
    eventName = "Priya and Ravi - Garden Reception"
    eventDate = (Get-Date).AddMonths(7).Date.ToString("yyyy-MM-ddT15:00:00Z")
    totalBudget = 2800000
    clientEmail = $seedSummary.Users.client2.Email
}
$event2Id = $ev2.eventId
$seedSummary.EventIds["secondary"] = $event2Id
Write-Host "  Second event: $event2Id" -ForegroundColor Green

# --- Budget expenses ---
Write-Host "`n--- Budget expenses ---" -ForegroundColor Cyan
$expenses = @(
    @{ Title = "Venue deposit - Queen's Hotel Kandy"; Amount = 350000; Cat = "77777777-7777-7777-7777-777777777777" },
    @{ Title = "Engagement shoot deposit"; Amount = 25000; Cat = "88888888-8888-8888-8888-888888888888" },
    @{ Title = "Catering tasting session"; Amount = 15000; Cat = "99999999-9999-9999-9999-999999999999" },
    @{ Title = "Bridal bouquet mock-up"; Amount = 12000; Cat = "AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAA" },
    @{ Title = "DJ sound check fee"; Amount = 8000; Cat = "BBBBBBBB-BBBB-BBBB-BBBB-BBBBBBBBBBBB" },
    @{ Title = "Wedding cake tasting"; Amount = 5000; Cat = "CCCCCCCC-CCCC-CCCC-CCCC-CCCCCCCCCCCC" }
)
foreach ($e in $expenses) {
    Api-Post -Token $seedSummary.Users.planner.Token -Path "/api/events/$event1Id/expenses" -Body @{
        title = $e.Title
        amount = $e.Amount
        expenseDate = (Get-Date).ToString("o")
        budgetCategoryId = $e.Cat
    } | Out-Null
}

# --- Shortlist (6 categories) ---
Write-Host "`n--- Vendor shortlist ---" -ForegroundColor Cyan
$shortlistItems = @(
    @{ ServiceKey = "kandylens|Full Day Wedding Photography"; Label = "Photography"; Amount = 185000 },
    @{ ServiceKey = "queenskandy|Grand Ballroom Reception 250 Guests"; Label = "Venue"; Amount = 950000 },
    @{ ServiceKey = "royalspice|Royal Banquet 300 Guests"; Label = "Catering"; Amount = 850000 },
    @{ ServiceKey = "lotusbloom|Bridal Bouquet and Boutonniere Set"; Label = "Floral"; Amount = 45000 },
    @{ ServiceKey = "islandbeats|Full Reception DJ Package"; Label = "Music & DJ"; Amount = 95000 },
    @{ ServiceKey = "ceyloncakes|Four-Tier Signature Wedding Cake"; Label = "Other"; Amount = 125000 }
)

$shortlistPayload = @()
foreach ($item in $shortlistItems) {
    $serviceId = $seedSummary.ServiceIds[$item.ServiceKey]
    if (-not $serviceId) {
        throw "Missing service ID for $($item.ServiceKey). Check vendor seed completed."
    }
    $shortlistPayload += @{
        vendorServiceId = ([guid]$serviceId).ToString()
        categoryLabel = $item.Label
        plannerNotes = "Recommended for Shamini and Udara Kandyan wedding"
        proposedAmount = [decimal]$item.Amount
        serviceDate = $weddingDate
    }
}
$short = Api-Post -Token $seedSummary.Users.planner.Token -Path "/api/planner/events/$event1Id/vendor-shortlist" -Body @{
    sendToClient = $false
    items = $shortlistPayload
}
$shortlistItemIds = $short.itemIds
Api-Post -Token $seedSummary.Users.planner.Token -Path "/api/planner/events/$event1Id/vendor-shortlist/send" -Body @{
    itemIds = $shortlistItemIds
} | Out-Null
Api-Post -Token $seedSummary.Users.bride.Token -Path "/api/events/$event1Id/vendor-shortlist/$($shortlistItemIds[0])/approve" -Body @{} | Out-Null
Write-Host "  Shortlist: $($shortlistItemIds.Count) items sent, photography approved" -ForegroundColor Green

# --- Bookings (mixed statuses) ---
Write-Host "`n--- Bookings ---" -ForegroundColor Cyan
# Main event bookings reuse shortlist items 0-5 (avoid duplicate shortlist rows).
$seedSummary.BookingIds["photo_awaiting"] = Advance-ShortlistItemBooking `
    -ClientToken $seedSummary.Users.bride.Token `
    -VendorToken $seedSummary.Users.kandylens.Token `
    -EventId $event1Id `
    -ItemId "$($shortlistItemIds[0])" `
    -NeedApprove $false `
    -TargetStatus "AwaitingPayment"

$seedSummary.BookingIds["venue_confirmed"] = Advance-ShortlistItemBooking `
    -ClientToken $seedSummary.Users.bride.Token `
    -VendorToken $seedSummary.Users.queenskandy.Token `
    -EventId $event1Id `
    -ItemId "$($shortlistItemIds[1])" `
    -NeedApprove $true `
    -TargetStatus "Confirmed"

$seedSummary.BookingIds["catering_requested"] = Advance-ShortlistItemBooking `
    -ClientToken $seedSummary.Users.bride.Token `
    -VendorToken $seedSummary.Users.royalspice.Token `
    -EventId $event1Id `
    -ItemId "$($shortlistItemIds[2])" `
    -NeedApprove $true `
    -TargetStatus "Requested"

$seedSummary.BookingIds["floral_signed"] = Advance-ShortlistItemBooking `
    -ClientToken $seedSummary.Users.bride.Token `
    -VendorToken $seedSummary.Users.lotusbloom.Token `
    -EventId $event1Id `
    -ItemId "$($shortlistItemIds[3])" `
    -NeedApprove $true `
    -TargetStatus "ContractSigned"

$seedSummary.BookingIds["dj_awaiting"] = Advance-ShortlistItemBooking `
    -ClientToken $seedSummary.Users.bride.Token `
    -VendorToken $seedSummary.Users.islandbeats.Token `
    -EventId $event1Id `
    -ItemId "$($shortlistItemIds[4])" `
    -NeedApprove $true `
    -TargetStatus "AwaitingPayment"

$seedSummary.BookingIds["cakes_requested"] = Advance-ShortlistItemBooking `
    -ClientToken $seedSummary.Users.bride.Token `
    -VendorToken $seedSummary.Users.ceyloncakes.Token `
    -EventId $event1Id `
    -ItemId "$($shortlistItemIds[5])" `
    -NeedApprove $true `
    -TargetStatus "Requested"

$seedSummary.BookingIds["photo_event2"] = New-ShortlistBooking `
    -PlannerToken $seedSummary.Users.planner.Token `
    -ClientToken $seedSummary.Users.client2.Token `
    -VendorToken $seedSummary.Users.colombocapture.Token `
    -EventId $event2Id `
    -ServiceId $seedSummary.ServiceIds["colombocapture|Premium Photo + Video Duo"] `
    -CategoryLabel "Photography" `
    -Amount 245000 `
    -ServiceDate (Get-Date).AddMonths(7).Date.ToString("yyyy-MM-ddT15:00:00Z") `
    -TargetStatus "AwaitingPayment" `
    -SignerName "Priya"

$seedSummary.BookingIds["catering_event2"] = New-ShortlistBooking `
    -PlannerToken $seedSummary.Users.planner.Token `
    -ClientToken $seedSummary.Users.client2.Token `
    -VendorToken $seedSummary.Users.coastalbites.Token `
    -EventId $event2Id `
    -ServiceId $seedSummary.ServiceIds["coastalbites|Seafood Beach Banquet"] `
    -CategoryLabel "Catering" `
    -Amount 680000 `
    -ServiceDate (Get-Date).AddMonths(7).Date.ToString("yyyy-MM-ddT15:00:00Z") `
    -TargetStatus "Requested" `
    -SignerName "Priya"

foreach ($bk in $seedSummary.BookingIds.GetEnumerator()) {
    Write-Host "  $($bk.Key): $($bk.Value)" -ForegroundColor Green
}

# --- Inquiries ---
Write-Host "`n--- Vendor inquiries ---" -ForegroundColor Cyan
$inquiries = @(
    @{ Vendor = "kandylens"; Message = "Hi, can you confirm availability for a Kandyan poruwa shoot on our wedding date?" },
    @{ Vendor = "royalspice"; Message = "We need a vegetarian tasting session before confirming the banquet menu." },
    @{ Vendor = "gallefort"; Message = "Is the fort courtyard available for a December evening ceremony?" },
    @{ Vendor = "orchidgarden"; Message = "Looking for gold and white centrepieces for 25 tables in Colombo." },
    @{ Vendor = "kandyensemble"; Message = "Do you provide drummers for the homecoming as well as the poruwa?" }
)
foreach ($inq in $inquiries) {
    Api-Post -Token $seedSummary.Users.planner.Token -Path "/api/vendors/$($seedSummary.Users[$inq.Vendor].Uid)/inquiries" -Body @{
        message = $inq.Message
    } | Out-Null
}
Api-Post -Token $seedSummary.Users.bride.Token -Path "/api/vendors/$($seedSummary.Users.lotusbloom.Uid)/inquiries" -Body @{
    message = "Can we swap orchids for lotus in the bridal bouquet?"
} | Out-Null
Write-Host "  6 inquiries sent (5 planner, 1 bride)" -ForegroundColor Green

# --- Availability blocks ---
Write-Host "`n--- Availability blocks ---" -ForegroundColor Cyan
$blockDate1 = (Get-Date).AddMonths(3).Date.ToString("yyyy-MM-dd")
$blockDate2 = (Get-Date).AddMonths(4).Date.ToString("yyyy-MM-dd")
Api-Post -Token $seedSummary.Users.kandylens.Token -Path "/api/vendor/availability/block" -Body @{
    date = $blockDate1
    reason = "Pre-booked for corporate gala"
} | Out-Null
Api-Post -Token $seedSummary.Users.kandylens.Token -Path "/api/vendor/availability/block" -Body @{
    date = $blockDate2
    reason = "Team training day"
} | Out-Null
Api-Post -Token $seedSummary.Users.queenskandy.Token -Path "/api/vendor/availability/block" -Body @{
    date = $blockDate1
    reason = "Private hotel maintenance closure"
} | Out-Null
Write-Host "  Blocked dates on Kandy Lens (2) and Queen's Hotel (1)" -ForegroundColor Green

# --- Reviews (SQL — no public create API) ---
Write-Host "`n--- Reviews ---" -ForegroundColor Cyan
$reviewSql = @"
INSERT INTO VendorReviews (Id, Rating, ReviewContent, CreatedAt, VendorId, ReviewerId, EventId)
SELECT NEWID(), 5, N'Exceptional Kandyan wedding photos — every ritual captured beautifully.', GETUTCDATE(), N'$($seedSummary.Users.kandylens.Uid)', N'$($seedSummary.Users.bride.Uid)', '$event1Id'
WHERE NOT EXISTS (
    SELECT 1 FROM VendorReviews WHERE VendorId = N'$($seedSummary.Users.kandylens.Uid)' AND ReviewerId = N'$($seedSummary.Users.bride.Uid)' AND EventId = '$event1Id'
);

INSERT INTO VendorReviews (Id, Rating, ReviewContent, CreatedAt, VendorId, ReviewerId, EventId)
SELECT NEWID(), 5, N'Guests still talk about the buffet — flawless service for 280 guests.', GETUTCDATE(), N'$($seedSummary.Users.royalspice.Uid)', N'$($seedSummary.Users.bride.Uid)', '$event1Id'
WHERE NOT EXISTS (
    SELECT 1 FROM VendorReviews WHERE VendorId = N'$($seedSummary.Users.royalspice.Uid)' AND ReviewerId = N'$($seedSummary.Users.bride.Uid)' AND EventId = '$event1Id'
);

INSERT INTO VendorReviews (Id, Rating, ReviewContent, CreatedAt, VendorId, ReviewerId, EventId)
SELECT NEWID(), 4, N'Stunning lotus-themed poruwa decor. Minor delivery delay but outcome was perfect.', GETUTCDATE(), N'$($seedSummary.Users.lotusbloom.Uid)', N'$($seedSummary.Users.bride.Uid)', '$event1Id'
WHERE NOT EXISTS (
    SELECT 1 FROM VendorReviews WHERE VendorId = N'$($seedSummary.Users.lotusbloom.Uid)' AND ReviewerId = N'$($seedSummary.Users.bride.Uid)' AND EventId = '$event1Id'
);

UPDATE Vendors SET AverageRating = 5.0 WHERE UserId = N'$($seedSummary.Users.kandylens.Uid)';
UPDATE Vendors SET AverageRating = 5.0 WHERE UserId = N'$($seedSummary.Users.royalspice.Uid)';
UPDATE Vendors SET AverageRating = 4.0 WHERE UserId = N'$($seedSummary.Users.lotusbloom.Uid)';
"@
Invoke-DemoSql -Query $reviewSql
Write-Host "  3 reviews inserted" -ForegroundColor Green

# --- Full checklist ---
Write-Host "`n--- Task checklist ---" -ForegroundColor Cyan
$gen = Api-Post -Token $seedSummary.Users.planner.Token -Path "/api/events/$event1Id/tasks/generate-checklist" -Body @{}
Write-Host "  Full checklist: $($gen.tasksCreated) tasks" -ForegroundColor Green

# --- Platform stats ---
$stats = Api-Call -Method Get -Token $seedSummary.Users.admin.Token -Path "/api/admin/stats"
try {
    if (Get-Command Invoke-Sqlcmd -ErrorAction SilentlyContinue) {
        $migrationParams = @{
            ServerInstance = $SqlServer
            Database = $Database
            Query = "SELECT TOP 1 MigrationId FROM __EFMigrationsHistory ORDER BY MigrationId DESC"
        }
        if ((Get-Command Invoke-Sqlcmd).Parameters.ContainsKey('TrustServerCertificate')) {
            $migrationParams.TrustServerCertificate = $true
        }
        $migrationRow = Invoke-Sqlcmd @migrationParams | Select-Object -First 1
    }
}
catch { }
if (-not $migrationRow) {
    $migrationRow = [pscustomobject]@{ MigrationId = "(see dotnet ef migrations list)" }
}

# --- Write inventory report ---
$vendorTable = ($seedSummary.VendorRows | ForEach-Object {
    "| $($_.BusinessName) | $($_.Category) | $($_.City) | $($_.ServiceCount) | $($_.Uid) |"
}) -join "`n"

$bookingTable = ($seedSummary.BookingIds.GetEnumerator() | Sort-Object Name | ForEach-Object {
    "| $($_.Name) | $($_.Value) |"
}) -join "`n"

$loginTable = @"
| Role | Email | Password |
|------|-------|----------|
| Admin | admin@mw.com | admin@mw.com |
| Planner | demoplanner@mw.com | demoplanner@mw.com |
| Bride (main event) | shaminibride@mw.com | shaminibride@mw.com |
| Groom | udaragroom@mw.com | udaragroom@mw.com |
| Client 2 | priya.client@mw.com | priya.client@mw.com |
| Vendors (20) | `{slug}@mw.com` | same as email |
"@

$report = @"
# MyWeddingDbDemo — Data Inventory Report

Generated: $(Get-Date -Format "yyyy-MM-dd HH:mm")

## Database

| Setting | Value |
|---------|-------|
| Database name | **MyWeddingDbDemo** |
| SQL Server | ``$SqlServer`` |
| Latest migration | ``$($migrationRow.MigrationId)`` |

## Admin user

| Field | Value |
|-------|-------|
| Email | admin@mw.com |
| Firebase UID | zHvTgDy7CZVDYtyUTHNatugwwg82 |
| Role | admin |

## Table counts (from admin stats API)

| Entity | Count |
|--------|------:|
| Users | $($stats.totalUsers) |
| Vendors | $($stats.totalVendors) |
| Wedding events | $($stats.totalEvents) |
| Bookings | $($stats.totalBookings) |

## Key entity IDs

| Entity | ID |
|--------|-----|
| Main event (Shamini & Udara) | ``$event1Id`` |
| Second event (Priya & Ravi) | ``$event2Id`` |
| Planner UID | ``$($seedSummary.Users.planner.Uid)`` |
| Bride UID | ``$($seedSummary.Users.bride.Uid)`` |

### Sample booking IDs

| Label | Booking ID |
|-------|------------|
$bookingTable

## Vendor directory (20 verified)

| Business | Category | City | Services | Firebase UID |
|----------|----------|------|----------|--------------|
$vendorTable

## Demo login credentials

$loginTable

## Seed scripts

``````powershell
# 1. Connection string → MyWeddingDbDemo (user-secrets)
# 2. dotnet ef database update
# 3. dotnet run (API)
powershell -ExecutionPolicy Bypass -File backend\scripts\seed_mwdemo_bootstrap.ps1
powershell -ExecutionPolicy Bypass -File backend\scripts\seed_mwdemo_full.ps1
``````

## Notes

- Sign out and sign in once per role so Firebase custom claims apply in the browser.
- Vendor cover images are set via SQL (``CoverImageUrl``); service images use Unsplash URLs on each listing.
- Reviews are seeded via SQL because there is no public review-create API.
- To reset: ``DROP DATABASE MyWeddingDbDemo``, re-run migrations, bootstrap, and full seed.
"@

$reportDir = Split-Path $ReportPath -Parent
if (-not (Test-Path $reportDir)) {
    New-Item -ItemType Directory -Path $reportDir -Force | Out-Null
}
Set-Content -Path $ReportPath -Value $report -Encoding UTF8
Write-Host "`nReport written: $ReportPath" -ForegroundColor Cyan

Write-Host "`n=== MyWeddingDbDemo seed complete ===" -ForegroundColor Green
Write-Host "Sign out and sign in once per role before demoing." -ForegroundColor Yellow
Write-Host @"

MAIN EVENT: Shamini and Udara - Royal Kandyan Wedding
  ID: $event1Id
  URL: http://localhost:3000/events/$event1Id

SECOND EVENT: Priya and Ravi - Garden Reception
  ID: $event2Id

Vendors: 20 verified | Bookings: $($seedSummary.BookingIds.Count) | Inquiries: 6 | Reviews: 3
"@ -ForegroundColor Cyan
