@echo off
title Svony Browser Launcher
color 0A
echo.
echo  ╔═══════════════════════════════════════════════════════════╗
echo  ║              SVONY BROWSER - Dual Panel Evony             ║
echo  ║           AutoEvony + EvonyClient Side-by-Side            ║
echo  ╚═══════════════════════════════════════════════════════════╝
echo.

REM Set base path
set "BASEPATH=%~dp0"
cd /d "%BASEPATH%"

REM Check for Fiddler
echo [1/3] Checking Fiddler proxy...
tasklist /FI "IMAGENAME eq Fiddler.exe" 2>NUL | find /I "Fiddler.exe" >NUL
if "%ERRORLEVEL%"=="0" (
    echo       Fiddler is already running ✓
) else (
    echo       Starting Fiddler...
    if exist "%BASEPATH%Fiddler\Fiddler.exe" (
        start "" "%BASEPATH%Fiddler\Fiddler.exe"
        timeout /t 3 /nobreak >NUL
        echo       Fiddler started ✓
    ) else (
        echo       [WARNING] Fiddler not found at %BASEPATH%Fiddler\
        echo       Traffic capture will not work without Fiddler.
    )
)

REM Wait for proxy
echo.
echo [2/3] Waiting for proxy (127.0.0.1:8888)...
set "ATTEMPTS=0"
:waitloop
set /a ATTEMPTS+=1
if %ATTEMPTS% gtr 10 (
    echo       [WARNING] Proxy not responding after 10 attempts.
    echo       Continuing anyway...
    goto startbrowser
)
powershell -Command "try { $c = New-Object System.Net.Sockets.TcpClient('127.0.0.1', 8888); $c.Close(); exit 0 } catch { exit 1 }" >NUL 2>&1
if "%ERRORLEVEL%"=="0" (
    echo       Proxy ready ✓
    goto startbrowser
)
timeout /t 1 /nobreak >NUL
goto waitloop

:startbrowser
echo.
echo [3/3] Starting Svony Browser...

REM Check for built executable
set "EXEPATH=%BASEPATH%SvonyBrowser\bin\Release\net6.0-windows\SvonyBrowser.exe"
if not exist "%EXEPATH%" (
    set "EXEPATH=%BASEPATH%SvonyBrowser\bin\Debug\net6.0-windows\SvonyBrowser.exe"
)
if not exist "%EXEPATH%" (
    echo       [ERROR] SvonyBrowser.exe not found!
    echo       Please build the project first:
    echo         cd SvonyBrowser
    echo         dotnet build -c Release
    echo.
    pause
    exit /b 1
)

start "" "%EXEPATH%"
echo       Svony Browser launched ✓

echo.
echo ═══════════════════════════════════════════════════════════════
echo   Svony Browser is now running!
echo   
echo   Keyboard Shortcuts:
echo     Ctrl+1  = Show Bot Only
echo     Ctrl+2  = Show Both Panels
echo     Ctrl+3  = Show Client Only
echo     Ctrl+S  = Swap Panels
echo     F5      = Reload Left Panel
echo     F6      = Reload Right Panel
echo ═══════════════════════════════════════════════════════════════
echo.
timeout /t 5
