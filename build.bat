@echo off
set MSBUILD=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe
set ROOT=%~dp0

echo === WeavyTaskbar Build ===
%MSBUILD% "%ROOT%WeavyTaskbar.csproj" /p:Configuration=Release /v:minimal
if %ERRORLEVEL% NEQ 0 (
    echo Build FAILED!
    exit /b 1
)

echo === Copy to root ===
copy /Y "%ROOT%bin\Release\WeavyTaskbar.exe" "%ROOT%" >nul
if not exist "%ROOT%Styles" mkdir "%ROOT%Styles"
echo Done: WeavyTaskbar.exe
echo Edit .cs files in Styles\ folder, restart to apply.
