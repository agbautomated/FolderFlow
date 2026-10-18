param([string]$DotNetPath = 'dotnet')
$ErrorActionPreference = 'Stop'
& $DotNetPath build (Join-Path $PSScriptRoot 'src\FolderFlow.App\FolderFlow.App.csproj') -c Release --configfile (Join-Path $PSScriptRoot 'NuGet.Config')
if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
