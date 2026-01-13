@echo off
:: Evony RE Toolkit - Quick Launcher
:: Double-click this file to start the toolkit
title Evony RE Toolkit

cd /d "%~dp0"

echo.
echo ╔═══════════════════════════════════════════════════════════════════╗
echo ║           EVONY RE TOOLKIT - Quick Launcher                       ║
echo ╚═══════════════════════════════════════════════════════════════════╝
echo.

:: Check for PowerShell
where powershell >nul 2>&1
if %errorlevel% neq 0 (
    echo ERROR: PowerShell not found!
    pause
    exit /b 1
)

:: Launch the PowerShell script
powershell -ExecutionPolicy Bypass -File "scripts\Launch-EvonyRE.ps1" %*

pause
