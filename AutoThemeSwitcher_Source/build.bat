@echo off
chcp 65001 >nul
setlocal
cd /d "%~dp0"

set CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe
if not exist "%CSC%" set CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe

if not exist "%CSC%" (
    echo Could not find the C# compiler ^(csc.exe^).
    pause
    exit /b 1
)

set REFS=/reference:System.dll /reference:System.Windows.Forms.dll /reference:System.Drawing.dll

echo [1/2] Building the app...
"%CSC%" /nologo /target:winexe /optimize+ /codepage:65001 /win32icon:app.ico /out:AutoThemeSwitcher.exe %REFS% AutoThemeSwitcher.cs Fluent.cs
if errorlevel 1 goto fail

echo [2/2] Building the installer...
"%CSC%" /nologo /target:winexe /optimize+ /codepage:65001 /win32icon:app.ico /out:AutoThemeSwitcher_Setup.exe /resource:AutoThemeSwitcher.exe,AppPayload %REFS% Setup.cs Fluent.cs
if errorlevel 1 goto fail

echo.
echo Done! Installer created: AutoThemeSwitcher_Setup.exe
echo Opening the installer...
start "" "%~dp0AutoThemeSwitcher_Setup.exe"
timeout /t 3 >nul
exit /b 0

:fail
echo.
echo Build FAILED.
pause
exit /b 1
