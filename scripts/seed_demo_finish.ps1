# Completes seed after partial run (expenses, shortlist, second event, Pro tier).
$ErrorActionPreference = "Stop"
$BaseUrl = "http://localhost:5141"
$ApiKey = $env:FIREBASE_WEB_API_KEY
if (-not $ApiKey) {
    throw "Set FIREBASE_WEB_API_KEY (Firebase Web API key) before running this script."
}

function Get-Token($email, $password) {
    $r = Invoke-RestMethod -Method Post -Uri "https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key=$ApiKey" `
        -ContentType "application/json" -Body (@{ email = $email; password = $password; returnSecureToken = $true } | ConvertTo-Json)
    return $r.idToken
}

function Post($token, $path, $body) {
    Invoke-RestMethod -Method Post -Uri "$BaseUrl$path" -Headers @{ Authorization = "Bearer $token" } `
        -ContentType "application/json" -Body ($body | ConvertTo-Json -Depth 6)
}

$planner = Get-Token "demoplanner@mw.com" "demoplanner@mw.com"
$bride = Get-Token "shaminibride@mw.com" "shaminibride@mw.com"
$admin = Get-Token "admin@mw.com" "admin@mw.com"

# Upgrade to Planner Pro if needed
Post $planner "/api/planner/subscription" @{ tier = 3; monthlyFee = 14999 }

$event1Id = "49334c6e-c1b3-4627-8f7a-9bc2ffd68c8a"
$weddingDate = (Get-Date).AddMonths(6).Date.ToString("yyyy-MM-ddT12:00:00Z")

$kandy = Get-Token "kandylens@mw.com" "kandylens@mw.com"
$svcList = Invoke-RestMethod -Uri "$BaseUrl/api/vendor/dashboard/services" -Headers @{ Authorization = "Bearer $kandy" }
$photoId = ($svcList | Where-Object { $_.serviceName -like "*Full Day*" }).id
if (-not $photoId) { $photoId = $svcList[0].id }

$royal = Get-Token "royalspice@mw.com" "royalspice@mw.com"
$svcList2 = Invoke-RestMethod -Uri "$BaseUrl/api/vendor/dashboard/services" -Headers @{ Authorization = "Bearer $royal" }
$caterId = ($svcList2 | Where-Object { $_.serviceName -like "*Banquet*" }).id
if (-not $caterId) { $caterId = $svcList2[0].id }

$lotus = Get-Token "lotusbloom@mw.com" "lotusbloom@mw.com"
$svcList3 = Invoke-RestMethod -Uri "$BaseUrl/api/vendor/dashboard/services" -Headers @{ Authorization = "Bearer $lotus" }
$floralId = ($svcList3 | Where-Object { $_.serviceName -like "*Bouquet*" }).id
if (-not $floralId) { $floralId = $svcList3[0].id }

Write-Host "Services: $photoId $caterId $floralId"

$expenses = @(
    @{ title = "Venue deposit - Queens Hotel Kandy"; amount = 350000; budgetCategoryId = "77777777-7777-7777-7777-777777777777" },
    @{ title = "Engagement shoot deposit"; amount = 25000; budgetCategoryId = "88888888-8888-8888-8888-888888888888" },
    @{ title = "Tasting session - catering"; amount = 15000; budgetCategoryId = "99999999-9999-9999-9999-999999999999" },
    @{ title = "Bridal bouquet mock-up"; amount = 12000; budgetCategoryId = "AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAA" }
)
foreach ($e in $expenses) {
    $e.expenseDate = (Get-Date).ToString("o")
    try { Post $planner "/api/events/$event1Id/expenses" $e } catch { Write-Host "Expense skip: $($e.title)" }
}

$short = Post $planner "/api/planner/events/$event1Id/vendor-shortlist" @{
    sendToClient = $false
    items = @(
        @{ vendorServiceId = $photoId; categoryLabel = "Photography"; plannerNotes = "Recommended - Kandyan portfolio"; proposedAmount = 185000; serviceDate = $weddingDate },
        @{ vendorServiceId = $caterId; categoryLabel = "Catering"; plannerNotes = "Matches guest count 280-320"; proposedAmount = 850000; serviceDate = $weddingDate },
        @{ vendorServiceId = $floralId; categoryLabel = "Floral"; plannerNotes = "Lotus theme for poruwa"; proposedAmount = 45000; serviceDate = $weddingDate }
    )
}
Post $planner "/api/planner/events/$event1Id/vendor-shortlist/send" @{ itemIds = $short.itemIds }
Post $bride "/api/events/$event1Id/vendor-shortlist/$($short.itemIds[0])/approve" @{}

$ev2 = Post $planner "/api/planner/events" @{
    eventName = "Priya and Ravi - Garden Reception"
    eventDate = (Get-Date).AddMonths(7).Date.ToString("yyyy-MM-ddT15:00:00Z")
    totalBudget = 2800000
    clientEmail = "priya.client@mw.com"
}
Write-Host "Done. Event1=$event1Id Event2=$($ev2.eventId)"
