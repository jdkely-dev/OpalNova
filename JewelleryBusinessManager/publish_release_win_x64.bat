@echo off
setlocal
cd /d "%~dp0"
echo Publishing OPALNOVA standalone Release build...
dotnet restore
if errorlevel 1 goto failed
dotnet publish JewelleryBusinessManager.csproj -c Release -p:PublishProfile=win-x64-self-contained
if errorlevel 1 goto failed
echo.
echo Done. Standalone files are in:
echo %CD%\bin\Release\net10.0-windows\win-x64\publish\
echo.
pause
exit /b 0
:failed
echo.
echo Publish failed. Open the project in Visual Studio, confirm the .NET Desktop SDK is installed, then try again.
pause
exit /b 1
