@echo off
setlocal
title PMDG Livery Extractor

if "%~1"=="" (
    echo [ERROR] No file provided.
    echo Please drag and drop a .ptp file directly onto this batch script!
    pause
    exit /b 1
)

echo Starting extraction for: %~1
echo.

"%~dp0bin\Release\net472\PtpExtractor.exe" "%~1"

echo.
pause
