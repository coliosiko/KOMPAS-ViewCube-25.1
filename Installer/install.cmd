@echo off
setlocal

net session >nul 2>&1
if not "%errorlevel%"=="0" (
    powershell.exe -NoProfile -ExecutionPolicy Bypass -Command "Start-Process -FilePath '%~f0' -Verb RunAs"
    exit /b
)

set "SOURCE_DIR=%~dp0"
set "INSTALL_DIR=%ProgramFiles%\KOMPAS ViewCube"
set "REGASM=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\RegAsm.exe"

if not exist "%REGASM%" (
    echo Windows x64 and .NET Framework 4.8 are required.
    pause
    exit /b 1
)
if not exist "%SOURCE_DIR%KompasViewCube.Plugin.dll" goto missing_files
if not exist "%SOURCE_DIR%KompasViewCube.exe" goto missing_files

echo Close KOMPAS-3D v25 before continuing.
pause

if not exist "%INSTALL_DIR%" mkdir "%INSTALL_DIR%"
if not exist "%INSTALL_DIR%" goto copy_error
copy /Y "%SOURCE_DIR%KompasViewCube.Plugin.dll" "%INSTALL_DIR%\" >nul
if errorlevel 1 goto copy_error
copy /Y "%SOURCE_DIR%KompasViewCube.exe" "%INSTALL_DIR%\" >nul
if errorlevel 1 goto copy_error
if exist "%SOURCE_DIR%KompasViewCube.exe.config" copy /Y "%SOURCE_DIR%KompasViewCube.exe.config" "%INSTALL_DIR%\" >nul
copy /Y "%SOURCE_DIR%uninstall.cmd" "%INSTALL_DIR%\" >nul
if errorlevel 1 goto copy_error

"%REGASM%" "%INSTALL_DIR%\KompasViewCube.Plugin.dll" /codebase
if errorlevel 1 goto registration_error

echo.
echo KOMPAS ViewCube was installed successfully.
echo Restart KOMPAS-3D, open Applications, then switch to the ActiveX tab.
pause
exit /b 0

:missing_files
echo Installation files are missing. Extract the whole archive first.
pause
exit /b 1

:registration_error
echo KOMPAS ViewCube registration failed.
pause
exit /b 1

:copy_error
echo Could not copy installation files. Close KOMPAS-3D and try again.
pause
exit /b 1