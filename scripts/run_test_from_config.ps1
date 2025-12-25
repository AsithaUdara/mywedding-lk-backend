$ErrorActionPreference = "Stop"
$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$configPath = Join-Path $scriptRoot "test_config.json"
if (-not (Test-Path $configPath)) { throw "Missing $configPath" }
$config = Get-Content $configPath -Raw | ConvertFrom-Json

$alice = $config.AliceToken
$bob = $config.BobToken
$auto = [bool]$config.AutoCreateEvent
$eventName = $config.EventName
$eventDate = $config.EventDate

Write-Host "Loaded config: AutoCreateEvent=$auto, EventName='$eventName'" -ForegroundColor Cyan

$argsScript = Join-Path $scriptRoot "clean_and_test_args.ps1"
& $argsScript -AliceToken $alice -BobToken $bob -AutoCreateEvent:$auto -EventName $eventName -EventDate $eventDate
