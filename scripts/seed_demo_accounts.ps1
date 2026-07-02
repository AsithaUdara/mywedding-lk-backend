# Seeds demo accounts and rich wedding data for MyWedding.lk presentations.
# Requires: API at http://localhost:5141, Firebase project myweddinglk-111c3
$ErrorActionPreference = "Stop"

$BaseUrl = "http://localhost:5141"
$ApiKey = "AIzaSyAu-Z0ZUAR2fQsLspGkmBbmhEEWrjsLtdc"
$BootstrapSecret = "mywedding-local-bootstrap-dev-only"
$Img = "https://images.unsplash.com/photo-1519741497674-611481863552?w=800&q=80"

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

function Api-Post {
    param([string]$Token, [string]$Path, [object]$Body)
    $h = @{ Authorization = "Bearer $Token" }
    try {
        if ($null -ne $Body) {
            return Invoke-RestMethod -Method Post -Uri "$BaseUrl$Path" -Headers $h -ContentType "application/json" -Body ($Body | ConvertTo-Json -Depth 6)
        }
        return Invoke-RestMethod -Method Post -Uri "$BaseUrl$Path" -Headers $h
    }
    catch {
        $reader = [System.IO.StreamReader]::new($_.Exception.Response.GetResponseStream())
        $err = $reader.ReadToEnd()
        throw "POST $Path failed: $err"
    }
}

function Api-Patch {
    param([string]$Token, [string]$Path, [object]$Body)
    Invoke-RestMethod -Method Patch -Uri "$BaseUrl$Path" `
        -Headers @{ Authorization = "Bearer $Token" } `
        -ContentType "application/json" -Body ($Body | ConvertTo-Json) | Out-Null
}

Write-Host "`n=== MyWedding.lk Demo Seed ===" -ForegroundColor Cyan

# --- Accounts ---
$accounts = @(
    @{ Key = "admin";    Email = "admin@mw.com";           Password = "admin@mw.com";           Display = "Platform Admin"; Role = "admin" },
    @{ Key = "bride";    Email = "shaminibride@mw.com";    Password = "shaminibride@mw.com";    Display = "Shamini";        Role = "user" },
    @{ Key = "groom";    Email = "udaragroom@mw.com";      Password = "udaragroom@mw.com";      Display = "Udara";          Role = "user" },
    @{ Key = "planner";  Email = "demoplanner@mw.com";     Password = "demoplanner@mw.com";     Display = "Nethmi Perera";  Role = "planner" },
    @{ Key = "client2";  Email = "priya.client@mw.com";    Password = "priya.client@mw.com";    Display = "Priya Fernando"; Role = "user" },
    @{ Key = "vendor1";  Email = "kandylens@mw.com";       Password = "kandylens@mw.com";       Display = "Kandy Lens";     Role = "vendor" },
    @{ Key = "vendor2";  Email = "royalspice@mw.com";      Password = "royalspice@mw.com";      Display = "Royal Spice";    Role = "vendor" },
    @{ Key = "vendor3";  Email = "lotusbloom@mw.com";      Password = "lotusbloom@mw.com";      Display = "Lotus Bloom";    Role = "vendor" }
)

$users = @{}
foreach ($a in $accounts) {
    Write-Host "Account: $($a.Email)" -ForegroundColor Cyan
    $fb = Ensure-FirebaseUser -Email $a.Email -Password $a.Password -DisplayName $a.Display
    Set-Role -Uid $fb.uid -Role $a.Role
    Sync-User -Token $fb.token
    $users[$a.Key] = @{ Uid = $fb.uid; Token = $fb.token; Email = $a.Email }
}

# --- Planner profile + Pro (multiple events) ---
Write-Host "`nPlanner signup..." -ForegroundColor Cyan
Api-Post -Token $users.planner.Token -Path "/api/planner/signup" -Body @{
    businessName = "Royal Kandyan Weddings by Nethmi"
    businessDescription = "Boutique wedding planning across Kandy, Colombo, and the hill country. Specializing in traditional Kandyan ceremonies with modern coordination."
    contactPhone = "0777123456"
    city = "Kandy"
} | Out-Null
Api-Post -Token $users.planner.Token -Path "/api/planner/subscription" -Body @{
    tier = 3
    monthlyFee = 14999
} | Out-Null

