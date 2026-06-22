$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot
Write-Host 'Publishing OPALNOVA standalone Release build...'
dotnet restore
dotnet publish .\JewelleryBusinessManager.csproj -c Release -p:PublishProfile=win-x64-self-contained
Write-Host ''
Write-Host 'Done. Standalone files are in:'
Write-Host (Join-Path $PSScriptRoot 'bin\Release\net10.0-windows\win-x64\publish')
