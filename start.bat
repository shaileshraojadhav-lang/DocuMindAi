@echo off
echo Qdrant and Ollama are running remotely on 20.193.139.150 — skipping local start.

echo.
echo Starting DocuMind on http://localhost:5050 ...
start "" "http://localhost:5050"

cd /d "%~dp0"
dotnet run --project DocuMind.csproj --no-launch-profile
pause
