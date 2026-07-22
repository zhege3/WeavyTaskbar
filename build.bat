@echo off
set MSBUILD=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe
set ROOT=%~dp0

echo === WeavyTaskbar Build ===

if exist "%ROOT%bin" rmdir /s /q "%ROOT%bin"
if exist "%ROOT%obj" rmdir /s /q "%ROOT%obj"

%MSBUILD% "%ROOT%WeavyTaskbar.csproj" /p:Configuration=Release /v:minimal
if %ERRORLEVEL% NEQ 0 ( echo Build FAILED! & exit /b 1 )
copy /Y "%ROOT%bin\Release\WeavyTaskbar.exe" "%ROOT%" >nul

echo.
echo Done: WeavyTaskbar.exe
