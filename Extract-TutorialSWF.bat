@echo off
:: Extract Tutorial SWF - Quick Launcher
title Evony Tutorial SWF Extractor

cd /d "%~dp0"

echo.
echo ╔═══════════════════════════════════════════════════════════════════╗
echo ║         EVONY TUTORIAL SWF EXTRACTOR                              ║
echo ╚═══════════════════════════════════════════════════════════════════╝
echo.
echo This tool helps capture the tutorial/advisor SWF files.
echo.
echo Choose mode:
echo   1. Scan existing captures
echo   2. Watch mode (real-time monitoring)
echo.

set /p choice="Enter choice (1 or 2): "

if "%choice%"=="1" (
    powershell -ExecutionPolicy Bypass -File "scripts\Extract-TutorialSWF.ps1"
) else if "%choice%"=="2" (
    powershell -ExecutionPolicy Bypass -File "scripts\Extract-TutorialSWF.ps1" -WatchMode
) else (
    echo Invalid choice.
    pause
)
