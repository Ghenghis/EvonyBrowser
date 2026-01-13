@echo off
:: Quick Server Switcher
title Evony RE - Server Switcher

echo.
echo ╔═══════════════════════════════════════════════════════════════════╗
echo ║              EVONY SERVER SWITCHER                                ║
echo ╚═══════════════════════════════════════════════════════════════════╝
echo.
echo Available servers:
echo   1. cc1.evony.com
echo   2. cc2.evony.com (default)
echo   3. cc3.evony.com
echo   4. cc4.evony.com
echo   5. cc5.evony.com
echo   6. Custom server
echo.

set /p choice="Enter choice (1-6): "

if "%choice%"=="1" set SERVER=cc1
if "%choice%"=="2" set SERVER=cc2
if "%choice%"=="3" set SERVER=cc3
if "%choice%"=="4" set SERVER=cc4
if "%choice%"=="5" set SERVER=cc5
if "%choice%"=="6" (
    set /p SERVER="Enter server name (e.g., cc6): "
)

if "%SERVER%"=="" (
    echo Invalid choice.
    pause
    exit /b 1
)

echo.
echo Launching with server: %SERVER%.evony.com
echo.

cd /d "%~dp0"
powershell -ExecutionPolicy Bypass -File "scripts\Launch-EvonyRE.ps1" -Server %SERVER%
