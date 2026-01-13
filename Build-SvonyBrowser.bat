@echo off
title Build Svony Browser
color 0E

echo.
echo  ╔═══════════════════════════════════════════════════════════╗
echo  ║                 BUILD SVONY BROWSER                       ║
echo  ╚═══════════════════════════════════════════════════════════╝
echo.

set "BASEPATH=%~dp0"
cd /d "%BASEPATH%SvonyBrowser"

echo [1/3] Restoring NuGet packages...
dotnet restore
if %ERRORLEVEL% neq 0 (
    echo [ERROR] Failed to restore packages
    pause
    exit /b 1
)
echo       Done ✓
echo.

echo [2/3] Building Release configuration...
dotnet build -c Release
if %ERRORLEVEL% neq 0 (
    echo [ERROR] Build failed
    pause
    exit /b 1
)
echo       Done ✓
echo.

echo [3/3] Verifying output...
if exist "bin\Release\net6.0-windows\SvonyBrowser.exe" (
    echo       SvonyBrowser.exe created ✓
    echo.
    echo  ═══════════════════════════════════════════════════════════
    echo   Build successful!
    echo   
    echo   Output: %BASEPATH%SvonyBrowser\bin\Release\net6.0-windows\
    echo   
    echo   Run Launch-SvonyBrowser.bat to start the application.
    echo  ═══════════════════════════════════════════════════════════
) else (
    echo [ERROR] Output file not found!
)

echo.
pause
