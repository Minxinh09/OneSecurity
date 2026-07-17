@echo off
title OneSecurity Collector - Port 5050
echo ==========================================
echo   OneSecurity COLLECTOR
echo   Listening on: http://0.0.0.0:5050
echo   Forwarding to Server: http://localhost:5082
echo   Press Ctrl+C to STOP
echo ==========================================
echo.
dotnet run
pause
