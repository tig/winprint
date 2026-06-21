@echo off
rem Thin wrapper so `scripts\build` works from cmd. Prefers pwsh, falls back to Windows
rem PowerShell. See build.ps1 for what it does (avoids the WinPrint.Analyzers lock race).
where pwsh >nul 2>nul
if %errorlevel%==0 (
    pwsh -NoProfile -ExecutionPolicy Bypass -File "%~dp0build.ps1" %*
) else (
    powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0build.ps1" %*
)
exit /b %errorlevel%
