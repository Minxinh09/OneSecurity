# run_all.ps1
# Runs OneSecurity Central Server, React Dashboard, and Go Agent concurrently in separate windows.

$ErrorActionPreference = "Stop"

$workspaceDir = $PSScriptRoot
if ([string]::IsNullOrEmpty($workspaceDir)) {
    $workspaceDir = Get-Location
}

# 1. Load local environment helper to verify tools
$envScript = Join-Path $workspaceDir "load_env.ps1"
if (-not (Test-Path $envScript)) {
    Write-Error "Environment helper not found! Please make sure setup_tools.ps1 completed successfully."
}

Write-Host "Starting OneSecurity suite..." -ForegroundColor Cyan

# 2. Start Central Server
Write-Host "Launching Central Server (ASP.NET Core)..." -ForegroundColor Green
$serverCommand = @"
cd '$workspaceDir\server';
dotnet run --urls 'http://localhost:5082'
"@
Start-Process powershell -ArgumentList "-NoExit", "-Command", $serverCommand -WindowStyle Normal

# Wait 3 seconds for server boot before starting dashboard & agent
Start-Sleep -Seconds 3

# 3. Start Collector
Write-Host "Launching Collector Service (ASP.NET Core)..." -ForegroundColor Green
$collectorCommand = @"
cd '$workspaceDir\collector';
dotnet run
"@
Start-Process powershell -ArgumentList "-NoExit", "-Command", $collectorCommand -WindowStyle Normal

# Wait 3 seconds
Start-Sleep -Seconds 3

# 3. Setup and Start React Dashboard
Write-Host "Installing dashboard packages & launching Web Dashboard..." -ForegroundColor Green
$dashboardCommand = @"
. '$envScript';
cd '$workspaceDir\dashboard';
Write-Host 'Installing Node modules... This may take a minute...' -ForegroundColor Cyan;
npm install;
Write-Host 'Launching Vite dev server...' -ForegroundColor Cyan;
npm run dev -- --host
"@
Start-Process powershell -ArgumentList "-NoExit", "-Command", $dashboardCommand -WindowStyle Normal

# 4. Start Go Agent Simulator
Write-Host "Launching Go Agent Simulator..." -ForegroundColor Green
$agentCommand = @"
. '$envScript';
cd '$workspaceDir\agent';
go run main.go
"@
Start-Process powershell -ArgumentList "-NoExit", "-Command", $agentCommand -WindowStyle Normal

Write-Host "`nAll components launched!" -ForegroundColor Green
Write-Host " - Web UI Dashboard: http://localhost:5173" -ForegroundColor Yellow
Write-Host " - Backend API: http://localhost:5082" -ForegroundColor Yellow
Write-Host "Keep the spawned console windows open to view logs and interact with the Agent CLI." -ForegroundColor Cyan
