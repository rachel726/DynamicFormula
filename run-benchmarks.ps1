# ==============================================================
#  run-benchmarks.ps1
#  Runs all 3 calculation methods in sequence: SQL + C# -> Node.js
#
#  Prerequisites:
#    1. Run in SSMS: 01_schema -> 02_seed -> 03_formulas -> 04_sp
#    2. Run this script from the project root folder
# ==============================================================

$ErrorActionPreference = "Stop"
$root = $PSScriptRoot

Write-Host ""
Write-Host "==============================================================" -ForegroundColor Cyan
Write-Host "   Dynamic Formula Engine -- Full Benchmark Run              " -ForegroundColor Cyan
Write-Host "   SQL + C# .NET + Node.js  x  1,000,000 rows               " -ForegroundColor Cyan
Write-Host "==============================================================" -ForegroundColor Cyan
Write-Host ""

# -- Step 1: C# (runs SQL Stored Procedure + C# Compiled Lambda) --
Write-Host "[1/3] Running C# Benchmark (SQL + C# methods)..." -ForegroundColor Yellow
Set-Location "$root\src\DynamicFormula.Benchmarks"
dotnet run -c Release
if ($LASTEXITCODE -ne 0) { Write-Host "FAILED: C# benchmark" -ForegroundColor Red; exit 1 }

# -- Step 2: Node.js --
Write-Host ""
Write-Host "[2/3] Running Node.js Calculator..." -ForegroundColor Yellow
Set-Location "$root\node-calculator"

if (-not (Test-Path "node_modules")) {
    Write-Host "  Installing dependencies..." -ForegroundColor Gray
    npm install
}

node calculate.js
if ($LASTEXITCODE -ne 0) { Write-Host "FAILED: Node.js calculator" -ForegroundColor Red; exit 1 }

# -- Step 3: Export JSON once -- all 3 methods are now in DB --
Write-Host ""
Write-Host "[3/3] Exporting report.json (all 3 methods)..." -ForegroundColor Yellow
Set-Location "$root\src\DynamicFormula.Benchmarks"
dotnet run -c Release -- --export-only
if ($LASTEXITCODE -ne 0) { Write-Host "FAILED: JSON export" -ForegroundColor Red; exit 1 }

# -- Done --
Set-Location $root
Write-Host ""
Write-Host "==============================================================" -ForegroundColor Green
Write-Host "  All 3 methods complete. report.json updated.               " -ForegroundColor Green
Write-Host "  Next: run 05_verify_results.sql in SSMS to verify results. " -ForegroundColor Green
Write-Host "==============================================================" -ForegroundColor Green
Write-Host ""