# --- Vendors ---
$vendorDefs = @(
    @{
        Key = "vendor1"
        BusinessName = "Kandy Lens Studio"
        Category = "photography"
        City = "Kandy"
        Phone = "0777111001"
        Services = @(
            @{ Name = "Full Day Wedding Photography"; Price = 185000; Cat = "22222222-2222-2222-2222-222222222222"; Desc = "12-hour coverage, 2 photographers, 600+ edited photos, online gallery." },
            @{ Name = "Engagement & Pre-Wedding Shoot"; Price = 75000; Cat = "22222222-2222-2222-2222-222222222222"; Desc = "Half-day session at heritage location with 80 edited portraits." },
            @{ Name = "Cinematic Highlight Film"; Price = 120000; Cat = "22222222-2222-2222-2222-222222222222"; Desc = "4-6 min cinematic reel delivered within 3 weeks." }
        )
    },
    @{
        Key = "vendor2"
        BusinessName = "Royal Spice Catering"
        Category = "catering"
        City = "Colombo"
        Phone = "0777222002"
        Services = @(
            @{ Name = "Royal Banquet 300 Guests"; Price = 850000; Cat = "33333333-3333-3333-3333-333333333333"; Desc = "Traditional Sri Lankan buffet, live cooking stations, service staff included." }
            @{ Name = "Cocktail and Canape Reception"; Price = 220000; Cat = "33333333-3333-3333-3333-333333333333"; Desc = "2-hour cocktail service with 8 canape varieties and beverage station." }
            @{ Name = "Kiribath & Traditional Breakfast"; Price = 95000; Cat = "33333333-3333-3333-3333-333333333333"; Desc = "Morning after-wedding breakfast for 150 guests." }
        )
    },
    @{
        Key = "vendor3"
        BusinessName = "Lotus Bloom Florists"
        Category = "floral"
        City = "Kandy"
        Phone = "0777333003"
        Services = @(
            @{ Name = "Bridal Bouquet and Boutonniere Set"; Price = 45000; Cat = "44444444-4444-4444-4444-444444444444"; Desc = "Orchid and lotus bridal bouquet with groom and party flowers." }
            @{ Name = "Ceremony Mandap & Aisle Decor"; Price = 175000; Cat = "44444444-4444-4444-4444-444444444444"; Desc = "Full poruwa/mandap floral styling with aisle petals and entrance arch." },
            @{ Name = "Reception Centrepieces per table"; Price = 8500; Cat = "44444444-4444-4444-4444-444444444444"; Desc = "Gold pedestal arrangements, priced per table, minimum 20 tables." }
        )
    }
)

$serviceIds = @{}
foreach ($vd in $vendorDefs) {
    $v = $users[$vd.Key]
    Write-Host "`nVendor: $($vd.BusinessName)" -ForegroundColor Cyan
    Api-Post -Token $v.Token -Path "/api/vendors/register" -Body @{
        userId = $v.Uid
        email = $v.Email
        firstName = $vd.BusinessName.Split(" ")[0]
        lastName = "Studio"
        businessName = $vd.BusinessName
        category = $vd.Category
        city = $vd.City
        contactPhone = $vd.Phone
    } | Out-Null
    Api-Patch -Token $users.admin.Token -Path "/api/admin/vendors/$($v.Uid)/verify" -Body @{}
    foreach ($svc in $vd.Services) {
        $id = Api-Post -Token $v.Token -Path "/api/vendor/dashboard/services" -Body @{
            serviceName = $svc.Name
            description = $svc.Desc
            basePrice = $svc.Price
            pricingType = 3
            categoryId = $svc.Cat
            isActive = $true
            primaryImageUrl = $Img
            galleryUrls = @($Img)
            tagline = "Demo listing - verified for MyWedding.lk"
        }
        $serviceIds[$svc.Name] = $id.id
        Write-Host "  Service: $($svc.Name)" -ForegroundColor Green
    }
}

# --- Events ---
$weddingDate = (Get-Date).AddMonths(6).Date.ToString("yyyy-MM-ddT12:00:00Z")
Write-Host "`nCreating Shamini and Udara wedding event..." -ForegroundColor Cyan
$ev1 = Api-Post -Token $users.planner.Token -Path "/api/planner/events" -Body @{
    eventName = "Shamini and Udara - Royal Kandyan Wedding"
    eventDate = $weddingDate
    totalBudget = 3500000
    clientEmail = $users.bride.Email
}
$event1Id = $ev1.eventId
Write-Host "  Event: $event1Id - tasks generated: $($ev1.tasksGenerated)" -ForegroundColor Green

Api-Patch -Token $users.planner.Token -Path "/api/planner/events/$event1Id/stage" -Body @{ stage = "Planning" }

