@echo off
if exist "%~dp0portable\FolderFlow.exe" (
  start "" "%~dp0portable\FolderFlow.exe"
  exit /b 0
)
echo Build the app first using publish.ps1, then open this launcher again.
echo See README.md for instructions.
pause
