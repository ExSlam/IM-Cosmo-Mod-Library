$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Push-Location $root
try {
  dotnet build .\IMDataCore.DataMigrationTool.sln -c Release
  Write-Host "Build complete. Output is under src\IMDataCore.DataMigrationTool\bin\Release\net8.0-windows\."
} finally { Pop-Location }
