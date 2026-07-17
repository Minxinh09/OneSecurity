@echo off
set "WORKSPACE_DIR=%~dp0"
set "TEMP_DIR=%WORKSPACE_DIR%temp_package"

echo Packaging OneSecurity project for sharing...
echo (Excluding tools, database, and temporary build folders to keep size small)
echo.

:: Clean up old files
if exist "%WORKSPACE_DIR%onesecurity_release.zip" del /f /q "%WORKSPACE_DIR%onesecurity_release.zip"
if exist "%TEMP_DIR%" rmdir /s /q "%TEMP_DIR%"

:: Create temp directories
mkdir "%TEMP_DIR%"
mkdir "%TEMP_DIR%\server"
mkdir "%TEMP_DIR%\agent"
mkdir "%TEMP_DIR%\dashboard"

:: Copy server files (exclude bin, obj, database files)
robocopy "%WORKSPACE_DIR%server" "%TEMP_DIR%\server" /S /XD bin obj /XF onesecurity.db onesecurity.db-shm onesecurity.db-wal > nul

:: Copy agent files
robocopy "%WORKSPACE_DIR%agent" "%TEMP_DIR%\agent" /S > nul

:: Copy dashboard files (exclude node_modules and dist)
robocopy "%WORKSPACE_DIR%dashboard" "%TEMP_DIR%\dashboard" /S /XD node_modules dist > nul

:: Copy root scripts
copy "%WORKSPACE_DIR%setup_tools.ps1" "%TEMP_DIR%\" > nul
copy "%WORKSPACE_DIR%load_env.ps1" "%TEMP_DIR%\" > nul
copy "%WORKSPACE_DIR%load_env.bat" "%TEMP_DIR%\" > nul
copy "%WORKSPACE_DIR%run_all.ps1" "%TEMP_DIR%\" > nul
copy "%WORKSPACE_DIR%run_all.bat" "%TEMP_DIR%\" > nul
copy "%WORKSPACE_DIR%README.md" "%TEMP_DIR%\" > nul

:: Zip the temp folder
powershell -Command "& { Compress-Archive -Path '%TEMP_DIR%\*' -DestinationPath '%WORKSPACE_DIR%onesecurity_release.zip' -Force }"

:: Clean up temp folder
rmdir /s /q "%TEMP_DIR%"

echo.
echo Packaging complete! Release archive created at:
echo %WORKSPACE_DIR%onesecurity_release.zip
echo.
pause
