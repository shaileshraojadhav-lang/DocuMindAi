@echo off
:: Must be run as Administrator
net session >nul 2>&1 || (echo Run as Administrator & pause & exit /b 1)

set SVC_NAME=DocuMind
set SVC_DISPLAY=DocuMind API Service
set SVC_DESC=DocuMind document intelligence API (ASP.NET Core 8)
set PUBLISH_DIR=%~dp0publish

if /i "%1"=="uninstall" goto uninstall
if /i "%1"=="stop"      goto stop
if /i "%1"=="start"     goto start

:: ── Publish ──────────────────────────────────────────────────────────
echo Publishing...
dotnet publish "%~dp0DocuMind.csproj" -c Release -r win-x64 --no-self-contained -o "%PUBLISH_DIR%"
if errorlevel 1 (echo Publish failed & pause & exit /b 1)

:: ── Install ───────────────────────────────────────────────────────────
sc query %SVC_NAME% >nul 2>&1 && goto already_installed

sc create %SVC_NAME% binPath= "\"%PUBLISH_DIR%\DocuMind.exe\"" ^
    DisplayName= "%SVC_DISPLAY%" start= auto
sc description %SVC_NAME% "%SVC_DESC%"
sc failure %SVC_NAME% reset= 86400 actions= restart/5000/restart/10000/restart/30000

:: Open firewall for HTTP and HTTPS
netsh advfirewall firewall add rule name="%SVC_DISPLAY% HTTP"  protocol=TCP dir=in localport=5000 action=allow >nul
netsh advfirewall firewall add rule name="%SVC_DISPLAY% HTTPS" protocol=TCP dir=in localport=5001 action=allow >nul

:start
sc start %SVC_NAME%
echo Service started. Listening on http://0.0.0.0:5000 and https://0.0.0.0:5001
goto end

:already_installed
echo Service already installed. Use: %~nx0 stop / start / uninstall
goto end

:stop
sc stop %SVC_NAME%
goto end

:uninstall
sc stop %SVC_NAME% >nul 2>&1
sc delete %SVC_NAME%
netsh advfirewall firewall delete rule name="%SVC_DISPLAY% HTTP"  >nul
netsh advfirewall firewall delete rule name="%SVC_DISPLAY% HTTPS" >nul
echo Service removed.

:end
pause