Write-Host "Inviting groom Udara..." -ForegroundColor Cyan
$inv = Api-Post -Token $users.planner.Token -Path "/api/invitations/invite" -Body @{
    eventId = $event1Id
    email = $users.groom.Email
    role = 1
    permissionLevel = 1
}
if ($inv.acceptUrl -match "token=([^&]+)") {
    $token = $Matches[1]
    Api-Post -Token $users.groom.Token -Path "/api/invitations/accept" -Body @{ token = $token } | Out-Null
    Write-Host "  Groom joined event" -ForegroundColor Green
}
else {
    Write-Host "  Invite URL (accept manually): $($inv.acceptUrl)" -ForegroundColor Yellow
}

Write-Host "Creating second client event (Priya)..." -ForegroundColor Cyan
$ev2 = Api-Post -Token $users.planner.Token -Path "/api/planner/events" -Body @{
    eventName = "Priya and Ravi - Garden Reception"
    eventDate = (Get-Date).AddMonths(7).Date.ToString("yyyy-MM-ddT15:00:00Z")
    totalBudget = 2800000
    clientEmail = $users.client2.Email
}
$event2Id = $ev2.eventId
Write-Host "  Event: $event2Id" -ForegroundColor Green

# --- Budget expenses (main event) ---
$expenses = @(
    @{ Title = "Venue deposit - Queens Hotel Kandy"; Amount = 350000; Cat = "77777777-7777-7777-7777-777777777777" },
    @{ Title = "Engagement shoot deposit"; Amount = 25000; Cat = "88888888-8888-8888-8888-888888888888" },
    @{ Title = "Tasting session - catering"; Amount = 15000; Cat = "99999999-9999-9999-9999-999999999999" },
    @{ Title = "Bridal bouquet mock-up"; Amount = 12000; Cat = "AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAA" }
)
foreach ($e in $expenses) {
    Api-Post -Token $users.planner.Token -Path "/api/events/$event1Id/expenses" -Body @{
        title = $e.Title
        amount = $e.Amount
        expenseDate = (Get-Date).ToString("o")
        budgetCategoryId = $e.Cat
    } | Out-Null
}

# --- Vendor shortlist (3 categories) ---
$photoId = $serviceIds["Full Day Wedding Photography"]
$caterId = $serviceIds["Royal Banquet 300 Guests"]
$floralId = $serviceIds["Bridal Bouquet and Boutonniere Set"]

$short = Api-Post -Token $users.planner.Token -Path "/api/planner/events/$event1Id/vendor-shortlist" -Body @{
    sendToClient = $false
    items = @(
        @{ vendorServiceId = $photoId; categoryLabel = "Photography"; plannerNotes = "Recommended - strong Kandyan portfolio"; proposedAmount = 185000; serviceDate = $weddingDate },
        @{ vendorServiceId = $caterId; categoryLabel = "Catering"; plannerNotes = "Matches guest count 280-320"; proposedAmount = 850000; serviceDate = $weddingDate },
        @{ vendorServiceId = $floralId; categoryLabel = "Floral"; plannerNotes = "Lotus theme for poruwa"; proposedAmount = 45000; serviceDate = $weddingDate }
    )
}
$itemIds = $short.itemIds
Api-Post -Token $users.planner.Token -Path "/api/planner/events/$event1Id/vendor-shortlist/send" -Body @{
    itemIds = $itemIds
} | Out-Null

# Bride approves photography option
Api-Post -Token $users.bride.Token -Path "/api/events/$event1Id/vendor-shortlist/$($itemIds[0])/approve" -Body @{} | Out-Null

# Full Gantt checklist (~50 tasks)
$gen = Api-Post -Token $users.planner.Token -Path "/api/events/$event1Id/tasks/generate-checklist" -Body @{}
Write-Host "  Full checklist: $($gen.tasksCreated) tasks" -ForegroundColor Green

Write-Host "`n=== Seed complete ===" -ForegroundColor Green
Write-Host "Sign out and sign in once per role so Firebase role claims apply in the browser." -ForegroundColor Yellow
Write-Host @"

DEMO LOGINS (email = password):
  Admin:    admin@mw.com
  Bride:    shaminibride@mw.com  (Shamini)
  Groom:    udaragroom@mw.com    (Udara - on event as Groom)
  Planner:  demoplanner@mw.com   (Nethmi Perera)
  Client 2: priya.client@mw.com  (Priya Fernando)
  Vendors:  kandylens@mw.com | royalspice@mw.com | lotusbloom@mw.com

MAIN EVENT: Shamini and Udara - Royal Kandyan Wedding
  ID: $event1Id
  URL: http://localhost:3000/events/$event1Id
  Planner: http://localhost:3000/planner/procurement (select event)

SECOND EVENT: Priya and Ravi - Garden Reception
  ID: $event2Id

Services created: 9 (3 per vendor, all verified)
Shortlist: 3 sent to client, photography approved
"@ -ForegroundColor Cyan
