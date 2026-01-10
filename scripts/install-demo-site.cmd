@echo off
REM Windows wrapper for install-demo-site.sh
REM Automatically uses Git Bash if available

where git >nul 2>nul
if %ERRORLEVEL% neq 0 (
    echo Error: Git is not installed or not in PATH.
    echo Please install Git for Windows from https://git-scm.com/download/win
    exit /b 1
)

REM Get the directory where this script is located
set "SCRIPT_DIR=%~dp0"

REM Try to find Git Bash
for /f "delims=" %%i in ('where git') do set "GIT_PATH=%%i"
set "GIT_DIR=%GIT_PATH:\cmd\git.exe=%"
set "GIT_DIR=%GIT_DIR:\bin\git.exe=%"
set "BASH_PATH=%GIT_DIR%\bin\bash.exe"

if not exist "%BASH_PATH%" (
    REM Try alternative path for Git Bash
    set "BASH_PATH=%GIT_DIR%\usr\bin\bash.exe"
)

if not exist "%BASH_PATH%" (
    echo Error: Could not find Git Bash.
    echo Please ensure Git for Windows is properly installed.
    exit /b 1
)

REM Run the bash script
"%BASH_PATH%" "%SCRIPT_DIR%install-demo-site.sh"
