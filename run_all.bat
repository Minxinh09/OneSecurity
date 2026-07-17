@echo off
set "WORKSPACE_DIR=%~dp0"
set "TOOLS_DIR=%WORKSPACE_DIR%tools"
set "GOROOT=%TOOLS_DIR%\go"
set "PATH=%TOOLS_DIR%\go\bin;%TOOLS_DIR%\node;%PATH%"

echo Starting OneSecurity suite...

:: 1. Launch Central Server
echo Launching Central Server (ASP.NET Core)...
start "OneSecurity Server" cmd /k "cd /d %WORKSPACE_DIR%server && dotnet run"

:: Wait 3 seconds
timeout /t 3 /nobreak >nul

:: 2. Launch Collector
echo Launching Collector Service (ASP.NET Core)...
start "OneSecurity Collector" cmd /k "cd /d %WORKSPACE_DIR%collector && dotnet run"

:: Wait 3 seconds
timeout /t 3 /nobreak >nul

:: 2. Setup and Launch React Dashboard
echo Installing packages and launching Web Dashboard...
start "OneSecurity Dashboard" cmd /k "cd /d %WORKSPACE_DIR%dashboard && call ..\load_env.bat && npm install && npm run dev"

:: 3. Launch Go Agent Simulator
echo Launching Go Agent Simulator...
start "OneSecurity Agent" cmd /k "cd /d %WORKSPACE_DIR%agent && call ..\load_env.bat && go run main.go"

echo.
echo All components launched!
echo - Web UI Dashboard: http://localhost:5173
echo - Backend API: http://localhost:5000
echo.
echo Please keep the spawned console windows open to view logs and interact with the Agent CLI.
