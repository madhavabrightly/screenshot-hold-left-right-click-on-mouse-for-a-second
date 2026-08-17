@echo off
taskkill /IM MouseScreenshot.exe /F >nul 2>&1

if %errorlevel%==0 (
    echo Mouse Screenshot Service stopped.
) else (
    echo Mouse Screenshot Service is not running.
)

timeout /t 2 /nobreak >nul