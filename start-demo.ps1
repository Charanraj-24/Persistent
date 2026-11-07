# Starts the Reflect Provider Dashboard and a public Cloudflare tunnel to it.
# Run from the repo root (the folder with ReflectProviderDashboard.slnx):
#   powershell -ExecutionPolicy Bypass -File .\start-demo.ps1
# To stop: press Ctrl+C in this window, then close the app window it opened.

$ErrorActionPreference = "Continue"
$root = $PSScriptRoot
$env:Path = [Environment]::GetEnvironmentVariable("Path","Machine") + ";" + [Environment]::GetEnvironmentVariable("Path","User")

# Find cloudflared
$cf = (Get-Command cloudflared -ErrorAction SilentlyContinue).Source
if (-not $cf) {
    $cf = @("C:\Program Files (x86)\cloudflared\cloudflared.exe",
            "C:\Program Files\cloudflared\cloudflared.exe",
            "$HOME\Downloads\cloudflared-windows-amd64.exe") | Where-Object { Test-Path $_ } | Select-Object -First 1
}
if (-not $cf) { throw "cloudflared not found. Install it: winget install -e --id Cloudflare.cloudflared" }

# 1. Start the app in its own window, set up to sit behind the tunnel
Write-Host "Starting the app in a new window..." -ForegroundColor Cyan
$web = Join-Path $root "src\Reflect.Web"
$cmd = "`$env:Hosting__BehindProxy='true'; `$env:Csp__StrictScripts='true'; Set-Location '$web'; dotnet run --urls http://localhost:5080"
Start-Process powershell -ArgumentList "-NoExit", "-Command", $cmd

# 2. Wait until the app answers
Write-Host "Waiting for the app to start (up to 2 minutes)..." -ForegroundColor Cyan
$up = $false
for ($i = 0; $i -lt 60 -and -not $up; $i++) {
    Start-Sleep -Seconds 2
    try { Invoke-WebRequest http://localhost:5080 -UseBasicParsing -TimeoutSec 3 | Out-Null; $up = $true } catch { if ($_.Exception.Response) { $up = $true } }
}
if (-not $up) { throw "The app did not start. Look at the app window for the error." }

# 3. Start the tunnel here
Write-Host "`nApp is up. Starting the tunnel. Your public address is in the box below." -ForegroundColor Green
Write-Host "Open THAT address (not localhost) to present, so the QR code uses it.`n" -ForegroundColor Green
& $cf tunnel --url http://localhost:5080
