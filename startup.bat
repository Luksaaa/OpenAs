@echo off
setlocal

set "ROOT=%~dp0"
set "PROJECT=%ROOT%OpenAs\OpenAs.csproj"

echo OpenAs startup
echo.

where dotnet >nul 2>nul
if errorlevel 1 (
    echo .NET SDK was not found.
    call :InstallDotNetSdk
) else (
    dotnet --list-sdks | findstr /R "^8\." >nul 2>nul
    if errorlevel 1 (
        echo .NET 8 SDK was not found.
        call :InstallDotNetSdk
    )
)

where dotnet >nul 2>nul
if errorlevel 1 (
    echo.
    echo dotnet is still not available. Install .NET 8 SDK manually:
    echo https://dotnet.microsoft.com/download/dotnet/8.0
    pause
    exit /b 1
)

dotnet --list-sdks | findstr /R "^8\." >nul 2>nul
if errorlevel 1 (
    echo.
    echo .NET 8 SDK is still not available. Install it manually:
    echo https://dotnet.microsoft.com/download/dotnet/8.0
    pause
    exit /b 1
)

echo Restoring packages...
dotnet restore "%PROJECT%"
if errorlevel 1 goto :Fail

echo Building OpenAs...
dotnet build "%PROJECT%"
if errorlevel 1 goto :Fail

echo Starting OpenAs...
dotnet run --project "%PROJECT%"
exit /b %errorlevel%

:InstallDotNetSdk
where winget >nul 2>nul
if errorlevel 1 (
    echo winget was not found. Cannot install .NET automatically.
    exit /b 0
)

echo Installing .NET 8 SDK with winget...
winget install --id Microsoft.DotNet.SDK.8 --source winget --accept-package-agreements --accept-source-agreements
exit /b 0

:Fail
echo.
echo Startup failed. Check the error above.
pause
exit /b 1
