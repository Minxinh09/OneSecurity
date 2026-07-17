@echo off
set "WORKSPACE_DIR=%~dp0"
set "TOOLS_DIR=%WORKSPACE_DIR%tools"
set "GOROOT=%TOOLS_DIR%\go"
set "PATH=%TOOLS_DIR%\go\bin;%TOOLS_DIR%\node;%PATH%"

echo Environment configured for this CMD session.
echo.
go version
node -v
npm -v
