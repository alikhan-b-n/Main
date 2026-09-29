# Copies the values from deploy/.env.google into the user-secrets of Lama.Api.
# Only needed to run the CRM locally: on the server the same values live in deploy/.env.
#
#   powershell -ExecutionPolicy Bypass -File deploy\set-google-secrets.ps1
#
# Prints key names only — the refresh token grants full access to the calendar.

$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$envFile = Join-Path $PSScriptRoot '.env.google'
$project = Join-Path $root 'Lama.Api'

if (-not (Test-Path $envFile)) {
    Write-Host "No $envFile yet. Run deploy\get-google-refresh-token.py first."
    exit 1
}

# Name in the file -> configuration key
$keys = @{
    'GOOGLE_CLIENT_ID'     = 'Integrations:GoogleCalendar:ClientId'
    'GOOGLE_CLIENT_SECRET' = 'Integrations:GoogleCalendar:ClientSecret'
    'GOOGLE_REFRESH_TOKEN' = 'Integrations:GoogleCalendar:RefreshToken'
    'GOOGLE_CALENDAR_ID'   = 'Integrations:GoogleCalendar:CalendarId'
}

$written = 0
foreach ($line in Get-Content $envFile) {
    if ($line -match '^\s*#' -or $line -notmatch '=') { continue }

    $name, $value = $line -split '=', 2
    $name = $name.Trim()
    $value = $value.Trim()
    if (-not $keys.ContainsKey($name) -or [string]::IsNullOrWhiteSpace($value)) { continue }

    dotnet user-secrets set $keys[$name] $value --project $project | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "Could not store $($keys[$name])" }
    Write-Host "  $($keys[$name])"
    $written++
}

if ($written -eq 0) {
    Write-Host "Nothing to store: $envFile has no values."
    exit 1
}

Write-Host ""
Write-Host "Stored $written keys. Check with: dotnet user-secrets list --project Lama.Api"
