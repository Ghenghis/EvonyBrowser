@echo off
:: Evony RE Toolkit - Quick Setup
:: Run this first to install everything
title Evony RE Toolkit Setup

cd /d "%~dp0"

echo.
echo ╔═══════════════════════════════════════════════════════════════════╗
echo ║           EVONY RE TOOLKIT - Setup                                ║
echo ╚═══════════════════════════════════════════════════════════════════╝
echo.

powershell -ExecutionPolicy Bypass -File "scripts\Setup-EvonyRE.ps1" -All

pause
