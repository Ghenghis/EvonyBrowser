<#
.SYNOPSIS
    Svony Browser V7.0 - Test Runner Script
    Runs all tests with coverage reporting

.DESCRIPTION
    Executes unit tests, integration tests, and generates coverage reports.
    Requires 95% coverage to pass.

.EXAMPLE
    .\run-tests.ps1
    .\run-tests.ps1 -Verbose
#>

param(
    [switch]$Verbose,
    [switch]$NoCoverage,
    [int]$RequiredCoverage = 95
)

$ErrorActionPreference = "Stop"

Write-Host "╔════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║        SVONY BROWSER V7.0 - TEST RUNNER                    ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

# Clean previous results
Write-Host "Cleaning previous test results..." -ForegroundColor Yellow
Remove-Item -Recurse -Force ..\TestResults -ErrorAction SilentlyContinue
Remove-Item -Recurse -Force ..\coveragereport -ErrorAction SilentlyContinue

# Build test project
Write-Host "Building test project..." -ForegroundColor Yellow
dotnet build ..\SvonyBrowser.Tests\SvonyBrowser.Tests.csproj -c Release

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}

# Run tests
Write-Host "Running tests..." -ForegroundColor Yellow

$testArgs = @(
    "test",
    "..\SvonyBrowser.Tests\SvonyBrowser.Tests.csproj",
    "--no-build",
    "-c", "Release",
    "--results-directory", "..\TestResults",
    "--logger", "trx;LogFileName=results.trx",
    "--logger", "console;verbosity=normal"
)

if (!$NoCoverage) {
    $testArgs += "--collect:XPlat Code Coverage"
    $testArgs += "--settings", "..\SvonyBrowser.Tests\coverlet.runsettings"
}

if ($Verbose) {
    $testArgs += "-v", "detailed"
}

& dotnet $testArgs

$testExitCode = $LASTEXITCODE

# Generate coverage report
if (!$NoCoverage) {
    Write-Host ""
    Write-Host "Generating coverage report..." -ForegroundColor Yellow
    
    # Install report generator if not present
    dotnet tool install -g dotnet-reportgenerator-globaltool 2>$null
    
    $coverageFiles = Get-ChildItem -Path ..\TestResults -Recurse -Filter "coverage.cobertura.xml"
    
    if ($coverageFiles.Count -gt 0) {
        $coverageFile = $coverageFiles[0].FullName
        
        reportgenerator `
            -reports:"$coverageFile" `
            -targetdir:"..\coveragereport" `
            -reporttypes:"Html;Badges;TextSummary"
        
        # Parse coverage
        [xml]$xml = Get-Content $coverageFile
        $lineRate = [double]$xml.coverage.'line-rate'
        $branchRate = [double]$xml.coverage.'branch-rate'
        $lineCoverage = [math]::Round($lineRate * 100, 2)
        $branchCoverage = [math]::Round($branchRate * 100, 2)
        
        Write-Host ""
        Write-Host "╔════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
        Write-Host "║                    COVERAGE REPORT                         ║" -ForegroundColor Cyan
        Write-Host "╚════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
        Write-Host ""
        Write-Host "Line Coverage:   $lineCoverage%" -ForegroundColor $(if ($lineCoverage -ge $RequiredCoverage) { "Green" } else { "Red" })
        Write-Host "Branch Coverage: $branchCoverage%" -ForegroundColor $(if ($branchCoverage -ge $RequiredCoverage) { "Green" } else { "Red" })
        Write-Host ""
        Write-Host "HTML Report: ..\coveragereport\index.html"
        
        if ($lineCoverage -lt $RequiredCoverage) {
            Write-Host ""
            Write-Host "COVERAGE FAILED: $lineCoverage% (Required: $RequiredCoverage%)" -ForegroundColor Red
            exit 1
        } else {
            Write-Host ""
            Write-Host "COVERAGE PASSED: $lineCoverage% >= $RequiredCoverage%" -ForegroundColor Green
        }
    } else {
        Write-Host "No coverage files found!" -ForegroundColor Yellow
    }
}

# Summary
Write-Host ""
Write-Host "╔════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║                    TEST SUMMARY                            ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan

if ($testExitCode -eq 0) {
    Write-Host "All tests passed!" -ForegroundColor Green
} else {
    Write-Host "Some tests failed!" -ForegroundColor Red
    exit $testExitCode
}
