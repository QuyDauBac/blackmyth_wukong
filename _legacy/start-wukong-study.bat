@echo off
setlocal
cd /d "%~dp0"
where node >nul 2>&1
if errorlevel 1 (
  echo Node.js was not found. Install Node.js, then run this file again.
  pause
  exit /b 1
)
start "Wukong Study Server" node "%~dp0server\server.js"
timeout /t 2 /nobreak >nul
start "" "http://127.0.0.1:3000"
