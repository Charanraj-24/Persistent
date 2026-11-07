# Deploys the Reflect Provider Dashboard to Azure App Service (Linux, .NET 10).
# Run from the repo root (the folder with ReflectProviderDashboard.slnx), in PowerShell:
#   .\deploy-azure.ps1 -AppName reflect-dash-yourinitials
# Re-run it any time to push new code; it skips anything that already exists.

param(
    [Parameter(Mandatory = $true)][string]$AppName,       # becomes https://<AppName>.azurewebsites.net (must be globally unique)
    [string]$ResourceGroup = "reflect-demo-rg",
    [string]$Location = "eastus2",
    [string]$Plan = "reflect-plan",
    [string]$Sku = "B1"
)
$ErrorActionPreference = "Continue"   # az writes warnings to stderr; we check exit codes ourselves
$Url = "https://$AppName.azurewebsites.net"

Write-Host "`n== 1. Checking Azure sign-in" -ForegroundColor Cyan
az account show --query "{subscription:name, user:user.name}" -o table
if ($LASTEXITCODE -ne 0) { throw "Not signed in to Azure. Run: az login --use-device-code   then run this script again." }

Write-Host "`n== 2. Checking that App Service offers .NET 10 on Linux" -ForegroundColor Cyan
$runtimes = az webapp list-runtimes --os linux -o tsv
if ($LASTEXITCODE -ne 0) { throw "Could not list runtimes (see the error above)." }
$runtime = ($runtimes | Where-Object { $_ -match '^\s*DOTNET(CORE)?[:|]10' } | Select-Object -First 1)
if (-not $runtime) {
    Write-Host "Linux .NET runtimes offered:"; $runtimes | Where-Object { $_ -match 'DOTNET' }
    throw ".NET 10 not offered on Linux. Paste the list above to Claude."
}
$runtime = $runtime.Trim().Replace("|", ":")   # a | would break az.cmd on Windows
Write-Host "Using runtime $runtime"

Write-Host "`n== 3. Creating resource group, plan and web app (skipped if they exist)" -ForegroundColor Cyan
if ((az group exists -n $ResourceGroup) -ne "true") { az group create -n $ResourceGroup -l $Location -o none }
if (-not (az appservice plan show -g $ResourceGroup -n $Plan -o tsv --query name 2>$null)) {
    az appservice plan create -g $ResourceGroup -n $Plan --is-linux --sku $Sku -l $Location -o none
    if ($LASTEXITCODE -ne 0) { throw "Could not create the plan in $Location. Pick a region from the allowed list and re-run with -Location <region>." }
}
if (-not (az webapp show -g $ResourceGroup -n $AppName -o tsv --query name 2>$null)) {
    az webapp create -g $ResourceGroup -p $Plan -n $AppName --runtime "$runtime" -o none
    if ($LASTEXITCODE -ne 0) { throw "Could not create the web app (name taken, or region not allowed: try -Location centralus)." }
}

Write-Host "`n== 4. Configuring: always on, HTTPS only, one instance, startup command" -ForegroundColor Cyan
$alwaysOn = if ($Sku -eq "F1") { "false" } else { "true" }   # Free tier cannot stay always on
az webapp config set -g $ResourceGroup -n $AppName --always-on $alwaysOn --http20-enabled true --min-tls-version 1.2 --ftps-state Disabled --startup-file "dotnet Reflect.Web.dll" -o none
az webapp update -g $ResourceGroup -n $AppName --https-only true --client-affinity-enabled true -o none

Write-Host "`n== 5. App settings" -ForegroundColor Cyan
$existing = az webapp config appsettings list -g $ResourceGroup -n $AppName --query "[].name" -o tsv
$settings = @(
    "ASPNETCORE_ENVIRONMENT=Production",
    "Hosting__BehindProxy=true",
    "Csp__StrictScripts=true",
    "Demo__PublicBaseUrl=$Url"
)
if (-not ($existing -match "^Demo__Password$")) {
    $pw = Read-Host "Choose the demo sign-in password (letters and digits only; all demo users share it)"
    $settings += "Demo__Password=$pw"
}
if (-not ($existing -match "^Ingest__ApiKey$")) {
    $settings += "Ingest__ApiKey=" + [guid]::NewGuid().ToString("N")
}
az webapp config appsettings set -g $ResourceGroup -n $AppName --settings $settings -o none

Write-Host "`n== 6. Building" -ForegroundColor Cyan
if (Test-Path publish) { Remove-Item publish -Recurse -Force }
if (Test-Path app.zip) { Remove-Item app.zip -Force }
dotnet publish src/Reflect.Web/Reflect.Web.csproj -c Release -o publish -p:RazorRuntimeCompilation=false
if ($LASTEXITCODE -ne 0) { throw "Build failed." }
Compress-Archive -Path publish\* -DestinationPath app.zip -Force

Write-Host "`n== 7. Deploying (takes 1 to 3 minutes)" -ForegroundColor Cyan
az webapp deploy -g $ResourceGroup -n $AppName --src-path app.zip --type zip -o none
if ($LASTEXITCODE -ne 0) { throw "Deploy failed. Run: az webapp log tail -g $ResourceGroup -n $AppName" }

Write-Host "`n== Done: $Url" -ForegroundColor Green
Write-Host "First load can take ~30 seconds while it starts. Logs: az webapp log tail -g $ResourceGroup -n $AppName"
