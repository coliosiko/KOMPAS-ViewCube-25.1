@echo off
setlocal

net session >nul 2>&1
if not "%errorlevel%"=="0" (
    powershell.exe -NoProfile -ExecutionPolicy Bypass -Command "Start-Process -FilePath '%~f0' -Verb RunAs"
    exit /b
)

set "INSTALL_DIR=%ProgramFiles%\KOMPAS ViewCube"
set "REGASM=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\RegAsm.exe"

if not exist "%REGASM%" set "REGASM=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\RegAsm.exe"

echo Close KOMPAS-3D before continuing.
pause

if exist "%INSTALL_DIR%\KompasViewCube.Plugin.dll" (
    "%REGASM%" "%INSTALL_DIR%\KompasViewCube.Plugin.dll" /unregister
)

del /Q "%INSTALL_DIR%\KompasViewCube.Plugin.dll" 2>nul
del /Q "%INSTALL_DIR%\KompasViewCube.exe" 2>nul
del /Q "%INSTALL_DIR%\KompasViewCube.exe.config" 2>nul

echo.
echo KOMPAS ViewCube was uninstalled.
echo You can now delete this file and the folder containing it.
pause
